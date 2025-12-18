# Test Email Endpoints

**Base URL:** `http://192.168.150.52:5201/api/test-emails`

The Test Email API provides targeted test email functionality with comprehensive recipient management, delivery tracking, and full audit trail. Send test emails to specific recipient groups, individual contacts, or ad-hoc email addresses for template validation, integration testing, and stakeholder review.

## Table of Contents

- [Overview](#overview)
- [Group Purpose and Eligibility](#group-purpose-and-eligibility)
- [Endpoints](#endpoints)
  - [Sending Test Emails](#sending-test-emails)
  - [Recipient Discovery](#recipient-discovery)
  - [Delivery History](#delivery-history)
- [Examples](#examples)
- [Error Handling](#error-handling)
- [Best Practices](#best-practices)

---

## Overview

The Test Email API is designed for safe testing and validation of email templates and notification workflows without affecting production recipient lists. Key capabilities include:

- **Recipient Group Targeting** - Send to predefined test groups (QA teams, stakeholders)
- **Contact-Based Sending** - Send to specific individuals by contact ID
- **Ad-Hoc Delivery** - Send directly to email addresses bypassing the contact system
- **Recipient Preview** - Preview who will receive an email before sending
- **Full Audit Trail** - Track every test email with reason, initiator, and delivery status
- **Group Protection** - Production groups are protected from accidental test emails

All test email deliveries are tracked separately from production notifications and include metadata for troubleshooting and compliance.

---

## Group Purpose and Eligibility

Recipient groups have a `Purpose` attribute that controls their eligibility for test emails:

| Purpose | Production Routing | Test Emails Allowed | Use Case |
|---------|-------------------|---------------------|----------|
| `Production` | Yes | No | Live stakeholder notifications only |
| `TestOnly` | No | Yes | QA teams, test environments |
| `Both` | Yes | Yes | Groups that serve dual purposes |

**Protection Mechanism:** Attempting to send test emails to a `Production`-only group will return a `400 Bad Request` with a clear error message. This prevents accidental test emails to production recipients.

---

## Endpoints

### Sending Test Emails

#### POST /api/test-emails/send-to-group

Send a test email to all active members of a recipient group.

**Request Body:**

```json
{
  "templateName": "daily_import_summary",
  "recipientGroupId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "templateData": "{\"ClientName\": \"Test Corp\", \"ImportDate\": \"2025-12-18\", \"RecordsProcessed\": 1500}",
  "testReason": "Validating new template layout before Monday deployment"
}
```

**Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `templateName` | string | Yes | Name of email template to use |
| `recipientGroupId` | guid | Yes | ID of the recipient group |
| `templateData` | string | No | JSON string with template variable values |
| `testReason` | string | No | Reason for sending (audit trail) |

**Response:** `200 OK` (Success)

```json
{
  "success": true,
  "deliveryId": "d7f3b2a1-8c5e-4d6f-9a7b-3e2f1c0d8e9a",
  "messageId": "msg_abc123xyz",
  "message": "Test email sent successfully to 5 recipient(s)",
  "sentAt": "2025-12-18T16:55:00Z",
  "recipients": [
    "qa-lead@example.com",
    "qa-tester1@example.com",
    "qa-tester2@example.com",
    "product-owner@example.com",
    "tech-lead@example.com"
  ],
  "renderedSubject": "Import Summary for Test Corp - 2025-12-18"
}
```

**Response:** `400 Bad Request` (Failure)

```json
{
  "success": false,
  "deliveryId": "d7f3b2a1-8c5e-4d6f-9a7b-3e2f1c0d8e9a",
  "errorMessage": "SMTP connection failed: timeout",
  "message": "Test email failed: SMTP connection failed: timeout",
  "recipients": ["qa-lead@example.com"],
  "renderedSubject": "Import Summary for Test Corp - 2025-12-18"
}
```

**Errors:**

- `404 Not Found` - Recipient group does not exist or template not found
- `400 Bad Request` - Group is production-only, has no members, or has no active members

**Important Notes:**

1. **Active Members Only** - Only sends to group members where `IsActive = true`
2. **Delivery Tracking** - Creates a `TestEmailDelivery` record with full audit data
3. **Initiator Tracking** - Captures user from JWT claims or defaults to "api"
4. **Template Rendering** - Validates template exists and renders with provided data
5. **Delivery ID** - Always returned for tracking, even on failure

---

#### POST /api/test-emails/send-to-contacts

Send a test email to specific contacts by their contact IDs.

**Request Body:**

```json
{
  "templateName": "workflow_notification",
  "contactIds": [
    "a1b2c3d4-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
    "b2c3d4e5-6f7a-8b9c-0d1e-2f3a4b5c6d7e"
  ],
  "templateData": "{\"WorkflowName\": \"Census Import\", \"Status\": \"Completed\"}",
  "testReason": "Stakeholder review of workflow completion notification"
}
```

**Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `templateName` | string | Yes | Name of email template to use |
| `contactIds` | array | Yes | List of contact IDs (at least one required) |
| `templateData` | string | No | JSON string with template variable values |
| `testReason` | string | No | Reason for sending (audit trail) |

**Response:** `200 OK` (Success)

```json
{
  "success": true,
  "deliveryId": "e8a4c5b2-9d6f-3e7a-8b1c-4f5d6e7a8b9c",
  "messageId": "msg_def456uvw",
  "message": "Test email sent successfully to 2 recipient(s)",
  "sentAt": "2025-12-18T17:05:00Z",
  "recipients": [
    "product-manager@example.com",
    "business-analyst@example.com"
  ],
  "renderedSubject": "Workflow Notification: Census Import - Completed"
}
```

**Errors:**

- `404 Not Found` - One or more contact IDs not found (returns list of missing IDs)
- `400 Bad Request` - No contact IDs provided or no active contacts in the list

**Use Case:** Send test emails to specific stakeholders for review without creating a dedicated recipient group.

---

#### POST /api/test-emails/send-to-addresses

Send a test email directly to email addresses, bypassing the contact system entirely.

**Request Body:**

```json
{
  "templateName": "escalation_alert",
  "emailAddresses": [
    "external-consultant@acme.com",
    "vendor-support@vendor.com"
  ],
  "templateData": "{\"AlertLevel\": \"High\", \"Message\": \"Import failure detected\"}",
  "testReason": "Testing escalation notification with external stakeholders"
}
```

**Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `templateName` | string | Yes | Name of email template to use |
| `emailAddresses` | array | Yes | List of email addresses (at least one required) |
| `templateData` | string | No | JSON string with template variable values |
| `testReason` | string | No | Reason for sending (audit trail) |

**Response:** `200 OK` (Success)

```json
{
  "success": true,
  "deliveryId": "f9b5d6c3-0e7a-4f8b-9c2d-5e6f7a8b9c0d",
  "messageId": "msg_ghi789rst",
  "message": "Test email sent successfully to 2 recipient(s)",
  "sentAt": "2025-12-18T17:15:00Z",
  "recipients": [
    "external-consultant@acme.com",
    "vendor-support@vendor.com"
  ],
  "renderedSubject": "Escalation Alert: High Priority Issue"
}
```

**Errors:**

- `400 Bad Request` - No email addresses provided or invalid email format detected
- `404 Not Found` - Template not found

**Important Notes:**

1. **Email Validation** - Basic email format validation using `System.Net.Mail.MailAddress`
2. **Bypass Contacts** - Does not require contacts to exist in the system
3. **No Group Association** - Delivery record has `RecipientGroupId = null`
4. **External Recipients** - Useful for testing with vendors, consultants, or external stakeholders

**Use Case:** Send test emails to external parties, temporary addresses, or recipients not yet added to the contact system.

---

### Recipient Discovery

#### GET /api/test-emails/eligible-groups

Get all recipient groups eligible for test emails (groups with `Purpose = TestOnly` or `Both`).

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `clientId` | string | No | Filter by client ID (omit for all clients) |

**Response:** `200 OK`

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "QA Team - Import Testing",
    "clientId": "CLIENT123",
    "description": "QA team members responsible for import validation",
    "purpose": "TestOnly",
    "tags": ["qa", "import", "testing"],
    "isActive": true,
    "memberCount": 5,
    "policyCount": 0
  },
  {
    "id": "4gb96f75-6828-5673-c4gd-3d074g77bgb7",
    "name": "Stakeholders - Census Notifications",
    "clientId": "CLIENT123",
    "description": "Business stakeholders for census import updates",
    "purpose": "Both",
    "tags": ["stakeholders", "census", "business"],
    "isActive": true,
    "memberCount": 12,
    "policyCount": 3
  }
]
```

**Field Descriptions:**

| Field | Description |
|-------|-------------|
| `id` | Group unique identifier |
| `name` | Human-readable group name |
| `clientId` | Client identifier (for multi-tenant scenarios) |
| `description` | Group purpose and membership details |
| `purpose` | `TestOnly`, `Both`, or `Production` |
| `tags` | Searchable tags for group categorization |
| `isActive` | Whether group is currently active |
| `memberCount` | Number of contacts in the group |
| `policyCount` | Number of routing policies using this group |

**Use Case:** Populate a dropdown in a UI for selecting test email recipients, or discover available test groups programmatically.

---

#### GET /api/test-emails/preview-recipients/{groupId}

Preview who would receive an email if sent to a specific group.

**Parameters:**

- `groupId` (guid, path) - Recipient group ID

**Response:** `200 OK`

```json
{
  "totalRecipients": 5,
  "recipients": [
    {
      "contactId": "a1b2c3d4-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
      "name": "Sarah Chen",
      "email": "sarah.chen@example.com",
      "organization": "QA Department",
      "groupName": "QA Team - Import Testing"
    },
    {
      "contactId": "b2c3d4e5-6f7a-8b9c-0d1e-2f3a4b5c6d7e",
      "name": "Marcus Johnson",
      "email": "marcus.johnson@example.com",
      "organization": "QA Department",
      "groupName": "QA Team - Import Testing"
    },
    {
      "contactId": "c3d4e5f6-7a8b-9c0d-1e2f-3a4b5c6d7e8f",
      "name": "Priya Patel",
      "email": "priya.patel@example.com",
      "organization": "Engineering",
      "groupName": "QA Team - Import Testing"
    }
  ]
}
```

**Errors:**

- `404 Not Found` - Recipient group does not exist

**Important Notes:**

1. **Active Members Only** - Only includes contacts where `IsActive = true`
2. **No Email Sent** - This is a read-only preview operation
3. **Validation Before Send** - Use this to verify recipient list before sending

**Use Case:** Confirm who will receive a test email before sending, especially useful for large groups or when validating group membership changes.

---

### Delivery History

#### GET /api/test-emails/history

Get paginated test email delivery history with advanced filtering.

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `groupId` | guid | No | Filter by recipient group |
| `startDate` | datetime | No | Filter deliveries after this date |
| `endDate` | datetime | No | Filter deliveries before this date |
| `initiatedBy` | string | No | Filter by user who initiated the send |
| `successOnly` | boolean | No | Only show successful deliveries (true) or failures (false) |
| `page` | integer | No | Page number (default: 1) |
| `pageSize` | integer | No | Items per page (default: 20, max recommended: 100) |

**Response:** `200 OK`

```json
{
  "data": [
    {
      "id": "d7f3b2a1-8c5e-4d6f-9a7b-3e2f1c0d8e9a",
      "recipientGroupId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "recipientGroupName": "QA Team - Import Testing",
      "templateName": "daily_import_summary",
      "subject": "Import Summary for Test Corp - 2025-12-18",
      "recipients": [
        "qa-lead@example.com",
        "qa-tester1@example.com",
        "qa-tester2@example.com"
      ],
      "testReason": "Validating new template layout before Monday deployment",
      "initiatedBy": "sarah.chen@example.com",
      "sentAt": "2025-12-18T16:55:00Z",
      "success": true,
      "errorMessage": null,
      "messageId": "msg_abc123xyz",
      "provider": "SendGrid"
    },
    {
      "id": "e8a4c5b2-9d6f-3e7a-8b1c-4f5d6e7a8b9c",
      "recipientGroupId": null,
      "recipientGroupName": null,
      "templateName": "escalation_alert",
      "subject": "Escalation Alert: High Priority Issue",
      "recipients": [
        "external-consultant@acme.com"
      ],
      "testReason": "Testing escalation with external stakeholder",
      "initiatedBy": "marcus.johnson@example.com",
      "sentAt": "2025-12-18T15:30:00Z",
      "success": false,
      "errorMessage": "SMTP connection timeout after 30 seconds",
      "messageId": null,
      "provider": "SMTP"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 47,
  "totalPages": 3,
  "hasNext": true,
  "hasPrevious": false
}
```

**Field Descriptions:**

| Field | Description |
|-------|-------------|
| `id` | Unique delivery record ID |
| `recipientGroupId` | Group ID (null for ad-hoc sends) |
| `recipientGroupName` | Group name (null for ad-hoc sends) |
| `templateName` | Template used for the email |
| `subject` | Rendered email subject line |
| `recipients` | List of email addresses sent to |
| `testReason` | Reason provided by sender (audit trail) |
| `initiatedBy` | User who sent the test email |
| `sentAt` | Timestamp when email was sent |
| `success` | Whether delivery succeeded |
| `errorMessage` | Error details if `success = false` |
| `messageId` | Email provider message ID (for tracking) |
| `provider` | Email provider used (`SendGrid` or `SMTP`) |

**Pagination Fields:**

| Field | Description |
|-------|-------------|
| `page` | Current page number |
| `pageSize` | Items per page |
| `totalItems` | Total matching records |
| `totalPages` | Total pages available |
| `hasNext` | Whether next page exists |
| `hasPrevious` | Whether previous page exists |

**Example Queries:**

```bash
# Get all test emails sent today
GET /api/test-emails/history?startDate=2025-12-18T00:00:00Z

# Get failed deliveries for a specific group
GET /api/test-emails/history?groupId=3fa85f64-5717-4562-b3fc-2c963f66afa6&successOnly=false

# Get test emails initiated by a specific user
GET /api/test-emails/history?initiatedBy=sarah.chen@example.com

# Get recent deliveries (page 2, 50 per page)
GET /api/test-emails/history?page=2&pageSize=50
```

**Use Case:** Audit test email activity, troubleshoot delivery failures, or generate reports on testing activity.

---

#### GET /api/test-emails/history/{id}

Get a specific test email delivery record by ID.

**Parameters:**

- `id` (guid, path) - Delivery record ID

**Response:** `200 OK`

```json
{
  "id": "d7f3b2a1-8c5e-4d6f-9a7b-3e2f1c0d8e9a",
  "recipientGroupId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "recipientGroupName": "QA Team - Import Testing",
  "templateName": "daily_import_summary",
  "subject": "Import Summary for Test Corp - 2025-12-18",
  "recipients": [
    "qa-lead@example.com",
    "qa-tester1@example.com",
    "qa-tester2@example.com"
  ],
  "testReason": "Validating new template layout before Monday deployment",
  "initiatedBy": "sarah.chen@example.com",
  "sentAt": "2025-12-18T16:55:00Z",
  "success": true,
  "errorMessage": null,
  "messageId": "msg_abc123xyz",
  "provider": "SendGrid"
}
```

**Errors:**

- `404 Not Found` - Delivery record does not exist

**Use Case:** Retrieve detailed information about a specific test email delivery for troubleshooting or audit purposes.

---

## Examples

### Example 1: Send Test Email to QA Team

**Scenario:** QA team needs to validate a new daily import summary template before it goes to production stakeholders.

**Step 1: Find Eligible Groups**

```bash
GET /api/test-emails/eligible-groups?clientId=CLIENT123
```

**Step 2: Preview Recipients**

```bash
GET /api/test-emails/preview-recipients/3fa85f64-5717-4562-b3fc-2c963f66afa6

Response:
{
  "totalRecipients": 5,
  "recipients": [
    { "name": "Sarah Chen", "email": "sarah.chen@example.com", ... },
    { "name": "Marcus Johnson", "email": "marcus.johnson@example.com", ... },
    ...
  ]
}
```

**Step 3: Send Test Email**

```bash
POST /api/test-emails/send-to-group
Content-Type: application/json

{
  "templateName": "daily_import_summary",
  "recipientGroupId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "templateData": "{\"ClientName\": \"Acme Corp\", \"ImportDate\": \"2025-12-18\", \"RecordsProcessed\": 1500, \"ErrorCount\": 0}",
  "testReason": "Final QA validation before production deployment on Monday"
}

Response:
{
  "success": true,
  "deliveryId": "d7f3b2a1-8c5e-4d6f-9a7b-3e2f1c0d8e9a",
  "message": "Test email sent successfully to 5 recipient(s)",
  "sentAt": "2025-12-18T16:55:00Z"
}
```

**Step 4: Verify Delivery**

```bash
GET /api/test-emails/history/d7f3b2a1-8c5e-4d6f-9a7b-3e2f1c0d8e9a

Response:
{
  "success": true,
  "templateName": "daily_import_summary",
  "messageId": "msg_abc123xyz",
  "provider": "SendGrid"
}
```

---

### Example 2: Send to External Stakeholder

**Scenario:** Need to share a workflow notification template with an external consultant for review.

```bash
POST /api/test-emails/send-to-addresses
Content-Type: application/json

{
  "templateName": "workflow_notification",
  "emailAddresses": [
    "consultant@external-firm.com"
  ],
  "templateData": "{\"WorkflowName\": \"Census Import\", \"Status\": \"Completed\", \"Duration\": \"2 minutes\"}",
  "testReason": "External consultant review of workflow completion notification design"
}

Response:
{
  "success": true,
  "deliveryId": "f9b5d6c3-0e7a-4f8b-9c2d-5e6f7a8b9c0d",
  "message": "Test email sent successfully to 1 recipient(s)",
  "recipients": ["consultant@external-firm.com"],
  "renderedSubject": "Workflow Completed: Census Import"
}
```

---

### Example 3: Troubleshoot Delivery Failure

**Scenario:** Test email failed to send, need to investigate why.

**Step 1: Check Recent Failures**

```bash
GET /api/test-emails/history?successOnly=false&page=1&pageSize=10

Response:
{
  "data": [
    {
      "id": "e8a4c5b2-9d6f-3e7a-8b1c-4f5d6e7a8b9c",
      "success": false,
      "errorMessage": "SMTP connection timeout after 30 seconds",
      "provider": "SMTP",
      "sentAt": "2025-12-18T15:30:00Z"
    }
  ]
}
```

**Step 2: Get Full Details**

```bash
GET /api/test-emails/history/e8a4c5b2-9d6f-3e7a-8b1c-4f5d6e7a8b9c

Response:
{
  "id": "e8a4c5b2-9d6f-3e7a-8b1c-4f5d6e7a8b9c",
  "templateName": "escalation_alert",
  "recipients": ["external-consultant@acme.com"],
  "success": false,
  "errorMessage": "SMTP connection timeout after 30 seconds",
  "provider": "SMTP",
  "initiatedBy": "marcus.johnson@example.com"
}
```

**Action:** SMTP timeout suggests network or provider issue. Switch to SendGrid provider or check SMTP server connectivity.

---

### Example 4: Audit Test Email Activity

**Scenario:** Generate a report of all test emails sent by the QA team in the last week.

```bash
GET /api/test-emails/history?startDate=2025-12-11T00:00:00Z&endDate=2025-12-18T23:59:59Z&groupId=3fa85f64-5717-4562-b3fc-2c963f66afa6&pageSize=100

Response:
{
  "data": [
    {
      "id": "...",
      "templateName": "daily_import_summary",
      "testReason": "QA validation before deployment",
      "initiatedBy": "sarah.chen@example.com",
      "sentAt": "2025-12-18T16:55:00Z",
      "success": true
    },
    {
      "id": "...",
      "templateName": "workflow_notification",
      "testReason": "Testing workflow completion email",
      "initiatedBy": "marcus.johnson@example.com",
      "sentAt": "2025-12-17T14:20:00Z",
      "success": true
    }
  ],
  "totalItems": 23,
  "totalPages": 1
}
```

---

### Example 5: Send to Multiple Individual Contacts

**Scenario:** Send a test email to the product manager and business analyst for stakeholder review.

**Step 1: Get Contact IDs**

```bash
# Assume you have contact IDs from contact management system
# a1b2c3d4-5e6f-7a8b-9c0d-1e2f3a4b5c6d (Product Manager)
# b2c3d4e5-6f7a-8b9c-0d1e-2f3a4b5c6d7e (Business Analyst)
```

**Step 2: Send Test Email**

```bash
POST /api/test-emails/send-to-contacts
Content-Type: application/json

{
  "templateName": "workflow_notification",
  "contactIds": [
    "a1b2c3d4-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
    "b2c3d4e5-6f7a-8b9c-0d1e-2f3a4b5c6d7e"
  ],
  "templateData": "{\"WorkflowName\": \"Census Import\", \"Status\": \"Completed\"}",
  "testReason": "Stakeholder review of workflow completion notification"
}

Response:
{
  "success": true,
  "deliveryId": "e8a4c5b2-9d6f-3e7a-8b1c-4f5d6e7a8b9c",
  "message": "Test email sent successfully to 2 recipient(s)",
  "recipients": [
    "product-manager@example.com",
    "business-analyst@example.com"
  ]
}
```

---

## Error Handling

### Standard Error Responses

**400 Bad Request - Production Group**

```json
{
  "message": "Group 'Executive Stakeholders' is marked as Production-only and cannot receive test emails. Change the group purpose to 'TestOnly' or 'Both' to enable test emails."
}
```

**400 Bad Request - No Active Members**

```json
{
  "message": "Group 'QA Team - Import Testing' has no active members to send to"
}
```

**400 Bad Request - Invalid Email**

```json
{
  "message": "Invalid email addresses: invalid-email, @missing-local-part, no-domain@"
}
```

**404 Not Found - Group**

```json
{
  "message": "Recipient group 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found"
}
```

**404 Not Found - Template**

```json
{
  "message": "Template 'unknown_template' not found"
}
```

**404 Not Found - Contacts**

```json
{
  "message": "Contacts not found: a1b2c3d4-5e6f-7a8b-9c0d-1e2f3a4b5c6d, b2c3d4e5-6f7a-8b9c-0d1e-2f3a4b5c6d7e"
}
```

### Email Delivery Failures

When email sending fails, the API returns `400 Bad Request` with delivery details:

```json
{
  "success": false,
  "deliveryId": "d7f3b2a1-8c5e-4d6f-9a7b-3e2f1c0d8e9a",
  "errorMessage": "SMTP connection failed: timeout",
  "message": "Test email failed: SMTP connection failed: timeout",
  "recipients": ["user@example.com"]
}
```

The `deliveryId` is always returned so you can track the failure in the delivery history.

---

## Best Practices

### Group Management

1. **Set Appropriate Purpose** - Mark groups as `TestOnly` for QA teams, `Production` for live stakeholders, and `Both` only when necessary
2. **Use Descriptive Names** - Include purpose in name: "QA Team - Import Testing" vs "Group1"
3. **Maintain Active Status** - Deactivate contacts who should no longer receive test emails
4. **Document Membership** - Use group descriptions to explain who should be included

### Testing Workflow

1. **Preview Before Sending** - Always use `/preview-recipients/{groupId}` to verify recipient list
2. **Check Eligible Groups** - Use `/eligible-groups` to find available test groups
3. **Provide Test Reasons** - Include clear `testReason` for audit trail and compliance
4. **Use Realistic Data** - Provide template data that resembles production scenarios
5. **Validate Templates First** - Use template validation endpoints before sending test emails

### Delivery Tracking

1. **Save Delivery IDs** - Store `deliveryId` from responses for later reference
2. **Monitor Failures** - Regularly check `/history?successOnly=false` for delivery issues
3. **Track Initiators** - Use `initiatedBy` filter to track who is sending test emails
4. **Set Up Alerts** - Monitor delivery history for patterns indicating provider issues

### Security and Compliance

1. **Protect Production Groups** - Never change production groups to `Both` without approval
2. **Audit Test Activity** - Use delivery history for compliance and security audits
3. **Validate External Addresses** - Verify external email addresses before using `send-to-addresses`
4. **Document Test Reasons** - Always provide meaningful `testReason` for audit trail

### Performance

1. **Limit Page Size** - Use reasonable `pageSize` (20-100) when querying history
2. **Use Date Filters** - Filter history queries by date range to improve performance
3. **Cache Group Lists** - Cache results of `/eligible-groups` to reduce API calls
4. **Batch Test Sends** - Use groups instead of multiple `send-to-contacts` calls

### Common Pitfalls

1. **Sending to Production Groups** - Always verify group purpose before sending
2. **Missing Active Checks** - Inactive contacts won't receive emails
3. **Invalid JSON Data** - Ensure `templateData` is properly escaped JSON string
4. **Forgetting Audit Trail** - Always provide `testReason` for compliance
5. **Not Checking Previews** - Verify recipient list before sending to large groups

---

## Related Documentation

- [Templates Endpoints](./templates-endpoints.md) - Email template management
- [Notifications Endpoints](./notifications-endpoints.md) - Production notification sending
- [Delivery Endpoints](./delivery-endpoints.md) - Delivery tracking and status
- [Recipient Groups API](./recipient-groups-endpoints.md) - Group management

---

**Last Updated:** 2025-12-18
**API Version:** v1
**Service:** NotificationService.Api
**Port:** 5201
