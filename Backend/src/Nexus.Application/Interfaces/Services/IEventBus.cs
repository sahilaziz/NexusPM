namespace Nexus.Application.Interfaces.Services;

/// <summary>
/// Event Bus - Asinxron message queue
/// Azure Service Bus implementasiyası
/// </summary>
public interface IEventBus
{
    Task PublishAsync<T>(T @event) where T : IEvent;
    Task SubscribeAsync<T, THandler>() 
        where T : IEvent 
        where THandler : IEventHandler<T>;
}

public interface IEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}

public interface IEventHandler<in T> where T : IEvent
{
    Task HandleAsync(T @event);
}

/// <summary>
/// Base Event class
/// </summary>
public abstract class BaseEvent : IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public abstract string EventType { get; }
}
