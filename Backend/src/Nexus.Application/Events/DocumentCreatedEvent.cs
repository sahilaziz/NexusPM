namespace Nexus.Application.Events;

/// <summary>
/// Domain Event - Document yaradıldıqda trigger olur
/// Event-Driven Architecture üçün
/// </summary>
public class DocumentCreatedEvent : IEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public long DocumentId { get; set; }
    public string Title { get; set; } = null!;
    public string OrganizationCode { get; set; } = null!;
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Event Handler - Notification göndərmək üçün
/// </summary>
public class DocumentCreatedNotificationHandler : IEventHandler<DocumentCreatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<DocumentCreatedNotificationHandler> _logger;

    public DocumentCreatedNotificationHandler(
        INotificationService notificationService,
        ILogger<DocumentCreatedNotificationHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(DocumentCreatedEvent @event)
    {
        _logger.LogInformation(
            "Sending notifications for document {DocumentId}", 
            @event.DocumentId);

        // Real-time notification
        await _notificationService.NotifyOrganizationAsync(
            @event.OrganizationCode,
            new NotificationMessage
            {
                Type = NotificationType.DocumentUploaded,
                Title = "Yeni sənəd",
                Message = $"{@event.Title} sənədi yükləndi",
                EntityId = @event.DocumentId,
                EntityType = "Document"
            });
    }
}

/// <summary>
/// Event Handler - Search index update üçün
/// </summary>
public class DocumentCreatedSearchIndexHandler : IEventHandler<DocumentCreatedEvent>
{
    private readonly ISearchService _searchService;

    public DocumentCreatedSearchIndexHandler(ISearchService searchService)
    {
        _searchService = searchService;
    }

    public async Task Handle(DocumentCreatedEvent @event)
    {
        // Azure Cognitive Search index-inə əlavə et
        await _searchService.IndexDocumentAsync(new SearchDocument
        {
            Id = @event.DocumentId.ToString(),
            Title = @event.Title,
            OrganizationCode = @event.OrganizationCode,
            CreatedAt = @event.CreatedAt
        });
    }
}

/// <summary>
/// Event Handler - Audit log üçün
/// </summary>
public class DocumentCreatedAuditHandler : IEventHandler<DocumentCreatedEvent>
{
    private readonly IAuditRepository _auditRepository;

    public DocumentCreatedAuditHandler(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task Handle(DocumentCreatedEvent @event)
    {
        await _auditRepository.LogAsync(new AuditLog
        {
            EventType = "DocumentCreated",
            EntityId = @event.DocumentId,
            EntityType = "Document",
            UserId = @event.CreatedBy,
            OrganizationCode = @event.OrganizationCode,
            Timestamp = @event.CreatedAt,
            Details = $"Document '{@event.Title}' created"
        });
    }
}

// Interfaces
public interface IEvent
{
    Guid EventId { get; set; }
}

public interface IEventHandler<T> where T : IEvent
{
    Task Handle(T @event);
}

public interface IEventBus
{
    Task PublishAsync<T>(T @event) where T : IEvent;
}
