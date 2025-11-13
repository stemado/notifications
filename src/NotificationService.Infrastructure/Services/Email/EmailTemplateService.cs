using System.Text;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Services.Email;

/// <summary>
/// Service for rendering email templates for notifications
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    public string RenderNotificationHtml(Notification notification)
    {
        var severityColor = GetSeverityColor(notification.Severity);
        var severityIcon = GetSeverityIcon(notification.Severity);

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"utf-8\">");
        html.AppendLine("    <style>");
        html.AppendLine("        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
        html.AppendLine("        .container { max-width: 600px; margin: 0 auto; padding: 20px; }");
        html.AppendLine("        .header { background-color: #f4f4f4; padding: 20px; text-align: center; }");
        html.AppendLine($"        .severity-badge {{ display: inline-block; padding: 5px 15px; background-color: {severityColor}; color: white; border-radius: 3px; font-weight: bold; }}");
        html.AppendLine("        .content { background-color: white; padding: 30px; border: 1px solid #ddd; margin-top: 20px; }");
        html.AppendLine("        .title { font-size: 24px; margin-bottom: 10px; color: #333; }");
        html.AppendLine("        .message { font-size: 16px; margin-bottom: 20px; }");
        html.AppendLine("        .actions { margin-top: 20px; }");
        html.AppendLine("        .btn { display: inline-block; padding: 10px 20px; margin-right: 10px; text-decoration: none; border-radius: 3px; }");
        html.AppendLine("        .btn-primary { background-color: #007bff; color: white; }");
        html.AppendLine("        .btn-secondary { background-color: #6c757d; color: white; }");
        html.AppendLine("        .btn-danger { background-color: #dc3545; color: white; }");
        html.AppendLine("        .metadata { margin-top: 30px; padding: 15px; background-color: #f8f9fa; border-left: 3px solid #007bff; }");
        html.AppendLine("        .footer { text-align: center; margin-top: 20px; font-size: 12px; color: #666; }");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"container\">");
        html.AppendLine("        <div class=\"header\">");
        html.AppendLine($"            <h2>{severityIcon} Notification</h2>");
        html.AppendLine($"            <span class=\"severity-badge\">{notification.Severity}</span>");
        html.AppendLine("        </div>");
        html.AppendLine("        <div class=\"content\">");
        html.AppendLine($"            <h1 class=\"title\">{notification.Title}</h1>");
        html.AppendLine($"            <p class=\"message\">{notification.Message}</p>");

        // Add actions if any
        if (notification.Actions.Any())
        {
            html.AppendLine("            <div class=\"actions\">");
            foreach (var action in notification.Actions)
            {
                var btnClass = action.Variant switch
                {
                    "primary" => "btn-primary",
                    "danger" => "btn-danger",
                    _ => "btn-secondary"
                };
                html.AppendLine($"                <a href=\"{action.Target}\" class=\"btn {btnClass}\">{action.Label}</a>");
            }
            html.AppendLine("            </div>");
        }

        // Add metadata if any
        if (notification.Metadata.Any())
        {
            html.AppendLine("            <div class=\"metadata\">");
            html.AppendLine("                <strong>Additional Information:</strong>");
            html.AppendLine("                <ul>");
            foreach (var kvp in notification.Metadata)
            {
                html.AppendLine($"                    <li><strong>{kvp.Key}:</strong> {kvp.Value}</li>");
            }
            html.AppendLine("                </ul>");
            html.AppendLine("            </div>");
        }

        html.AppendLine("        </div>");
        html.AppendLine("        <div class=\"footer\">");
        html.AppendLine($"            <p>Notification created at {notification.CreatedAt:f}</p>");
        html.AppendLine("            <p>This is an automated notification. Please do not reply to this email.</p>");
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    public string RenderNotificationPlainText(Notification notification)
    {
        var text = new StringBuilder();
        text.AppendLine($"[{notification.Severity.ToString().ToUpper()}] {notification.Title}");
        text.AppendLine();
        text.AppendLine(notification.Message);
        text.AppendLine();

        if (notification.Actions.Any())
        {
            text.AppendLine("Actions:");
            foreach (var action in notification.Actions)
            {
                text.AppendLine($"  - {action.Label}: {action.Target}");
            }
            text.AppendLine();
        }

        if (notification.Metadata.Any())
        {
            text.AppendLine("Additional Information:");
            foreach (var kvp in notification.Metadata)
            {
                text.AppendLine($"  - {kvp.Key}: {kvp.Value}");
            }
            text.AppendLine();
        }

        text.AppendLine($"Created at: {notification.CreatedAt:f}");
        text.AppendLine();
        text.AppendLine("This is an automated notification.");

        return text.ToString();
    }

    public string GenerateSubject(Notification notification)
    {
        var prefix = notification.Severity switch
        {
            NotificationSeverity.Critical => "[CRITICAL]",
            NotificationSeverity.Urgent => "[URGENT]",
            NotificationSeverity.Warning => "[WARNING]",
            _ => "[INFO]"
        };

        return $"{prefix} {notification.Title}";
    }

    private string GetSeverityColor(NotificationSeverity severity)
    {
        return severity switch
        {
            NotificationSeverity.Critical => "#dc3545",
            NotificationSeverity.Urgent => "#fd7e14",
            NotificationSeverity.Warning => "#ffc107",
            NotificationSeverity.Info => "#17a2b8",
            _ => "#6c757d"
        };
    }

    private string GetSeverityIcon(NotificationSeverity severity)
    {
        return severity switch
        {
            NotificationSeverity.Critical => "🚨",
            NotificationSeverity.Urgent => "⚠️",
            NotificationSeverity.Warning => "⚡",
            NotificationSeverity.Info => "ℹ️",
            _ => "📢"
        };
    }
}
