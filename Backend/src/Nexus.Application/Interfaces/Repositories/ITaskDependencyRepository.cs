using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repositories;

/// <summary>
/// Task Dependency repository interface
/// </summary>
public interface ITaskDependencyRepository
{
    Task<TaskDependency?> GetByIdAsync(long dependencyId, CancellationToken cancellationToken = default);
    Task<TaskDependency?> GetByTaskIdsAsync(long taskId, long dependsOnTaskId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(long taskId, long dependsOnTaskId, CancellationToken cancellationToken = default);
    Task AddAsync(TaskDependency dependency, CancellationToken cancellationToken = default);
    Task DeleteAsync(TaskDependency dependency, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Bir tapşırığın bütün asılılıqlarını gətir (bu tapşırıq kimdən asılıdır)
    /// </summary>
    Task<IReadOnlyList<TaskDependencyInfo>> GetDependenciesAsync(long taskId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Bir tapşırığa asılı olan bütün tapşırıqları gətir (kimlər bu tapşırıqdan asılıdır)
    /// </summary>
    Task<IReadOnlyList<TaskDependencyInfo>> GetDependentsAsync(long taskId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dairəvi asılılıq yaranacaqmı yoxlayır (DFS algorithm)
    /// Əgər Task A → B → C → A olarsa, bu dairəvi asılılıq yaradır
    /// </summary>
    Task<bool> WouldCreateCycleAsync(long taskId, long dependsOnTaskId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tapşırıq bloklanıb yoxsa yox (asılı olduğu tapşırıqlar tamamlanıbmı)
    /// </summary>
    Task<bool> IsBlockedAsync(long taskId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tapşırığın layihə ID-sini gətir
    /// </summary>
    Task<long?> GetTaskProjectIdAsync(long taskId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tapşırığın statusunu gətir
    /// </summary>
    Task<TaskStatus> GetTaskStatusAsync(long taskId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Bir tapşırıq başlaya bilərmi (bütün FS asılılıqlar tamamlanıbmı)
    /// </summary>
    Task<bool> CanStartAsync(long taskId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Bir tapşırıq bitə bilərmi (bütün FF asılılıqlar tamamlanıbmı)
    /// </summary>
    Task<bool> CanCompleteAsync(long taskId, CancellationToken cancellationToken = default);
}
