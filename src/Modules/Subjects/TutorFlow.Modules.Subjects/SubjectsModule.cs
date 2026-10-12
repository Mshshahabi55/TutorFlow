using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Subjects.Persistence;
using GetLanguagesFeature = TutorFlow.Modules.Subjects.Features.GetLanguages.GetLanguages;
using GetSpecialtiesFeature = TutorFlow.Modules.Subjects.Features.GetSpecialties.GetSpecialties;
using GetSubjectsFeature = TutorFlow.Modules.Subjects.Features.GetSubjects.GetSubjects;

namespace TutorFlow.Modules.Subjects;

public static class SubjectsModule
{
    public static IServiceCollection AddSubjectsModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<SubjectsDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("SubjectsDb")));

        services.AddScoped<GetSubjectsFeature.Handler>();
        services.AddScoped<GetSpecialtiesFeature.Handler>();
        services.AddScoped<GetLanguagesFeature.Handler>();

        return services;
    }

    public static IEndpointRouteBuilder MapSubjectsModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Subjects");

        // Public catalog reads — Guests can browse (ROLES_AND_PERMISSIONS.md), no auth required.
        group.MapGet("/subjects", async (GetSubjectsFeature.Handler handler, CancellationToken ct) =>
                TypedResults.Ok(await handler.HandleAsync(ct)))
            .WithName("GetSubjects")
            .WithSummary("List active subjects, the top-level teachable catalog entries.");

        group.MapGet("/specialties", async (Guid? subjectId, GetSpecialtiesFeature.Handler handler, CancellationToken ct) =>
                TypedResults.Ok(await handler.HandleAsync(subjectId, ct)))
            .WithName("GetSpecialties")
            .WithSummary("List active specialties, optionally scoped to one subject.");

        group.MapGet("/languages", async (GetLanguagesFeature.Handler handler, CancellationToken ct) =>
                TypedResults.Ok(await handler.HandleAsync(ct)))
            .WithName("GetLanguages")
            .WithSummary("List the admin-enabled spoken-language reference list (ADR-0020).");

        return app;
    }
}
