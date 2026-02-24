using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Data;

namespace Nexus.Infrastructure.Repositories;

public class TaskDependencyRepository : ITaskDependencyRepository
{
    private readonly AppDbContext _context;

    public TaskDependencyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TaskDependency?> GetByIdAsync(long dependencyId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskDependencies
            .Include(d => d.Task)
            .Include(d => d.DependsOnTask)
            .FirstOrDefaultAsync(d => d.DependencyId == dependencyId, cancellationToken);
    }

    public async Task<TaskDependency?> GetByTaskIdsAsync(long taskId, long dependsOnTaskId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskDependencies
            .FirstOrDefaultAsync(
                d => d.TaskId == taskId && d.DependsOnTaskId == dependsOnTaskId, 
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(long taskId, long dependsOnTaskId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskDependencies
            .AnyAsync(
                d => d.TaskId == taskId && d.DependsOnTaskId == dependsOnTaskId, 
                cancellationToken);
    }

    public async Task AddAsync(TaskDependency dependency, CancellationToken cancellationToken = default)
    {
        await _context.TaskDependencies.AddAsync(dependency, cancellationToken);
    }

    public Task DeleteAsync(TaskDependency dependency, CancellationToken cancellationToken = default)
    {
        _context.TaskDependencies.Remove(dependency);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<TaskDependencyInfo>> GetDependenciesAsync(long taskId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskDependencies
            .Where(d => d.TaskId == taskId)
            .Select(d => new TaskDependencyInfo
            {
                DependencyId = d.DependencyId,
                TaskId = d.TaskId,
                TaskTitle = d.Task.TaskTitle,
                TaskStatus = d.Task.Status,
                DependsOnTaskId = d.DependsOnTaskId,
                DependsOnTaskTitle = d.DependsOnTask.TaskTitle,
                DependsOnTaskStatus = d.DependsOnTask.Status,
                Type = d.Type,
                LagDays = d.LagDays
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskDependencyInfo>> GetDependentsAsync(long taskId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskDependencies
            .Where(d => d.DependsOnTaskId == taskId)
            .Select(d => new TaskDependencyInfo
            {
                DependencyId = d.DependencyId,
                TaskId = d.TaskId,
                TaskTitle = d.Task.TaskTitle,
                TaskStatus = d.Task.Status,
                DependsOnTaskId = d.DependsOnTaskId,
                DependsOnTaskTitle = d.DependsOnTask.TaskTitle,
                DependsOnTaskStatus = d.DependsOnTask.Status,
                Type = d.Type,
                LagDays = d.LagDays
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Dairəvi asılılıq yoxlanışı - DFS (Depth-First Search) algorithm
    /// Əgər A → B → C → A varsa, bu dairəvi asılılıq yaradır
    /// </summary>
    public async Task<bool> WouldCreateCycleAsync(long taskId, long dependsOnTaskId, CancellationToken cancellationToken = default)
    {
        // Əgər dependsOnTaskId artıq taskId-dən asılıdırsa, dairəvi asılılıq yaradacaq
        // A → B əlavə etmək istəyirik, amma B → A artıq mövcuddursa - CYCLE!
        
        var visited = new HashSet<long>();
        var recursionStack = new HashSet<long>();
        
        // dependsOnTaskId-dən başlayaraq baxırıq, taskId-ə çatırıqmı
        return await HasPathAsync(dependsOnTaskId, taskId, visited, recursionStack, cancellationToken);
    }

    private async Task<bool> HasPathAsync(
        long currentTaskId, 
        long targetTaskId,
        HashSet<long> visited,
        HashSet<long> recursionStack,
        CancellationToken cancellationToken)
    {
        if (currentTaskId == targetTaskId)
            return true; // Yol tapıldı = dairəvi asılılıq!

        visited.Add(currentTaskId);
        recursionStack.Add(currentTaskId);

        // Cari tapşırığın bütün asılılıqlarını tap
        var dependencies = await _context.TaskDependencies
            .Where(d => d.TaskId == currentTaskId)
            .Select(d => d.DependsOnTaskId)
            .ToListAsync(cancellationToken);

        foreach (var nextTaskId in dependencies)
        {
            if (!visited.Contains(nextTaskId))
            {
                if (await HasPathAsync(nextTaskId, targetTaskId, visited, recursionStack, cancellationToken))
                    return true;
            }
            else if (recursionStack.Contains(nextTaskId))
            {
                // Dairəvi asılılıq tapıldı
                return true;
            }
        }

        recursionStack.Remove(currentTaskId);
        return false;
    }

    public async Task<bool> IsBlockedAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var dependencies = await _context.TaskDependencies
            .Where(d => d.TaskId == taskId && d.Type == DependencyType.FinishToStart)
            .Include(d => d.DependsOnTask)
            .ToListAsync(cancellationToken);

        // Əgər hər hansı bir FS asılılıq tamamlanmayıbsa, bloklanıb
        return dependencies.Any(d => d.DependsOnTask.Status != TaskStatus.Done);
    }

    public async Task<long?> GetTaskProjectIdAsync(long taskId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItems
            .Where(t => t.TaskId == taskId)
            .Select(t => (long?)t.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TaskStatus> GetTaskStatusAsync(long taskId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItems
            .Where(t => t.TaskId == taskId)
            .Select(t => t.Status)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> CanStartAsync(long taskId, CancellationToken cancellationToken = default)
    {
        // FinishToStart (FS) asılılıqlar: bütün əvvəlki tapşırıqlar tamamlanmalıdır
        var fsDependencies = await _context.TaskDependencies
            .Where(d => d.TaskId == taskId && d.Type == DependencyType.FinishToStart)
            .Include(d => d.DependsOnTask)
            .ToListAsync(cancellationToken);

        if (fsDependencies.Any(d => d.DependsOnTask.Status != TaskStatus.Done))
            return false;

        // StartToStart (SS) asılılıqlar: əvvəlki tapşırıqlar başlamalıdır
        var ssDependencies = await _context.TaskDependencies
            .Where(d => d.TaskId == taskId && d.Type == DependencyType.StartToStart)
            .Include(d => d.DependsOnTask)
            .ToListAsync(cancellationToken);

        if (ssDependencies.Any(d => d.DependsOnTask.Status == TaskStatus.Todo))
            return false;

        return true;
    }

    public async Task<bool> CanCompleteAsync(long taskId, CancellationToken cancellationToken = default)
    {
        // FinishToFinish (FF) asılılıqlar: hər ikisi eyni vaxtda bitməlidir
        var ffDependencies = await _context.TaskDependencies
            .Where(d => d.TaskId == taskId && d.Type == DependencyType.FinishToFinish)
            .Include(d => d.DependsOnTask)
            .ToListAsync(cancellationToken);

        // Əgər FF asılılıq varsa və o tamamlanmayıbsa, bu da tamamlana bilməz
        // (Yəni eyni vaxtda tamamlanmalılar)
        
        // StartToFinish (SF) çox nadir istifadə olunur, ona görə burada sadəcə yoxlanılır
        var sfDependencies = await _context.TaskDependencies
            .Where(d => d.TaskId == taskId && d.Type == DependencyType.StartToFinish)
            .Include(d => d.DependsOnTask)
            .ToListAsync(cancellationToken);

        if (sfDependencies.Any(d => d.DependsOnTask.Status == TaskStatus.Todo))
            return false;

        return true;
    }
}
