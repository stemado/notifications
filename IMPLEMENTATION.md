# Notification Service Implementation

This document provides an overview of the implemented notification service architecture.

## What Has Been Implemented (Phase 1)

### ✅ Complete Implementation

1. **Database Schema**
   - Full PostgreSQL schema with all tables (Notifications, NotificationDeliveries, UserNotificationPreferences, NotificationSubscriptions)
   - Comprehensive indexes for performance
   - Support for multi-tenancy and event sourcing from day one
   - Location: `database/migrations/001_create_notifications_schema.sql`

2. **Domain Models**
   - `Notification` - Core notification entity with full architecture support
   - `NotificationAction` - Actions that can be performed on notifications
   - `NotificationSeverity` enum - Info, Warning, Urgent, Critical
   - `NotificationChannel` enum - SignalR, Email, SMS, Slack
   - `NotificationDelivery` - Delivery tracking entity (Phase 2 usage)
   - Location: `src/NotificationService.Domain/Models/`, `src/NotificationService.Domain/Enums/`

3. **Data Access Layer**
   - `NotificationDbContext` - EF Core DbContext with PostgreSQL support
   - `INotificationRepository` & `NotificationRepository` - Data access with full CRUD operations
   - JSON column support for Actions and Metadata
   - Location: `src/NotificationService.Infrastructure/Data/`, `src/NotificationService.Infrastructure/Repositories/`

4. **Service Layer**
   - `INotificationService` & `NotificationService` - Core business logic
   - Methods:
     - `CreateAsync` - Create new notifications
     - `CreateOrUpdateAsync` - Create or update using GroupKey for deduplication
     - `GetActiveForUserAsync` - Query active notifications
     - `GetByTenantAsync` - Multi-tenant support
     - `AcknowledgeAsync`, `DismissAsync`, `SnoozeAsync` - User actions
     - `ExpireOldNotificationsAsync`, `DeleteAcknowledgedAsync` - Cleanup
   - Location: `src/NotificationService.Infrastructure/Services/`

5. **SignalR Hub**
   - `NotificationHub` - Real-time notification delivery
   - Group management (user groups, tenant groups, ops-team)
   - Client methods: AcknowledgeNotification, DismissNotification, SnoozeNotification, GetActiveNotifications
   - Location: `src/NotificationService.Api/Hubs/NotificationHub.cs`

6. **Event Handlers**
   - `IEventHandler<TEvent>` - Generic event handler interface
   - `SagaStuckNotificationHandler` - Example event handler with:
     - Severity determination based on stuck duration
     - Repeat interval configuration
     - Notification actions (Fix Now, Snooze)
     - SignalR push integration
   - Location: `src/NotificationService.Api/EventHandlers/`, `src/NotificationService.Api/Events/`

7. **Background Jobs (Hangfire)**
   - `NotificationRepeatJob` - Repeats notifications based on RepeatInterval (every 5 minutes)
   - `NotificationCleanupJob` - Expires and deletes old notifications (daily at 2 AM)
   - `NotificationBackupPollingJob` - Safety net for missed notifications (every 15 minutes)
   - Location: `src/NotificationService.Api/Jobs/`

8. **REST API Controllers**
   - `NotificationsController` - Full CRUD API
   - Endpoints:
     - `GET /api/notifications/active` - Get active notifications
     - `GET /api/notifications/{id}` - Get specific notification
     - `GET /api/notifications/tenant/{tenantId}` - Get tenant notifications
     - `POST /api/notifications` - Create notification
     - `POST /api/notifications/create-or-update` - Create or update
     - `POST /api/notifications/{id}/acknowledge` - Acknowledge
     - `POST /api/notifications/{id}/dismiss` - Dismiss
     - `POST /api/notifications/{id}/snooze` - Snooze
   - Location: `src/NotificationService.Api/Controllers/`

9. **Dependency Injection & Configuration**
   - `ServiceCollectionExtensions` - DI registration for all services
   - `Program.cs` - Application startup with SignalR, Hangfire, and CORS
   - `appsettings.json` - Configuration files
   - Hangfire dashboard available at `/hangfire`
   - SignalR hub available at `/hubs/notifications`
   - Location: `src/NotificationService.Api/Extensions/`, `src/NotificationService.Api/`

### ⏳ Phase 2 Stubs (Implemented but Not Active)

1. **Multi-Channel Dispatcher**
   - `INotificationDispatcher` & `NotificationDispatcher` - Stub implementation
   - `INotificationChannel` - Channel interface
   - `SignalRChannel` - Stub (Phase 1 uses direct SignalR push)
   - `EmailChannel` - Stub for future email notifications
   - `SlackChannel` - Stub for future Slack integration
   - Location: `src/NotificationService.Infrastructure/Services/`, `src/NotificationService.Infrastructure/Services/Channels/`

## Project Structure

```
notifications/
├── src/
│   ├── NotificationService.Api/          # ASP.NET Core Web API
│   │   ├── Controllers/                  # REST API controllers
│   │   ├── EventHandlers/                # Domain event handlers
│   │   ├── Events/                       # Event definitions
│   │   ├── Extensions/                   # DI extensions
│   │   ├── Hubs/                        # SignalR hubs
│   │   ├── Jobs/                        # Hangfire background jobs
│   │   ├── Program.cs                   # Application entry point
│   │   └── appsettings.json            # Configuration
│   ├── NotificationService.Domain/       # Domain models & DTOs
│   │   ├── DTOs/                        # Data transfer objects
│   │   ├── Enums/                       # Enumerations
│   │   └── Models/                      # Domain entities
│   └── NotificationService.Infrastructure/ # Data access & services
│       ├── Data/                        # EF Core DbContext
│       ├── Repositories/                # Data repositories
│       └── Services/                    # Business logic & channels
├── tests/
│   └── NotificationService.Tests/        # Unit & integration tests
├── database/
│   └── migrations/                      # SQL migration scripts
├── NotificationService.sln              # Solution file
└── README.md                           # Architecture documentation
```

## Running the Service

### Prerequisites

- .NET 8.0 SDK
- PostgreSQL 12+
- Connection string configured in `appsettings.json`

### Database Setup

1. Create the PostgreSQL database:
   ```sql
   CREATE DATABASE notifications;
   ```

2. Run the migration script:
   ```bash
   psql -U postgres -d notifications -f database/migrations/001_create_notifications_schema.sql
   ```

### Running the API

```bash
cd src/NotificationService.Api
dotnet restore
dotnet run
```

The API will be available at:
- HTTPS: https://localhost:5201
- HTTP: http://localhost:5200
- SignalR Hub: https://localhost:5201/hubs/notifications
- Hangfire Dashboard: https://localhost:5201/hangfire

### API Documentation

Swagger UI available at: https://localhost:5201/swagger

## Key Features

### Notification Grouping & Deduplication
- Use `GroupKey` to prevent duplicate notifications
- `CreateOrUpdateAsync` updates existing notifications instead of creating duplicates
- `GroupCount` tracks how many times a notification was triggered

### Severity-Based Behavior
- **Info/Warning**: Optional acknowledgment, auto-expires after 3 days
- **Urgent/Critical**: Requires acknowledgment, no expiration
- **Critical**: Repeats every 15 minutes
- **Urgent**: Repeats every 30 minutes

### Real-Time Delivery
- SignalR for instant push notifications
- User-specific groups (`user:{userId}`)
- Tenant groups (`tenant:{tenantId}`)
- Ops team group (`ops-team`)

### Background Processing
- Automatic notification repeats
- Daily cleanup of old notifications
- Backup polling for missed notifications

## Next Steps (Phase 2)

1. **Multi-Channel Delivery**
   - Activate `NotificationDispatcher`
   - Implement `EmailChannel`
   - Implement user preferences

2. **User Preferences**
   - Service for managing channel preferences
   - Minimum severity filtering
   - Channel enable/disable per user

3. **Subscription Management**
   - Subscribe to specific clients/sagas
   - Subscription-based notification routing

4. **Authentication & Authorization**
   - JWT authentication
   - Role-based access control
   - Hangfire dashboard authentication

## Design Principles

✅ **Design for the architecture you need, implement what you need now.**

- Database schema supports full vision (multi-tenant, event sourcing, multi-channel)
- Code implements Phase 1 (SignalR, basic notifications)
- No refactoring needed later
- Clean separation of concerns
- SOLID principles throughout

## Testing

To run tests:
```bash
cd tests/NotificationService.Tests
dotnet test
```

## Contributing

See README.md for architectural decisions and design patterns.
