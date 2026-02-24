using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Application.Interfaces.Services;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Services;

/// <summary>
/// SMTP Email Service implementation
/// </summary>
public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IEmailLogRepository _emailLogRepository;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<SmtpSettings> settings,
        IEmailTemplateRepository templateRepository,
        IEmailLogRepository emailLogRepository,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _templateRepository = templateRepository;
        _emailLogRepository = emailLogRepository;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = CreateSmtpClient();
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            if (!string.IsNullOrEmpty(plainTextBody))
            {
                message.AlternateViews.Add(
                    AlternateView.CreateAlternateViewFromString(
                        plainTextBody, 
                        null, 
                        "text/plain"));
            }

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendTemplatedEmailAsync(
        string toEmail,
        string templateCode,
        Dictionary<string, string> templateData,
        string? languageCode = "az",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (subject, body) = await RenderTemplateAsync(
                templateCode, 
                templateData, 
                languageCode, 
                cancellationToken);

            var success = await SendEmailAsync(toEmail, subject, body, null, cancellationToken);

            // Log the email
            await LogEmailAsync(toEmail, subject, templateCode, success, cancellationToken);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send templated email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<BatchEmailResult> SendBatchEmailsAsync(
        IEnumerable<string> toEmails,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var result = new BatchEmailResult();
        var emails = toEmails.ToList();
        result.TotalCount = emails.Count;

        foreach (var email in emails)
        {
            var success = await SendEmailAsync(email, subject, htmlBody, null, cancellationToken);
            
            if (success)
                result.SuccessCount++;
            else
            {
                result.FailedCount++;
                result.FailedEmails.Add(email);
            }

            // Small delay to avoid overwhelming the SMTP server
            await Task.Delay(100, cancellationToken);
        }

        return result;
    }

    public async Task<(string Subject, string Body)> RenderTemplateAsync(
        string templateCode,
        Dictionary<string, string> templateData,
        string? languageCode = "az",
        CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByCodeAsync(
            templateCode, 
            languageCode ?? "az", 
            cancellationToken);

        if (template == null)
        {
            throw new KeyNotFoundException($"Email template not found: {templateCode}");
        }

        var subject = ReplaceTemplateVariables(template.SubjectTemplate, templateData);
        var body = ReplaceTemplateVariables(template.BodyTemplate, templateData);

        // Add tracking pixel if enabled
        if (_settings.EnableTracking)
        {
            var trackingId = Guid.NewGuid().ToString("N");
            var trackingPixel = $"<img src=\"{_settings.TrackingBaseUrl}/api/email/track/{trackingId}\" width=\"1\" height=\"1\" />";
            body = body.Replace("</body>", $"{trackingPixel}</body>");
        }

        // Wrap in layout if provided
        if (!string.IsNullOrEmpty(_settings.EmailLayoutTemplate))
        {
            body = _settings.EmailLayoutTemplate.Replace("{{Body}}", body);
        }

        return (subject, body);
    }

    public async Task QueueEmailAsync(
        string toEmail,
        string templateCode,
        Dictionary<string, string> templateData,
        EmailType type,
        string? relatedEntityType = null,
        long? relatedEntityId = null,
        CancellationToken cancellationToken = default)
    {
        var emailLog = new EmailLog
        {
            ToEmail = toEmail,
            FromEmail = _settings.FromEmail,
            Subject = $"[QUEUED] {templateCode}", // Will be updated when rendered
            TemplateCode = templateCode,
            Type = type,
            Status = EmailStatus.Queued,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            CreatedAt = DateTime.UtcNow
        };

        await _emailLogRepository.AddAsync(emailLog, cancellationToken);
        await _emailLogRepository.SaveChangesAsync(cancellationToken);

        // TODO: Add to background job queue for processing
        _logger.LogInformation(
            "Email queued for {Email} with template {Template}", 
            toEmail, 
            templateCode);
    }

    #region Private Helpers

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrEmpty(_settings.Username))
        {
            client.Credentials = new NetworkCredential(
                _settings.Username, 
                _settings.Password);
        }

        return client;
    }

    private static string ReplaceTemplateVariables(
        string template, 
        Dictionary<string, string> variables)
    {
        var result = template;
        
        foreach (var variable in variables)
        {
            result = result.Replace($"{{{{{variable.Key}}}}}", variable.Value);
        }

        return result;
    }

    private async Task LogEmailAsync(
        string toEmail,
        string subject,
        string? templateCode,
        bool success,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = new EmailLog
            {
                ToEmail = toEmail,
                FromEmail = _settings.FromEmail,
                Subject = subject,
                TemplateCode = templateCode,
                Status = success ? EmailStatus.Sent : EmailStatus.Failed,
                SentAt = success ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            };

            await _emailLogRepository.AddAsync(log, cancellationToken);
            await _emailLogRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log email");
        }
    }

    #endregion
}

/// <summary>
/// SMTP Settings
/// </summary>
public class SmtpSettings
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FromEmail { get; set; } = null!;
    public string FromName { get; set; } = "Nexus PM";
    public bool EnableSsl { get; set; } = true;
    public bool EnableTracking { get; set; } = false;
    public string? TrackingBaseUrl { get; set; }
    public string? EmailLayoutTemplate { get; set; }
}
