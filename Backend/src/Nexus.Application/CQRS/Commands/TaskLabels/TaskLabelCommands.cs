using MediatR;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;

namespace Nexus.Application.CQRS.Commands.TaskLabels;

// ============== COMMANDS ==============

public record CreateTaskLabelCommand(
    string Name,
    string? Description,
    string Color,
    int SortOrder,
    long? ProjectId,
    string OrganizationCode,
    string CreatedBy = "system"
) : IRequest<TaskLabelResponse>;

public record UpdateTaskLabelCommand(
    long LabelId,
    string Name,
    string? Description,
    string Color,
    int SortOrder,
    string UpdatedBy = "system"
) : IRequest<TaskLabelResponse>;

public record DeleteTaskLabelCommand(
    long LabelId,
    string DeletedBy = "system"
) : IRequest<Unit>;

public record AssignLabelToTaskCommand(
    long TaskId,
    long LabelId,
    string AssignedBy = "system"
) : IRequest<Unit>;

public record RemoveLabelFromTaskCommand(
    long TaskId,
    long LabelId,
    string RemovedBy = "system"
) : IRequest<Unit>;

public record BatchAssignLabelsCommand(
    long TaskId,
    List<long> LabelIds,
    string AssignedBy = "system"
) : IRequest<BatchAssignLabelsResponse>;

// ============== RESPONSES ==============

public record TaskLabelResponse(
    long LabelId,
    string Name,
    string? Description,
    string Color,
    int SortOrder,
    long? ProjectId,
    string OrganizationCode,
    bool IsSystem,
    bool IsActive
);

public record BatchAssignLabelsResponse(
    long TaskId,
    int AssignedCount,
    int FailedCount,
    List<long> FailedLabelIds
);

// ============== HANDLERS ==============

public class CreateTaskLabelHandler : IRequestHandler<CreateTaskLabelCommand, TaskLabelResponse>
{
    private readonly ITaskLabelRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTaskLabelHandler(ITaskLabelRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskLabelResponse> Handle(CreateTaskLabelCommand request, CancellationToken cancellationToken)
    {
        // Validation: Name cannot be empty
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Etiket adı boş ola bilməz");
        }

        // Validation: Name must be unique within project
        var exists = await _repository.ExistsAsync(
            request.Name, 
            request.ProjectId, 
            request.OrganizationCode, 
            cancellationToken);
        
        if (exists)
        {
            throw new InvalidOperationException($"'{request.Name}' adlı etiket artıq mövcuddur");
        }

        // Validation: Color must be valid hex
        if (!IsValidHexColor(request.Color))
        {
            throw new ArgumentException("Rəng kodu düzgün deyil (məsələn: #FF5733)");
        }

        var label = new TaskLabel
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Color = request.Color.ToUpper(),
            SortOrder = request.SortOrder,
            ProjectId = request.ProjectId,
            OrganizationCode = request.OrganizationCode,
            IsSystem = false,
            IsActive = true,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(label, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(label);
    }

    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrEmpty(color) || color.Length != 7)
            return false;
        
        return color[0] == '#' && 
               color.Skip(1).All(c => (c >= '0' && c <= '9') || 
                                        (c >= 'A' && c <= 'F') || 
                                        (c >= 'a' && c <= 'f'));
    }
}

public class UpdateTaskLabelHandler : IRequestHandler<UpdateTaskLabelCommand, TaskLabelResponse>
{
    private readonly ITaskLabelRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTaskLabelHandler(ITaskLabelRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskLabelResponse> Handle(UpdateTaskLabelCommand request, CancellationToken cancellationToken)
    {
        var label = await _repository.GetByIdAsync(request.LabelId, cancellationToken);
        
        if (label == null)
        {
            throw new KeyNotFoundException($"Etiket tapılmadı: {request.LabelId}");
        }

        if (label.IsSystem)
        {
            throw new InvalidOperationException("Sistem etiketləri redaktə edilə bilməz");
        }

        // Validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Etiket adı boş ola bilməz");
        }

        // Check if name changed and new name already exists
        if (!label.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _repository.ExistsAsync(
                request.Name, 
                label.ProjectId, 
                label.OrganizationCode, 
                cancellationToken);
            
            if (exists)
            {
                throw new InvalidOperationException($"'{request.Name}' adlı etiket artıq mövcuddur");
            }
        }

        label.Name = request.Name.Trim();
        label.Description = request.Description?.Trim();
        label.Color = request.Color.ToUpper();
        label.SortOrder = request.SortOrder;

        await _repository.UpdateAsync(label, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(label);
    }
}

public class DeleteTaskLabelHandler : IRequestHandler<DeleteTaskLabelCommand, Unit>
{
    private readonly ITaskLabelRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTaskLabelHandler(ITaskLabelRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteTaskLabelCommand request, CancellationToken cancellationToken)
    {
        var label = await _repository.GetByIdAsync(request.LabelId, cancellationToken);
        
        if (label == null)
        {
            throw new KeyNotFoundException($"Etiket tapılmadı: {request.LabelId}");
        }

        if (label.IsSystem)
        {
            throw new InvalidOperationException("Sistem etiketləri silinə bilməz");
        }

        // Check if label is in use
        var taskCount = await _repository.GetTaskCountByLabelAsync(request.LabelId, cancellationToken);
        if (taskCount > 0)
        {
            throw new InvalidOperationException(
                $"Bu etiket {taskCount} tapşırıqda istifadə olunur. Əvvəlcə etiketi tapşırıqlardan çıxarın.");
        }

        await _repository.DeleteAsync(label, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return Unit.Value;
    }
}

public class AssignLabelToTaskHandler : IRequestHandler<AssignLabelToTaskCommand, Unit>
{
    private readonly ITaskLabelRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignLabelToTaskHandler(ITaskLabelRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(AssignLabelToTaskCommand request, CancellationToken cancellationToken)
    {
        // Check if already assigned
        var exists = await _repository.TaskHasLabelAsync(
            request.TaskId, 
            request.LabelId, 
            cancellationToken);
        
        if (exists)
        {
            throw new InvalidOperationException("Bu etiket artıq tapşırığa təyin edilib");
        }

        await _repository.AssignLabelToTaskAsync(
            request.TaskId, 
            request.LabelId, 
            request.AssignedBy, 
            cancellationToken);
        
        await _unitOfWork.SaveChangesAsync();

        return Unit.Value;
    }
}

public class RemoveLabelFromTaskHandler : IRequestHandler<RemoveLabelFromTaskCommand, Unit>
{
    private readonly ITaskLabelRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveLabelFromTaskHandler(ITaskLabelRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(RemoveLabelFromTaskCommand request, CancellationToken cancellationToken)
    {
        await _repository.RemoveLabelFromTaskAsync(
            request.TaskId, 
            request.LabelId, 
            cancellationToken);
        
        await _unitOfWork.SaveChangesAsync();

        return Unit.Value;
    }
}

public class BatchAssignLabelsHandler : IRequestHandler<BatchAssignLabelsCommand, BatchAssignLabelsResponse>
{
    private readonly ITaskLabelRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public BatchAssignLabelsHandler(ITaskLabelRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BatchAssignLabelsResponse> Handle(
        BatchAssignLabelsCommand request, 
        CancellationToken cancellationToken)
    {
        var failedLabelIds = new List<long>();
        var assignedCount = 0;

        foreach (var labelId in request.LabelIds)
        {
            try
            {
                var exists = await _repository.TaskHasLabelAsync(
                    request.TaskId, 
                    labelId, 
                    cancellationToken);
                
                if (!exists)
                {
                    await _repository.AssignLabelToTaskAsync(
                        request.TaskId, 
                        labelId, 
                        request.AssignedBy, 
                        cancellationToken);
                    
                    assignedCount++;
                }
            }
            catch
            {
                failedLabelIds.Add(labelId);
            }
        }

        if (assignedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return new BatchAssignLabelsResponse(
            request.TaskId,
            assignedCount,
            failedLabelIds.Count,
            failedLabelIds);
    }
}

// ============== HELPER ==============

private static TaskLabelResponse MapToResponse(TaskLabel label)
{
    return new TaskLabelResponse(
        label.LabelId,
        label.Name,
        label.Description,
        label.Color,
        label.SortOrder,
        label.ProjectId,
        label.OrganizationCode,
        label.IsSystem,
        label.IsActive
    );
}
