using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.CQRS.Commands.TaskDependencies;
using Nexus.Application.CQRS.Queries.TaskDependencies;

namespace Nexus.API.Controllers;

/// <summary>
/// Tapşırıq asılılıqları API
/// </summary>
[ApiController]
[Route("api/tasks/{taskId:long}/dependencies")]
[Authorize]
public class TaskDependenciesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TaskDependenciesController> _logger;

    public TaskDependenciesController(
        IMediator mediator,
        ILogger<TaskDependenciesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Bir tapşırığın bütün asılılıqlarını gətir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDependencies(
        long taskId,
        CancellationToken cancellationToken)
    {
        var query = new GetTaskDependenciesQuery(taskId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bir tapşırığa asılı olan bütün tapşırıqları gətir
    /// </summary>
    [HttpGet("dependents")]
    public async Task<IActionResult> GetDependents(
        long taskId,
        CancellationToken cancellationToken)
    {
        var query = new GetTaskDependentsQuery(taskId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Yeni asılılıq əlavə et
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddDependency(
        long taskId,
        [FromBody] AddDependencyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddTaskDependencyCommand(
            TaskId: taskId,
            DependsOnTaskId: request.DependsOnTaskId,
            Type: request.Type,
            LagDays: request.LagDays,
            Description: request.Description,
            CreatedBy: User.Identity?.Name ?? "system"
        );

        var result = await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation(
            "User {User} added dependency: Task {TaskId} depends on {DependsOnTaskId}",
            User.Identity?.Name,
            taskId,
            request.DependsOnTaskId);

        return CreatedAtAction(
            nameof(GetDependencies),
            new { taskId },
            result);
    }

    /// <summary>
    /// Asılılığı sil
    /// </summary>
    [HttpDelete("{dependencyId:long}")]
    public async Task<IActionResult> RemoveDependency(
        long taskId,
        long dependencyId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveTaskDependencyCommand(
            DependencyId: dependencyId,
            TaskId: taskId,
            RemovedBy: User.Identity?.Name ?? "system"
        );

        await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation(
            "User {User} removed dependency {DependencyId} from task {TaskId}",
            User.Identity?.Name,
            dependencyId,
            taskId);

        return NoContent();
    }

    /// <summary>
    /// Tapşırıq bloklanıb yoxsa yox
    /// </summary>
    [HttpGet("blocked")]
    public async Task<IActionResult> IsBlocked(
        long taskId,
        CancellationToken cancellationToken)
    {
        var query = new IsTaskBlockedQuery(taskId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new { taskId, isBlocked = result });
    }

    /// <summary>
    /// Tapşırıq başlaya bilərmi
    /// </summary>
    [HttpGet("can-start")]
    public async Task<IActionResult> CanStart(
        long taskId,
        CancellationToken cancellationToken)
    {
        var query = new CanTaskStartQuery(taskId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new { taskId, canStart = result });
    }

    /// <summary>
    /// Asılılıq grafi (visualization üçün)
    /// </summary>
    [HttpGet("graph")]
    public async Task<IActionResult> GetDependencyGraph(
        long taskId,
        [FromQuery] int depth = 3,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDependencyGraphQuery(taskId, depth);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}

public record AddDependencyRequest(
    long DependsOnTaskId,
    DependencyType Type = DependencyType.FinishToStart,
    int LagDays = 0,
    string? Description = null
);
