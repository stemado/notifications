using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Domain.Models.Preferences;
using System.Text.Json;

namespace NotificationService.Infrastructure.Data;

/// <summary>
/// Entity Framework DbContext for the notification service
/// </summary>
public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<UserNotificationPreference> UserNotificationPreferences => Set<UserNotificationPreference>();
    public DbSet<NotificationSubscription> NotificationSubscriptions => Set<NotificationSubscription>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Notification entity
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.UserId).IsRequired().HasColumnName("user_id");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.Severity).IsRequired().HasConversion<string>().HasMaxLength(20).HasColumnName("severity");
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200).HasColumnName("title");
            entity.Property(e => e.Message).IsRequired().HasColumnName("message");
            entity.Property(e => e.SagaId).HasColumnName("saga_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.EventType).HasMaxLength(100).HasColumnName("event_type");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()").HasColumnName("created_at");
            entity.Property(e => e.AcknowledgedAt).HasColumnName("acknowledged_at");
            entity.Property(e => e.DismissedAt).HasColumnName("dismissed_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.RepeatInterval).HasColumnName("repeat_interval");
            entity.Property(e => e.LastRepeatedAt).HasColumnName("last_repeated_at");
            entity.Property(e => e.RequiresAck).HasDefaultValue(false).HasColumnName("requires_ack");
            entity.Property(e => e.GroupKey).HasMaxLength(200).HasColumnName("group_key");
            entity.Property(e => e.GroupCount).HasDefaultValue(1).HasColumnName("group_count");

            // Configure JSON columns
            entity.Property(e => e.Actions)
                .HasColumnName("actions_json")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<NotificationAction>>(v, (JsonSerializerOptions?)null) ?? new List<NotificationAction>()
                );

            entity.Property(e => e.Metadata)
                .HasColumnName("metadata_json")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                );

            // Configure indexes
            entity.HasIndex(e => new { e.UserId, e.AcknowledgedAt })
                .HasDatabaseName("idx_notifications_user_unread")
                .HasFilter("acknowledged_at IS NULL");

            entity.HasIndex(e => new { e.TenantId, e.CreatedAt })
                .HasDatabaseName("idx_notifications_tenant")
                .IsDescending(false, true);

            entity.HasIndex(e => e.GroupKey)
                .HasDatabaseName("idx_notifications_group_key")
                .HasFilter("acknowledged_at IS NULL");

            entity.HasIndex(e => new { e.SagaId, e.CreatedAt })
                .HasDatabaseName("idx_notifications_saga")
                .IsDescending(false, true);
        });

        // Configure NotificationDelivery entity
        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.ToTable("notification_deliveries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.NotificationId).IsRequired().HasColumnName("notification_id");
            entity.Property(e => e.Channel).IsRequired().HasConversion<string>().HasMaxLength(20).HasColumnName("channel");
            entity.Property(e => e.DeliveredAt).HasColumnName("delivered_at");
            entity.Property(e => e.FailedAt).HasColumnName("failed_at");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.AttemptCount).HasDefaultValue(0).HasColumnName("attempt_count");

            // Configure relationship
            entity.HasOne(e => e.Notification)
                .WithMany()
                .HasForeignKey(e => e.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure index
            entity.HasIndex(e => e.NotificationId)
                .HasDatabaseName("idx_deliveries_notification");
        });

        // Configure UserNotificationPreference entity (Phase 2)
        modelBuilder.Entity<UserNotificationPreference>(entity =>
        {
            entity.ToTable("user_notification_preferences");
            entity.HasKey(e => new { e.UserId, e.Channel });

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Channel).IsRequired().HasConversion<string>().HasMaxLength(20).HasColumnName("channel");
            entity.Property(e => e.MinSeverity).IsRequired().HasConversion<string>().HasMaxLength(20).HasColumnName("min_severity");
            entity.Property(e => e.Enabled).HasDefaultValue(true).HasColumnName("enabled");
        });

        // Configure NotificationSubscription entity (Phase 2)
        modelBuilder.Entity<NotificationSubscription>(entity =>
        {
            entity.ToTable("notification_subscriptions");
            entity.HasKey(e => new { e.UserId, e.ClientId, e.SagaId });

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.SagaId).HasColumnName("saga_id");
            entity.Property(e => e.MinSeverity).IsRequired().HasConversion<string>().HasMaxLength(20).HasColumnName("min_severity");
        });

        // Configure EmailTemplate entity (Phase 0)
        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.ToTable("email_templates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .UseIdentityAlwaysColumn();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");

            entity.Property(e => e.Subject)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("subject");

            entity.Property(e => e.HtmlContent)
                .HasColumnName("html_content");

            entity.Property(e => e.TextContent)
                .HasColumnName("text_content");

            entity.Property(e => e.Variables)
                .HasColumnType("jsonb")
                .HasColumnName("variables");

            entity.Property(e => e.TestData)
                .HasColumnType("jsonb")
                .HasColumnName("test_data");

            entity.Property(e => e.DefaultRecipients)
                .HasColumnType("jsonb")
                .HasColumnName("default_recipients");

            entity.Property(e => e.TemplateType)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("notification")
                .HasColumnName("template_type");

            entity.Property(e => e.IsActive)
                .IsRequired()
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

            // Unique index on name
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("idx_email_templates_name");

            // Index for active templates by type
            entity.HasIndex(e => new { e.IsActive, e.TemplateType })
                .HasDatabaseName("idx_email_templates_active_type");
        });
    }
}
