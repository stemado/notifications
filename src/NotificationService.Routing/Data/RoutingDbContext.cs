using MassTransit;
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
    public DbSet<TestEmailDelivery> TestEmailDeliveries => Set<TestEmailDelivery>();
    public DbSet<ClientAttestationTemplate> ClientAttestationTemplates => Set<ClientAttestationTemplate>();
    public DbSet<ClientAttestationTemplatePolicy> ClientAttestationTemplatePolicies => Set<ClientAttestationTemplatePolicy>();
    public DbSet<ClientAttestationTemplateGroup> ClientAttestationTemplateGroups => Set<ClientAttestationTemplateGroup>();
    public DbSet<TopicTemplateMapping> TopicTemplateMappings => Set<TopicTemplateMapping>();

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

            entity.Property(e => e.Purpose)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValueSql("'Production'")
                .HasColumnName("purpose");

            entity.Property(e => e.Tags)
                .HasColumnName("tags")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                );

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

            entity.HasIndex(e => e.Purpose)
                .HasDatabaseName("idx_recipient_groups_purpose");
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

        // Configure TestEmailDelivery entity
        modelBuilder.Entity<TestEmailDelivery>(entity =>
        {
            entity.ToTable("test_email_deliveries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");

            entity.Property(e => e.RecipientGroupId)
                .HasColumnName("recipient_group_id");

            entity.Property(e => e.TemplateName)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("template_name");

            entity.Property(e => e.Subject)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("subject");

            entity.Property(e => e.Recipients)
                .IsRequired()
                .HasColumnName("recipients")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                );

            entity.Property(e => e.TestReason)
                .HasMaxLength(500)
                .HasColumnName("test_reason");

            entity.Property(e => e.InitiatedBy)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("initiated_by");

            entity.Property(e => e.SentAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()")
                .HasColumnName("sent_at");

            entity.Property(e => e.Success)
                .IsRequired()
                .HasDefaultValue(false)
                .HasColumnName("success");

            entity.Property(e => e.ErrorMessage)
                .HasColumnName("error_message");

            entity.Property(e => e.MessageId)
                .HasMaxLength(200)
                .HasColumnName("message_id");

            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .HasColumnName("provider");

            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, JsonElement>()
                );

            // Role-based sending properties
            entity.Property(e => e.ToGroupId)
                .HasColumnName("to_group_id");

            entity.Property(e => e.CcGroupId)
                .HasColumnName("cc_group_id");

            entity.Property(e => e.BccGroupId)
                .HasColumnName("bcc_group_id");

            entity.Property(e => e.ToRecipients)
                .IsRequired()
                .HasColumnName("to_recipients")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                );

            entity.Property(e => e.CcRecipients)
                .IsRequired()
                .HasColumnName("cc_recipients")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                );

            entity.Property(e => e.BccRecipients)
                .IsRequired()
                .HasColumnName("bcc_recipients")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                );

            entity.Property(e => e.UsedRoleBasedSending)
                .IsRequired()
                .HasDefaultValue(false)
                .HasColumnName("used_role_based_sending");

            // Relationships
            entity.HasOne(e => e.RecipientGroup)
                .WithMany()
                .HasForeignKey(e => e.RecipientGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ToGroup)
                .WithMany()
                .HasForeignKey(e => e.ToGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CcGroup)
                .WithMany()
                .HasForeignKey(e => e.CcGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.BccGroup)
                .WithMany()
                .HasForeignKey(e => e.BccGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(e => e.RecipientGroupId)
                .HasDatabaseName("idx_test_email_deliveries_group");

            entity.HasIndex(e => e.SentAt)
                .HasDatabaseName("idx_test_email_deliveries_sent")
                .IsDescending();

            entity.HasIndex(e => e.InitiatedBy)
                .HasDatabaseName("idx_test_email_deliveries_initiated_by");
        });

        // Configure ClientAttestationTemplate entity
        modelBuilder.Entity<ClientAttestationTemplate>(entity =>
        {
            entity.ToTable("client_attestation_templates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");

            entity.Property(e => e.ClientId)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("client_id");

            entity.Property(e => e.TemplateId)
                .IsRequired()
                .HasColumnName("template_id");

            entity.Property(e => e.IsEnabled)
                .HasDefaultValue(true)
                .HasColumnName("is_enabled");

            entity.Property(e => e.Priority)
                .HasDefaultValue(0)
                .HasColumnName("priority");

            entity.Property(e => e.Notes)
                .HasColumnName("notes");

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

            // Unique constraint: one configuration per client-template pair
            entity.HasIndex(e => new { e.ClientId, e.TemplateId })
                .IsUnique()
                .HasDatabaseName("uq_client_template");

            // Indexes for lookups
            entity.HasIndex(e => e.ClientId)
                .HasDatabaseName("idx_client_attestation_templates_client");

            entity.HasIndex(e => e.TemplateId)
                .HasDatabaseName("idx_client_attestation_templates_template");

            entity.HasIndex(e => new { e.ClientId, e.IsEnabled })
                .HasDatabaseName("idx_client_attestation_templates_enabled")
                .HasFilter("is_enabled = true");
        });

        // Configure ClientAttestationTemplatePolicy entity
        modelBuilder.Entity<ClientAttestationTemplatePolicy>(entity =>
        {
            entity.ToTable("client_attestation_template_policies");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");

            entity.Property(e => e.ClientAttestationTemplateId)
                .IsRequired()
                .HasColumnName("client_attestation_template_id");

            entity.Property(e => e.RoutingPolicyId)
                .IsRequired()
                .HasColumnName("routing_policy_id");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()")
                .HasColumnName("created_at");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasColumnName("created_by");

            // Relationships
            entity.HasOne(e => e.ClientAttestationTemplate)
                .WithMany(t => t.Policies)
                .HasForeignKey(e => e.ClientAttestationTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RoutingPolicy)
                .WithMany()
                .HasForeignKey(e => e.RoutingPolicyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: one policy per client-template
            entity.HasIndex(e => new { e.ClientAttestationTemplateId, e.RoutingPolicyId })
                .IsUnique()
                .HasDatabaseName("uq_attestation_template_policy");

            entity.HasIndex(e => e.ClientAttestationTemplateId)
                .HasDatabaseName("idx_cat_policies_template");

            entity.HasIndex(e => e.RoutingPolicyId)
                .HasDatabaseName("idx_cat_policies_policy");
        });

        // Configure ClientAttestationTemplateGroup entity
        modelBuilder.Entity<ClientAttestationTemplateGroup>(entity =>
        {
            entity.ToTable("client_attestation_template_groups");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");

            entity.Property(e => e.ClientAttestationTemplateId)
                .IsRequired()
                .HasColumnName("client_attestation_template_id");

            entity.Property(e => e.RecipientGroupId)
                .IsRequired()
                .HasColumnName("recipient_group_id");

            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(10)
                .HasDefaultValueSql("'To'")
                .HasColumnName("role");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()")
                .HasColumnName("created_at");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasColumnName("created_by");

            // Relationships
            entity.HasOne(e => e.ClientAttestationTemplate)
                .WithMany(t => t.Groups)
                .HasForeignKey(e => e.ClientAttestationTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RecipientGroup)
                .WithMany()
                .HasForeignKey(e => e.RecipientGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: one group per client-template
            entity.HasIndex(e => new { e.ClientAttestationTemplateId, e.RecipientGroupId })
                .IsUnique()
                .HasDatabaseName("uq_attestation_template_group");

            entity.HasIndex(e => e.ClientAttestationTemplateId)
                .HasDatabaseName("idx_cat_groups_template");

            entity.HasIndex(e => e.RecipientGroupId)
                .HasDatabaseName("idx_cat_groups_group");
        });

        // Configure TopicTemplateMapping entity
        modelBuilder.Entity<TopicTemplateMapping>(entity =>
        {
            entity.ToTable("topic_template_mappings");
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

            entity.Property(e => e.TemplateId)
                .IsRequired()
                .HasColumnName("template_id");

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

            // Indexes
            entity.HasIndex(e => new { e.Service, e.Topic, e.IsEnabled })
                .HasDatabaseName("idx_topic_template_mappings_lookup")
                .HasFilter("is_enabled = true");

            entity.HasIndex(e => new { e.Service, e.Topic, e.ClientId, e.IsEnabled })
                .HasDatabaseName("idx_topic_template_mappings_client")
                .HasFilter("is_enabled = true");
        });

        // Configure MassTransit outbox tables
        // These tables are used for transactional outbox pattern
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
