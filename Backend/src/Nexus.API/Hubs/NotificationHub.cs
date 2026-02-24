using Microsoft.AspNetCore.SignalR;
using Nexus.Domain.Entities;
using System.Collections.Concurrent;

namespace Nexus.API.Hubs;

/// <summary>
/// Real-time notification hub for task assignments, status changes, and alerts
/// 5000+ concurrent users support with Redis backplane
/// </summary>
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;
    
    // Online istifadəçiləri yadda saxla (UserId -> ConnectionId-lər)
    private static readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();
    private static readonly ConcurrentDictionary<string, string> _connectionUsers = new();

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// İstifadəçi daxil olduqda öz qrupuna qoşulur
    /// </summary>
    public async Task JoinUserGroup(string userId, string organizationCode)
    {
        var connectionId = Context.ConnectionId;
        
        // Connection-User mapping
        _connectionUsers[connectionId] = userId;
        
        // User-Connections mapping (birdən çox cihaz ola bilər)
        _userConnections.AddOrUpdate(userId, 
            _ => new HashSet<string> { connectionId },
            (_, existing) => { existing.Add(connectionId); return existing; });

        // Şəxsi qrupa qoşul (bildirişlər üçün)
        await Groups.AddToGroupAsync(connectionId, $"user:{userId}");
        
        // Təşkilat qrupuna qoşul (ümumi bildirişlər üçün)
        await Groups.AddToGroupAsync(connectionId, $"org:{organizationCode}");

        _logger.LogInformation(
            "User {UserId} connected with connection {ConnectionId}. Total online: {OnlineCount}", 
            userId, connectionId, _userConnections.Count);

        // İstifadəçiyə oxunmamış bildirişlərin sayını göndər
        await Clients.Caller.SendAsync("Connected", new
        {
            UserId = userId,
            ConnectionId = connectionId,
            OnlineUsers = _userConnections.Count,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Bildirişi oxundu olaraq işarələ
    /// </summary>
    public async Task MarkAsRead(long notificationId)
    {
        await Clients.Caller.SendAsync("NotificationRead", notificationId);
        _logger.LogDebug("Notification {NotificationId} marked as read by {User}", 
            notificationId, Context.User?.Identity?.Name);
    }

    /// <summary>
    /// Birbaşa istifadəçiyə bildiriş göndər (API-dən çağrılır)
    /// </summary>
    public static async Task SendToUser(
        IHubContext<NotificationHub> hubContext,
        string userId,
        Notification notification)
    {
        var userGroup = $"user:{userId}";
        
        await hubContext.Clients
            .Group(userGroup)
            .SendAsync("NewNotification", new NotificationDto
            {
                NotificationId = notification.NotificationId,
                Type = notification.Type.ToString(),
                Title = notification.Title,
                Message = notification.Message,
                EntityType = notification.EntityType,
                EntityId = notification.EntityId,
                Metadata = notification.Metadata,
                SenderUserId = notification.SenderUserId,
                CreatedAt = notification.CreatedAt,
                Badge = 1 // Yeni bildiriş sayı
            });
    }

    /// <summary>
    /// Bütün online istifadəçilərə bildiriş göndər
    /// </summary>
    public static async Task BroadcastToOrganization(
        IHubContext<NotificationHub> hubContext,
        string organizationCode,
        Notification notification)
    {
        var orgGroup = $"org:{organizationCode}";
        
        await hubContext.Clients
            .Group(orgGroup)
            .SendAsync("BroadcastNotification", new NotificationDto
            {
                NotificationId = notification.NotificationId,
                Type = notification.Type.ToString(),
                Title = notification.Title,
                Message = notification.Message,
                EntityType = notification.EntityType,
                EntityId = notification.EntityId,
                SenderUserId = notification.SenderUserId,
                CreatedAt = notification.CreatedAt
            });
    }

    /// <summary>
    /// Tapşırıq təyinatı bildirişi göndər
    /// </summary>
    public static async Task SendTaskAssignedNotification(
        IHubContext<NotificationHub> hubContext,
        string assignedToUserId,
        TaskItem task,
        string assignedByUserName,
        string projectName)
    {
        var notification = new NotificationDto
        {
            Type = NotificationType.TaskAssigned.ToString(),
            Title = "Yeni tapşırıq təyin edildi",
            Message = $"'{task.TaskTitle}' tapşırığı {assignedByUserName} tərəfindən sizə təyin edildi",
            EntityType = "Task",
            EntityId = task.TaskId,
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                TaskTitle = task.TaskTitle,
                ProjectName = projectName,
                Priority = task.Priority.ToString(),
                DueDate = task.DueDate,
                AssignedBy = assignedByUserName
            }),
            SenderUserId = assignedByUserName,
            CreatedAt = DateTime.UtcNow,
            Badge = 1
        };

        await hubContext.Clients
            .Group($"user:{assignedToUserId}")
            .SendAsync("TaskAssigned", notification);

        // Ümumi task listini yenilə (hər kəs görsün)
        await hubContext.Clients
            .Group($"user:{assignedToUserId}")
            .SendAsync("RefreshTaskList", new { TaskId = task.TaskId, Action = "Added" });
    }

    /// <summary>
    /// Tapşırıq status dəyişikliyi bildirişi
    /// </summary>
    public static async Task SendTaskStatusChangedNotification(
        IHubContext<NotificationHub> hubContext,
        string assignedToUserId,
        string createdByUserId,
        TaskItem task,
        TaskStatus oldStatus,
        string changedByUserName)
    {
        // Tapşırıq sahibinə bildiriş
        if (!string.IsNullOrEmpty(assignedToUserId))
        {
            await hubContext.Clients
                .Group($"user:{assignedToUserId}")
                .SendAsync("TaskUpdated", new TaskUpdateDto
                {
                    TaskId = task.TaskId,
                    Status = task.Status.ToString(),
                    OldStatus = oldStatus.ToString(),
                    UpdatedBy = changedByUserName,
                    UpdatedAt = DateTime.UtcNow,
                    Message = $"'{task.TaskTitle}' statusu {oldStatus} -> {task.Status} olaraq dəyişdirildi"
                });
        }

        // Yaradıcısına da bildiriş
        if (!string.IsNullOrEmpty(createdByUserId) && createdByUserId != assignedToUserId)
        {
            await hubContext.Clients
                .Group($"user:{createdByUserId}")
                .SendAsync("TaskUpdated", new TaskUpdateDto
                {
                    TaskId = task.TaskId,
                    Status = task.Status.ToString(),
                    OldStatus = oldStatus.ToString(),
                    UpdatedBy = changedByUserName,
                    UpdatedAt = DateTime.UtcNow,
                    Message = $"'{task.TaskTitle}' statusu dəyişdirildi"
                });
        }
    }

    /// <summary>
    /// Sənəd yükləndikdə bildiriş
    /// </summary>
    public static async Task SendDocumentUploadedNotification(
        IHubContext<NotificationHub> hubContext,
        string organizationCode,
        DocumentNode document,
        string uploadedBy)
    {
        await hubContext.Clients
            .Group($"org:{organizationCode}")
            .SendAsync("DocumentUploaded", new DocumentNotificationDto
            {
                DocumentId = document.NodeId,
                DocumentNumber = document.DocumentNumber,
                DocumentSubject = document.EntityName,
                MaterializedPath = document.MaterializedPath,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Deadline xatırlatması
    /// </summary>
    public static async Task SendDeadlineReminder(
        IHubContext<NotificationHub> hubContext,
        string userId,
        TaskItem task,
        int hoursRemaining)
    {
        await hubContext.Clients
            .Group($"user:{userId}")
            .SendAsync("DeadlineReminder", new
            {
                TaskId = task.TaskId,
                TaskTitle = task.TaskTitle,
                HoursRemaining = hoursRemaining,
                DueDate = task.DueDate,
                Priority = task.Priority.ToString()
            });
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("NotificationHub client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        
        if (_connectionUsers.TryRemove(connectionId, out var userId))
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(connectionId);
                if (connections.Count == 0)
                {
                    _userConnections.TryRemove(userId, out _);
                }
            }
        }
        
        _logger.LogInformation(
            "NotificationHub client disconnected: {ConnectionId}. Total online: {OnlineCount}", 
            connectionId, _userConnections.Count);
            
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Online istifadəçiləri əldə et (admin üçün)
    /// </summary>
    public static IReadOnlyDictionary<string, HashSet<string>> GetOnlineUsers() => _userConnections;
}

// DTO-lar
public class NotificationDto
{
    public long NotificationId { get; set; }
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }
    public string? Metadata { get; set; }
    public string? SenderUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Badge { get; set; }
}

public class TaskUpdateDto
{
    public long TaskId { get; set; }
    public string Status { get; set; } = null!;
    public string OldStatus { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
    public DateTime UpdatedAt { get; set; }
    public string Message { get; set; } = null!;
}

public class DocumentNotificationDto
{
    public long DocumentId { get; set; }
    public string? DocumentNumber { get; set; }
    public string DocumentSubject { get; set; } = null!;
    public string MaterializedPath { get; set; } = null!;
    public string UploadedBy { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
}
