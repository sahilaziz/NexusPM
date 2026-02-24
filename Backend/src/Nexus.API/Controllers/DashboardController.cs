using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.CQRS.Queries.Dashboard;

namespace Nexus.API.Controllers;

/// <summary>
/// Dashboard API
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// İstifadəçi dashboard
    /// </summary>
    [HttpGet("user/{userId:long}")]
    public async Task<IActionResult> GetUserDashboard(long userId, CancellationToken cancellationToken)
    {
        var query = new GetUserDashboardQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Layihə dashboard
    /// </summary>
    [HttpGet("projects/{projectId:long}")]
    public async Task<IActionResult> GetProjectDashboard(long projectId, CancellationToken cancellationToken)
    {
        var query = new GetProjectDashboardQuery(projectId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Admin dashboard
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAdminDashboard(CancellationToken cancellationToken)
    {
        var query = new GetAdminDashboardQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
