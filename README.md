# Notification Service

A production-ready, multi-channel notification service built with .NET 8, PostgreSQL, SignalR, and support for Email, Microsoft Teams, and SMS delivery.

## Overview

This notification service provides a complete solution for managing and delivering notifications across multiple channels with real-time delivery, user preferences, subscription management, and comprehensive tracking.

**Status**: ✅ **All Phases Complete** - Production Ready

## Features

### Core Capabilities

- **Multi-Channel Delivery**: SignalR (real-time), Email (SMTP), Microsoft Teams (Adaptive Cards), SMS (Twilio)
- **Real-Time Notifications**: WebSocket-based instant delivery via SignalR
- **User Preferences**: Per-channel configuration with severity-based filtering
- **Subscription Management**: Subscribe to specific clients, sagas, or wildcard subscriptions
- **Delivery Tracking**: Complete audit trail of all notification deliveries
- **Smart Deduplication**: GroupKey-based notification merging to prevent duplicates
- **Severity-Based Behavior**: Automatic repeating for critical/urgent notifications
- **Background Jobs**: Automated repeat, cleanup, and backup polling
- **JWT Authentication**: Secure API access with bearer token authentication
- **Multi-Tenancy Ready**: Built-in support for tenant isolation
- **Event Sourcing Ready**: Links notifications to domain events

### Notification Features

- **4 Severity Levels**: Info, Warning, Urgent, Critical
- **Acknowledgment Tracking**: Required acknowledgment for urgent/critical notifications
- **Auto-Expiration**: Configurable expiration for non-critical notifications
- **Notification Actions**: Customizable action buttons (navigate, API call, dismiss)
- **Metadata Support**: Extensible JSON metadata for additional context
- **Repeating Notifications**: Automatic repeat intervals for critical alerts
- **Snooze Functionality**: Temporary dismissal with automatic re-notification

### Channels

| Channel | Status | Use Case | Default |
|---------|--------|----------|---------|
| **SignalR** | ✅ Active | Real-time web notifications | Enabled (All severities) |
| **Email** | ✅ Active | Asynchronous notifications with HTML templates | Enabled (Warning+) |
| **Teams** | ✅ Active | Team collaboration with Adaptive Cards | Disabled (Urgent+) |
| **SMS** | ✅ Active | Critical alerts via Twilio | Disabled (Critical only) |

## Architecture

```
notifications/
├── src/
│   ├── NotificationService.Api/           # ASP.NET Core Web API
│   │   ├── Controllers/                   # REST API endpoints
│   │   ├── EventHandlers/                 # Domain event handlers
│   │   ├── Hubs/                         # SignalR hubs
│   │   ├── Jobs/                         # Hangfire background jobs
│   │   └── Authentication/               # JWT configuration
│   ├── NotificationService.Domain/        # Domain models & DTOs
│   │   ├── Models/                       # Core entities
│   │   ├── Enums/                        # Enumerations
│   │   └── DTOs/                         # Data transfer objects
│   └── NotificationService.Infrastructure/ # Data & Services
│       ├── Data/                         # EF Core DbContext
│       ├── Repositories/                 # Data access
│       └── Services/                     # Business logic & channels
├── database/
│   └── migrations/                        # SQL migration scripts
├── tests/
│   └── NotificationService.Tests/         # Unit & integration tests
└── docs/
    ├── IMPLEMENTATION.md                  # Phase 1 details
    ├── PHASE2.md                         # Phase 2 details
    ├── PHASE3.md                         # Phase 3 details
    └── API-INTEGRATION-GUIDE.md          # Frontend integration guide
```

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- PostgreSQL 12+
- (Optional) SMTP server for email
- (Optional) Twilio account for SMS
- (Optional) Microsoft Teams webhook for Teams integration

### 1. Database Setup

Create the PostgreSQL database:

```bash
createdb notifications
```

Run the migration script:

```bash
psql -U postgres -d notifications -f database/migrations/001_create_notifications_schema.sql
```

### 2. Configuration

Update `src/NotificationService.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=notifications;Username=postgres;Password=yourpassword"
  },
  "Jwt": {
    "SecretKey": "your-secret-key-min-32-characters-long",
    "Issuer": "NotificationService",
    "Audience": "NotificationServiceClients",
    "ExpirationMinutes": 60
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "noreply@yourcompany.com",
    "FromName": "Notification Service",
    "EnableSsl": "true"
  },
  "Teams": {
    "WebhookUrl": "https://your-org.webhook.office.com/webhookb2/..."
  },
  "Sms": {
    "Twilio": {
      "AccountSid": "your-account-sid",
      "AuthToken": "your-auth-token",
      "FromPhoneNumber": "+1234567890"
    }
  }
}
```

Notes:
- The application will prefer `appsettings.json`/`appsettings.Development.json` connection strings (ConnectionStrings:NotificationDb).
- For local development, the code will automatically append `Ssl Mode=Disable` to the connection string if it is not present to avoid SSL negotiation issues with a local PostgreSQL instance. If you need SSL, add `Ssl Mode=Require` and `Trust Server Certificate=true` as appropriate.
- You can also configure the connection string using the `IMPORT_PULSE_CONNECTION_STRING` environment variable; the app will fall back to that if a connection string is not present in configuration.

### 3. Run the Service

```bash
cd src/NotificationService.Api
dotnet restore
dotnet run
```

The service will be available at:
- **API**: https://localhost:5201
- **Swagger**: https://localhost:5201/swagger
- **SignalR Hub**: https://localhost:5201/hubs/notifications
- **Hangfire Dashboard**: https://localhost:5201/hangfire

## API Endpoints

### Notifications

```
GET    /api/notifications/active              # Get active notifications for current user
GET    /api/notifications/{id}                # Get notification by ID
GET    /api/notifications/tenant/{tenantId}   # Get notifications for tenant
POST   /api/notifications                     # Create notification
POST   /api/notifications/create-or-update    # Create or update (deduplication)
POST   /api/notifications/{id}/acknowledge    # Acknowledge notification
POST   /api/notifications/{id}/dismiss        # Dismiss notification
POST   /api/notifications/{id}/snooze         # Snooze notification
```

### Preferences

```
GET    /api/preferences                       # Get all user preferences
GET    /api/preferences/{channel}             # Get preference for channel
PUT    /api/preferences/{channel}             # Set/update preference
DELETE /api/preferences/{channel}             # Delete preference (reset to default)
POST   /api/preferences/defaults              # Set default preferences
```

### Subscriptions

```
GET    /api/subscriptions                     # Get all user subscriptions
POST   /api/subscriptions                     # Create/update subscription
DELETE /api/subscriptions                     # Delete subscription
```

**Authentication**: All endpoints require JWT bearer token in `Authorization` header.

## Usage Examples

### Create a Notification

```bash
curl -X POST https://localhost:5201/api/notifications \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user-uuid",
    "severity": "Critical",
    "title": "System Alert",
    "message": "Something important happened",
    "requiresAck": true,
    "actions": [
      {
        "label": "View Details",
        "action": "navigate",
        "target": "/details",
        "variant": "primary"
      }
    ]
  }'
```

### Configure Email Preferences

```bash
  curl -X PUT https://localhost:5201/api/preferences/Email \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "minSeverity": "Warning",
    "enabled": true
  }'
```

### Subscribe to Client Notifications

```bash
curl -X POST https://localhost:5201/api/subscriptions \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "client-uuid",
    "minSeverity": "Info"
  }'
```

## Frontend Integration

Complete TypeScript/React/Next.js integration guide available in [API-INTEGRATION-GUIDE.md](./API-INTEGRATION-GUIDE.md).

Includes:
- Complete TypeScript type definitions
- SignalR real-time integration
- React hooks (`useNotifications`, `usePreferences`)
- Example components (NotificationCenter, NotificationItem)
- Authentication service
- API client services
- Error handling utilities

## Configuration Guide

### Email (SMTP)

For Gmail:
1. Enable 2FA on your Google account
2. Generate an App Password
3. Use app password in `appsettings.json`

For other providers, update `SmtpHost`, `SmtpPort`, and credentials accordingly.

### Microsoft Teams

1. In Teams, go to your channel → "..." → Connectors
2. Add "Incoming Webhook"
3. Copy webhook URL
4. Add to `appsettings.json` under `Teams:WebhookUrl`

### SMS (Twilio)

1. Sign up at https://www.twilio.com/
2. Get a phone number
3. Copy Account SID and Auth Token
4. Add to `appsettings.json` under `Sms:Twilio`

**Note**: SMS has costs (~$0.0075 per message in US). Recommend enabling only for critical notifications.

### JWT Authentication

Generate a secure secret key:

```bash
openssl rand -base64 32
```

Add to `appsettings.json` under `Jwt:SecretKey`.

**Important**: Use a strong, unique secret in production!

## Background Jobs

The service includes 3 Hangfire background jobs:

| Job | Schedule | Purpose |
|-----|----------|---------|
| **NotificationRepeatJob** | Every 5 minutes | Repeats critical/urgent notifications |
| **NotificationCleanupJob** | Daily at 2 AM | Expires old and deletes acknowledged notifications |
| **NotificationBackupPollingJob** | Every 15 minutes | Safety net for missed event-driven notifications |

Monitor jobs at: https://localhost:5201/hangfire

## Testing

Run unit and integration tests:

```bash
cd tests/NotificationService.Tests
dotnet test
```

## Documentation

- **[IMPLEMENTATION.md](./IMPLEMENTATION.md)** - Phase 1 implementation details (Core system)
- **[PHASE2.md](./PHASE2.md)** - Phase 2 implementation details (Multi-channel & preferences)
- **[PHASE3.md](./PHASE3.md)** - Phase 3 implementation details (Teams, SMS, JWT)
- **[API-INTEGRATION-GUIDE.md](./API-INTEGRATION-GUIDE.md)** - Complete frontend integration guide

## Design Principles

✅ **Design for the architecture you need, implement what you need now.**

- Database schema supports the full vision (multi-tenant, event sourcing, multi-channel)
- All phases implemented incrementally without refactoring
- Clean separation of concerns (Domain, Infrastructure, API)
- SOLID principles throughout
- Ready for event-driven architecture integration
- Multi-tenancy support built-in

## Key Features Explained

### Notification Deduplication

Use `GroupKey` to prevent duplicate notifications:

```csharp
var notification = await _notificationService.CreateOrUpdateAsync(new CreateNotificationRequest
{
    GroupKey = $"saga:stuck:{sagaId}",  // Same key = update existing
    GroupCount = 1,                      // Increments on each update
    ...
});
```

### Severity-Based Behavior

| Severity | Acknowledgment | Expiration | Repeat Interval |
|----------|----------------|------------|-----------------|
| Info | Optional | 3 days | None |
| Warning | Optional | 3 days | None |
| Urgent | Required | Never | 30 minutes |
| Critical | Required | Never | 15 minutes |

### Multi-Channel Dispatch Flow

1. Event occurs (e.g., `SagaStuckEvent`)
2. Event handler creates notification
3. Dispatcher checks user subscriptions
4. For each enabled channel:
   - Check user preference
   - Check severity threshold
   - Deliver if conditions met
5. Track delivery in database

### Subscription Logic

- **No subscriptions**: Receive ALL notifications (default behavior)
- **Has subscriptions**: Only receive matching notifications
- **Wildcard**: `clientId = null` means all clients
- **Specific**: Only notifications for subscribed client/saga

## Production Checklist

Before deploying:

- [ ] Change JWT secret key (use strong, random key)
- [ ] Configure SMTP settings for email
- [ ] Set up Teams webhook (if using Teams)
- [ ] Set up Twilio account (if using SMS)
- [ ] Update CORS origins (remove `AllowAnyOrigin`)
- [ ] Enable HTTPS only
- [ ] Configure proper PostgreSQL credentials
- [ ] Set up database backups
- [ ] Configure monitoring/alerting
- [ ] Implement user service integration (replace placeholder)
- [ ] Test all channels end-to-end
- [ ] Review Hangfire dashboard security
- [ ] Set up log aggregation (e.g., Serilog to Seq/ELK)

## Performance Considerations

- **Parallel delivery**: All channels deliver simultaneously
- **Async operations**: All I/O is asynchronous
- **Database indexes**: Optimized for common queries
- **Connection pooling**: PostgreSQL connection pooling enabled
- **SignalR groups**: Efficient message routing to specific users
- **Background jobs**: Offload recurring tasks to Hangfire

## Security

- **JWT Authentication**: All API endpoints secured
- **HTTPS**: SSL/TLS encryption
- **SQL Injection**: Protected via EF Core parameterized queries
- **CORS**: Configurable allowed origins
- **Secrets**: Use environment variables or Azure Key Vault for production
- **Rate Limiting**: Consider adding in production (e.g., AspNetCoreRateLimit)

## Troubleshooting

### Common Issues

**Database connection fails**
- Verify PostgreSQL is running
- Check connection string in `appsettings.json`
- Ensure database exists and migrations are run

**SignalR won't connect**
- Check JWT token is valid
- Verify CORS allows credentials
- Ensure WebSocket is not blocked

**Emails not sending**
- Verify SMTP credentials
- Check firewall allows SMTP port
- Review logs for error messages

**401 Unauthorized**
- Check JWT token is being sent
- Verify token hasn't expired
- Ensure correct `Authorization: Bearer {token}` format

## License

This project is provided as-is for use in your applications.

## Support

For issues, questions, or contributions, please refer to the detailed documentation in the `/docs` folder.

---

**Built with .NET 8, PostgreSQL, SignalR, and ❤️**

**Status**: Production Ready 🚀
