using MediatR;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;
using TaskStatus = Nexus.Domain.Entities.TaskStatus;

namespace Nexus.Application.CQRS.Queries.Dashboard;

// ============== QUERIES ==============

/// <summary>
/// İstifadəçinin dashboard məlumatları
/// </summary>
public record GetUserDashboardQuery(long UserId) : IRequest<UserDashboardDto>;

/// <summary>
/// Layihə dashboard
/// </summary>
public record GetProjectDashboardQuery(long ProjectId) : IRequest<ProjectDashboardDto>;

/// <summary>
/// Admin ümumi dashboard
/// </summary>
public record GetAdminDashboardQuery : IRequest<AdminDashboardDto>;

// ============== DTOS ==============

/// <summary>
/// İstifadəçi Dashboard
/// </summary>
public class UserDashboardDto
{
    public long UserId { get; set; }
    public string UserName { get; set; } = null!;
    
    // My Tasks Summary
    public TaskSummaryDto MyTasks { get; set; } = null!;
    
    // Today's Tasks
    public List<TaskItemDto> TodaysTasks { get; set; } = new();
    
    // Overdue Tasks
    public List<TaskItemDto> OverdueTasks { get; set; } = new();
    
    // Upcoming Deadlines
    public List<TaskItemDto> UpcomingDeadlines { get; set; } = new();
    
    // Time Tracking Today
    public TodayTimeDto TodayTime { get; set; } = null!;
    
    // Notifications
    public int UnreadNotificationsCount { get; set; }
    public List<NotificationDto> RecentNotifications { get; set; } = new();
    
    // Projects
    public List<ProjectSummaryDto> MyProjects { get; set; } = new();
}

/// <summary>
/// Layihə Dashboard
/// </summary>
public class ProjectDashboardDto
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = null!;
    public string? ProjectDescription { get; set; }
    public ProjectStatus Status { get; set; }
    
    // Task Statistics
    public TaskStatisticsDto TaskStats { get; set; } = null!;
    
    // Progress
    public double OverallProgress { get; set; }
    public int CompletedTasks { get; set; }
    public int TotalTasks { get; set; }
    
    // Team
    public List<TeamMemberDto> TeamMembers { get; set; } = new();
    
    // Recent Activity
    public List<ActivityDto> RecentActivity { get; set; } = new();
    
    // Upcoming Milestones
    public List<MilestoneDto> UpcomingMilestones { get; set; } = new();
    
    // Burndown data (for charts)
    public List<BurndownDataPointDto> BurndownData { get; set; } = new();
}

/// <summary>
/// Admin Dashboard
/// </summary>
public class AdminDashboardDto
{
    // System Overview
    public int TotalUsers { get; set; }
    public int TotalProjects { get; set; }
    public int TotalTasks { get; set; }
    public int ActiveTasksToday { get; set; }
    
    // Storage
    public StorageStatsDto StorageStats { get; set; } = null!;
    
    // System Health
    public SystemHealthDto SystemHealth { get; set; } = null!;
    
    // Recent Users
    public List<UserDto> RecentUsers { get; set; } = new();
    
    // Recent Projects
    public List<ProjectSummaryDto> RecentProjects { get; set; } = new();
    
    // Pending Approvals
    public int PendingTimeEntryApprovals { get; set; }
    public int PendingUserApprovals { get; set; }
}

// Helper DTOs

public class TaskSummaryDto
{
    public int Total { get; set; }
    public int Todo { get; set; }
    public int InProgress { get; set; }
    public int Review { get; set; }
    public int Done { get; set; }
    public int Overdue { get; set; }
}

public class TaskItemDto
{
    public long TaskId { get; set; }
    public string TaskTitle { get; set; } = null!;
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = null!;
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public List<LabelInfoDto> Labels { get; set; } = new();
}

public class TodayTimeDto
{
    public int TotalMinutes { get; set; }
    public string FormattedTime { get; set; } = null!;
    public bool IsTimerRunning { get; set; }
    public RunningTimerDto? CurrentTimer { get; set; }
}

public class RunningTimerDto
{
    public long TaskId { get; set; }
    public string TaskTitle { get; set; } = null!;
    public int CurrentDurationMinutes { get; set; }
}

public class NotificationDto
{
    public long NotificationId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProjectSummaryDto
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = null!;
    public ProjectStatus Status { get; set; }
    public int TaskCount { get; set; }
    public int CompletedTasks { get; set; }
    public double ProgressPercent { get; set; }
}

public class TaskStatisticsDto
{
    public int Total { get; set; }
    public Dictionary<TaskStatus, int> ByStatus { get; set; } = new();
    public Dictionary<TaskPriority, int> ByPriority { get; set; } = new();
    public int Overdue { get; set; }
    public int DueThisWeek { get; set; }
}

public class TeamMemberDto
{
    public long UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string? Avatar { get; set; }
    public string Role { get; set; } = null!;
    public int AssignedTasks { get; set; }
    public int CompletedTasks { get; set; }
}

public class ActivityDto
{
    public string ActivityType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}

public class MilestoneDto
{
    public long TaskId { get; set; }
    public string Title { get; set; } = null!;
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
}

public class BurndownDataPointDto
{
    public DateTime Date { get; set; }
    public int RemainingTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int IdealRemaining { get; set; }
}

public class StorageStatsDto
{
    public long TotalBytes { get; set; }
    public long UsedBytes { get; set; }
    public long FreeBytes { get; set; }
    public double UsedPercent { get; set; }
}

public class SystemHealthDto
{
    public string Status { get; set; } = null!; // Healthy, Warning, Critical
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public int ActiveConnections { get; set; }
    public double AvgResponseTime { get; set; }
}

public class UserDto
{
    public long UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

// ============== HANDLERS ==============

public class GetUserDashboardHandler : IRequestHandler<GetUserDashboardQuery, UserDashboardDto>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITimeEntryRepository _timeEntryRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IProjectRepository _projectRepository;

    public GetUserDashboardHandler(
        ITaskRepository taskRepository,
        ITimeEntryRepository timeEntryRepository,
        INotificationRepository notificationRepository,
        IProjectRepository projectRepository)
    {
        _taskRepository = taskRepository;
        _timeEntryRepository = timeEntryRepository;
        _notificationRepository = notificationRepository;
        _projectRepository = projectRepository;
    }

    public async Task<UserDashboardDto> Handle(GetUserDashboardQuery request, CancellationToken cancellationToken)
    {
        // Get tasks assigned to user
        var myTasks = await _taskRepository.GetByAssigneeAsync(request.UserId.ToString(), cancellationToken);
        
        // Calculate summary
        var taskSummary = new TaskSummaryDto
        {
            Total = myTasks.Count,
            Todo = myTasks.Count(t => t.Status == TaskStatus.Todo),
            InProgress = myTasks.Count(t => t.Status == TaskStatus.InProgress),
            Review = myTasks.Count(t => t.Status == TaskStatus.Review),
            Done = myTasks.Count(t => t.Status == TaskStatus.Done),
            Overdue = myTasks.Count(t => t.DueDate < DateTime.Today && t.Status != TaskStatus.Done)
        };

        // Today's tasks
        var todaysTasks = myTasks
            .Where(t => t.DueDate?.Date == DateTime.Today)
            .Select(MapToTaskItemDto)
            .ToList();

        // Overdue tasks
        var overdueTasks = myTasks
            .Where(t => t.DueDate < DateTime.Today && t.Status != TaskStatus.Done)
            .OrderBy(t => t.DueDate)
            .Take(5)
            .Select(MapToTaskItemDto)
            .ToList();

        // Upcoming deadlines (next 7 days)
        var upcomingDeadlines = myTasks
            .Where(t => t.DueDate >= DateTime.Today && t.DueDate <= DateTime.Today.AddDays(7) && t.Status != TaskStatus.Done)
            .OrderBy(t => t.DueDate)
            .Take(5)
            .Select(MapToTaskItemDto)
            .ToList();

        // Time tracking today
        var todaySummary = await _timeEntryRepository.GetDailySummaryAsync(request.UserId, DateTime.Today, cancellationToken);
        var runningTimer = await _timeEntryRepository.GetRunningTimerAsync(request.UserId, cancellationToken);

        // Notifications
        var notifications = await _notificationRepository.GetUnreadByUserAsync(request.UserId, 5, cancellationToken);

        // My projects
        var projects = await _projectRepository.GetByUserAsync(request.UserId, cancellationToken);

        return new UserDashboardDto
        {
            UserId = request.UserId,
            UserName = "User", // TODO: Get from user repository
            MyTasks = taskSummary,
            TodaysTasks = todaysTasks,
            OverdueTasks = overdueTasks,
            UpcomingDeadlines = upcomingDeadlines,
            TodayTime = new TodayTimeDto
            {
                TotalMinutes = todaySummary?.TotalMinutes ?? 0,
                FormattedTime = FormatMinutes(todaySummary?.TotalMinutes ?? 0),
                IsTimerRunning = runningTimer != null,
                CurrentTimer = runningTimer != null ? new RunningTimerDto
                {
                    TaskId = runningTimer.TaskId,
                    TaskTitle = runningTimer.Task?.TaskTitle ?? "Unknown",
                    CurrentDurationMinutes = (int)(DateTime.UtcNow - runningTimer.StartTime).TotalMinutes
                } : null
            },
            UnreadNotificationsCount = await _notificationRepository.GetUnreadCountAsync(request.UserId, cancellationToken),
            RecentNotifications = notifications.Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList(),
            MyProjects = projects.Select(p => new ProjectSummaryDto
            {
                ProjectId = p.ProjectId,
                ProjectName = p.ProjectName,
                Status = p.Status,
                TaskCount = 0, // TODO: Get task count
                CompletedTasks = 0,
                ProgressPercent = 0
            }).ToList()
        };
    }

    private static TaskItemDto MapToTaskItemDto(TaskItem task)
    {
        return new TaskItemDto
        {
            TaskId = task.TaskId,
            TaskTitle = task.TaskTitle,
            ProjectId = task.ProjectId,
            ProjectName = "Project", // TODO
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            IsOverdue = task.DueDate < DateTime.Today && task.Status != TaskStatus.Done,
            Labels = new List<LabelInfoDto>()
        };
    }

    private static string FormatMinutes(int minutes)
    {
        var hours = minutes / 60;
        var mins = minutes % 60;
        return hours > 0 ? $"{hours}h {mins}m" : $"{mins}m";
    }
}

public class GetProjectDashboardHandler : IRequestHandler<GetProjectDashboardQuery, ProjectDashboardDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskRepository _taskRepository;

    public GetProjectDashboardHandler(
        IProjectRepository projectRepository,
        ITaskRepository taskRepository)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
    }

    public async Task<ProjectDashboardDto> Handle(GetProjectDashboardQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            throw new KeyNotFoundException($"Layihə tapılmadı: {request.ProjectId}");

        var tasks = await _taskRepository.GetByProjectAsync(request.ProjectId, cancellationToken);

        var completedTasks = tasks.Count(t => t.Status == TaskStatus.Done);
        var totalTasks = tasks.Count;
        var progress = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0;

        return new ProjectDashboardDto
        {
            ProjectId = project.ProjectId,
            ProjectName = project.ProjectName,
            ProjectDescription = project.Description,
            Status = project.Status,
            TaskStats = new TaskStatisticsDto
            {
                Total = totalTasks,
                ByStatus = tasks.GroupBy(t => t.Status).ToDictionary(g => g.Key, g => g.Count()),
                ByPriority = tasks.GroupBy(t => t.Priority).ToDictionary(g => g.Key, g => g.Count()),
                Overdue = tasks.Count(t => t.DueDate < DateTime.Today && t.Status != TaskStatus.Done),
                DueThisWeek = tasks.Count(t => t.DueDate >= DateTime.Today && t.DueDate <= DateTime.Today.AddDays(7))
            },
            OverallProgress = progress,
            CompletedTasks = completedTasks,
            TotalTasks = totalTasks,
            TeamMembers = new List<TeamMemberDto>(), // TODO
            RecentActivity = new List<ActivityDto>(), // TODO
            UpcomingMilestones = new List<MilestoneDto>(), // TODO
            BurndownData = new List<BurndownDataPointDto>() // TODO
        };
    }
}
