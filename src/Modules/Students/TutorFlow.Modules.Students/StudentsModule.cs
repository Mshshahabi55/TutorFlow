using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Students.Infrastructure;
using TutorFlow.Modules.Students.Persistence;
using AddFavoriteFeature = TutorFlow.Modules.Students.Features.AddFavorite.AddFavorite;
using GetFavoriteStatusFeature = TutorFlow.Modules.Students.Features.GetFavoriteStatus.GetFavoriteStatus;
using GetFavoriteStatusesFeature = TutorFlow.Modules.Students.Features.GetFavoriteStatuses.GetFavoriteStatuses;
using RemoveFavoriteFeature = TutorFlow.Modules.Students.Features.RemoveFavorite.RemoveFavorite;

namespace TutorFlow.Modules.Students;

public static class StudentsModule
{
    public static IServiceCollection AddStudentsModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<StudentsDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("StudentsDb")));

        services.AddScoped<AddFavoriteFeature.Handler>();
        services.AddScoped<RemoveFavoriteFeature.Handler>();
        services.AddScoped<GetFavoriteStatusFeature.Handler>();
        services.AddScoped<GetFavoriteStatusesFeature.Handler>();

        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.StudentOnly, p => p.RequireRole(RoleNames.Student));

        return services;
    }

    public static IEndpointRouteBuilder MapStudentsModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students")
            .WithTags("Students")
            .RequireAuthorization(PolicyNames.StudentOnly);

        // PUT/DELETE/GET rather than a single toggle endpoint — idempotent, matches the
        // Tutor Profile BookingPanel's favorite-toggle UX (ADR-0022,
        // PREPLY_TUTOR_PROFILE_PARITY_SPEC.md §2/§6): the heart icon reflects and sets a
        // binary favorited/not-favorited state, never a counter or history.
        group.MapPut("/me/favorites/{tutorUserId:guid}", AddFavoriteAsync)
            .WithName("AddFavoriteTutor")
            .WithSummary("Add a tutor to the current student's favorites (idempotent).");

        group.MapDelete("/me/favorites/{tutorUserId:guid}", RemoveFavoriteAsync)
            .WithName("RemoveFavoriteTutor")
            .WithSummary("Remove a tutor from the current student's favorites (idempotent).");

        group.MapGet("/me/favorites/{tutorUserId:guid}", GetFavoriteStatusAsync)
            .WithName("GetFavoriteTutorStatus")
            .WithSummary("Whether the current student has favorited this tutor.");

        // Bulk counterpart (ADR-0024) — one request for a whole page of search results instead
        // of one per card.
        group.MapGet("/me/favorites", GetFavoriteStatusesAsync)
            .WithName("GetFavoriteTutorStatuses")
            .WithSummary("Which of the given tutor IDs the current student has favorited.");

        return app;
    }

    private static async Task<IResult> AddFavoriteAsync(
        ClaimsPrincipal user,
        Guid tutorUserId,
        AddFavoriteFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(userId, tutorUserId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> RemoveFavoriteAsync(
        ClaimsPrincipal user,
        Guid tutorUserId,
        RemoveFavoriteFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        await handler.HandleAsync(userId, tutorUserId, ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetFavoriteStatusAsync(
        ClaimsPrincipal user,
        Guid tutorUserId,
        GetFavoriteStatusFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await handler.HandleAsync(userId, tutorUserId, ct));
    }

    private static async Task<IResult> GetFavoriteStatusesAsync(
        ClaimsPrincipal user,
        string? tutorUserIds,
        GetFavoriteStatusesFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var ids = (tutorUserIds ?? "")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s, out var id) ? id : (Guid?)null)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToList();

        return TypedResults.Ok(await handler.HandleAsync(userId, ids, ct));
    }
}
