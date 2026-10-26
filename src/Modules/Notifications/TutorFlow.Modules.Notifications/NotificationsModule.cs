using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Notifications.Infrastructure;
using TutorFlow.Modules.Notifications.Persistence;
using SendAbandonedCheckoutRecoveryEmailFeature = TutorFlow.Modules.Notifications.Features.SendAbandonedCheckoutRecoveryEmail.SendAbandonedCheckoutRecoveryEmail;
using SendCertificateRejectedEmailFeature = TutorFlow.Modules.Notifications.Features.SendCertificateRejectedEmail.SendCertificateRejectedEmail;

namespace TutorFlow.Modules.Notifications;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("NotificationsDb"), npgsql => npgsql.EnableRetryOnFailure()));

        // ADR-0029 Addendum A — ValidateOnStart alone validates nothing about the actual values
        // (Batch 50 finding); the explicit Validate predicates below are what actually rejects a
        // misconfigured value, and do so at options-access time (startup), not later inside
        // PeriodicTimer's constructor or deep in the reconciliation loop.
        services.AddOptions<NotificationsOptions>()
            .Bind(config.GetSection("Notifications"))
            .Validate(o => o.ClaimLeaseMinutes > 0, "Notifications:ClaimLeaseMinutes must be a positive integer.")
            .Validate(o => o.ClaimReconciliationIntervalSeconds > 0, "Notifications:ClaimReconciliationIntervalSeconds must be a positive integer.")
            .Validate(o => o.ClaimReconciliationBatchSize > 0, "Notifications:ClaimReconciliationBatchSize must be a positive integer.")
            .ValidateOnStart();

        services.AddHttpClient(PostmarkEmailSender.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://api.postmarkapp.com/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Real Postmark when a server token is configured; a deterministic test adapter
        // otherwise (ADR-0029, mirroring Payments' IPaymentProviderClient selection) — local
        // dev/test has no Postmark account. Selected once at registration time, purely an
        // infrastructure choice, never a product-logic branch.
        services.AddScoped<IEmailSender>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<NotificationsOptions>>().Value;
            return string.IsNullOrWhiteSpace(options.PostmarkServerToken)
                ? new TestEmailSender()
                : ActivatorUtilities.CreateInstance<PostmarkEmailSender>(sp);
        });

        services.AddScoped<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();
        services.AddScoped<SendCertificateRejectedEmailFeature.Handler>();

        // ADR-0029 Addendum A — registered as a resolvable singleton (not plain
        // AddHostedService<T>()) so tests can drive ReconcileOnceAsync deterministically,
        // matching BookingsModule's identical BookingHoldExpirySweepService registration.
        services.AddSingleton<AbandonedCheckoutClaimReconciliationService>();
        services.AddHostedService(sp => sp.GetRequiredService<AbandonedCheckoutClaimReconciliationService>());

        return services;
    }

    public static IEndpointRouteBuilder MapNotificationsModule(this IEndpointRouteBuilder app)
    {
        return app;
    }
}
