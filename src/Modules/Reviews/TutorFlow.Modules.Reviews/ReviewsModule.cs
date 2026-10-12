using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Reviews.Infrastructure;
using TutorFlow.Modules.Reviews.Persistence;
using GetReviewEligibilityFeature = TutorFlow.Modules.Reviews.Features.GetReviewEligibility.GetReviewEligibility;
using GetSubcategoryRatingEligibilityFeature = TutorFlow.Modules.Reviews.Features.GetSubcategoryRatingEligibility.GetSubcategoryRatingEligibility;
using GetTutorReviewSummaryFeature = TutorFlow.Modules.Reviews.Features.GetTutorReviewSummary.GetTutorReviewSummary;
using SubmitReviewFeature = TutorFlow.Modules.Reviews.Features.SubmitReview.SubmitReview;
using SubmitSubcategoryRatingFeature = TutorFlow.Modules.Reviews.Features.SubmitSubcategoryRating.SubmitSubcategoryRating;

namespace TutorFlow.Modules.Reviews;

public static class ReviewsModule
{
    public static IServiceCollection AddReviewsModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ReviewsDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("ReviewsDb")));

        services.AddScoped<GetReviewEligibilityFeature.Handler>();
        services.AddScoped<SubmitReviewFeature.Handler>();
        services.AddScoped<IValidator<SubmitReviewFeature.Command>, SubmitReviewFeature.Validator>();
        services.AddScoped<GetTutorReviewSummaryFeature.Handler>();
        services.AddScoped<GetSubcategoryRatingEligibilityFeature.Handler>();
        services.AddScoped<SubmitSubcategoryRatingFeature.Handler>();
        services.AddScoped<IValidator<SubmitSubcategoryRatingFeature.Command>, SubmitSubcategoryRatingFeature.Validator>();

        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.StudentOnly, p => p.RequireRole(RoleNames.Student));

        return services;
    }

    public static IEndpointRouteBuilder MapReviewsModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reviews").WithTags("Reviews");

        group.MapGet("/tutors/{tutorUserId:guid}/summary", GetTutorReviewSummaryAsync)
            .WithName("GetTutorReviewSummary")
            .WithSummary("Public review summary for a tutor's profile. No authentication required.");

        group.MapGet("/tutors/{tutorUserId:guid}/me/eligibility", GetReviewEligibilityAsync)
            .WithName("GetReviewEligibility")
            .WithSummary("Whether the caller is eligible to review this tutor, and their existing review if any.")
            .RequireAuthorization(PolicyNames.StudentOnly);

        group.MapPut("/tutors/{tutorUserId:guid}/me", SubmitReviewAsync)
            .WithName("SubmitReview")
            .WithSummary("Create or update the caller's review of this tutor.")
            .RequireAuthorization(PolicyNames.StudentOnly)
            .AddEndpointFilter<ValidationFilter<SubmitReviewFeature.Command>>();

        group.MapGet("/tutors/{tutorUserId:guid}/me/subcategory-eligibility", GetSubcategoryRatingEligibilityAsync)
            .WithName("GetSubcategoryRatingEligibility")
            .WithSummary("Whether the caller can submit subcategory ratings for this tutor this month (DOCUMENTED: once per month, after >=1 completed lesson).")
            .RequireAuthorization(PolicyNames.StudentOnly);

        group.MapPost("/tutors/{tutorUserId:guid}/me/subcategory-ratings", SubmitSubcategoryRatingAsync)
            .WithName("SubmitSubcategoryRating")
            .WithSummary("Submit (never edit) this month's optional Clarity/Progress/Preparation/Reassurance ratings for this tutor.")
            .RequireAuthorization(PolicyNames.StudentOnly)
            .AddEndpointFilter<ValidationFilter<SubmitSubcategoryRatingFeature.Command>>();

        return app;
    }

    private static async Task<IResult> GetTutorReviewSummaryAsync(
        Guid tutorUserId,
        GetTutorReviewSummaryFeature.Handler handler,
        CancellationToken ct)
    {
        return TypedResults.Ok(await handler.HandleAsync(tutorUserId, ct));
    }

    private static async Task<IResult> GetReviewEligibilityAsync(
        ClaimsPrincipal user,
        Guid tutorUserId,
        GetReviewEligibilityFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await handler.HandleAsync(userId, tutorUserId, ct));
    }

    private static async Task<IResult> SubmitReviewAsync(
        ClaimsPrincipal user,
        Guid tutorUserId,
        SubmitReviewFeature.Command command,
        SubmitReviewFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var (outcome, response) = await handler.HandleAsync(userId, tutorUserId, command, ct);
        return outcome switch
        {
            SubmitReviewFeature.Outcome.Created => TypedResults.Created($"/api/reviews/tutors/{tutorUserId}/me", response),
            SubmitReviewFeature.Outcome.Updated => TypedResults.Ok(response),
            _ => TypedResults.Problem(
                title: "Not eligible to review this tutor yet.",
                detail: $"You need {GetReviewEligibilityFeature.RequiredCompletedLessonCount} completed lessons with this tutor before you can leave a review.",
                statusCode: StatusCodes.Status403Forbidden)
        };
    }

    private static async Task<IResult> GetSubcategoryRatingEligibilityAsync(
        ClaimsPrincipal user,
        Guid tutorUserId,
        GetSubcategoryRatingEligibilityFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await handler.HandleAsync(userId, tutorUserId, ct));
    }

    private static async Task<IResult> SubmitSubcategoryRatingAsync(
        ClaimsPrincipal user,
        Guid tutorUserId,
        SubmitSubcategoryRatingFeature.Command command,
        SubmitSubcategoryRatingFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var (outcome, response) = await handler.HandleAsync(userId, tutorUserId, command, ct);
        return outcome switch
        {
            SubmitSubcategoryRatingFeature.Outcome.Created => TypedResults.Created($"/api/reviews/tutors/{tutorUserId}/summary", response),
            SubmitSubcategoryRatingFeature.Outcome.AlreadySubmittedThisMonth => TypedResults.Problem(
                title: "Already submitted this month.",
                detail: "You can submit subcategory ratings for this tutor once per calendar month.",
                statusCode: StatusCodes.Status409Conflict),
            _ => TypedResults.Problem(
                title: "Not eligible to rate this tutor yet.",
                detail: "You need at least one completed lesson with this tutor first.",
                statusCode: StatusCodes.Status403Forbidden)
        };
    }
}
