# Routing Events API Endpoints

## Overview

The Routing Events API provides endpoints for querying and analyzing outbound notification events that have been routed through the notification system. These endpoints allow you to track event creation, routing decisions, and delivery status across multiple channels (Email, SignalR, SMS, Teams).

**Base URL**: `http://192.168.150.52:5201/api/routing/events`

**Authentication**: Authentication requirements vary by endpoint (documented per endpoint below).

---

## Key Concepts

### Outbound Events
An **Outbound Event** represents a notification that has been published by a source service and routed to recipients based on routing policies. Each event can result in multiple deliveries to different contacts through different channels.

### Event Lifecycle
1. **Event Published**: A source service publishes an event with topic, severity, and content
2. **Routing Applied**: The routing engine matches the event to routing policies based on filters
3. **Deliveries Created**: One delivery record is created per recipient per channel
4. **Delivery Processing**: Each delivery is sent through its respective channel (Email, SignalR, etc.)
5. **Status Tracking**: Delivery status is updated (Pending → Processing → Delivered/Failed)

---

## Endpoints

### 1. List Outbound Events

Retrieves a paginated list of outbound events with optional filtering. Each event summary includes delivery counts for quick status assessment.

**Endpoint**: `GET /api/routing/events`

**Authentication**: Not specified (likely required in production)

**Query Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `clientId` | String | No | Filter by client ID |
| `service` | String (Enum) | No | Filter by source service (see [SourceService Enum](#sourceservice-enum)) |
| `topic` | String (Enum) | No | Filter by notification topic (see [NotificationTopic Enum](#notificationtopic-enum)) |
| `sagaId` | Guid | No | Filter by saga ID |
| `fromDate` | DateTime | No | Filter events created on or after this date |
| `toDate` | DateTime | No | Filter events created on or before this date |
| `page` | Int | No | Page number (default: 1) |
| `pageSize` | Int | No | Number of items per page (default: 20) |

**Response**: `200 OK`
```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "service": "CensusReconciliation",
      "topic": "ReconciliationComplete",
      "clientId": "CLIENT123",
      "severity": "Info",
      "subject": "Census reconciliation completed successfully",
      "sagaId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "createdAt": "2025-12-18T10:30:00Z",
      "processedAt": "2025-12-18T10:30:05Z",
      "deliveryCount": 3,
      "pendingCount": 0,
      "deliveredCount": 3,
      "failedCount": 0
    },
    {
      "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "service": "CensusReconciliation",
      "topic": "WorkflowStuck",
      "clientId": "CLIENT123",
      "severity": "Critical",
      "subject": "Workflow stuck in ProcessingFile state",
      "sagaId": "1b9d6bcd-bbfd-4b2d-9b5d-ab8dfbbd4bed",
      "createdAt": "2025-12-18T10:15:00Z",
      "processedAt": "2025-12-18T10:15:02Z",
      "deliveryCount": 5,
      "pendingCount": 1,
      "deliveredCount": 3,
      "failedCount": 1
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 42,
  "totalPages": 3,
  "hasNext": true,
  "hasPrevious": false
}
```

**Response Fields**:

**PaginatedResponse**:
| Field | Type | Description |
|-------|------|-------------|
| `data` | Array | Array of OutboundEventSummary objects |
| `page` | Int | Current page number |
| `pageSize` | Int | Number of items per page |
| `totalItems` | Int | Total number of events matching the filter |
| `totalPages` | Int | Total number of pages |
| `hasNext` | Boolean | Whether there is a next page |
| `hasPrevious` | Boolean | Whether there is a previous page |

**OutboundEventSummary**:
| Field | Type | Description |
|-------|------|-------------|
| `id` | Guid | Unique event identifier |
| `service` | String | Source service that published the event |
| `topic` | String | Notification topic/category |
| `clientId` | String | Associated client ID (nullable) |
| `severity` | String | Event severity level |
| `subject` | String | Event subject/title (nullable) |
| `sagaId` | Guid | Associated saga ID (nullable) |
| `createdAt` | DateTime | When the event was created |
| `processedAt` | DateTime | When routing was processed (nullable) |
| `deliveryCount` | Int | Total number of deliveries created |
| `pendingCount` | Int | Number of deliveries still pending |
| `deliveredCount` | Int | Number of successfully delivered notifications |
| `failedCount` | Int | Number of failed deliveries |

**Error Responses**:
- `500 Internal Server Error`: Error retrieving events

**Example Usage**:
```bash
# Get all events
curl -X GET "http://192.168.150.52:5201/api/routing/events"

# Filter by client and service
curl -X GET "http://192.168.150.52:5201/api/routing/events?clientId=CLIENT123&service=CensusReconciliation"

# Filter by saga
curl -X GET "http://192.168.150.52:5201/api/routing/events?sagaId=9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d"

# Filter by date range
curl -X GET "http://192.168.150.52:5201/api/routing/events?fromDate=2025-12-18T00:00:00Z&toDate=2025-12-18T23:59:59Z"

# Filter by topic and severity
curl -X GET "http://192.168.150.52:5201/api/routing/events?topic=WorkflowStuck&page=2&pageSize=50"
```

---

### 2. Get Event by ID

Retrieves detailed information about a specific outbound event, including its full content, payload, and all delivery details with contact information.

**Endpoint**: `GET /api/routing/events/{id}`

**Authentication**: Not specified (likely required in production)

**Path Parameters**:
- `id` (Guid, required): The event ID

**Response**: `200 OK`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "service": "CensusReconciliation",
  "topic": "WorkflowStuck",
  "clientId": "CLIENT123",
  "severity": "Critical",
  "templateId": "workflow-stuck-template",
  "subject": "Workflow stuck in ProcessingFile state",
  "body": "The reconciliation workflow for CLIENT123 has been stuck in the ProcessingFile state for over 2 hours. Manual intervention may be required.",
  "payload": {
    "sagaId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "clientName": "Example Corporation",
    "state": "ProcessingFile",
    "stuckDurationMinutes": 145,
    "fileName": "census_2025_12_18.csv",
    "retryCount": 3
  },
  "sagaId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "correlationId": "abc123-correlation-id",
  "createdAt": "2025-12-18T10:30:00Z",
  "processedAt": "2025-12-18T10:30:02Z",
  "deliveries": [
    {
      "id": "c9168c5e-5f32-4b3f-8b3e-7d5a5c4e5f3a",
      "contactId": "e4d5c6b7-8a9f-4e3d-9c8b-7a6f5e4d3c2b",
      "contactName": "John Doe",
      "contactEmail": "john.doe@example.com",
      "channel": "Email",
      "role": "To",
      "status": "Delivered",
      "createdAt": "2025-12-18T10:30:02Z",
      "sentAt": "2025-12-18T10:30:15Z",
      "deliveredAt": "2025-12-18T10:30:18Z",
      "failedAt": null,
      "errorMessage": null,
      "attemptCount": 1
    },
    {
      "id": "d8259d6f-9g43-5c4g-9c9f-8e6b6d5e6g4b",
      "contactId": "f5e6d7c8-9b0g-5f4e-0d9c-8b7g6f5e4d3c",
      "contactName": "Jane Smith",
      "contactEmail": "jane.smith@example.com",
      "channel": "SignalR",
      "role": "To",
      "status": "Delivered",
      "createdAt": "2025-12-18T10:30:02Z",
      "sentAt": "2025-12-18T10:30:03Z",
      "deliveredAt": "2025-12-18T10:30:03Z",
      "failedAt": null,
      "errorMessage": null,
      "attemptCount": 1
    },
    {
      "id": "e9360e7g-0h54-6d5h-0d0g-9f7c7e6f7h5c",
      "contactId": "g6f7e8d9-0c1h-6g5f-1e0d-9c8h7g6f5e4d",
      "contactName": "Operations Team",
      "contactEmail": "ops@example.com",
      "channel": "Email",
      "role": "Cc",
      "status": "Pending",
      "createdAt": "2025-12-18T10:30:02Z",
      "sentAt": null,
      "deliveredAt": null,
      "failedAt": null,
      "errorMessage": null,
      "attemptCount": 0
    }
  ]
}
```

**Response Fields**:

**OutboundEventDetails**:
| Field | Type | Description |
|-------|------|-------------|
| `id` | Guid | Unique event identifier |
| `service` | String | Source service that published the event |
| `topic` | String | Notification topic/category |
| `clientId` | String | Associated client ID (nullable) |
| `severity` | String | Event severity level |
| `templateId` | String | Template ID used for rendering (nullable) |
| `subject` | String | Event subject/title (nullable) |
| `body` | String | Full event message body (nullable) |
| `payload` | Object | Structured data payload (key-value pairs) |
| `sagaId` | Guid | Associated saga ID (nullable) |
| `correlationId` | Guid | Correlation ID for distributed tracing (nullable) |
| `createdAt` | DateTime | When the event was created |
| `processedAt` | DateTime | When routing was processed (nullable) |
| `deliveries` | Array | Array of DeliveryInfo objects |

**DeliveryInfo**:
| Field | Type | Description |
|-------|------|-------------|
| `id` | Guid | Unique delivery identifier |
| `contactId` | Guid | ID of the recipient contact |
| `contactName` | String | Display name of the recipient |
| `contactEmail` | String | Email address of the recipient |
| `channel` | String | Delivery channel (SignalR, Email, SMS, Teams) |
| `role` | String | Recipient role (To, Cc, Bcc) |
| `status` | String | Current delivery status |
| `createdAt` | DateTime | When the delivery was created |
| `sentAt` | DateTime | When the notification was sent (nullable) |
| `deliveredAt` | DateTime | When delivery was confirmed (nullable) |
| `failedAt` | DateTime | When the delivery failed (nullable) |
| `errorMessage` | String | Error message if delivery failed (nullable) |
| `attemptCount` | Int | Number of delivery attempts |

**Error Responses**:
- `404 Not Found`: Event not found
- `500 Internal Server Error`: Error retrieving event

**Example Usage**:
```bash
# Get event by ID
curl -X GET "http://192.168.150.52:5201/api/routing/events/3fa85f64-5717-4562-b3fc-2c963f66afa6"

# Using PowerShell
Invoke-RestMethod -Uri "http://192.168.150.52:5201/api/routing/events/3fa85f64-5717-4562-b3fc-2c963f66afa6" -Method Get
```

---

### 3. Get Events by Saga

Retrieves all outbound events associated with a specific saga. Useful for tracking all notifications generated during a workflow's lifecycle.

**Endpoint**: `GET /api/routing/events/saga/{sagaId}`

**Authentication**: Not specified (likely required in production)

**Path Parameters**:
- `sagaId` (Guid, required): The saga ID

**Response**: `200 OK`
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "service": "CensusReconciliation",
    "topic": "FileProcessingStarted",
    "clientId": "CLIENT123",
    "severity": "Info",
    "subject": "Census file processing started",
    "sagaId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "createdAt": "2025-12-18T08:00:00Z",
    "processedAt": "2025-12-18T08:00:01Z",
    "deliveryCount": 2,
    "pendingCount": 0,
    "deliveredCount": 2,
    "failedCount": 0
  },
  {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "service": "CensusReconciliation",
    "topic": "WorkflowStuck",
    "clientId": "CLIENT123",
    "severity": "Critical",
    "subject": "Workflow stuck in ProcessingFile state",
    "sagaId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "createdAt": "2025-12-18T10:30:00Z",
    "processedAt": "2025-12-18T10:30:02Z",
    "deliveryCount": 5,
    "pendingCount": 1,
    "deliveredCount": 3,
    "failedCount": 1
  },
  {
    "id": "8d0f7780-8536-51ef-a55c-f18gc2g01bf8",
    "service": "CensusReconciliation",
    "topic": "ReconciliationComplete",
    "clientId": "CLIENT123",
    "severity": "Info",
    "subject": "Census reconciliation completed successfully",
    "sagaId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "createdAt": "2025-12-18T12:45:00Z",
    "processedAt": "2025-12-18T12:45:01Z",
    "deliveryCount": 3,
    "pendingCount": 0,
    "deliveredCount": 3,
    "failedCount": 0
  }
]
```

**Response**: Array of `OutboundEventSummary` objects (same structure as List Events endpoint)

**Error Responses**:
- `500 Internal Server Error`: Error retrieving events

**Example Usage**:
```bash
# Get all events for a saga
curl -X GET "http://192.168.150.52:5201/api/routing/events/saga/9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d"

# Using PowerShell
Invoke-RestMethod -Uri "http://192.168.150.52:5201/api/routing/events/saga/9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d" -Method Get
```

---

## Common Workflows

### Workflow 1: Monitor Events for a Client

1. Call `GET /api/routing/events?clientId=CLIENT123&page=1&pageSize=50`
2. Review event summaries to identify delivery issues
3. For events with failed deliveries, drill down using `GET /api/routing/events/{id}`
4. Check delivery details to identify specific failures and error messages

### Workflow 2: Track Saga Notification Timeline

1. Call `GET /api/routing/events/saga/{sagaId}` to get all events for a workflow
2. Sort events by `createdAt` to build a timeline
3. Identify notification patterns (e.g., escalations, stuck notifications)
4. Correlate with saga state transitions to validate notification triggers

### Workflow 3: Diagnose Delivery Failures

1. Call `GET /api/routing/events?fromDate=2025-12-18T00:00:00Z&toDate=2025-12-18T23:59:59Z`
2. Filter results to find events with `failedCount > 0`
3. For each failed event, call `GET /api/routing/events/{id}`
4. Review `deliveries` array to find failed deliveries
5. Check `errorMessage` and `attemptCount` for failure details
6. Identify patterns (e.g., specific channel failures, contact issues)

### Workflow 4: Audit Notification Coverage

1. Call `GET /api/routing/events?service=CensusReconciliation&topic=WorkflowStuck`
2. Review `deliveryCount` for each event
3. Drill down on specific events to verify routing policy effectiveness
4. Check if all expected contacts received notifications
5. Validate that channel distribution matches routing policy configuration

### Workflow 5: Filter Events by Date Range and Severity

1. Call `GET /api/routing/events?fromDate=2025-12-01T00:00:00Z&toDate=2025-12-31T23:59:59Z&topic=ReconciliationEscalation`
2. Review events for patterns in escalations
3. Identify clients with frequent escalations
4. Correlate with routing policy changes or system issues

---

## Data Models

### SourceService Enum

```csharp
public enum SourceService
{
    CensusAutomation,        // Census automation and file processing
    PayrollFileGeneration,   // Payroll file generation service
    CensusReconciliation,    // Census reconciliation workflow service
    CensusOrchestration,     // Census orchestration service
    PlanSourceIntegration    // PlanSource integration service
}
```

**Valid String Values**: `CensusAutomation`, `PayrollFileGeneration`, `CensusReconciliation`, `CensusOrchestration`, `PlanSourceIntegration`

### NotificationTopic Enum

```csharp
public enum NotificationTopic
{
    // Census Automation
    DailyImportSuccess,           // Daily import completed successfully
    DailyImportFailure,           // Daily import failed
    SchemaValidationError,        // Schema validation error detected
    RecordCountMismatch,          // Record count mismatch detected
    FileProcessingStarted,        // File processing has started
    FileProcessingCompleted,      // File processing completed

    // Payroll
    PayrollFileGenerated,         // Payroll file was generated successfully
    PayrollFileError,             // Payroll file generation failed
    PayrollFilePending,           // Payroll file is pending approval
    PayrollFileApproved,          // Payroll file was approved

    // Reconciliation
    ReconciliationComplete,       // Reconciliation workflow completed
    ReconciliationEscalation,     // Workflow was escalated for attention
    WorkflowStuck,                // Workflow is stuck and needs intervention
    ManualInterventionRequired,   // Manual intervention is required
    RetryLimitExceeded,           // Workflow retry limit exceeded

    // General
    SystemAlert,                  // System-level alert
    HealthCheckFailure,           // Service health check failure
    Custom                        // Custom topic for ad-hoc notifications
}
```

**Common Topic Values**: `DailyImportSuccess`, `DailyImportFailure`, `FileProcessingStarted`, `FileProcessingCompleted`, `ReconciliationComplete`, `ReconciliationEscalation`, `WorkflowStuck`, `ManualInterventionRequired`, `RetryLimitExceeded`, `PayrollFileGenerated`, `SystemAlert`

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

**Valid String Values**: `Info`, `Warning`, `Urgent`, `Critical`

### NotificationChannel Enum

```csharp
public enum NotificationChannel
{
    SignalR,   // Real-time notification via SignalR
    Email,     // Email notification
    SMS,       // SMS notification
    Teams      // Microsoft Teams notification (Phase 3)
}
```

**Valid String Values**: `SignalR`, `Email`, `SMS`, `Teams`

### DeliveryRole Enum

```csharp
public enum DeliveryRole
{
    To,   // Primary recipient (To field)
    Cc,   // Carbon copy recipient (CC field)
    Bcc   // Blind carbon copy recipient (BCC field)
}
```

**Valid String Values**: `To`, `Cc`, `Bcc`

### DeliveryStatus Enum

```csharp
public enum DeliveryStatus
{
    Pending,      // Delivery is queued and waiting to be sent
    Processing,   // Delivery is currently being processed
    Delivered,    // Delivery was successful
    Failed,       // Delivery failed and will be retried
    Bounced,      // Delivery bounced (email specific)
    Cancelled     // Delivery was cancelled
}
```

**Valid String Values**: `Pending`, `Processing`, `Delivered`, `Failed`, `Bounced`, `Cancelled`

---

## Filtering Best Practices

### 1. Use Specific Filters for Performance

When querying large datasets, use the most specific filters available:
```bash
# Good - Specific saga filter
curl -X GET "http://192.168.150.52:5201/api/routing/events?sagaId=9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d"

# Better - Combine filters
curl -X GET "http://192.168.150.52:5201/api/routing/events?clientId=CLIENT123&service=CensusReconciliation&fromDate=2025-12-18T00:00:00Z"
```

### 2. Use Date Ranges for Historical Analysis

Always specify date ranges when analyzing historical data:
```bash
# Last 24 hours
curl -X GET "http://192.168.150.52:5201/api/routing/events?fromDate=2025-12-17T10:00:00Z&toDate=2025-12-18T10:00:00Z"

# Specific business day
curl -X GET "http://192.168.150.52:5201/api/routing/events?fromDate=2025-12-18T00:00:00Z&toDate=2025-12-18T23:59:59Z"
```

### 3. Pagination for Large Result Sets

Use appropriate page sizes based on your use case:
```bash
# UI display (smaller pages)
curl -X GET "http://192.168.150.52:5201/api/routing/events?page=1&pageSize=20"

# Bulk export (larger pages)
curl -X GET "http://192.168.150.52:5201/api/routing/events?page=1&pageSize=100"
```

### 4. Combine Topic and Severity for Alerts

Filter critical events by topic and severity:
```bash
# Critical workflow issues
curl -X GET "http://192.168.150.52:5201/api/routing/events?topic=WorkflowStuck&service=CensusReconciliation"

# All escalations for a client
curl -X GET "http://192.168.150.52:5201/api/routing/events?clientId=CLIENT123&topic=ReconciliationEscalation"
```

---

## Integration Examples

### PowerShell Integration

```powershell
# Get events and analyze delivery success rate
$baseUrl = "http://192.168.150.52:5201/api/routing/events"
$response = Invoke-RestMethod -Uri "$baseUrl?page=1&pageSize=100" -Method Get

$totalDeliveries = ($response.data | Measure-Object -Property deliveryCount -Sum).Sum
$totalDelivered = ($response.data | Measure-Object -Property deliveredCount -Sum).Sum
$successRate = ($totalDelivered / $totalDeliveries) * 100

Write-Host "Delivery Success Rate: $successRate%"
Write-Host "Total Events: $($response.totalItems)"
Write-Host "Total Deliveries: $totalDeliveries"
Write-Host "Successful Deliveries: $totalDelivered"
```

### C# Integration

```csharp
using System.Net.Http.Json;

public class RoutingEventsClient
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://192.168.150.52:5201/api/routing/events";

    public async Task<PaginatedResponse<OutboundEventSummary>> GetEventsAsync(
        string? clientId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(clientId))
            queryParams.Add($"clientId={Uri.EscapeDataString(clientId)}");

        if (fromDate.HasValue)
            queryParams.Add($"fromDate={fromDate.Value:O}");

        if (toDate.HasValue)
            queryParams.Add($"toDate={toDate.Value:O}");

        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var url = $"{BaseUrl}?{string.Join("&", queryParams)}";
        return await _httpClient.GetFromJsonAsync<PaginatedResponse<OutboundEventSummary>>(url);
    }

    public async Task<OutboundEventDetails> GetEventByIdAsync(Guid eventId)
    {
        return await _httpClient.GetFromJsonAsync<OutboundEventDetails>($"{BaseUrl}/{eventId}");
    }

    public async Task<List<OutboundEventSummary>> GetEventsBySagaAsync(Guid sagaId)
    {
        return await _httpClient.GetFromJsonAsync<List<OutboundEventSummary>>($"{BaseUrl}/saga/{sagaId}");
    }
}
```

### JavaScript/TypeScript Integration

```typescript
interface RoutingEventsQuery {
  clientId?: string;
  service?: string;
  topic?: string;
  sagaId?: string;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

class RoutingEventsClient {
  private baseUrl = 'http://192.168.150.52:5201/api/routing/events';

  async getEvents(query: RoutingEventsQuery = {}): Promise<PaginatedResponse<OutboundEventSummary>> {
    const params = new URLSearchParams();

    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        params.append(key, String(value));
      }
    });

    const response = await fetch(`${this.baseUrl}?${params}`);
    if (!response.ok) throw new Error('Failed to fetch events');
    return response.json();
  }

  async getEventById(eventId: string): Promise<OutboundEventDetails> {
    const response = await fetch(`${this.baseUrl}/${eventId}`);
    if (!response.ok) throw new Error('Event not found');
    return response.json();
  }

  async getEventsBySaga(sagaId: string): Promise<OutboundEventSummary[]> {
    const response = await fetch(`${this.baseUrl}/saga/${sagaId}`);
    if (!response.ok) throw new Error('Failed to fetch saga events');
    return response.json();
  }
}
```

---

## Error Handling

All endpoints follow consistent error response patterns:

### 404 Not Found
```json
{
  "error": "Event 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found"
}
```

### 500 Internal Server Error
```json
{
  "error": "Error retrieving events"
}
```

**Error Handling Best Practice**:
```csharp
try
{
    var eventDetails = await client.GetEventByIdAsync(eventId);
    // Process event
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    _logger.LogWarning("Event {EventId} not found", eventId);
    return null;
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Failed to retrieve event {EventId}", eventId);
    throw;
}
```

---

## Performance Considerations

### Query Optimization

1. **Use Specific Filters**: Filtering by `sagaId` or `clientId` uses indexed columns
2. **Limit Date Ranges**: Avoid querying months of data without date filters
3. **Appropriate Page Sizes**: Default 20 items is suitable for UI, increase for bulk operations
4. **Avoid Wildcard Searches**: The API doesn't support wildcard searches on string fields

### Caching Recommendations

```csharp
// Cache event details for 5 minutes
var cacheKey = $"routing-event-{eventId}";
var cached = await _cache.GetAsync<OutboundEventDetails>(cacheKey);

if (cached == null)
{
    cached = await client.GetEventByIdAsync(eventId);
    await _cache.SetAsync(cacheKey, cached, TimeSpan.FromMinutes(5));
}
```

---

## Additional Resources

- **Routing Policies API**: See [Routing Policies Endpoints](./routing-policies-endpoints.md) for managing routing rules
- **Subscriptions API**: See [Subscriptions Endpoints](./subscriptions-endpoints.md) for user subscription management
- **Delivery API**: See [Delivery Endpoints](./delivery-endpoints.md) for delivery-specific operations
- **Notification Architecture**: See [Notification Routing Architecture](../architecture/notification-routing-architecture.md)

---

## Support

For issues or questions:
- Create an issue in the project repository
- Contact the ops team at `ops@plansource.com`
- Check the [Troubleshooting Guide](./troubleshooting.md)
