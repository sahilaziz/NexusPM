using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.CQRS.Queries.Kanban;
using Nexus.Domain.Entities;

namespace Nexus.API.Controllers;

/// <summary>
/// Kanban Board API
/// </summary>
[ApiController]
[Route("api/kanban")]
[Authorize]
public class KanbanController : ControllerBase
{
    private readonly IMediator _mediator;

    public KanbanController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Layihənin Kanban board məlumatları
    /// </summary>
    [HttpGet("projects/{projectId:long}")]
    public async Task<IActionResult> GetProjectKanban(
        long projectId,
        [FromQuery] long? assignedTo,
        CancellationToken cancellationToken)
    {
        var query = new GetProjectKanbanQuery(projectId, assignedTo);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Kanban sütunlarında tapşırıqları gətir
    /// </summary>
    [HttpGet("projects/{projectId:long}/tasks")]
    public async Task<IActionResult> GetKanbanTasks(
        long projectId,
        [FromQuery] TaskStatus? status,
        [FromQuery] long? assignedTo,
        CancellationToken cancellationToken)
    {
        var query = new GetKanbanTasksQuery(projectId, status, assignedTo);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
