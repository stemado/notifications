# Preferences API Endpoints

## Overview

The Preferences API provides endpoints for managing user notification preferences within the PlanSource Automation platform. Users can control which notification channels they receive alerts through and set minimum severity thresholds for each channel (SignalR, Email, SMS, Teams).

**Base URL**: `http://192.168.150.52:5201/api/preferences`

**Authentication**: All endpoints require authentication via JWT token. The authenticated user's ID is extracted from the `ClaimTypes.NameIdentifier` claim.

**Phase**: Phase 2 feature

---

## Endpoints

### 1. Get All User Preferences

Retrieves all notification channel preferences for the authenticated user. Returns preferences for all available channels with their enabled state and minimum severity levels.

**Endpoint**: `GET /api/preferences`

**Authentication**: Required

**Request**: No parameters required.

**Response**: `200 OK`
```json
[
  {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "channel": "SignalR",
    "minSeverity": "Info",
    "enabled": true
  },
  {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "channel": "Email",
    "minSeverity": "Warning",
    "enabled": true
  },
  {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "channel": "SMS",
    "minSeverity": "Urgent",
    "enabled": false
  },
  {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "channel": "Teams",
    "minSeverity": "Critical",
    "enabled": false
  }
]
```

**Response Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `userId` | Guid | User ID who owns this preference |
| `channel` | Enum | Notification channel: `SignalR`, `Email`, `SMS`, `Teams` |
| `minSeverity` | Enum | Minimum severity level: `Info`, `Warning`, `Urgent`, `Critical` |
| `enabled` | Boolean | Whether this channel is enabled for the user |

**Error Responses**:
- `401 Unauthorized`: User not authenticated
- `500 Internal Server Error`: Error retrieving preferences

**Example Usage**:
```bash
curl -X GET "http://192.168.150.52:5201/api/preferences" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 2. Get Preference for Specific Channel

Retrieves the notification preference for a specific channel for the authenticated user.

**Endpoint**: `GET /api/preferences/{channel}`

**Authentication**: Required

**Path Parameters**:
- `channel` (Enum, required): The notification channel
  - Valid values: `SignalR`, `Email`, `SMS`, `Teams`

**Response**: `200 OK`
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "channel": "Email",
  "minSeverity": "Warning",
  "enabled": true
}
```

**Error Responses**:
- `401 Unauthorized`: User not authenticated
- `404 Not Found`: Preference for channel not found
- `500 Internal Server Error`: Error retrieving preference

**Example Usage**:
```bash
# Get email preferences
curl -X GET "http://192.168.150.52:5201/api/preferences/Email" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Get SMS preferences
curl -X GET "http://192.168.150.52:5201/api/preferences/SMS" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 3. Set or Update Channel Preference

Sets or updates the notification preference for a specific channel. This allows users to enable/disable channels and configure the minimum severity threshold for each channel.

**Endpoint**: `PUT /api/preferences/{channel}`

**Authentication**: Required

**Path Parameters**:
- `channel` (Enum, required): The notification channel to configure
  - Valid values: `SignalR`, `Email`, `SMS`, `Teams`

**Request Body**:
```json
{
  "minSeverity": "Warning",
  "enabled": true
}
```

**Request Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `minSeverity` | Enum | Yes | Minimum severity level: `Info`, `Warning`, `Urgent`, `Critical` |
| `enabled` | Boolean | Yes | Whether to enable this channel |

**Behavior**:
- Notifications with severity **greater than or equal to** `minSeverity` will be delivered
- If `enabled` is `false`, no notifications will be sent through this channel regardless of severity
- Example: If `minSeverity` is `Warning`, the user will receive `Warning`, `Urgent`, and `Critical` notifications, but not `Info`

**Response**: `200 OK`
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "channel": "Email",
  "minSeverity": "Warning",
  "enabled": true
}
```

**Error Responses**:
- `401 Unauthorized`: User not authenticated
- `500 Internal Server Error`: Error setting preference

**Example Usage**:
```bash
# Enable email notifications for Warning and above
curl -X PUT "http://192.168.150.52:5201/api/preferences/Email" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "minSeverity": "Warning",
    "enabled": true
  }'

# Enable SMS only for Critical notifications
curl -X PUT "http://192.168.150.52:5201/api/preferences/SMS" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "minSeverity": "Critical",
    "enabled": true
  }'

# Disable Teams notifications
curl -X PUT "http://192.168.150.52:5201/api/preferences/Teams" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "minSeverity": "Info",
    "enabled": false
  }'
```

---

### 4. Reset Channel Preference to Default

Deletes a user's custom preference for a specific channel, resetting it to the system default. The system will apply default preferences the next time preferences are queried.

**Endpoint**: `DELETE /api/preferences/{channel}`

**Authentication**: Required

**Path Parameters**:
- `channel` (Enum, required): The notification channel to reset
  - Valid values: `SignalR`, `Email`, `SMS`, `Teams`

**Request**: No body required.

**Response**: `200 OK` (empty body)

**Behavior**:
- Removes the user's custom preference for the specified channel
- The channel will revert to default system settings
- Default preferences are typically set when calling `POST /api/preferences/defaults`

**Error Responses**:
- `401 Unauthorized`: User not authenticated
- `500 Internal Server Error`: Error deleting preference

**Example Usage**:
```bash
# Reset email preferences to default
curl -X DELETE "http://192.168.150.52:5201/api/preferences/Email" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Reset all channels by deleting each one
curl -X DELETE "http://192.168.150.52:5201/api/preferences/SignalR" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
curl -X DELETE "http://192.168.150.52:5201/api/preferences/Email" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
curl -X DELETE "http://192.168.150.52:5201/api/preferences/SMS" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
curl -X DELETE "http://192.168.150.52:5201/api/preferences/Teams" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 5. Set Default Preferences for User

Initializes or resets all notification preferences to system defaults for the authenticated user. This is typically called during user onboarding or when a user wants to restore all settings to defaults.

**Endpoint**: `POST /api/preferences/defaults`

**Authentication**: Required

**Request**: No body required.

**Response**: `200 OK` (empty body)

**Behavior**:
- Creates default preferences for all available notification channels
- Default configuration (typical values):
  - **SignalR**: Enabled, MinSeverity = `Info`
  - **Email**: Enabled, MinSeverity = `Warning`
  - **SMS**: Disabled, MinSeverity = `Urgent`
  - **Teams**: Disabled, MinSeverity = `Critical`
- Overwrites any existing custom preferences

**Error Responses**:
- `401 Unauthorized`: User not authenticated
- `500 Internal Server Error`: Error setting default preferences

**Use Cases**:
- New user onboarding: Initialize preferences on first login
- Reset to defaults: Allow user to restore all settings
- Bulk reset: Admin operation to reset preferences for troubleshooting

**Example Usage**:
```bash
# Set default preferences for current user
curl -X POST "http://192.168.150.52:5201/api/preferences/defaults" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Notification Channels

### Available Channels

| Channel | Description | Typical Use Case | Availability |
|---------|-------------|------------------|--------------|
| **SignalR** | Real-time browser notifications | In-app alerts, immediate feedback | Phase 1 |
| **Email** | Email notifications | Non-urgent updates, daily summaries | Phase 2 |
| **SMS** | Text message notifications | Urgent alerts, critical failures | Phase 2 |
| **Teams** | Microsoft Teams notifications | Team collaboration, shared alerts | Phase 3 |

### Channel Priority Recommendations

Users should configure channels based on urgency and availability:

**SignalR** (Real-time):
- **Recommended**: Enabled for `Info` and above
- **Purpose**: Immediate in-app feedback while using the platform
- **Best for**: Active users, dashboard monitoring

**Email**:
- **Recommended**: Enabled for `Warning` and above
- **Purpose**: Asynchronous notifications that can wait
- **Best for**: Non-urgent issues, daily updates, summary reports

**SMS**:
- **Recommended**: Enabled for `Urgent` and above
- **Purpose**: Time-sensitive alerts when not at computer
- **Best for**: Critical issues requiring immediate attention
- **Note**: May incur costs, use sparingly

**Teams**:
- **Recommended**: Enabled for `Critical` only or team-specific alerts
- **Purpose**: Collaboration and escalation
- **Best for**: Team-wide incidents, shared responsibility

---

## Notification Severity Levels

### Severity Hierarchy

Severity levels are hierarchical. Setting a minimum severity of `Warning` will deliver `Warning`, `Urgent`, and `Critical` notifications, but filter out `Info`.

| Severity | Description | Example Scenarios | Recommended Channels |
|----------|-------------|-------------------|----------------------|
| **Info** | Informational, routine operations | Import completed, schedule triggered | SignalR |
| **Warning** | Non-critical issues needing attention | File processing delayed, validation warnings | SignalR, Email |
| **Urgent** | Time-sensitive issues requiring prompt action | Saga stuck, multiple retries failing | SignalR, Email, SMS |
| **Critical** | System failures or blocking issues | Service down, data corruption, maximum retries exceeded | All channels |

### Severity Filtering Examples

**Example 1: Conservative User (Less Noise)**
```json
{
  "SignalR": { "minSeverity": "Warning", "enabled": true },
  "Email": { "minSeverity": "Urgent", "enabled": true },
  "SMS": { "minSeverity": "Critical", "enabled": true },
  "Teams": { "minSeverity": "Critical", "enabled": false }
}
```
- **Result**: Only sees warnings and above in browser, urgent/critical via email, critical via SMS

**Example 2: Active Monitor (Maximum Visibility)**
```json
{
  "SignalR": { "minSeverity": "Info", "enabled": true },
  "Email": { "minSeverity": "Info", "enabled": true },
  "SMS": { "minSeverity": "Urgent", "enabled": true },
  "Teams": { "minSeverity": "Warning", "enabled": true }
}
```
- **Result**: Sees all notifications across multiple channels

**Example 3: Critical Only (On-Call)**
```json
{
  "SignalR": { "minSeverity": "Critical", "enabled": true },
  "Email": { "minSeverity": "Critical", "enabled": true },
  "SMS": { "minSeverity": "Critical", "enabled": true },
  "Teams": { "minSeverity": "Critical", "enabled": false }
}
```
- **Result**: Only receives critical alerts, ideal for on-call engineers

---

## Common Workflows

### Workflow 1: New User Setup

A new user logs in for the first time and needs preferences initialized:

1. User authenticates and receives JWT token
2. Call `POST /api/preferences/defaults` to initialize preferences
3. Call `GET /api/preferences` to retrieve and display current settings
4. User can then customize individual channels as needed

```bash
# 1. Initialize defaults
curl -X POST "http://192.168.150.52:5201/api/preferences/defaults" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# 2. Get current preferences
curl -X GET "http://192.168.150.52:5201/api/preferences" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Workflow 2: Customize Notification Channels

User wants to reduce email noise but stay informed via in-app notifications:

1. Get current preferences: `GET /api/preferences`
2. Update SignalR to receive all notifications: `PUT /api/preferences/SignalR`
3. Update Email to only receive urgent and critical: `PUT /api/preferences/Email`
4. Disable SMS completely: `PUT /api/preferences/SMS`

```bash
# 1. Enable all SignalR notifications
curl -X PUT "http://192.168.150.52:5201/api/preferences/SignalR" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"minSeverity": "Info", "enabled": true}'

# 2. Limit email to urgent and above
curl -X PUT "http://192.168.150.52:5201/api/preferences/Email" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"minSeverity": "Urgent", "enabled": true}'

# 3. Disable SMS
curl -X PUT "http://192.168.150.52:5201/api/preferences/SMS" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"minSeverity": "Info", "enabled": false}'
```

### Workflow 3: On-Call Configuration

Engineer going on-call wants to receive critical alerts via all channels:

```bash
# Enable SMS for critical alerts
curl -X PUT "http://192.168.150.52:5201/api/preferences/SMS" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"minSeverity": "Critical", "enabled": true}'

# Enable Email for urgent and above
curl -X PUT "http://192.168.150.52:5201/api/preferences/Email" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"minSeverity": "Urgent", "enabled": true}'

# Keep SignalR for all notifications
curl -X PUT "http://192.168.150.52:5201/api/preferences/SignalR" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"minSeverity": "Info", "enabled": true}'
```

### Workflow 4: Reset All Preferences

User wants to restore default settings after customizing:

```bash
# Option 1: Reset individual channels
curl -X DELETE "http://192.168.150.52:5201/api/preferences/SignalR" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
curl -X DELETE "http://192.168.150.52:5201/api/preferences/Email" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
curl -X DELETE "http://192.168.150.52:5201/api/preferences/SMS" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
curl -X DELETE "http://192.168.150.52:5201/api/preferences/Teams" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Option 2: Set system defaults
curl -X POST "http://192.168.150.52:5201/api/preferences/defaults" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Workflow 5: Preferences UI Component

Build a user settings page that displays and updates preferences:

1. **Load preferences**: Call `GET /api/preferences` on page load
2. **Display toggles**: Show enable/disable toggle for each channel
3. **Display severity dropdowns**: Show dropdown with `Info`, `Warning`, `Urgent`, `Critical` options
4. **Save changes**: Call `PUT /api/preferences/{channel}` when user updates a setting
5. **Reset button**: Call `POST /api/preferences/defaults` to restore defaults

---

## Data Models

### UserNotificationPreference

```csharp
public class UserNotificationPreference
{
    /// <summary>
    /// User ID who owns this preference
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Notification channel (SignalR, Email, SMS, Teams)
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Minimum severity level for this channel
    /// Only notifications with severity >= MinSeverity will be delivered
    /// </summary>
    public NotificationSeverity MinSeverity { get; set; }

    /// <summary>
    /// Whether this channel is enabled
    /// If false, no notifications will be sent through this channel
    /// </summary>
    public bool Enabled { get; set; }
}
```

### NotificationChannel Enum

```csharp
public enum NotificationChannel
{
    SignalR,  // Real-time notification via SignalR
    Email,    // Email notification
    SMS,      // SMS notification
    Teams     // Microsoft Teams notification (Phase 3)
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

### SetPreferenceRequest

```csharp
public class SetPreferenceRequest
{
    /// <summary>
    /// Minimum severity level for the channel
    /// </summary>
    public NotificationSeverity MinSeverity { get; set; }

    /// <summary>
    /// Whether to enable this channel
    /// </summary>
    public bool Enabled { get; set; }
}
```

---

## Best Practices

### 1. Initialize Preferences on First Login

Always ensure users have preferences initialized:
```csharp
// Check if user has preferences
var preferences = await _apiClient.GetPreferencesAsync();
if (preferences == null || preferences.Count == 0)
{
    // Initialize with defaults
    await _apiClient.SetDefaultPreferencesAsync();
    preferences = await _apiClient.GetPreferencesAsync();
}
```

### 2. Use Appropriate Severity Thresholds

Configure channels based on their urgency and intrusiveness:
- **SignalR**: `Info` - User is actively using the platform
- **Email**: `Warning` - Asynchronous, can wait for user to check inbox
- **SMS**: `Urgent` or `Critical` - Intrusive, should be reserved for important alerts
- **Teams**: `Warning` or `Critical` - Depends on team collaboration needs

### 3. Provide Clear UI Indicators

When building preference UI, clearly indicate what each severity level means:
```jsx
<select name="minSeverity">
  <option value="Info">All notifications (Info and above)</option>
  <option value="Warning">Important (Warning and above)</option>
  <option value="Urgent">Urgent (Urgent and above)</option>
  <option value="Critical">Critical only</option>
</select>
```

### 4. Respect User Preferences in Notification Creation

When creating notifications, the system automatically filters based on user preferences. However, you can optimize by checking preferences before expensive operations:

```csharp
// Check if user would receive this notification before generating expensive content
var emailPreference = await _preferenceService.GetPreferenceAsync(userId, NotificationChannel.Email);
if (emailPreference.Enabled && notification.Severity >= emailPreference.MinSeverity)
{
    // Generate and send email
    await _emailService.SendNotificationAsync(notification);
}
```

### 5. Allow Easy Temporary Adjustments

Provide quick actions for common scenarios:
- "Quiet hours" mode: Disable all channels except critical
- "On-call" mode: Enable SMS and Email for urgent and above
- "Do not disturb": Disable all channels temporarily

### 6. Document Default Behavior

Clearly communicate default preference settings to users:
```markdown
Default Notification Settings:
- In-App (SignalR): All notifications
- Email: Warnings and above
- SMS: Disabled (enable in settings)
- Teams: Disabled (enable in settings)
```

---

## Integration with Notification Creation

### How Preferences Affect Notification Delivery

When a notification is created via `POST /api/notifications`, the system:

1. **Creates the notification record** in the database
2. **Retrieves user preferences** for all channels
3. **Filters channels** based on enabled state and minimum severity
4. **Dispatches to eligible channels**:
   - SignalR: Immediate real-time push
   - Email: Queued for batch sending
   - SMS: Queued with rate limiting
   - Teams: Posted to configured webhook

### Example Delivery Logic

```
Notification: Severity = Warning
User Preferences:
  - SignalR: Enabled, MinSeverity = Info    ✓ Delivered (Warning >= Info)
  - Email:   Enabled, MinSeverity = Warning  ✓ Delivered (Warning >= Warning)
  - SMS:     Enabled, MinSeverity = Urgent   ✗ Filtered (Warning < Urgent)
  - Teams:   Disabled                        ✗ Filtered (Channel disabled)

Result: Notification delivered via SignalR and Email only
```

### Testing Preference Filtering

Test that preferences correctly filter notifications:

```bash
# 1. Set email to only receive Critical
curl -X PUT "http://192.168.150.52:5201/api/preferences/Email" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"minSeverity": "Critical", "enabled": true}'

# 2. Create a Warning notification
curl -X POST "http://192.168.150.52:5201/api/notifications" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "YOUR_USER_ID",
    "severity": "Warning",
    "title": "Test Warning",
    "message": "This should not trigger email",
    "requiresAck": false
  }'

# Expected: Email should NOT be sent (Warning < Critical)

# 3. Create a Critical notification
curl -X POST "http://192.168.150.52:5201/api/notifications" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "YOUR_USER_ID",
    "severity": "Critical",
    "title": "Test Critical",
    "message": "This SHOULD trigger email",
    "requiresAck": true
  }'

# Expected: Email SHOULD be sent (Critical >= Critical)
```

---

## Error Handling

All endpoints follow consistent error response patterns:

### 401 Unauthorized
Returned when the JWT token is missing, invalid, or expired.

```json
{
  "error": "User not authenticated"
}
```

**Resolution**: Ensure valid JWT token is included in Authorization header.

### 404 Not Found
Returned when a preference for the specified channel doesn't exist.

```json
{
  "error": "Preference for channel Email not found"
}
```

**Resolution**: Initialize preferences with `POST /api/preferences/defaults` or set the preference with `PUT /api/preferences/{channel}`.

### 500 Internal Server Error
Returned when an unexpected server error occurs.

```json
{
  "error": "Error retrieving preferences"
}
```

**Resolution**: Check server logs for detailed error information. Contact system administrator if issue persists.

---

## Security Considerations

### User Isolation

- All preference endpoints operate on the **authenticated user's preferences only**
- Users cannot view or modify other users' preferences
- User ID is extracted from JWT token claims, not request parameters
- This prevents preference manipulation attacks

### Authorization

- All endpoints require valid JWT authentication
- No admin endpoints for bulk preference management (Phase 2+)
- Preferences are user-scoped, not tenant-scoped

### Data Validation

- Channel names must match enum values (validated by ASP.NET Core model binding)
- Severity levels must match enum values
- Boolean values are strongly typed
- Invalid enum values result in 400 Bad Request

---

## Performance Considerations

### Caching

Consider caching user preferences in the frontend to reduce API calls:

```javascript
// Cache preferences for 5 minutes
const CACHE_DURATION = 5 * 60 * 1000;
let cachedPreferences = null;
let cacheTimestamp = null;

async function getPreferences() {
  const now = Date.now();
  if (cachedPreferences && (now - cacheTimestamp) < CACHE_DURATION) {
    return cachedPreferences;
  }

  cachedPreferences = await fetch('/api/preferences', {
    headers: { 'Authorization': `Bearer ${token}` }
  }).then(r => r.json());
  cacheTimestamp = now;

  return cachedPreferences;
}
```

### Batch Updates

When updating multiple preferences, make individual PUT calls. Future versions may support batch operations:

```javascript
// Current approach (individual updates)
async function updateMultiplePreferences(updates) {
  for (const update of updates) {
    await fetch(`/api/preferences/${update.channel}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        minSeverity: update.minSeverity,
        enabled: update.enabled
      })
    });
  }
}
```

---

## Troubleshooting

### Issue: Preferences Not Found (404)

**Symptom**: `GET /api/preferences/{channel}` returns 404

**Cause**: User has never set preferences for that channel

**Solution**:
```bash
# Initialize defaults
curl -X POST "http://192.168.150.52:5201/api/preferences/defaults" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Issue: Not Receiving Notifications Despite Enabled Channel

**Symptom**: Channel is enabled but notifications aren't being delivered

**Checklist**:
1. **Check minimum severity**: Ensure notification severity >= channel's `minSeverity`
2. **Verify channel is enabled**: Check `enabled` field is `true`
3. **Check notification service logs**: Review NotificationService.Api logs for delivery errors
4. **Test channel connectivity**: Verify email/SMS/Teams services are operational
5. **Check user ID**: Ensure notification `userId` matches preference `userId`

**Debug Example**:
```bash
# 1. Get current preferences
curl -X GET "http://192.168.150.52:5201/api/preferences" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# 2. Check specific channel
curl -X GET "http://192.168.150.52:5201/api/preferences/Email" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# 3. Lower the threshold temporarily
curl -X PUT "http://192.168.150.52:5201/api/preferences/Email" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"minSeverity": "Info", "enabled": true}'
```

### Issue: Unauthorized (401) Error

**Symptom**: All API calls return 401 Unauthorized

**Causes**:
- JWT token expired
- JWT token malformed
- User not authenticated

**Solution**:
1. Verify JWT token is included in Authorization header
2. Check token expiration
3. Re-authenticate if necessary
4. Ensure token format: `Bearer YOUR_JWT_TOKEN`

### Issue: Changes Not Taking Effect

**Symptom**: Updated preferences but notifications still follow old settings

**Causes**:
- Frontend caching
- Multiple browser tabs with stale state
- Backend caching (if implemented)

**Solution**:
1. Clear frontend cache
2. Refresh all browser tabs
3. Verify changes with `GET /api/preferences`
4. Wait a few seconds for background jobs to pick up new preferences

---

## Additional Resources

- **Notifications API**: See [notifications-endpoints.md](./notifications-endpoints.md) for notification creation and management
- **SignalR Integration**: See SignalR Hub Documentation for real-time notification delivery
- **Email Notifications**: See Email Templates documentation for notification email formatting
- **Architecture Overview**: See Notification Service Architecture documentation

---

## Support

For issues or questions:
- Create an issue in the project repository
- Contact the ops team at `ops@plansource.com`
- Check the NotificationService.Api logs at: `D:\Projects\PlanSourceAutomation-V2\NotificationServices\logs\`
- Review the [Notification Service README](../README.md)
