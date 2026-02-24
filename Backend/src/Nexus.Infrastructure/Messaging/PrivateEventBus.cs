using System.Text.Json;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Application.Interfaces.Services;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Messaging;

/// <summary>
/// Tam Şəxsi Event Bus - SQL Server-based
/// Xarici dependency yoxdur (Azure, RabbitMQ və s.)
/// </summary>
public class PrivateEventBus : IEventBus, IDisposable
{
    private readonly IMessageQueueRepository _queueRepository;
    private readonly ILogger<PrivateEventBus> _logger;
    private readonly Dictionary<string, List<Func<IEvent, Task>>> _handlers = new();
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public PrivateEventBus(
        IMessageQueueRepository queueRepository,
        ILogger<PrivateEventBus> logger)
    {
        _queueRepository = queueRepository;
        _logger = logger;
        
        // Hər saat köhnə message-ləri təmizlə
        _cleanupTimer = new Timer(
            async _ => await CleanupAsync(), 
            null, 
            TimeSpan.FromHours(1), 
            TimeSpan.FromHours(1));
    }

    /// <summary>
    /// Event publish et - database queue-ya yaz
    /// </summary>
    public async Task PublishAsync<T>(T @event) where T : IEvent
    {
        var eventType = @event.GetType().Name;
        var queueName = GetQueueName(eventType);
        
        var message = new MessageQueue
        {
            QueueName = queueName,
            MessageType = eventType,
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            Status = MessageStatus.Pending,
            MaxRetries = 3,
            CorrelationId = @event.EventId.ToString(),
            Priority = GetEventPriority(eventType),
            ExpiresAt = DateTime.UtcNow.AddHours(24) // 24 saat valid
        };

        await _queueRepository.EnqueueAsync(message);
        
        _logger.LogInformation(
            "Event {EventType} published to queue {QueueName} with MessageId {MessageId}",
            eventType, queueName, message.MessageId);
    }

    /// <summary>
    /// Handler qeydiyyatı - in-memory subscription
    /// </summary>
    public Task SubscribeAsync<T, THandler>() 
        where T : IEvent 
        where THandler : IEventHandler<T>
    {
        var eventType = typeof(T).Name;
        
        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<Func<IEvent, Task>>();
        }

        _handlers[eventType].Add(async (evt) =>
        {
            using var scope = ServiceProviderScope.ServiceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<THandler>();
            await handler.HandleAsync((T)evt);
        });

        _logger.LogInformation("Subscribed to event {EventType}", eventType);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Queue processor - background service tərəfindən çağrılacaq
    /// </summary>
    public async Task ProcessQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var message = await _queueRepository.DequeueAsync(queueName);
                
                if (message == null)
                {
                    // Queue boşdur, gözlə
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                await ProcessMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue {QueueName}", queueName);
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Message-i işlə
    /// </summary>
    private async Task ProcessMessageAsync(MessageQueue message)
    {
        try
        {
            _logger.LogDebug(
                "Processing message {MessageId} of type {MessageType}",
                message.MessageId, message.MessageType);

            // Event tipini tap
            var eventType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == message.MessageType && typeof(IEvent).IsAssignableFrom(t));

            if (eventType == null)
            {
                throw new InvalidOperationException($"Event type {message.MessageType} not found");
            }

            // Deserialize
            var @event = JsonSerializer.Deserialize(message.Payload, eventType) as IEvent;
            if (@event == null)
            {
                throw new InvalidOperationException("Failed to deserialize event");
            }

            // Handler-ləri çağır
            if (_handlers.TryGetValue(message.MessageType, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    await handler(@event);
                }
            }

            // Uğurlu
            await _queueRepository.UpdateStatusAsync(
                message.MessageId, 
                MessageStatus.Completed);

            _logger.LogInformation(
                "Message {MessageId} processed successfully",
                message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error processing message {MessageId}", 
                message.MessageId);

            await HandleFailureAsync(message, ex);
        }
    }

    /// <summary>
    /// Uğursuzluq halını idarə et - Retry və ya Dead Letter
    /// </summary>
    private async Task HandleFailureAsync(MessageQueue message, Exception ex)
    {
        if (message.RetryCount >= message.MaxRetries)
        {
            // Bütün retry-lər bitdi - Dead Letter Queue
            await _queueRepository.MoveToDeadLetterAsync(message);
        }
        else
        {
            // Yenidən cəhd et - Exponential backoff
            var delay = TimeSpan.FromSeconds(Math.Pow(2, message.RetryCount));
            var scheduledFor = DateTime.UtcNow.Add(delay);
            
            await _queueRepository.ScheduleRetryAsync(
                message.MessageId, 
                scheduledFor, 
                ex.Message);

            _logger.LogWarning(
                "Message {MessageId} scheduled for retry #{RetryCount} in {Delay}s",
                message.MessageId, message.RetryCount + 1, delay.TotalSeconds);
        }
    }

    /// <summary>
    /// Queue adını event tipindən yarat
    /// </summary>
    private static string GetQueueName(string eventType)
    {
        // Məsələn: DocumentCreatedEvent → document-events
        return eventType.ToLowerInvariant()
            .Replace("event", "")
            .Replace("created", "")
            .Replace("updated", "")
            .Replace("deleted", "")
            + "-events";
    }

    /// <summary>
    /// Event prioritetini təyin et
    /// </summary>
    private static int GetEventPriority(string eventType)
    {
        // Email və notification-lar daha vacibdir
        if (eventType.Contains("Notification") || eventType.Contains("Email"))
            return 10;
        
        // Index update daha az vacibdir
        if (eventType.Contains("Index"))
            return 1;
        
        return 5;
    }

    /// <summary>
    /// Köhnə message-ləri təmizlə
    /// </summary>
    private async Task CleanupAsync()
    {
        try
        {
            // Stuck message-ləri reset et (15 dəqiqədan çox işlənməyən)
            await _queueRepository.ResetStuckMessagesAsync(TimeSpan.FromMinutes(15));
            
            // 7 gündən köhnə message-ləri sil
            await _queueRepository.CleanupOldMessagesAsync(TimeSpan.FromDays(7));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cleanupTimer?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Background service - Queue-ları avtomatik işlətmək üçün
/// </summary>
public class MessageQueueProcessor : BackgroundService
{
    private readonly PrivateEventBus _eventBus;
    private readonly ILogger<MessageQueueProcessor> _logger;

    public MessageQueueProcessor(
        PrivateEventBus eventBus,
        ILogger<MessageQueueProcessor> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Message Queue Processor started");

        // Bütün queue-ları paralel işlət
        var queues = new[] { "document-events", "notification-events", "email-events" };
        
        var tasks = queues.Select(q => ProcessQueueAsync(q, stoppingToken));
        
        await Task.WhenAll(tasks);
    }

    private async Task ProcessQueueAsync(string queueName, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await _eventBus.ProcessQueueAsync(queueName, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in queue {QueueName}", queueName);
                await Task.Delay(5000, token);
            }
        }
    }
}
