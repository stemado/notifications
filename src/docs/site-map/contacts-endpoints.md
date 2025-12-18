# Contacts Endpoints

## Overview

The Contacts API provides comprehensive contact management functionality for the notification routing system. Contacts represent individuals who can receive notifications, including external stakeholders like brokers, client HR contacts, and other non-system users. Each contact can be associated with multiple recipient groups to define their notification routing rules.

**Base URL:** `http://192.168.150.52:5201/api/routing/contacts`

**Key Features:**
- Full CRUD operations for contact management
- Soft deletion (deactivation) preserves historical data
- Search and filtering capabilities
- Pagination support for large datasets
- Group membership tracking

## Table of Contents

- [Endpoints](#endpoints)
  - [List Contacts](#list-contacts)
  - [Get Contact Details](#get-contact-details)
  - [Create Contact](#create-contact)
  - [Update Contact](#update-contact)
  - [Deactivate Contact](#deactivate-contact)
  - [Get Contact Groups](#get-contact-groups)
- [Data Models](#data-models)
- [Common Use Cases](#common-use-cases)
- [Error Handling](#error-handling)

---

## Endpoints

### List Contacts

Retrieve a paginated list of contacts with optional search and filtering.

**Endpoint:** `GET /api/routing/contacts`

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `search` | string | No | - | Search term to filter contacts by name, email, organization, or phone |
| `includeInactive` | boolean | No | `false` | Include deactivated contacts in results |
| `page` | integer | No | `1` | Page number for pagination (1-indexed) |
| `pageSize` | integer | No | `20` | Number of items per page |

**Response:** `200 OK`

```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "John Doe",
      "email": "john.doe@example.com",
      "phone": "+1-555-0100",
      "organization": "ABC Broker Agency",
      "isActive": true,
      "createdAt": "2024-01-15T10:30:00Z",
      "groupCount": 3
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

**Example Requests:**

```bash
# List all active contacts (first page)
curl http://192.168.150.52:5201/api/routing/contacts

# Search for contacts with "broker" in any field
curl "http://192.168.150.52:5201/api/routing/contacts?search=broker"

# Get page 2 with 50 items per page, including inactive contacts
curl "http://192.168.150.52:5201/api/routing/contacts?page=2&pageSize=50&includeInactive=true"
```

**Search Behavior:**
- The search parameter matches against: name, email, phone, and organization fields
- Search is case-insensitive
- Partial matches are supported (e.g., "john" matches "John Doe")
- By default, only active contacts are returned unless `includeInactive=true`

---

### Get Contact Details

Retrieve detailed information about a specific contact, including group memberships.

**Endpoint:** `GET /api/routing/contacts/{id}`

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | The unique identifier of the contact |

**Response:** `200 OK`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "John Doe",
  "email": "john.doe@example.com",
  "phone": "+1-555-0100",
  "organization": "ABC Broker Agency",
  "isActive": true,
  "userId": null,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-02-20T14:45:00Z",
  "deactivatedAt": null,
  "notes": "Primary broker contact for ABC Insurance",
  "groups": [
    {
      "groupId": "7d9f6f8e-1234-5678-9abc-def012345678",
      "groupName": "Broker Notifications",
      "clientId": "CLIENT001",
      "addedAt": "2024-01-15T10:35:00Z"
    },
    {
      "groupId": "8e0f7f9f-2345-6789-0bcd-ef0123456789",
      "groupName": "HR Team - ABC Corp",
      "clientId": null,
      "addedAt": "2024-01-20T09:15:00Z"
    }
  ]
}
```

**Response:** `404 Not Found`

```json
{
  "error": "Contact 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found"
}
```

**Example Request:**

```bash
curl http://192.168.150.52:5201/api/routing/contacts/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Field Descriptions:**

- **userId**: Links to an identity record if the contact has portal access. Typically `null` for external contacts.
- **deactivatedAt**: Timestamp when the contact was deactivated. `null` for active contacts.
- **notes**: Internal notes about the contact (not visible to the contact).
- **groups**: Array of recipient groups this contact belongs to, determining which notifications they receive.

---

### Create Contact

Create a new contact in the system.

**Endpoint:** `POST /api/routing/contacts`

**Request Body:**

```json
{
  "name": "Jane Smith",
  "email": "jane.smith@example.com",
  "phone": "+1-555-0200",
  "organization": "XYZ Insurance Brokers",
  "notes": "Secondary contact for commercial accounts"
}
```

**Required Fields:**
- `name` (string): Display name for the contact
- `email` (string): Primary email address for notifications

**Optional Fields:**
- `phone` (string): Phone number for SMS notifications
- `organization` (string): Company or organization name
- `notes` (string): Internal notes about the contact

**Response:** `201 Created`

```json
{
  "id": "9f1e2d3c-4b5a-6789-0abc-def123456789",
  "name": "Jane Smith",
  "email": "jane.smith@example.com",
  "phone": "+1-555-0200",
  "organization": "XYZ Insurance Brokers",
  "isActive": true,
  "userId": null,
  "createdAt": "2024-03-10T11:20:00Z",
  "updatedAt": "2024-03-10T11:20:00Z",
  "deactivatedAt": null,
  "notes": "Secondary contact for commercial accounts",
  "groups": []
}
```

**Response Headers:**

```
Location: /api/routing/contacts/9f1e2d3c-4b5a-6789-0abc-def123456789
```

**Example Request:**

```bash
curl -X POST http://192.168.150.52:5201/api/routing/contacts \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Jane Smith",
    "email": "jane.smith@example.com",
    "phone": "+1-555-0200",
    "organization": "XYZ Insurance Brokers",
    "notes": "Secondary contact for commercial accounts"
  }'
```

**Validation Rules:**
- Email must be in valid format
- Name cannot be empty
- Duplicate emails are allowed (the same person might have multiple contact records for different contexts)

**Default Values:**
- `isActive`: `true`
- `groups`: empty array (group memberships must be added separately)
- `userId`: `null`

---

### Update Contact

Update an existing contact's information.

**Endpoint:** `PUT /api/routing/contacts/{id}`

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | The unique identifier of the contact |

**Request Body:**

```json
{
  "name": "Jane Smith-Johnson",
  "email": "jane.smith@newcompany.com",
  "phone": "+1-555-0299",
  "organization": "New Insurance Co",
  "notes": "Updated contact info after merger"
}
```

**All Fields Required:**
- `name` (string): Display name for the contact
- `email` (string): Primary email address
- `phone` (string): Phone number (use `null` to clear)
- `organization` (string): Organization name (use `null` to clear)
- `notes` (string): Internal notes (use `null` to clear)

**Response:** `200 OK`

```json
{
  "id": "9f1e2d3c-4b5a-6789-0abc-def123456789",
  "name": "Jane Smith-Johnson",
  "email": "jane.smith@newcompany.com",
  "phone": "+1-555-0299",
  "organization": "New Insurance Co",
  "isActive": true,
  "userId": null,
  "createdAt": "2024-03-10T11:20:00Z",
  "updatedAt": "2024-03-15T09:45:00Z",
  "deactivatedAt": null,
  "notes": "Updated contact info after merger",
  "groups": [
    {
      "groupId": "7d9f6f8e-1234-5678-9abc-def012345678",
      "groupName": "Broker Notifications",
      "clientId": "CLIENT001",
      "addedAt": "2024-03-10T11:25:00Z"
    }
  ]
}
```

**Response:** `404 Not Found`

```json
{
  "error": "Contact 9f1e2d3c-4b5a-6789-0abc-def123456789 not found"
}
```

**Example Request:**

```bash
curl -X PUT http://192.168.150.52:5201/api/routing/contacts/9f1e2d3c-4b5a-6789-0abc-def123456789 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Jane Smith-Johnson",
    "email": "jane.smith@newcompany.com",
    "phone": "+1-555-0299",
    "organization": "New Insurance Co",
    "notes": "Updated contact info after merger"
  }'
```

**Important Notes:**
- This is a full update (PUT) - all fields must be provided
- To clear optional fields, pass `null` explicitly
- The `updatedAt` timestamp is automatically updated
- Group memberships are not affected by contact updates
- Cannot modify `isActive` status through this endpoint (use deactivate endpoint instead)

---

### Deactivate Contact

Soft delete a contact by marking it as inactive. This preserves the contact's historical data and associations while preventing them from receiving new notifications.

**Endpoint:** `DELETE /api/routing/contacts/{id}`

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | The unique identifier of the contact |

**Response:** `204 No Content`

(Empty response body on success)

**Response:** `404 Not Found`

```json
{
  "error": "Contact 9f1e2d3c-4b5a-6789-0abc-def123456789 not found"
}
```

**Example Request:**

```bash
curl -X DELETE http://192.168.150.52:5201/api/routing/contacts/9f1e2d3c-4b5a-6789-0abc-def123456789
```

**Behavior:**
- Sets `isActive` to `false`
- Sets `deactivatedAt` to current timestamp
- Updates `updatedAt` timestamp
- Preserves all contact data and group memberships
- Does not remove the contact from recipient groups
- Contact can be reactivated through database operations if needed

**Use Cases:**
- Contact left their organization
- Contact no longer needs to receive notifications
- Temporary suspension of notifications
- Replacing a contact with a new person

**Important Notes:**
- This is a soft delete - no data is permanently removed
- Deactivated contacts are excluded from notifications
- Historical notification records remain intact
- To completely remove a contact, database-level operations are required
- Use `includeInactive=true` when listing contacts to see deactivated contacts

---

### Get Contact Groups

Retrieve all recipient groups that a contact belongs to.

**Endpoint:** `GET /api/routing/contacts/{id}/groups`

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | The unique identifier of the contact |

**Response:** `200 OK`

```json
[
  {
    "id": "7d9f6f8e-1234-5678-9abc-def012345678",
    "name": "Broker Notifications",
    "clientId": "CLIENT001",
    "description": "All broker contacts for CLIENT001",
    "purpose": "Notification",
    "tags": ["broker", "external"],
    "isActive": true,
    "memberCount": 12,
    "policyCount": 3
  },
  {
    "id": "8e0f7f9f-2345-6789-0bcd-ef0123456789",
    "name": "HR Team - ABC Corp",
    "clientId": null,
    "description": "Internal HR team contacts",
    "purpose": "Alert",
    "tags": ["hr", "internal"],
    "isActive": true,
    "memberCount": 8,
    "policyCount": 5
  }
]
```

**Response:** `404 Not Found`

```json
{
  "error": "Contact 9f1e2d3c-4b5a-6789-0abc-def123456789 not found"
}
```

**Example Request:**

```bash
curl http://192.168.150.52:5201/api/routing/contacts/9f1e2d3c-4b5a-6789-0abc-def123456789/groups
```

**Response Fields:**

- **id**: Unique identifier for the recipient group
- **name**: Display name of the group
- **clientId**: Optional client identifier if group is client-specific
- **description**: Purpose and details of the group
- **purpose**: Group purpose enum (Notification, Alert, Report, Test)
- **tags**: Array of tags for categorization and filtering
- **isActive**: Whether the group is active
- **memberCount**: Total number of contacts in the group
- **policyCount**: Number of routing policies using this group

**Use Cases:**
- View all notification types a contact will receive
- Audit contact's group memberships
- Identify which policies affect a specific contact
- Troubleshoot notification routing issues

---

## Data Models

### ContactSummary

Lightweight contact representation for list views.

```typescript
{
  id: string;              // GUID
  name: string;            // Display name
  email: string;           // Primary email address
  phone?: string;          // Optional phone number
  organization?: string;   // Optional organization name
  isActive: boolean;       // Active status
  createdAt: string;       // ISO 8601 timestamp
  groupCount: number;      // Number of groups contact belongs to
}
```

### ContactDetails

Complete contact information including group memberships.

```typescript
{
  id: string;              // GUID
  name: string;            // Display name
  email: string;           // Primary email address
  phone?: string;          // Optional phone number
  organization?: string;   // Optional organization name
  isActive: boolean;       // Active status
  userId?: string;         // Optional link to identity (GUID)
  createdAt: string;       // ISO 8601 timestamp
  updatedAt: string;       // ISO 8601 timestamp
  deactivatedAt?: string;  // ISO 8601 timestamp or null
  notes?: string;          // Internal notes
  groups: GroupMembershipInfo[];  // Array of group memberships
}
```

### CreateContactRequest

Required data for creating a new contact.

```typescript
{
  name: string;            // Required - Display name
  email: string;           // Required - Primary email address
  phone?: string;          // Optional - Phone number
  organization?: string;   // Optional - Organization name
  notes?: string;          // Optional - Internal notes
}
```

### UpdateContactRequest

Required data for updating a contact.

```typescript
{
  name: string;            // Required - Display name
  email: string;           // Required - Primary email address
  phone?: string;          // Optional - Phone number (null to clear)
  organization?: string;   // Optional - Organization name (null to clear)
  notes?: string;          // Optional - Internal notes (null to clear)
}
```

### GroupMembershipInfo

Information about a contact's membership in a recipient group.

```typescript
{
  groupId: string;         // GUID
  groupName: string;       // Display name of the group
  clientId?: string;       // Optional client identifier
  addedAt: string;         // ISO 8601 timestamp when added to group
}
```

### RecipientGroupSummary

Summary information about a recipient group.

```typescript
{
  id: string;              // GUID
  name: string;            // Display name
  clientId?: string;       // Optional client identifier
  description?: string;    // Optional description
  purpose: string;         // Group purpose enum
  tags: string[];          // Array of tags
  isActive: boolean;       // Active status
  memberCount: number;     // Number of contacts in group
  policyCount: number;     // Number of policies using this group
}
```

### PaginatedResponse<T>

Wrapper for paginated list responses.

```typescript
{
  data: T[];               // Array of items for current page
  page: number;            // Current page number (1-indexed)
  pageSize: number;        // Number of items per page
  totalItems: number;      // Total number of items across all pages
  totalPages: number;      // Total number of pages
  hasNext: boolean;        // Whether there is a next page
  hasPrevious: boolean;    // Whether there is a previous page
}
```

---

## Common Use Cases

### Creating a New Broker Contact

```bash
# Step 1: Create the contact
curl -X POST http://192.168.150.52:5201/api/routing/contacts \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Michael Johnson",
    "email": "michael.johnson@brokerco.com",
    "phone": "+1-555-0300",
    "organization": "BrokerCo Insurance",
    "notes": "Primary contact for group health policies"
  }'

# Response includes the new contact ID
# {
#   "id": "abc123...",
#   ...
# }

# Step 2: Add contact to appropriate groups (see Groups API)
# This determines which notifications they receive
```

### Searching for Contacts

```bash
# Find all contacts from a specific organization
curl "http://192.168.150.52:5201/api/routing/contacts?search=BrokerCo"

# Find a contact by email (partial match works)
curl "http://192.168.150.52:5201/api/routing/contacts?search=michael.johnson"

# Find contacts by phone number
curl "http://192.168.150.52:5201/api/routing/contacts?search=555-0300"
```

### Updating Contact Information

```bash
# Update email address and organization after a merger
curl -X PUT http://192.168.150.52:5201/api/routing/contacts/abc123... \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Michael Johnson",
    "email": "michael.johnson@newbroker.com",
    "phone": "+1-555-0300",
    "organization": "NewBroker Insurance Group",
    "notes": "Email updated after company merger"
  }'
```

### Auditing Contact Group Memberships

```bash
# Get all groups a contact belongs to
curl http://192.168.150.52:5201/api/routing/contacts/abc123.../groups

# Use this to:
# - Verify notification routing
# - Audit access levels
# - Troubleshoot missing notifications
# - Review group assignments
```

### Deactivating a Contact

```bash
# When someone leaves their organization
curl -X DELETE http://192.168.150.52:5201/api/routing/contacts/abc123...

# This will:
# - Mark contact as inactive
# - Stop future notifications
# - Preserve historical data
# - Keep group memberships intact
```

### Viewing Inactive Contacts

```bash
# Include inactive contacts in search results
curl "http://192.168.150.52:5201/api/routing/contacts?includeInactive=true"

# Useful for:
# - Finding old contacts for reactivation
# - Auditing historical relationships
# - Reviewing deactivation dates
```

### Pagination for Large Contact Lists

```bash
# Get first page (default 20 items)
curl http://192.168.150.52:5201/api/routing/contacts

# Get larger page size for fewer requests
curl "http://192.168.150.52:5201/api/routing/contacts?pageSize=100"

# Navigate to specific page
curl "http://192.168.150.52:5201/api/routing/contacts?page=3&pageSize=50"

# Check pagination metadata in response:
# - hasNext: true if more pages available
# - hasPrevious: true if can go back
# - totalPages: total number of pages
```

---

## Error Handling

### Common HTTP Status Codes

| Status Code | Meaning | Common Causes |
|-------------|---------|---------------|
| `200 OK` | Request successful | GET and PUT operations completed |
| `201 Created` | Resource created | POST operation successful |
| `204 No Content` | Success with no body | DELETE operation successful |
| `400 Bad Request` | Invalid request | Missing required fields, invalid JSON |
| `404 Not Found` | Resource not found | Invalid contact ID, contact doesn't exist |
| `500 Internal Server Error` | Server error | Database issues, unexpected exceptions |

### Error Response Format

Error responses include a descriptive message:

```json
{
  "error": "Contact 9f1e2d3c-4b5a-6789-0abc-def123456789 not found"
}
```

### Validation Errors

**Missing Required Fields:**

```bash
# Request with missing email field
curl -X POST http://192.168.150.52:5201/api/routing/contacts \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe"
  }'

# Response: 400 Bad Request
{
  "error": "Email is required"
}
```

**Invalid Email Format:**

```bash
# Request with invalid email
curl -X POST http://192.168.150.52:5201/api/routing/contacts \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe",
    "email": "not-an-email"
  }'

# Response: 400 Bad Request
{
  "error": "Invalid email format"
}
```

### Not Found Errors

```bash
# Attempting to get a non-existent contact
curl http://192.168.150.52:5201/api/routing/contacts/00000000-0000-0000-0000-000000000000

# Response: 404 Not Found
{
  "error": "Contact 00000000-0000-0000-0000-000000000000 not found"
}
```

### Troubleshooting Common Issues

**Contact Not Receiving Notifications:**
1. Verify contact is active: `isActive: true`
2. Check group memberships: `GET /api/routing/contacts/{id}/groups`
3. Verify groups are active
4. Check routing policies for those groups

**Search Not Finding Contact:**
1. Verify search term spelling
2. Try partial matches (search is flexible)
3. Check if contact is inactive (add `includeInactive=true`)
4. Verify contact exists: list all contacts without search

**Update Not Reflected:**
1. Check `updatedAt` timestamp in response
2. Verify correct contact ID is being used
3. Ensure all required fields are provided in PUT request
4. Check for validation errors in response

**Cannot Deactivate Contact:**
1. Verify contact ID is correct
2. Check if contact is already inactive
3. Ensure endpoint URL is correct (DELETE method)

---

## Best Practices

### Contact Management

1. **Use Descriptive Names**: Include full names and titles when relevant
2. **Keep Organization Current**: Update organization field when contacts change companies
3. **Use Notes Field**: Document important context about the contact relationship
4. **Validate Emails**: Ensure email addresses are valid before creating contacts
5. **Soft Delete Only**: Use deactivate endpoint instead of hard deletes

### Search and Filtering

1. **Use Pagination**: Set appropriate page sizes for your use case
2. **Include Inactive When Auditing**: Add `includeInactive=true` for complete history
3. **Search Broadly**: Start with general terms, narrow down as needed
4. **Cache Results**: List endpoints are efficient, but cache when appropriate

### Group Management

1. **Check Group Memberships**: Use the groups endpoint to verify routing
2. **Document Changes**: Update notes field when modifying group memberships
3. **Audit Regularly**: Review contact group assignments periodically
4. **Test Routing**: Use test groups before adding contacts to production groups

### API Integration

1. **Handle 404s Gracefully**: Contact may have been deleted by another process
2. **Check Response Codes**: Don't assume success without checking status
3. **Store Contact IDs**: Use returned ID from create operations
4. **Use Location Header**: Follow location header after POST for created resource
5. **Implement Retry Logic**: Handle transient failures appropriately

---

## Related Documentation

- [Groups Endpoints](./groups-endpoints.md) - Manage recipient groups and memberships
- [Routing Policies API](./routing-policies-endpoints.md) - Configure notification routing rules
- [Notification Service Overview](../README.md) - Service architecture and concepts

---

## Support

For issues or questions about the Contacts API:
- Review the error response message for guidance
- Check related documentation for group and policy management
- Verify contact data using the detail endpoint
- Use the search endpoint to locate contacts by various fields
