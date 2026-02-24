using MediatR;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;

namespace Nexus.Application.CQRS.Queries.TaskLabels;

// ============== QUERIES ==============

public record GetAllLabelsQuery(
    long? ProjectId, 
    string OrganizationCode,
    bool IncludeInactive = false
) : IRequest<IReadOnlyList<LabelListItemDto>>;

public record GetLabelByIdQuery(long LabelId) : IRequest<LabelDetailDto?>;

public record GetTaskLabelsQuery(long TaskId) : IRequest<IReadOnlyList<LabelDto>>;

public record GetTasksByLabelQuery(long LabelId) : IRequest<IReadOnlyList<TaskWithLabels>>;

public record GetLabelStatisticsQuery(
    long? ProjectId,
    string OrganizationCode
) : IRequest<IReadOnlyList<LabelStatisticsDto>>;

public record SearchLabelsQuery(
    string SearchTerm,
    long? ProjectId,
    string OrganizationCode,
    int MaxResults = 10
) : IRequest<IReadOnlyList<LabelListItemDto>>;

// ============== DTOs ==============

public record LabelListItemDto(
    long LabelId,
    string Name,
    string? Description,
    string Color,
    int SortOrder,
    bool IsSystem,
    bool IsActive,
    int TaskCount
);

public record LabelDetailDto(
    long LabelId,
    string Name,
    string? Description,
    string Color,
    int SortOrder,
    long? ProjectId,
    string? ProjectName,
    string OrganizationCode,
    bool IsSystem,
    bool IsActive,
    DateTime CreatedAt,
    string CreatedBy,
    int TaskCount
);

public record LabelStatisticsDto(
    long LabelId,
    string Name,
    string Color,
    int TotalTasks,
    int TodoTasks,
    int InProgressTasks,
    int DoneTasks
);

// ============== HANDLERS ==============

public class GetAllLabelsHandler : IRequestHandler<GetAllLabelsQuery, IReadOnlyList<LabelListItemDto>>
{
    private readonly ITaskLabelRepository _repository;

    public GetAllLabelsHandler(ITaskLabelRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<LabelListItemDto>> Handle(
        GetAllLabelsQuery request, 
        CancellationToken cancellationToken)
    {
        var labels = request.IncludeInactive
            ? await _repository.GetByProjectAsync(request.ProjectId, request.OrganizationCode, cancellationToken)
            : await _repository.GetActiveLabelsAsync(request.ProjectId, request.OrganizationCode, cancellationToken);

        var result = new List<LabelListItemDto>();
        
        foreach (var label in labels)
        {
            var count = await _repository.GetTaskCountByLabelAsync(label.LabelId, cancellationToken);
            
            result.Add(new LabelListItemDto(
                label.LabelId,
                label.Name,
                label.Description,
                label.Color,
                label.SortOrder,
                label.IsSystem,
                label.IsActive,
                count
            ));
        }

        return result;
    }
}

public class GetLabelByIdHandler : IRequestHandler<GetLabelByIdQuery, LabelDetailDto?>
{
    private readonly ITaskLabelRepository _repository;

    public GetLabelByIdHandler(ITaskLabelRepository repository)
    {
        _repository = repository;
    }

    public async Task<LabelDetailDto?> Handle(
        GetLabelByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var label = await _repository.GetByIdAsync(request.LabelId, cancellationToken);
        
        if (label == null)
            return null;

        var count = await _repository.GetTaskCountByLabelAsync(label.LabelId, cancellationToken);

        return new LabelDetailDto(
            label.LabelId,
            label.Name,
            label.Description,
            label.Color,
            label.SortOrder,
            label.ProjectId,
            label.Project?.ProjectName,
            label.OrganizationCode,
            label.IsSystem,
            label.IsActive,
            label.CreatedAt,
            label.CreatedBy,
            count
        );
    }
}

public class GetTaskLabelsHandler : IRequestHandler<GetTaskLabelsQuery, IReadOnlyList<LabelDto>>
{
    private readonly ITaskLabelRepository _repository;

    public GetTaskLabelsHandler(ITaskLabelRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<LabelDto>> Handle(
        GetTaskLabelsQuery request, 
        CancellationToken cancellationToken)
    {
        return await _repository.GetTaskLabelsAsync(request.TaskId, cancellationToken);
    }
}

public class GetTasksByLabelHandler : IRequestHandler<GetTasksByLabelQuery, IReadOnlyList<TaskWithLabels>>
{
    private readonly ITaskLabelRepository _repository;

    public GetTasksByLabelHandler(ITaskLabelRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TaskWithLabels>> Handle(
        GetTasksByLabelQuery request, 
        CancellationToken cancellationToken)
    {
        return await _repository.GetTasksByLabelAsync(request.LabelId, cancellationToken);
    }
}

public class GetLabelStatisticsHandler : IRequestHandler<GetLabelStatisticsQuery, IReadOnlyList<LabelStatisticsDto>>
{
    private readonly ITaskLabelRepository _repository;
    private readonly ITaskRepository _taskRepository;

    public GetLabelStatisticsHandler(ITaskLabelRepository repository, ITaskRepository taskRepository)
    {
        _repository = repository;
        _taskRepository = taskRepository;
    }

    public async Task<IReadOnlyList<LabelStatisticsDto>> Handle(
        GetLabelStatisticsQuery request, 
        CancellationToken cancellationToken)
    {
        var labels = await _repository.GetActiveLabelsAsync(
            request.ProjectId, 
            request.OrganizationCode, 
            cancellationToken);

        var result = new List<LabelStatisticsDto>();

        foreach (var label in labels)
        {
            var tasks = await _repository.GetTasksByLabelAsync(label.LabelId, cancellationToken);
            
            result.Add(new LabelStatisticsDto(
                label.LabelId,
                label.Name,
                label.Color,
                tasks.Count,
                tasks.Count(t => t.Status == TaskStatus.Todo),
                tasks.Count(t => t.Status == TaskStatus.InProgress),
                tasks.Count(t => t.Status == TaskStatus.Done)
            ));
        }

        return result;
    }
}

public class SearchLabelsHandler : IRequestHandler<SearchLabelsQuery, IReadOnlyList<LabelListItemDto>>
{
    private readonly ITaskLabelRepository _repository;

    public SearchLabelsHandler(ITaskLabelRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<LabelListItemDto>> Handle(
        SearchLabelsQuery request, 
        CancellationToken cancellationToken)
    {
        var labels = await _repository.GetActiveLabelsAsync(
            request.ProjectId, 
            request.OrganizationCode, 
            cancellationToken);

        var filtered = labels
            .Where(l => l.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase))
            .Take(request.MaxResults)
            .ToList();

        var result = new List<LabelListItemDto>();
        
        foreach (var label in filtered)
        {
            var count = await _repository.GetTaskCountByLabelAsync(label.LabelId, cancellationToken);
            
            result.Add(new LabelListItemDto(
                label.LabelId,
                label.Name,
                label.Description,
                label.Color,
                label.SortOrder,
                label.IsSystem,
                label.IsActive,
                count
            ));
        }

        return result;
    }
}
