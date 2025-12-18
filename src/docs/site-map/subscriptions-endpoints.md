# Subscriptions API Endpoints

## Overview

The Subscriptions API provides endpoints for managing user notification subscriptions within the PlanSource Automation platform. Users can subscribe to specific clients, sagas, or all notifications, and configure minimum severity levels for their subscriptions.

**Base URL**: `http://192.168.150.52:5201/api/subscriptions`

**Authentication**: All endpoints require authentication via JWT token. The authenticated user's ID is extracted from the `ClaimTypes.NameIdentifier` claim.

**Phase**: Phase 2 feature (advanced subscription management)

---

## Endpoints

### 1. Get User Subscriptions

Retrieves all notification subscriptions for the authenticated user. This includes subscriptions to specific clients, specific sagas, or global subscriptions.

**Endpoint**: `GET /api/subscriptions`

**Authentication**: Required

**Request**: No parameters required.

**Response**: `200 OK`
```json
[
  {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "a1b2c3d4-5e6f-7890-abcd-ef1234567890",
    "sagaId": null,
    "minSeverity": "Warning"
  },
  {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": null,
    "sagaId": null,
    "minSeverity": "Critical"
  },
  {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "a1b2c3d4-5e6f-7890-abcd-ef1234567890",
    "sagaId": "b2c3d4e5-6f78-9012-bcde-f12345678901",
    "minSeverity": "Info"
  }
]
```

**Response Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `userId` | Guid | User ID who owns this subscription |
| `clientId` | Guid (nullable) | Client ID to subscribe to. `null` = all clients |
| `sagaId` | Guid (nullable) | Saga ID to subscribe to. `null` = all sagas for the client |
| `minSeverity` | Enum | Minimum severity level: `Info`, `Warning`, `Urgent`, `Critical` |

**Subscription Scope Examples**:
- `clientId: null, sagaId: null` - Subscribe to ALL notifications (global subscription)
- `clientId: {guid}, sagaId: null` - Subscribe to all sagas for a specific client
- `clientId: {guid}, sagaId: {guid}` - Subscribe to a specific saga for a specific client

**Error Responses**:
- `401 Unauthorized`: User not authenticated
- `500 Internal Server Error`: Error retrieving subscriptions

**Example Usage**:
```bash
curl -X GET "http://192.168.150.52:5201/api/subscriptions" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 2. Create or Update Subscription

Creates a new notification subscription or updates an existing one for the authenticated user. Subscriptions are identified by the combination of `userId`, `clientId`, and `sagaId`.

**Endpoint**: `POST /api/subscriptions`

**Authentication**: Required

**Request Body**:
```json
{
  "clientId": "a1b2c3d4-5e6f-7890-abcd-ef1234567890",
  "sagaId": null,
  "minSeverity": "Warning"
}
```

**Request Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `clientId` | Guid | No | Client ID to subscribe to. Omit or set to `null` for all clients |
| `sagaId` | Guid | No | Saga ID to subscribe to. Omit or set to `null` for all sagas |
| `minSeverity` | Enum | Yes | Minimum severity level: `Info`, `Warning`, `Urgent`, `Critical` |

**Behavior**:
- If a subscription with the same `userId`, `clientId`, and `sagaId` exists:
  - Updates the `minSeverity` level
  - Returns the updated subscription
- If no matching subscription exists:
  - Creates a new subscription
  - Returns the created subscription

**Response**: `200 OK`
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "a1b2c3d4-5e6f-7890-abcd-ef1234567890",
  "sagaId": null,
  "minSeverity": "Warning"
}
```

**Error Responses**:
- `401 Unauthorized`: User not authenticated
- `500 Internal Server Error`: Error creating subscription

**Example Usage**:
```bash
# Subscribe to all sagas for a specific client (Warning and above)
curl -X POST "http://192.168.150.52:5201/api/subscriptions" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "a1b2c3d4-5e6f-7890-abcd-ef1234567890",
    "sagaId": null,
    "minSeverity": "Warning"
  }'

# Subscribe to a specific saga (Info and above)
curl -X POST "http://192.168.150.52:5201/api/subscriptions" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "a1b2c3d4-5e6f-7890-abcd-ef1234567890",
    "sagaId": "b2c3d4e5-6f78-9012-bcde-f12345678901",
    "minSeverity": "Info"
  }'

# Subscribe to all notifications globally (Critical only)
curl -X POST "http://192.168.150.52:5201/api/subscriptions" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": null,
    "sagaId": null,
    "minSeverity": "Critical"
  }'
```

---

### 3. Delete Subscription

Removes a notification subscription for the authenticated user. The subscription is identified by `clientId` and/or `sagaId` query parameters.

**Endpoint**: `DELETE /api/subscriptions`

**Authentication**: Required

**Query Parameters**:
- `clientId` (Guid, optional): Client ID to unsubscribe from
- `sagaId` (Guid, optional): Saga ID to unsubscribe from

**Parameter Combinations**:
- `clientId=null&sagaId=null` - Delete global subscription (all notifications)
- `clientId={guid}&sagaId=null` - Delete subscription to all sagas for a specific client
- `clientId={guid}&sagaId={guid}` - Delete subscription to a specific saga

**Request**: No body required.

**Response**: `200 OK` (empty body)

**Error Responses**:
- `401 Unauthorized`: User not authenticated
- `500 Internal Server Error`: Error deleting subscription

**Example Usage**:
```bash
# Unsubscribe from all sagas for a specific client
curl -X DELETE "http://192.168.150.52:5201/api/subscriptions?clientId=a1b2c3d4-5e6f-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Unsubscribe from a specific saga
curl -X DELETE "http://192.168.150.52:5201/api/subscriptions?clientId=a1b2c3d4-5e6f-7890-abcd-ef1234567890&sagaId=b2c3d4e5-6f78-9012-bcde-f12345678901" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Unsubscribe from all notifications globally
curl -X DELETE "http://192.168.150.52:5201/api/subscriptions" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Common Workflows

### Workflow 1: Subscribe to Client Notifications

A user wants to receive notifications for all sagas related to a specific client.

1. User selects client from UI dropdown
2. User selects minimum severity level (e.g., "Warning")
3. Call `POST /api/subscriptions`:
   ```json
   {
     "clientId": "a1b2c3d4-5e6f-7890-abcd-ef1234567890",
     "sagaId": null,
     "minSeverity": "Warning"
   }
   ```
4. User now receives notifications for all sagas related to this client where severity is Warning or higher

### Workflow 2: Subscribe to Specific Saga

A user wants to monitor a specific saga closely and receive all notifications (Info and above).

1. User navigates to saga details page
2. User clicks "Subscribe to this Saga"
3. Call `POST /api/subscriptions`:
   ```json
   {
     "clientId": "a1b2c3d4-5e6f-7890-abcd-ef1234567890",
     "sagaId": "b2c3d4e5-6f78-9012-bcde-f12345678901",
     "minSeverity": "Info"
   }
   ```
4. User receives all notifications for this specific saga

### Workflow 3: Update Severity Level

A user wants to reduce notification noise by increasing the minimum severity level.

1. User views their current subscriptions (`GET /api/subscriptions`)
2. User identifies subscription with too many notifications
3. Call `POST /api/subscriptions` with higher severity:
   ```json
   {
     "clientId": "a1b2c3d4-5e6f-7890-abcd-ef1234567890",
     "sagaId": null,
     "minSeverity": "Critical"  // Changed from "Warning" to "Critical"
   }
   ```
4. Existing subscription is updated with new severity level

### Workflow 4: Manage Multiple Subscriptions

A user manages multiple client subscriptions with different severity levels.

1. Call `GET /api/subscriptions` to view all current subscriptions
2. Display subscriptions in a table/list format:
   - Global subscription (all clients, Critical only)
   - Client A (all sagas, Warning and above)
   - Client B, Saga X (all severity levels)
3. User can add, update, or delete subscriptions as needed

### Workflow 5: Unsubscribe from Notifications

A user no longer wants to receive notifications for a specific client.

1. User navigates to subscription management page
2. User selects subscription to delete
3. Call `DELETE /api/subscriptions?clientId={clientId}`
4. Subscription is removed, user stops receiving notifications for that client

---

## Data Models

### NotificationSubscription

```csharp
public class NotificationSubscription
{
    public Guid UserId { get; set; }          // User ID who owns this subscription
    public Guid? ClientId { get; set; }       // Client ID (NULL = all clients)
    public Guid? SagaId { get; set; }         // Saga ID (NULL = all sagas)
    public NotificationSeverity MinSeverity { get; set; }  // Minimum severity level
}
```

### NotificationSeverity Enum

```csharp
public enum NotificationSeverity
{
    Info,      // Informational notification
    Warning,   // Warning notification
    Urgent,    // Urgent notification requiring attention
    Critical   // Critical notification requiring immediate action
}
```

**Severity Hierarchy**:
- `Info` < `Warning` < `Urgent` < `Critical`
- A subscription with `minSeverity: Warning` will receive `Warning`, `Urgent`, and `Critical` notifications
- A subscription with `minSeverity: Critical` will only receive `Critical` notifications

### SubscribeRequest

```csharp
public class SubscribeRequest
{
    public Guid? ClientId { get; set; }       // Client ID to subscribe to (null = all)
    public Guid? SagaId { get; set; }         // Saga ID to subscribe to (null = all)
    public NotificationSeverity MinSeverity { get; set; }  // Minimum severity
}
```

---

## Subscription Filtering Logic

When a notification is created, the system determines which users should receive it based on their subscriptions:

### Matching Rules

1. **Global Subscription** (`clientId: null, sagaId: null`):
   - Matches ALL notifications
   - Filtered by `minSeverity` level

2. **Client-Level Subscription** (`clientId: {guid}, sagaId: null`):
   - Matches all notifications for the specified client
   - Applies to all sagas under that client
   - Filtered by `minSeverity` level

3. **Saga-Level Subscription** (`clientId: {guid}, sagaId: {guid}`):
   - Matches only notifications for the specific saga
   - Most specific subscription type
   - Filtered by `minSeverity` level

### Priority Rules

When multiple subscriptions match a notification, the **most specific** subscription takes precedence:

1. Saga-level subscription (highest priority)
2. Client-level subscription
3. Global subscription (lowest priority)

**Example**:
- User has global subscription with `minSeverity: Critical`
- User has client-level subscription with `minSeverity: Warning`
- User has saga-level subscription with `minSeverity: Info`
- A notification for that specific saga with severity `Info` **will be sent** (saga-level subscription wins)

---

## Best Practices

### 1. Start with Client-Level Subscriptions

For most users, client-level subscriptions provide the right balance:
```json
{
  "clientId": "a1b2c3d4-5e6f-7890-abcd-ef1234567890",
  "sagaId": null,
  "minSeverity": "Warning"
}
```

This allows users to:
- Monitor all sagas for clients they care about
- Filter out low-priority Info messages
- Receive important warnings and critical alerts

### 2. Use Saga-Level Subscriptions for Active Issues

When investigating a specific problem, create a saga-level subscription:
```json
{
  "clientId": "a1b2c3d4-5e6f-7890-abcd-ef1234567890",
  "sagaId": "b2c3d4e5-6f78-9012-bcde-f12345678901",
  "minSeverity": "Info"
}
```

This provides:
- Detailed visibility into a specific workflow
- All information-level messages for troubleshooting
- Temporary subscription that can be deleted when issue is resolved

### 3. Reserve Global Subscriptions for Ops Team

Global subscriptions should typically be limited to operations team members:
```json
{
  "clientId": null,
  "sagaId": null,
  "minSeverity": "Critical"
}
```

This ensures:
- Ops team sees all critical system issues
- Prevents notification overload for individual users
- Clear escalation path for system-wide problems

### 4. Adjust Severity Based on Role

Different roles should use different severity thresholds:

**Operations Team**:
```json
{ "minSeverity": "Warning" }  // See warnings and above
```

**Client Success Team**:
```json
{ "minSeverity": "Urgent" }  // Only urgent client issues
```

**Executive/Management**:
```json
{ "minSeverity": "Critical" }  // Only critical system failures
```

### 5. Clean Up Old Subscriptions

Regularly review and delete subscriptions for:
- Resolved issues (saga-level subscriptions)
- Inactive clients
- Completed projects

This prevents:
- Notification fatigue
- Wasted system resources
- Confusion about current responsibilities

### 6. Provide UI Helpers

When building subscription management UI:
- Show notification counts per subscription (last 7 days)
- Highlight subscriptions with zero activity
- Provide "Quick Subscribe" buttons on client/saga pages
- Allow bulk operations (subscribe to multiple clients)

---

## UI Integration Examples

### Subscription Management Table

```typescript
interface Subscription {
  userId: string;
  clientId?: string;
  sagaId?: string;
  minSeverity: 'Info' | 'Warning' | 'Urgent' | 'Critical';
}

// Display user's subscriptions
function SubscriptionList({ subscriptions }: { subscriptions: Subscription[] }) {
  return (
    <table>
      <thead>
        <tr>
          <th>Scope</th>
          <th>Client</th>
          <th>Saga</th>
          <th>Min Severity</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        {subscriptions.map(sub => (
          <tr key={`${sub.clientId}-${sub.sagaId}`}>
            <td>{getScope(sub)}</td>
            <td>{sub.clientId ? getClientName(sub.clientId) : 'All Clients'}</td>
            <td>{sub.sagaId ? getSagaName(sub.sagaId) : 'All Sagas'}</td>
            <td>
              <SeverityBadge severity={sub.minSeverity} />
            </td>
            <td>
              <button onClick={() => updateSeverity(sub)}>Edit</button>
              <button onClick={() => deleteSubscription(sub)}>Delete</button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function getScope(sub: Subscription): string {
  if (!sub.clientId && !sub.sagaId) return 'Global';
  if (sub.clientId && !sub.sagaId) return 'Client';
  return 'Saga';
}
```

### Quick Subscribe Button

```typescript
function SagaDetailsPage({ saga }: { saga: Saga }) {
  const [isSubscribed, setIsSubscribed] = useState(false);

  async function toggleSubscription() {
    if (isSubscribed) {
      // Unsubscribe
      await fetch(`/api/subscriptions?clientId=${saga.clientId}&sagaId=${saga.id}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${token}` }
      });
      setIsSubscribed(false);
    } else {
      // Subscribe
      await fetch('/api/subscriptions', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          clientId: saga.clientId,
          sagaId: saga.id,
          minSeverity: 'Info'
        })
      });
      setIsSubscribed(true);
    }
  }

  return (
    <div>
      <h1>Saga Details</h1>
      <button onClick={toggleSubscription}>
        {isSubscribed ? 'Unsubscribe' : 'Subscribe to Notifications'}
      </button>
    </div>
  );
}
```

### Severity Selector Component

```typescript
function SeveritySelector({ value, onChange }: {
  value: NotificationSeverity,
  onChange: (severity: NotificationSeverity) => void
}) {
  const severities: NotificationSeverity[] = ['Info', 'Warning', 'Urgent', 'Critical'];

  return (
    <select value={value} onChange={e => onChange(e.target.value as NotificationSeverity)}>
      {severities.map(severity => (
        <option key={severity} value={severity}>
          {severity} and above
        </option>
      ))}
    </select>
  );
}
```

---

## Error Handling

All endpoints follow consistent error response patterns:

### 401 Unauthorized
```json
{
  "error": "User not authenticated"
}
```

**Cause**: Missing or invalid JWT token

**Resolution**:
- Ensure user is logged in
- Check that JWT token is included in `Authorization` header
- Verify token hasn't expired

### 500 Internal Server Error
```json
{
  "error": "Error retrieving subscriptions"
}
```

**Cause**: Database error or internal service failure

**Resolution**:
- Check server logs for detailed error information
- Verify database connectivity
- Contact ops team if issue persists

---

## Integration with Notifications API

Subscriptions work in conjunction with the Notifications API to deliver targeted notifications:

### How It Works

1. **Notification Created**: A new notification is created via `POST /api/notifications`
2. **Subscription Matching**: The system finds all subscriptions that match:
   - User subscriptions with matching `clientId` and/or `sagaId`
   - Subscriptions where notification severity >= `minSeverity`
3. **Delivery**: Notification is dispatched to matched users via:
   - SignalR (real-time)
   - Email (if enabled)
   - SMS (if enabled)

### Example Flow

```
1. Saga enters "Stuck" state
   └─> Creates notification: clientId=ABC, sagaId=123, severity=Critical

2. System finds matching subscriptions:
   └─> User A: Global subscription (minSeverity=Critical) ✓
   └─> User B: Client subscription (clientId=ABC, minSeverity=Warning) ✓
   └─> User C: Saga subscription (clientId=ABC, sagaId=123, minSeverity=Info) ✓
   └─> User D: Client subscription (clientId=XYZ, minSeverity=Info) ✗

3. Notification delivered to Users A, B, and C via enabled channels
```

---

## Additional Resources

- **Notifications API**: See [Notifications Endpoints](./notifications-endpoints.md) for creating and managing notifications
- **SignalR Integration**: See [SignalR Hub Documentation](./signalr-hub.md) for real-time notification delivery
- **User Preferences API**: See [Preferences Endpoints](./preferences-endpoints.md) for channel-specific preferences
- **Architecture Overview**: See [Notification Service Architecture](../architecture/notification-service-architecture.md)

---

## Support

For issues or questions:
- Create an issue in the project repository
- Contact the ops team at `ops@plansource.com`
- Check the [Troubleshooting Guide](./troubleshooting.md)
