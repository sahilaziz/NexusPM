using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Data.SeedData;

/// <summary>
/// Default email templates for the system
/// </summary>
public static class DefaultEmailTemplates
{
    public static readonly List<EmailTemplate> Templates = new()
    {
        // Welcome Email
        new EmailTemplate
        {
            TemplateCode = "WELCOME",
            Name = "Xoş Gəlmisiniz",
            SubjectTemplate = "{{ProjectName}} - Xoş Gəlmisiniz, {{UserName}}!",
            BodyTemplate = @"<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #3B82F6; color: white; padding: 20px; text-align: center; }
        .content { background: #f9f9f9; padding: 30px; margin: 20px 0; }
        .button { display: inline-block; padding: 12px 24px; background: #3B82F6; color: white; text-decoration: none; border-radius: 4px; }
        .footer { text-align: center; color: #666; font-size: 12px; margin-top: 30px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{{ProjectName}}</h1>
        </div>
        <div class='content'>
            <h2>Salam, {{UserName}}!</h2>
            <p>{{ProjectName}} layihə idarəetmə sisteminə xoş gəlmisiniz.</p>
            <p>Hesabınız uğurla yaradıldı. Aşağıdakı düyməni istifadə edərək sistemin daxil ola bilərsiniz:</p>
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{{LoginUrl}}' class='button'>Sistemə Daxil Ol</a>
            </p>
            <p>Əgər hesab yaratmağı siz etməmisinizsə, bu email-i nəzərə almayın.</p>
        </div>
        <div class='footer'>
            <p>{{ProjectName}} - Layihə İdarəetmə Sistemi</p>
            <p>{{CurrentYear}}</p>
        </div>
    </div>
</body>
</html>",
            PlainTextTemplate = "Salam {{UserName}}, {{ProjectName}} sisteminə xoş gəlmisiniz! Login: {{LoginUrl}}",
            Type = EmailTemplateType.System,
            IsActive = true,
            IsDefault = true,
            LanguageCode = "az"
        },

        // Password Reset
        new EmailTemplate
        {
            TemplateCode = "PASSWORD_RESET",
            Name = "Şifrə Sıfırlama",
            SubjectTemplate = "{{ProjectName}} - Şifrə Sıfırlama",
            BodyTemplate = @"<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #EF4444; color: white; padding: 20px; text-align: center; }
        .content { background: #f9f9f9; padding: 30px; margin: 20px 0; }
        .button { display: inline-block; padding: 12px 24px; background: #EF4444; color: white; text-decoration: none; border-radius: 4px; }
        .warning { background: #FEF3C7; border-left: 4px solid #F59E0B; padding: 15px; margin: 20px 0; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Şifrə Sıfırlama</h1>
        </div>
        <div class='content'>
            <h2>Salam, {{UserName}}</h2>
            <p>Şifrə sıfırlama tələbi aldıq. Şifrənizi sıfırlamaq üçün aşağıdakı düyməyə klikləyin:</p>
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{{ResetUrl}}' class='button'>Şifrəni Sıfırla</a>
            </p>
            <div class='warning'>
                <strong>Diqqət:</strong> Bu link {{ExpiryHours}} saat ərzində etibarlıdır.
            </div>
            <p>Əgər bu tələbi siz etməmisinizsə, bu email-i nəzərə almayın.</p>
        </div>
    </div>
</body>
</html>",
            Type = EmailTemplateType.System,
            IsActive = true,
            IsDefault = true,
            LanguageCode = "az"
        },

        // Task Assigned
        new EmailTemplate
        {
            TemplateCode = "TASK_ASSIGNED",
            Name = "Tapşırıq Təyinatı",
            SubjectTemplate = "[{{ProjectName}}] Sizə yeni tapşırıq təyin edildi: {{TaskTitle}}",
            BodyTemplate = @"<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #3B82F6; color: white; padding: 20px; text-align: center; }
        .content { background: #f9f9f9; padding: 30px; margin: 20px 0; }
        .task-box { background: white; border-left: 4px solid #3B82F6; padding: 20px; margin: 20px 0; }
        .button { display: inline-block; padding: 12px 24px; background: #3B82F6; color: white; text-decoration: none; border-radius: 4px; }
        .priority-high { color: #EF4444; font-weight: bold; }
        .priority-medium { color: #F59E0B; font-weight: bold; }
        .priority-low { color: #10B981; font-weight: bold; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Yeni Tapşırıq</h1>
        </div>
        <div class='content'>
            <h2>Salam, {{UserName}}</h2>
            <p><strong>{{AssignedBy}}</strong> sizə yeni tapşırıq təyin etdi:</p>
            <div class='task-box'>
                <h3>{{TaskTitle}}</h3>
                <p><strong>Layihə:</strong> {{ProjectName}}</p>
                <p><strong>Prioritet:</strong> <span class='priority-{{PriorityClass}}'>{{Priority}}</span></p>
                <p><strong>Son tarix:</strong> {{DueDate}}</p>
                <p><strong>Təsvir:</strong></p>
                <p>{{TaskDescription}}</p>
            </div>
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{{TaskUrl}}' class='button'>Tapşırığı Gör</a>
            </p>
        </div>
    </div>
</body>
</html>",
            Type = EmailTemplateType.Notification,
            IsActive = true,
            IsDefault = true,
            LanguageCode = "az"
        },

        // Daily Digest
        new EmailTemplate
        {
            TemplateCode = "DAILY_DIGEST",
            Name = "Günlük Xülasə",
            SubjectTemplate = "{{ProjectName}} - Günlük Xülasə: {{Date}}",
            BodyTemplate = @"<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #10B981; color: white; padding: 20px; text-align: center; }
        .content { background: #f9f9f9; padding: 30px; margin: 20px 0; }
        .stats { display: flex; justify-content: space-around; margin: 20px 0; }
        .stat-box { text-align: center; padding: 15px; background: white; border-radius: 8px; }
        .stat-number { font-size: 24px; font-weight: bold; color: #3B82F6; }
        .task-list { background: white; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .task-item { padding: 10px; border-bottom: 1px solid #eee; }
        .overdue { color: #EF4444; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Günlük Xülasə</h1>
            <p>{{Date}}</p>
        </div>
        <div class='content'>
            <h2>Salam, {{UserName}}</h2>
            <div class='stats'>
                <div class='stat-box'>
                    <div class='stat-number'>{{TotalTasks}}</div>
                    <div>Tapşırıqlar</div>
                </div>
                <div class='stat-box'>
                    <div class='stat-number'>{{OverdueTasks}}</div>
                    <div>Gecikmiş</div>
                </div>
                <div class='stat-box'>
                    <div class='stat-number'>{{DueToday}}</div>
                    <div>Bu gün</div>
                </div>
            </div>
            <div class='task-list'>
                <h3>Gün ərzində son tarix olan tapşırıqlar:</h3>
                {{TaskList}}
            </div>
        </div>
    </div>
</body>
</html>",
            Type = EmailTemplateType.Notification,
            IsActive = true,
            IsDefault = true,
            LanguageCode = "az"
        },

        // Task Comment
        new EmailTemplate
        {
            TemplateCode = "TASK_COMMENT",
            Name = "Yeni Şərh",
            SubjectTemplate = "[{{ProjectName}}] {{CommenterName}} şərh yazdı: {{TaskTitle}}",
            BodyTemplate = @"<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #8B5CF6; color: white; padding: 20px; text-align: center; }
        .content { background: #f9f9f9; padding: 30px; margin: 20px 0; }
        .comment-box { background: white; border-left: 4px solid #8B5CF6; padding: 20px; margin: 20px 0; }
        .commenter { font-weight: bold; color: #8B5CF6; }
        .button { display: inline-block; padding: 12px 24px; background: #8B5CF6; color: white; text-decoration: none; border-radius: 4px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Yeni Şərh</h1>
        </div>
        <div class='content'>
            <h2>Salam, {{UserName}}</h2>
            <p><span class='commenter'>{{CommenterName}}</span> <strong>{{TaskTitle}}</strong> tapşırığına şərh yazdı:</p>
            <div class='comment-box'>
                <p>"{{CommentText}}"</p>
            </div>
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{{TaskUrl}}' class='button'>Şərhi Gör</a>
            </p>
        </div>
    </div>
</body>
</html>",
            Type = EmailTemplateType.Notification,
            IsActive = true,
            IsDefault = true,
            LanguageCode = "az"
        }
    };
}
