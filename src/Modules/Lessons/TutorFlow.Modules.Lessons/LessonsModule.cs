using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Lessons.Features.CreateLessonFromBooking;
using TutorFlow.Modules.Lessons.Infrastructure;
using TutorFlow.Modules.Lessons.Persistence;
using TutorFlow.Shared;
using CompleteDueLessonsFeature = TutorFlow.Modules.Lessons.Features.CompleteDueLessons.CompleteDueLessons;
using GetMyLessonsFeature = TutorFlow.Modules.Lessons.Features.GetMyLessons.GetMyLessons;

namespace TutorFlow.Modules.Lessons;

public static class LessonsModule
{
    public static IServiceCollection AddLessonsModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<LessonsDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("LessonsDb")));

        services.AddScoped<CreateLessonFromBooking.Handler>();
        services.AddScoped<CompleteDueLessonsFeature.Handler>();
        services.AddScoped<GetMyLessonsFeature.Handler>();
        services.AddScoped<ICompletedLessonCountQuery, CompletedLessonCountQuery>();

        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.StudentOnly, p => p.RequireRole(RoleNames.Student));

        return services;
    }

    public static IEndpointRouteBuilder MapLessonsModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lessons").WithTags("Lessons");

        group.MapGet("/me", GetMyLessonsAsync)
            .WithName("GetMyLessons")
            .WithSummary("List the caller's own lessons as a student.")
            .RequireAuthorization(PolicyNames.StudentOnly);

        return app;
    }

    private static async Task<IResult> GetMyLessonsAsync(
        ClaimsPrincipal user,
        GetMyLessonsFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await handler.HandleAsync(userId, ct));
    }
}
