## Claude Code Prompt: NotificationService.Routing Bounded Context

### Context

We're adding a new bounded context to the existing NotificationService solution to handle **outbound notification routing** to external recipients (brokers, clients, etc.) who don't have user accounts in our system.

**The existing NotificationService is user-centric:**
- Notifications are always tied to a `UserId`
- User preferences control channel delivery
- Subscriptions filter what notifications a user sees

**The new Routing context is recipient-centric:**
- Routes notifications to external contacts (email addresses, phone numbers)
- Supports delivery roles (To/CC/BCC)
- Routes based on Service + Topic + Client combinations
- Contacts are people who receive notifications, not authenticated users

### Solution Structure

```
NotificationServices/
├── src/
│   ├── NotificationService.Api/
│   ├── NotificationService.Client/
│   ├── NotificationService.Domain/
│   ├── NotificationService.Infrastructure/
│   └── NotificationService.Routing/        <-- NEW PROJECT
│       ├── Domain/
│       │   ├── Enums/
│       │   └── Models/
│       ├── Data/
│       ├── Repositories/
│       ├── Services/
│       └── NotificationService.Routing.csproj
```

### Domain Models to Create

**Location:** `NotificationService.Routing/Domain/`

#### Enums

```csharp
// Domain/Enums/SourceService.cs
public enum SourceService
{
    CensusAutomation,
    PayrollFileGeneration,
    CensusReconciliation,
    // Future: add as needed
}

// Domain/Enums/NotificationTopic.cs
public enum NotificationTopic
{
    // Census Automation
    DailyImportSuccess,
    DailyImportFailure,
    SchemaValidationError,
    RecordCountMismatch,
    FileProcessingStarted,
    
    // Payroll
    PayrollFileGenerated,
    PayrollFileError,
    PayrollFilePending,
    
    // Reconciliation
    ReconciliationComplete,
    ReconciliationEscalation,
    WorkflowStuck,
    ManualInterventionRequired,
    
    // General
    SystemAlert,
    Custom
}

// Domain/Enums/DeliveryRole.cs
public enum DeliveryRole
{
    To,
    Cc,
    Bcc
}
```

#### Models

```csharp
// Domain/Models/Contact.cs
/// <summary>
/// A person who can receive notifications. Not necessarily a system user.
/// </summary>
public class Contact
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Organization { get; set; }  // e.g., "ABC Broker Agency"
    public bool IsActive { get; set; } = true;
    
    // Future-proofing: link to identity when/if they get portal access
    public Guid? UserId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public string? Notes { get; set; }
}

// Domain/Models/RecipientGroup.cs
/// <summary>
/// A named collection of contacts for routing purposes.
/// </summary>
public class RecipientGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;  // e.g., "HenryCounty-BrokerTeam"
    public string? ClientId { get; set; }              // null = global group (e.g., "Internal-CensusOps")
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation
    public List<GroupMembership> Memberships { get; set; } = new();
}

// Domain/Models/GroupMembership.cs
/// <summary>
/// Junction table linking contacts to groups.
/// </summary>
public class GroupMembership
{
    public Guid GroupId { get; set; }
    public Guid ContactId { get; set; }
    public DateTime AddedAt { get; set; }
    public string? AddedBy { get; set; }
    
    // Navigation
    public RecipientGroup? Group { get; set; }
    public Contact? Contact { get; set; }
}

// Domain/Models/RoutingPolicy.cs
/// <summary>
/// Defines who receives what notifications for which service/client combination.
/// </summary>
public class RoutingPolicy
{
    public Guid Id { get; set; }
    
    // Match criteria
    public SourceService Service { get; set; }
    public NotificationTopic Topic { get; set; }
    public string? ClientId { get; set; }  // null = default for all clients without specific override
    
    // Severity filter (optional)
    public NotificationSeverity? MinSeverity { get; set; }
    
    // Delivery configuration
    public NotificationChannel Channel { get; set; }  // Reuse from existing Domain
    public Guid RecipientGroupId { get; set; }
    public DeliveryRole Role { get; set; }
    
    // Control
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 0;  // Higher = evaluated first (for conflict resolution)
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    // Navigation
    public RecipientGroup? RecipientGroup { get; set; }
}

// Domain/Models/OutboundEvent.cs
/// <summary>
/// An event published by a service that needs to be routed to recipients.
/// </summary>
public class OutboundEvent
{
    public Guid Id { get; set; }
    
    public SourceService Service { get; set; }
    public NotificationTopic Topic { get; set; }
    public string? ClientId { get; set; }
    public NotificationSeverity Severity { get; set; }
    
    // Template/content
    public string? TemplateId { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public Dictionary<string, object> Payload { get; set; } = new();
    
    // Tracking
    public Guid? SagaId { get; set; }
    public Guid? CorrelationId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

// Domain/Models/OutboundDelivery.cs
/// <summary>
/// Tracks delivery of a routed notification to a specific recipient.
/// </summary>
public class OutboundDelivery
{
    public Guid Id { get; set; }
    public Guid OutboundEventId { get; set; }
    public Guid RoutingPolicyId { get; set; }
    public Guid ContactId { get; set; }
    
    public NotificationChannel Channel { get; set; }
    public DeliveryRole Role { get; set; }
    public DeliveryStatus Status { get; set; }  // Reuse from existing Domain
    
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
    
    // Navigation
    public OutboundEvent? OutboundEvent { get; set; }
    public RoutingPolicy? RoutingPolicy { get; set; }
    public Contact? Contact { get; set; }
}
```

### Database Schema

Follow existing conventions from `NotificationDbContext`:
- Table names: snake_case plural (e.g., `contacts`, `recipient_groups`, `routing_policies`)
- Column names: snake_case
- Use `gen_random_uuid()` for default GUIDs
- Use `NOW()` for default timestamps
- Store enums as strings with `HasConversion<string>()`
- Use `jsonb` for dictionary/JSON columns

Create a separate `RoutingDbContext` in `NotificationService.Routing/Data/` that follows the same patterns.

### Key Indexes to Create

```sql
-- Contacts
CREATE INDEX idx_contacts_email ON contacts(email);
CREATE INDEX idx_contacts_organization ON contacts(organization) WHERE organization IS NOT NULL;
CREATE INDEX idx_contacts_active ON contacts(is_active) WHERE is_active = true;

-- Recipient Groups
CREATE UNIQUE INDEX idx_recipient_groups_name_client ON recipient_groups(name, client_id);
CREATE INDEX idx_recipient_groups_client ON recipient_groups(client_id) WHERE client_id IS NOT NULL;

-- Group Memberships
CREATE INDEX idx_group_memberships_contact ON group_memberships(contact_id);

-- Routing Policies (the critical query path)
CREATE INDEX idx_routing_policies_lookup ON routing_policies(service, topic, client_id, is_enabled)
    WHERE is_enabled = true;
CREATE INDEX idx_routing_policies_client_fallback ON routing_policies(service, topic, is_enabled)
    WHERE client_id IS NULL AND is_enabled = true;

-- Outbound Events
CREATE INDEX idx_outbound_events_unprocessed ON outbound_events(created_at)
    WHERE processed_at IS NULL;
CREATE INDEX idx_outbound_events_saga ON outbound_events(saga_id) WHERE saga_id IS NOT NULL;

-- Outbound Deliveries
CREATE INDEX idx_outbound_deliveries_pending ON outbound_deliveries(status, created_at)
    WHERE status IN ('Pending', 'Failed');
CREATE INDEX idx_outbound_deliveries_event ON outbound_deliveries(outbound_event_id);
```

### Core Services to Implement

```csharp
// Services/IOutboundRouter.cs
public interface IOutboundRouter
{
    /// <summary>
    /// Publish an event to be routed to appropriate recipients.
    /// </summary>
    Task<Guid> PublishAsync(OutboundEvent evt);
    
    /// <summary>
    /// Get routing policies that match the given criteria.
    /// </summary>
    Task<List<RoutingPolicy>> GetMatchingPoliciesAsync(
        SourceService service, 
        NotificationTopic topic, 
        string? clientId,
        NotificationSeverity severity);
}

// Services/IContactService.cs
public interface IContactService
{
    Task<Contact> CreateAsync(Contact contact);
    Task<Contact?> GetByIdAsync(Guid id);
    Task<Contact?> GetByEmailAsync(string email);
    Task<List<Contact>> SearchAsync(string searchTerm, bool includeInactive = false);
    Task<Contact> UpdateAsync(Contact contact);
    Task DeactivateAsync(Guid id);
    Task<List<RecipientGroup>> GetGroupsForContactAsync(Guid contactId);
}

// Services/IRecipientGroupService.cs
public interface IRecipientGroupService
{
    Task<RecipientGroup> CreateAsync(RecipientGroup group);
    Task<RecipientGroup?> GetByIdAsync(Guid id);
    Task<List<RecipientGroup>> GetByClientAsync(string? clientId);
    Task<List<Contact>> GetMembersAsync(Guid groupId);
    Task AddMemberAsync(Guid groupId, Guid contactId, string? addedBy = null);
    Task RemoveMemberAsync(Guid groupId, Guid contactId);
    Task<List<RoutingPolicy>> GetPoliciesUsingGroupAsync(Guid groupId);
}

// Services/IRoutingPolicyService.cs
public interface IRoutingPolicyService
{
    Task<RoutingPolicy> CreateAsync(RoutingPolicy policy);
    Task<RoutingPolicy?> GetByIdAsync(Guid id);
    Task<List<RoutingPolicy>> GetByClientAsync(string? clientId);
    Task<List<RoutingPolicy>> GetByServiceAndTopicAsync(SourceService service, NotificationTopic topic);
    Task<RoutingPolicy> UpdateAsync(RoutingPolicy policy);
    Task DeleteAsync(Guid id);
    Task<RoutingPolicy> ToggleAsync(Guid id);
}
```

### API Endpoints to Create

Add these to `NotificationService.Api` under a `/api/routing/` prefix:

```
# Contacts
GET    /api/routing/contacts                    # List/search contacts
GET    /api/routing/contacts/{id}               # Get contact details + groups
POST   /api/routing/contacts                    # Create contact
PUT    /api/routing/contacts/{id}               # Update contact
DELETE /api/routing/contacts/{id}               # Deactivate contact

# Recipient Groups
GET    /api/routing/groups                      # List groups (optional ?clientId filter)
GET    /api/routing/groups/{id}                 # Get group with members
POST   /api/routing/groups                      # Create group
PUT    /api/routing/groups/{id}                 # Update group
POST   /api/routing/groups/{id}/members         # Add member
DELETE /api/routing/groups/{id}/members/{contactId}  # Remove member

# Routing Policies
GET    /api/routing/policies                    # List policies (optional ?clientId, ?service filters)
GET    /api/routing/policies/{id}               # Get policy details
POST   /api/routing/policies                    # Create policy
PUT    /api/routing/policies/{id}               # Update policy
DELETE /api/routing/policies/{id}               # Delete policy
POST   /api/routing/policies/{id}/toggle        # Enable/disable

# Client Configuration View (aggregate)
GET    /api/routing/clients/{clientId}/configuration  # Full routing config for a client

# Publish (internal use)
POST   /api/routing/publish                     # Publish an outbound event
```

### MCP Tools to Add

Create corresponding MCP tools for Claude to manage routing:

- `routing_list_contacts` / `routing_get_contact` / `routing_create_contact` / `routing_deactivate_contact`
- `routing_list_groups` / `routing_get_group` / `routing_add_group_member` / `routing_remove_group_member`
- `routing_list_policies` / `routing_create_policy` / `routing_toggle_policy`
- `routing_get_client_configuration` (aggregate view)
- `routing_publish_event` (for testing)

### Project References

`NotificationService.Routing.csproj` should reference:
- `NotificationService.Domain` (for shared enums like `NotificationChannel`, `NotificationSeverity`, `DeliveryStatus`)
- Standard packages: `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`

### Migration Strategy

1. Create the new project with domain models
2. Create `RoutingDbContext` with all entity configurations
3. Generate EF migration
4. Add API endpoints
5. Add MCP tools
6. Wire up DI in `NotificationService.Api`

### Key Design Decisions

1. **Separate DbContext** - `RoutingDbContext` keeps this bounded context isolated while allowing deployment in the same service
2. **Contact.UserId is nullable** - Future-proofing for when contacts become portal users
3. **Client-specific vs global groups** - `RecipientGroup.ClientId = null` means the group is available for any client
4. **Policy fallback** - When routing, first look for client-specific policy, then fall back to `ClientId = null` default
5. **Priority field** - When multiple policies match, higher priority wins (allows overrides)

### Success Criteria

- [ ] Can create/manage contacts without them being system users
- [ ] Can group contacts into named recipient groups
- [ ] Can define routing policies: "For CensusAutomation + DailyImportSuccess + HenryCounty → Email To: HenryCounty-ClientContacts, CC: HenryCounty-BrokerTeam"
- [ ] Can deactivate a contact and have them removed from all notification routing instantly
- [ ] MCP tools allow Claude to manage routing configuration
- [ ] Existing NotificationService user-centric flow is unchanged
