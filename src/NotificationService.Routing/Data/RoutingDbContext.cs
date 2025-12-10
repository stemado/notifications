using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Enums;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;
using System.Text.Json;

namespace NotificationService.Routing.Data;

/// <summary>
/// Entity Framework DbContext for the notification routing bounded context
/// </summary>
public class RoutingDbContext : DbContext
{
    public RoutingDbContext(DbContextOptions<RoutingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<RecipientGroup> RecipientGroups => Set<RecipientGroup>();
    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();
    public DbSet<RoutingPolicy> RoutingPolicies => Set<RoutingPolicy>();
    public DbSet<OutboundEvent> OutboundEvents => Set<OutboundEvent>();
    public DbSet<OutboundDelivery> OutboundDeliveries => Set<OutboundDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Contact entity
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("contacts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(320)
                .HasColumnName("email");

            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasColumnName("phone");

            entity.Property(e => e.Organization)
                .HasMaxLength(200)
                .HasColumnName("organization");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()")
                .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()")
                .HasColumnName("updated_at");

            entity.Property(e => e.DeactivatedAt)
                .HasColumnName("deactivated_at");

            entity.Property(e => e.Notes)
                .HasColumnName("notes");

            // Indexes
            entity.HasIndex(e => e.Email)
                .HasDatabaseName("idx_contacts_email");

            entity.HasIndex(e => e.Organization)
                .HasDatabaseName("idx_contacts_organization")
                .HasFilter("organization IS NOT NULL");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("idx_contacts_active")
                .HasFilter("is_active = true");
        });

        // Configure RecipientGroup entity
        modelBuilder.Entity<RecipientGroup>(entity =>
        {
            entity.ToTable("recipient_groups");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            entity.Property(e => e.ClientId)
                .HasMaxLength(100)
                .HasColumnName("client_id");

            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()")
                .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()")
                .HasColumnName("updated_at");

            // Indexes
            entity.HasIndex(e => new { e.Name, e.ClientId })
                .IsUnique()
                .HasDatabaseName("idx_recipient_groups_name_client");

            entity.HasIndex(e => e.ClientId)
                .HasDatabaseName("idx_recipient_groups_client")
                .HasFilter("client_id IS NOT NULL");
        });

        // Configure GroupMembership entity (junction table)
        modelBuilder.Entity<GroupMembership>(entity =>
        {
            entity.ToTable("group_memberships");
            entity.HasKey(e => new { e.GroupId, e.ContactId });

            entity.Property(e => e.GroupId)
                .HasColumnName("group_id");

            entity.Property(e => e.ContactId)
                .HasColumnName("contact_id");

            entity.Property(e => e.AddedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()")
                .HasColumnName("added_at");

            entity.Property(e => e.AddedBy)
                .HasMaxLength(200)
                .HasColumnName("added_by");

            // Relationships
            entity.HasOne(e => e.Group)
                .WithMany(g => g.Memberships)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Contact)
                .WithMany(c => c.Memberships)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.ContactId)
                .HasDatabaseName("idx_group_memberships_contact");
        });

        // Configure RoutingPolicy entity
        modelBuilder.Entity<RoutingPolicy>(entity =>
        {
            entity.ToTable("routing_policies");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");

            entity.Property(e => e.Service)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasColumnName("service");

            entity.Property(e => e.Topic)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasColumnName("topic");

            entity.Property(e => e.ClientId)
                .HasMaxLength(100)
                .HasColumnName("client_id");

            entity.Property(e => e.MinSeverity)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("min_severity");

            entity.Property(e => e.Channel)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("channel");

            entity.Property(e => e.RecipientGroupId)
                .IsRequired()
                .HasColumnName("recipient_group_id");

            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(10)
                .HasColumnName("role");

            entity.Property(e => e.IsEnabled)
                .HasDefaultValue(true)
                .HasColumnName("is_enabled");

            entity.Property(e => e.Priority)
                .HasDefaultValue(0)
                .HasColumnName("priority");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()")
                .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()")
                .HasColumnName("updated_at");

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasColumnName("updated_by");

            // Relationships
            entity.HasOne(e => e.RecipientGroup)
                .WithMany(g => g.Policies)
                .HasForeignKey(e => e.RecipientGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => new { e.Service, e.Topic, e.ClientId, e.IsEnabled })
                .HasDatabaseName("idx_routing_policies_lookup")
                .HasFilter("is_enabled = true");

            entity.HasIndex(e => new { e.Service, e.Topic, e.IsEnabled })
                .HasDatabaseName("idx_routing_policies_client_fallback")
                .HasFilter("client_id IS NULL AND is_enabled = true");
        });

        // Configure OutboundEvent entity
        modelBuilder.Entity<OutboundEvent>(entity =>
        {
            entity.ToTable("outbound_events");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");

            entity.Property(e => e.Service)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasColumnName("service");

            entity.Property(e => e.Topic)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasColumnName("topic");

            entity.Property(e => e.ClientId)
                .HasMaxLength(100)
                .HasColumnName("client_id");

            entity.Property(e => e.Severity)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("severity");

            entity.Property(e => e.TemplateId)
                .HasMaxLength(100)
                .HasColumnName("template_id");

            entity.Property(e => e.Subject)
                .HasMaxLength(500)
                .HasColumnName("subject");

            entity.Property(e => e.Body)
                .HasColumnName("body");

            entity.Property(e => e.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, JsonElement>()
                );

            entity.Property(e => e.SagaId)
                .HasColumnName("saga_id");

            entity.Property(e => e.CorrelationId)
                .HasColumnName("correlation_id");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()")
                .HasColumnName("created_at");

            entity.Property(e => e.ProcessedAt)
                .HasColumnName("processed_at");

            // Indexes
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("idx_outbound_events_unprocessed")
                .HasFilter("processed_at IS NULL");

            entity.HasIndex(e => e.SagaId)
                .HasDatabaseName("idx_outbound_events_saga")
                .HasFilter("saga_id IS NOT NULL");

            entity.HasIndex(e => new { e.ClientId, e.CreatedAt })
                .HasDatabaseName("idx_outbound_events_client")
                .IsDescending(false, true);
        });

        // Configure OutboundDelivery entity
        modelBuilder.Entity<OutboundDelivery>(entity =>
        {
            entity.ToTable("outbound_deliveries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");

            entity.Property(e => e.OutboundEventId)
                .IsRequired()
                .HasColumnName("outbound_event_id");

            entity.Property(e => e.RoutingPolicyId)
                .IsRequired()
                .HasColumnName("routing_policy_id");

            entity.Property(e => e.ContactId)
                .IsRequired()
                .HasColumnName("contact_id");

            entity.Property(e => e.Channel)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("channel");

            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(10)
                .HasColumnName("role");

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValueSql("'Pending'")
                .HasColumnName("status");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()")
                .HasColumnName("created_at");

            entity.Property(e => e.SentAt)
                .HasColumnName("sent_at");

            entity.Property(e => e.DeliveredAt)
                .HasColumnName("delivered_at");

            entity.Property(e => e.FailedAt)
                .HasColumnName("failed_at");

            entity.Property(e => e.ErrorMessage)
                .HasColumnName("error_message");

            entity.Property(e => e.AttemptCount)
                .HasDefaultValue(0)
                .HasColumnName("attempt_count");

            entity.Property(e => e.NextRetryAt)
                .HasColumnName("next_retry_at");

            // Relationships
            entity.HasOne(e => e.OutboundEvent)
                .WithMany(ev => ev.Deliveries)
                .HasForeignKey(e => e.OutboundEventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RoutingPolicy)
                .WithMany()
                .HasForeignKey(e => e.RoutingPolicyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Contact)
                .WithMany()
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                .HasDatabaseName("idx_outbound_deliveries_pending")
                .HasFilter("status IN ('Pending', 'Failed')");

            entity.HasIndex(e => e.OutboundEventId)
                .HasDatabaseName("idx_outbound_deliveries_event");

            entity.HasIndex(e => new { e.ContactId, e.CreatedAt })
                .HasDatabaseName("idx_outbound_deliveries_contact")
                .IsDescending(false, true);
        });
    }
}
