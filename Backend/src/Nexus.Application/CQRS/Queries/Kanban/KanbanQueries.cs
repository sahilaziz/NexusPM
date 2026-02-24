using MediatR;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;
using TaskStatus = Nexus.Domain.Entities.TaskStatus;

namespace Nexus.Application.CQRS.Queries.Kanban;

// ============== QUERIES ==============

/// <summary>
/// Layihənin Kanban board məlumatlarını gətir
/// </summary>
public record GetProjectKanbanQuery(
    long ProjectId,
    long? AssignedToUserId = null
) : IRequest<KanbanBoardDto>;

/// <summary>
/// Kanban sütunlarında tapşırıqları gətir
/// </summary>
public record GetKanbanTasksQuery(
    long ProjectId,
    TaskStatus? Status = null,
    long? AssignedToUserId = null
) : IRequest<List<KanbanTaskDto>>;

// ============== DTOS ==============

/// <summary>
/// Kanban Board
/// </summary>
public class KanbanBoardDto
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = null!;
    public List<KanbanColumnDto> Columns { get; set; } = new();
    public int TotalTasks { get; set; }
    public int WipLimit { get; set; } = 10; // Work in progress limit
    public bool HasWipLimit { get; set; } = false;
}

/// <summary>
/// Kanban Sütunu
/// </summary>
public class KanbanColumnDto
{
    public string Id { get; set; } = null!; // Status name
    public string Title { get; set; } = null!;
    public TaskStatus Status { get; set; }
    public string Color { get; set; } = null!;
    public List<KanbanTaskDto> Tasks { get; set; } = new();
    public int TaskCount => Tasks.Count;
    public int? WipLimit { get; set; }
    public bool IsOverLimit => WipLimit.HasValue && Tasks.Count > WipLimit.Value;
}

/// <summary>
/// Kanban Tapşırığı
/// </summary>
public class KanbanTaskDto
{
    public long TaskId { get; set; }
    public string TaskTitle { get; set; } = null!;
    public string? TaskDescription { get; set; }
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    
    // Assignee
    public long? AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }
    public string? AssignedToAvatar { get; set; }
    public string? AssignedToInitials { get; set; }
    
    // Dates
    public DateTime? DueDate { get; set; }
    public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.Today && Status != TaskStatus.Done;
    public bool IsDueToday => DueDate.HasValue && DueDate.Value.Date == DateTime.Today;
    
    // Indicators
    public int CommentsCount { get; set; }
    public int AttachmentsCount { get; set; }
    public int SubTasksCount { get; set; }
    public int CompletedSubTasksCount { get; set; }
    public bool HasDependencies { get; set; }
    public bool IsBlocked { get; set; }
    
    // Time tracking
    public int? TrackedTimeMinutes { get; set; }
    public int? EstimatedTimeMinutes { get; set; }
    
    // Labels
    public List<KanbanLabelDto> Labels { get; set; } = new();
    
    // Order in column
    public int SortOrder { get; set; }
}

public class KanbanLabelDto
{
    public long LabelId { get; set; }
    public string Name { get; set; } = null!;
    public string Color { get; set; } = null!;
}

// ============== HANDLERS ==============

public class GetProjectKanbanHandler : IRequestHandler<GetProjectKanbanQuery, KanbanBoardDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskLabelRepository _labelRepository;
    private readonly ITimeEntryRepository _timeEntryRepository;
    private readonly ITaskDependencyRepository _dependencyRepository;

    public GetProjectKanbanHandler(
        IProjectRepository projectRepository,
        ITaskRepository taskRepository,
        ITaskLabelRepository labelRepository,
        ITimeEntryRepository timeEntryRepository,
        ITaskDependencyRepository dependencyRepository)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _labelRepository = labelRepository;
        _timeEntryRepository = timeEntryRepository;
        _dependencyRepository = dependencyRepository;
    }

    public async Task<KanbanBoardDto> Handle(GetProjectKanbanQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            throw new KeyNotFoundException($"Layihə tapılmadı: {request.ProjectId}");

        // Get all tasks for the project
        var tasks = await _taskRepository.GetByProjectAsync(request.ProjectId, cancellationToken);
        
        // Filter by assignee if specified
        if (request.AssignedToUserId.HasValue)
        {
            tasks = tasks.Where(t => t.AssignedTo == request.AssignedToUserId.Value.ToString()).ToList();
        }

        // Define columns based on TaskStatus
        var columns = new List<KanbanColumnDto>
        {
            new() { Id = "todo", Title = "To Do", Status = TaskStatus.Todo, Color = "#6B7280", WipLimit = null },
            new() { Id = "inprogress", Title = "In Progress", Status = TaskStatus.InProgress, Color = "#3B82F6", WipLimit = 5 },
            new() { Id = "review", Title = "Review", Status = TaskStatus.Review, Color = "#F59E0B", WipLimit = 3 },
            new() { Id = "done", Title = "Done", Status = TaskStatus.Done, Color = "#10B981", WipLimit = null }
        };

        // Populate tasks in each column
        foreach (var column in columns)
        {
            var columnTasks = tasks.Where(t => t.Status == column.Status).ToList();
            column.Tasks = await BuildKanbanTasksAsync(columnTasks, cancellationToken);
        }

        return new KanbanBoardDto
        {
            ProjectId = project.ProjectId,
            ProjectName = project.ProjectName,
            Columns = columns,
            TotalTasks = tasks.Count,
            WipLimit = 5,
            HasWipLimit = true
        };
    }

    private async Task<List<KanbanTaskDto>> BuildKanbanTasksAsync(
        List<TaskItem> tasks, 
        CancellationToken cancellationToken)
    {
        var result = new List<KanbanTaskDto>();

        foreach (var task in tasks.OrderByDescending(t => t.Priority).ThenBy(t => t.CreatedAt))
        {
            // Get labels
            var labels = await _labelRepository.GetTaskLabelsAsync(task.TaskId, cancellationToken);
            
            // Get tracked time
            var trackedMinutes = await _timeEntryRepository.GetTotalMinutesByTaskAsync(task.TaskId, cancellationToken);
            
            // Check if blocked
            var isBlocked = await _dependencyRepository.IsBlockedAsync(task.TaskId, cancellationToken);
            
            // Get dependency info
            var hasDependencies = (await _dependencyRepository.GetDependenciesAsync(task.TaskId, cancellationToken)).Any();

            result.Add(new KanbanTaskDto
            {
                TaskId = task.TaskId,
                TaskTitle = task.TaskTitle,
                TaskDescription = task.TaskDescription?.Length > 100 
                    ? task.TaskDescription.Substring(0, 100) + "..." 
                    : task.TaskDescription,
                Status = task.Status,
                Priority = task.Priority,
                AssignedToUserId = task.AssignedTo != null ? long.Parse(task.AssignedTo) : null,
                AssignedToName = task.AssignedTo,
                AssignedToInitials = GetInitials(task.AssignedTo),
                DueDate = task.DueDate,
                CommentsCount = task.Comments?.Count ?? 0,
                AttachmentsCount = task.Attachments?.Count ?? 0,
                SubTasksCount = task.SubTasks?.Count ?? 0,
                CompletedSubTasksCount = task.SubTasks?.Count(s => s.Status == TaskStatus.Done) ?? 0,
                HasDependencies = hasDependencies,
                IsBlocked = isBlocked,
                TrackedTimeMinutes = trackedMinutes,
                Labels = labels.Select(l => new KanbanLabelDto
                {
                    LabelId = l.LabelId,
                    Name = l.Name,
                    Color = l.Color
                }).ToList(),
                SortOrder = (int)task.Priority * -1 // Higher priority first
            });
        }

        return result;
    }

    private static string? GetInitials(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
        
        return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
    }
}

public class GetKanbanTasksHandler : IRequestHandler<GetKanbanTasksQuery, List<KanbanTaskDto>>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskLabelRepository _labelRepository;
    private readonly ITaskDependencyRepository _dependencyRepository;

    public GetKanbanTasksHandler(
        ITaskRepository taskRepository,
        ITaskLabelRepository labelRepository,
        ITaskDependencyRepository dependencyRepository)
    {
        _taskRepository = taskRepository;
        _labelRepository = labelRepository;
        _dependencyRepository = dependencyRepository;
    }

    public async Task<List<KanbanTaskDto>> Handle(GetKanbanTasksQuery request, CancellationToken cancellationToken)
    {
        var tasks = await _taskRepository.GetByProjectAsync(request.ProjectId, cancellationToken);

        if (request.Status.HasValue)
        {
            tasks = tasks.Where(t => t.Status == request.Status.Value).ToList();
        }

        if (request.AssignedToUserId.HasValue)
        {
            tasks = tasks.Where(t => t.AssignedTo == request.AssignedToUserId.Value.ToString()).ToList();
        }

        var result = new List<KanbanTaskDto>();

        foreach (var task in tasks)
        {
            var labels = await _labelRepository.GetTaskLabelsAsync(task.TaskId, cancellationToken);
            var isBlocked = await _dependencyRepository.IsBlockedAsync(task.TaskId, cancellationToken);

            result.Add(new KanbanTaskDto
            {
                TaskId = task.TaskId,
                TaskTitle = task.TaskTitle,
                Status = task.Status,
                Priority = task.Priority,
                AssignedToName = task.AssignedTo,
                DueDate = task.DueDate,
                IsBlocked = isBlocked,
                Labels = labels.Select(l => new KanbanLabelDto
                {
                    LabelId = l.LabelId,
                    Name = l.Name,
                    Color = l.Color
                }).ToList()
            });
        }

        return result;
    }
}
