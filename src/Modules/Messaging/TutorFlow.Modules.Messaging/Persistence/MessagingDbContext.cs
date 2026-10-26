using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Messaging.Domain;

namespace TutorFlow.Modules.Messaging.Persistence;

public sealed class MessagingDbContext(DbContextOptions<MessagingDbContext> options) : DbContext(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("messaging");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MessagingDbContext).Assembly);
    }
}
