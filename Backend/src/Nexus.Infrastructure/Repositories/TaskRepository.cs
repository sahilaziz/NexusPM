using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Data;

namespace Nexus.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem?> GetByIdAsync(long taskId)
    {
        return await _context.TaskItems
            .Include(t => t.Project)
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.TaskId == taskId);
    }

    public async Task<IEnumerable<TaskItem>> GetTasksByProjectAsync(long projectId)
    {
        return await _context.TaskItems
            .Where(t => t.ProjectId == projectId && t.ParentTaskId == null)
            .Include(t => t.SubTasks)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetTasksByAssigneeAsync(string assigneeUserId)
    {
        return await _context.TaskItems
            .Where(t => t.AssignedTo == assigneeUserId)
            .Include(t => t.Project)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetPendingTasksByAssigneeAsync(string assigneeUserId)
    {
        return await _context.TaskItems
            .Where(t => t.AssignedTo == assigneeUserId && 
                       (t.Status == TaskStatus.Todo || t.Status == TaskStatus.InProgress))
            .Include(t => t.Project)
            .OrderBy(t => t.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetOverdueTasksAsync(string organizationCode)
    {
        return await _context.TaskItems
            .Where(t => t.DueDate < DateTime.UtcNow && 
                       t.Status != TaskStatus.Done && 
                       t.Status != TaskStatus.Cancelled)
            .Include(t => t.Project)
            .ToListAsync();
    }

    public async Task AddAsync(TaskItem task)
    {
        await _context.TaskItems.AddAsync(task);
    }

    public Task UpdateAsync(TaskItem task)
    {
        _context.TaskItems.Update(task);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TaskItem task)
    {
        _context.TaskItems.Remove(task);
        return Task.CompletedTask;
    }

    public async Task AddCommentAsync(TaskComment comment)
    {
        await _context.TaskComments.AddAsync(comment);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByIdAsync(long projectId)
    {
        return await _context.Projects.FindAsync(projectId);
    }

    public async Task<IEnumerable<Project>> GetByOrganizationAsync(string organizationCode)
    {
        return await _context.Projects
            .Where(p => p.OrganizationCode == organizationCode)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();
    }

    public async Task AddAsync(Project project)
    {
        await _context.Projects.AddAsync(project);
    }

    public Task UpdateAsync(Project project)
    {
        _context.Projects.Update(project);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> GetByIdAsync(long notificationId)
    {
        return await _context.Notifications.FindAsync(notificationId);
    }

    public async Task<IEnumerable<Notification>> GetUnreadByUserAsync(string userId)
    {
        return await _context.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Notification>> GetRecentByUserAsync(string userId, int count = 50)
    {
        return await _context.Notifications
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.RecipientUserId == userId && !n.IsRead);
    }

    public async Task AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
    }

    public async Task MarkAsReadAsync(long notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
