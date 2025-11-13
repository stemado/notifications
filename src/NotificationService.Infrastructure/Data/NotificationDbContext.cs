using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Notification entity
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Severity).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            entity.Property(e => e.GroupKey).HasMaxLength(200);
            entity.Property(e => e.GroupCount).HasDefaultValue(1);
            entity.Property(e => e.RequiresAck).HasDefaultValue(false);

            // Configure JSON columns
            entity.Property(e => e.Actions)
                .HasColumnName("ActionsJson")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<NotificationAction>>(v, (JsonSerializerOptions?)null) ?? new List<NotificationAction>()
                );

            entity.Property(e => e.Metadata)
                .HasColumnName("MetadataJson")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                );

            // Configure indexes
            entity.HasIndex(e => new { e.UserId, e.AcknowledgedAt })
                .HasDatabaseName("idx_notifications_user_unread")
                .HasFilter("\"AcknowledgedAt\" IS NULL");

            entity.HasIndex(e => new { e.TenantId, e.CreatedAt })
                .HasDatabaseName("idx_notifications_tenant")
                .IsDescending(false, true);

            entity.HasIndex(e => e.GroupKey)
                .HasDatabaseName("idx_notifications_group_key")
                .HasFilter("\"AcknowledgedAt\" IS NULL");

            entity.HasIndex(e => new { e.SagaId, e.CreatedAt })
                .HasDatabaseName("idx_notifications_saga")
                .IsDescending(false, true);
        });

        // Configure NotificationDelivery entity
        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.ToTable("NotificationDeliveries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.NotificationId).IsRequired();
            entity.Property(e => e.Channel).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.AttemptCount).HasDefaultValue(0);

            // Configure relationship
            entity.HasOne(e => e.Notification)
                .WithMany()
                .HasForeignKey(e => e.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure index
            entity.HasIndex(e => e.NotificationId)
                .HasDatabaseName("idx_deliveries_notification");
        });
    }
}
