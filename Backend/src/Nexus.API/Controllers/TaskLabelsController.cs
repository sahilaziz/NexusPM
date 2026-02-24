using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.CQRS.Commands.TaskLabels;
using Nexus.Application.CQRS.Queries.TaskLabels;

namespace Nexus.API.Controllers;

/// <summary>
/// Task Labels API - Etiket idarəetməsi
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaskLabelsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TaskLabelsController> _logger;

    public TaskLabelsController(IMediator mediator, ILogger<TaskLabelsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Bütün etiketləri gətir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] long? projectId,
        [FromQuery] string orgCode = "default",
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAllLabelsQuery(projectId, orgCode, includeInactive);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Etiket axtar
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string term,
        [FromQuery] long? projectId,
        [FromQuery] string orgCode = "default",
        CancellationToken cancellationToken = default)
    {
        var query = new SearchLabelsQuery(term, projectId, orgCode);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Etiket detalları
    /// </summary>
    [HttpGet("{labelId:long}")]
    public async Task<IActionResult> GetById(long labelId, CancellationToken cancellationToken)
    {
        var query = new GetLabelByIdQuery(labelId);
        var result = await _mediator.Send(query, cancellationToken);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Yeni etiket yarat
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(
        [FromBody] CreateLabelRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTaskLabelCommand(
            request.Name,
            request.Description,
            request.Color,
            request.SortOrder,
            request.ProjectId,
            request.OrganizationCode ?? "default",
            User.Identity?.Name ?? "system"
        );

        var result = await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation("User {User} created label '{LabelName}'", 
            User.Identity?.Name, request.Name);

        return CreatedAtAction(nameof(GetById), new { labelId = result.LabelId }, result);
    }

    /// <summary>
    /// Etiketi redaktə et
    /// </summary>
    [HttpPut("{labelId:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(
        long labelId,
        [FromBody] UpdateLabelRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTaskLabelCommand(
            labelId,
            request.Name,
            request.Description,
            request.Color,
            request.SortOrder,
            User.Identity?.Name ?? "system"
        );

        var result = await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation("User {User} updated label {LabelId}", 
            User.Identity?.Name, labelId);

        return Ok(result);
    }

    /// <summary>
    /// Etiketi sil
    /// </summary>
    [HttpDelete("{labelId:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(long labelId, CancellationToken cancellationToken)
    {
        var command = new DeleteTaskLabelCommand(labelId, User.Identity?.Name ?? "system");
        await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation("User {User} deleted label {LabelId}", 
            User.Identity?.Name, labelId);

        return NoContent();
    }

    /// <summary>
    /// Etiket statistikası
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] long? projectId,
        [FromQuery] string orgCode = "default",
        CancellationToken cancellationToken = default)
    {
        var query = new GetLabelStatisticsQuery(projectId, orgCode);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    #region Task-Label Operations

    /// <summary>
    /// Tapşırığın etiketlərini gətir
    /// </summary>
    [HttpGet("tasks/{taskId:long}")]
    public async Task<IActionResult> GetTaskLabels(long taskId, CancellationToken cancellationToken)
    {
        var query = new GetTaskLabelsQuery(taskId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tapşırığa etiket təyin et
    /// </summary>
    [HttpPost("tasks/{taskId:long}")]
    public async Task<IActionResult> AssignLabelToTask(
        long taskId,
        [FromBody] AssignLabelRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignLabelToTaskCommand(
            taskId, 
            request.LabelId, 
            User.Identity?.Name ?? "system");
        
        await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation("User {User} assigned label {LabelId} to task {TaskId}", 
            User.Identity?.Name, request.LabelId, taskId);

        return NoContent();
    }

    /// <summary>
    /// Tapşırıqdan etiketi çıxar
    /// </summary>
    [HttpDelete("tasks/{taskId:long}/{labelId:long}")]
    public async Task<IActionResult> RemoveLabelFromTask(
        long taskId, 
        long labelId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveLabelFromTaskCommand(
            taskId, 
            labelId, 
            User.Identity?.Name ?? "system");
        
        await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation("User {User} removed label {LabelId} from task {TaskId}", 
            User.Identity?.Name, labelId, taskId);

        return NoContent();
    }

    /// <summary>
    /// Batch etiket təyinatı
    /// </summary>
    [HttpPost("tasks/{taskId:long}/batch")]
    public async Task<IActionResult> BatchAssignLabels(
        long taskId,
        [FromBody] BatchAssignLabelsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new BatchAssignLabelsCommand(
            taskId, 
            request.LabelIds, 
            User.Identity?.Name ?? "system");
        
        var result = await _mediator.Send(command, cancellationToken);
        
        return Ok(result);
    }

    /// <summary>
    /// Etiketə aid tapşırıqları gətir
    /// </summary>
    [HttpGet("{labelId:long}/tasks")]
    public async Task<IActionResult> GetTasksByLabel(long labelId, CancellationToken cancellationToken)
    {
        var query = new GetTasksByLabelQuery(labelId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    #endregion
}

// Request DTOs
public record CreateLabelRequest(
    string Name,
    string? Description,
    string Color,
    int SortOrder = 0,
    long? ProjectId = null,
    string? OrganizationCode = null
);

public record UpdateLabelRequest(
    string Name,
    string? Description,
    string Color,
    int SortOrder = 0
);

public record AssignLabelRequest(long LabelId);

public record BatchAssignLabelsRequest(List<long> LabelIds);
