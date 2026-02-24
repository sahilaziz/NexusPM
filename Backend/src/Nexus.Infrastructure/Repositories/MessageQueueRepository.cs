using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Data;

namespace Nexus.Infrastructure.Repositories;

/// <summary>
/// SQL Server-based Message Queue Repository
/// Tam şəxsi implementasiya
/// </summary>
public class MessageQueueRepository : IMessageQueueRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<MessageQueueRepository> _logger;

    public MessageQueueRepository(AppDbContext context, ILogger<MessageQueueRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MessageQueue> EnqueueAsync(MessageQueue message)
    {
        _context.MessageQueues.Add(message);
        await _context.SaveChangesAsync();
        
        _logger.LogDebug(
            "Message {MessageId} enqueued to {QueueName}", 
            message.MessageId, message.QueueName);
        
        return message;
    }

    public async Task<MessageQueue?> DequeueAsync(string queueName)
    {
        // Transaction ilə lock al - concurrent processing üçün
        await using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // SKIP LOCKED - SQL Server 2016+ 
            // Digər processor-lərin bu message-i görməsinə əngəl olur
            var message = await _context.MessageQueues
                .FromSqlInterpolated($@"
                    SELECT TOP 1 * FROM MessageQueues WITH (UPDLOCK, READPAST)
                    WHERE QueueName = {queueName}
                        AND Status = {(int)MessageStatus.Pending}
                        AND (ScheduledFor IS NULL OR ScheduledFor <= GETUTCDATE())
                        AND (ExpiresAt IS NULL OR ExpiresAt > GETUTCDATE())
                    ORDER BY Priority DESC, CreatedAt ASC")
                .FirstOrDefaultAsync();

            if (message != null)
            {
                message.Status = MessageStatus.Processing;
                message.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogDebug(
                    "Message {MessageId} dequeued from {QueueName}", 
                    message.MessageId, queueName);
            }
            
            return message;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error dequeuing message from {QueueName}", queueName);
            throw;
        }
    }

    public async Task<IEnumerable<MessageQueue>> DequeueBatchAsync(string queueName, int batchSize)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var messages = await _context.MessageQueues
                .FromSqlInterpolated($@"
                    SELECT TOP ({batchSize}) * FROM MessageQueues WITH (UPDLOCK, READPAST)
                    WHERE QueueName = {queueName}
                        AND Status = {(int)MessageStatus.Pending}
                        AND (ScheduledFor IS NULL OR ScheduledFor <= GETUTCDATE())
                    ORDER BY Priority DESC, CreatedAt ASC")
                .ToListAsync();

            foreach (var message in messages)
            {
                message.Status = MessageStatus.Processing;
                message.ProcessedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger.LogDebug(
                "Batch dequeued {Count} messages from {QueueName}", 
                messages.Count, queueName);
            
            return messages;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error batch dequeuing from {QueueName}", queueName);
            throw;
        }
    }

    public async Task UpdateStatusAsync(long messageId, MessageStatus status, string? errorMessage = null)
    {
        var message = await _context.MessageQueues.FindAsync(messageId);
        if (message != null)
        {
            message.Status = status;
            if (errorMessage != null)
            {
                message.ErrorMessage = errorMessage.Length > 4000 
                    ? errorMessage[..4000] 
                    : errorMessage;
            }
            
            if (status == MessageStatus.Completed)
            {
                message.ProcessedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
        }
    }

    public async Task ScheduleRetryAsync(long messageId, DateTime scheduledFor, string errorMessage)
    {
        var message = await _context.MessageQueues.FindAsync(messageId);
        if (message != null)
        {
            message.RetryCount++;
            message.Status = MessageStatus.Pending;
            message.ScheduledFor = scheduledFor;
            message.ErrorMessage = errorMessage;
            message.ProcessedAt = null;
            
            await _context.SaveChangesAsync();
            
            _logger.LogDebug(
                "Message {MessageId} scheduled for retry #{RetryCount} at {ScheduledFor}",
                messageId, message.RetryCount, scheduledFor);
        }
    }

    public async Task MoveToDeadLetterAsync(MessageQueue message)
    {
        var deadLetter = new DeadLetterMessage
        {
            OriginalMessageId = message.MessageId,
            QueueName = message.QueueName,
            MessageType = message.MessageType,
            Payload = message.Payload,
            RetryCount = message.RetryCount,
            ErrorMessage = message.ErrorMessage,
            OrganizationCode = message.OrganizationCode
        };
        
        _context.DeadLetterMessages.Add(deadLetter);
        message.Status = MessageStatus.DeadLetter;
        
        await _context.SaveChangesAsync();
        
        _logger.LogWarning(
            "Message {MessageId} moved to dead letter queue after {RetryCount} retries",
            message.MessageId, message.RetryCount);
    }

    public async Task<int> GetPendingCountAsync(string queueName)
    {
        return await _context.MessageQueues
            .CountAsync(m => m.QueueName == queueName 
                && m.Status == MessageStatus.Pending);
    }

    public async Task ResetStuckMessagesAsync(TimeSpan timeout)
    {
        var stuckTime = DateTime.UtcNow.Subtract(timeout);
        
        var stuckMessages = await _context.MessageQueues
            .Where(m => m.Status == MessageStatus.Processing 
                && m.ProcessedAt < stuckTime)
            .ToListAsync();

        foreach (var message in stuckMessages)
        {
            message.Status = MessageStatus.Pending;
            message.ProcessedAt = null;
            _logger.LogWarning(
                "Reset stuck message {MessageId} in queue {QueueName}",
                message.MessageId, message.QueueName);
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task CleanupOldMessagesAsync(TimeSpan retentionPeriod)
    {
        var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod);
        
        var oldMessages = await _context.MessageQueues
            .Where(m => m.Status == MessageStatus.Completed 
                && m.ProcessedAt < cutoffDate)
            .ToListAsync();

        _context.MessageQueues.RemoveRange(oldMessages);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation(
            "Cleaned up {Count} old messages", oldMessages.Count);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
