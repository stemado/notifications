# Notifications API Endpoints

## Overview

The Notifications API provides endpoints for managing user notifications within the PlanSource Automation platform. This API supports creating, retrieving, acknowledging, dismissing, and snoozing notifications across multiple channels (SignalR, Email, SMS, etc.).

**Base URL**: `http://192.168.150.52:5201/api/notifications`

**Authentication**: Most endpoints require authentication via JWT token. The authenticated user's ID is extracted from the `ClaimTypes.NameIdentifier` claim.

---

## Endpoints

### 1. Get Active Notifications

Retrieves all active (non-dismissed, non-expired) notifications for the authenticated user.

**Endpoint**: `GET /api/notifications/active`

**Authentication**: Required

**Request**: No parameters required.

**Response**: `200 OK`
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "severity": "Warning",
    "title": "File Processing Delayed",
    "message": "Census file for Client XYZ is taking longer than expected to process.",
    "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "eventType": "FileProcessingDelayed",
    "createdAt": "2025-12-18T10:30:00Z",
    "acknowledgedAt": null,
    "dismissedAt": null,
    "expiresAt": "2025-12-18T18:30:00Z",
    "repeatInterval": 30,
    "lastRepeatedAt": null,
    "requiresAck": true,
    "groupKey": "saga:delayed:3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "groupCount": 1,
    "actions": [
      {
        "label": "View Details",
        "action": "navigate",
        "target": "/sagas/3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "variant": "primary"
      },
      {
        "label": "Retry Now",
        "action": "api_call",
        "target": "/api/sagas/3fa85f64-5717-4562-b3fc-2c963f66afa6/retry",
        "variant": "secondary"
      }
    ],
    "metadata": {
      "fileName": "census_2025_12_18.csv",
      "clientName": "Client XYZ",
      "processingTimeMinutes": 45
    }
  }
]
```

**Error Responses**:
- `401 Unauthorized`: User not authenticated
- `500 Internal Server Error`: Error retrieving notifications

**Example Usage**:
```bash
curl -X GET "http://192.168.150.52:5201/api/notifications/active" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 2. Get Specific Notification

Retrieves a specific notification by ID. Includes authorization check to ensure the authenticated user owns the notification.

**Endpoint**: `GET /api/notifications/{id}`

**Authentication**: Required (with ownership validation)

**Path Parameters**:
- `id` (Guid, required): The notification ID

**Response**: `200 OK`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tenantId": null,
  "severity": "Critical",
  "title": "Saga Stuck",
  "message": "Reconciliation saga has been in ProcessingFile state for over 2 hours.",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "eventId": null,
  "eventType": null,
  "createdAt": "2025-12-18T08:00:00Z",
  "acknowledgedAt": null,
  "dismissedAt": null,
  "expiresAt": null,
  "repeatInterval": 15,
  "lastRepeatedAt": "2025-12-18T09:45:00Z",
  "requiresAck": true,
  "groupKey": "saga:stuck:3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "groupCount": 5,
  "actions": [],
  "metadata": {}
}
```

**Error Responses**:
- `401 Unauthorized`: User not authenticated
- `403 Forbidden`: User does not own this notification
- `404 Not Found`: Notification not found
- `500 Internal Server Error`: Error retrieving notification

**Example Usage**:
```bash
curl -X GET "http://192.168.150.52:5201/api/notifications/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 3. Get Tenant Notifications

Retrieves all notifications for a specific tenant. Intended for admin/ops users to view all notifications within a tenant's scope.

**Endpoint**: `GET /api/notifications/tenant/{tenantId}`

**Authentication**: Required (admin/ops role - Phase 2)

**Path Parameters**:
- `tenantId` (Guid, required): The tenant ID

**Response**: `200 OK`
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "severity": "Info",
    "title": "Import Complete",
    "message": "Census import completed successfully.",
    "sagaId": null,
    "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "eventId": null,
    "eventType": "ImportCompleted",
    "createdAt": "2025-12-18T11:00:00Z",
    "acknowledgedAt": "2025-12-18T11:05:00Z",
    "dismissedAt": null,
    "expiresAt": "2025-12-19T11:00:00Z",
    "repeatInterval": null,
    "lastRepeatedAt": null,
    "requiresAck": false,
    "groupKey": null,
    "groupCount": 1,
    "actions": [],
    "metadata": {}
  }
]
```

**Error Responses**:
- `500 Internal Server Error`: Error retrieving tenant notifications

**Note**: Authorization checks for admin/ops role will be added in Phase 2.

**Example Usage**:
```bash
curl -X GET "http://192.168.150.52:5201/api/notifications/tenant/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 4. Create Notification

Creates a new notification and dispatches it to all enabled channels (SignalR, Email, SMS, etc.) with delivery tracking.

**Endpoint**: `POST /api/notifications`

**Authentication**: Required

**Request Body**:
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "severity": "Warning",
  "title": "File Processing Delayed",
  "message": "Census file for Client XYZ is taking longer than expected to process.",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "eventType": "FileProcessingDelayed",
  "repeatInterval": 30,
  "requiresAck": true,
  "expiresAt": "2025-12-18T18:30:00Z",
  "groupKey": "saga:delayed:3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "actions": [
    {
      "label": "View Details",
      "action": "navigate",
      "target": "/sagas/3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "variant": "primary"
    }
  ],
  "metadata": {
    "fileName": "census_2025_12_18.csv",
    "clientName": "Client XYZ"
  }
}
```

**Request Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `userId` | Guid | Yes | User who should receive this notification |
| `tenantId` | Guid | No | Tenant ID for multi-tenant scenarios (null = ops team, not null = client portal) |
| `severity` | Enum | Yes | Severity level: `Info`, `Warning`, `Urgent`, `Critical` |
| `title` | String | Yes | Notification title (max 200 characters) |
| `message` | String | Yes | Detailed notification message |
| `sagaId` | Guid | No | Link to associated saga |
| `clientId` | Guid | No | Link to associated client |
| `eventId` | Guid | No | Link to domain event (event sourcing ready) |
| `eventType` | String | No | Type of event that triggered this notification |
| `repeatInterval` | Int | No | Minutes between notification repeats (for persistent issues) |
| `requiresAck` | Boolean | Yes | Whether this notification requires acknowledgment |
| `expiresAt` | DateTime | No | When the notification should expire |
| `groupKey` | String | No | Key for grouping/deduplicating similar notifications |
| `actions` | Array | No | Available actions for this notification |
| `metadata` | Object | No | Additional metadata as key-value pairs |

**Action Object Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `label` | String | Yes | Display label for the action button |
| `action` | String | Yes | Type of action: `navigate`, `api_call`, `dismiss` |
| `target` | String | No | Target for the action (URL for navigate, endpoint for api_call) |
| `variant` | String | Yes | Visual variant: `primary`, `secondary`, `danger` |

**Response**: `201 Created`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "severity": "Warning",
  "title": "File Processing Delayed",
  "message": "Census file for Client XYZ is taking longer than expected to process.",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "eventType": "FileProcessingDelayed",
  "createdAt": "2025-12-18T10:30:00Z",
  "acknowledgedAt": null,
  "dismissedAt": null,
  "expiresAt": "2025-12-18T18:30:00Z",
  "repeatInterval": 30,
  "lastRepeatedAt": null,
  "requiresAck": true,
  "groupKey": "saga:delayed:3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "groupCount": 1,
  "actions": [
    {
      "label": "View Details",
      "action": "navigate",
      "target": "/sagas/3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "variant": "primary"
    }
  ],
  "metadata": {
    "fileName": "census_2025_12_18.csv",
    "clientName": "Client XYZ"
  }
}
```

**Error Responses**:
- `500 Internal Server Error`: Error creating notification

**Example Usage**:
```bash
curl -X POST "http://192.168.150.52:5201/api/notifications" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "severity": "Warning",
    "title": "File Processing Delayed",
    "message": "Census file is taking longer than expected.",
    "requiresAck": true
  }'
```

---

### 5. Create or Update Notification

Creates a new notification or updates an existing one using the `GroupKey` field for deduplication. This endpoint is ideal for preventing duplicate notifications for the same issue (e.g., a saga stuck in the same state).

**Endpoint**: `POST /api/notifications/create-or-update`

**Authentication**: Required

**Request Body**: Same as [Create Notification](#4-create-notification)

**Behavior**:
- If a notification with the same `groupKey` exists and is not dismissed:
  - Updates the existing notification
  - Increments the `groupCount` field
  - Updates timestamps and metadata
  - Re-dispatches to all enabled channels
- If no matching notification exists or all are dismissed:
  - Creates a new notification
  - Sets `groupCount` to 1

**Response**: `200 OK`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tenantId": null,
  "severity": "Critical",
  "title": "Saga Stuck",
  "message": "Reconciliation saga has been in ProcessingFile state for over 2 hours.",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "eventId": null,
  "eventType": null,
  "createdAt": "2025-12-18T08:00:00Z",
  "acknowledgedAt": null,
  "dismissedAt": null,
  "expiresAt": null,
  "repeatInterval": 15,
  "lastRepeatedAt": "2025-12-18T10:30:00Z",
  "requiresAck": true,
  "groupKey": "saga:stuck:3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "groupCount": 5,
  "actions": [],
  "metadata": {}
}
```

**Error Responses**:
- `500 Internal Server Error`: Error creating or updating notification

**Use Cases**:
- Saga stuck notifications: Use `groupKey` like `saga:stuck:{sagaId}` to update the same notification rather than creating duplicates
- File processing delays: Use `groupKey` like `file:delayed:{fileId}` to track repeated delays
- System health alerts: Use `groupKey` like `health:{serviceName}:{issue}` to consolidate alerts

**Example Usage**:
```bash
curl -X POST "http://192.168.150.52:5201/api/notifications/create-or-update" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "severity": "Critical",
    "title": "Saga Stuck",
    "message": "Reconciliation saga has been stuck for over 2 hours.",
    "requiresAck": true,
    "groupKey": "saga:stuck:3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }'
```

---

### 6. Acknowledge Notification

Marks a notification as acknowledged by the authenticated user. This updates the `acknowledgedAt` timestamp.

**Endpoint**: `POST /api/notifications/{id}/acknowledge`

**Authentication**: Required (with ownership validation)

**Path Parameters**:
- `id` (Guid, required): The notification ID

**Request**: No body required.

**Response**: `200 OK` (empty body)

**Error Responses**:
- `401 Unauthorized`: User not authenticated
- `404 Not Found`: Notification not found or user doesn't own it
- `500 Internal Server Error`: Error acknowledging notification

**Example Usage**:
```bash
curl -X POST "http://192.168.150.52:5201/api/notifications/3fa85f64-5717-4562-b3fc-2c963f66afa6/acknowledge" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 7. Dismiss Notification

Marks a notification as dismissed by the authenticated user. Dismissed notifications are excluded from active notification queries.

**Endpoint**: `POST /api/notifications/{id}/dismiss`

**Authentication**: Required (with ownership validation)

**Path Parameters**:
- `id` (Guid, required): The notification ID

**Request**: No body required.

**Response**: `200 OK` (empty body)

**Error Responses**:
- `401 Unauthorized`: User not authenticated
- `404 Not Found`: Notification not found or user doesn't own it
- `500 Internal Server Error`: Error dismissing notification

**Example Usage**:
```bash
curl -X POST "http://192.168.150.52:5201/api/notifications/3fa85f64-5717-4562-b3fc-2c963f66afa6/dismiss" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 8. Snooze Notification

Temporarily hides a notification for a specified number of minutes. The notification will reappear after the snooze period expires.

**Endpoint**: `POST /api/notifications/{id}/snooze`

**Authentication**: Optional (snooze works without authentication, but user context is logged if available)

**Path Parameters**:
- `id` (Guid, required): The notification ID

**Query Parameters**:
- `minutes` (Int, required): Number of minutes to snooze (must be greater than 0)

**Request**: No body required.

**Response**: `200 OK` (empty body)

**Error Responses**:
- `400 Bad Request`: Minutes must be greater than 0
- `404 Not Found`: Notification not found
- `500 Internal Server Error`: Error snoozing notification

**Example Usage**:
```bash
# Snooze for 30 minutes
curl -X POST "http://192.168.150.52:5201/api/notifications/3fa85f64-5717-4562-b3fc-2c963f66afa6/snooze?minutes=30" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Snooze for 1 hour
curl -X POST "http://192.168.150.52:5201/api/notifications/3fa85f64-5717-4562-b3fc-2c963f66afa6/snooze?minutes=60" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Common Workflows

### Workflow 1: Display Active Notifications in UI

1. Call `GET /api/notifications/active` when user logs in
2. Display notifications in a badge/bell icon with count
3. Show notification details in a dropdown or panel
4. Render action buttons based on `actions` array
5. Poll periodically (e.g., every 30 seconds) or use SignalR for real-time updates

### Workflow 2: Create Saga Stuck Notification

1. Background monitor detects saga stuck in same state for > 2 hours
2. Call `POST /api/notifications/create-or-update` with:
   - `groupKey`: `saga:stuck:{sagaId}`
   - `severity`: `Critical`
   - `requiresAck`: `true`
   - `repeatInterval`: `15` (repeat every 15 minutes)
3. Notification is dispatched via SignalR, email, and SMS
4. Each repeat increments `groupCount` instead of creating duplicate notifications

### Workflow 3: User Acknowledges and Dismisses

1. User sees notification in UI
2. User clicks "Acknowledge" button
3. Call `POST /api/notifications/{id}/acknowledge`
4. Notification remains visible but marked as acknowledged
5. User clicks "Dismiss" button
6. Call `POST /api/notifications/{id}/dismiss`
7. Notification is removed from active list

### Workflow 4: Snooze Notification

1. User sees notification but isn't ready to address it
2. User clicks "Snooze" and selects duration (e.g., 30 minutes)
3. Call `POST /api/notifications/{id}/snooze?minutes=30`
4. Notification is hidden from active list
5. After 30 minutes, notification reappears in active list

---

## Data Models

### Notification Severity Enum

```csharp
public enum NotificationSeverity
{
    Info,      // Informational notification
    Warning,   // Warning notification
    Urgent,    // Urgent notification requiring attention
    Critical   // Critical notification requiring immediate action
}
```

### Notification Action Object

```csharp
public class NotificationAction
{
    public string Label { get; set; }      // Display label for button
    public string Action { get; set; }     // "navigate", "api_call", "dismiss"
    public string Target { get; set; }     // URL or API endpoint
    public string Variant { get; set; }    // "primary", "secondary", "danger"
}
```

---

## Best Practices

### 1. Use GroupKey for Deduplication

Always use a meaningful `groupKey` for notifications that might be triggered multiple times:
- Saga issues: `saga:stuck:{sagaId}`, `saga:failed:{sagaId}`
- File processing: `file:delayed:{fileId}`, `file:error:{fileId}`
- System health: `health:{serviceName}:{issue}`

### 2. Set Appropriate Severity Levels

- **Info**: Routine operations completed successfully
- **Warning**: Non-critical issues that may need attention
- **Urgent**: Time-sensitive issues requiring prompt action
- **Critical**: System failures or blocking issues requiring immediate intervention

### 3. Include Actionable Buttons

Provide clear actions users can take:
```json
{
  "actions": [
    {
      "label": "View Details",
      "action": "navigate",
      "target": "/sagas/123",
      "variant": "primary"
    },
    {
      "label": "Retry Now",
      "action": "api_call",
      "target": "/api/sagas/123/retry",
      "variant": "secondary"
    },
    {
      "label": "Ignore",
      "action": "dismiss",
      "target": null,
      "variant": "danger"
    }
  ]
}
```

### 4. Use Metadata for Context

Store additional context in the `metadata` field:
```json
{
  "metadata": {
    "fileName": "census_2025_12_18.csv",
    "clientName": "Client XYZ",
    "processingTimeMinutes": 45,
    "errorCode": "TIMEOUT",
    "retryCount": 3
  }
}
```

### 5. Set Expiration for Time-Sensitive Notifications

Use `expiresAt` for notifications that become irrelevant after a certain time:
```json
{
  "expiresAt": "2025-12-18T18:00:00Z"  // Expires at end of business day
}
```

### 6. Use RepeatInterval for Persistent Issues

For ongoing issues, set a repeat interval to remind users without spamming:
```json
{
  "repeatInterval": 30,  // Repeat every 30 minutes
  "groupKey": "saga:stuck:123"  // Use groupKey to update same notification
}
```

---

## Error Handling

All endpoints follow consistent error response patterns:

### 400 Bad Request
```json
{
  "error": "Minutes must be greater than 0"
}
```

### 401 Unauthorized
```json
{
  "error": "User not authenticated"
}
```

### 403 Forbidden
```json
{
  "error": "Access denied"
}
```

### 404 Not Found
```json
{
  "error": "Notification 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found"
}
```

### 500 Internal Server Error
```json
{
  "error": "Error creating notification"
}
```

---

## Additional Resources

- **SignalR Integration**: See [SignalR Hub Documentation](./signalr-hub.md) for real-time notification delivery
- **Email Notifications**: See [Email Templates](./email-templates.md) for notification email formatting
- **SMS Notifications**: See [SMS Integration](./sms-integration.md) for SMS notification setup
- **Notification Service Architecture**: See [Architecture Overview](../architecture/notification-service-architecture.md)

---

## Support

For issues or questions:
- Create an issue in the project repository
- Contact the ops team at `ops@plansource.com`
- Check the [Troubleshooting Guide](./troubleshooting.md)
