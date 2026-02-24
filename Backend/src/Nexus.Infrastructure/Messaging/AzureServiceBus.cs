using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Nexus.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Nexus.Infrastructure.Messaging;

/// <summary>
/// Azure Service Bus Event Bus implementasiyası
/// </summary>
public class AzureServiceBus : IEventBus, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<AzureServiceBus> _logger;
    private readonly Dictionary<string, ServiceBusProcessor> _processors = new();

    public AzureServiceBus(IConfiguration configuration, ILogger<AzureServiceBus> logger)
    {
        var connectionString = configuration["AzureServiceBus:ConnectionString"];
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender("nexusevents");
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event) where T : IEvent
    {
        try
        {
            var message = new ServiceBusMessage(JsonSerializer.Serialize(@event))
            {
                MessageId = @event.EventId.ToString(),
                Subject = @event.EventType
            };

            await _sender.SendMessageAsync(message);
            _logger.LogInformation("Event {EventType} published", @event.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event");
            throw;
        }
    }

    public async Task SubscribeAsync<T, THandler>() 
        where T : IEvent 
        where THandler : IEventHandler<T>
    {
        var processor = _client.CreateProcessor("nexusevents");
        _processors[typeof(T).Name] = processor;
        await processor.StartProcessingAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var processor in _processors.Values)
        {
            await processor.StopProcessingAsync();
            await processor.DisposeAsync();
        }
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}

/// <summary>
/// In-Memory Event Bus (development üçün)
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T @event) where T : IEvent
    {
        _logger.LogInformation("Event {EventType} published (in-memory)", @event.EventType);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T, THandler>() 
        where T : IEvent 
        where THandler : IEventHandler<T>
    {
        return Task.CompletedTask;
    }
}
