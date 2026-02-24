using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repositories;

/// <summary>
/// Time Entry repository interface
/// </summary>
public interface ITimeEntryRepository
{
    // CRUD
    Task<TimeEntry?> GetByIdAsync(long timeEntryId, CancellationToken cancellationToken = default);
    Task AddAsync(TimeEntry timeEntry, CancellationToken cancellationToken = default);
    Task UpdateAsync(TimeEntry timeEntry, CancellationToken cancellationToken = default);
    Task DeleteAsync(TimeEntry timeEntry, CancellationToken cancellationToken = default);
    
    // Timer operations
    Task<TimeEntry?> GetRunningTimerAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> HasRunningTimerAsync(long userId, CancellationToken cancellationToken = default);
    Task StopRunningTimerAsync(long userId, DateTime stopTime, CancellationToken cancellationToken = default);
    
    // Queries
    Task<IReadOnlyList<TimeEntry>> GetByUserAsync(
        long userId, 
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<TimeEntry>> GetByTaskAsync(
        long taskId, 
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<TimeEntry>> GetByProjectAsync(
        long projectId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
    
    // Summaries
    Task<DailyTimeSummary?> GetDailySummaryAsync(
        long userId, 
        DateTime date, 
        CancellationToken cancellationToken = default);
    
    Task<WeeklyTimeSummary?> GetWeeklySummaryAsync(
        long userId, 
        int year, 
        int weekNumber, 
        CancellationToken cancellationToken = default);
    
    Task<int> GetTotalMinutesByTaskAsync(long taskId, CancellationToken cancellationToken = default);
    Task<int> GetTotalMinutesByUserAsync(long userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    
    // Statistics
    Task<IReadOnlyList<(WorkType Type, int Minutes)>> GetWorkTypeBreakdownAsync(
        long userId, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);
    
    Task<decimal?> GetTotalBillableAmountAsync(
        long userId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
    
    // Approval workflow
    Task<IReadOnlyList<TimeEntry>> GetPendingApprovalAsync(
        long? projectId = null,
        CancellationToken cancellationToken = default);
    
    Task ApproveAsync(
        long timeEntryId,
        string approvedBy,
        CancellationToken cancellationToken = default);
}
