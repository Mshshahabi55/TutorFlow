using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Search.Domain;

namespace TutorFlow.Modules.Search.Persistence;

public sealed class SearchDbContext(DbContextOptions<SearchDbContext> options) : DbContext(options)
{
    public DbSet<TutorSearchDocument> TutorSearchDocuments => Set<TutorSearchDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("search");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SearchDbContext).Assembly);
    }
}
