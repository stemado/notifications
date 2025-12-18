# NotificationService.Api - Site Map Documentation

This documentation provides comprehensive coverage of all 72+ API endpoints in the NotificationService.Api application.

**Framework**: ASP.NET Core 9.0
**Base URL**: `http://localhost:5201` or `http://anf-srv06.antfarmllc.local:5201`
**Protocol**: HTTP (HTTPS in production)

---

## Quick Reference

| Category | Endpoints | Documentation |
|----------|-----------|---------------|
| Health & Status | 4 | [health-endpoints.md](./health-endpoints.md) |
| Notifications | 8 | [notifications-endpoints.md](./notifications-endpoints.md) |
| Channels | 7 | [channels-endpoints.md](./channels-endpoints.md) |
| Delivery | 5 | [delivery-endpoints.md](./delivery-endpoints.md) |
| Events | 10 | [events-endpoints.md](./events-endpoints.md) |
| Templates | 15 | [templates-endpoints.md](./templates-endpoints.md) |
| Preferences | 5 | [preferences-endpoints.md](./preferences-endpoints.md) |
| Subscriptions | 3 | [subscriptions-endpoints.md](./subscriptions-endpoints.md) |
| Test Emails | 7 | [test-email-endpoints.md](./test-email-endpoints.md) |
| Routing Core | 3 | [routing-core-endpoints.md](./routing-core-endpoints.md) |
| Contacts | 6 | [contacts-endpoints.md](./contacts-endpoints.md) |
| Groups | 7 | [groups-endpoints.md](./groups-endpoints.md) |
| Routing Events | 3 | [routing-events-endpoints.md](./routing-events-endpoints.md) |
| Policies | 6 | [policies-endpoints.md](./policies-endpoints.md) |
| SignalR Hub | 1 | [signalr-hub.md](./signalr-hub.md) |

**Total: 90 documented endpoints/features**

---

## API Categories

### Core Notification System

#### [Health Endpoints](./health-endpoints.md)
Monitor service health and readiness for Kubernetes probes and load balancers.
- `GET /health` - Built-in ASP.NET Core health check
- `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe
- `GET /api/health` - Comprehensive service health with channel status

#### [Notifications Endpoints](./notifications-endpoints.md)
Create, retrieve, and manage user notifications.
- `GET /api/notifications/active` - Get active notifications
- `GET /api/notifications/{id}` - Get specific notification
- `GET /api/notifications/tenant/{tenantId}` - Get tenant notifications
- `POST /api/notifications` - Create notification
- `POST /api/notifications/create-or-update` - Create or update by GroupKey
- `POST /api/notifications/{id}/acknowledge` - Acknowledge
- `POST /api/notifications/{id}/dismiss` - Dismiss
- `POST /api/notifications/{id}/snooze` - Snooze

#### [Channels Endpoints](./channels-endpoints.md)
Configure and manage notification delivery channels (Email, SignalR, SMS, Teams).
- `GET /api/channels/status` - Get all channel statuses
- `GET /api/channels/config` - Get all configurations
- `GET /api/channels/{channel}/config` - Get specific config
- `PUT /api/channels/{channel}/config` - Update config
- `POST /api/channels/{channel}/enable` - Enable channel
- `POST /api/channels/{channel}/disable` - Disable channel
- `POST /api/channels/{channel}/test` - Test connectivity

#### [Delivery Endpoints](./delivery-endpoints.md)
Track and manage notification delivery queue and history.
- `GET /api/delivery/queue` - Current delivery queue
- `GET /api/delivery/history` - Delivery history with filters
- `GET /api/delivery/stats` - Delivery statistics
- `POST /api/delivery/{id}/retry` - Retry failed delivery
- `DELETE /api/delivery/{id}` - Cancel pending delivery

---

### Event Processing

#### [Events Endpoints](./events-endpoints.md)
Receive and process system events that trigger notifications.
- `POST /api/events/saga-stuck` - Saga stuck event
- `POST /api/events/import-completed` - Import completed
- `POST /api/events/import-failed` - Import failed
- `POST /api/events/escalation-created` - Escalation created
- `POST /api/events/file-processing-error` - File processing error
- `POST /api/events/file-picked-up` - File picked up
- `POST /api/events/sla-breach` - SLA breach
- `POST /api/events/plansource-failed` - PlanSource operation failed
- `POST /api/events/aggregate-stalled` - Aggregate generation stalled
- `POST /api/events/templates-queued` - Templates queued

---

### Email Templates

#### [Templates Endpoints](./templates-endpoints.md)
Manage email templates with variable substitution and preview.
- `GET /api/templates` - Get active templates
- `GET /api/templates/all` - Get all templates
- `GET /api/templates/{id}` - Get by ID
- `GET /api/templates/name/{name}` - Get by name
- `GET /api/templates/type/{type}` - Get by type
- `GET /api/templates/type/{type}/all` - Get all of type
- `POST /api/templates` - Create template
- `PUT /api/templates/{id}` - Update template
- `DELETE /api/templates/{id}` - Delete template
- `POST /api/templates/preview` - Preview with data
- `POST /api/templates/extract-variables/{name}` - Extract variables
- `POST /api/templates/validate/{name}` - Validate syntax
- `POST /api/templates/send` - Send templated email
- `GET /api/templates/health` - Template service health
- `GET /api/templates/syntax-guide` - Syntax documentation

#### [Test Email Endpoints](./test-email-endpoints.md)
Send test emails to verify delivery and templates.
- `POST /api/test-emails/send-to-group` - Send to recipient group
- `POST /api/test-emails/send-to-contacts` - Send to specific contacts
- `POST /api/test-emails/send-to-addresses` - Send to email addresses
- `GET /api/test-emails/eligible-groups` - Get eligible groups
- `GET /api/test-emails/preview-recipients/{groupId}` - Preview recipients
- `GET /api/test-emails/history` - Get delivery history
- `GET /api/test-emails/history/{id}` - Get specific record

---

### User Preferences

#### [Preferences Endpoints](./preferences-endpoints.md)
Manage user notification preferences per channel.
- `GET /api/preferences` - Get all preferences
- `GET /api/preferences/{channel}` - Get channel preference
- `PUT /api/preferences/{channel}` - Set/update preference
- `DELETE /api/preferences/{channel}` - Reset to default
- `POST /api/preferences/defaults` - Set default preferences

#### [Subscriptions Endpoints](./subscriptions-endpoints.md)
Subscribe to notifications for specific clients or sagas.
- `GET /api/subscriptions` - Get user subscriptions
- `POST /api/subscriptions` - Create/update subscription
- `DELETE /api/subscriptions` - Delete subscription

---

### Routing System

#### [Routing Core Endpoints](./routing-core-endpoints.md)
Core routing functionality for event publishing and dashboard.
- `GET /api/routing/dashboard` - Routing dashboard with stats
- `POST /api/routing/publish` - Publish outbound event
- `GET /api/routing/clients/{clientId}/configuration` - Get client config

#### [Contacts Endpoints](./contacts-endpoints.md)
Manage notification recipients.
- `GET /api/routing/contacts` - List contacts
- `GET /api/routing/contacts/{id}` - Get contact details
- `POST /api/routing/contacts` - Create contact
- `PUT /api/routing/contacts/{id}` - Update contact
- `DELETE /api/routing/contacts/{id}` - Deactivate contact
- `GET /api/routing/contacts/{id}/groups` - Get contact's groups

#### [Groups Endpoints](./groups-endpoints.md)
Manage recipient groups for notification routing.
- `GET /api/routing/groups` - List groups
- `GET /api/routing/groups/{id}` - Get group details
- `POST /api/routing/groups` - Create group
- `PUT /api/routing/groups/{id}` - Update group
- `POST /api/routing/groups/{id}/members` - Add member
- `DELETE /api/routing/groups/{id}/members/{contactId}` - Remove member
- `GET /api/routing/groups/{id}/members` - Get members

#### [Routing Events Endpoints](./routing-events-endpoints.md)
Query outbound events and delivery history.
- `GET /api/routing/events` - List events with filters
- `GET /api/routing/events/{id}` - Get event details
- `GET /api/routing/events/saga/{sagaId}` - Get saga events

#### [Policies Endpoints](./policies-endpoints.md)
Configure routing policies that determine how events reach recipients.
- `GET /api/routing/policies` - List policies
- `GET /api/routing/policies/{id}` - Get policy details
- `POST /api/routing/policies` - Create policy
- `PUT /api/routing/policies/{id}` - Update policy
- `DELETE /api/routing/policies/{id}` - Delete policy
- `POST /api/routing/policies/{id}/toggle` - Toggle enabled/disabled

---

### Real-Time Communication

#### [SignalR Hub](./signalr-hub.md)
WebSocket-based real-time notification delivery.
- Hub Path: `/hubs/notifications`
- Events: `NewNotification`
- Methods: `AcknowledgeNotification`, `DismissNotification`, `SnoozeNotification`, `GetActiveNotifications`
- Groups: User, Tenant, Ops-Team

---

## Authentication

| Access Level | Endpoints | Notes |
|--------------|-----------|-------|
| **Public** | Health, Channel Status, Event Handlers, Templates (read), Routing queries | No authentication required |
| **Protected** | Notifications, Preferences, Subscriptions | JWT authentication required |
| **Admin** | Channel Config, Test Emails, Contact/Group/Policy management | Admin role required |

### JWT Configuration
- **Issuer**: Keycloak or configured identity provider
- **Required Claims**: `NameIdentifier`, `TenantId`, `Role`
- **Development Mode**: Auto-injects system user for testing

---

## Common Response Formats

### Success Response
```json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully"
}
```

### Error Response
```json
{
  "success": false,
  "error": "Error description",
  "details": "Additional context"
}
```

### Paginated Response
```json
{
  "items": [ ... ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 150,
  "totalPages": 8,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

---

## Severity Levels

| Level | Value | Use Case |
|-------|-------|----------|
| Info | 0 | Informational notifications |
| Warning | 1 | Attention needed, not urgent |
| Urgent | 2 | Requires prompt action |
| Critical | 3 | Immediate action required |

---

## Notification Channels

| Channel | Description | Best For |
|---------|-------------|----------|
| **SignalR** | Real-time WebSocket | Immediate UI updates |
| **Email** | SMTP/MS Graph | Detailed notifications, audit trail |
| **SMS** | Twilio integration | Critical alerts, on-call |
| **Teams** | Microsoft Teams webhooks | Team collaboration |

---

## Getting Started

### 1. Check Service Health
```bash
curl http://localhost:5201/api/health
```

### 2. Create a Notification
```bash
curl -X POST http://localhost:5201/api/notifications \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Notification",
    "message": "Hello from NotificationService!",
    "severity": "Info",
    "channels": ["SignalR", "Email"]
  }'
```

### 3. Connect to SignalR
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5201/hubs/notifications")
  .withAutomaticReconnect()
  .build();

connection.on("NewNotification", (notification) => {
  console.log("Received:", notification);
});

await connection.start();
```

---

## Related Documentation

- [NotificationService.Api Project](../../../NotificationService.Api/)
- [Census Reconciliation MCP Server](../../../../mcp-servers/census-reconciliation-mcp-server/)
- [Notification Service MCP Server](../../../../mcp-servers/notification-service-mcp-server/)

---

*Generated: December 2025*
