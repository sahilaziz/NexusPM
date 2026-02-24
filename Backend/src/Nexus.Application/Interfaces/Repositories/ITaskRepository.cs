using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repositories;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(long taskId);
    Task<IEnumerable<TaskItem>> GetTasksByProjectAsync(long projectId);
    Task<IEnumerable<TaskItem>> GetTasksByAssigneeAsync(string assigneeUserId);
    Task<IEnumerable<TaskItem>> GetPendingTasksByAssigneeAsync(string assigneeUserId);
    Task<IEnumerable<TaskItem>> GetOverdueTasksAsync(string organizationCode);
    Task AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(TaskItem task);
    Task AddCommentAsync(TaskComment comment);
    Task SaveChangesAsync();
}

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(long projectId);
    Task<IEnumerable<Project>> GetByOrganizationAsync(string organizationCode);
    Task AddAsync(Project project);
    Task UpdateAsync(Project project);
    Task SaveChangesAsync();
}

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(long notificationId);
    Task<IEnumerable<Notification>> GetUnreadByUserAsync(string userId);
    Task<IEnumerable<Notification>> GetRecentByUserAsync(string userId, int count = 50);
    Task<int> GetUnreadCountAsync(string userId);
    Task AddAsync(Notification notification);
    Task MarkAsReadAsync(long notificationId);
    Task MarkAllAsReadAsync(string userId);
    Task DeleteAsync(long notificationId);
    Task SaveChangesAsync();
}
