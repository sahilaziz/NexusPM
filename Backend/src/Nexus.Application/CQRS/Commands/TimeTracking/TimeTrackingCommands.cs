using MediatR;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;

namespace Nexus.Application.CQRS.Commands.TimeTracking;

// ============== COMMANDS ==============

/// <summary>
/// Timer başlat
/// </summary>
public record StartTimerCommand(
    long TaskId,
    long UserId,
    string? Description,
    WorkType WorkType,
    bool IsBillable,
    decimal? HourlyRate,
    string StartedBy = "system"
) : IRequest<TimeEntryResponse>;

/// <summary>
/// Timer dayandır
/// </summary>
public record StopTimerCommand(
    long UserId,
    string? Description = null
) : IRequest<TimeEntryResponse?>;

/// <summary>
/// Manuel vaxt qeydi əlavə et
/// </summary>
public record LogTimeCommand(
    long TaskId,
    long UserId,
    DateTime StartTime,
    DateTime EndTime,
    string? Description,
    WorkType WorkType,
    bool IsBillable,
    decimal? HourlyRate,
    string CreatedBy = "system"
) : IRequest<TimeEntryResponse>;

/// <summary>
/// Vaxt qeydini redaktə et
/// </summary>
public record EditTimeEntryCommand(
    long TimeEntryId,
    DateTime? NewStartTime,
    DateTime? NewEndTime,
    string? Description,
    WorkType? WorkType,
    bool? IsBillable,
    string EditedBy = "system"
) : IRequest<TimeEntryResponse>;

/// <summary>
/// Vaxt qeydini sil
/// </summary>
public record DeleteTimeEntryCommand(
    long TimeEntryId,
    string DeletedBy = "system"
) : IRequest<Unit>;

/// <summary>
/// Vaxt qeydini təsdiqlə
/// </summary>
public record ApproveTimeEntryCommand(
    long TimeEntryId,
    string ApprovedBy
) : IRequest<Unit>;

// ============== RESPONSES ==============

public record TimeEntryResponse(
    long TimeEntryId,
    long TaskId,
    string TaskTitle,
    long UserId,
    DateTime StartTime,
    DateTime? EndTime,
    int? DurationMinutes,
    string FormattedDuration,
    string? Description,
    WorkType WorkType,
    bool IsBillable,
    decimal? CalculatedAmount,
    bool IsRunning,
    bool IsApproved
);

// ============== HANDLERS ==============

public class StartTimerHandler : IRequestHandler<StartTimerCommand, TimeEntryResponse>
{
    private readonly ITimeEntryRepository _repository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StartTimerHandler(
        ITimeEntryRepository repository,
        ITaskRepository taskRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TimeEntryResponse> Handle(StartTimerCommand request, CancellationToken cancellationToken)
    {
        // Check if user already has running timer
        var hasRunning = await _repository.HasRunningTimerAsync(request.UserId, cancellationToken);
        if (hasRunning)
        {
            throw new InvalidOperationException("Artıq aktiv bir timer var. Əvvəlcə onu dayandırın.");
        }

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task == null)
        {
            throw new KeyNotFoundException($"Tapşırıq tapılmadı: {request.TaskId}");
        }

        var timeEntry = new TimeEntry
        {
            TaskId = request.TaskId,
            UserId = request.UserId,
            StartTime = DateTime.UtcNow,
            Description = request.Description,
            WorkType = request.WorkType,
            IsBillable = request.IsBillable,
            HourlyRate = request.HourlyRate,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(timeEntry, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(timeEntry, task.TaskTitle);
    }
}

public class StopTimerHandler : IRequestHandler<StopTimerCommand, TimeEntryResponse?>
{
    private readonly ITimeEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public StopTimerHandler(ITimeEntryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TimeEntryResponse?> Handle(StopTimerCommand request, CancellationToken cancellationToken)
    {
        var runningTimer = await _repository.GetRunningTimerAsync(request.UserId, cancellationToken);
        
        if (runningTimer == null)
        {
            return null;
        }

        runningTimer.EndTime = DateTime.UtcNow;
        
        if (!string.IsNullOrEmpty(request.Description))
        {
            runningTimer.Description = request.Description;
        }

        runningTimer.CalculateDuration();
        
        await _repository.UpdateAsync(runningTimer, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(runningTimer, runningTimer.Task?.TaskTitle ?? "Unknown");
    }
}

public class LogTimeHandler : IRequestHandler<LogTimeCommand, TimeEntryResponse>
{
    private readonly ITimeEntryRepository _repository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LogTimeHandler(
        ITimeEntryRepository repository,
        ITaskRepository taskRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TimeEntryResponse> Handle(LogTimeCommand request, CancellationToken cancellationToken)
    {
        if (request.EndTime <= request.StartTime)
        {
            throw new ArgumentException("Bitmə vaxtı başlama vaxtından sonra olmalıdır");
        }

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task == null)
        {
            throw new KeyNotFoundException($"Tapşırıq tapılmadı: {request.TaskId}");
        }

        var timeEntry = new TimeEntry
        {
            TaskId = request.TaskId,
            UserId = request.UserId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Description = request.Description,
            WorkType = request.WorkType,
            IsBillable = request.IsBillable,
            HourlyRate = request.HourlyRate,
            CreatedAt = DateTime.UtcNow
        };

        timeEntry.CalculateDuration();

        await _repository.AddAsync(timeEntry, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(timeEntry, task.TaskTitle);
    }
}

public class EditTimeEntryHandler : IRequestHandler<EditTimeEntryCommand, TimeEntryResponse>
{
    private readonly ITimeEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public EditTimeEntryHandler(ITimeEntryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TimeEntryResponse> Handle(EditTimeEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.TimeEntryId, cancellationToken);
        
        if (entry == null)
        {
            throw new KeyNotFoundException($"Vaxt qeydi tapılmadı: {request.TimeEntryId}");
        }

        if (!entry.IsEdited)
        {
            entry.OriginalDurationMinutes = entry.DurationMinutes;
        }

        if (request.NewStartTime.HasValue)
            entry.StartTime = request.NewStartTime.Value;

        if (request.NewEndTime.HasValue)
            entry.EndTime = request.NewEndTime.Value;

        if (!string.IsNullOrEmpty(request.Description))
            entry.Description = request.Description;

        if (request.WorkType.HasValue)
            entry.WorkType = request.WorkType.Value;

        if (request.IsBillable.HasValue)
            entry.IsBillable = request.IsBillable.Value;

        entry.IsEdited = true;
        entry.ModifiedAt = DateTime.UtcNow;

        // Recalculate duration and amount
        if (entry.EndTime.HasValue)
        {
            entry.CalculateDuration();
        }

        await _repository.UpdateAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(entry, entry.Task?.TaskTitle ?? "Unknown");
    }
}

public class DeleteTimeEntryHandler : IRequestHandler<DeleteTimeEntryCommand, Unit>
{
    private readonly ITimeEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTimeEntryHandler(ITimeEntryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteTimeEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.TimeEntryId, cancellationToken);
        
        if (entry == null)
        {
            throw new KeyNotFoundException($"Vaxt qeydi tapılmadı: {request.TimeEntryId}");
        }

        if (entry.IsApproved)
        {
            throw new InvalidOperationException("Təsdiqlənmiş vaxt qeydi silinə bilməz");
        }

        await _repository.DeleteAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return Unit.Value;
    }
}

public class ApproveTimeEntryHandler : IRequestHandler<ApproveTimeEntryCommand, Unit>
{
    private readonly ITimeEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveTimeEntryHandler(ITimeEntryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ApproveTimeEntryCommand request, CancellationToken cancellationToken)
    {
        await _repository.ApproveAsync(request.TimeEntryId, request.ApprovedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync();
        return Unit.Value;
    }

    private static TimeEntryResponse MapToResponse(TimeEntry entry, string taskTitle)
    {
        return new TimeEntryResponse(
            entry.TimeEntryId,
            entry.TaskId,
            taskTitle,
            entry.UserId,
            entry.StartTime,
            entry.EndTime,
            entry.DurationMinutes,
            entry.GetFormattedDuration(),
            entry.Description,
            entry.WorkType,
            entry.IsBillable,
            entry.CalculatedAmount,
            !entry.EndTime.HasValue,
            entry.IsApproved
        );
    }
}
