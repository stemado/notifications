# Policies Endpoints

## Overview

The Policies endpoints allow you to manage routing policies that determine how notifications are distributed to recipient groups. Routing policies define the rules for matching incoming notification events to delivery channels and recipients based on service, topic, severity, and client context.

**Base URL**: `/api/routing/policies`

## What Are Routing Policies?

Routing policies are the core configuration mechanism that connects notification events to recipient groups. Each policy specifies:

- **What to match**: Service, topic, optional client ID, and minimum severity level
- **Where to send**: Notification channel (SignalR, Email, SMS, Teams)
- **Who receives**: Recipient group ID and delivery role (To, CC, BCC)
- **Priority**: Order for policy evaluation when multiple policies match

When a notification event is published, the routing engine evaluates all enabled policies and routes the notification to matching recipient groups through the specified channels.

## Policy Model

### RoutingPolicyDetails (Full)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "service": "CensusReconciliation",
  "topic": "ReconciliationComplete",
  "clientId": "CLIENT001",
  "minSeverity": "Info",
  "channel": "Email",
  "recipientGroupId": "8e2d4c8a-6f3b-4d1e-9a7c-5b3e8f2d4a1c",
  "recipientGroupName": "Finance Team",
  "recipientCount": 5,
  "role": "To",
  "priority": 100,
  "isEnabled": true,
  "createdAt": "2025-01-15T08:30:00Z",
  "updatedAt": "2025-01-15T10:45:00Z",
  "updatedBy": "admin@example.com"
}
```

### RoutingPolicySummary (List View)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "service": "CensusReconciliation",
  "topic": "ReconciliationComplete",
  "clientId": "CLIENT001",
  "minSeverity": "Info",
  "channel": "Email",
  "recipientGroupId": "8e2d4c8a-6f3b-4d1e-9a7c-5b3e8f2d4a1c",
  "recipientGroupName": "Finance Team",
  "role": "To",
  "priority": 100,
  "isEnabled": true
}
```

### Property Descriptions

| Property | Type | Description |
|----------|------|-------------|
| `id` | GUID | Unique identifier for the routing policy |
| `service` | string | Source service publishing the notification (enum: `SourceService`) |
| `topic` | string | Notification topic to match (enum: `NotificationTopic`) |
| `clientId` | string | Optional client identifier for client-specific routing. If null, policy applies to all clients |
| `minSeverity` | string | Minimum severity level to match (enum: `NotificationSeverity`). If null, all severities match |
| `channel` | string | Delivery channel for notifications (enum: `NotificationChannel`) |
| `recipientGroupId` | GUID | ID of the recipient group to receive notifications |
| `recipientGroupName` | string | Display name of the recipient group |
| `recipientCount` | int | Number of active recipients in the group (details view only) |
| `role` | string | Delivery role for recipients (enum: `DeliveryRole`) |
| `priority` | int | Evaluation priority (higher values = higher priority). Default: 0 |
| `isEnabled` | bool | Whether the policy is active |
| `createdAt` | DateTime | When the policy was created (details view only) |
| `updatedAt` | DateTime | When the policy was last modified (details view only) |
| `updatedBy` | string | User who last modified the policy (details view only) |

## Enum Values

### SourceService

| Value | Description |
|-------|-------------|
| `CensusAutomation` | Census automation and file processing |
| `PayrollFileGeneration` | Payroll file generation service |
| `CensusReconciliation` | Census reconciliation workflow service |
| `CensusOrchestration` | Census orchestration service |
| `PlanSourceIntegration` | PlanSource integration service |

### NotificationTopic

#### Census Automation Topics
| Value | Description |
|-------|-------------|
| `DailyImportSuccess` | Daily import completed successfully |
| `DailyImportFailure` | Daily import failed |
| `SchemaValidationError` | Schema validation error detected |
| `RecordCountMismatch` | Record count mismatch detected |
| `FileProcessingStarted` | File processing has started |
| `FileProcessingCompleted` | File processing completed |

#### Payroll Topics
| Value | Description |
|-------|-------------|
| `PayrollFileGenerated` | Payroll file was generated successfully |
| `PayrollFileError` | Payroll file generation failed |
| `PayrollFilePending` | Payroll file is pending approval |
| `PayrollFileApproved` | Payroll file was approved |

#### Reconciliation Topics
| Value | Description |
|-------|-------------|
| `ReconciliationComplete` | Reconciliation workflow completed |
| `ReconciliationEscalation` | Workflow was escalated for attention |
| `WorkflowStuck` | Workflow is stuck and needs intervention |
| `ManualInterventionRequired` | Manual intervention is required |
| `RetryLimitExceeded` | Workflow retry limit exceeded |

#### General Topics
| Value | Description |
|-------|-------------|
| `SystemAlert` | System-level alert |
| `HealthCheckFailure` | Service health check failure |
| `Custom` | Custom topic for ad-hoc notifications |

### NotificationSeverity

| Value | Description |
|-------|-------------|
| `Info` | Informational notification |
| `Warning` | Warning notification |
| `Urgent` | Urgent notification requiring attention |
| `Critical` | Critical notification requiring immediate action |

**Severity Filtering**: When `minSeverity` is set, the policy matches notifications at or above that level. For example, `minSeverity: "Warning"` matches Warning, Urgent, and Critical, but not Info.

### NotificationChannel

| Value | Description |
|-------|-------------|
| `SignalR` | Real-time notification via SignalR |
| `Email` | Email notification |
| `SMS` | SMS notification |
| `Teams` | Microsoft Teams notification (Phase 3) |

### DeliveryRole

| Value | Description |
|-------|-------------|
| `To` | Primary recipient (To field) |
| `Cc` | Carbon copy recipient (CC field) |
| `Bcc` | Blind carbon copy recipient (BCC field) |

## Endpoints

### 1. List Routing Policies

Retrieve a paginated list of routing policies with optional filtering.

**Endpoint**: `GET /api/routing/policies`

**Query Parameters**:

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `clientId` | string | No | null | Filter by specific client ID |
| `service` | SourceService | No | null | Filter by source service |
| `topic` | NotificationTopic | No | null | Filter by notification topic |
| `includeDisabled` | bool | No | false | Include disabled policies in results |
| `page` | int | No | 1 | Page number (1-based) |
| `pageSize` | int | No | 20 | Number of items per page |

**Filter Behavior**:
- If `clientId` is provided, returns policies for that client only
- If both `service` and `topic` are provided, returns policies matching that service/topic combination
- If no filters are provided, returns all enabled policies (or all policies if `includeDisabled=true`)
- Filters are mutually exclusive: `clientId` takes precedence over `service`/`topic`

**Response**: `200 OK`

```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "service": "CensusReconciliation",
      "topic": "ReconciliationComplete",
      "clientId": "CLIENT001",
      "minSeverity": "Info",
      "channel": "Email",
      "recipientGroupId": "8e2d4c8a-6f3b-4d1e-9a7c-5b3e8f2d4a1c",
      "recipientGroupName": "Finance Team",
      "role": "To",
      "priority": 100,
      "isEnabled": true
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 45,
  "totalPages": 3,
  "hasNext": true,
  "hasPrevious": false
}
```

**Example Requests**:

```bash
# Get all enabled policies (first page)
curl -X GET "http://localhost:5201/api/routing/policies"

# Get policies for a specific client
curl -X GET "http://localhost:5201/api/routing/policies?clientId=CLIENT001"

# Get policies for a specific service and topic
curl -X GET "http://localhost:5201/api/routing/policies?service=CensusReconciliation&topic=WorkflowStuck"

# Get all policies including disabled ones
curl -X GET "http://localhost:5201/api/routing/policies?includeDisabled=true"

# Paginated request
curl -X GET "http://localhost:5201/api/routing/policies?page=2&pageSize=50"
```

---

### 2. Get Policy Details

Retrieve detailed information about a specific routing policy.

**Endpoint**: `GET /api/routing/policies/{id}`

**Path Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | Unique identifier of the routing policy |

**Response**: `200 OK`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "service": "CensusReconciliation",
  "topic": "ReconciliationComplete",
  "clientId": "CLIENT001",
  "minSeverity": "Info",
  "channel": "Email",
  "recipientGroupId": "8e2d4c8a-6f3b-4d1e-9a7c-5b3e8f2d4a1c",
  "recipientGroupName": "Finance Team",
  "recipientCount": 5,
  "role": "To",
  "priority": 100,
  "isEnabled": true,
  "createdAt": "2025-01-15T08:30:00Z",
  "updatedAt": "2025-01-15T10:45:00Z",
  "updatedBy": "admin@example.com"
}
```

**Error Responses**:

- `404 Not Found`: Policy with the specified ID does not exist

```json
"Routing policy 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found"
```

**Example Request**:

```bash
curl -X GET "http://localhost:5201/api/routing/policies/3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

### 3. Create Routing Policy

Create a new routing policy to route notifications to a recipient group.

**Endpoint**: `POST /api/routing/policies`

**Request Body**: `CreateRoutingPolicyRequest`

```json
{
  "service": "CensusReconciliation",
  "topic": "WorkflowStuck",
  "clientId": "CLIENT001",
  "minSeverity": "Urgent",
  "channel": "Email",
  "recipientGroupId": "8e2d4c8a-6f3b-4d1e-9a7c-5b3e8f2d4a1c",
  "role": "To",
  "priority": 100
}
```

**Required Fields**:
- `service`: Source service (SourceService enum)
- `topic`: Notification topic (NotificationTopic enum)
- `channel`: Delivery channel (NotificationChannel enum)
- `recipientGroupId`: ID of existing recipient group
- `role`: Delivery role (DeliveryRole enum)

**Optional Fields**:
- `clientId`: Client identifier for client-specific routing. Omit for policies that apply to all clients
- `minSeverity`: Minimum severity level. Omit to match all severities
- `priority`: Evaluation priority (default: 0)

**Response**: `201 Created`

Returns the full `RoutingPolicyDetails` object and sets the `Location` header to the new policy's URL.

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "service": "CensusReconciliation",
  "topic": "WorkflowStuck",
  "clientId": "CLIENT001",
  "minSeverity": "Urgent",
  "channel": "Email",
  "recipientGroupId": "8e2d4c8a-6f3b-4d1e-9a7c-5b3e8f2d4a1c",
  "recipientGroupName": "Operations Team",
  "recipientCount": 8,
  "role": "To",
  "priority": 100,
  "isEnabled": true,
  "createdAt": "2025-01-15T14:20:00Z",
  "updatedAt": "2025-01-15T14:20:00Z",
  "updatedBy": "api"
}
```

**Example Request**:

```bash
curl -X POST "http://localhost:5201/api/routing/policies" \
  -H "Content-Type: application/json" \
  -d '{
    "service": "CensusReconciliation",
    "topic": "WorkflowStuck",
    "clientId": "CLIENT001",
    "minSeverity": "Urgent",
    "channel": "Email",
    "recipientGroupId": "8e2d4c8a-6f3b-4d1e-9a7c-5b3e8f2d4a1c",
    "role": "To",
    "priority": 100
  }'
```

**Common Use Cases**:

**Critical Alert Routing**:
```json
{
  "service": "CensusReconciliation",
  "topic": "ManualInterventionRequired",
  "minSeverity": "Critical",
  "channel": "Email",
  "recipientGroupId": "critical-alerts-group-id",
  "role": "To",
  "priority": 1000
}
```

**Client-Specific Success Notifications**:
```json
{
  "service": "CensusAutomation",
  "topic": "DailyImportSuccess",
  "clientId": "CLIENT001",
  "minSeverity": "Info",
  "channel": "Email",
  "recipientGroupId": "client-001-team-id",
  "role": "To",
  "priority": 10
}
```

**CC Recipients for Audit Trail**:
```json
{
  "service": "PayrollFileGeneration",
  "topic": "PayrollFileGenerated",
  "channel": "Email",
  "recipientGroupId": "audit-team-id",
  "role": "Cc",
  "priority": 5
}
```

---

### 4. Update Routing Policy

Update configuration for an existing routing policy.

**Endpoint**: `PUT /api/routing/policies/{id}`

**Path Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | Unique identifier of the routing policy to update |

**Request Body**: `UpdateRoutingPolicyRequest`

```json
{
  "minSeverity": "Warning",
  "channel": "Teams",
  "recipientGroupId": "8e2d4c8a-6f3b-4d1e-9a7c-5b3e8f2d4a1c",
  "role": "To",
  "priority": 150,
  "isEnabled": true
}
```

**All Fields Required**:
- `minSeverity`: Minimum severity level (nullable)
- `channel`: Delivery channel (NotificationChannel enum)
- `recipientGroupId`: ID of recipient group
- `role`: Delivery role (DeliveryRole enum)
- `priority`: Evaluation priority (default: 0)
- `isEnabled`: Whether policy is active (default: true)

**Immutable Fields**:
The following fields cannot be changed after creation:
- `service`
- `topic`
- `clientId`

To change these fields, delete the policy and create a new one.

**Response**: `200 OK`

Returns the updated `RoutingPolicyDetails` object.

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "service": "CensusReconciliation",
  "topic": "WorkflowStuck",
  "clientId": "CLIENT001",
  "minSeverity": "Warning",
  "channel": "Teams",
  "recipientGroupId": "8e2d4c8a-6f3b-4d1e-9a7c-5b3e8f2d4a1c",
  "recipientGroupName": "Operations Team",
  "recipientCount": 8,
  "role": "To",
  "priority": 150,
  "isEnabled": true,
  "createdAt": "2025-01-15T08:30:00Z",
  "updatedAt": "2025-01-15T15:45:00Z",
  "updatedBy": "admin@example.com"
}
```

**Error Responses**:

- `404 Not Found`: Policy with the specified ID does not exist

**Example Request**:

```bash
curl -X PUT "http://localhost:5201/api/routing/policies/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "Content-Type: application/json" \
  -d '{
    "minSeverity": "Warning",
    "channel": "Teams",
    "recipientGroupId": "8e2d4c8a-6f3b-4d1e-9a7c-5b3e8f2d4a1c",
    "role": "To",
    "priority": 150,
    "isEnabled": true
  }'
```

---

### 5. Delete Routing Policy

Permanently delete a routing policy.

**Endpoint**: `DELETE /api/routing/policies/{id}`

**Path Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | Unique identifier of the routing policy to delete |

**Response**: `204 No Content`

No response body is returned on successful deletion.

**Error Responses**:

- `404 Not Found`: Policy with the specified ID does not exist

**Example Request**:

```bash
curl -X DELETE "http://localhost:5201/api/routing/policies/3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

**Important Notes**:
- Deletion is permanent and cannot be undone
- Consider disabling policies instead of deleting them to preserve audit history
- Use the toggle endpoint to temporarily disable policies

---

### 6. Toggle Policy Enabled/Disabled

Quickly enable or disable a routing policy without changing other configuration.

**Endpoint**: `POST /api/routing/policies/{id}/toggle`

**Path Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | Unique identifier of the routing policy to toggle |

**Response**: `200 OK`

Returns the updated `RoutingPolicySummary` with the new `isEnabled` state.

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "service": "CensusReconciliation",
  "topic": "ReconciliationComplete",
  "clientId": "CLIENT001",
  "minSeverity": "Info",
  "channel": "Email",
  "recipientGroupId": "8e2d4c8a-6f3b-4d1e-9a7c-5b3e8f2d4a1c",
  "recipientGroupName": "Finance Team",
  "role": "To",
  "priority": 100,
  "isEnabled": false
}
```

**Behavior**:
- If policy is currently enabled, it will be disabled
- If policy is currently disabled, it will be enabled
- The `updatedAt` timestamp is updated
- The `updatedBy` field is set to the authenticated user (or "api")

**Error Responses**:

- `404 Not Found`: Policy with the specified ID does not exist

**Example Request**:

```bash
curl -X POST "http://localhost:5201/api/routing/policies/3fa85f64-5717-4562-b3fc-2c963f66afa6/toggle"
```

**Use Cases**:
- Temporarily disable policies during maintenance windows
- Quickly enable/disable policies from a UI toggle switch
- Disable policies without losing configuration for future re-enablement

---

## How Policies Route Events to Groups

### Routing Flow

When a notification event is published to the NotificationService:

1. **Event Reception**: Service receives an outbound notification event with:
   - Source service (e.g., `CensusReconciliation`)
   - Topic (e.g., `WorkflowStuck`)
   - Severity (e.g., `Critical`)
   - Optional client ID (e.g., `CLIENT001`)
   - Event payload with notification details

2. **Policy Matching**: Routing engine queries all enabled policies and matches based on:
   - Service must match exactly
   - Topic must match exactly
   - Client ID must match (or policy has null `clientId` for global policies)
   - Event severity must meet or exceed `minSeverity` (if specified)

3. **Priority Ordering**: Matching policies are sorted by priority (highest first)

4. **Group Resolution**: For each matching policy:
   - Recipient group is loaded with all active members
   - Channel configuration is applied
   - Delivery role is assigned

5. **Notification Delivery**: Notifications are sent to recipients through the specified channels

### Policy Priority System

Priority determines the order in which policies are evaluated when multiple policies match the same event:

- **Higher numbers = Higher priority**
- Default priority: `0`
- Recommended ranges:
  - `1000+`: Critical alert policies (immediate escalation)
  - `500-999`: High priority operational alerts
  - `100-499`: Standard operational notifications
  - `1-99`: Informational and low-priority notifications
  - `0`: Default priority

**Example Priority Hierarchy**:

```
Priority 1000: Critical alerts → On-call team (Email + SMS)
Priority 500:  Urgent alerts → Operations team (Teams)
Priority 100:  Standard alerts → Department team (Email)
Priority 10:   Success notifications → Project managers (Email, CC)
Priority 0:    Audit logs → Compliance team (Email, BCC)
```

### Client-Specific vs Global Policies

**Client-Specific Policy** (`clientId` is set):
```json
{
  "service": "CensusAutomation",
  "topic": "DailyImportFailure",
  "clientId": "CLIENT001",
  "channel": "Email",
  "recipientGroupId": "client-001-ops-team"
}
```
- Only matches events where `clientId = "CLIENT001"`
- Allows custom routing per client
- Use for client-specific notification requirements

**Global Policy** (`clientId` is null):
```json
{
  "service": "CensusReconciliation",
  "topic": "SystemAlert",
  "clientId": null,
  "channel": "Teams",
  "recipientGroupId": "platform-ops-team"
}
```
- Matches events from all clients
- Acts as a fallback or default routing
- Use for platform-wide monitoring and alerting

### Severity Filtering

Policies can filter by minimum severity level:

**Match All Severities** (`minSeverity` is null):
```json
{
  "minSeverity": null
}
```
- Matches Info, Warning, Urgent, and Critical

**Match Warning and Above**:
```json
{
  "minSeverity": "Warning"
}
```
- Matches Warning, Urgent, and Critical
- Does not match Info

**Match Only Critical**:
```json
{
  "minSeverity": "Critical"
}
```
- Matches only Critical
- Does not match Info, Warning, or Urgent

### Multi-Channel Delivery

To send the same notification through multiple channels, create separate policies with the same matching criteria but different channels:

```json
// Policy 1: Email delivery
{
  "service": "CensusReconciliation",
  "topic": "ManualInterventionRequired",
  "minSeverity": "Critical",
  "channel": "Email",
  "recipientGroupId": "ops-team-id",
  "role": "To",
  "priority": 1000
}

// Policy 2: SMS delivery for same event
{
  "service": "CensusReconciliation",
  "topic": "ManualInterventionRequired",
  "minSeverity": "Critical",
  "channel": "SMS",
  "recipientGroupId": "ops-team-id",
  "role": "To",
  "priority": 1000
}

// Policy 3: Teams notification for same event
{
  "service": "CensusReconciliation",
  "topic": "ManualInterventionRequired",
  "minSeverity": "Critical",
  "channel": "Teams",
  "recipientGroupId": "ops-team-id",
  "role": "To",
  "priority": 1000
}
```

### Delivery Roles

Use different roles to control recipient visibility:

**Primary Recipients** (`To`):
```json
{
  "role": "To",
  "recipientGroupId": "primary-team-id"
}
```
- Main recipients responsible for action
- Visible to all other recipients

**Carbon Copy** (`Cc`):
```json
{
  "role": "Cc",
  "recipientGroupId": "stakeholder-group-id"
}
```
- Keep stakeholders informed
- Visible to all other recipients
- Not expected to take action

**Blind Carbon Copy** (`Bcc`):
```json
{
  "role": "Bcc",
  "recipientGroupId": "audit-team-id"
}
```
- Hidden recipients for audit/compliance
- Not visible to other recipients
- Useful for silent monitoring

---

## Common Scenarios

### Scenario 1: Route Critical Workflow Failures to On-Call Team

**Requirement**: When reconciliation workflows require manual intervention, immediately notify the on-call team via email and SMS.

**Solution**:

```bash
# Create high-priority email policy
curl -X POST "http://localhost:5201/api/routing/policies" \
  -H "Content-Type: application/json" \
  -d '{
    "service": "CensusReconciliation",
    "topic": "ManualInterventionRequired",
    "minSeverity": "Critical",
    "channel": "Email",
    "recipientGroupId": "on-call-team-id",
    "role": "To",
    "priority": 1000
  }'

# Create parallel SMS policy for same event
curl -X POST "http://localhost:5201/api/routing/policies" \
  -H "Content-Type: application/json" \
  -d '{
    "service": "CensusReconciliation",
    "topic": "ManualInterventionRequired",
    "minSeverity": "Critical",
    "channel": "SMS",
    "recipientGroupId": "on-call-team-id",
    "role": "To",
    "priority": 1000
  }'
```

### Scenario 2: Client-Specific Import Success Notifications

**Requirement**: Send daily import success notifications to each client's designated team.

**Solution**:

```bash
# Create policy for Client A
curl -X POST "http://localhost:5201/api/routing/policies" \
  -H "Content-Type: application/json" \
  -d '{
    "service": "CensusAutomation",
    "topic": "DailyImportSuccess",
    "clientId": "CLIENT_A",
    "minSeverity": "Info",
    "channel": "Email",
    "recipientGroupId": "client-a-operations-id",
    "role": "To",
    "priority": 10
  }'

# Create policy for Client B
curl -X POST "http://localhost:5201/api/routing/policies" \
  -H "Content-Type: application/json" \
  -d '{
    "service": "CensusAutomation",
    "topic": "DailyImportSuccess",
    "clientId": "CLIENT_B",
    "minSeverity": "Info",
    "channel": "Email",
    "recipientGroupId": "client-b-operations-id",
    "role": "To",
    "priority": 10
  }'
```

### Scenario 3: Escalation with CC to Management

**Requirement**: When workflows get stuck, notify operations team and CC management for visibility.

**Solution**:

```bash
# Primary notification to operations
curl -X POST "http://localhost:5201/api/routing/policies" \
  -H "Content-Type: application/json" \
  -d '{
    "service": "CensusReconciliation",
    "topic": "WorkflowStuck",
    "minSeverity": "Urgent",
    "channel": "Teams",
    "recipientGroupId": "operations-team-id",
    "role": "To",
    "priority": 500
  }'

# CC management for visibility
curl -X POST "http://localhost:5201/api/routing/policies" \
  -H "Content-Type: application/json" \
  -d '{
    "service": "CensusReconciliation",
    "topic": "WorkflowStuck",
    "minSeverity": "Urgent",
    "channel": "Email",
    "recipientGroupId": "management-team-id",
    "role": "Cc",
    "priority": 500
  }'
```

### Scenario 4: Audit Trail with BCC

**Requirement**: All payroll file generation notifications should be silently logged to compliance team.

**Solution**:

```bash
# Create BCC policy for audit trail
curl -X POST "http://localhost:5201/api/routing/policies" \
  -H "Content-Type: application/json" \
  -d '{
    "service": "PayrollFileGeneration",
    "topic": "PayrollFileGenerated",
    "channel": "Email",
    "recipientGroupId": "compliance-audit-id",
    "role": "Bcc",
    "priority": 0
  }'
```

### Scenario 5: Temporarily Disable Policy During Maintenance

**Requirement**: Disable notifications during a scheduled maintenance window without losing policy configuration.

**Solution**:

```bash
# Get policy ID first
POLICY_ID="3fa85f64-5717-4562-b3fc-2c963f66afa6"

# Disable before maintenance
curl -X POST "http://localhost:5201/api/routing/policies/$POLICY_ID/toggle"

# ... perform maintenance ...

# Re-enable after maintenance
curl -X POST "http://localhost:5201/api/routing/policies/$POLICY_ID/toggle"
```

### Scenario 6: Find All Policies for a Topic

**Requirement**: Review all routing configurations for workflow stuck events.

**Solution**:

```bash
# List all policies for specific service and topic
curl -X GET "http://localhost:5201/api/routing/policies?service=CensusReconciliation&topic=WorkflowStuck&includeDisabled=true"
```

---

## Best Practices

### Policy Design

1. **Use Descriptive Priority Values**
   - Assign priorities in ranges (e.g., 100s, 500s, 1000s)
   - Leave gaps for future insertion
   - Document priority scheme in your team's runbook

2. **Start with Client-Specific Policies**
   - Create targeted policies for known clients first
   - Add global policies as fallbacks
   - Avoid over-matching with overly broad policies

3. **Leverage Severity Filtering**
   - Set appropriate `minSeverity` thresholds
   - Use Critical for immediate action items
   - Use Info for audit trails and success notifications

4. **Plan Multi-Channel Delivery**
   - Critical alerts: Email + SMS + Teams
   - Urgent alerts: Email + Teams
   - Standard notifications: Email only
   - Audit trails: Email (BCC)

### Operational Guidelines

1. **Test Before Enabling**
   - Create policies in disabled state
   - Use test notification endpoint to verify routing
   - Toggle to enabled after successful test

2. **Monitor Policy Performance**
   - Review policy match rates regularly
   - Identify unused or rarely matched policies
   - Adjust priority and filtering as needed

3. **Maintain Policy Documentation**
   - Document the purpose of each policy
   - Note which teams or stakeholders are affected
   - Keep contact information for recipient groups current

4. **Handle Policy Updates Carefully**
   - Changing `recipientGroupId` affects who receives notifications
   - Changing `channel` affects how notifications are delivered
   - Changing `minSeverity` affects which events match
   - Consider creating a new policy instead of updating critical policies

### Common Pitfalls

1. **Overly Broad Matching**
   - Setting `minSeverity: "Info"` on all policies creates notification overload
   - Omitting `clientId` creates global policies that match all clients
   - Solution: Be specific with severity and client filters

2. **Priority Conflicts**
   - Multiple policies with same priority may execute in undefined order
   - Solution: Assign unique priorities or document that order doesn't matter

3. **Forgotten Disabled Policies**
   - Policies remain disabled after testing
   - Solution: Use `includeDisabled=true` filter to audit disabled policies regularly

4. **Orphaned Policies**
   - Policies reference deleted recipient groups
   - Solution: Review policy details for recipient count = 0

---

## Error Handling

### Standard Error Responses

All endpoints return consistent error formats:

**404 Not Found**:
```json
"Routing policy {id} not found"
```

**400 Bad Request** (validation errors):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Service": ["The Service field is required."],
    "RecipientGroupId": ["The RecipientGroupId field is required."]
  }
}
```

### Troubleshooting

**Policy Not Matching Events**:
1. Verify policy is enabled (`isEnabled: true`)
2. Check service and topic match exactly
3. Verify client ID matches (or is null for global policies)
4. Ensure event severity meets or exceeds `minSeverity`
5. Review recipient group has active members

**No Notifications Being Sent**:
1. List policies with filters to verify policy exists
2. Get policy details to check recipient count
3. Check recipient group has active contacts
4. Verify delivery channel is properly configured

**Duplicate Notifications**:
1. List all policies with `includeDisabled=true`
2. Look for multiple policies matching the same criteria
3. Review priority values to ensure proper ordering
4. Consider consolidating policies or disabling duplicates

---

## Related Endpoints

- **Recipient Groups**: `/api/routing/groups` - Manage recipient groups that policies route to
- **Contacts**: `/api/routing/contacts` - Manage individual contacts within groups
- **Events**: `/api/routing/events` - View notification events and routing history
- **Routing Overview**: `/api/routing` - Get complete routing configuration summary

---

## Next Steps

- **Create Recipient Groups**: Before creating policies, set up recipient groups with active contacts
- **Test Routing**: Use the test notification endpoint to verify policy matching
- **Monitor Events**: Review the events endpoint to see which policies are matching real notifications
- **Adjust Priorities**: Fine-tune priority values based on operational needs

---

**File Path**: `D:\Projects\PlanSourceAutomation-V2\NotificationServices\src\docs\site-map\policies-endpoints.md`
