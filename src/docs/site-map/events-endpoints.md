# Events API Endpoints

## Overview

The Events API provides HTTP endpoints for publishing domain events directly to the NotificationService. These endpoints serve as an alternative to using the NotificationService.Client library, allowing services to post raw events that trigger notification creation and multi-channel dispatch.

**Base URL**: `http://192.168.150.52:5201/api/events`

**Authentication**: Optional (events are accepted without authentication for internal service-to-service communication)

**Event Flow**:
1. External service posts event to `/api/events/{event-type}`
2. Event is validated (required fields checked)
3. Event handler processes the event
4. Notification is created with appropriate severity and routing
5. Notification is dispatched via multi-channel dispatcher (SignalR, Email, SMS)
6. Response is returned immediately (202 Accepted) - processing is asynchronous

---

## Table of Contents

1. [Saga Stuck Event](#1-saga-stuck-event)
2. [Import Completed Event](#2-import-completed-event)
3. [Import Failed Event](#3-import-failed-event)
4. [Escalation Created Event](#4-escalation-created-event)
5. [File Processing Error Event](#5-file-processing-error-event)
6. [File Picked Up Event](#6-file-picked-up-event)
7. [SLA Breach Event](#7-sla-breach-event)
8. [PlanSource Operation Failed Event](#8-plansource-operation-failed-event)
9. [Aggregate Generation Stalled Event](#9-aggregate-generation-stalled-event)
10. [Templates Queued Event](#10-templates-queued-event)

---

## 1. Saga Stuck Event

Triggered when a reconciliation saga/workflow has been stuck in the same state for an extended period.

**Endpoint**: `POST /api/events/saga-stuck`

**When Triggered**:
- Saga has been in the same state for more than 2 hours
- Background monitoring detects no state progression
- Retry attempts have stalled

**Request Body**:
```json
{
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "HenryCounty",
  "clientName": "Henry County Schools",
  "stuckDuration": "02:30:00",
  "currentState": "ProcessingFile",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "census_2025_12_18.csv",
  "correlationId": "correlation-123",
  "detectedAt": "2025-12-18T10:30:00Z"
}
```

**Request Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sagaId` | Guid | Yes | The saga/workflow identifier (cannot be empty) |
| `clientId` | String | No | Client identifier (e.g., "HenryCounty") |
| `clientName` | String | No | Client display name |
| `stuckDuration` | TimeSpan | No | How long the saga has been stuck (format: "HH:mm:ss") |
| `currentState` | String | No | Current workflow state where saga is stuck |
| `tenantId` | Guid | No | Tenant ID for multi-tenant scenarios |
| `fileName` | String | No | File being processed (if applicable) |
| `correlationId` | String | No | Correlation ID for distributed tracing |
| `detectedAt` | DateTime | No | When the stuck condition was detected (defaults to UtcNow) |

**Notification Routing**:
- **Severity**: Dynamic based on stuck duration
  - Info: < 2 hours
  - Warning: 2-24 hours
  - Urgent: 24 hours - 3 days
  - Critical: > 3 days
- **Recipients**: Operations team (configured via `Notifications:OpsTeamUserId`)
- **Channels**: SignalR, Email (for Urgent/Critical), SMS (for Critical)
- **Repeat Interval**:
  - Critical: Every 15 minutes
  - Urgent: Every 30 minutes
  - Warning/Info: No repeat
- **Requires Acknowledgment**: Yes (for Urgent and Critical)
- **Expiration**: 3 days (for Info/Warning), never (for Urgent/Critical)

**Actions Provided**:
- "Fix Now" - Navigate to `/timeline/{sagaId}` (primary)
- "Snooze 1h" - Dismiss notification temporarily (secondary)

**Response**: `202 Accepted`
```json
{
  "message": "Event processed",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Error Responses**:
- `400 Bad Request`: SagaId is required
```json
{
  "error": "SagaId is required"
}
```

**Example Usage**:
```bash
curl -X POST "http://192.168.150.52:5201/api/events/saga-stuck" \
  -H "Content-Type: application/json" \
  -d '{
    "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "HenryCounty",
    "clientName": "Henry County Schools",
    "stuckDuration": "03:15:00",
    "currentState": "ProcessingFile"
  }'
```

---

## 2. Import Completed Event

Triggered when a census import workflow completes successfully.

**Endpoint**: `POST /api/events/import-completed`

**When Triggered**:
- File processing has completed successfully
- All records have been imported to PlanSource
- Final reconciliation has been performed

**Request Body**:
```json
{
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "4811266",
  "clientName": "Henry County Schools",
  "fileName": "census_2025_12_18.csv",
  "totalRecords": 500,
  "successCount": 485,
  "failureCount": 10,
  "skippedCount": 5,
  "newHireCount": 12,
  "terminationCount": 8,
  "demographicChangeCount": 120,
  "startedAt": "2025-12-18T08:00:00Z",
  "completedAt": "2025-12-18T10:30:00Z",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "correlationId": "correlation-123"
}
```

**Request Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sagaId` | Guid | Yes | The saga/workflow identifier (cannot be empty) |
| `clientId` | String | No | Client identifier |
| `clientName` | String | No | Client display name |
| `fileName` | String | No | Name of the file that was processed |
| `totalRecords` | Int | No | Total records in the file |
| `successCount` | Int | No | Successfully processed records |
| `failureCount` | Int | No | Failed records |
| `skippedCount` | Int | No | Skipped records |
| `newHireCount` | Int | No | New hires detected |
| `terminationCount` | Int | No | Terminations detected |
| `demographicChangeCount` | Int | No | Demographic changes detected |
| `startedAt` | DateTime | No | When the import started |
| `completedAt` | DateTime | No | When the import completed (defaults to UtcNow) |
| `tenantId` | Guid | No | Tenant ID for multi-tenant scenarios |
| `correlationId` | String | No | Correlation ID for distributed tracing |

**Computed Field**:
- `duration`: `completedAt - startedAt` (calculated automatically)

**Notification Routing**:
- **Severity**: Dynamic based on failure rate
  - Info: Failure rate < 10%
  - Warning: Failure rate >= 10%
- **Recipients**: Operations team
- **Channels**: SignalR, Email (for Warning)
- **Expiration**: 7 days
- **Requires Acknowledgment**: No

**Message Format**:
```
File 'census_2025_12_18.csv' processed successfully. 485/500 records imported. Changes: 12 new hires, 8 terminations, 120 demographics. Failures: 10 records. Duration: 2 hour(s)
```

**Actions Provided**:
- "View Details" - Navigate to `/timeline/{sagaId}` (primary)

**Response**: `202 Accepted`
```json
{
  "message": "Event processed",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Error Responses**:
- `400 Bad Request`: SagaId is required

**Example Usage**:
```bash
curl -X POST "http://192.168.150.52:5201/api/events/import-completed" \
  -H "Content-Type: application/json" \
  -d '{
    "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "4811266",
    "clientName": "Henry County Schools",
    "fileName": "census_2025_12_18.csv",
    "totalRecords": 500,
    "successCount": 485,
    "failureCount": 10,
    "newHireCount": 12,
    "terminationCount": 8,
    "startedAt": "2025-12-18T08:00:00Z"
  }'
```

---

## 3. Import Failed Event

Triggered when a census import workflow fails.

**Endpoint**: `POST /api/events/import-failed`

**When Triggered**:
- File parsing error
- Validation failure
- PlanSource API error during import
- Unhandled exception during processing
- Exceeded maximum retry attempts

**Request Body**:
```json
{
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "4811266",
  "clientName": "Henry County Schools",
  "fileName": "census_2025_12_18.csv",
  "errorMessage": "File format validation failed: Missing required column 'SSN'",
  "exceptionType": "FileValidationException",
  "stackTrace": "at FileValidator.Validate()\n  at FileProcessor.Process()",
  "failedAtState": "ValidatingFile",
  "retryCount": 3,
  "failedAt": "2025-12-18T10:30:00Z",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "correlationId": "correlation-123",
  "wasEscalated": true
}
```

**Request Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sagaId` | Guid | Yes | The saga/workflow identifier (cannot be empty) |
| `clientId` | String | No | Client identifier |
| `clientName` | String | No | Client display name |
| `fileName` | String | No | Name of the file that failed |
| `errorMessage` | String | No | Error message describing the failure |
| `exceptionType` | String | No | Exception type if applicable |
| `stackTrace` | String | No | Stack trace for debugging (may be truncated) |
| `failedAtState` | String | No | Workflow state when failure occurred |
| `retryCount` | Int | No | Number of retry attempts made |
| `failedAt` | DateTime | No | When the failure occurred (defaults to UtcNow) |
| `tenantId` | Guid | No | Tenant ID for multi-tenant scenarios |
| `correlationId` | String | No | Correlation ID for distributed tracing |
| `wasEscalated` | Bool | No | Whether this failure was escalated |

**Notification Routing**:
- **Severity**: Warning (default, may be escalated to Urgent/Critical in handler)
- **Recipients**: Operations team
- **Channels**: SignalR, Email, SMS (for Critical failures)
- **Requires Acknowledgment**: Yes
- **GroupKey**: `import:failed:{sagaId}`

**Response**: `202 Accepted`
```json
{
  "message": "Event processed",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Error Responses**:
- `400 Bad Request`: SagaId is required

**Example Usage**:
```bash
curl -X POST "http://192.168.150.52:5201/api/events/import-failed" \
  -H "Content-Type: application/json" \
  -d '{
    "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "4811266",
    "clientName": "Henry County Schools",
    "fileName": "census_2025_12_18.csv",
    "errorMessage": "File format validation failed",
    "failedAtState": "ValidatingFile",
    "retryCount": 3
  }'
```

---

## 4. Escalation Created Event

Triggered when a workflow escalation is created requiring manual intervention.

**Endpoint**: `POST /api/events/escalation-created`

**When Triggered**:
- Saga stuck for extended period
- Import failure after max retries
- Manual intervention required
- Critical business rule violation

**Request Body**:
```json
{
  "escalationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "4811266",
  "clientName": "Henry County Schools",
  "escalationType": "StuckWorkflow",
  "severity": "Urgent",
  "reason": "Workflow stuck in ProcessingFile state for over 4 hours with no progress",
  "currentState": "ProcessingFile",
  "timeInState": "04:15:00",
  "fileName": "census_2025_12_18.csv",
  "createdAt": "2025-12-18T10:30:00Z",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "correlationId": "correlation-123",
  "suggestedActions": [
    "Check PlanSource API connectivity",
    "Verify file is not locked by another process",
    "Review logs for stuck state processor"
  ]
}
```

**Request Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `escalationId` | Guid | Yes | The escalation identifier (cannot be empty) |
| `sagaId` | Guid | No | The related saga/workflow ID |
| `clientId` | String | No | Client identifier |
| `clientName` | String | No | Client display name |
| `escalationType` | String | No | Type of escalation (e.g., "StuckWorkflow", "ImportFailure", "ManualInterventionRequired") |
| `severity` | Enum | No | Severity: Info, Warning, Urgent, Critical |
| `reason` | String | No | Detailed reason for the escalation |
| `currentState` | String | No | Current workflow state |
| `timeInState` | TimeSpan | No | How long the workflow has been in this state |
| `fileName` | String | No | File being processed (if applicable) |
| `createdAt` | DateTime | No | When the escalation was created (defaults to UtcNow) |
| `tenantId` | Guid | No | Tenant ID for multi-tenant scenarios |
| `correlationId` | String | No | Correlation ID for distributed tracing |
| `suggestedActions` | Array | No | Recommended actions to resolve the escalation |

**Notification Routing**:
- **Severity**: Matches event severity
- **Recipients**: Operations team + escalation team
- **Channels**: SignalR, Email, SMS (for Urgent/Critical)
- **Requires Acknowledgment**: Yes (for Urgent/Critical)
- **GroupKey**: `escalation:{escalationType}:{sagaId}`

**Response**: `202 Accepted`
```json
{
  "message": "Event processed",
  "escalationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Error Responses**:
- `400 Bad Request`: EscalationId is required

**Example Usage**:
```bash
curl -X POST "http://192.168.150.52:5201/api/events/escalation-created" \
  -H "Content-Type: application/json" \
  -d '{
    "escalationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "4811266",
    "escalationType": "StuckWorkflow",
    "severity": "Urgent",
    "reason": "Workflow stuck for over 4 hours"
  }'
```

---

## 5. File Processing Error Event

Triggered when a file processing error occurs outside the main workflow.

**Endpoint**: `POST /api/events/file-processing-error`

**When Triggered**:
- File not found in expected location
- File parsing error
- Validation error (schema, format, business rules)
- File locked or inaccessible
- Encoding or corruption issues

**Request Body**:
```json
{
  "clientId": "HenryCounty",
  "clientName": "Henry County Schools",
  "filePath": "\\\\server\\share\\census\\HenryCounty\\census_2025_12_18.csv",
  "errorType": "FileNotFound",
  "errorMessage": "File not found at expected location",
  "severity": "Warning",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "occurredAt": "2025-12-18T10:30:00Z",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "isRecoverable": true,
  "resolution": "Check file path and network connectivity. Verify file was uploaded by client."
}
```

**Request Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `clientId` | String | Yes | Client identifier (cannot be empty) |
| `clientName` | String | No | Client display name |
| `filePath` | String | No | File path or name that failed |
| `errorType` | String | No | Type of error (e.g., "FileNotFound", "ParseError", "ValidationError") |
| `errorMessage` | String | No | Detailed error message |
| `severity` | Enum | No | Severity: Info, Warning, Urgent, Critical (defaults to Warning) |
| `sagaId` | Guid | No | Related saga ID if part of a workflow |
| `occurredAt` | DateTime | No | When the error occurred (defaults to UtcNow) |
| `tenantId` | Guid | No | Tenant ID for multi-tenant scenarios |
| `isRecoverable` | Bool | No | Whether this error is recoverable |
| `resolution` | String | No | Suggested resolution steps |

**Notification Routing**:
- **Severity**: Matches event severity
- **Recipients**: Operations team
- **Channels**: SignalR, Email (for Warning+)
- **GroupKey**: `file:error:{clientId}:{errorType}`

**Response**: `202 Accepted`
```json
{
  "message": "Event processed",
  "clientId": "HenryCounty",
  "errorType": "FileNotFound"
}
```

**Error Responses**:
- `400 Bad Request`: ClientId is required

**Example Usage**:
```bash
curl -X POST "http://192.168.150.52:5201/api/events/file-processing-error" \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "HenryCounty",
    "filePath": "\\\\server\\share\\census\\census_2025_12_18.csv",
    "errorType": "FileNotFound",
    "errorMessage": "File not found at expected location",
    "severity": "Warning"
  }'
```

---

## 6. File Picked Up Event

Triggered when a census file is detected and registered for processing.

**Endpoint**: `POST /api/events/file-picked-up`

**When Triggered**:
- File detected in monitored directory
- File validation passes initial checks
- Saga/workflow created for file processing
- File registered in file tracking system

**Request Body**:
```json
{
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "HenryCounty",
  "clientName": "Henry County Schools",
  "fileName": "census_2025_12_18.csv",
  "filePath": "\\\\server\\share\\census\\HenryCounty\\census_2025_12_18.csv",
  "fileSizeBytes": 1048576,
  "pickedUpAt": "2025-12-18T10:30:00Z",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "correlationId": "correlation-123"
}
```

**Request Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sagaId` | Guid | Yes | The saga/workflow ID created for this file (cannot be empty) |
| `clientId` | String | Yes | Client identifier (cannot be empty) |
| `clientName` | String | No | Client display name |
| `fileName` | String | No | Name of the file that was picked up |
| `filePath` | String | No | Full file path |
| `fileSizeBytes` | Long | No | File size in bytes |
| `pickedUpAt` | DateTime | No | When the file was picked up (defaults to UtcNow) |
| `tenantId` | Guid | No | Tenant ID for multi-tenant scenarios |
| `correlationId` | String | No | Correlation ID for distributed tracing |

**Notification Routing**:
- **Severity**: Info
- **Recipients**: Operations team
- **Channels**: SignalR only (low-priority informational)
- **Expiration**: 24 hours
- **GroupKey**: `file:pickedup:{sagaId}`

**Response**: `202 Accepted`
```json
{
  "message": "Event processed",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Error Responses**:
- `400 Bad Request`: SagaId is required
- `400 Bad Request`: ClientId is required

**Example Usage**:
```bash
curl -X POST "http://192.168.150.52:5201/api/events/file-picked-up" \
  -H "Content-Type: application/json" \
  -d '{
    "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "HenryCounty",
    "clientName": "Henry County Schools",
    "fileName": "census_2025_12_18.csv",
    "fileSizeBytes": 1048576
  }'
```

---

## 7. SLA Breach Event

Triggered when a workflow exceeds defined SLA thresholds.

**Endpoint**: `POST /api/events/sla-breach`

**When Triggered**:
- Processing time exceeds configured threshold
- Workflow stuck longer than SLA allows
- Business rules define specific time limits
- Completion target missed

**Request Body**:
```json
{
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "4811266",
  "clientName": "Henry County Schools",
  "slaType": "ProcessingTime",
  "thresholdMinutes": 120,
  "actualMinutes": 185,
  "currentState": "ProcessingFile",
  "severity": "Warning",
  "detectedAt": "2025-12-18T10:30:00Z",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "correlationId": "correlation-123"
}
```

**Request Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sagaId` | Guid | Yes | The saga/workflow identifier (cannot be empty) |
| `clientId` | String | No | Client identifier |
| `clientName` | String | No | Client display name |
| `slaType` | String | No | Type of SLA breached (e.g., "ProcessingTime", "CompletionTarget") |
| `thresholdMinutes` | Int | No | The threshold in minutes that was exceeded |
| `actualMinutes` | Int | No | The actual time in minutes |
| `currentState` | String | No | Current workflow state when breach was detected |
| `severity` | Enum | No | Severity: Info, Warning, Urgent, Critical |
| `detectedAt` | DateTime | No | When the breach was detected (defaults to UtcNow) |
| `tenantId` | Guid | No | Tenant ID for multi-tenant scenarios |
| `correlationId` | String | No | Correlation ID for distributed tracing |

**Notification Routing**:
- **Severity**: Matches event severity
- **Recipients**: Operations team + client (if configured)
- **Channels**: SignalR, Email (for Warning+), SMS (for Critical)
- **Requires Acknowledgment**: Yes (for Urgent/Critical)
- **GroupKey**: `sla:breach:{slaType}:{sagaId}`

**Response**: `202 Accepted`
```json
{
  "message": "Event processed",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Error Responses**:
- `400 Bad Request`: SagaId is required

**Example Usage**:
```bash
curl -X POST "http://192.168.150.52:5201/api/events/sla-breach" \
  -H "Content-Type: application/json" \
  -d '{
    "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "4811266",
    "slaType": "ProcessingTime",
    "thresholdMinutes": 120,
    "actualMinutes": 185,
    "severity": "Warning"
  }'
```

---

## 8. PlanSource Operation Failed Event

Triggered when a PlanSource API operation fails.

**Endpoint**: `POST /api/events/plansource-failed`

**When Triggered**:
- File upload to PlanSource fails
- Template extraction fails
- Full file pull operation fails
- API authentication error
- Rate limiting or throttling
- Network connectivity issues

**Request Body**:
```json
{
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "4811266",
  "clientName": "Henry County Schools",
  "operationType": "FileUpload",
  "errorMessage": "PlanSource API returned HTTP 500: Internal Server Error",
  "errorCode": "PS-500",
  "isRetryable": true,
  "attemptNumber": 2,
  "maxRetries": 3,
  "currentState": "UploadingToPlanSource",
  "severity": "Warning",
  "failedAt": "2025-12-18T10:30:00Z",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "correlationId": "correlation-123"
}
```

**Request Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sagaId` | Guid | Yes | The saga/workflow identifier (cannot be empty) |
| `clientId` | String | No | Client identifier |
| `clientName` | String | No | Client display name |
| `operationType` | String | No | Type of operation that failed (e.g., "FileUpload", "Extraction", "FullFilePull") |
| `errorMessage` | String | No | Error message describing the failure |
| `errorCode` | String | No | Error code if available (e.g., "PS-500") |
| `isRetryable` | Bool | No | Whether the error is retryable |
| `attemptNumber` | Int | No | Current retry attempt number |
| `maxRetries` | Int | No | Maximum retries allowed |
| `currentState` | String | No | Current workflow state |
| `severity` | Enum | No | Severity: Info, Warning, Urgent, Critical |
| `failedAt` | DateTime | No | When the failure occurred (defaults to UtcNow) |
| `tenantId` | Guid | No | Tenant ID for multi-tenant scenarios |
| `correlationId` | String | No | Correlation ID for distributed tracing |

**Notification Routing**:
- **Severity**:
  - Warning: Retryable errors (attempts < maxRetries)
  - Urgent: Final retry failed
  - Critical: Non-retryable errors
- **Recipients**: Operations team + PlanSource integration team
- **Channels**: SignalR, Email (for Warning+), SMS (for Critical)
- **Requires Acknowledgment**: Yes (for Urgent/Critical)
- **GroupKey**: `plansource:failed:{operationType}:{sagaId}`

**Response**: `202 Accepted`
```json
{
  "message": "Event processed",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Error Responses**:
- `400 Bad Request`: SagaId is required

**Example Usage**:
```bash
curl -X POST "http://192.168.150.52:5201/api/events/plansource-failed" \
  -H "Content-Type: application/json" \
  -d '{
    "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "4811266",
    "operationType": "FileUpload",
    "errorMessage": "PlanSource API returned HTTP 500",
    "isRetryable": true,
    "attemptNumber": 2,
    "maxRetries": 3,
    "severity": "Warning"
  }'
```

---

## 9. Aggregate Generation Stalled Event

Triggered when aggregate file generation in PlanSource appears stalled.

**Endpoint**: `POST /api/events/aggregate-stalled`

**When Triggered**:
- Waiting for aggregate file generation in PlanSource
- Multiple polling checks show no progress
- Wait count exceeds threshold
- Generation taking longer than expected

**Request Body**:
```json
{
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "4811266",
  "clientName": "Henry County Schools",
  "waitCount": 15,
  "maxWaitCount": 20,
  "minutesWaiting": 45,
  "fileName": "census_2025_12_18.csv",
  "severity": "Warning",
  "detectedAt": "2025-12-18T10:30:00Z",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "correlationId": "correlation-123"
}
```

**Request Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sagaId` | Guid | Yes | The saga/workflow identifier (cannot be empty) |
| `clientId` | String | No | Client identifier |
| `clientName` | String | No | Client display name |
| `waitCount` | Int | No | Number of times we've checked for the aggregate |
| `maxWaitCount` | Int | No | Maximum wait count before escalation |
| `minutesWaiting` | Int | No | Total minutes spent waiting |
| `fileName` | String | No | File being processed (if applicable) |
| `severity` | Enum | No | Severity: Info, Warning, Urgent, Critical |
| `detectedAt` | DateTime | No | When the stall was detected (defaults to UtcNow) |
| `tenantId` | Guid | No | Tenant ID for multi-tenant scenarios |
| `correlationId` | String | No | Correlation ID for distributed tracing |

**Notification Routing**:
- **Severity**:
  - Warning: waitCount approaching maxWaitCount (75-90%)
  - Urgent: waitCount >= maxWaitCount
  - Critical: Waiting > 2 hours
- **Recipients**: Operations team + PlanSource integration team
- **Channels**: SignalR, Email (for Warning+), SMS (for Critical)
- **Requires Acknowledgment**: Yes (for Urgent/Critical)
- **GroupKey**: `aggregate:stalled:{sagaId}`

**Response**: `202 Accepted`
```json
{
  "message": "Event processed",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Error Responses**:
- `400 Bad Request`: SagaId is required

**Example Usage**:
```bash
curl -X POST "http://192.168.150.52:5201/api/events/aggregate-stalled" \
  -H "Content-Type: application/json" \
  -d '{
    "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "4811266",
    "waitCount": 15,
    "maxWaitCount": 20,
    "minutesWaiting": 45,
    "severity": "Warning"
  }'
```

---

## 10. Templates Queued Event

Triggered when template files have been queued for import processing in PlanSource. This event triggers the Import History Scheduler to schedule a delayed check for import completion.

**Endpoint**: `POST /api/events/templates-queued`

**When Triggered**:
- Template files successfully uploaded to PlanSource
- Files added to PlanSource import queue
- QueueIds assigned to each template
- Need to schedule delayed import history check

**Request Body**:
```json
{
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "4811266",
  "clientName": "Henry County Schools",
  "templateCount": 3,
  "queueIds": [12345, 12346, 12347],
  "templateFiles": [
    "NewHire_Template.csv",
    "Termination_Template.csv",
    "Demographic_Template.csv"
  ],
  "importTypes": ["NewHire", "Termination", "Demographic"],
  "delayMinutes": 90,
  "queuedAt": "2025-12-18T10:30:00Z",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "correlationId": "correlation-123"
}
```

**Request Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sagaId` | Guid | No | The saga/workflow identifier |
| `clientId` | String | Yes | Client identifier (cannot be empty) |
| `clientName` | String | No | Client display name |
| `templateCount` | Int | No | Number of template files queued |
| `queueIds` | Array | No | Queue IDs assigned to the templates |
| `templateFiles` | Array | No | Template file names that were queued |
| `importTypes` | Array | No | Import types included (e.g., NewHire, Termination, Demographic) |
| `delayMinutes` | Int | No | Delay in minutes before checking import history (default: 90) |
| `queuedAt` | DateTime | No | When the templates were queued (defaults to UtcNow) |
| `tenantId` | Guid | No | Tenant ID for multi-tenant scenarios |
| `correlationId` | String | No | Correlation ID for distributed tracing |

**Special Behavior**:
This event handler performs TWO critical actions:
1. **Schedules Import History Check**: Calls `IImportHistoryScheduler.ScheduleCheckAsync()` to create a delayed background job
2. **Creates Notification**: Creates an informational notification to track the scheduled check

**Notification Routing**:
- **Severity**: Info
- **Recipients**: Operations team
- **Channels**: SignalR only (informational)
- **Expiration**: 24 hours
- **GroupKey**: `templates:queued:{sagaId}`

**Message Format**:
```
3 template file(s) queued for import. Types: NewHire, Termination, Demographic. Queue IDs: 12345, 12346, 12347. Import history check scheduled in 90 minutes.
```

**Actions Provided**:
- "View Timeline" - Navigate to `/timeline/{sagaId}` (primary)

**Response**: `202 Accepted`
```json
{
  "message": "Event processed - Import history check scheduled",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "4811266",
  "templateCount": 3,
  "delayMinutes": 90
}
```

**Error Responses**:
- `400 Bad Request`: ClientId is required

**Why This Event Exists**:
This event is the EVENT-DRIVEN fix for the broken scheduler chain. Previously, `ScheduleCheckAsync` was only called directly in client code. If the direct call failed, no import history check would be scheduled. By triggering this event, we ensure the scheduler is invoked reliably via the notification service event pipeline.

**Example Usage**:
```bash
curl -X POST "http://192.168.150.52:5201/api/events/templates-queued" \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "4811266",
    "clientName": "Henry County Schools",
    "templateCount": 3,
    "queueIds": [12345, 12346, 12347],
    "templateFiles": ["NewHire_Template.csv", "Termination_Template.csv"],
    "importTypes": ["NewHire", "Termination"],
    "delayMinutes": 90
  }'
```

---

## Common Event Patterns

### Event Validation

All events follow consistent validation patterns:
- Required fields are validated (returns 400 if missing)
- Guid fields cannot be `Guid.Empty`
- String fields are checked for null/empty where required
- Validation errors return descriptive messages

### Event Response Pattern

All endpoints return `202 Accepted` to indicate the event was received and queued for processing:
```json
{
  "message": "Event processed",
  "sagaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

Processing is asynchronous - the response confirms receipt, not completion.

### Notification Grouping

Events use `GroupKey` to prevent duplicate notifications:
- `saga:stuck:{sagaId}` - Saga stuck notifications
- `import:failed:{sagaId}` - Import failure notifications
- `file:error:{clientId}:{errorType}` - File processing errors
- `sla:breach:{slaType}:{sagaId}` - SLA breach notifications
- `plansource:failed:{operationType}:{sagaId}` - PlanSource failures
- `aggregate:stalled:{sagaId}` - Aggregate stall notifications

When an event with an existing `GroupKey` is received:
1. Existing notification is updated (not recreated)
2. `groupCount` is incremented
3. Timestamps and metadata are refreshed
4. Notification is re-dispatched to all channels

### Severity-Based Routing

Notification channels are activated based on severity:

| Severity | SignalR | Email | SMS |
|----------|---------|-------|-----|
| Info | Yes | No | No |
| Warning | Yes | Yes | No |
| Urgent | Yes | Yes | No |
| Critical | Yes | Yes | Yes |

### Correlation and Tracing

All events support distributed tracing:
- `correlationId`: Links related events across services
- `tenantId`: Enables tenant-specific filtering
- `sagaId`: Links to workflow/saga instance
- `eventId`: Links to domain event (event sourcing ready)

---

## Integration Examples

### Example 1: CensusReconciliationService Integration

```csharp
// In state processor when saga gets stuck
if (IsStuck(saga))
{
    await _notificationClient.PublishSagaStuckEvent(new SagaStuckEvent
    {
        SagaId = saga.Id,
        ClientId = saga.ClientId,
        ClientName = saga.ClientName,
        StuckDuration = DateTime.UtcNow - saga.LastStateChange,
        CurrentState = saga.CurrentState,
        FileName = saga.Context.FileName
    });
}
```

### Example 2: File Monitoring Service

```csharp
// When file is detected
await _httpClient.PostAsJsonAsync(
    "http://192.168.150.52:5201/api/events/file-picked-up",
    new FilePickedUpEvent
    {
        SagaId = newSagaId,
        ClientId = detectedClient,
        FileName = fileInfo.Name,
        FilePath = fileInfo.FullName,
        FileSizeBytes = fileInfo.Length
    });
```

### Example 3: PlanSource Integration Wrapper

```csharp
// When PlanSource operation fails
try
{
    await _planSourceApi.UploadFile(file);
}
catch (PlanSourceApiException ex)
{
    await _notificationClient.PublishPlanSourceFailedEvent(new PlanSourceOperationFailedEvent
    {
        SagaId = sagaId,
        ClientId = clientId,
        OperationType = "FileUpload",
        ErrorMessage = ex.Message,
        ErrorCode = ex.Code,
        IsRetryable = ex.IsRetryable,
        AttemptNumber = currentAttempt,
        MaxRetries = maxRetries,
        Severity = currentAttempt >= maxRetries
            ? NotificationSeverity.Urgent
            : NotificationSeverity.Warning
    });
}
```

### Example 4: Import Completion Handler

```csharp
// When import completes successfully
await _httpClient.PostAsJsonAsync(
    "http://192.168.150.52:5201/api/events/import-completed",
    new ImportCompletedEvent
    {
        SagaId = saga.Id,
        ClientId = saga.ClientId,
        ClientName = saga.ClientName,
        FileName = saga.Context.FileName,
        TotalRecords = saga.Context.TotalRecords,
        SuccessCount = saga.Context.SuccessCount,
        FailureCount = saga.Context.FailureCount,
        NewHireCount = saga.Context.NewHireCount,
        TerminationCount = saga.Context.TerminationCount,
        StartedAt = saga.CreatedAt,
        CompletedAt = DateTime.UtcNow
    });
```

---

## Best Practices

### 1. Always Set CorrelationId

Use correlation IDs to track requests across services:
```json
{
  "correlationId": "saga-123-step-5-plansource-upload"
}
```

### 2. Provide Context in Error Messages

Include actionable information:
```json
{
  "errorMessage": "PlanSource API returned HTTP 429: Rate limit exceeded. Retry after 60 seconds.",
  "errorCode": "PS-429"
}
```

### 3. Use Appropriate Severity Levels

Don't over-escalate:
- **Info**: Normal operations, file picked up, processing started
- **Warning**: Recoverable errors, first retry attempts, delays
- **Urgent**: Multiple failures, SLA breaches, stuck workflows
- **Critical**: System failures, data loss risk, blocking issues

### 4. Set Realistic Delay for Templates Queued

PlanSource processing time varies by template count and complexity:
- 1-3 templates: 60-90 minutes
- 4-10 templates: 90-120 minutes
- 10+ templates: 120-180 minutes

### 5. Include Suggested Actions

Help operators resolve issues quickly:
```json
{
  "suggestedActions": [
    "Check PlanSource API status",
    "Verify network connectivity",
    "Review recent deployments"
  ]
}
```

---

## Monitoring and Troubleshooting

### Check Event Processing

Monitor NotificationService logs for event processing:
```
[2025-12-18 10:30:15] INFO: Received SagaStuckEvent: SagaId=3fa85f64, ClientId=HenryCounty
[2025-12-18 10:30:16] INFO: Created notification {NotificationId} for stuck saga {SagaId} with severity Critical
[2025-12-18 10:30:17] INFO: Dispatched notification via SignalR, Email, SMS
```

### Verify Notification Creation

Query the notifications endpoint:
```bash
curl -X GET "http://192.168.150.52:5201/api/notifications/active" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Check Multi-Channel Dispatch

Verify notification appears in:
1. SignalR hub (real-time UI updates)
2. Email inbox (for Warning+ severity)
3. SMS (for Critical severity)

### Common Issues

**Event returns 400 Bad Request**
- Check required fields (sagaId, clientId, escalationId)
- Ensure Guids are not empty
- Verify JSON structure matches examples

**Notification not created**
- Check NotificationService logs for errors
- Verify event handler is registered in DI
- Confirm notification service is running

**Notification created but not dispatched**
- Check dispatcher configuration
- Verify channel settings (SignalR, Email, SMS)
- Review user preferences for channel routing

---

## Related Documentation

- [Notifications API Endpoints](./notifications-endpoints.md) - Core notification management
- [Subscriptions API Endpoints](./subscriptions-endpoints.md) - Event subscription management
- [Preferences API Endpoints](./preferences-endpoints.md) - User notification preferences
- [NotificationService.Client Library](../client/notification-client.md) - Typed client for event publishing
- [Event Handler Architecture](../architecture/event-handlers.md) - Event processing pipeline

---

## Support

For issues or questions:
- Review NotificationService logs at `D:\Logs\NotificationService\`
- Check CensusReconciliationService integration at port 5100
- Verify service health at `http://192.168.150.52:5201/health`
- Contact ops team for notification routing issues
