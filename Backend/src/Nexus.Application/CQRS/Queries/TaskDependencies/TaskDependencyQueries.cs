using MediatR;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;
using TaskStatus = Nexus.Domain.Entities.TaskStatus;

namespace Nexus.Application.CQRS.Queries.TaskDependencies;

// ============== QUERIES ==============

public record GetTaskDependenciesQuery(long TaskId) : IRequest<IReadOnlyList<TaskDependencyInfo>>;
public record GetTaskDependentsQuery(long TaskId) : IRequest<IReadOnlyList<TaskDependencyInfo>>;
public record IsTaskBlockedQuery(long TaskId) : IRequest<bool>;
public record CanTaskStartQuery(long TaskId) : IRequest<bool>;
public record GetDependencyGraphQuery(long TaskId, int Depth = 3) : IRequest<DependencyGraphDto>;

// ============== HANDLERS ==============

public class GetTaskDependenciesHandler : IRequestHandler<GetTaskDependenciesQuery, IReadOnlyList<TaskDependencyInfo>>
{
    private readonly ITaskDependencyRepository _repository;

    public GetTaskDependenciesHandler(ITaskDependencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TaskDependencyInfo>> Handle(
        GetTaskDependenciesQuery request, 
        CancellationToken cancellationToken)
    {
        return await _repository.GetDependenciesAsync(request.TaskId, cancellationToken);
    }
}

public class GetTaskDependentsHandler : IRequestHandler<GetTaskDependentsQuery, IReadOnlyList<TaskDependencyInfo>>
{
    private readonly ITaskDependencyRepository _repository;

    public GetTaskDependentsHandler(ITaskDependencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TaskDependencyInfo>> Handle(
        GetTaskDependentsQuery request, 
        CancellationToken cancellationToken)
    {
        return await _repository.GetDependentsAsync(request.TaskId, cancellationToken);
    }
}

public class IsTaskBlockedHandler : IRequestHandler<IsTaskBlockedQuery, bool>
{
    private readonly ITaskDependencyRepository _repository;

    public IsTaskBlockedHandler(ITaskDependencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(
        IsTaskBlockedQuery request, 
        CancellationToken cancellationToken)
    {
        return await _repository.IsBlockedAsync(request.TaskId, cancellationToken);
    }
}

public class CanTaskStartHandler : IRequestHandler<CanTaskStartQuery, bool>
{
    private readonly ITaskDependencyRepository _repository;

    public CanTaskStartHandler(ITaskDependencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(
        CanTaskStartQuery request, 
        CancellationToken cancellationToken)
    {
        return await _repository.CanStartAsync(request.TaskId, cancellationToken);
    }
}

public class GetDependencyGraphHandler : IRequestHandler<GetDependencyGraphQuery, DependencyGraphDto>
{
    private readonly ITaskDependencyRepository _repository;

    public GetDependencyGraphHandler(ITaskDependencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<DependencyGraphDto> Handle(
        GetDependencyGraphQuery request, 
        CancellationToken cancellationToken)
    {
        var nodes = new List<DependencyNodeDto>();
        var edges = new List<DependencyEdgeDto>();
        var visited = new HashSet<long>();
        
        await BuildGraphRecursive(
            request.TaskId, 
            0, 
            request.Depth, 
            nodes, 
            edges, 
            visited, 
            cancellationToken);

        return new DependencyGraphDto
        {
            RootTaskId = request.TaskId,
            Nodes = nodes,
            Edges = edges
        };
    }

    private async Task BuildGraphRecursive(
        long taskId,
        int currentDepth,
        int maxDepth,
        List<DependencyNodeDto> nodes,
        List<DependencyEdgeDto> edges,
        HashSet<long> visited,
        CancellationToken cancellationToken)
    {
        if (currentDepth > maxDepth || visited.Contains(taskId))
            return;

        visited.Add(taskId);

        // Bu tapşırığın asılılıqlarını al
        var dependencies = await _repository.GetDependenciesAsync(taskId, cancellationToken);
        
        // Node əlavə et (əgər əlavə edilməyibsə)
        if (!nodes.Any(n => n.TaskId == taskId))
        {
            nodes.Add(new DependencyNodeDto
            {
                TaskId = taskId,
                Depth = currentDepth,
                IsRoot = currentDepth == 0
            });
        }

        // Hər bir asılılıq üçün recursive çağırış
        foreach (var dep in dependencies)
        {
            // Edge əlavə et
            edges.Add(new DependencyEdgeDto
            {
                FromTaskId = taskId,
                ToTaskId = dep.DependsOnTaskId,
                Type = dep.Type,
                IsBlocking = dep.IsBlocking
            });

            // Node əlavə et
            if (!nodes.Any(n => n.TaskId == dep.DependsOnTaskId))
            {
                nodes.Add(new DependencyNodeDto
                {
                    TaskId = dep.DependsOnTaskId,
                    Title = dep.DependsOnTaskTitle,
                    Status = dep.DependsOnTaskStatus,
                    Depth = currentDepth + 1,
                    IsRoot = false
                });
            }

            // Recursive
            await BuildGraphRecursive(
                dep.DependsOnTaskId, 
                currentDepth + 1, 
                maxDepth, 
                nodes, 
                edges, 
                visited, 
                cancellationToken);
        }
    }
}

// ============== DTOs ==============

public class DependencyGraphDto
{
    public long RootTaskId { get; set; }
    public IReadOnlyList<DependencyNodeDto> Nodes { get; set; } = new List<DependencyNodeDto>();
    public IReadOnlyList<DependencyEdgeDto> Edges { get; set; } = new List<DependencyEdgeDto>();
}

public class DependencyNodeDto
{
    public long TaskId { get; set; }
    public string? Title { get; set; }
    public TaskStatus? Status { get; set; }
    public int Depth { get; set; }
    public bool IsRoot { get; set; }
}

public class DependencyEdgeDto
{
    public long FromTaskId { get; set; }
    public long ToTaskId { get; set; }
    public DependencyType Type { get; set; }
    public bool IsBlocking { get; set; }
}
