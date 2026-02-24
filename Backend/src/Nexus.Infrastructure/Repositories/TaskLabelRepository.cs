using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Data;

namespace Nexus.Infrastructure.Repositories;

public class TaskLabelRepository : ITaskLabelRepository
{
    private readonly AppDbContext _context;

    public TaskLabelRepository(AppDbContext context)
    {
        _context = context;
    }

    #region Label CRUD

    public async Task<TaskLabel?> GetByIdAsync(long labelId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskLabels
            .Include(l => l.Project)
            .FirstOrDefaultAsync(l => l.LabelId == labelId, cancellationToken);
    }

    public async Task<TaskLabel?> GetByNameAsync(
        string name, 
        long? projectId, 
        string organizationCode, 
        CancellationToken cancellationToken = default)
    {
        return await _context.TaskLabels
            .FirstOrDefaultAsync(
                l => l.Name.ToLower() == name.ToLower() 
                    && l.ProjectId == projectId 
                    && l.OrganizationCode == organizationCode,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TaskLabel>> GetByProjectAsync(
        long? projectId, 
        string organizationCode, 
        CancellationToken cancellationToken = default)
    {
        return await _context.TaskLabels
            .Where(l => (l.ProjectId == projectId || l.ProjectId == null) 
                     && l.OrganizationCode == organizationCode)
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskLabel>> GetActiveLabelsAsync(
        long? projectId, 
        string organizationCode, 
        CancellationToken cancellationToken = default)
    {
        return await _context.TaskLabels
            .Where(l => (l.ProjectId == projectId || l.ProjectId == null) 
                     && l.OrganizationCode == organizationCode
                     && l.IsActive)
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskLabel>> GetSystemLabelsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TaskLabels
            .Where(l => l.IsSystem && l.IsActive)
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string name, 
        long? projectId, 
        string organizationCode, 
        CancellationToken cancellationToken = default)
    {
        return await _context.TaskLabels
            .AnyAsync(
                l => l.Name.ToLower() == name.ToLower() 
                    && l.ProjectId == projectId 
                    && l.OrganizationCode == organizationCode,
                cancellationToken);
    }

    public async Task AddAsync(TaskLabel label, CancellationToken cancellationToken = default)
    {
        await _context.TaskLabels.AddAsync(label, cancellationToken);
    }

    public Task UpdateAsync(TaskLabel label, CancellationToken cancellationToken = default)
    {
        _context.TaskLabels.Update(label);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TaskLabel label, CancellationToken cancellationToken = default)
    {
        _context.TaskLabels.Remove(label);
        return Task.CompletedTask;
    }

    #endregion

    #region Task-Label Operations

    public async Task AssignLabelToTaskAsync(
        long taskId, 
        long labelId, 
        string assignedBy, 
        CancellationToken cancellationToken = default)
    {
        var taskLabel = new TaskItemLabel
        {
            TaskId = taskId,
            LabelId = labelId,
            AssignedBy = assignedBy,
            AssignedAt = DateTime.UtcNow
        };

        await _context.TaskItemLabels.AddAsync(taskLabel, cancellationToken);
    }

    public async Task RemoveLabelFromTaskAsync(long taskId, long labelId, CancellationToken cancellationToken = default)
    {
        var taskLabel = await _context.TaskItemLabels
            .FirstOrDefaultAsync(tl => tl.TaskId == taskId && tl.LabelId == labelId, cancellationToken);

        if (taskLabel != null)
        {
            _context.TaskItemLabels.Remove(taskLabel);
        }
    }

    public async Task RemoveAllLabelsFromTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var taskLabels = await _context.TaskItemLabels
            .Where(tl => tl.TaskId == taskId)
            .ToListAsync(cancellationToken);

        _context.TaskItemLabels.RemoveRange(taskLabels);
    }

    public async Task<IReadOnlyList<LabelDto>> GetTaskLabelsAsync(long taskId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItemLabels
            .Where(tl => tl.TaskId == taskId)
            .Include(tl => tl.Label)
            .OrderBy(tl => tl.Label.SortOrder)
            .ThenBy(tl => tl.Label.Name)
            .Select(tl => new LabelDto
            {
                LabelId = tl.Label.LabelId,
                Name = tl.Label.Name,
                Color = tl.Label.Color
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskWithLabels>> GetTasksByLabelAsync(long labelId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItemLabels
            .Where(tl => tl.LabelId == labelId)
            .Include(tl => tl.Task)
            .Select(tl => new TaskWithLabels
            {
                TaskId = tl.Task.TaskId,
                TaskTitle = tl.Task.TaskTitle,
                Status = tl.Task.Status,
                Priority = tl.Task.Priority,
                Labels = _context.TaskItemLabels
                    .Where(tl2 => tl2.TaskId == tl.TaskId)
                    .Select(tl2 => new LabelDto
                    {
                        LabelId = tl2.Label.LabelId,
                        Name = tl2.Label.Name,
                        Color = tl2.Label.Color
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TaskHasLabelAsync(long taskId, long labelId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItemLabels
            .AnyAsync(tl => tl.TaskId == taskId && tl.LabelId == labelId, cancellationToken);
    }

    #endregion

    #region Statistics

    public async Task<int> GetTaskCountByLabelAsync(long labelId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItemLabels
            .CountAsync(tl => tl.LabelId == labelId, cancellationToken);
    }

    public async Task<IReadOnlyList<(long LabelId, int Count)>> GetLabelStatisticsAsync(
        long? projectId, 
        CancellationToken cancellationToken = default)
    {
        var stats = await _context.TaskItemLabels
            .Where(tl => projectId == null || tl.Label.ProjectId == projectId)
            .GroupBy(tl => tl.LabelId)
            .Select(g => new { LabelId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return stats.Select(s => (s.LabelId, s.Count)).ToList();
    }

    #endregion
}
