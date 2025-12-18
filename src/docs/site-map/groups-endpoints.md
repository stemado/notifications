# Groups Endpoints

API endpoints for managing recipient groups in the NotificationService routing system.

## Table of Contents

- [Overview](#overview)
- [Base URL](#base-url)
- [Endpoints](#endpoints)
  - [List Groups](#list-groups)
  - [Get Group Details](#get-group-details)
  - [Create Group](#create-group)
  - [Update Group](#update-group)
  - [Add Member to Group](#add-member-to-group)
  - [Remove Member from Group](#remove-member-from-group)
  - [Get Group Members](#get-group-members)
- [Data Models](#data-models)
- [Common Patterns](#common-patterns)
- [Error Handling](#error-handling)
- [Examples](#examples)

## Overview

Recipient groups are named collections of contacts used for routing notifications. Groups can be client-specific or global, and they serve as the target for routing policies.

**Key Features:**
- Client-specific or global groups
- Group purpose designation (Production, TestOnly, Both)
- Tag-based organization
- Member management
- Active/inactive status
- Policy usage tracking

## Base URL

```
/api/routing/groups
```

## Endpoints

### List Groups

Retrieve a paginated list of recipient groups with summary information.

**Endpoint:** `GET /api/routing/groups`

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `clientId` | string | No | null | Filter groups by client ID. Omit to get all groups. |
| `includeInactive` | boolean | No | false | Include inactive groups in results. |
| `page` | integer | No | 1 | Page number (1-based). |
| `pageSize` | integer | No | 20 | Number of items per page. |

**Response:** `200 OK`

```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "HenryCounty-BrokerTeam",
      "clientId": "henry-county",
      "description": "Broker team for Henry County",
      "purpose": "Production",
      "tags": ["client", "broker"],
      "isActive": true,
      "memberCount": 5,
      "policyCount": 3
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1,
  "hasNext": false,
  "hasPrevious": false
}
```

**Use Cases:**
- List all groups for administration
- Filter groups by client for client-specific configuration
- Browse groups with pagination for UI display
- Check member and policy counts before deletion

---

### Get Group Details

Retrieve detailed information about a specific recipient group, including members and policies.

**Endpoint:** `GET /api/routing/groups/{id}`

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | Unique identifier of the recipient group. |

**Response:** `200 OK`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "HenryCounty-BrokerTeam",
  "clientId": "henry-county",
  "description": "Broker team for Henry County",
  "purpose": "Production",
  "tags": ["client", "broker"],
  "isActive": true,
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2025-01-16T14:20:00Z",
  "members": [
    {
      "contactId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
      "name": "John Doe",
      "email": "john.doe@example.com",
      "organization": "Acme Insurance",
      "isActive": true,
      "addedAt": "2025-01-15T10:35:00Z",
      "addedBy": "admin@plansource.com"
    }
  ],
  "policies": [
    {
      "id": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
      "service": "CensusReconciliation",
      "topic": "ImportCompleted",
      "channel": "Email",
      "role": "To",
      "isEnabled": true
    }
  ]
}
```

**Response:** `404 Not Found`

```json
{
  "message": "Recipient group 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found"
}
```

**Use Cases:**
- View complete group information
- See all members with their contact details
- Check which policies use this group
- Audit group membership history

---

### Create Group

Create a new recipient group.

**Endpoint:** `POST /api/routing/groups`

**Request Body:**

```json
{
  "name": "HenryCounty-BrokerTeam",
  "clientId": "henry-county",
  "description": "Broker team for Henry County",
  "purpose": "Production",
  "tags": ["client", "broker"]
}
```

**Request Schema:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `name` | string | Yes | - | Unique name for the group. |
| `clientId` | string | No | null | Client ID (null for global groups). |
| `description` | string | No | null | Description of the group's purpose. |
| `purpose` | enum | No | Production | Group purpose: `Production`, `TestOnly`, or `Both`. |
| `tags` | array | No | [] | List of tags for categorization. |

**Response:** `201 Created`

Returns the created group details with `Location` header pointing to the new resource.

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "HenryCounty-BrokerTeam",
  "clientId": "henry-county",
  "description": "Broker team for Henry County",
  "purpose": "Production",
  "tags": ["client", "broker"],
  "isActive": true,
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2025-01-15T10:30:00Z",
  "members": [],
  "policies": []
}
```

**Use Cases:**
- Set up new client notification groups
- Create test groups for QA testing
- Organize contacts by department or team
- Establish global notification groups

**Notes:**
- New groups are created with `isActive = true` by default
- Groups start with no members or policies
- Tags can be added during creation for organization

---

### Update Group

Update an existing recipient group's properties.

**Endpoint:** `PUT /api/routing/groups/{id}`

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | Unique identifier of the recipient group. |

**Request Body:**

All fields are optional. Only provide fields you want to update.

```json
{
  "name": "HenryCounty-BrokerTeam-Updated",
  "description": "Updated broker team description",
  "isActive": true,
  "purpose": "Both",
  "tags": ["client", "broker", "production"]
}
```

**Request Schema:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | No | New name for the group (must not be empty). |
| `description` | string | No | New description (null allowed). |
| `isActive` | boolean | No | Active status (null means no change). |
| `purpose` | enum | No | New purpose: `Production`, `TestOnly`, or `Both`. |
| `tags` | array | No | Complete replacement of tags list. |

**Response:** `200 OK`

Returns the updated group details including members and policies.

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "HenryCounty-BrokerTeam-Updated",
  "clientId": "henry-county",
  "description": "Updated broker team description",
  "purpose": "Both",
  "tags": ["client", "broker", "production"],
  "isActive": true,
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2025-01-16T14:20:00Z",
  "members": [...],
  "policies": [...]
}
```

**Response:** `404 Not Found`

```json
{
  "message": "Recipient group 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found"
}
```

**Use Cases:**
- Rename groups for clarity
- Deactivate groups without deleting
- Change group purpose (e.g., from TestOnly to Production)
- Update tags for better organization

**Notes:**
- Partial updates are supported (only send fields to change)
- `isActive = null` means don't change the current status
- `clientId` cannot be changed after creation
- `updatedAt` timestamp is automatically updated

---

### Add Member to Group

Add a contact to a recipient group.

**Endpoint:** `POST /api/routing/groups/{id}/members`

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | Unique identifier of the recipient group. |

**Request Body:**

```json
{
  "contactId": "4fa85f64-5717-4562-b3fc-2c963f66afa7"
}
```

**Request Schema:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `contactId` | GUID | Yes | ID of the contact to add to the group. |

**Response:** `200 OK`

Empty response body on success.

**Response:** `404 Not Found`

```json
{
  "message": "Recipient group 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found"
}
```

**Use Cases:**
- Add new team members to notification groups
- Build routing groups for specific projects
- Add contacts discovered during client setup
- Expand test groups for QA purposes

**Notes:**
- The authenticated user's name is recorded as `addedBy` (defaults to "api" if not authenticated)
- Adding a contact that's already a member is typically idempotent
- The contact must exist before adding to a group
- `addedAt` timestamp is recorded automatically

---

### Remove Member from Group

Remove a contact from a recipient group.

**Endpoint:** `DELETE /api/routing/groups/{id}/members/{contactId}`

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | Unique identifier of the recipient group. |
| `contactId` | GUID | Yes | ID of the contact to remove from the group. |

**Response:** `204 No Content`

Empty response body on success.

**Response:** `404 Not Found`

```json
{
  "message": "Recipient group 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found"
}
```

**Use Cases:**
- Remove team members who have left
- Clean up test groups after QA testing
- Adjust notification recipients based on role changes
- Maintain accurate group membership

**Notes:**
- Removing a contact that's not in the group is typically safe (no error)
- The membership record is deleted, not just marked inactive
- Removal doesn't affect the contact record itself
- Active routing policies using this group will no longer include the removed contact

---

### Get Group Members

Retrieve a list of all contacts who are members of a specific group.

**Endpoint:** `GET /api/routing/groups/{id}/members`

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | Unique identifier of the recipient group. |

**Response:** `200 OK`

```json
[
  {
    "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    "name": "John Doe",
    "email": "john.doe@example.com",
    "phone": "+1-555-0100",
    "organization": "Acme Insurance",
    "isActive": true,
    "createdAt": "2025-01-10T09:00:00Z",
    "groupCount": 3
  }
]
```

**Response:** `404 Not Found`

```json
{
  "message": "Recipient group 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found"
}
```

**Use Cases:**
- View all members in a group for audit purposes
- Export member lists for reporting
- Verify group membership before sending notifications
- Check contact details within a group context

**Notes:**
- Returns contact summary information (not full details)
- `groupCount` shows how many groups each contact belongs to
- Inactive contacts are included (check `isActive` field)
- Results are not paginated (returns all members)

---

## Data Models

### RecipientGroupSummary

Summary view used in list operations.

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Unique identifier |
| `name` | string | Group name |
| `clientId` | string | Client ID (null for global) |
| `description` | string | Description |
| `purpose` | GroupPurpose | Production, TestOnly, or Both |
| `tags` | array | List of tags |
| `isActive` | boolean | Active status |
| `memberCount` | integer | Number of members |
| `policyCount` | integer | Number of policies using this group |

### RecipientGroupDetails

Detailed view with members and policies.

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Unique identifier |
| `name` | string | Group name |
| `clientId` | string | Client ID (null for global) |
| `description` | string | Description |
| `purpose` | GroupPurpose | Production, TestOnly, or Both |
| `tags` | array | List of tags |
| `isActive` | boolean | Active status |
| `createdAt` | datetime | Creation timestamp (ISO 8601) |
| `updatedAt` | datetime | Last update timestamp (ISO 8601) |
| `members` | array | List of GroupMemberInfo objects |
| `policies` | array | List of PolicySummaryForGroup objects |

### GroupMemberInfo

Member information within a group.

| Field | Type | Description |
|-------|------|-------------|
| `contactId` | GUID | Contact identifier |
| `name` | string | Contact name |
| `email` | string | Contact email |
| `organization` | string | Organization name |
| `isActive` | boolean | Contact active status |
| `addedAt` | datetime | When contact was added to group |
| `addedBy` | string | Who added the contact |

### PolicySummaryForGroup

Policy information shown in group details.

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Policy identifier |
| `service` | string | Source service (e.g., "CensusReconciliation") |
| `topic` | string | Notification topic (e.g., "ImportCompleted") |
| `channel` | string | Delivery channel (e.g., "Email") |
| `role` | string | Recipient role (e.g., "To", "Cc", "Bcc") |
| `isEnabled` | boolean | Policy enabled status |

### ContactSummary

Contact summary used in member lists.

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Contact identifier |
| `name` | string | Contact name |
| `email` | string | Contact email |
| `phone` | string | Contact phone |
| `organization` | string | Organization name |
| `isActive` | boolean | Active status |
| `createdAt` | datetime | Creation timestamp |
| `groupCount` | integer | Number of groups contact belongs to |

### PaginatedResponse

Generic pagination wrapper.

| Field | Type | Description |
|-------|------|-------------|
| `data` | array | List of items |
| `page` | integer | Current page number (1-based) |
| `pageSize` | integer | Items per page |
| `totalItems` | integer | Total number of items |
| `totalPages` | integer | Total number of pages |
| `hasNext` | boolean | Whether there's a next page |
| `hasPrevious` | boolean | Whether there's a previous page |

### GroupPurpose Enum

| Value | Description |
|-------|-------------|
| `Production` | Group is used for production notification routing only |
| `TestOnly` | Group is designated for test email sends only |
| `Both` | Group can be used for both production routing and test emails |

---

## Common Patterns

### Client-Specific vs Global Groups

**Client-Specific Groups:**
- Have a `clientId` value
- Only visible/usable for that specific client
- Naming convention: `{ClientId}-{Purpose}` (e.g., "HenryCounty-BrokerTeam")

**Global Groups:**
- Have `clientId = null`
- Available for use by any client
- Naming convention: `Internal-{Purpose}` (e.g., "Internal-CensusOps")

### Group Purpose Usage

**Production Groups:**
```json
{
  "purpose": "Production"
}
```
- Used for live notification routing
- Associated with routing policies
- Cannot be used for test email sends

**TestOnly Groups:**
```json
{
  "purpose": "TestOnly"
}
```
- Used exclusively for test email sends
- Cannot be used in routing policies
- Ideal for QA and development teams

**Both Groups:**
```json
{
  "purpose": "Both"
}
```
- Can be used for both production and testing
- Maximum flexibility
- Useful for internal teams that need both capabilities

### Tag Organization

Use tags to categorize and filter groups:

```json
{
  "tags": ["client", "broker", "production"]
}
```

**Common Tag Patterns:**
- `client` - Client-facing groups
- `internal` - Internal team groups
- `qa` - QA and testing groups
- `production` - Production-ready groups
- `deprecated` - Groups marked for removal

### Pagination Navigation

**First Page:**
```
GET /api/routing/groups?page=1&pageSize=20
```

**Next Page:**
```
GET /api/routing/groups?page=2&pageSize=20
```

Check `hasNext` and `hasPrevious` in response to determine navigation options.

### Filtering by Client

**Get client-specific groups:**
```
GET /api/routing/groups?clientId=henry-county
```

**Get all groups (including global):**
```
GET /api/routing/groups
```

Note: When filtering by `clientId`, only groups for that client are returned (global groups with `clientId = null` are excluded).

---

## Error Handling

### Common HTTP Status Codes

| Status | Description |
|--------|-------------|
| 200 OK | Successful GET, PUT request |
| 201 Created | Successful POST (group created) |
| 204 No Content | Successful DELETE request |
| 400 Bad Request | Invalid request body or parameters |
| 404 Not Found | Group or contact not found |
| 500 Internal Server Error | Server error |

### Error Response Format

```json
{
  "message": "Recipient group 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found"
}
```

### Handling 404 Errors

When a group is not found:
- Verify the group ID is correct (valid GUID format)
- Check if the group has been deleted
- Ensure you have access to the client's groups

### Validation Errors

**Missing Required Fields:**
```json
{
  "errors": {
    "Name": ["The Name field is required."]
  }
}
```

**Invalid GUID Format:**
```json
{
  "message": "The value 'invalid-id' is not valid."
}
```

---

## Examples

### Example 1: Create and Populate a Group

**Step 1: Create the group**
```bash
POST /api/routing/groups
Content-Type: application/json

{
  "name": "HenryCounty-BrokerTeam",
  "clientId": "henry-county",
  "description": "Broker team for Henry County Schools",
  "purpose": "Production",
  "tags": ["client", "broker"]
}
```

**Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "HenryCounty-BrokerTeam",
  ...
}
```

**Step 2: Add members to the group**
```bash
POST /api/routing/groups/3fa85f64-5717-4562-b3fc-2c963f66afa6/members
Content-Type: application/json

{
  "contactId": "4fa85f64-5717-4562-b3fc-2c963f66afa7"
}
```

**Step 3: Verify members**
```bash
GET /api/routing/groups/3fa85f64-5717-4562-b3fc-2c963f66afa6/members
```

---

### Example 2: Find Groups and Check Usage

**Step 1: List all groups for a client**
```bash
GET /api/routing/groups?clientId=henry-county&page=1&pageSize=20
```

**Step 2: Get detailed information**
```bash
GET /api/routing/groups/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

Check the `policyCount` field to see if the group is in use:
```json
{
  "policyCount": 3,
  "policies": [
    {
      "service": "CensusReconciliation",
      "topic": "ImportCompleted",
      ...
    }
  ]
}
```

---

### Example 3: Update Group for Testing

**Convert a production group to support testing:**
```bash
PUT /api/routing/groups/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json

{
  "purpose": "Both",
  "tags": ["client", "broker", "testing"]
}
```

This allows the group to be used for both production routing and test emails.

---

### Example 4: Deactivate a Group

**Instead of deleting, deactivate the group:**
```bash
PUT /api/routing/groups/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json

{
  "isActive": false
}
```

**Benefits:**
- Preserves historical data
- Can be reactivated later
- Maintains referential integrity with policies

---

### Example 5: Paginated Group Browsing

**Get first page:**
```bash
GET /api/routing/groups?page=1&pageSize=10
```

**Response indicates more pages:**
```json
{
  "data": [...],
  "page": 1,
  "pageSize": 10,
  "totalItems": 35,
  "totalPages": 4,
  "hasNext": true,
  "hasPrevious": false
}
```

**Navigate to next page:**
```bash
GET /api/routing/groups?page=2&pageSize=10
```

---

### Example 6: Clean Up Group Members

**Remove multiple members from a group:**
```bash
# Remove first member
DELETE /api/routing/groups/3fa85f64-5717-4562-b3fc-2c963f66afa6/members/4fa85f64-5717-4562-b3fc-2c963f66afa7

# Remove second member
DELETE /api/routing/groups/3fa85f64-5717-4562-b3fc-2c963f66afa6/members/5fa85f64-5717-4562-b3fc-2c963f66afa8
```

Each request returns `204 No Content` on success.

---

### Example 7: Global Group for Internal Notifications

**Create a global group (no clientId):**
```bash
POST /api/routing/groups
Content-Type: application/json

{
  "name": "Internal-CensusOps",
  "description": "Census operations team (all clients)",
  "purpose": "Production",
  "tags": ["internal", "operations"]
}
```

Note: `clientId` is omitted, making this group available for any client's routing policies.

---

## Related Endpoints

- [Contacts Endpoints](./contacts-endpoints.md) - Manage individual contacts
- [Policies Endpoints](./policies-endpoints.md) - Configure routing policies that use groups
- [Test Email Endpoints](./test-email-endpoints.md) - Send test emails to groups

---

## Best Practices

### Group Naming Conventions

**Client-Specific:**
- Format: `{ClientId}-{Purpose}`
- Example: `HenryCounty-BrokerTeam`, `DadeCounty-PayrollTeam`

**Global/Internal:**
- Format: `Internal-{Purpose}`
- Example: `Internal-CensusOps`, `Internal-DevTeam`

### Group Organization

1. **Use Purpose Appropriately**
   - Set `Production` for live routing groups
   - Set `TestOnly` for QA and development groups
   - Set `Both` only when truly needed

2. **Tag Strategically**
   - Use consistent tag vocabularies
   - Tag for filtering and discovery
   - Consider: department, role, environment, client type

3. **Maintain Active Status**
   - Deactivate unused groups instead of deleting
   - Regularly audit inactive groups
   - Document reasons for deactivation

### Member Management

1. **Audit Regularly**
   - Review group members periodically
   - Remove contacts who have left organizations
   - Verify contact information is current

2. **Document Changes**
   - Use descriptive commit messages
   - Track who added/removed members (via `addedBy`)
   - Monitor group member changes through audit logs

3. **Prevent Duplication**
   - Check if contact is already a member before adding
   - Use the GET members endpoint to verify

### Performance Considerations

1. **Pagination**
   - Use reasonable page sizes (20-50 items)
   - Avoid requesting all groups in a single request

2. **Filtering**
   - Filter by `clientId` when working with specific clients
   - Use `includeInactive=false` to reduce result sets

3. **Caching**
   - Consider caching group lists for UI display
   - Invalidate cache after create/update operations

---

## Troubleshooting

### Issue: Group not appearing in list

**Possible Causes:**
1. Group is inactive and `includeInactive=false`
2. Filtering by wrong `clientId`
3. Group belongs to different client

**Solution:**
```bash
# Check with inactive included
GET /api/routing/groups?includeInactive=true

# Check without client filter
GET /api/routing/groups
```

### Issue: Cannot add member to group

**Possible Causes:**
1. Group ID is incorrect
2. Contact ID doesn't exist
3. Contact is already a member

**Solution:**
1. Verify group exists: `GET /api/routing/groups/{id}`
2. Verify contact exists: `GET /api/routing/contacts/{contactId}`
3. Check current members: `GET /api/routing/groups/{id}/members`

### Issue: Update not reflecting changes

**Possible Causes:**
1. Only null fields were sent (no-op)
2. Validation error occurred
3. Caching issue in client

**Solution:**
```bash
# Send explicit values, not nulls
PUT /api/routing/groups/{id}
{
  "name": "NewName",
  "isActive": true
}

# Verify with fresh GET request
GET /api/routing/groups/{id}
```

### Issue: 404 error on valid group ID

**Possible Causes:**
1. Group was deleted
2. GUID format is incorrect
3. ID was copied incorrectly

**Solution:**
1. Verify GUID format: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
2. Check if group exists in list: `GET /api/routing/groups`
3. Ensure no extra whitespace in ID

---

## Additional Resources

- [NotificationService API Overview](./README.md)
- [Routing System Architecture](../architecture/routing-system.md)
- [Authentication & Authorization](../guides/authentication.md)
- [API Rate Limits & Quotas](../guides/rate-limits.md)

---

**Last Updated:** 2025-12-18
**API Version:** v1
**Base URL:** `/api/routing/groups`
