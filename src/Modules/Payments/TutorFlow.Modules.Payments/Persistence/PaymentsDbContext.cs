using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Payments.Domain;
using Wolverine.EntityFrameworkCore;

namespace TutorFlow.Modules.Payments.Persistence;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);

        // A44: Payments' own durable outbox/inbox tables, in its own schema — registered as an
        // Ancillary Wolverine message store (Program.cs) for ProcessStripeWebhook.
        modelBuilder.MapWolverineEnvelopeStorage();
    }
}
