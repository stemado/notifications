# Email Provider Quick Start

## Overview

The NotificationService supports two email providers via a factory pattern:

| Provider | Use Case | Config Value |
|----------|----------|--------------|
| **Smtp** | Development (Papercut) or future production SMTP | `"Smtp"` |
| **MicrosoftGraph** | Production (delegated Outlook account) | `"MicrosoftGraph"` |

## Quick Setup

### Development (Papercut)

1. **Install Papercut SMTP:**
   ```powershell
   winget install ChangemakerStudios.Papercut-SMTP
   ```

2. **Launch Papercut** from Start Menu (runs on `localhost:25`)

3. **Configuration is automatic** - `appsettings.Development.json` already configured:
   ```json
   "Email": {
     "Provider": "Smtp",
     "Smtp": {
       "Host": "localhost",
       "Port": 25,
       "IsLocalDevServer": true
     }
   }
   ```

4. **Test it** - Send a test email from the template editor. It appears in Papercut UI.

### Production (Microsoft Graph)

No changes needed - `appsettings.json` defaults to Microsoft Graph:
```json
"Email": {
  "Provider": "MicrosoftGraph",
  "MicrosoftGraph": {
    "SendFromAddress": "dexchange@antfarmservices.com"
  }
}
```

Graph API credentials are loaded from the shared file server config automatically.

## Configuration Reference

```json
"Email": {
  // Provider: "Smtp" or "MicrosoftGraph"
  "Provider": "Smtp",

  "Smtp": {
    "Host": "localhost",           // SMTP server host
    "Port": 25,                    // SMTP port (25 for Papercut, 587 for TLS)
    "Username": "",                // Leave empty for Papercut
    "Password": "",                // Leave empty for Papercut
    "EnableSsl": false,            // true for production SMTP
    "FromEmail": "noreply@example.com",
    "FromName": "Notification Service",
    "IsLocalDevServer": true       // true = skip auth (Papercut), false = require credentials
  },

  "MicrosoftGraph": {
    "SendFromAddress": "dexchange@antfarmservices.com"
  }
}
```

## Switching Providers

Just change `"Provider"` value:

```json
// Use Papercut for local testing
"Provider": "Smtp"

// Use Outlook delegated account
"Provider": "MicrosoftGraph"
```

## Error Messages

The system now returns specific error messages:

| Error | Meaning |
|-------|---------|
| `SMTP Host is not configured` | Missing `Smtp.Host` setting |
| `From email address is not configured` | Missing `Smtp.FromEmail` setting |
| `SMTP credentials are required for production servers` | `IsLocalDevServer: false` but no credentials |
| `SMTP error: [details] (Status: XYZ)` | SMTP server rejected the email |
| `Graph API send failed: [details]` | Microsoft Graph API error |

## Code Usage

```csharp
// Inject IEmailService
public class MyService
{
    private readonly IEmailService _emailService;

    public MyService(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task SendNotification()
    {
        // Check current provider
        var provider = _emailService.CurrentProvider; // Smtp or MicrosoftGraph

        // Send email with detailed result
        var result = await _emailService.SendEmailAsync(
            recipients: new[] { "user@example.com" },
            subject: "Hello",
            htmlBody: "<p>Hello World</p>");

        if (result.Success)
        {
            Console.WriteLine($"Sent via {result.Provider}, MessageId: {result.MessageId}");
        }
        else
        {
            Console.WriteLine($"Failed: {result.ErrorMessage}");
        }
    }
}
```

## Files

| File | Purpose |
|------|---------|
| `EmailProviderOptions.cs` | Configuration models |
| `EmailSendResult.cs` | Rich result type with error details |
| `EmailServiceFactory.cs` | Factory pattern for provider selection |
| `SmtpEmailService.cs` | SMTP provider (Papercut/production SMTP) |
| `GraphEmailService.cs` | Microsoft Graph provider |

## Future: Production SMTP

When you get SMTP access in ~6 months, just update config:

```json
"Email": {
  "Provider": "Smtp",
  "Smtp": {
    "Host": "smtp.yourprovider.com",
    "Port": 587,
    "Username": "your-username",
    "Password": "your-password",
    "EnableSsl": true,
    "FromEmail": "noreply@antfarmservices.com",
    "FromName": "Notification Service",
    "IsLocalDevServer": false
  }
}
```
