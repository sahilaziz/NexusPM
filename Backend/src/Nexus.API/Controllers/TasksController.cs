using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Nexus.API.Hubs;
using Nexus.Application.Services;
using Nexus.Domain.Entities;

namespace Nexus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly INotificationRepository _notificationRepository;
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly ILogger<TasksController> _logger;

    public TasksController(
        ITaskService taskService,
        INotificationRepository notificationRepository,
        IHubContext<NotificationHub> notificationHub,
        ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _notificationRepository = notificationRepository;
        _notificationHub = notificationHub;
        _logger = logger;
    }

    /// <summary>
    /// Cari istifadəçinin tapşırıqlarını gətir
    /// </summary>
    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks()
    {
        var userId = User.Identity?.Name ?? "unknown";
        var tasks = await _taskService.GetUserTasksAsync(userId);
        return Ok(tasks);
    }

    /// <summary>
    /// Cari istifadəçinin gözləyən tapşırıqlarını gətir
    /// </summary>
    [HttpGet("my-pending")]
    public async Task<IActionResult> GetMyPendingTasks()
    {
        var userId = User.Identity?.Name ?? "unknown";
        var tasks = await _taskService.GetPendingTasksAsync(userId);
        return Ok(tasks);
    }

    /// <summary>
    /// Yeni tapşırıq yaradır və təyin edilən şəxsə bildiriş göndərir
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        try
        {
            var createdBy = User.Identity?.Name ?? "system";
            var task = await _taskService.CreateTaskAsync(request, createdBy);
            
            _logger.LogInformation(
                "Task {TaskId} created by {User}. Assigned to: {Assignee}", 
                task.TaskId, createdBy, request.AssignedTo);

            return CreatedAtAction(
                nameof(GetTaskById), 
                new { id = task.TaskId }, 
                new { 
                    Message = "Tapşırıq yaradıldı", 
                    TaskId = task.TaskId,
                    AssignedTo = task.AssignedTo,
                    RealTimeNotification = !string.IsNullOrEmpty(task.AssignedTo)
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Tapşırığı ID-yə görə gətir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaskById(long id)
    {
        // TaskService-də GetById metodu əlavə olunmalı
        // İndi sadəcə mock qaytarıram
        return Ok(new { TaskId = id });
    }

    /// <summary>
    /// Tapşırığı başqa istifadəçiyə təyin edir
    /// </summary>
    [HttpPost("{taskId}/assign")]
    public async Task<IActionResult> AssignTask(long taskId, [FromBody] AssignTaskRequest request)
    {
        try
        {
            var assignedBy = User.Identity?.Name ?? "system";
            var task = await _taskService.AssignTaskAsync(taskId, request.AssignedTo, assignedBy);

            return Ok(new 
            { 
                Message = $"Tapşırıq {request.AssignedTo} istifadəçisinə təyin edildi",
                TaskId = task.TaskId,
                AssignedTo = task.AssignedTo,
                RealTimeDelivered = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning task {TaskId}", taskId);
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Tapşırıq statusunu dəyişir
    /// </summary>
    [HttpPut("{taskId}/status")]
    public async Task<IActionResult> UpdateStatus(long taskId, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            var updatedBy = User.Identity?.Name ?? "system";
            var task = await _taskService.UpdateTaskStatusAsync(taskId, request.Status, updatedBy);

            return Ok(new 
            { 
                Message = "Status yeniləndi",
                TaskId = task.TaskId,
                NewStatus = task.Status.ToString(),
                NotificationsSent = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task status {TaskId}", taskId);
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Tapşırığa şərh əlavə edir
    /// </summary>
    [HttpPost("{taskId}/comments")]
    public async Task<IActionResult> AddComment(long taskId, [FromBody] AddCommentRequest request)
    {
        try
        {
            var commentedBy = User.Identity?.Name ?? "system";
            await _taskService.AddCommentAsync(taskId, request.Comment, commentedBy);

            return Ok(new { Message = "Şərh əlavə edildi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to task {TaskId}", taskId);
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    // ==================== NOTIFICATION ENDPOINTS ====================

    /// <summary>
    /// Cari istifadəçinin oxunmamış bildirişlərini gətir
    /// </summary>
    [HttpGet("notifications/unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        var userId = User.Identity?.Name ?? "unknown";
        var notifications = await _notificationRepository.GetUnreadByUserAsync(userId);
        var count = await _notificationRepository.GetUnreadCountAsync(userId);

        return Ok(new 
        { 
            Notifications = notifications,
            UnreadCount = count
        });
    }

    /// <summary>
    /// Cari istifadəçinin son bildirişlərini gətir
    /// </summary>
    [HttpGet("notifications/recent")]
    public async Task<IActionResult> GetRecentNotifications([FromQuery] int count = 20)
    {
        var userId = User.Identity?.Name ?? "unknown";
        var notifications = await _notificationRepository.GetRecentByUserAsync(userId, count);
        var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);

        return Ok(new 
        { 
            Notifications = notifications,
            UnreadCount = unreadCount
        });
    }

    /// <summary>
    /// Bildirişi oxundu olaraq işarələ
    /// </summary>
    [HttpPut("notifications/{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(long notificationId)
    {
        await _notificationRepository.MarkAsReadAsync(notificationId);
        return Ok(new { Message = "Bildiriş oxundu olaraq işarələndi" });
    }

    /// <summary>
    /// Bütün bildirişləri oxundu olaraq işarələ
    /// </summary>
    [HttpPut("notifications/read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.Identity?.Name ?? "unknown";
        await _notificationRepository.MarkAllAsReadAsync(userId);
        return Ok(new { Message = "Bütün bildirişlər oxundu olaraq işarələndi" });
    }

    /// <summary>
    /// Online istifadəçiləri əldə et (admin üçün)
    /// </summary>
    [HttpGet("online-users")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetOnlineUsers()
    {
        var onlineUsers = NotificationHub.GetOnlineUsers();
        return Ok(new 
        { 
            OnlineUsers = onlineUsers.Select(u => new 
            { 
                UserId = u.Key, 
                ConnectionCount = u.Value.Count 
            }),
            TotalOnline = onlineUsers.Count
        });
    }
}

// Request DTOs
public class AssignTaskRequest
{
    public string AssignedTo { get; set; } = null!;
}

public class UpdateStatusRequest
{
    public TaskStatus Status { get; set; }
}

public class AddCommentRequest
{
    public string Comment { get; set; } = null!;
}
