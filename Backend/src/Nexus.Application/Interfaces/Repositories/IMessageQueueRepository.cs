using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repositories;

/// <summary>
/// Message Queue Repository Interface
/// </summary>
public interface IMessageQueueRepository
{
    /// <summary>
    /// Yeni message əlavə et
    /// </summary>
    Task<MessageQueue> EnqueueAsync(MessageQueue message);
    
    /// <summary>
    /// Növbəti pending message-i al (lock ilə)
    /// </summary>
    Task<MessageQueue?> DequeueAsync(string queueName);
    
    /// <summary>
    /// Bir neçə message-i toplu şəkildə al
    /// </summary>
    Task<IEnumerable<MessageQueue>> DequeueBatchAsync(string queueName, int batchSize);
    
    /// <summary>
    /// Message status-unu yenilə
    /// </summary>
    Task UpdateStatusAsync(long messageId, MessageStatus status, string? errorMessage = null);
    
    /// <summary>
    /// Retry üçün schedule et
    /// </summary>
    Task ScheduleRetryAsync(long messageId, DateTime scheduledFor, string errorMessage);
    
    /// <summary>
    /// Message-i dead letter queue-ya göndər
    /// </summary>
    Task MoveToDeadLetterAsync(MessageQueue message);
    
    /// <summary>
    /// Pending message sayını al
    /// </summary>
    Task<int> GetPendingCountAsync(string queueName);
    
    /// <summary>
    /// Stuck message-ləri yenidən queue-ya qaytar
    /// </summary>
    Task ResetStuckMessagesAsync(TimeSpan timeout);
    
    /// <summary>
    /// Köhnə message-ləri təmizlə
    /// </summary>
    Task CleanupOldMessagesAsync(TimeSpan retentionPeriod);
    
    Task SaveChangesAsync();
}
