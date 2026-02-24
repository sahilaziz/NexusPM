using MediatR;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;
using TaskStatus = Nexus.Domain.Entities.TaskStatus;

namespace Nexus.Application.CQRS.Queries.Gantt;

// ============== QUERIES ==============

public record GetProjectGanttQuery(long ProjectId) : IRequest<GanttChartDto>;

public record CalculateCriticalPathQuery(long ProjectId) : IRequest<CriticalPathDto>;

// ============== DTOS ==============

public class GanttChartDto
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = null!;
    public List<GanttTaskDto> Tasks { get; set; } = new();
    public List<GanttDependencyDto> Dependencies { get; set; } = new();
}

public class GanttTaskDto
{
    public long TaskId { get; set; }
    public string TaskTitle { get; set; } = null!;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int ProgressPercent { get; set; }
    public TaskStatus Status { get; set; }
    public long? ParentTaskId { get; set; }
    public string Color { get; set; } = "#3B82F6";
}

public class GanttDependencyDto
{
    public long FromTaskId { get; set; }
    public long ToTaskId { get; set; }
    public DependencyType Type { get; set; }
}

public class CriticalPathDto
{
    public List<long> CriticalTaskIds { get; set; } = new();
    public int TotalDurationDays { get; set; }
}

// ============== HANDLERS ==============

public class GetProjectGanttHandler : IRequestHandler<GetProjectGanttQuery, GanttChartDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskDependencyRepository _dependencyRepository;

    public GetProjectGanttHandler(
        IProjectRepository projectRepository,
        ITaskRepository taskRepository,
        ITaskDependencyRepository dependencyRepository)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _dependencyRepository = dependencyRepository;
    }

    public async Task<GanttChartDto> Handle(GetProjectGanttQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            throw new KeyNotFoundException($"Layihə tapılmadı: {request.ProjectId}");

        var tasks = await _taskRepository.GetByProjectAsync(request.ProjectId, cancellationToken);
        
        var ganttTasks = tasks.Select(t => new GanttTaskDto
        {
            TaskId = t.TaskId,
            TaskTitle = t.TaskTitle,
            StartDate = t.StartDate ?? t.DueDate?.AddDays(-7),
            EndDate = t.EndDate ?? t.DueDate,
            ProgressPercent = t.Status switch
            {
                TaskStatus.Todo => 0,
                TaskStatus.InProgress => 50,
                TaskStatus.Review => 80,
                TaskStatus.Done => 100,
                _ => 0
            },
            Status = t.Status,
            ParentTaskId = t.ParentTaskId,
            Color = t.Status switch
            {
                TaskStatus.Todo => "#6B7280",
                TaskStatus.InProgress => "#3B82F6",
                TaskStatus.Review => "#F59E0B",
                TaskStatus.Done => "#10B981",
                _ => "#6B7280"
            }
        }).ToList();

        var dependencies = new List<GanttDependencyDto>();
        foreach (var task in tasks)
        {
            var deps = await _dependencyRepository.GetDependenciesAsync(task.TaskId, cancellationToken);
            dependencies.AddRange(deps.Select(d => new GanttDependencyDto
            {
                FromTaskId = d.DependsOnTaskId,
                ToTaskId = d.TaskId,
                Type = d.Type
            }));
        }

        return new GanttChartDto
        {
            ProjectId = project.ProjectId,
            ProjectName = project.ProjectName,
            Tasks = ganttTasks,
            Dependencies = dependencies
        };
    }
}
