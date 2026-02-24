using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Services;

/// <summary>
/// Email göndərmə servisi interface
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Tək email göndər
    /// </summary>
    Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Şablon istifadə edərək email göndər
    /// </summary>
    Task<bool> SendTemplatedEmailAsync(
        string toEmail,
        string templateCode,
        Dictionary<string, string> templateData,
        string? languageCode = "az",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch email göndər
    /// </summary>
    Task<BatchEmailResult> SendBatchEmailsAsync(
        IEnumerable<string> toEmails,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Email şablonunu render et
    /// </summary>
    Task<(string Subject, string Body)> RenderTemplateAsync(
        string templateCode,
        Dictionary<string, string> templateData,
        string? languageCode = "az",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Email göndərilməsini növbəyə əlavə et (background processing üçün)
    /// </summary>
    Task QueueEmailAsync(
        string toEmail,
        string templateCode,
        Dictionary<string, string> templateData,
        EmailType type,
        string? relatedEntityType = null,
        long? relatedEntityId = null,
        CancellationToken cancellationToken = default);
}

public class BatchEmailResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> FailedEmails { get; set; } = new();
}
