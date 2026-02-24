using MediatR;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;

namespace Nexus.Application.CQRS.Queries.TimeTracking;

// ============== QUERIES ==============

public record GetRunningTimerQuery(long UserId) : IRequest<RunningTimerDto?>;

public record GetUserTimeEntriesQuery(
    long UserId,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<IReadOnlyList<TimeEntryListDto>>;

public record GetTaskTimeEntriesQuery(long TaskId) : IRequest<IReadOnlyList<TimeEntryListDto>>;

public record GetDailySummaryQuery(
    long UserId,
    DateTime Date
) : IRequest<DailySummaryDto?>;

public record GetWeeklySummaryQuery(
    long UserId,
    int Year,
    int WeekNumber
) : IRequest<WeeklySummaryDto?>;

public record GetTimeStatisticsQuery(
    long UserId,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<TimeStatisticsDto>;

public record GetPendingApprovalQuery(long? ProjectId = null) : IRequest<IReadOnlyList<TimeEntryForApprovalDto>>;

// ============== DTOs ==============

public record RunningTimerDto(
    long TimeEntryId,
    long TaskId,
    string TaskTitle,
    DateTime StartTime,
    string CurrentDuration,
    string? Description,
    WorkType WorkType
);

public record TimeEntryListDto(
    long TimeEntryId,
    long TaskId,
    string TaskTitle,
    DateTime StartTime,
    DateTime? EndTime,
    int? DurationMinutes,
    string FormattedDuration,
    string? Description,
    WorkType WorkType,
    bool IsBillable,
    decimal? CalculatedAmount,
    bool IsApproved,
    bool IsEdited
);

public record DailySummaryDto(
    DateTime Date,
    long UserId,
    string UserName,
    int TotalMinutes,
    string FormattedTotal,
    decimal? TotalAmount,
    int EntryCount,
    IReadOnlyList<TimeEntryListDto> Entries
);

public record WeeklySummaryDto(
    int Year,
    int WeekNumber,
    DateTime WeekStart,
    DateTime WeekEnd,
    long UserId,
    string UserName,
    int TotalMinutes,
    string FormattedTotal,
    decimal? BillableAmount,
    IReadOnlyList<DailyDto> DailyBreakdown
);

public record DailyDto(
    DateTime Date,
    string DayName,
    int TotalMinutes,
    int EntryCount,
    bool IsToday
);

public record TimeStatisticsDto(
    long UserId,
    DateTime StartDate,
    DateTime EndDate,
    int TotalMinutes,
    string FormattedTotal,
    decimal? BillableAmount,
    int EntryCount,
    IReadOnlyList<WorkTypeBreakdownDto> WorkTypeBreakdown,
    IReadOnlyList<DailyTotalDto> DailyTotals
);

public record WorkTypeBreakdownDto(
    WorkType WorkType,
    string WorkTypeName,
    int Minutes,
    double Percentage
);

public record DailyTotalDto(
    DateTime Date,
    int Minutes,
    int EntryCount
);

public record TimeEntryForApprovalDto(
    long TimeEntryId,
    long TaskId,
    string TaskTitle,
    long ProjectId,
    string ProjectName,
    long UserId,
    string UserName,
    DateTime StartTime,
    DateTime EndTime,
    int DurationMinutes,
    string FormattedDuration,
    string? Description,
    WorkType WorkType,
    bool IsBillable,
    decimal? CalculatedAmount
);

// ============== HANDLERS ==============

public class GetRunningTimerHandler : IRequestHandler<GetRunningTimerQuery, RunningTimerDto?>
{
    private readonly ITimeEntryRepository _repository;

    public GetRunningTimerHandler(ITimeEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<RunningTimerDto?> Handle(GetRunningTimerQuery request, CancellationToken cancellationToken)
    {
        var timer = await _repository.GetRunningTimerAsync(request.UserId, cancellationToken);
        
        if (timer == null)
            return null;

        return new RunningTimerDto(
            timer.TimeEntryId,
            timer.TaskId,
            timer.Task?.TaskTitle ?? "Unknown",
            timer.StartTime,
            timer.GetFormattedDuration(),
            timer.Description,
            timer.WorkType
        );
    }
}

public class GetUserTimeEntriesHandler : IRequestHandler<GetUserTimeEntriesQuery, IReadOnlyList<TimeEntryListDto>>
{
    private readonly ITimeEntryRepository _repository;

    public GetUserTimeEntriesHandler(ITimeEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TimeEntryListDto>> Handle(
        GetUserTimeEntriesQuery request, 
        CancellationToken cancellationToken)
    {
        var entries = await _repository.GetByUserAsync(
            request.UserId, 
            request.StartDate, 
            request.EndDate, 
            cancellationToken);

        return entries.Select(e => new TimeEntryListDto(
            e.TimeEntryId,
            e.TaskId,
            e.Task?.TaskTitle ?? "Unknown",
            e.StartTime,
            e.EndTime,
            e.DurationMinutes,
            e.GetFormattedDuration(),
            e.Description,
            e.WorkType,
            e.IsBillable,
            e.CalculatedAmount,
            e.IsApproved,
            e.IsEdited
        )).ToList();
    }
}

public class GetTaskTimeEntriesHandler : IRequestHandler<GetTaskTimeEntriesQuery, IReadOnlyList<TimeEntryListDto>>
{
    private readonly ITimeEntryRepository _repository;

    public GetTaskTimeEntriesHandler(ITimeEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TimeEntryListDto>> Handle(
        GetTaskTimeEntriesQuery request, 
        CancellationToken cancellationToken)
    {
        var entries = await _repository.GetByTaskAsync(request.TaskId, cancellationToken);

        return entries.Select(e => new TimeEntryListDto(
            e.TimeEntryId,
            e.TaskId,
            e.Task?.TaskTitle ?? "Unknown",
            e.StartTime,
            e.EndTime,
            e.DurationMinutes,
            e.GetFormattedDuration(),
            e.Description,
            e.WorkType,
            e.IsBillable,
            e.CalculatedAmount,
            e.IsApproved,
            e.IsEdited
        )).ToList();
    }
}

public class GetDailySummaryHandler : IRequestHandler<GetDailySummaryQuery, DailySummaryDto?>
{
    private readonly ITimeEntryRepository _repository;

    public GetDailySummaryHandler(ITimeEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<DailySummaryDto?> Handle(GetDailySummaryQuery request, CancellationToken cancellationToken)
    {
        var summary = await _repository.GetDailySummaryAsync(request.UserId, request.Date, cancellationToken);
        
        if (summary == null)
            return null;

        return new DailySummaryDto(
            summary.Date,
            summary.UserId,
            summary.UserName,
            summary.TotalMinutes,
            FormatDuration(summary.TotalMinutes),
            summary.TotalAmount,
            summary.EntryCount,
            summary.Entries.Select(e => new TimeEntryListDto(
                e.TimeEntryId,
                e.TaskId,
                e.TaskTitle,
                e.StartTime,
                e.EndTime,
                e.DurationMinutes,
                FormatDuration(e.DurationMinutes ?? 0),
                e.Description,
                e.WorkType,
                e.IsBillable,
                null,
                false,
                false
            )).ToList()
        );
    }

    private static string FormatDuration(int minutes)
    {
        var ts = TimeSpan.FromMinutes(minutes);
        return ts.TotalHours >= 1 ? $"{ts.Hours}h {ts.Minutes}m" : $"{ts.Minutes}m";
    }
}

public class GetWeeklySummaryHandler : IRequestHandler<GetWeeklySummaryQuery, WeeklySummaryDto?>
{
    private readonly ITimeEntryRepository _repository;

    public GetWeeklySummaryHandler(ITimeEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<WeeklySummaryDto?> Handle(GetWeeklySummaryQuery request, CancellationToken cancellationToken)
    {
        var summary = await _repository.GetWeeklySummaryAsync(
            request.UserId, 
            request.Year, 
            request.WeekNumber, 
            cancellationToken);
        
        if (summary == null)
            return null;

        return new WeeklySummaryDto(
            summary.Year,
            summary.WeekNumber,
            summary.WeekStart,
            summary.WeekEnd,
            summary.UserId,
            summary.UserName,
            summary.TotalMinutes,
            FormatDuration(summary.TotalMinutes),
            summary.BillableAmount,
            summary.DailyBreakdown.Select(d => new DailyDto(
                d.Date,
                d.Date.ToString("ddd"),
                d.TotalMinutes,
                d.EntryCount,
                d.Date.Date == DateTime.Today
            )).ToList()
        );
    }

    private static string FormatDuration(int minutes)
    {
        var ts = TimeSpan.FromMinutes(minutes);
        return ts.TotalHours >= 1 ? $"{ts.Hours}h {ts.Minutes}m" : $"{ts.Minutes}m";
    }
}

public class GetTimeStatisticsHandler : IRequestHandler<GetTimeStatisticsQuery, TimeStatisticsDto>
{
    private readonly ITimeEntryRepository _repository;

    public GetTimeStatisticsHandler(ITimeEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<TimeStatisticsDto> Handle(GetTimeStatisticsQuery request, CancellationToken cancellationToken)
    {
        var entries = await _repository.GetByUserAsync(
            request.UserId, 
            request.StartDate, 
            request.EndDate, 
            cancellationToken);

        var totalMinutes = entries.Sum(e => e.DurationMinutes ?? 0);
        var totalAmount = entries.Where(e => e.IsBillable).Sum(e => e.CalculatedAmount ?? 0);
        
        var workTypeBreakdown = entries
            .GroupBy(e => e.WorkType)
            .Select(g => new WorkTypeBreakdownDto(
                g.Key,
                g.Key.ToString(),
                g.Sum(e => e.DurationMinutes ?? 0),
                totalMinutes > 0 ? (g.Sum(e => e.DurationMinutes ?? 0) * 100.0 / totalMinutes) : 0
            ))
            .OrderByDescending(x => x.Minutes)
            .ToList();

        var dailyTotals = entries
            .GroupBy(e => e.StartTime.Date)
            .Select(g => new DailyTotalDto(
                g.Key,
                g.Sum(e => e.DurationMinutes ?? 0),
                g.Count()
            ))
            .OrderBy(x => x.Date)
            .ToList();

        return new TimeStatisticsDto(
            request.UserId,
            request.StartDate,
            request.EndDate,
            totalMinutes,
            FormatDuration(totalMinutes),
            totalAmount,
            entries.Count,
            workTypeBreakdown,
            dailyTotals
        );
    }

    private static string FormatDuration(int minutes)
    {
        var ts = TimeSpan.FromMinutes(minutes);
        return ts.TotalHours >= 1 ? $"{ts.Hours}h {ts.Minutes}m" : $"{ts.Minutes}m";
    }
}

public class GetPendingApprovalHandler : IRequestHandler<GetPendingApprovalQuery, IReadOnlyList<TimeEntryForApprovalDto>>
{
    private readonly ITimeEntryRepository _repository;

    public GetPendingApprovalHandler(ITimeEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TimeEntryForApprovalDto>> Handle(
        GetPendingApprovalQuery request, 
        CancellationToken cancellationToken)
    {
        var entries = await _repository.GetPendingApprovalAsync(request.ProjectId, cancellationToken);

        return entries.Select(e => new TimeEntryForApprovalDto(
            e.TimeEntryId,
            e.TaskId,
            e.Task?.TaskTitle ?? "Unknown",
            e.Task?.ProjectId ?? 0,
            e.Task?.Project?.ProjectName ?? "Unknown",
            e.UserId,
            e.User?.DisplayName ?? "Unknown",
            e.StartTime,
            e.EndTime ?? DateTime.MinValue,
            e.DurationMinutes ?? 0,
            FormatDuration(e.DurationMinutes ?? 0),
            e.Description,
            e.WorkType,
            e.IsBillable,
            e.CalculatedAmount
        )).ToList();
    }

    private static string FormatDuration(int minutes)
    {
        var ts = TimeSpan.FromMinutes(minutes);
        return ts.TotalHours >= 1 ? $"{ts.Hours}h {ts.Minutes}m" : $"{ts.Minutes}m";
    }
}
