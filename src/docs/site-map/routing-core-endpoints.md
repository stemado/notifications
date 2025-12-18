# Routing Core Endpoints

## Overview

The Routing Core endpoints provide the foundation for the NotificationService's outbound notification routing system. These endpoints enable you to monitor routing performance, publish outbound events, and inspect client-specific routing configurations.

**Base URL**: `/api/routing`

## Endpoints

### GET /api/routing/dashboard

Get a comprehensive routing dashboard with system-wide statistics, recent events, and failed deliveries.

#### Purpose

The dashboard endpoint provides a real-time overview of the entire notification routing system. Use this endpoint to monitor system health, track delivery success rates, and identify issues requiring attention.

#### Request

```http
GET /api/routing/dashboard HTTP/1.1
Host: localhost:5201
```

No parameters required.

#### Response

**Status Code**: `200 OK`

**Response Body**:

```json
{
  "statistics": {
    "totalContacts": 42,
    "activeContacts": 38,
    "totalGroups": 15,
    "activeGroups": 13,
    "totalPolicies": 28,
    "enabledPolicies": 25,
    "eventsToday": 156,
    "deliveriesToday": 312,
    "successRateToday": 98.5,
    "pendingDeliveries": 3,
    "failedDeliveries": 2
  },
  "recentEvents": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "service": "CensusReconciliation",
      "topic": "ReconciliationComplete",
      "clientId": "LSR7",
      "severity": "Info",
      "subject": "Reconciliation completed for LSR7 - 2025-12-18",
      "sagaId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "createdAt": "2025-12-18T10:30:00Z",
      "processedAt": "2025-12-18T10:30:02Z",
      "deliveryCount": 5,
      "pendingCount": 0,
      "deliveredCount": 5,
      "failedCount": 0
    }
  ],
  "failedDeliveries": [
    {
      "id": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "eventId": "8f14e45f-ceea-467a-9af1-8e8f0b5e0e3d",
      "contactName": "John Smith",
      "contactEmail": "john.smith@example.com",
      "channel": "Email",
      "status": "Failed",
      "createdAt": "2025-12-18T09:15:00Z",
      "errorMessage": "SMTP server connection timeout"
    }
  ],
  "topClients": [
    {
      "clientId": "LSR7",
      "totalGroups": 4,
      "totalPolicies": 12,
      "activePolicies": 12,
      "uniqueContacts": 8,
      "eventsLast7Days": 35,
      "deliveriesLast7Days": 140,
      "successRate": 99.3
    }
  ]
}
```

#### Dashboard Components

##### Statistics

System-wide metrics providing a snapshot of routing system health.

| Field | Type | Description |
|-------|------|-------------|
| `totalContacts` | integer | Total contacts in the system |
| `activeContacts` | integer | Contacts currently active (not deactivated) |
| `totalGroups` | integer | Total recipient groups |
| `activeGroups` | integer | Active recipient groups |
| `totalPolicies` | integer | Total routing policies (all clients) |
| `enabledPolicies` | integer | Policies currently enabled |
| `eventsToday` | integer | Outbound events published today |
| `deliveriesToday` | integer | Total delivery attempts today |
| `successRateToday` | number | Today's delivery success rate (0-100) |
| `pendingDeliveries` | integer | Deliveries awaiting processing |
| `failedDeliveries` | integer | Deliveries in failed status |

##### Recent Events

The 10 most recent outbound events, showing event details and delivery status.

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Event unique identifier |
| `service` | string | Source service (see [Source Services](#source-services)) |
| `topic` | string | Notification topic (see [Notification Topics](#notification-topics)) |
| `clientId` | string | Client identifier (null for global events) |
| `severity` | string | Severity level (see [Severity Levels](#severity-levels)) |
| `subject` | string | Event subject line |
| `sagaId` | GUID | Optional saga correlation ID |
| `createdAt` | datetime | Event creation timestamp |
| `processedAt` | datetime | When routing processing completed |
| `deliveryCount` | integer | Total deliveries created |
| `pendingCount` | integer | Deliveries pending |
| `deliveredCount` | integer | Successfully delivered |
| `failedCount` | integer | Failed deliveries |

##### Failed Deliveries

Recent failed delivery attempts requiring attention.

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Delivery unique identifier |
| `eventId` | GUID | Parent event ID |
| `contactName` | string | Recipient contact name |
| `contactEmail` | string | Recipient email address |
| `channel` | string | Delivery channel (Email, SignalR, SMS, Teams) |
| `status` | string | Current status (typically "Failed") |
| `createdAt` | datetime | Delivery creation timestamp |
| `errorMessage` | string | Error description |

##### Top Clients

Client-specific routing statistics for the most active clients.

| Field | Type | Description |
|-------|------|-------------|
| `clientId` | string | Client identifier |
| `totalGroups` | integer | Recipient groups for this client |
| `totalPolicies` | integer | Routing policies (client + defaults) |
| `activePolicies` | integer | Enabled policies |
| `uniqueContacts` | integer | Unique contacts receiving notifications |
| `eventsLast7Days` | integer | Events in the last 7 days |
| `deliveriesLast7Days` | integer | Deliveries in the last 7 days |
| `successRate` | number | 7-day delivery success rate (0-100) |

#### Use Cases

**System Health Monitoring**
```bash
# Check overall system health
curl http://localhost:5201/api/routing/dashboard | jq '.statistics'

# Monitor today's success rate
curl http://localhost:5201/api/routing/dashboard | jq '.statistics.successRateToday'
```

**Troubleshooting**
```bash
# Check for failed deliveries
curl http://localhost:5201/api/routing/dashboard | jq '.failedDeliveries'

# View recent events
curl http://localhost:5201/api/routing/dashboard | jq '.recentEvents[] | {topic, severity, deliveredCount, failedCount}'
```

**Client Performance**
```bash
# View top clients by activity
curl http://localhost:5201/api/routing/dashboard | jq '.topClients'
```

---

### POST /api/routing/publish

Publish an outbound event to be routed to recipients based on routing policies.

#### Purpose

This is the primary entry point for publishing outbound notifications. When you publish an event, the routing system:

1. Evaluates all matching routing policies
2. Determines recipient groups based on service, topic, client, and severity
3. Creates delivery records for each contact in the matched groups
4. Queues deliveries for processing via the configured channels

#### Request

```http
POST /api/routing/publish HTTP/1.1
Host: localhost:5201
Content-Type: application/json

{
  "service": "CensusReconciliation",
  "topic": "ReconciliationComplete",
  "clientId": "LSR7",
  "severity": "Info",
  "templateId": "reconciliation-complete-v1",
  "subject": "Daily reconciliation completed for LSR7",
  "body": "The daily census reconciliation workflow completed successfully.",
  "payload": {
    "recordCount": 1523,
    "duration": "00:02:34",
    "checksPerformed": 15
  },
  "sagaId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "correlationId": "a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"
}
```

#### Request Body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `service` | string (enum) | Yes | Source service publishing the event (see [Source Services](#source-services)) |
| `topic` | string (enum) | Yes | Notification topic (see [Notification Topics](#notification-topics)) |
| `clientId` | string | No | Client identifier; null for global events |
| `severity` | string (enum) | No | Severity level (default: Info; see [Severity Levels](#severity-levels)) |
| `templateId` | string | No | Template identifier for rendering (future use) |
| `subject` | string | No | Event subject line |
| `body` | string | No | Event body/message |
| `payload` | object | No | Additional structured data for template rendering |
| `sagaId` | GUID | No | Saga correlation ID for workflow tracking |
| `correlationId` | GUID | No | Cross-service correlation ID |

#### Response

**Status Code**: `200 OK`

**Response Body**:

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "service": "CensusReconciliation",
  "topic": "ReconciliationComplete",
  "clientId": "LSR7",
  "severity": "Info",
  "subject": "Daily reconciliation completed for LSR7",
  "sagaId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "createdAt": "2025-12-18T10:30:00Z",
  "processedAt": "2025-12-18T10:30:02Z",
  "deliveryCount": 5,
  "pendingCount": 5,
  "deliveredCount": 0,
  "failedCount": 0
}
```

#### Event Publishing Flow

```
1. Event Received
   ↓
2. Validate Request
   ↓
3. Create OutboundEvent Record
   ↓
4. Query Routing Policies
   - Match by Service + Topic
   - Match by ClientId (or default)
   - Filter by MinSeverity
   ↓
5. Resolve Recipient Groups
   - Get all matching groups
   - Retrieve group members (contacts)
   ↓
6. Create Delivery Records
   - One per contact per policy
   - Set channel and role from policy
   - Initial status: Pending
   ↓
7. Queue for Processing
   - Deliveries await channel processors
   - SignalR: immediate
   - Email/SMS: background jobs
   ↓
8. Return Event Summary
```

#### Use Cases

**Publish Workflow Completion**
```bash
curl -X POST http://localhost:5201/api/routing/publish \
  -H "Content-Type: application/json" \
  -d '{
    "service": "CensusReconciliation",
    "topic": "ReconciliationComplete",
    "clientId": "LSR7",
    "severity": "Info",
    "subject": "Reconciliation completed",
    "body": "Daily workflow completed successfully",
    "sagaId": "7c9e6679-7425-40de-944b-e07fc1f90ae7"
  }'
```

**Publish Critical Alert**
```bash
curl -X POST http://localhost:5201/api/routing/publish \
  -H "Content-Type: application/json" \
  -d '{
    "service": "CensusReconciliation",
    "topic": "WorkflowStuck",
    "clientId": "LSR7",
    "severity": "Critical",
    "subject": "URGENT: Workflow stuck in AwaitingAggregates",
    "body": "Manual intervention required",
    "payload": {
      "sagaId": "abc123",
      "currentState": "AwaitingAggregates",
      "retryCount": 10
    }
  }'
```

**Publish with Template**
```bash
curl -X POST http://localhost:5201/api/routing/publish \
  -H "Content-Type: application/json" \
  -d '{
    "service": "PayrollFileGeneration",
    "topic": "PayrollFileGenerated",
    "clientId": "LSR7",
    "severity": "Info",
    "templateId": "payroll-generated-v2",
    "payload": {
      "fileName": "LSR7_Payroll_20251218.csv",
      "recordCount": 450,
      "generatedAt": "2025-12-18T08:00:00Z"
    }
  }'
```

#### Error Responses

**400 Bad Request** - Invalid request body
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Service": ["The Service field is required."],
    "Topic": ["The Topic field is required."]
  }
}
```

**500 Internal Server Error** - Server error during processing
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500
}
```

---

### GET /api/routing/clients/{clientId}/configuration

Get the complete routing configuration for a specific client, including groups, policies, contacts, and statistics.

#### Purpose

This endpoint provides a comprehensive view of how notifications are routed for a specific client. Use this to:

- Understand which contacts receive notifications for a client
- Review which policies are active and their priority
- Audit routing coverage across topics
- Troubleshoot missing or incorrect notifications

#### Request

```http
GET /api/routing/clients/LSR7/configuration HTTP/1.1
Host: localhost:5201
```

**Path Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `clientId` | string | Yes | Client identifier |

#### Response

**Status Code**: `200 OK`

**Response Body**:

```json
{
  "clientId": "LSR7",
  "groups": [
    {
      "id": "a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d",
      "name": "LSR7 Operations Team",
      "clientId": "LSR7",
      "description": "Primary operations contacts for LSR7 workflows",
      "purpose": "Production",
      "tags": ["operations", "primary"],
      "isActive": true,
      "memberCount": 5,
      "policyCount": 8
    },
    {
      "id": "f1e2d3c4-b5a6-4758-9291-0a1b2c3d4e5f",
      "name": "Global Escalations",
      "clientId": null,
      "description": "Global escalation contacts for all clients",
      "purpose": "Production",
      "tags": ["global", "escalations"],
      "isActive": true,
      "memberCount": 3,
      "policyCount": 4
    }
  ],
  "policies": [
    {
      "id": "b2c3d4e5-f6a7-4b58-9c0d-1e2f3a4b5c6d",
      "service": "CensusReconciliation",
      "topic": "ReconciliationComplete",
      "clientId": "LSR7",
      "minSeverity": null,
      "channel": "Email",
      "recipientGroupId": "a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d",
      "recipientGroupName": "LSR7 Operations Team",
      "role": "To",
      "priority": 0,
      "isEnabled": true
    },
    {
      "id": "c3d4e5f6-a7b8-4c59-0d1e-2f3a4b5c6d7e",
      "service": "CensusReconciliation",
      "topic": "WorkflowStuck",
      "clientId": "LSR7",
      "minSeverity": "Urgent",
      "channel": "SignalR",
      "recipientGroupId": "a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d",
      "recipientGroupName": "LSR7 Operations Team",
      "role": "To",
      "priority": 10,
      "isEnabled": true
    }
  ],
  "contacts": [
    {
      "id": "d4e5f6a7-b8c9-4d5a-1e2f-3a4b5c6d7e8f",
      "name": "Jane Doe",
      "email": "jane.doe@lsr7.org",
      "phone": "+1-555-0100",
      "organization": "LSR7 School District",
      "isActive": true,
      "createdAt": "2025-01-15T09:00:00Z",
      "groupCount": 2
    }
  ],
  "stats": {
    "totalGroups": 2,
    "totalPolicies": 12,
    "activePolicies": 12,
    "uniqueContacts": 8,
    "policyCoverageByTopic": 6
  }
}
```

#### Client Configuration Structure

##### Groups

Recipient groups assigned to this client (includes both client-specific and global groups).

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Group unique identifier |
| `name` | string | Group name |
| `clientId` | string | Client ID (null for global groups) |
| `description` | string | Group description |
| `purpose` | string | Group purpose (Production, Testing, Development) |
| `tags` | array | Searchable tags |
| `isActive` | boolean | Whether group is active |
| `memberCount` | integer | Number of contacts in group |
| `policyCount` | integer | Number of policies using this group |

##### Policies

Routing policies that apply to this client (includes client-specific and default policies).

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Policy unique identifier |
| `service` | string | Source service (see [Source Services](#source-services)) |
| `topic` | string | Notification topic (see [Notification Topics](#notification-topics)) |
| `clientId` | string | Client ID (null for default policies) |
| `minSeverity` | string | Minimum severity to trigger (null = all) |
| `channel` | string | Delivery channel (SignalR, Email, SMS, Teams) |
| `recipientGroupId` | GUID | Target recipient group |
| `recipientGroupName` | string | Group name for reference |
| `role` | string | Delivery role (To, Cc, Bcc) |
| `priority` | integer | Policy priority (higher = processed first) |
| `isEnabled` | boolean | Whether policy is active |

##### Contacts

Unique contacts who receive notifications for this client (aggregated from all groups).

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Contact unique identifier |
| `name` | string | Contact name |
| `email` | string | Email address |
| `phone` | string | Phone number (optional) |
| `organization` | string | Organization name (optional) |
| `isActive` | boolean | Whether contact is active |
| `createdAt` | datetime | Contact creation timestamp |
| `groupCount` | integer | Number of groups this contact belongs to |

##### Stats

Summary statistics for this client's routing configuration.

| Field | Type | Description |
|-------|------|-------------|
| `totalGroups` | integer | Total groups (client + global) |
| `totalPolicies` | integer | Total policies (client + default) |
| `activePolicies` | integer | Enabled policies |
| `uniqueContacts` | integer | Unique contacts across all groups |
| `policyCoverageByTopic` | integer | Number of unique topics with policies |

#### Use Cases

**Audit Client Routing**
```bash
# Get full configuration
curl http://localhost:5201/api/routing/clients/LSR7/configuration | jq .

# List all contacts
curl http://localhost:5201/api/routing/clients/LSR7/configuration | jq '.contacts[] | {name, email, groupCount}'

# List enabled policies
curl http://localhost:5201/api/routing/clients/LSR7/configuration | jq '.policies[] | select(.isEnabled) | {service, topic, channel}'
```

**Check Policy Coverage**
```bash
# View coverage statistics
curl http://localhost:5201/api/routing/clients/LSR7/configuration | jq '.stats'

# List policies by priority
curl http://localhost:5201/api/routing/clients/LSR7/configuration | jq '.policies | sort_by(.priority) | reverse'
```

**Troubleshoot Missing Notifications**
```bash
# Check for WorkflowStuck policies
curl http://localhost:5201/api/routing/clients/LSR7/configuration | jq '.policies[] | select(.topic == "WorkflowStuck")'

# Verify contact is in groups
curl http://localhost:5201/api/routing/clients/LSR7/configuration | jq '.contacts[] | select(.email == "jane.doe@lsr7.org")'
```

#### Error Responses

**404 Not Found** - Client has no configuration
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404
}
```

---

## Reference

### Source Services

Services that can publish outbound notifications:

| Service | Description |
|---------|-------------|
| `CensusAutomation` | Census automation and file processing |
| `PayrollFileGeneration` | Payroll file generation service |
| `CensusReconciliation` | Census reconciliation workflow service |
| `CensusOrchestration` | Census orchestration service |
| `PlanSourceIntegration` | PlanSource integration service |

### Notification Topics

Available notification topics organized by category:

#### Census Automation
- `DailyImportSuccess` - Daily import completed successfully
- `DailyImportFailure` - Daily import failed
- `SchemaValidationError` - Schema validation error detected
- `RecordCountMismatch` - Record count mismatch detected
- `FileProcessingStarted` - File processing has started
- `FileProcessingCompleted` - File processing completed

#### Payroll
- `PayrollFileGenerated` - Payroll file was generated successfully
- `PayrollFileError` - Payroll file generation failed
- `PayrollFilePending` - Payroll file is pending approval
- `PayrollFileApproved` - Payroll file was approved

#### Reconciliation
- `ReconciliationComplete` - Reconciliation workflow completed
- `ReconciliationEscalation` - Workflow was escalated for attention
- `WorkflowStuck` - Workflow is stuck and needs intervention
- `ManualInterventionRequired` - Manual intervention is required
- `RetryLimitExceeded` - Workflow retry limit exceeded

#### General
- `SystemAlert` - System-level alert
- `HealthCheckFailure` - Service health check failure
- `Custom` - Custom topic for ad-hoc notifications

### Severity Levels

Notification severity levels in ascending order:

| Severity | Description | Typical Use |
|----------|-------------|-------------|
| `Info` | Informational notification | Status updates, completions |
| `Warning` | Warning notification | Non-critical issues, warnings |
| `Urgent` | Urgent notification requiring attention | Time-sensitive issues |
| `Critical` | Critical notification requiring immediate action | System failures, stuck workflows |

**Severity Filtering**: Routing policies can specify a `minSeverity`. Events below this threshold will not trigger the policy.

Example: A policy with `minSeverity: "Urgent"` will match `Urgent` and `Critical` events but not `Info` or `Warning`.

### Delivery Channels

Available delivery channels:

| Channel | Description | Status |
|---------|-------------|--------|
| `SignalR` | Real-time notification via SignalR | Phase 1 (Active) |
| `Email` | Email notification | Phase 2 (Planned) |
| `SMS` | SMS notification | Phase 2 (Planned) |
| `Teams` | Microsoft Teams notification | Phase 3 (Future) |

### Delivery Roles

Recipient roles for email-based channels:

| Role | Description |
|------|-------------|
| `To` | Primary recipient (To field) |
| `Cc` | Carbon copy recipient (CC field) |
| `Bcc` | Blind carbon copy recipient (BCC field) |

## Best Practices

### Publishing Events

1. **Always specify severity appropriately**
   - Reserve `Critical` for true emergencies
   - Use `Info` for routine status updates
   - Use `Urgent` for time-sensitive but non-critical issues

2. **Include correlation IDs**
   - Set `sagaId` for workflow-related events
   - Set `correlationId` for cross-service tracing
   - Enables end-to-end tracking in logs and dashboards

3. **Use structured payloads**
   - Include relevant context in the `payload` field
   - Supports future template rendering
   - Enables rich notifications with actionable data

4. **Provide meaningful subjects**
   - Include client ID and date when relevant
   - Keep subjects concise but descriptive
   - Example: "Reconciliation completed for LSR7 - 2025-12-18"

### Monitoring Routing

1. **Check the dashboard regularly**
   - Monitor success rates
   - Review failed deliveries
   - Track system load trends

2. **Audit client configurations periodically**
   - Verify policy coverage for all critical topics
   - Ensure contacts are active and correct
   - Check for disabled policies that should be enabled

3. **Monitor delivery counts**
   - Unexpectedly low delivery counts may indicate policy issues
   - High failure rates suggest channel problems (SMTP, connectivity)

### Configuration Management

1. **Use priority for escalations**
   - Higher priority policies process first
   - Use for critical alert routing
   - Example: `WorkflowStuck` with priority 10, `ReconciliationComplete` with priority 0

2. **Leverage global groups**
   - Create global escalation groups (clientId = null)
   - Apply to high-severity events across all clients
   - Reduces configuration duplication

3. **Document group purposes**
   - Use meaningful names and descriptions
   - Tag groups for easy searching
   - Specify purpose (Production, Testing, Development)

## Common Scenarios

### Scenario 1: Route workflow completion to operations team

```bash
# Step 1: Verify client configuration
curl http://localhost:5201/api/routing/clients/LSR7/configuration | jq '.policies[] | select(.topic == "ReconciliationComplete")'

# Step 2: Publish event
curl -X POST http://localhost:5201/api/routing/publish \
  -H "Content-Type: application/json" \
  -d '{
    "service": "CensusReconciliation",
    "topic": "ReconciliationComplete",
    "clientId": "LSR7",
    "severity": "Info",
    "subject": "Daily reconciliation completed",
    "sagaId": "7c9e6679-7425-40de-944b-e07fc1f90ae7"
  }'
```

### Scenario 2: Send critical alert to multiple channels

```bash
# Publish critical event (policies route to both SignalR and Email)
curl -X POST http://localhost:5201/api/routing/publish \
  -H "Content-Type: application/json" \
  -d '{
    "service": "CensusReconciliation",
    "topic": "WorkflowStuck",
    "clientId": "LSR7",
    "severity": "Critical",
    "subject": "URGENT: Workflow requires intervention",
    "body": "Workflow stuck in AwaitingAggregates for 2 hours",
    "payload": {
      "sagaId": "abc123",
      "state": "AwaitingAggregates",
      "stuckDuration": "02:00:00"
    }
  }'
```

### Scenario 3: Monitor delivery success

```bash
# Check dashboard for failures
curl http://localhost:5201/api/routing/dashboard | jq '{
  successRate: .statistics.successRateToday,
  failed: .statistics.failedDeliveries,
  pending: .statistics.pendingDeliveries
}'

# View failed delivery details
curl http://localhost:5201/api/routing/dashboard | jq '.failedDeliveries[] | {contact: .contactName, error: .errorMessage}'
```

## Related Documentation

- [Events Management Endpoints](./events-management-endpoints.md) - Query and manage outbound events
- [Routing Policy Endpoints](./routing-policy-endpoints.md) - Create and manage routing policies
- [Recipient Groups Endpoints](./recipient-groups-endpoints.md) - Manage recipient groups
- [Contact Management Endpoints](./contact-management-endpoints.md) - Manage contacts

## Support

For issues or questions:
- Review the dashboard for system health
- Check client configuration for missing policies
- Inspect event details for delivery failures
- Contact the development team with correlation IDs for troubleshooting
