# Phase 2 Implementation - Multi-Channel Delivery & User Preferences

This document describes the Phase 2 implementation of the notification service.

## What Was Implemented

### ✅ User Preferences System

**Domain Models:**
- `UserNotificationPreference` - User channel preferences with severity filtering
- Location: `src/NotificationService.Domain/Models/Preferences/`

**Repositories:**
- `IUserPreferenceRepository` & `UserPreferenceRepository` - CRUD operations for preferences
- Location: `src/NotificationService.Infrastructure/Repositories/`

**Services:**
- `IUserPreferenceService` & `UserPreferenceService` - Business logic for preferences
- Features:
  - Get/Set/Delete user preferences
  - Default preferences (SignalR: all, Email: warning+, SMS: critical, Slack: urgent+)
  - Channel enablement checking with severity filtering
- Location: `src/NotificationService.Infrastructure/Services/`

**API Endpoints:**
- `GET /api/preferences` - Get all preferences for current user
- `GET /api/preferences/{channel}` - Get preference for specific channel
- `PUT /api/preferences/{channel}` - Set/update preference
- `DELETE /api/preferences/{channel}` - Delete preference (reset to default)
- `POST /api/preferences/defaults` - Set default preferences
- Location: `src/NotificationService.Api/Controllers/PreferencesController.cs`

### ✅ Subscription Management

**Domain Models:**
- `NotificationSubscription` - User subscriptions to clients/sagas
- Location: `src/NotificationService.Domain/Models/Preferences/`

**Repositories:**
- `ISubscriptionRepository` & `SubscriptionRepository` - CRUD operations for subscriptions
- Location: `src/NotificationService.Infrastructure/Repositories/`

**Services:**
- `ISubscriptionService` & `SubscriptionService` - Business logic for subscriptions
- Features:
  - Subscribe/unsubscribe to clients or sagas
  - Get subscribed users for a notification
  - Check if user should receive notification based on subscriptions
  - Wildcard subscriptions (null client/saga = all)
- Location: `src/NotificationService.Infrastructure/Services/`

**API Endpoints:**
- `GET /api/subscriptions` - Get all subscriptions for current user
- `POST /api/subscriptions` - Create/update subscription
- `DELETE /api/subscriptions` - Delete subscription
- Location: `src/NotificationService.Api/Controllers/SubscriptionsController.cs`

### ✅ Email Channel

**Email Services:**
- `IEmailService` & `SmtpEmailService` - SMTP email sending
- `IEmailTemplateService` & `EmailTemplateService` - HTML/plain text email templates
- Features:
  - Beautiful HTML email templates with severity-based colors
  - Plain text alternatives
  - Action buttons in emails
  - Metadata display
  - Severity icons (🚨 Critical, ⚠️ Urgent, ⚡Warning, ℹ️ Info)
- Location: `src/NotificationService.Infrastructure/Services/Email/`

**EmailChannel Implementation:**
- Delivers notifications via SMTP
- Tracks delivery status in `NotificationDeliveries` table
- Error handling and logging
- Location: `src/NotificationService.Infrastructure/Services/Channels/EmailChannel.cs`

**Configuration:**
- SMTP settings in `appsettings.json`:
  - SmtpHost, SmtpPort, SmtpUsername, SmtpPassword
  - FromEmail, FromName, EnableSsl

### ✅ Activated Multi-Channel Dispatcher

**NotificationDispatcher (ACTIVE):**
- Checks user subscriptions
- Checks channel preferences and severity thresholds
- Dispatches to multiple channels in parallel
- Comprehensive logging
- Location: `src/NotificationService.Infrastructure/Services/NotificationDispatcher.cs`

**SignalRChannel Implementation:**
- Moved SignalR push logic into channel
- Delivers via SignalR Hub with group management
- Tracks delivery status
- Location: `src/NotificationService.Infrastructure/Services/Channels/SignalRChannel.cs`

**Event Handler Refactoring:**
- `SagaStuckNotificationHandler` now uses dispatcher instead of direct SignalR
- All notifications go through multi-channel dispatcher
- Location: `src/NotificationService.Api/EventHandlers/SagaStuckNotificationHandler.cs`

### ✅ Delivery Tracking

**Repository:**
- `INotificationDeliveryRepository` & `NotificationDeliveryRepository`
- Tracks all delivery attempts across all channels
- Location: `src/NotificationService.Infrastructure/Repositories/`

**Features:**
- Records delivery attempts with timestamps
- Tracks success/failure with error messages
- Attempt counting for retry logic (future)

### ✅ User Service (Placeholder)

**Services:**
- `IUserService` & `UserService` - User information lookup
- **NOTE**: Currently returns placeholder data
- **TODO**: Replace with integration to actual user management system
- Location: `src/NotificationService.Infrastructure/Services/`

### ✅ Database Schema Updates

**DbContext Updates:**
- Added `UserNotificationPreferences` DbSet
- Added `NotificationSubscriptions` DbSet
- Configured composite keys and indexes
- Location: `src/NotificationService.Infrastructure/Data/NotificationDbContext.cs`

**Tables Already Created (Phase 1):**
- `UserNotificationPreferences` - Ready to use
- `NotificationSubscriptions` - Ready to use
- `NotificationDeliveries` - Ready to use

### ✅ Dependency Injection Updates

**New Services Registered:**
- User preference repository and service
- Subscription repository and service
- Notification delivery repository
- User service
- Email service and template service
- Email channel (in addition to SignalR channel)
- Location: `src/NotificationService.Api/Extensions/ServiceCollectionExtensions.cs`

## How It Works

### Notification Flow (Phase 2)

1. **Event occurs** (e.g., SagaStuckEvent)
2. **Event handler** creates notification via `NotificationService`
3. **Dispatcher** is called with the notification
4. **Subscription check**: Does user want this notification?
5. **For each channel** (SignalR, Email):
   - **Preference check**: Is channel enabled for this user?
   - **Severity check**: Does notification severity meet minimum threshold?
   - If yes, **deliver** via channel
   - **Track delivery** in `NotificationDeliveries` table
6. **Multiple channels deliver in parallel**

### User Preference Defaults

When a user first uses the system (or resets to defaults):

| Channel | Enabled | Min Severity |
|---------|---------|--------------|
| SignalR | ✅ Yes  | Info         |
| Email   | ✅ Yes  | Warning      |
| SMS     | ❌ No   | Critical     |
| Slack   | ❌ No   | Urgent       |

### Subscription Behavior

- **No subscriptions**: User receives ALL notifications (default)
- **Has subscriptions**: User only receives notifications matching subscriptions
- **Wildcard subscription** (null client/saga): Receives all notifications
- **Client subscription**: Receives all notifications for that client
- **Saga subscription**: Receives notifications for that specific saga

## Configuration

### Email (SMTP)

Update `appsettings.json`:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "noreply@yourcompany.com",
    "FromName": "Your Notification Service",
    "EnableSsl": "true"
  }
}
```

**For Gmail:**
1. Enable 2FA on your Google account
2. Create an App Password
3. Use app password in `SmtpPassword`

**For other SMTP providers:**
- Update `SmtpHost` and `SmtpPort`
- Set credentials accordingly

### User Service Integration

**IMPORTANT**: The `UserService` currently returns placeholder data.

To integrate with your user management system:

1. Update `src/NotificationService.Infrastructure/Services/UserService.cs`
2. Inject your user repository or external user service
3. Implement `GetUserEmailAsync` and `GetUserNameAsync` methods

Example:
```csharp
public async Task<string?> GetUserEmailAsync(Guid userId)
{
    return await _userRepository.GetEmailByIdAsync(userId);
}
```

## API Usage Examples

### Set User Preferences

```bash
# Enable email notifications for urgent+ severity
curl -X PUT https://localhost:5201/api/preferences/Email \
  -H "Content-Type: application/json" \
  -d '{
    "minSeverity": "Urgent",
    "enabled": true
  }'
```

### Subscribe to Client Notifications

```bash
# Subscribe to all notifications for a specific client
curl -X POST https://localhost:5201/api/subscriptions \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "12345678-1234-1234-1234-123456789012",
    "sagaId": null,
    "minSeverity": "Warning"
  }'
```

### Subscribe to Specific Saga

```bash
# Subscribe to critical notifications for a specific saga
curl -X POST https://localhost:5201/api/subscriptions \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "12345678-1234-1234-1234-123456789012",
    "sagaId": "87654321-4321-4321-4321-210987654321",
    "minSeverity": "Critical"
  }'
```

## Testing

### Test Email Delivery

1. Configure SMTP settings in `appsettings.json`
2. Create a notification via API or trigger an event
3. Check email inbox
4. Verify HTML formatting and action buttons

### Test Preferences

1. Set preference: `PUT /api/preferences/Email` (disable)
2. Trigger notification
3. Verify email is NOT sent (but SignalR still works)
4. Enable preference again
5. Verify email IS sent

### Test Subscriptions

1. Create subscription for specific client
2. Trigger notification for different client
3. Verify user does NOT receive it
4. Trigger notification for subscribed client
5. Verify user DOES receive it

## What's Next (Phase 3)

### Slack Integration
- Implement `SlackChannel`
- Slack webhook configuration
- Message formatting for Slack
- Support for channels and DMs

### SMS Channel
- Implement `SmsChannel`
- Twilio/AWS SNS integration
- Phone number management
- Cost tracking

### Advanced Features
- Notification templates (customizable)
- Localization/i18n support
- Notification history/audit log
- Analytics dashboard
- Retry logic for failed deliveries
- Rate limiting

### Authentication & Authorization
- JWT authentication
- Role-based access control
- API key authentication for external services
- Hangfire dashboard authentication

## Migration from Phase 1

**Good News**: No migration needed! 🎉

- Database schema was already created in Phase 1
- Phase 1 code continues to work
- Phase 2 enhances with multi-channel delivery
- Gradual adoption - channels can be enabled per user

## Troubleshooting

### Emails Not Sending

1. Check SMTP configuration in `appsettings.json`
2. Verify SMTP credentials
3. Check firewall/network allows SMTP port
4. Review logs for error messages
5. Test SMTP connection separately

### User Receives No Notifications

1. Check user preferences: `GET /api/preferences`
2. Verify severity thresholds
3. Check subscriptions: `GET /api/subscriptions`
4. Review logs for dispatcher decisions

### SignalR Still Works But Email Doesn't

1. Check if Email channel is enabled in preferences
2. Verify user has valid email address
3. Check `NotificationDeliveries` table for error messages
4. Review email service logs

## Performance Considerations

- **Parallel delivery**: Channels deliver simultaneously
- **Async operations**: All I/O is asynchronous
- **Database indexes**: Optimized queries for preferences and subscriptions
- **Logging**: Comprehensive but configurable log levels

## Security Notes

- **SMTP credentials**: Store securely (use Azure Key Vault, AWS Secrets Manager, etc.)
- **User authentication**: Phase 3 will add JWT auth
- **Email spoofing**: Use proper SPF/DKIM records
- **Rate limiting**: Consider implementing in Phase 3

## Summary

Phase 2 delivers a production-ready multi-channel notification system with:

- ✅ User preferences with channel control
- ✅ Subscription-based filtering
- ✅ Email notifications with beautiful templates
- ✅ Multi-channel dispatcher
- ✅ Delivery tracking
- ✅ Backward compatible with Phase 1

**All Phase 1 functionality remains intact while adding powerful new features!**
