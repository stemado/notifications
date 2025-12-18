# Templates Endpoints

**Base URL:** `http://192.168.150.52:5201/api/templates`

The Templates API provides comprehensive email template management capabilities, including CRUD operations, template rendering, validation, variable extraction, and email sending with delivery tracking.

## Table of Contents

- [Overview](#overview)
- [Template Structure](#template-structure)
- [Variable Syntax](#variable-syntax)
- [Endpoints](#endpoints)
  - [Template Retrieval](#template-retrieval)
  - [Template Management](#template-management)
  - [Template Rendering](#template-rendering)
  - [Email Sending](#email-sending)
  - [Health and Documentation](#health-and-documentation)
- [Examples](#examples)
- [Error Handling](#error-handling)

---

## Overview

The Templates API uses the **Scriban template engine** with **Liquid/Jinja2-compatible syntax** for dynamic email content generation. Templates support:

- **Variable substitution** with `{{ variable }}`
- **Conditional logic** with `{% if %}` blocks
- **Loops** with `{% for %}` blocks
- **Filters** like `{{ value | upcase }}`
- **Safe rendering** (missing variables display as empty strings)

All templates are stored in a database and can be managed through these RESTful endpoints.

---

## Template Structure

### EmailTemplate Model

```json
{
  "id": 1,
  "name": "daily_import_summary",
  "description": "Daily summary of import operations",
  "subject": "Import Summary for {{ ClientName }} - {{ ImportDate }}",
  "htmlContent": "<h1>Hello {{ ClientName }}!</h1>...",
  "textContent": "Hello {{ ClientName }}!...",
  "variables": {
    "ClientName": "The client's display name",
    "ImportDate": "Date of import",
    "RecordsProcessed": "Number of records processed"
  },
  "testData": {
    "ClientName": "Acme Corporation",
    "ImportDate": "2025-11-03",
    "RecordsProcessed": 1500
  },
  "defaultRecipients": "admin@example.com,operations@example.com",
  "templateType": "notification",
  "isActive": true,
  "createdAt": "2025-11-01T10:00:00Z",
  "updatedAt": "2025-11-03T15:30:00Z"
}
```

### Field Descriptions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | integer | Auto-generated | Primary key |
| `name` | string | Yes | Unique template identifier (e.g., "daily_import_summary") |
| `description` | string | No | Human-readable purpose description |
| `subject` | string | Yes | Email subject line (supports template variables) |
| `htmlContent` | string | No | HTML email body with template variables |
| `textContent` | string | No | Plain text fallback for non-HTML email clients |
| `variables` | object | No | Variable definitions with descriptions |
| `testData` | object | No | Sample data for template preview/testing |
| `defaultRecipients` | string | No | Comma-separated list of default recipients |
| `templateType` | string | No | Template category (default: "notification") |
| `isActive` | boolean | No | Whether template is active (default: true) |
| `createdAt` | datetime | Auto-generated | Template creation timestamp |
| `updatedAt` | datetime | Auto-generated | Last modification timestamp |

### Common Template Types

- `notification` - General notifications
- `success` - Success confirmation emails
- `error` - Error alert emails
- `escalation` - Escalation notifications
- `workflow_triggered` - Workflow event notifications
- `file_detected` - File detection alerts

---

## Variable Syntax

Templates use **Liquid/Jinja2 syntax** powered by the Scriban engine.

### Basic Variable Substitution

```html
{{ ClientName }}          <!-- Simple variable -->
{{ user.FirstName }}      <!-- Object property -->
{{ items[0].Name }}       <!-- Array element -->
```

### Filters

```html
{{ value | upcase }}              <!-- CONVERT TO UPPERCASE -->
{{ value | downcase }}            <!-- convert to lowercase -->
{{ value | capitalize }}          <!-- Capitalize First Letter -->
{{ items | size }}                <!-- Get array length -->
{{ text | truncate: 100 }}        <!-- Truncate to 100 characters -->
```

### Conditionals

```html
{% if ErrorCount > 0 %}
  <p style="color: red;">Warning: {{ ErrorCount }} errors occurred.</p>
{% endif %}

{% if status == 'Success' %}
  <span style="color: green;">Success</span>
{% elsif status == 'Failed' %}
  <span style="color: red;">Failed</span>
{% else %}
  <span style="color: orange;">Pending</span>
{% endif %}
```

### Loops

```html
<ul>
{% for item in ImportSummaries %}
  <li>{{ item.ImportType }}: {{ item.TotalCount }} records</li>
{% endfor %}
</ul>

<table>
{% for row in DataRows %}
  <tr>
    <td>{{ row.Name }}</td>
    <td>{{ row.Value }}</td>
  </tr>
{% endfor %}
</table>
```

### Comments

```html
{% comment %}
This is a comment - will not appear in rendered output
{% endcomment %}
```

### Complete Example

```html
<h1>Hello {{ ClientName }}!</h1>
<p>Your import on {{ ImportDate }} processed {{ RecordsProcessed }} records.</p>

{% if ErrorCount > 0 %}
<div style="background-color: #ffeeee; padding: 10px; border: 1px solid red;">
  <strong>Warning:</strong> {{ ErrorCount }} records failed to import.
</div>
{% endif %}

<h2>Import Summary:</h2>
<table border="1" cellpadding="5">
  <thead>
    <tr>
      <th>Import Type</th>
      <th>Total Count</th>
      <th>Status</th>
    </tr>
  </thead>
  <tbody>
  {% for item in ImportSummaries %}
    <tr>
      <td>{{ item.ImportType }}</td>
      <td>{{ item.TotalCount }}</td>
      <td>
        {% if item.Status == 'Success' %}
          <span style="color: green;">{{ item.Status }}</span>
        {% else %}
          <span style="color: red;">{{ item.Status }}</span>
        {% endif %}
      </td>
    </tr>
  {% endfor %}
  </tbody>
</table>
```

---

## Endpoints

### Template Retrieval

#### GET /api/templates

Get all active email templates.

**Response:** `200 OK`

```json
{
  "count": 5,
  "templates": [
    {
      "id": 1,
      "name": "daily_import_summary",
      "description": "Daily summary of import operations",
      "subject": "Import Summary for {{ ClientName }}",
      "templateType": "notification",
      "isActive": true,
      "createdAt": "2025-11-01T10:00:00Z",
      "updatedAt": "2025-11-03T15:30:00Z"
    }
  ]
}
```

---

#### GET /api/templates/all

Get all email templates, including inactive ones.

**Response:** `200 OK`

Same structure as `/api/templates`, but includes templates where `isActive = false`.

---

#### GET /api/templates/{id}

Get a specific template by ID with full details.

**Parameters:**
- `id` (integer, path) - Template ID

**Response:** `200 OK`

```json
{
  "id": 1,
  "name": "daily_import_summary",
  "description": "Daily summary of import operations",
  "subject": "Import Summary for {{ ClientName }} - {{ ImportDate }}",
  "htmlContent": "<h1>Hello {{ ClientName }}!</h1>...",
  "textContent": "Hello {{ ClientName }}!...",
  "variables": {
    "ClientName": "The client's display name",
    "ImportDate": "Date of import"
  },
  "testData": {
    "ClientName": "Acme Corporation",
    "ImportDate": "2025-11-03"
  },
  "defaultRecipients": "admin@example.com",
  "templateType": "notification",
  "isActive": true,
  "createdAt": "2025-11-01T10:00:00Z",
  "updatedAt": "2025-11-03T15:30:00Z"
}
```

**Errors:**
- `404 Not Found` - Template does not exist

---

#### GET /api/templates/name/{name}

Get a template by unique name.

**Parameters:**
- `name` (string, path) - Template name (e.g., "daily_import_summary")

**Response:** `200 OK` (same structure as GET by ID)

**Errors:**
- `400 Bad Request` - Template name is required
- `404 Not Found` - Template not found

---

#### GET /api/templates/type/{type}

Get the first active template matching a specific type.

**Parameters:**
- `type` (string, path) - Template type (e.g., "workflow_triggered", "file_detected")

**Response:** `200 OK` (same structure as GET by ID)

**Errors:**
- `400 Bad Request` - Template type is required
- `404 Not Found` - No active template found for type

**Use Case:** When you need any active template of a specific category without caring which one.

---

#### GET /api/templates/type/{type}/all

Get all templates of a specific type (active and inactive).

**Parameters:**
- `type` (string, path) - Template type

**Response:** `200 OK`

```json
{
  "count": 3,
  "templates": [
    {
      "id": 5,
      "name": "workflow_success",
      "templateType": "workflow_triggered",
      "isActive": true,
      ...
    },
    {
      "id": 8,
      "name": "workflow_failed",
      "templateType": "workflow_triggered",
      "isActive": true,
      ...
    }
  ]
}
```

---

### Template Management

#### POST /api/templates

Create a new email template.

**Request Body:**

```json
{
  "name": "new_template",
  "subject": "Subject Line with {{ Variable }}",
  "description": "Optional description",
  "htmlContent": "<h1>Hello {{ Name }}!</h1>",
  "textContent": "Hello {{ Name }}!",
  "variables": {
    "Name": "Recipient name",
    "Date": "Current date"
  },
  "testData": {
    "Name": "John Doe",
    "Date": "2025-11-03"
  },
  "defaultRecipients": "admin@example.com,alerts@example.com",
  "templateType": "notification",
  "isActive": true
}
```

**Required Fields:**
- `name` - Must be unique
- `subject`

**Optional Fields:**
- `description`
- `htmlContent`
- `textContent`
- `variables`
- `testData`
- `defaultRecipients`
- `templateType` (default: "notification")
- `isActive` (default: true)

**Response:** `201 Created`

Returns the created template with Location header pointing to GET by ID endpoint.

**Errors:**
- `400 Bad Request` - Missing required fields
- `409 Conflict` - Template name already exists

---

#### PUT /api/templates/{id}

Update an existing email template.

**Parameters:**
- `id` (integer, path) - Template ID

**Request Body:**

All fields are optional. Only provided fields will be updated.

```json
{
  "subject": "Updated Subject Line",
  "htmlContent": "<h1>Updated Content</h1>",
  "isActive": false
}
```

**Response:** `200 OK`

Returns the updated template.

**Errors:**
- `404 Not Found` - Template does not exist
- `409 Conflict` - Updated name conflicts with existing template

---

#### DELETE /api/templates/{id}

Delete an email template.

**Parameters:**
- `id` (integer, path) - Template ID

**Response:** `200 OK`

```json
{
  "message": "Template 5 deleted successfully"
}
```

**Errors:**
- `404 Not Found` - Template does not exist

---

### Template Rendering

#### POST /api/templates/preview

Preview template rendering with sample data without sending an email.

**Request Body:**

```json
{
  "templateName": "daily_import_summary",
  "data": "{\"ClientName\": \"Acme Corp\", \"ImportDate\": \"2025-11-03\", \"RecordsProcessed\": 1500}"
}
```

**Fields:**
- `templateName` (required) - Name of template to preview
- `data` (optional) - JSON string with variable values (defaults to empty object)

**Response:** `200 OK`

```json
{
  "templateName": "daily_import_summary",
  "renderedSubject": "Import Summary for Acme Corp - 2025-11-03",
  "renderedBody": "<h1>Hello Acme Corp!</h1><p>Your import on 2025-11-03 processed 1500 records.</p>",
  "previewedAt": "2025-11-03T16:45:00Z"
}
```

**Errors:**
- `400 Bad Request` - Template name is required
- `404 Not Found` - Template not found

**Use Case:** Test template rendering before sending emails or validate template syntax with real data.

---

#### POST /api/templates/extract-variables/{name}

Extract all variable names used in a template's subject and body.

**Parameters:**
- `name` (string, path) - Template name

**Response:** `200 OK`

```json
{
  "templateName": "daily_import_summary",
  "variables": [
    "ClientName",
    "ErrorCount",
    "ImportDate",
    "RecordsProcessed"
  ],
  "variableCount": 4
}
```

**Errors:**
- `400 Bad Request` - Template name is required
- `404 Not Found` - Template not found

**Use Case:** Discover what data needs to be provided when rendering a template.

---

#### POST /api/templates/validate/{name}

Validate template syntax without rendering.

**Parameters:**
- `name` (string, path) - Template name

**Response:** `200 OK`

```json
{
  "templateName": "daily_import_summary",
  "isValid": true,
  "errors": [],
  "validatedAt": "2025-11-03T16:50:00Z"
}
```

**Error Response Example:**

```json
{
  "templateName": "broken_template",
  "isValid": false,
  "errors": [
    "Subject: Unexpected end of file - expecting 'endif'",
    "Body: Unknown variable 'undefined_var'"
  ],
  "validatedAt": "2025-11-03T16:50:00Z"
}
```

**Errors:**
- `400 Bad Request` - Template name is required
- `404 Not Found` - Template not found

**Use Case:** Validate template syntax before saving or after editing.

---

### Email Sending

#### POST /api/templates/send

Send an email using a template with full delivery tracking.

**Request Body:**

```json
{
  "templateName": "daily_import_summary",
  "recipients": ["user@example.com", "admin@example.com"],
  "templateData": "{\"ClientName\": \"Acme Corp\", \"ImportDate\": \"2025-11-03\", \"RecordsProcessed\": 1500, \"ErrorCount\": 0}"
}
```

**Fields:**
- `templateName` (required) - Name of template to use
- `recipients` (required) - Array of email addresses (at least one required)
- `templateData` (optional) - JSON string with variable values

**Response:** `200 OK` (Success)

```json
{
  "success": true,
  "messageId": "msg_abc123xyz",
  "deliveryId": "d7f3b2a1-8c5e-4d6f-9a7b-3e2f1c0d8e9a",
  "message": "Email sent and tracked via SendGrid",
  "sentAt": "2025-11-03T16:55:00Z"
}
```

**Response:** `400 Bad Request` (Failure)

```json
{
  "success": false,
  "deliveryId": "d7f3b2a1-8c5e-4d6f-9a7b-3e2f1c0d8e9a",
  "errorMessage": "SMTP connection failed: timeout",
  "message": "Email send failed via SendGrid: SMTP connection failed: timeout"
}
```

**Errors:**
- `400 Bad Request` - Missing template name or recipients, or email send failed
- `404 Not Found` - Template not found

**Important Notes:**

1. **Delivery Tracking**: Every send attempt creates a `NotificationDelivery` record with status (Processing, Delivered, or Failed)
2. **Notification Record**: Creates a linked `Notification` record for audit trail
3. **System User**: Ad-hoc emails are attributed to system user `00000000-0000-0000-0000-000000000001`
4. **Provider**: Automatically uses configured email provider (SendGrid or SMTP)
5. **Delivery ID**: Always returned for tracking, even on failure

**Use Case:** Send templated emails with delivery tracking and audit trail.

---

### Health and Documentation

#### GET /api/templates/health

Check template service health and database connectivity.

**Response:** `200 OK` (Healthy)

```json
{
  "isHealthy": true,
  "status": "Healthy",
  "checkedAt": "2025-11-03T17:00:00Z",
  "totalTemplates": 12,
  "activeTemplates": 9
}
```

**Response:** `200 OK` (Unhealthy)

```json
{
  "isHealthy": false,
  "status": "Unhealthy",
  "checkedAt": "2025-11-03T17:00:00Z",
  "error": "Database connection failed"
}
```

**Use Case:** Monitor service health in deployment pipelines and health dashboards.

---

#### GET /api/templates/syntax-guide

Get comprehensive template syntax documentation.

**Response:** `200 OK`

```json
{
  "guide": "# Liquid/Jinja2 Template Syntax Guide\n\n## Basic Variable Substitution\n{{ variable }}..."
}
```

Returns a markdown-formatted guide covering:
- Variable substitution
- Property access
- Filters
- Loops
- Conditionals
- Comparisons
- Complete examples

**Use Case:** Embedded help documentation in template editors or management UIs.

---

## Examples

### Example 1: Create and Send a Simple Template

**Step 1: Create Template**

```bash
POST /api/templates
Content-Type: application/json

{
  "name": "welcome_email",
  "subject": "Welcome {{ FirstName }}!",
  "htmlContent": "<h1>Welcome {{ FirstName }} {{ LastName }}!</h1><p>We're glad to have you.</p>",
  "variables": {
    "FirstName": "User's first name",
    "LastName": "User's last name"
  },
  "templateType": "notification"
}
```

**Step 2: Preview Template**

```bash
POST /api/templates/preview
Content-Type: application/json

{
  "templateName": "welcome_email",
  "data": "{\"FirstName\": \"John\", \"LastName\": \"Doe\"}"
}
```

**Step 3: Send Email**

```bash
POST /api/templates/send
Content-Type: application/json

{
  "templateName": "welcome_email",
  "recipients": ["john.doe@example.com"],
  "templateData": "{\"FirstName\": \"John\", \"LastName\": \"Doe\"}"
}
```

---

### Example 2: Complex Template with Conditional Logic

**Create Template:**

```bash
POST /api/templates
Content-Type: application/json

{
  "name": "import_report",
  "subject": "Import Report - {{ Status }} - {{ ClientName }}",
  "htmlContent": "<h1>Import Report for {{ ClientName }}</h1><p>Date: {{ ImportDate }}</p><p>Total Records: {{ TotalRecords }}</p>{% if ErrorCount > 0 %}<div style='color: red;'><strong>Errors:</strong> {{ ErrorCount }} records failed</div>{% else %}<div style='color: green;'>All records processed successfully!</div>{% endif %}<h2>Details:</h2><table>{% for item in Details %}<tr><td>{{ item.Type }}</td><td>{{ item.Count }}</td></tr>{% endfor %}</table>",
  "variables": {
    "ClientName": "Client name",
    "ImportDate": "Import date",
    "Status": "Success or Failed",
    "TotalRecords": "Total record count",
    "ErrorCount": "Number of errors",
    "Details": "Array of detail objects"
  },
  "testData": {
    "ClientName": "Acme Corp",
    "ImportDate": "2025-11-03",
    "Status": "Success",
    "TotalRecords": 1500,
    "ErrorCount": 0,
    "Details": [
      { "Type": "Employee", "Count": 1200 },
      { "Type": "Dependent", "Count": 300 }
    ]
  },
  "templateType": "notification"
}
```

**Preview with Test Data:**

```bash
POST /api/templates/preview
Content-Type: application/json

{
  "templateName": "import_report",
  "data": "{\"ClientName\":\"Acme Corp\",\"ImportDate\":\"2025-11-03\",\"Status\":\"Success\",\"TotalRecords\":1500,\"ErrorCount\":0,\"Details\":[{\"Type\":\"Employee\",\"Count\":1200},{\"Type\":\"Dependent\",\"Count\":300}]}"
}
```

---

### Example 3: Extract and Validate Template

**Extract Variables:**

```bash
POST /api/templates/extract-variables/import_report

Response:
{
  "templateName": "import_report",
  "variables": [
    "ClientName",
    "Details",
    "ErrorCount",
    "ImportDate",
    "Status",
    "TotalRecords"
  ],
  "variableCount": 6
}
```

**Validate Syntax:**

```bash
POST /api/templates/validate/import_report

Response:
{
  "templateName": "import_report",
  "isValid": true,
  "errors": [],
  "validatedAt": "2025-11-03T17:30:00Z"
}
```

---

### Example 4: List Templates by Type

**Get all workflow templates:**

```bash
GET /api/templates/type/workflow_triggered/all

Response:
{
  "count": 2,
  "templates": [
    {
      "id": 10,
      "name": "workflow_started",
      "subject": "Workflow Started: {{ WorkflowName }}",
      "templateType": "workflow_triggered",
      "isActive": true,
      ...
    },
    {
      "id": 11,
      "name": "workflow_completed",
      "subject": "Workflow Completed: {{ WorkflowName }}",
      "templateType": "workflow_triggered",
      "isActive": true,
      ...
    }
  ]
}
```

---

### Example 5: Update Template and Deactivate

**Update and deactivate:**

```bash
PUT /api/templates/5
Content-Type: application/json

{
  "description": "Updated description - deprecated",
  "isActive": false
}
```

---

## Error Handling

### Standard Error Responses

**400 Bad Request**

```json
{
  "message": "Template name is required"
}
```

**404 Not Found**

```json
{
  "message": "Template 'unknown_template' not found"
}
```

**409 Conflict**

```json
{
  "message": "Template with name 'duplicate_name' already exists"
}
```

### Best Practices

1. **Always validate templates** using `/api/templates/validate/{name}` before deploying
2. **Preview templates** with real data using `/api/templates/preview` before sending
3. **Extract variables** to understand data requirements
4. **Check health** before bulk operations
5. **Use descriptive names** like "daily_import_summary" instead of "template1"
6. **Define variables** in the template metadata for documentation
7. **Provide test data** to enable easy previewing
8. **Set template types** to categorize templates logically
9. **Track delivery IDs** from send responses for audit and troubleshooting
10. **Handle missing variables gracefully** - they render as empty strings

### Common Pitfalls

1. **Case sensitivity**: Variable names are case-sensitive (`{{ ClientName }}` vs `{{ clientname }}`)
2. **elsif vs elif**: Use `{% elsif %}` not `{% elif %}`
3. **Missing endfor/endif**: Always close control structures
4. **JSON escaping**: When passing templateData, ensure proper JSON escaping
5. **Inactive templates**: Deactivated templates won't appear in `/api/templates` (use `/all`)

---

## Related Documentation

- [Notifications Endpoints](./notifications-endpoints.md) - Notification management
- [Delivery Endpoints](./delivery-endpoints.md) - Delivery tracking
- [Email Service Configuration](../configuration/email-service.md) - Email provider setup
- [Template Syntax Guide](../guides/template-syntax.md) - Detailed syntax reference

---

**Last Updated:** 2025-12-18
**API Version:** v1
**Service:** NotificationService.Api
**Port:** 5201
