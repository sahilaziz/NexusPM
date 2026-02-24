using MediatR;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Repositories;

namespace Nexus.Application.CQRS.Commands.TaskDependencies;

/// <summary>
/// Tapşırıq asılılığını sil
/// </summary>
public record RemoveTaskDependencyCommand(
    long DependencyId,
    long TaskId,
    string RemovedBy = "system"
) : IRequest<Unit>;

public class RemoveTaskDependencyHandler : IRequestHandler<RemoveTaskDependencyCommand, Unit>
{
    private readonly ITaskDependencyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveTaskDependencyHandler(
        ITaskDependencyRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(
        RemoveTaskDependencyCommand request, 
        CancellationToken cancellationToken)
    {
        var dependency = await _repository.GetByIdAsync(request.DependencyId, cancellationToken);
        
        if (dependency == null)
        {
            throw new KeyNotFoundException($"Asılılıq tapılmadı: {request.DependencyId}");
        }

        // Təhlükəsizlik: yalnız bu tapşırığın asılılığını silməyə icazə ver
        if (dependency.TaskId != request.TaskId)
        {
            throw new InvalidOperationException("Bu asılılıq göstərilən tapşırığa aid deyil");
        }

        await _repository.DeleteAsync(dependency, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return Unit.Value;
    }
}
