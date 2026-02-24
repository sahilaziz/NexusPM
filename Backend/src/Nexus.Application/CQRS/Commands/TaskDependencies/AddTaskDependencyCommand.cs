using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Domain.Entities;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Repositories;

namespace Nexus.Application.CQRS.Commands.TaskDependencies;

/// <summary>
/// Tapşırıq asılılığı əlavə et
/// </summary>
public record AddTaskDependencyCommand(
    long TaskId,
    long DependsOnTaskId,
    DependencyType Type,
    int LagDays = 0,
    string? Description = null,
    string CreatedBy = "system"
) : IRequest<TaskDependencyResponse>;

public record TaskDependencyResponse(
    long DependencyId,
    long TaskId,
    long DependsOnTaskId,
    DependencyType Type,
    bool IsValid,
    string? Warning = null
);

public class AddTaskDependencyHandler : IRequestHandler<AddTaskDependencyCommand, TaskDependencyResponse>
{
    private readonly ITaskDependencyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AddTaskDependencyHandler(
        ITaskDependencyRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskDependencyResponse> Handle(
        AddTaskDependencyCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Self-dependency check
        if (request.TaskId == request.DependsOnTaskId)
        {
            throw new InvalidOperationException("Tapşırıq özündən asılı ola bilməz");
        }

        // 2. Same project check
        var taskProject = await _repository.GetTaskProjectIdAsync(request.TaskId, cancellationToken);
        var dependsOnProject = await _repository.GetTaskProjectIdAsync(request.DependsOnTaskId, cancellationToken);
        
        if (taskProject != dependsOnProject)
        {
            throw new InvalidOperationException("Fərqli layihələrdəki tapşırıqlar arasında asılılıq yaradıla bilməz");
        }

        // 3. Circular dependency check
        var wouldCreateCycle = await _repository.WouldCreateCycleAsync(
            request.TaskId, 
            request.DependsOnTaskId, 
            cancellationToken);
        
        if (wouldCreateCycle)
        {
            throw new InvalidOperationException("Dairəvi asılılıq yaradıla bilməz (Circular dependency)");
        }

        // 4. Check if already exists
        var exists = await _repository.ExistsAsync(request.TaskId, request.DependsOnTaskId, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Bu asılılıq artıq mövcuddur");
        }

        // 5. Create dependency
        var dependency = new TaskDependency
        {
            TaskId = request.TaskId,
            DependsOnTaskId = request.DependsOnTaskId,
            Type = request.Type,
            LagDays = request.LagDays,
            Description = request.Description,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(dependency, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        // 6. Check if blocking
        var dependsOnStatus = await _repository.GetTaskStatusAsync(request.DependsOnTaskId, cancellationToken);
        var isBlocking = dependsOnStatus != TaskStatus.Done;

        return new TaskDependencyResponse(
            dependency.DependencyId,
            dependency.TaskId,
            dependency.DependsOnTaskId,
            dependency.Type,
            isValid: true,
            isBlocking ? "Bu asılılıq tapşırığın başlamasını bloklayır" : null
        );
    }
}
