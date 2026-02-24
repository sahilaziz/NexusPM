using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repositories;

/// <summary>
/// Email Template repository
/// </summary>
public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> GetByIdAsync(long templateId, CancellationToken cancellationToken = default);
    Task<EmailTemplate?> GetByCodeAsync(string templateCode, string languageCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailTemplate>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(EmailTemplate template, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmailTemplate template, CancellationToken cancellationToken = default);
    Task DeleteAsync(EmailTemplate template, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string templateCode, string languageCode, CancellationToken cancellationToken = default);
}

/// <summary>
/// Email Log repository
/// </summary>
public interface IEmailLogRepository
{
    Task<EmailLog?> GetByIdAsync(long emailLogId, CancellationToken cancellationToken = default);
    Task<EmailLog?> GetByTrackingIdAsync(string trackingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailLog>> GetByUserAsync(string email, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailLog>> GetPendingAsync(int maxItems = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailLog>> GetFailedAsync(int maxRetryCount = 3, CancellationToken cancellationToken = default);
    Task AddAsync(EmailLog emailLog, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmailLog emailLog, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<int> GetSentCountAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<int> GetFailedCountAsync(DateTime date, CancellationToken cancellationToken = default);
}

/// <summary>
/// User Email Preference repository
/// </summary>
public interface IUserEmailPreferenceRepository
{
    Task<UserEmailPreference?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(UserEmailPreference preference, CancellationToken cancellationToken = default);
    Task<bool> ShouldSendEmailAsync(long userId, EmailType emailType, CancellationToken cancellationToken = default);
}
