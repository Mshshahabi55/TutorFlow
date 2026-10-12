using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Notifications.Domain;

namespace TutorFlow.Modules.Notifications.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<SentAbandonedCheckoutEmail> SentAbandonedCheckoutEmails => Set<SentAbandonedCheckoutEmail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
