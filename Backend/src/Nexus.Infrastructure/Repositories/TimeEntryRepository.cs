using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Data;

namespace Nexus.Infrastructure.Repositories;

public class TimeEntryRepository : ITimeEntryRepository
{
    private readonly AppDbContext _context;

    public TimeEntryRepository(AppDbContext context)
    {
        _context = context;
    }

    #region CRUD

    public async Task<TimeEntry?> GetByIdAsync(long timeEntryId, CancellationToken cancellationToken = default)
    {
        return await _context.TimeEntries
            .Include(t => t.Task)
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TimeEntryId == timeEntryId, cancellationToken);
    }

    public async Task AddAsync(TimeEntry timeEntry, CancellationToken cancellationToken = default)
    {
        await _context.TimeEntries.AddAsync(timeEntry, cancellationToken);
    }

    public Task UpdateAsync(TimeEntry timeEntry, CancellationToken cancellationToken = default)
    {
        _context.TimeEntries.Update(timeEntry);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TimeEntry timeEntry, CancellationToken cancellationToken = default)
    {
        _context.TimeEntries.Remove(timeEntry);
        return Task.CompletedTask;
    }

    #endregion

    #region Timer Operations

    public async Task<TimeEntry?> GetRunningTimerAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _context.TimeEntries
            .Include(t => t.Task)
            .FirstOrDefaultAsync(
                t => t.UserId == userId && t.EndTime == null,
                cancellationToken);
    }

    public async Task<bool> HasRunningTimerAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _context.TimeEntries
            .AnyAsync(
                t => t.UserId == userId && t.EndTime == null,
                cancellationToken);
    }

    public async Task StopRunningTimerAsync(long userId, DateTime stopTime, CancellationToken cancellationToken = default)
    {
        var runningTimer = await GetRunningTimerAsync(userId, cancellationToken);
        if (runningTimer != null)
        {
            runningTimer.EndTime = stopTime;
            runningTimer.CalculateDuration();
            _context.TimeEntries.Update(runningTimer);
        }
    }

    #endregion

    #region Queries

    public async Task<IReadOnlyList<TimeEntry>> GetByUserAsync(
        long userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TimeEntries
            .Include(t => t.Task)
            .Where(t => t.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(t => t.StartTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.StartTime <= endDate.Value);

        return await query
            .OrderByDescending(t => t.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeEntry>> GetByTaskAsync(
        long taskId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TimeEntries
            .Include(t => t.User)
            .Where(t => t.TaskId == taskId)
            .OrderByDescending(t => t.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeEntry>> GetByProjectAsync(
        long projectId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TimeEntries
            .Include(t => t.Task)
            .Include(t => t.User)
            .Where(t => t.Task.ProjectId == projectId);

        if (startDate.HasValue)
            query = query.Where(t => t.StartTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.StartTime <= endDate.Value);

        return await query
            .OrderByDescending(t => t.StartTime)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Summaries

    public async Task<DailyTimeSummary?> GetDailySummaryAsync(
        long userId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var entries = await _context.TimeEntries
            .Include(t => t.Task)
            .Where(t => t.UserId == userId &&
                       t.StartTime >= startOfDay &&
                       t.StartTime < endOfDay)
            .ToListAsync(cancellationToken);

        if (!entries.Any())
            return null;

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);

        return new DailyTimeSummary
        {
            Date = date,
            UserId = userId,
            UserName = user?.DisplayName ?? "Unknown",
            TotalMinutes = entries.Sum(e => e.DurationMinutes ?? 0),
            TotalAmount = entries.Sum(e => e.CalculatedAmount ?? 0),
            EntryCount = entries.Count,
            Entries = entries.Select(e => new TimeEntrySummary
            {
                TimeEntryId = e.TimeEntryId,
                TaskId = e.TaskId,
                TaskTitle = e.Task.TaskTitle,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                DurationMinutes = e.DurationMinutes,
                Description = e.Description,
                WorkType = e.WorkType,
                IsBillable = e.IsBillable,
                IsRunning = !e.EndTime.HasValue
            }).ToList()
        };
    }

    public async Task<WeeklyTimeSummary?> GetWeeklySummaryAsync(
        long userId,
        int year,
        int weekNumber,
        CancellationToken cancellationToken = default)
    {
        // Calculate week start (Monday)
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
        var firstMonday = jan1.AddDays(daysOffset);
        var weekStart = firstMonday.AddDays((weekNumber - 1) * 7);
        var weekEnd = weekStart.AddDays(7);

        var entries = await _context.TimeEntries
            .Where(t => t.UserId == userId &&
                       t.StartTime >= weekStart &&
                       t.StartTime < weekEnd)
            .ToListAsync(cancellationToken);

        if (!entries.Any())
            return null;

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);

        var dailyBreakdown = Enumerable.Range(0, 7)
            .Select(i => weekStart.AddDays(i))
            .Select(date => new DailyBreakdown
            {
                Date = date,
                DayOfWeek = date.DayOfWeek,
                TotalMinutes = entries
                    .Where(e => e.StartTime.Date == date)
                    .Sum(e => e.DurationMinutes ?? 0),
                EntryCount = entries.Count(e => e.StartTime.Date == date)
            })
            .ToList();

        return new WeeklyTimeSummary
        {
            Year = year,
            WeekNumber = weekNumber,
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            UserId = userId,
            UserName = user?.DisplayName ?? "Unknown",
            TotalMinutes = entries.Sum(e => e.DurationMinutes ?? 0),
            BillableAmount = entries.Where(e => e.IsBillable).Sum(e => e.CalculatedAmount ?? 0),
            DailyBreakdown = dailyBreakdown
        };
    }

    public async Task<int> GetTotalMinutesByTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        return await _context.TimeEntries
            .Where(t => t.TaskId == taskId && t.EndTime != null)
            .SumAsync(t => t.DurationMinutes ?? 0, cancellationToken);
    }

    public async Task<int> GetTotalMinutesByUserAsync(
        long userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TimeEntries
            .Where(t => t.UserId == userId && t.EndTime != null);

        if (startDate.HasValue)
            query = query.Where(t => t.StartTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.StartTime <= endDate.Value);

        return await query.SumAsync(t => t.DurationMinutes ?? 0, cancellationToken);
    }

    #endregion

    #region Statistics

    public async Task<IReadOnlyList<(WorkType Type, int Minutes)>> GetWorkTypeBreakdownAsync(
        long userId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.TimeEntries
            .Where(t => t.UserId == userId &&
                       t.StartTime >= startDate &&
                       t.StartTime <= endDate &&
                       t.EndTime != null)
            .GroupBy(t => t.WorkType)
            .Select(g => new { Type = g.Key, Minutes = g.Sum(t => t.DurationMinutes ?? 0) })
            .ToListAsync(cancellationToken)
            .ContinueWith(t => t.Result.Select(x => (x.Type, x.Minutes)).ToList() as IReadOnlyList<(WorkType, int)>);
    }

    public async Task<decimal?> GetTotalBillableAmountAsync(
        long userId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.TimeEntries
            .Where(t => t.UserId == userId &&
                       t.IsBillable &&
                       t.StartTime >= startDate &&
                       t.StartTime <= endDate)
            .SumAsync(t => t.CalculatedAmount ?? 0, cancellationToken);
    }

    #endregion

    #region Approval Workflow

    public async Task<IReadOnlyList<TimeEntry>> GetPendingApprovalAsync(
        long? projectId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TimeEntries
            .Include(t => t.Task)
            .Include(t => t.User)
            .Where(t => !t.IsApproved && t.EndTime != null);

        if (projectId.HasValue)
            query = query.Where(t => t.Task.ProjectId == projectId.Value);

        return await query
            .OrderByDescending(t => t.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task ApproveAsync(
        long timeEntryId,
        string approvedBy,
        CancellationToken cancellationToken = default)
    {
        var entry = await _context.TimeEntries.FindAsync(new object[] { timeEntryId }, cancellationToken);
        if (entry != null)
        {
            entry.IsApproved = true;
            entry.ApprovedBy = approvedBy;
            entry.ApprovedAt = DateTime.UtcNow;
            _context.TimeEntries.Update(entry);
        }
    }

    #endregion
}
