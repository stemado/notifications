# SignalR NotificationHub Documentation

## Overview

The NotificationHub provides real-time, bidirectional communication between the NotificationService.Api and client applications. It enables instant notification delivery, acknowledgment tracking, and user-specific notification management through WebSocket connections.

**Hub Path:** `/hubs/notifications`

**Service URL:** `http://192.168.150.52:5201/hubs/notifications`

## Architecture

### Communication Pattern

- **Server-to-Client Events:** Push notifications to connected users via groups
- **Client-to-Server Methods:** Acknowledge, dismiss, snooze notifications, retrieve active notifications
- **Group-Based Routing:** User-specific, tenant-based, and ops-team groups for targeted delivery

### Automatic Group Assignment

When a user connects, they are automatically added to groups based on their authentication claims:

| Group Pattern | Assignment Criteria | Purpose |
|---------------|---------------------|---------|
| `user:{userId}` | All authenticated users | User-specific notifications |
| `tenant:{tenantId}` | Users with TenantId claim | Client portal notifications |
| `ops-team` | Users in "Operations" role | Operations team broadcasts |

## Authentication

### JWT Bearer Authentication

The hub requires JWT authentication with support for both Keycloak and symmetric key providers.

**Required Claims:**
- `NameIdentifier`: User ID (GUID format)
- `TenantId` (optional): For multi-tenant routing
- `Role` (optional): For ops-team group membership

### Query String Authentication

SignalR connections pass the JWT token via query string:

```
?access_token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

This is handled automatically by the authentication middleware for all requests to `/hubs/*` paths.

## Connection Lifecycle

### OnConnectedAsync

**Automatic Actions:**
1. Extract user ID from `NameIdentifier` claim
2. Add connection to `user:{userId}` group
3. If `TenantId` claim exists, add to `tenant:{tenantId}` group
4. If user has "Operations" role, add to `ops-team` group
5. Log connection with user ID and connection ID

**Client Impact:**
- Connection is ready to receive notifications immediately
- No manual group joining required for standard scenarios

### OnDisconnectedAsync

**Automatic Actions:**
1. Log disconnection (with error details if applicable)
2. Groups are automatically cleaned up by SignalR framework

**Exception Handling:**
- Network errors are logged but do not prevent disconnection
- Reconnection attempts should be handled by the client

## Server-to-Client Events

### NewNotification

**Description:** Sent when a new notification is created for the user.

**Payload:** Full `Notification` object

**Event Name:** `"NewNotification"`

**Routing:**
- Sent to `user:{userId}` group (always)
- If `TenantId` is null: Also sent to `ops-team` group
- If `TenantId` is set: Also sent to `tenant:{tenantId}` group

**Notification Structure:**

```typescript
interface Notification {
  id: string;                    // Notification GUID
  userId: string;                // Target user GUID
  tenantId: string | null;       // Tenant GUID (null = ops team)

  // Content
  severity: NotificationSeverity;
  title: string;                 // Max 200 characters
  message: string;

  // Source tracking
  sagaId: string | null;
  clientId: string | null;
  eventId: string | null;
  eventType: string | null;

  // Lifecycle timestamps
  createdAt: string;             // ISO 8601
  acknowledgedAt: string | null; // ISO 8601
  dismissedAt: string | null;    // ISO 8601
  expiresAt: string | null;      // ISO 8601

  // Behavior
  repeatInterval: number | null; // Minutes
  lastRepeatedAt: string | null; // ISO 8601
  requiresAck: boolean;

  // Grouping
  groupKey: string | null;
  groupCount: number;

  // Actions and metadata
  actions: NotificationAction[];
  metadata: Record<string, any>;
}

enum NotificationSeverity {
  Info = 0,
  Warning = 1,
  Urgent = 2,
  Critical = 3
}

interface NotificationAction {
  label: string;        // Button text
  action: string;       // "navigate", "api_call", "dismiss"
  target: string | null; // URL or API endpoint
  variant: string;      // "primary", "secondary", "danger"
}
```

## Client-to-Server Methods

### AcknowledgeNotification

**Description:** Mark a notification as acknowledged by the user.

**Method Name:** `"AcknowledgeNotification"`

**Parameters:**
- `notificationId` (Guid/string): ID of the notification to acknowledge

**Returns:** Task (void)

**Behavior:**
- Sets `AcknowledgedAt` timestamp
- User must own the notification (matching user ID)
- Throws `HubException` if authentication fails or notification not found

**Example:**
```typescript
await connection.invoke("AcknowledgeNotification", notificationId);
```

### DismissNotification

**Description:** Dismiss a notification (removes from active list).

**Method Name:** `"DismissNotification"`

**Parameters:**
- `notificationId` (Guid/string): ID of the notification to dismiss

**Returns:** Task (void)

**Behavior:**
- Sets `DismissedAt` timestamp
- User must own the notification
- Dismissed notifications are excluded from active queries
- Throws `HubException` on error

**Example:**
```typescript
await connection.invoke("DismissNotification", notificationId);
```

### SnoozeNotification

**Description:** Temporarily hide a notification for a specified duration.

**Method Name:** `"SnoozeNotification"`

**Parameters:**
- `notificationId` (Guid/string): ID of the notification to snooze
- `minutes` (number): Number of minutes to snooze

**Returns:** Task (void)

**Behavior:**
- Updates notification snooze state
- Notification will reappear after the snooze period
- No ownership check (can snooze any notification)
- Throws `HubException` on error

**Example:**
```typescript
await connection.invoke("SnoozeNotification", notificationId, 15);
```

### GetActiveNotifications

**Description:** Retrieve all active notifications for the authenticated user on connection.

**Method Name:** `"GetActiveNotifications"`

**Parameters:** None

**Returns:** `Task<List<Notification>>`

**Behavior:**
- Returns notifications where:
  - `DismissedAt` is null
  - `ExpiresAt` is null or in the future
  - `UserId` matches authenticated user
- Sorted by creation date (newest first)
- Throws `HubException` if not authenticated

**Example:**
```typescript
const activeNotifications = await connection.invoke("GetActiveNotifications");
```

## Client Implementation Examples

### JavaScript/TypeScript (SignalR Client)

#### Installation

```bash
npm install @microsoft/signalr
```

#### Basic Connection Setup

```typescript
import * as signalR from "@microsoft/signalr";

// Store the JWT token (from login response)
const accessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";

// Create the connection
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://192.168.150.52:5201/hubs/notifications", {
    accessTokenFactory: () => accessToken
  })
  .withAutomaticReconnect({
    nextRetryDelayInMilliseconds: retryContext => {
      // Exponential backoff: 0ms, 2s, 10s, 30s, then every 60s
      if (retryContext.previousRetryCount === 0) return 0;
      if (retryContext.previousRetryCount === 1) return 2000;
      if (retryContext.previousRetryCount === 2) return 10000;
      if (retryContext.previousRetryCount === 3) return 30000;
      return 60000;
    }
  })
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Handle reconnection events
connection.onreconnecting(error => {
  console.warn("Connection lost. Reconnecting...", error);
  // Update UI: show "reconnecting" indicator
});

connection.onreconnected(connectionId => {
  console.log("Reconnected with ID:", connectionId);
  // Update UI: hide "reconnecting" indicator
  // Refresh active notifications
  refreshNotifications();
});

connection.onclose(error => {
  console.error("Connection closed:", error);
  // Update UI: show "disconnected" state
});
```

#### Listening for Notifications

```typescript
// Listen for new notifications
connection.on("NewNotification", (notification) => {
  console.log("New notification received:", notification);

  // Add to notification state
  addNotificationToUI(notification);

  // Show toast/alert based on severity
  switch (notification.severity) {
    case 3: // Critical
      showCriticalAlert(notification);
      break;
    case 2: // Urgent
      showUrgentToast(notification);
      break;
    case 1: // Warning
      showWarningToast(notification);
      break;
    default: // Info
      showInfoToast(notification);
  }

  // Play sound for urgent/critical
  if (notification.severity >= 2) {
    playNotificationSound();
  }

  // Auto-acknowledge if not required
  if (!notification.requiresAck) {
    setTimeout(() => {
      connection.invoke("AcknowledgeNotification", notification.id);
    }, 3000);
  }
});
```

#### Starting the Connection

```typescript
async function startConnection() {
  try {
    await connection.start();
    console.log("SignalR connected!");

    // Load active notifications on connect
    const activeNotifications = await connection.invoke("GetActiveNotifications");
    console.log(`Loaded ${activeNotifications.length} active notifications`);

    // Display in UI
    activeNotifications.forEach(notification => {
      addNotificationToUI(notification);
    });

  } catch (error) {
    console.error("Error connecting to SignalR:", error);
    // Retry after delay
    setTimeout(() => startConnection(), 5000);
  }
}

// Start the connection
startConnection();
```

#### Interacting with Notifications

```typescript
// Acknowledge a notification
async function acknowledgeNotification(notificationId: string) {
  try {
    await connection.invoke("AcknowledgeNotification", notificationId);
    console.log(`Notification ${notificationId} acknowledged`);
    updateNotificationInUI(notificationId, { acknowledgedAt: new Date().toISOString() });
  } catch (error) {
    console.error("Failed to acknowledge notification:", error);
  }
}

// Dismiss a notification
async function dismissNotification(notificationId: string) {
  try {
    await connection.invoke("DismissNotification", notificationId);
    console.log(`Notification ${notificationId} dismissed`);
    removeNotificationFromUI(notificationId);
  } catch (error) {
    console.error("Failed to dismiss notification:", error);
  }
}

// Snooze a notification
async function snoozeNotification(notificationId: string, minutes: number) {
  try {
    await connection.invoke("SnoozeNotification", notificationId, minutes);
    console.log(`Notification ${notificationId} snoozed for ${minutes} minutes`);
    removeNotificationFromUI(notificationId);

    // Re-add after snooze period
    setTimeout(() => {
      refreshNotifications();
    }, minutes * 60 * 1000);
  } catch (error) {
    console.error("Failed to snooze notification:", error);
  }
}

// Refresh active notifications
async function refreshNotifications() {
  try {
    const notifications = await connection.invoke("GetActiveNotifications");
    updateNotificationListUI(notifications);
  } catch (error) {
    console.error("Failed to refresh notifications:", error);
  }
}
```

#### React Hook Example

```typescript
import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";

interface Notification {
  id: string;
  severity: number;
  title: string;
  message: string;
  requiresAck: boolean;
  // ... other fields
}

export function useNotificationHub(accessToken: string) {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [isReconnecting, setIsReconnecting] = useState(false);

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl("http://192.168.150.52:5201/hubs/notifications", {
        accessTokenFactory: () => accessToken
      })
      .withAutomaticReconnect()
      .build();

    // Connection events
    newConnection.onreconnecting(() => setIsReconnecting(true));
    newConnection.onreconnected(() => {
      setIsReconnecting(false);
      setIsConnected(true);
      loadActiveNotifications(newConnection);
    });
    newConnection.onclose(() => setIsConnected(false));

    // Listen for new notifications
    newConnection.on("NewNotification", (notification: Notification) => {
      setNotifications(prev => [notification, ...prev]);
    });

    // Start connection
    newConnection.start()
      .then(() => {
        setIsConnected(true);
        loadActiveNotifications(newConnection);
      })
      .catch(err => console.error("SignalR connection error:", err));

    setConnection(newConnection);

    // Cleanup
    return () => {
      newConnection.stop();
    };
  }, [accessToken]);

  async function loadActiveNotifications(conn: signalR.HubConnection) {
    try {
      const active = await conn.invoke<Notification[]>("GetActiveNotifications");
      setNotifications(active);
    } catch (error) {
      console.error("Failed to load notifications:", error);
    }
  }

  const acknowledge = async (notificationId: string) => {
    if (!connection) return;
    await connection.invoke("AcknowledgeNotification", notificationId);
    setNotifications(prev =>
      prev.map(n => n.id === notificationId ? { ...n, acknowledgedAt: new Date().toISOString() } : n)
    );
  };

  const dismiss = async (notificationId: string) => {
    if (!connection) return;
    await connection.invoke("DismissNotification", notificationId);
    setNotifications(prev => prev.filter(n => n.id !== notificationId));
  };

  const snooze = async (notificationId: string, minutes: number) => {
    if (!connection) return;
    await connection.invoke("SnoozeNotification", notificationId, minutes);
    setNotifications(prev => prev.filter(n => n.id !== notificationId));
  };

  return {
    notifications,
    isConnected,
    isReconnecting,
    acknowledge,
    dismiss,
    snooze
  };
}
```

### C# Client Example

```csharp
using Microsoft.AspNetCore.SignalR.Client;

public class NotificationHubClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ILogger<NotificationHubClient> _logger;

    public event Action<Notification>? NotificationReceived;

    public NotificationHubClient(string hubUrl, string accessToken, ILogger<NotificationHubClient> logger)
    {
        _logger = logger;

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(accessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        // Handle new notifications
        _connection.On<Notification>("NewNotification", notification =>
        {
            _logger.LogInformation("Notification received: {Title}", notification.Title);
            NotificationReceived?.Invoke(notification);
        });

        // Connection lifecycle
        _connection.Reconnecting += error =>
        {
            _logger.LogWarning(error, "Connection lost. Reconnecting...");
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            _logger.LogInformation("Reconnected with ID: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        };

        _connection.Closed += error =>
        {
            _logger.LogError(error, "Connection closed");
            return Task.CompletedTask;
        };
    }

    public async Task StartAsync()
    {
        await _connection.StartAsync();
        _logger.LogInformation("SignalR connection started");

        // Load active notifications
        var activeNotifications = await GetActiveNotificationsAsync();
        _logger.LogInformation("Loaded {Count} active notifications", activeNotifications.Count);
    }

    public async Task<List<Notification>> GetActiveNotificationsAsync()
    {
        return await _connection.InvokeAsync<List<Notification>>("GetActiveNotifications");
    }

    public async Task AcknowledgeAsync(Guid notificationId)
    {
        await _connection.InvokeAsync("AcknowledgeNotification", notificationId);
    }

    public async Task DismissAsync(Guid notificationId)
    {
        await _connection.InvokeAsync("DismissNotification", notificationId);
    }

    public async Task SnoozeAsync(Guid notificationId, int minutes)
    {
        await _connection.InvokeAsync("SnoozeNotification", notificationId, minutes);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
```

## Real-Time Notification Use Cases

### 1. Saga Stuck Alert (Operations Team)

**Scenario:** A reconciliation saga has been stuck in the same state for 30+ minutes.

**Notification Flow:**
1. `NotificationBackupPollingJob` detects stuck saga
2. Creates notification with `Severity.Urgent`, `TenantId = null`
3. SignalR sends to `ops-team` group
4. Operations team members receive instant alert
5. Notification includes action: "View Saga" → navigate to saga details

**Client Handling:**
```typescript
connection.on("NewNotification", (notification) => {
  if (notification.eventType === "SagaStuck" && notification.severity === 2) {
    // Show prominent alert
    showUrgentBanner({
      title: notification.title,
      message: notification.message,
      action: notification.actions.find(a => a.action === "navigate")
    });
  }
});
```

### 2. Import Completed (Client Portal)

**Scenario:** A census file import completes successfully for a specific client.

**Notification Flow:**
1. Import completion event triggers notification
2. Creates notification with `Severity.Info`, `TenantId = {clientTenantId}`
3. SignalR sends to `tenant:{clientTenantId}` group
4. Client users receive success notification
5. Auto-acknowledged after 5 seconds

**Client Handling:**
```typescript
connection.on("NewNotification", (notification) => {
  if (notification.eventType === "ImportCompleted" && !notification.requiresAck) {
    showSuccessToast(notification.title, 5000);
    setTimeout(() => {
      connection.invoke("AcknowledgeNotification", notification.id);
    }, 5000);
  }
});
```

### 3. Critical Deployment Alert (Individual User)

**Scenario:** A deployment is about to occur that affects a user's active work.

**Notification Flow:**
1. Deployment service creates targeted notification
2. Notification has `Severity.Critical`, `RequiresAck = true`
3. SignalR sends to `user:{userId}` group
4. User receives modal alert requiring acknowledgment
5. User must acknowledge before continuing work

**Client Handling:**
```typescript
connection.on("NewNotification", (notification) => {
  if (notification.severity === 3 && notification.requiresAck) {
    showModalAlert({
      title: notification.title,
      message: notification.message,
      actions: notification.actions,
      onAcknowledge: async () => {
        await connection.invoke("AcknowledgeNotification", notification.id);
        closeModal();
      }
    });
  }
});
```

### 4. Grouped Notifications (Repeated Errors)

**Scenario:** Multiple file processing errors occur for the same client.

**Notification Flow:**
1. First error creates notification with `GroupKey = "file-error:{clientId}"`
2. Subsequent errors increment `GroupCount` on existing notification
3. SignalR sends updated notification
4. Client displays single notification with count badge
5. User can expand to see grouped error details

**Client Handling:**
```typescript
connection.on("NewNotification", (notification) => {
  if (notification.groupKey && notification.groupCount > 1) {
    // Update existing notification instead of creating new one
    const existingIndex = notifications.findIndex(n => n.groupKey === notification.groupKey);
    if (existingIndex >= 0) {
      notifications[existingIndex] = notification;
      showToast(`${notification.title} (${notification.groupCount} occurrences)`);
    }
  }
});
```

## Group Management

### User Groups

**Pattern:** `user:{userId}`

**Membership:** Automatic on connection

**Use Case:** Personal notifications, direct messages, user-specific alerts

**Example:**
- "Your import template is ready"
- "Password expiring in 7 days"
- "New message from support"

### Tenant Groups

**Pattern:** `tenant:{tenantId}`

**Membership:** Automatic if `TenantId` claim exists

**Use Case:** Client portal notifications, organization-wide updates

**Example:**
- "Census file received"
- "Monthly report available"
- "System maintenance scheduled"

### Operations Team

**Pattern:** `ops-team`

**Membership:** Automatic if user has "Operations" role

**Use Case:** Internal alerts, saga monitoring, system health

**Example:**
- "Saga stuck in ProcessingFile state"
- "Database connection pool exhausted"
- "Critical error threshold exceeded"

### Custom Groups (Not Currently Implemented)

The hub doesn't expose manual `JoinGroup`/`LeaveGroup` methods. All group membership is claim-based and automatic. Future enhancements could add:

```typescript
// Future API (not yet implemented)
await connection.invoke("JoinGroup", "alerts:high-priority");
await connection.invoke("LeaveGroup", "alerts:low-priority");
```

## Error Handling

### HubException

All hub methods throw `HubException` on error with descriptive messages.

**Client Handling:**
```typescript
try {
  await connection.invoke("AcknowledgeNotification", notificationId);
} catch (error) {
  if (error instanceof Error) {
    console.error("Hub error:", error.message);
    // Display user-friendly error
    showError(`Failed to acknowledge notification: ${error.message}`);
  }
}
```

### Connection Errors

**Network Interruptions:**
- Automatic reconnection with exponential backoff
- Client should display reconnection status
- Refresh notifications on reconnection

**Authentication Failures:**
- Connection will fail if token is invalid/expired
- Client should refresh token and reconnect
- Display login prompt if refresh fails

**Example:**
```typescript
connection.onclose(async (error) => {
  if (error?.message.includes("Unauthorized")) {
    // Token expired - refresh and reconnect
    try {
      const newToken = await refreshAuthToken();
      // Create new connection with new token
      await recreateConnection(newToken);
    } catch {
      // Refresh failed - redirect to login
      redirectToLogin();
    }
  }
});
```

## Performance Considerations

### Connection Limits

- Each user can have multiple connections (multi-tab/device)
- Server manages connection pooling
- No artificial connection limit per user

### Message Size

- Notification objects are typically 1-5 KB
- Actions and metadata can increase size
- No compression (WebSocket handles framing)

### Scalability

**Current Architecture:**
- Single server instance
- In-memory group tracking
- Suitable for 100-1000 concurrent connections

**Future Enhancements:**
- Redis backplane for multi-server scaling
- Azure SignalR Service for cloud deployments
- Message batching for high-volume scenarios

### Best Practices

**Client-Side:**
1. Use automatic reconnection
2. Debounce rapid acknowledge/dismiss actions
3. Cache active notifications locally
4. Implement optimistic UI updates
5. Batch notification refreshes

**Example:**
```typescript
// Debounced acknowledge function
const debouncedAcknowledge = debounce(async (notificationId: string) => {
  await connection.invoke("AcknowledgeNotification", notificationId);
}, 500);

// Optimistic UI update
function dismissNotification(notificationId: string) {
  // Update UI immediately
  removeNotificationFromUI(notificationId);

  // Send to server
  connection.invoke("DismissNotification", notificationId)
    .catch(error => {
      // Revert on error
      console.error("Dismiss failed:", error);
      addNotificationBackToUI(notificationId);
    });
}
```

## Troubleshooting

### Connection Issues

**Problem:** Connection fails immediately

**Solutions:**
1. Verify service is running: `http://192.168.150.52:5201/health`
2. Check JWT token is valid (not expired)
3. Verify token includes required claims
4. Check CORS configuration if connecting from browser
5. Verify network connectivity to port 5201

**Problem:** Connection drops frequently

**Solutions:**
1. Check network stability
2. Increase reconnection delays
3. Verify firewall isn't closing idle connections
4. Check server logs for errors

### Not Receiving Notifications

**Problem:** Other users receive notifications but not me

**Solutions:**
1. Verify connection is active (`connection.state === "Connected"`)
2. Check `userId` claim matches notification target
3. Verify group membership (check server logs)
4. Ensure event listener is registered before connection starts

**Problem:** Duplicate notifications

**Solutions:**
1. Check for multiple active connections
2. Verify event listener isn't registered multiple times
3. Use notification `id` for deduplication

### Acknowledgment Failures

**Problem:** `AcknowledgeNotification` throws "User not authenticated"

**Solutions:**
1. Verify JWT token is valid
2. Check `NameIdentifier` claim exists
3. Ensure token includes user ID as GUID

**Problem:** `AcknowledgeNotification` throws "Notification not found"

**Solutions:**
1. Verify notification ID is correct
2. Check notification belongs to current user
3. Ensure notification hasn't been deleted

## Related Documentation

- [Notification Service API Overview](./api-overview.md)
- [Notification REST API](./notification-api.md)
- [User Preferences API](./user-preferences-api.md)
- [Subscription Management](./subscription-api.md)
- [Authentication Guide](./authentication.md)

## API Version

**Current Version:** 1.0

**Service:** NotificationService.Api

**Port:** 5201

**Last Updated:** 2025-12-18
