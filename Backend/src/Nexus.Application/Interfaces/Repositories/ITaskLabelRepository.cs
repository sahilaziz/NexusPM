using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repositories;

/// <summary>
/// Task Label repository interface
/// </summary>
public interface ITaskLabelRepository
{
    // Label CRUD
    Task<TaskLabel?> GetByIdAsync(long labelId, CancellationToken cancellationToken = default);
    Task<TaskLabel?> GetByNameAsync(string name, long? projectId, string organizationCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskLabel>> GetByProjectAsync(long? projectId, string organizationCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskLabel>> GetActiveLabelsAsync(long? projectId, string organizationCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskLabel>> GetSystemLabelsAsync(CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(string name, long? projectId, string organizationCode, CancellationToken cancellationToken = default);
    Task AddAsync(TaskLabel label, CancellationToken cancellationToken = default);
    Task UpdateAsync(TaskLabel label, CancellationToken cancellationToken = default);
    Task DeleteAsync(TaskLabel label, CancellationToken cancellationToken = default);
    
    // Task-Label operations
    Task AssignLabelToTaskAsync(long taskId, long labelId, string assignedBy, CancellationToken cancellationToken = default);
    Task RemoveLabelFromTaskAsync(long taskId, long labelId, CancellationToken cancellationToken = default);
    Task RemoveAllLabelsFromTaskAsync(long taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabelDto>> GetTaskLabelsAsync(long taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskWithLabels>> GetTasksByLabelAsync(long labelId, CancellationToken cancellationToken = default);
    Task<bool> TaskHasLabelAsync(long taskId, long labelId, CancellationToken cancellationToken = default);
    
    // Bulk operations
    Task<int> GetTaskCountByLabelAsync(long labelId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(long LabelId, int Count)>> GetLabelStatisticsAsync(long? projectId, CancellationToken cancellationToken = default);
}
