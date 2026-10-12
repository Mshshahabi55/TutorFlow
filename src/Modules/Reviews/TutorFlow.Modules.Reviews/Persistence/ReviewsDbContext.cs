using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Reviews.Domain;

namespace TutorFlow.Modules.Reviews.Persistence;

public sealed class ReviewsDbContext(DbContextOptions<ReviewsDbContext> options) : DbContext(options)
{
    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<SubcategoryRating> SubcategoryRatings => Set<SubcategoryRating>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reviews");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReviewsDbContext).Assembly);
    }
}
