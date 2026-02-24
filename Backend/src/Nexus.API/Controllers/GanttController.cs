using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.CQRS.Queries.Gantt;

namespace Nexus.API.Controllers;

/// <summary>
/// Gantt Chart API
/// </summary>
[ApiController]
[Route("api/gantt")]
[Authorize]
public class GanttController : ControllerBase
{
    private readonly IMediator _mediator;

    public GanttController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Layihənin Gantt chart məlumatları
    /// </summary>
    [HttpGet("projects/{projectId:long}")]
    public async Task<IActionResult> GetProjectGantt(long projectId, CancellationToken cancellationToken)
    {
        var query = new GetProjectGanttQuery(projectId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Kritik yolu hesabla
    /// </summary>
    [HttpGet("projects/{projectId:long}/critical-path")]
    public async Task<IActionResult> CalculateCriticalPath(long projectId, CancellationToken cancellationToken)
    {
        var query = new CalculateCriticalPathQuery(projectId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
