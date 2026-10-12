using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Notifications.Infrastructure;
using TutorFlow.Modules.Notifications.Persistence;
using SendAbandonedCheckoutRecoveryEmailFeature = TutorFlow.Modules.Notifications.Features.SendAbandonedCheckoutRecoveryEmail.SendAbandonedCheckoutRecoveryEmail;

namespace TutorFlow.Modules.Notifications;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("NotificationsDb")));

        services.AddOptions<NotificationsOptions>().Bind(config.GetSection("Notifications"));

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

        return services;
    }

    public static IEndpointRouteBuilder MapNotificationsModule(this IEndpointRouteBuilder app)
    {
        return app;
    }
}
