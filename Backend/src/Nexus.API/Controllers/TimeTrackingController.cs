using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.CQRS.Commands.TimeTracking;
using Nexus.Application.CQRS.Queries.TimeTracking;
using Nexus.Domain.Entities;

namespace Nexus.API.Controllers;

/// <summary>
/// Time Tracking API - Vaxt izləmə
/// </summary>
[ApiController]
[Route("api/time")]
[Authorize]
public class TimeTrackingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TimeTrackingController> _logger;

    public TimeTrackingController(IMediator mediator, ILogger<TimeTrackingController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    #region Timer Operations

    /// <summary>
    /// Aktiv timer-i gətir
    /// </summary>
    [HttpGet("timer")]
    public async Task<IActionResult> GetRunningTimer(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var query = new GetRunningTimerQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);
        
        if (result == null)
            return Ok(new { isRunning = false });
            
        return Ok(new { isRunning = true, timer = result });
    }

    /// <summary>
    /// Timer başlat
    /// </summary>
    [HttpPost("timer/start")]
    public async Task<IActionResult> StartTimer([FromBody] StartTimerRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        
        var command = new StartTimerCommand(
            request.TaskId,
            userId,
            request.Description,
            request.WorkType,
            request.IsBillable,
            request.HourlyRate,
            User.Identity?.Name ?? "system"
        );

        var result = await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation("User {User} started timer for task {TaskId}", 
            User.Identity?.Name, request.TaskId);

        return Ok(result);
    }

    /// <summary>
    /// Timer dayandır
    /// </summary>
    [HttpPost("timer/stop")]
    public async Task<IActionResult> StopTimer([FromBody] StopTimerRequest? request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        
        var command = new StopTimerCommand(userId, request?.Description);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result == null)
            return BadRequest(new { error = "Aktiv timer tapılmadı" });

        _logger.LogInformation("User {User} stopped timer. Duration: {Duration}", 
            User.Identity?.Name, result.FormattedDuration);

        return Ok(result);
    }

    #endregion

    #region Time Entries

    /// <summary>
    /// İstifadəçinin vaxt qeydləri
    /// </summary>
    [HttpGet("entries")]
    public async Task<IActionResult> GetUserEntries(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var query = new GetUserTimeEntriesQuery(userId, from, to);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tapşırığın vaxt qeydləri
    /// </summary>
    [HttpGet("tasks/{taskId:long}/entries")]
    public async Task<IActionResult> GetTaskEntries(long taskId, CancellationToken cancellationToken)
    {
        var query = new GetTaskTimeEntriesQuery(taskId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Manuel vaxt qeydi əlavə et
    /// </summary>
    [HttpPost("entries")]
    public async Task<IActionResult> LogTime([FromBody] LogTimeRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        
        var command = new LogTimeCommand(
            request.TaskId,
            userId,
            request.StartTime,
            request.EndTime,
            request.Description,
            request.WorkType,
            request.IsBillable,
            request.HourlyRate,
            User.Identity?.Name ?? "system"
        );

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetUserEntries), null, result);
    }

    /// <summary>
    /// Vaxt qeydini redaktə et
    /// </summary>
    [HttpPut("entries/{timeEntryId:long}")]
    public async Task<IActionResult> EditTimeEntry(
        long timeEntryId,
        [FromBody] EditTimeEntryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new EditTimeEntryCommand(
            timeEntryId,
            request.StartTime,
            request.EndTime,
            request.Description,
            request.WorkType,
            request.IsBillable,
            User.Identity?.Name ?? "system"
        );

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Vaxt qeydini sil
    /// </summary>
    [HttpDelete("entries/{timeEntryId:long}")]
    public async Task<IActionResult> DeleteTimeEntry(long timeEntryId, CancellationToken cancellationToken)
    {
        var command = new DeleteTimeEntryCommand(timeEntryId, User.Identity?.Name ?? "system");
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    #endregion

    #region Summaries & Reports

    /// <summary>
    /// Günlük özət
    /// </summary>
    [HttpGet("summary/daily")]
    public async Task<IActionResult> GetDailySummary(
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var targetDate = date ?? DateTime.Today;
        
        var query = new GetDailySummaryQuery(userId, targetDate);
        var result = await _mediator.Send(query, cancellationToken);
        
        if (result == null)
            return Ok(new { date = targetDate, totalMinutes = 0, entries = new List<object>() });
            
        return Ok(result);
    }

    /// <summary>
    /// Həftəlik özət
    /// </summary>
    [HttpGet("summary/weekly")]
    public async Task<IActionResult> GetWeeklySummary(
        [FromQuery] int? year,
        [FromQuery] int? week,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var targetYear = year ?? DateTime.Now.Year;
        var targetWeek = week ?? GetWeekNumber(DateTime.Now);
        
        var query = new GetWeeklySummaryQuery(userId, targetYear, targetWeek);
        var result = await _mediator.Send(query, cancellationToken);
        
        if (result == null)
            return Ok(new { year = targetYear, week = targetWeek, totalMinutes = 0 });
            
        return Ok(result);
    }

    /// <summary>
    /// Vaxt statistikası
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var query = new GetTimeStatisticsQuery(userId, from, to);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Approval Workflow

    /// <summary>
    /// Təsdiq gözləyən vaxt qeydləri
    /// </summary>
    [HttpGet("pending-approval")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetPendingApproval(
        [FromQuery] long? projectId,
        CancellationToken cancellationToken)
    {
        var query = new GetPendingApprovalQuery(projectId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Vaxt qeydini təsdiqlə
    /// </summary>
    [HttpPost("entries/{timeEntryId:long}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ApproveTimeEntry(long timeEntryId, CancellationToken cancellationToken)
    {
        var command = new ApproveTimeEntryCommand(timeEntryId, User.Identity?.Name ?? "system");
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    #endregion

    #region Helpers

    private long GetCurrentUserId()
    {
        // TODO: Get from JWT claims
        // For now, return 1
        return 1;
    }

    private static int GetWeekNumber(DateTime date)
    {
        var dayOffset = DayOfWeek.Monday - date.DayOfWeek;
        var monday = date.AddDays(dayOffset);
        var firstMonday = new DateTime(date.Year, 1, 1);
        dayOffset = DayOfWeek.Monday - firstMonday.DayOfWeek;
        firstMonday = firstMonday.AddDays(dayOffset);
        
        return (monday - firstMonday).Days / 7 + 1;
    }

    #endregion
}

// Request DTOs
public record StartTimerRequest(
    long TaskId,
    string? Description,
    WorkType WorkType = WorkType.Development,
    bool IsBillable = true,
    decimal? HourlyRate = null
);

public record StopTimerRequest(string? Description);

public record LogTimeRequest(
    long TaskId,
    DateTime StartTime,
    DateTime EndTime,
    string? Description,
    WorkType WorkType = WorkType.Development,
    bool IsBillable = true,
    decimal? HourlyRate = null
);

public record EditTimeEntryRequest(
    DateTime? StartTime,
    DateTime? EndTime,
    string? Description,
    WorkType? WorkType,
    bool? IsBillable
);
