using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Search.Infrastructure;
using TutorFlow.Modules.Search.Persistence;
using SearchTutorsFeature = TutorFlow.Modules.Search.Features.SearchTutors.SearchTutors;

namespace TutorFlow.Modules.Search;

public static class SearchModule
{
    public static IServiceCollection AddSearchModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<SearchDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("SearchDb")));

        services.AddScoped<SearchTutorsFeature.Handler>();
        services.AddScoped<IValidator<SearchTutorsFeature.Query>, SearchTutorsFeature.Validator>();

        return services;
    }

    public static IEndpointRouteBuilder MapSearchModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/search").WithTags("Search");

        // Public — Guests can browse/search (ROLES_AND_PERMISSIONS.md); no RequireAuthorization.
        group.MapGet("/tutors", SearchTutorsAsync)
            .WithName("SearchTutors")
            .WithSummary("Search published tutors with filters, sorting, and pagination.")
            .AddEndpointFilter<ValidationFilter<SearchTutorsFeature.Query>>();

        return app;
    }

    private static async Task<IResult> SearchTutorsAsync(
        [AsParameters] SearchTutorsFeature.Query query,
        SearchTutorsFeature.Handler handler,
        CancellationToken ct)
    {
        var response = await handler.HandleAsync(query, ct);
        return TypedResults.Ok(response);
    }
}
