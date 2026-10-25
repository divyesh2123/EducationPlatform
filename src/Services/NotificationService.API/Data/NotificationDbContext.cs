using Microsoft.EntityFrameworkCore;
using NotificationService.API.Models;

namespace NotificationService.API.Data;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(n => n.Id);
            entity.Property(n => n.EventType).HasMaxLength(100).IsRequired();
            entity.Property(n => n.Recipient).HasMaxLength(200).IsRequired();
            entity.Property(n => n.Subject).HasMaxLength(300).IsRequired();
            entity.Property(n => n.Body).HasMaxLength(4000).IsRequired();
            entity.HasIndex(n => n.Recipient);
            // One event -> one notification per recipient, even if the message is redelivered.
            entity.HasIndex(n => new { n.IntegrationEventId, n.Recipient }).IsUnique();
        });
    }
}
