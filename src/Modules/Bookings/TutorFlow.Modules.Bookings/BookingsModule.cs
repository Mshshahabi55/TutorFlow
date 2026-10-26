using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Bookings.Infrastructure;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Shared;
using Wolverine.EntityFrameworkCore;
using CancelBookingFeature = TutorFlow.Modules.Bookings.Features.CancelBooking.CancelBooking;
using CancelBookingForPaymentFailureFeature = TutorFlow.Modules.Bookings.Features.CancelBookingForPaymentFailure.CancelBookingForPaymentFailure;
using CheckoutBookingFeature = TutorFlow.Modules.Bookings.Features.CheckoutBooking.CheckoutBooking;
using CreateTrialHoldFeature = TutorFlow.Modules.Bookings.Features.CreateTrialHold.CreateTrialHold;
using GetBookingFeature = TutorFlow.Modules.Bookings.Features.GetBooking.GetBooking;
using GetMyBookingsAsStudentFeature = TutorFlow.Modules.Bookings.Features.GetMyBookingsAsStudent.GetMyBookingsAsStudent;
using GetMyBookingsAsTutorFeature = TutorFlow.Modules.Bookings.Features.GetMyBookingsAsTutor.GetMyBookingsAsTutor;
using GetTrialEligibilityFeature = TutorFlow.Modules.Bookings.Features.GetTrialEligibility.GetTrialEligibility;
using RescheduleBookingFeature = TutorFlow.Modules.Bookings.Features.RescheduleBooking.RescheduleBooking;

namespace TutorFlow.Modules.Bookings;

public static class BookingsModule
{
    public static IServiceCollection AddBookingsModule(this IServiceCollection services, IConfiguration config)
    {
        // A44: AddDbContextWithWolverineIntegration (not plain AddDbContext) wires this
        // DbContext into Wolverine's EF Core transactional middleware — required so handlers
        // like ConfirmBooking/BookingHoldExpirySweepService get atomic business-write + outbox
        // commits under opts.Policies.AutoApplyTransactions() (Program.cs). The schema name
        // registers the message-store naming for the module's own Ancillary store; the actual
        // store registration (connection string + MessageStoreRole.Ancillary) happens in
        // Program.cs's UseWolverine block, not here.
        services.AddDbContextWithWolverineIntegration<BookingsDbContext>(
            options => options.UseNpgsql(config.GetConnectionString("BookingsDb")),
            "bookings");

        services.AddOptions<BookingsOptions>()
            .Bind(config.GetSection("Bookings"))
            .ValidateOnStart();

        services.AddScoped<CreateTrialHoldFeature.Handler>();
        services.AddScoped<IValidator<CreateTrialHoldFeature.Command>, CreateTrialHoldFeature.Validator>();
        services.AddScoped<GetBookingFeature.Handler>();
        services.AddScoped<GetMyBookingsAsStudentFeature.Handler>();
        services.AddScoped<GetMyBookingsAsTutorFeature.Handler>();
        services.AddScoped<GetTrialEligibilityFeature.Handler>();
        services.AddHttpContextAccessor();
        services.AddHttpClient(PaymentsApiClient.HttpClientName, (sp, client) =>
        {
            // Same-process loopback (modular monolith, ADR-0019/ADR-0027) — a real HTTP call,
            // the same mechanism that survives unchanged if Payments is ever extracted into its
            // own deployable. Test fixtures override this client's primary handler to route to
            // their own in-memory TestServer instead of a real socket.
            var baseUrl = sp.GetRequiredService<IConfiguration>()["Payments:BaseUrl"] ?? "http://localhost:5148";
            client.BaseAddress = new Uri(baseUrl);
        });
        services.AddScoped<PaymentsApiClient>();

        services.AddScoped<CheckoutBookingFeature.Handler>();
        services.AddScoped<CancelBookingFeature.Handler>();
        services.AddScoped<CancelBookingForPaymentFailureFeature.Handler>();
        services.AddScoped<RescheduleBookingFeature.Handler>();

        // Webhook-driven transitions (ADR-0015 §4, §5) — no public endpoint maps to these;
        // registered so a future Payments webhook consumer, or a test simulating one directly,
        // can resolve them.
        services.AddScoped<Features.ConfirmBooking.ConfirmBooking.Handler>();
        services.AddScoped<Features.ApplyRefund.ApplyRefund.Handler>();

        // ADR-0029: the real, time-driven trigger for BookingHoldExpired — see
        // BookingHoldExpirySweepService's own doc comment for why the existing per-tutor lazy
        // sweep cannot serve this purpose. Registered as a singleton AND redirected as the
        // IHostedService instance (rather than plain AddHostedService<T>()) so tests can resolve
        // the same instance directly and drive SweepOnceAsync deterministically instead of
        // waiting on the real timer.
        services.AddSingleton<BookingHoldExpirySweepService>();
        services.AddHostedService(sp => sp.GetRequiredService<BookingHoldExpirySweepService>());

        // ADR-0014 correction: Bookings implements the reciprocal Shared query contract
        // Availability's browse-slots endpoint depends on via DI. No project reference to
        // TutorFlow.Modules.Availability is added by this registration.
        services.AddScoped<IActiveBookingIntervalsQuery, ActiveBookingIntervalsQuery>();

        // ADR-0019/ADR-0027: the reciprocal read-only query Payments depends on via DI for
        // checkout. No project reference to TutorFlow.Modules.Payments is added by this
        // registration.
        services.AddScoped<IBookingCheckoutEligibilityQuery, BookingCheckoutEligibilityQuery>();

        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.StudentOnly, p => p.RequireRole(RoleNames.Student))
            .AddPolicy(PolicyNames.TutorOnly, p => p.RequireRole(RoleNames.Tutor))
            .AddPolicy(PolicyNames.StudentOrTutorOnly, p => p.RequireRole(RoleNames.Student, RoleNames.Tutor));

        return services;
    }

    public static IEndpointRouteBuilder MapBookingsModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings").WithTags("Bookings");

        group.MapPost("/trial-holds", CreateTrialHoldAsync)
            .WithName("CreateTrialHold")
            .WithSummary("Create a Held booking for a trial slot.")
            .RequireAuthorization(PolicyNames.StudentOnly)
            .AddEndpointFilter<ValidationFilter<CreateTrialHoldFeature.Command>>();

        group.MapGet("/{id:guid}", GetBookingAsync)
            .WithName("GetBooking")
            .WithSummary("Read one booking the caller is a party to.")
            .RequireAuthorization(PolicyNames.StudentOrTutorOnly);

        group.MapGet("/me/as-student", GetMyBookingsAsStudentAsync)
            .WithName("GetMyBookingsAsStudent")
            .WithSummary("List the caller's bookings as a student.")
            .RequireAuthorization(PolicyNames.StudentOnly);

        group.MapGet("/me/as-tutor", GetMyBookingsAsTutorAsync)
            .WithName("GetMyBookingsAsTutor")
            .WithSummary("List the caller's bookings as a tutor.")
            .RequireAuthorization(PolicyNames.TutorOnly);

        group.MapGet("/me/trial-eligibility/{tutorUserId:guid}", GetTrialEligibilityAsync)
            .WithName("GetTrialEligibility")
            .WithSummary("Whether the caller has already used their one trial with this tutor (ADR-0022).")
            .RequireAuthorization(PolicyNames.StudentOnly);

        group.MapPost("/{id:guid}/checkout", CheckoutBookingAsync)
            .WithName("CheckoutBooking")
            .WithSummary("Transition a Held booking to PendingPayment and begin payment.")
            .RequireAuthorization(PolicyNames.StudentOnly);

        group.MapPost("/{id:guid}/cancel", CancelBookingAsync)
            .WithName("CancelBooking")
            .WithSummary("Cancel a booking from an allowed state.")
            .RequireAuthorization(PolicyNames.StudentOrTutorOnly);

        group.MapPost("/{id:guid}/reschedule", RescheduleBookingAsync)
            .WithName("RescheduleBooking")
            .WithSummary("Create a linked replacement booking for a new slot (ADR-0017).")
            .RequireAuthorization(PolicyNames.StudentOnly);

        return app;
    }

    private static async Task<IResult> CreateTrialHoldAsync(
        ClaimsPrincipal user,
        CreateTrialHoldFeature.Command command,
        CreateTrialHoldFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var result = await handler.HandleAsync(userId, command, ct);
            return result.IsSuccess
                ? TypedResults.Created($"/api/bookings/{result.Value.Id}", result.Value)
                : result.ToProblemDetails(StatusCodes.Status400BadRequest);
        }
        catch (SlotNotAvailableException ex)
        {
            return TypedResults.Problem(title: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> GetBookingAsync(
        ClaimsPrincipal user,
        Guid id,
        GetBookingFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var (outcome, response) = await handler.HandleAsync(userId, id, ct);
        return outcome switch
        {
            GetBookingFeature.Outcome.Found => TypedResults.Ok(response),
            GetBookingFeature.Outcome.Forbidden => TypedResults.Forbid(),
            _ => TypedResults.NotFound()
        };
    }

    private static async Task<IResult> GetMyBookingsAsStudentAsync(
        ClaimsPrincipal user,
        GetMyBookingsAsStudentFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await handler.HandleAsync(userId, ct));
    }

    private static async Task<IResult> GetMyBookingsAsTutorAsync(
        ClaimsPrincipal user,
        GetMyBookingsAsTutorFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await handler.HandleAsync(userId, ct));
    }

    private static async Task<IResult> GetTrialEligibilityAsync(
        ClaimsPrincipal user,
        Guid tutorUserId,
        GetTrialEligibilityFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await handler.HandleAsync(userId, tutorUserId, ct));
    }

    private static async Task<IResult> CheckoutBookingAsync(
        ClaimsPrincipal user,
        Guid id,
        CheckoutBookingFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var (outcome, response) = await handler.HandleAsync(userId, id, ct);
        return outcome switch
        {
            CheckoutBookingFeature.Outcome.Ok => TypedResults.Ok(response),
            CheckoutBookingFeature.Outcome.Forbidden => TypedResults.Forbid(),
            CheckoutBookingFeature.Outcome.NotFound => TypedResults.NotFound(),
            CheckoutBookingFeature.Outcome.Conflict => TypedResults.Problem(title: "Booking is not in a state that can begin checkout.", statusCode: StatusCodes.Status409Conflict),
            _ => TypedResults.Problem(
                title: "Payments is temporarily unavailable; please try checkout again shortly.",
                statusCode: StatusCodes.Status503ServiceUnavailable)
        };
    }

    private static async Task<IResult> CancelBookingAsync(
        ClaimsPrincipal user,
        Guid id,
        CancelBookingFeature.Command command,
        CancelBookingFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var (outcome, response) = await handler.HandleAsync(userId, id, command, ct);
        return outcome switch
        {
            CancelBookingFeature.Outcome.Ok => TypedResults.Ok(response),
            CancelBookingFeature.Outcome.Forbidden => TypedResults.Forbid(),
            CancelBookingFeature.Outcome.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Problem(title: "Booking cannot be cancelled from its current state.", statusCode: StatusCodes.Status409Conflict)
        };
    }

    private static async Task<IResult> RescheduleBookingAsync(
        ClaimsPrincipal user,
        Guid id,
        RescheduleBookingFeature.Command command,
        RescheduleBookingFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var (outcome, response, reason) = await handler.HandleAsync(userId, id, command, ct);
            return outcome switch
            {
                RescheduleBookingFeature.Outcome.Ok => TypedResults.Created($"/api/bookings/{response!.Id}", response),
                RescheduleBookingFeature.Outcome.Forbidden => TypedResults.Forbid(),
                RescheduleBookingFeature.Outcome.NotFound => TypedResults.NotFound(),
                RescheduleBookingFeature.Outcome.InvalidSlot => TypedResults.Problem(title: reason ?? "Requested slot is not available.", statusCode: StatusCodes.Status400BadRequest),
                RescheduleBookingFeature.Outcome.TooLate => TypedResults.Problem(title: reason ?? "Too close to the lesson start time to reschedule.", statusCode: StatusCodes.Status409Conflict),
                _ => TypedResults.Problem(title: "Booking cannot be rescheduled from its current state.", statusCode: StatusCodes.Status409Conflict)
            };
        }
        catch (SlotNotAvailableException ex)
        {
            return TypedResults.Problem(title: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
