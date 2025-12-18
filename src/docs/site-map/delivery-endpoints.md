# Delivery API Endpoints

## Overview

The Delivery API provides endpoints for monitoring and managing notification delivery across multiple channels (SignalR, Email, SMS, Teams). This API allows tracking delivery status, viewing delivery history, retrying failed deliveries, and canceling pending deliveries.

**Base URL**: `http://192.168.150.52:5201/api/delivery`

**Authentication**: Currently optional (Phase 1), will be required in Phase 2

---

## Endpoints

### 1. Get Delivery Queue

Retrieves all notifications currently queued for delivery or awaiting retry. This includes notifications in `Pending`, `Processing`, or `Failed` (retry) states.

**Endpoint**: `GET /api/delivery/queue`

**Authentication**: Optional (Phase 1)

**Request**: No parameters required.

**Response**: `200 OK`
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "notificationId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    "channel": "Email",
    "status": "retry",
    "attempts": 2,
    "maxAttempts": 3,
    "nextRetryAt": "2025-12-18T10:45:00Z",
    "lastError": "SMTP connection timeout",
    "createdAt": "2025-12-18T10:30:00Z"
  },
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
    "notificationId": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
    "channel": "SMS",
    "status": "pending",
    "attempts": 0,
    "maxAttempts": 3,
    "nextRetryAt": null,
    "lastError": null,
    "createdAt": "2025-12-18T10:35:00Z"
  },
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afb0",
    "notificationId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    "channel": "SignalR",
    "status": "processing",
    "attempts": 1,
    "maxAttempts": 1,
    "nextRetryAt": null,
    "lastError": null,
    "createdAt": "2025-12-18T10:30:00Z"
  }
]
```

**Status Values**:
- `pending`: Queued and waiting to be sent
- `processing`: Currently being processed
- `retry`: Failed and waiting for retry attempt

**Error Responses**:
- `500 Internal Server Error`: Error retrieving delivery queue

**Example Usage**:
```bash
curl -X GET "http://192.168.150.52:5201/api/delivery/queue"
```

**Use Cases**:
- Monitor current delivery backlog
- Identify deliveries stuck in retry state
- Track delivery attempts and retry schedules
- Debug delivery pipeline issues

---

### 2. Get Delivery History

Retrieves historical delivery records with optional filtering by channel, status, and date range. Supports pagination for large result sets.

**Endpoint**: `GET /api/delivery/history`

**Authentication**: Optional (Phase 1)

**Query Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `channel` | String | No | Filter by delivery channel: `SignalR`, `Email`, `SMS`, `Teams` |
| `status` | String | No | Filter by delivery status: `Pending`, `Processing`, `Delivered`, `Failed`, `Bounced`, `Cancelled` |
| `fromDate` | DateTime | No | Start date for filtering (ISO 8601 format) |
| `toDate` | DateTime | No | End date for filtering (ISO 8601 format) |
| `page` | Int | No | Page number (default: 1) |
| `pageSize` | Int | No | Records per page (default: 50) |

**Response**: `200 OK`
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "notificationId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    "channel": "Email",
    "status": "delivered",
    "deliveredAt": "2025-12-18T10:32:15Z",
    "failedAt": null,
    "errorMessage": null,
    "responseData": {
      "messageId": "msg_abc123",
      "provider": "SendGrid",
      "deliveryTimeMs": 1523
    }
  },
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
    "notificationId": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
    "channel": "SMS",
    "status": "failed",
    "deliveredAt": null,
    "failedAt": "2025-12-18T10:33:45Z",
    "errorMessage": "Invalid phone number format",
    "responseData": {
      "provider": "Twilio",
      "errorCode": "21211"
    }
  },
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afb0",
    "notificationId": "3fa85f64-5717-4562-b3fc-2c963f66afb1",
    "channel": "Email",
    "status": "bounced",
    "deliveredAt": null,
    "failedAt": "2025-12-18T10:35:20Z",
    "errorMessage": "Mailbox does not exist",
    "responseData": {
      "messageId": "msg_def456",
      "provider": "SendGrid",
      "bounceType": "hard"
    }
  }
]
```

**Status Values**:
- `delivered`: Successfully delivered
- `failed`: Delivery failed (exhausted all retries)
- `bounced`: Email bounced (invalid recipient)

**Error Responses**:
- `500 Internal Server Error`: Error retrieving delivery history

**Example Usage**:
```bash
# Get all email deliveries
curl -X GET "http://192.168.150.52:5201/api/delivery/history?channel=Email"

# Get failed deliveries from the last 24 hours
curl -X GET "http://192.168.150.52:5201/api/delivery/history?status=Failed&fromDate=2025-12-17T10:00:00Z"

# Get paginated SMS delivery history
curl -X GET "http://192.168.150.52:5201/api/delivery/history?channel=SMS&page=2&pageSize=25"

# Get all deliveries within a date range
curl -X GET "http://192.168.150.52:5201/api/delivery/history?fromDate=2025-12-01T00:00:00Z&toDate=2025-12-18T23:59:59Z"
```

**Filtering Tips**:
- Use `channel` to analyze performance by delivery method
- Use `status=Failed` to identify delivery issues
- Use `status=Bounced` to find invalid email addresses
- Combine filters for targeted analysis (e.g., failed emails from last week)

---

### 3. Get Delivery Statistics

Retrieves aggregate delivery statistics including total counts, delivery rates, and per-channel breakdowns.

**Endpoint**: `GET /api/delivery/stats`

**Authentication**: Optional (Phase 1)

**Request**: No parameters required.

**Response**: `200 OK`
```json
{
  "totalDelivered": 1247,
  "totalFailed": 83,
  "totalPending": 12,
  "deliveryRatePercent": 93.76,
  "avgDeliveryTimeMs": 1342,
  "channelStats": [
    {
      "channel": "Email",
      "delivered": 856,
      "failed": 42,
      "pending": 5
    },
    {
      "channel": "SignalR",
      "delivered": 325,
      "failed": 8,
      "pending": 2
    },
    {
      "channel": "SMS",
      "delivered": 66,
      "failed": 33,
      "pending": 5
    },
    {
      "channel": "Teams",
      "delivered": 0,
      "failed": 0,
      "pending": 0
    }
  ]
}
```

**Response Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `totalDelivered` | Int | Total successfully delivered notifications |
| `totalFailed` | Int | Total failed deliveries (exhausted retries) |
| `totalPending` | Int | Total deliveries currently queued or retrying |
| `deliveryRatePercent` | Decimal | Success rate as percentage (delivered / total * 100) |
| `avgDeliveryTimeMs` | Int | Average delivery time in milliseconds |
| `channelStats` | Array | Per-channel delivery statistics |

**Channel Stats Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `channel` | String | Delivery channel name |
| `delivered` | Int | Successful deliveries for this channel |
| `failed` | Int | Failed deliveries for this channel |
| `pending` | Int | Pending/retrying deliveries for this channel |

**Error Responses**:
- `500 Internal Server Error`: Error retrieving delivery statistics

**Example Usage**:
```bash
curl -X GET "http://192.168.150.52:5201/api/delivery/stats"
```

**Use Cases**:
- Dashboard widgets showing delivery health
- Monitoring delivery pipeline performance
- Identifying problematic delivery channels
- Capacity planning for notification infrastructure
- SLA reporting on notification delivery

---

### 4. Retry Failed Delivery

Manually retries a failed delivery by resetting its status and scheduling it for immediate processing. Useful for recovering from temporary failures or after fixing configuration issues.

**Endpoint**: `POST /api/delivery/{id}/retry`

**Authentication**: Optional (Phase 1)

**Path Parameters**:
- `id` (Guid, required): The delivery record ID

**Request**: No body required.

**Response**: `200 OK`
```json
{
  "message": "Delivery queued for retry"
}
```

**Error Responses**:
- `400 Bad Request`: Delivery cannot be retried (e.g., already delivered, cancelled)
  ```json
  {
    "error": "Cannot retry delivery in Delivered status"
  }
  ```
- `404 Not Found`: Delivery record not found (implicit in InvalidOperationException)
- `500 Internal Server Error`: Error processing retry request

**Example Usage**:
```bash
# Retry a specific failed delivery
curl -X POST "http://192.168.150.52:5201/api/delivery/3fa85f64-5717-4562-b3fc-2c963f66afa6/retry"
```

**Valid States for Retry**:
- `Failed`: Delivery failed after all retry attempts
- `Cancelled`: Previously cancelled delivery

**Invalid States for Retry** (will return 400 Bad Request):
- `Delivered`: Already successfully delivered
- `Pending`: Already queued for delivery
- `Processing`: Currently being processed

**Use Cases**:
- Retry after fixing SMTP configuration
- Retry after email service outage is resolved
- Retry after recipient phone number is corrected
- Manual intervention for critical notifications
- Testing delivery pipeline after infrastructure changes

---

### 5. Cancel Pending Delivery

Cancels a pending delivery, preventing it from being sent. Once cancelled, the delivery cannot be retried unless manually re-queued.

**Endpoint**: `DELETE /api/delivery/{id}`

**Authentication**: Optional (Phase 1)

**Path Parameters**:
- `id` (Guid, required): The delivery record ID

**Request**: No body required.

**Response**: `200 OK`
```json
{
  "message": "Delivery cancelled"
}
```

**Error Responses**:
- `400 Bad Request`: Delivery cannot be cancelled (e.g., already delivered, already cancelled)
  ```json
  {
    "error": "Cannot cancel delivery in Delivered status"
  }
  ```
- `404 Not Found`: Delivery record not found (implicit in InvalidOperationException)
- `500 Internal Server Error`: Error processing cancellation

**Example Usage**:
```bash
# Cancel a specific pending delivery
curl -X DELETE "http://192.168.150.52:5201/api/delivery/3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

**Valid States for Cancellation**:
- `Pending`: Queued and waiting to be sent
- `Failed`: Failed and waiting for retry

**Invalid States for Cancellation** (will return 400 Bad Request):
- `Delivered`: Already successfully delivered
- `Cancelled`: Already cancelled
- `Processing`: Currently being processed (may race with delivery)

**Use Cases**:
- Cancel notification before scheduled send time
- Prevent retry of failed delivery after issue is resolved differently
- Cancel delivery when notification becomes irrelevant
- Stop delivery of duplicate notifications
- Manual intervention for incorrect notifications

**Important Notes**:
- Cancellation is final - cancelled deliveries remain in history but cannot be retried
- To retry a cancelled delivery, use the retry endpoint
- Cancelling a delivery does not dismiss the associated notification

---

## Common Workflows

### Workflow 1: Monitor Delivery Pipeline Health

1. Call `GET /api/delivery/stats` to get overall health metrics
2. Display delivery rate, total pending, and per-channel statistics
3. Set up alerts for:
   - Delivery rate drops below 90%
   - Pending count exceeds threshold (e.g., 50)
   - Specific channel has high failure rate (e.g., SMS > 20% failed)
4. Poll periodically (e.g., every 60 seconds) or subscribe to delivery events

**Example Dashboard Query**:
```bash
# Get stats and queue size for dashboard
curl -X GET "http://192.168.150.52:5201/api/delivery/stats"
curl -X GET "http://192.168.150.52:5201/api/delivery/queue"
```

### Workflow 2: Investigate Failed Deliveries

1. Call `GET /api/delivery/history?status=Failed` to get recent failures
2. Group failures by error message to identify patterns
3. For each unique error:
   - Fix configuration issue (e.g., SMTP credentials, SMS provider settings)
   - Retry affected deliveries using `POST /api/delivery/{id}/retry`
4. Monitor queue to ensure retries succeed

**Example Investigation Query**:
```bash
# Get all failed email deliveries from last 24 hours
curl -X GET "http://192.168.150.52:5201/api/delivery/history?channel=Email&status=Failed&fromDate=2025-12-17T10:00:00Z"

# Retry specific delivery after fixing issue
curl -X POST "http://192.168.150.52:5201/api/delivery/3fa85f64-5717-4562-b3fc-2c963f66afa6/retry"
```

### Workflow 3: Clean Up Bounced Email Addresses

1. Call `GET /api/delivery/history?status=Bounced&channel=Email`
2. Extract email addresses from bounced deliveries
3. Mark email addresses as invalid in user database
4. Optionally cancel any pending deliveries to invalid addresses
5. Update notification preferences to skip email for these users

**Example Cleanup Query**:
```bash
# Get all bounced emails
curl -X GET "http://192.168.150.52:5201/api/delivery/history?status=Bounced&channel=Email&pageSize=100"

# Cancel pending email to bounced address
curl -X DELETE "http://192.168.150.52:5201/api/delivery/3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

### Workflow 4: Bulk Retry After Service Outage

1. Service outage occurs (e.g., email provider down for 2 hours)
2. Many deliveries fail during outage
3. After service is restored:
   - Call `GET /api/delivery/history?status=Failed&fromDate={outageStartTime}&toDate={outageEndTime}`
   - Filter results to affected channel (e.g., `Email`)
   - Retry each failed delivery: `POST /api/delivery/{id}/retry`
4. Monitor queue to ensure retries succeed

**Example Bulk Retry Script**:
```bash
# Get all failed emails during outage window
curl -X GET "http://192.168.150.52:5201/api/delivery/history?channel=Email&status=Failed&fromDate=2025-12-18T08:00:00Z&toDate=2025-12-18T10:00:00Z" \
  | jq -r '.[].id' \
  | while read delivery_id; do
      curl -X POST "http://192.168.150.52:5201/api/delivery/${delivery_id}/retry"
    done
```

### Workflow 5: Analyze Channel Performance

1. Call `GET /api/delivery/stats` to get per-channel statistics
2. For each channel, calculate:
   - Success rate: `delivered / (delivered + failed) * 100`
   - Failure rate: `failed / (delivered + failed) * 100`
   - Pending ratio: `pending / (delivered + failed + pending) * 100`
3. Identify underperforming channels (e.g., SMS with 50% failure rate)
4. Call `GET /api/delivery/history?channel=SMS&status=Failed` to analyze error patterns
5. Adjust channel configuration or disable unreliable channels

**Example Analysis Query**:
```bash
# Get stats for all channels
curl -X GET "http://192.168.150.52:5201/api/delivery/stats"

# Deep dive into SMS failures
curl -X GET "http://192.168.150.52:5201/api/delivery/history?channel=SMS&status=Failed&pageSize=100"
```

---

## Data Models

### Delivery Status Enum

```csharp
public enum DeliveryStatus
{
    Pending,      // Queued and waiting to be sent
    Processing,   // Currently being processed
    Delivered,    // Successfully delivered
    Failed,       // Delivery failed (will be retried or exhausted)
    Bounced,      // Email bounced (invalid recipient)
    Cancelled     // Delivery was cancelled
}
```

**Frontend Mapping**:
- `Pending` → `pending`
- `Processing` → `processing`
- `Failed` (in queue) → `retry`
- `Delivered` → `delivered`
- `Failed` (in history) → `failed`
- `Bounced` → `bounced`
- `Cancelled` → `cancelled`

### Notification Channel Enum

```csharp
public enum NotificationChannel
{
    SignalR,  // Real-time notification via SignalR
    Email,    // Email notification
    SMS,      // SMS notification
    Teams     // Microsoft Teams notification (Phase 3)
}
```

### Queue Item Response Model

```typescript
interface QueueItem {
  id: string;              // Delivery record ID
  notificationId: string;  // Associated notification ID
  channel: string;         // "SignalR" | "Email" | "SMS" | "Teams"
  status: string;          // "pending" | "processing" | "retry"
  attempts: number;        // Current attempt count
  maxAttempts: number;     // Maximum retry attempts
  nextRetryAt: string | null;  // ISO 8601 timestamp for next retry
  lastError: string | null;    // Last error message
  createdAt: string;       // ISO 8601 timestamp
}
```

### History Item Response Model

```typescript
interface HistoryItem {
  id: string;              // Delivery record ID
  notificationId: string;  // Associated notification ID
  channel: string;         // "SignalR" | "Email" | "SMS" | "Teams"
  status: string;          // "delivered" | "failed" | "bounced"
  deliveredAt: string | null;   // ISO 8601 timestamp
  failedAt: string | null;      // ISO 8601 timestamp
  errorMessage: string | null;  // Error message if failed
  responseData: object | null;  // Provider-specific response data
}
```

### Statistics Response Model

```typescript
interface DeliveryStats {
  totalDelivered: number;       // Total successful deliveries
  totalFailed: number;          // Total failed deliveries
  totalPending: number;         // Total pending/retrying
  deliveryRatePercent: number;  // Success rate percentage
  avgDeliveryTimeMs: number;    // Average delivery time in ms
  channelStats: ChannelStats[]; // Per-channel statistics
}

interface ChannelStats {
  channel: string;    // "SignalR" | "Email" | "SMS" | "Teams"
  delivered: number;  // Successful deliveries
  failed: number;     // Failed deliveries
  pending: number;    // Pending/retrying deliveries
}
```

---

## Best Practices

### 1. Monitor Delivery Queue Size

Set up alerts when the queue size exceeds normal thresholds:
- **Normal**: 0-10 pending deliveries
- **Warning**: 10-50 pending deliveries (possible backlog)
- **Critical**: 50+ pending deliveries (delivery pipeline issue)

```bash
# Check queue size periodically
queue_size=$(curl -s "http://192.168.150.52:5201/api/delivery/queue" | jq 'length')
if [ $queue_size -gt 50 ]; then
  echo "CRITICAL: Delivery queue has $queue_size items"
fi
```

### 2. Track Delivery Rate Over Time

Maintain a minimum 90% delivery rate for production systems:
- **Excellent**: 95%+ delivery rate
- **Good**: 90-95% delivery rate
- **Warning**: 85-90% delivery rate (investigate failures)
- **Critical**: <85% delivery rate (major issue)

```bash
# Get current delivery rate
curl -s "http://192.168.150.52:5201/api/delivery/stats" | jq '.deliveryRatePercent'
```

### 3. Handle Channel-Specific Failures

Different channels have different failure modes:

**Email**:
- **Bounces**: Remove invalid email addresses from user profiles
- **SMTP failures**: Check SMTP credentials and server status
- **Rate limits**: Implement backoff strategy

**SMS**:
- **Invalid numbers**: Validate phone number format
- **Provider failures**: Check SMS provider API status
- **Cost overruns**: Monitor SMS volume and costs

**SignalR**:
- **Connection failures**: Check client connectivity
- **Hub errors**: Review SignalR hub logs

### 4. Retry Strategy Guidelines

Follow these guidelines when retrying failed deliveries:

**Automatic Retries**:
- Retry up to 3 times with exponential backoff
- Wait 5 minutes, 15 minutes, 1 hour between attempts
- Give up after 3 failed attempts

**Manual Retries**:
- Use after fixing configuration issues
- Use after service outages are resolved
- Batch retry for bulk failures during known outage windows

**Do Not Retry**:
- Hard bounces (invalid email addresses)
- Invalid phone numbers
- Deliveries to cancelled notifications

### 5. Archive Old Delivery Records

Implement retention policies to keep the delivery history manageable:
- **Keep indefinitely**: Delivered, Failed (for analysis)
- **Archive after 90 days**: Pending, Cancelled
- **Purge after 1 year**: All delivery records

### 6. Use Response Data for Debugging

Provider response data includes valuable debugging information:

```json
{
  "responseData": {
    "messageId": "msg_abc123",      // Provider message ID
    "provider": "SendGrid",         // Provider name
    "deliveryTimeMs": 1523,         // Time to deliver
    "errorCode": "21211",           // Provider error code
    "bounceType": "hard"            // Hard vs soft bounce
  }
}
```

Use this data to:
- Track messages in provider dashboards
- Identify provider-specific issues
- Analyze delivery performance by provider
- Debug specific delivery failures

---

## Performance Considerations

### Response Times

Typical response times for delivery endpoints:
- `GET /api/delivery/queue`: 50-200ms (depends on queue size)
- `GET /api/delivery/history`: 100-500ms (depends on filters and page size)
- `GET /api/delivery/stats`: 200-1000ms (aggregation query)
- `POST /api/delivery/{id}/retry`: 50-100ms
- `DELETE /api/delivery/{id}`: 50-100ms

### Pagination

For large result sets, use pagination to improve performance:
```bash
# Get first page (50 records)
curl -X GET "http://192.168.150.52:5201/api/delivery/history?page=1&pageSize=50"

# Get next page
curl -X GET "http://192.168.150.52:5201/api/delivery/history?page=2&pageSize=50"
```

**Recommended Page Sizes**:
- **UI display**: 25-50 records per page
- **Background processing**: 100-500 records per page
- **Bulk export**: 1000+ records per page

### Caching

Delivery statistics change frequently and should not be cached aggressively:
- **Queue**: No caching (always real-time)
- **History**: Cache for 30-60 seconds
- **Stats**: Cache for 60-120 seconds

---

## Error Handling

All endpoints follow consistent error response patterns:

### 400 Bad Request
```json
{
  "error": "Cannot retry delivery in Delivered status"
}
```

**Causes**:
- Attempting to retry a delivered notification
- Attempting to cancel a completed delivery
- Invalid state transition

### 404 Not Found

Returned implicitly when delivery record is not found (wrapped in `InvalidOperationException` with 400 Bad Request).

**Causes**:
- Invalid delivery ID
- Delivery record deleted
- Typo in delivery ID

### 500 Internal Server Error
```json
{
  "error": "Failed to retrieve delivery queue"
}
```

**Causes**:
- Database connection issues
- Service unavailability
- Unexpected exceptions

**Recovery**:
- Check service health
- Review application logs
- Retry request after brief delay

---

## Additional Resources

- **Notifications API**: See [Notifications Endpoints](./notifications-endpoints.md) for creating and managing notifications
- **SignalR Integration**: See [SignalR Hub Documentation](./signalr-hub.md) for real-time delivery
- **Email Templates**: See [Email Templates](./email-templates.md) for email notification formatting
- **SMS Integration**: See [SMS Integration](./sms-integration.md) for SMS delivery configuration
- **Service Architecture**: See [Architecture Overview](../architecture/notification-service-architecture.md)

---

## Support

For issues or questions:
- Create an issue in the project repository
- Contact the ops team at `ops@plansource.com`
- Check the [Troubleshooting Guide](./troubleshooting.md)
