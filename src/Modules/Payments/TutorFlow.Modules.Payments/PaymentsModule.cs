using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stripe;
using TutorFlow.Modules.Payments.Infrastructure;
using TutorFlow.Modules.Payments.Persistence;
using TutorFlow.Shared;
using CreatePaymentIntentFeature = TutorFlow.Modules.Payments.Features.CreatePaymentIntent.CreatePaymentIntent;
using ProcessStripeWebhookFeature = TutorFlow.Modules.Payments.Features.ProcessStripeWebhook.ProcessStripeWebhook;

namespace TutorFlow.Modules.Payments;

public static class PaymentsModule
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<PaymentsDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("PaymentsDb")));

        services.AddOptions<PaymentsOptions>().Bind(config.GetSection("Payments"));

        // Real Stripe when a secret key is configured; a deterministic test adapter otherwise
        // (ADR-0027 §3) — local dev/test has no Stripe account. Selected once at registration
        // time, purely an infrastructure choice, never a product-logic branch.
        services.AddScoped<IPaymentProviderClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<PaymentsOptions>>().Value;
            return string.IsNullOrWhiteSpace(options.StripeSecretKey)
                ? new TestPaymentProviderClient()
                : new StripePaymentProviderClient(options.StripeSecretKey);
        });

        services.AddScoped<CreatePaymentIntentFeature.Handler>();
        services.AddScoped<IValidator<CreatePaymentIntentFeature.Command>, CreatePaymentIntentFeature.Validator>();
        services.AddScoped<ProcessStripeWebhookFeature.Handler>();

        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.StudentOnly, p => p.RequireRole(RoleNames.Student));

        return services;
    }

    public static IEndpointRouteBuilder MapPaymentsModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments");

        group.MapPost("/intents", CreatePaymentIntentAsync)
            .WithName("CreatePaymentIntent")
            .WithSummary("Create-or-retrieve the PaymentIntent for a PendingPayment booking. Called by Bookings, never the frontend directly (ADR-0019).")
            .RequireAuthorization(PolicyNames.StudentOnly)
            .AddEndpointFilter<ValidationFilter<CreatePaymentIntentFeature.Command>>();

        // Unauthenticated in the JWT sense — "authentication" is the Stripe signature, not
        // [Authorize] (ADR-0018 §3).
        group.MapPost("/webhooks/stripe", ProcessStripeWebhookAsync)
            .WithName("ProcessStripeWebhook")
            .WithSummary("Receive and process Stripe webhook events.")
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> CreatePaymentIntentAsync(
        ClaimsPrincipal user,
        CreatePaymentIntentFeature.Command command,
        CreatePaymentIntentFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var (outcome, response, reason) = await handler.HandleAsync(userId, command, ct);
        return outcome switch
        {
            CreatePaymentIntentFeature.Outcome.Created => TypedResults.Ok(response),
            _ => TypedResults.Problem(title: reason ?? "Booking is not eligible for checkout.", statusCode: StatusCodes.Status409Conflict)
        };
    }

    private static async Task<IResult> ProcessStripeWebhookAsync(
        HttpRequest request,
        IOptions<PaymentsOptions> options,
        ProcessStripeWebhookFeature.Handler handler,
        TimeProvider clock,
        CancellationToken ct)
    {
        var webhookSecret = options.Value.StripeWebhookSigningSecret;
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            // No webhook secret configured means no real Stripe webhook can ever legitimately
            // arrive here (local dev/test) — an honest "not configured" response, never a
            // bypass of signature verification.
            return TypedResults.Problem(title: "Stripe webhooks are not configured in this environment.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        using var reader = new StreamReader(request.Body);
        var json = await reader.ReadToEndAsync(ct);
        var signature = request.Headers["Stripe-Signature"];

        Event stripeEvent;
        try
        {
            // throwOnApiVersionMismatch: false — a correctly-signed event from a real Stripe
            // account whose configured webhook API version differs from whatever this Stripe.net
            // SDK version happens to expect must never be rejected as if it had a bad signature
            // (Stripe's own recommendation, surfaced directly in the exception message this
            // avoids). The signature check itself still throws, unconditionally, on any genuine
            // mismatch/forgery (ADR-0018 §3) — this only widens what a *valid* signature accepts.
            stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            return TypedResults.BadRequest();
        }

        var paymentIntentId = stripeEvent.Data.Object is PaymentIntent intent ? intent.Id : null;
        var failureReason = stripeEvent.Data.Object is PaymentIntent { LastPaymentError: not null } failedIntent
            ? failedIntent.LastPaymentError.Message
            : null;

        // Every outcome here (including Conflict/PaymentNotFound/Unrecognized) is acknowledged
        // with 200 — none of them are transient failures Stripe redelivery would resolve; a
        // genuine transient failure (DB unavailable, etc.) propagates as an unhandled exception
        // and surfaces as a real 5xx, which Stripe does redeliver (ADR-0018 §3).
        await handler.ProcessEventAsync(stripeEvent.Id, stripeEvent.Type, paymentIntentId, failureReason, json, ct);
        return TypedResults.Ok();
    }
}
