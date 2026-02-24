using Microsoft.AspNetCore.SignalR;
using Nexus.API.Hubs;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;

namespace Nexus.Application.Services;

/// <summary>
/// Task management service with real-time notifications
/// </summary>
public interface ITaskService
{
    Task<TaskItem> CreateTaskAsync(CreateTaskRequest request, string createdBy);
    Task<TaskItem> AssignTaskAsync(long taskId, string assignedTo, string assignedBy);
    Task<TaskItem> UpdateTaskStatusAsync(long taskId, TaskStatus newStatus, string updatedBy);
    Task<TaskItem> AddCommentAsync(long taskId, string comment, string commentedBy);
    Task<IEnumerable<TaskItem>> GetUserTasksAsync(string userId);
    Task<IEnumerable<TaskItem>> GetPendingTasksAsync(string userId);
}

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        INotificationRepository notificationRepository,
        IHubContext<NotificationHub> notificationHub,
        ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _notificationRepository = notificationRepository;
        _notificationHub = notificationHub;
        _logger = logger;
    }

    /// <summary>
    /// Yeni tapşırıq yaradır və təyinat bildirişi göndərir
    /// </summary>
    public async Task<TaskItem> CreateTaskAsync(CreateTaskRequest request, string createdBy)
    {
        // Tapşırığı yarat
        var task = new TaskItem
        {
            ProjectId = request.ProjectId,
            TaskTitle = request.Title,
            TaskDescription = request.Description,
            AssignedTo = request.AssignedTo,
            CreatedBy = createdBy,
            Status = TaskStatus.Todo,
            Priority = request.Priority,
            DueDate = request.DueDate,
            DocumentNodeId = request.DocumentNodeId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskRepository.AddAsync(task);
        await _taskRepository.SaveChangesAsync();

        // Əgər kimsəyə təyin edilibsə, bildiriş göndər
        if (!string.IsNullOrEmpty(request.AssignedTo))
        {
            await SendTaskAssignedNotification(task, createdBy);
        }

        _logger.LogInformation(
            "Task {TaskId} created by {CreatedBy} and assigned to {AssignedTo}", 
            task.TaskId, createdBy, request.AssignedTo);

        return task;
    }

    /// <summary>
    /// Mövcud tapşırığı başqa istifadəçiyə təyin edir
    /// </summary>
    public async Task<TaskItem> AssignTaskAsync(long taskId, string assignedTo, string assignedBy)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
            throw new Exception($"Task {taskId} not found");

        var oldAssignee = task.AssignedTo;
        task.AssignedTo = assignedTo;
        task.ModifiedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        // Database-də bildiriş yarat
        var notification = new Notification
        {
            RecipientUserId = assignedTo,
            SenderUserId = assignedBy,
            Type = NotificationType.TaskAssigned,
            Title = "Yeni tapşırıq təyin edildi",
            Message = $"'{task.TaskTitle}' tapşırığı sizə təyin edildi",
            EntityType = "Task",
            EntityId = task.TaskId,
            Metadata = System.Text.Json.JsonSerializer.Serialize(new { 
                TaskTitle = task.TaskTitle,
                Priority = task.Priority.ToString()
            }),
            OrganizationCode = task.Project?.OrganizationCode ?? "default",
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(notification);
        await _notificationRepository.SaveChangesAsync();

        // Real-time bildiriş göndər (əgər online-dırsa)
        var project = await _projectRepository.GetByIdAsync(task.ProjectId);
        await NotificationHub.SendTaskAssignedNotification(
            _notificationHub,
            assignedTo,
            task,
            assignedBy,
            project?.ProjectName ?? "Naməlum layihə");

        // Köhnə təyin olunana da bildiriş (əgər varsa və fərqlidirsə)
        if (!string.IsNullOrEmpty(oldAssignee) && oldAssignee != assignedTo)
        {
            await NotificationHub.SendToUser(_notificationHub, oldAssignee, new Notification
            {
                RecipientUserId = oldAssignee,
                Type = NotificationType.TaskAssigned,
                Title = "Tapşırıq yenidən təyin edildi",
                Message = $"'{task.TaskTitle}' tapşırığı başqa istifadəçiyə təyin edildi",
                EntityType = "Task",
                EntityId = task.TaskId
            });
        }

        _logger.LogInformation(
            "Task {TaskId} reassigned from {OldAssignee} to {NewAssignee} by {AssignedBy}",
            taskId, oldAssignee, assignedTo, assignedBy);

        return task;
    }

    /// <summary>
    /// Tapşırıq statusunu dəyişir və əlaqəli şəxslərə bildiriş göndərir
    /// </summary>
    public async Task<TaskItem> UpdateTaskStatusAsync(long taskId, TaskStatus newStatus, string updatedBy)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
            throw new Exception($"Task {taskId} not found");

        var oldStatus = task.Status;
        if (oldStatus == newStatus)
            return task; // Status dəyişməyibsə heç nə etmə

        task.Status = newStatus;
        task.ModifiedAt = DateTime.UtcNow;

        if (newStatus == TaskStatus.Done)
        {
            task.CompletedAt = DateTime.UtcNow;
        }

        await _taskRepository.SaveChangesAsync();

        // Bildiriş göndər
        await NotificationHub.SendTaskStatusChangedNotification(
            _notificationHub,
            task.AssignedTo ?? "",
            task.CreatedBy,
            task,
            oldStatus,
            updatedBy);

        // Database-də bildiriş yarat
        if (!string.IsNullOrEmpty(task.AssignedTo) && task.AssignedTo != updatedBy)
        {
            var notification = new Notification
            {
                RecipientUserId = task.AssignedTo,
                SenderUserId = updatedBy,
                Type = NotificationType.TaskStatusChanged,
                Title = "Tapşırıq statusu dəyişdi",
                Message = $"'{task.TaskTitle}' statusu {oldStatus} -> {newStatus} oldu",
                EntityType = "Task",
                EntityId = task.TaskId,
                OrganizationCode = task.Project?.OrganizationCode ?? "default"
            };
            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Task {TaskId} status changed from {OldStatus} to {NewStatus} by {UpdatedBy}",
            taskId, oldStatus, newStatus, updatedBy);

        return task;
    }

    /// <summary>
    /// Tapşırığa şərh əlavə edir
    /// </summary>
    public async Task<TaskItem> AddCommentAsync(long taskId, string comment, string commentedBy)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
            throw new Exception($"Task {taskId} not found");

        var taskComment = new TaskComment
        {
            TaskId = taskId,
            Comment = comment,
            CommentedBy = commentedBy,
            CommentedAt = DateTime.UtcNow
        };

        await _taskRepository.AddCommentAsync(taskComment);
        await _taskRepository.SaveChangesAsync();

        // Tapşırıq sahibinə bildiriş
        if (!string.IsNullOrEmpty(task.AssignedTo) && task.AssignedTo != commentedBy)
        {
            var notification = new Notification
            {
                RecipientUserId = task.AssignedTo,
                SenderUserId = commentedBy,
                Type = NotificationType.TaskCommentAdded,
                Title = "Tapşırığa yeni şərh",
                Message = $"'{task.TaskTitle}' tapşırığına şərh əlavə edildi",
                EntityType = "Task",
                EntityId = task.TaskId,
                OrganizationCode = task.Project?.OrganizationCode ?? "default"
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();

            await NotificationHub.SendToUser(_notificationHub, task.AssignedTo, notification);
        }

        return task;
    }

    /// <summary>
    /// İstifadəçinin bütün tapşırıqlarını gətir
    /// </summary>
    public async Task<IEnumerable<TaskItem>> GetUserTasksAsync(string userId)
    {
        return await _taskRepository.GetTasksByAssigneeAsync(userId);
    }

    /// <summary>
    /// İstifadəçinin gözləyən tapşırıqlarını gətir
    /// </summary>
    public async Task<IEnumerable<TaskItem>> GetPendingTasksAsync(string userId)
    {
        return await _taskRepository.GetPendingTasksByAssigneeAsync(userId);
    }

    private async Task SendTaskAssignedNotification(TaskItem task, string assignedBy)
    {
        if (string.IsNullOrEmpty(task.AssignedTo)) return;

        var project = await _projectRepository.GetByIdAsync(task.ProjectId);
        
        // Database bildirişi
        var notification = new Notification
        {
            RecipientUserId = task.AssignedTo,
            SenderUserId = assignedBy,
            Type = NotificationType.TaskAssigned,
            Title = "Yeni tapşırıq",
            Message = $"'{task.TaskTitle}' sizə təyin edildi",
            EntityType = "Task",
            EntityId = task.TaskId,
            OrganizationCode = project?.OrganizationCode ?? "default"
        };

        await _notificationRepository.AddAsync(notification);
        await _notificationRepository.SaveChangesAsync();

        // Real-time bildiriş
        await NotificationHub.SendTaskAssignedNotification(
            _notificationHub,
            task.AssignedTo,
            task,
            assignedBy,
            project?.ProjectName ?? "Naməlum layihə");
    }
}

// Request DTOs
public class CreateTaskRequest
{
    public long ProjectId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? AssignedTo { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public long? DocumentNodeId { get; set; }
}
