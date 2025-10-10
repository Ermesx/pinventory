using Microsoft.EntityFrameworkCore;

namespace Pinventory.Notifications;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{

    public DbSet<NotificationInbox> NotificationInboxes => Set<NotificationInbox>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // NotificationInbox + Notification
        b.Entity<NotificationInbox>(eb =>
        {
            eb.ToTable("notification_inboxes");
            eb.HasKey(x => x.UserId);
            eb.Navigation(x => x.Notifications).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<Notification>(eb =>
        {
            eb.ToTable("notifications");
            eb.HasKey(x => x.Id);
            eb.Property(x => x.Type).IsRequired();
            eb.Property(x => x.PayloadJson).IsRequired();
        });

        // Map notifications to inbox (1..n). Use shadow FK NotificationInboxUserId
        b.Entity<Notification>()
         .Property<Guid>("NotificationInboxUserId");
        b.Entity<Notification>()
         .HasOne<NotificationInbox>()
         .WithMany(i => i.Notifications)
         .HasForeignKey("NotificationInboxUserId")
         .OnDelete(DeleteBehavior.Cascade);
    }
}