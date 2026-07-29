using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using TutorFlow.Application.Common;
using TutorFlow.Web.Endpoints;
using TutorFlow.Web.RateLimiting;

namespace TutorFlow.Web.DependencyInjection;

public static class RateLimitingRegistration
{
    /// <summary>The named policy applied to POST /auth/login (see RateLimitingSettings.Auth).</summary>
    public const string AuthPolicyName = "auth";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // IOptions, not a one-time Get<T>() snapshot at registration time
        // (matches MeetingProviderSettings' own pattern) — deliberately, not
        // just for style: the partitioner delegates below run at
        // request-handling time (long after host startup), and resolve this
        // fresh from HttpContext.RequestServices each time a *new* partition
        // key is first seen, so a test host's post-registration
        // ConfigureAppConfiguration override (TutorFlowWebApplicationFactory)
        // is correctly visible. A one-time read of `configuration` at
        // AddApiRateLimiting's own call site in Program.cs would have
        // captured whatever was resolvable at that specific point in the
        // minimal-hosting-model startup sequence, which is not guaranteed to
        // already include every config source a test host layers in.
        services.AddSingleton<IValidateOptions<RateLimitingSettings>, RateLimitingSettingsValidator>();
        services.AddOptions<RateLimitingSettings>()
            .BindConfiguration(RateLimitingSettings.SectionName)
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Applies to every request regardless of endpoint — partitioned
            // by client IP so one abusive caller cannot exhaust another's
            // budget. QueueLimit 0: a rejected request fails immediately
            // rather than waiting for a future window, since there is no
            // product requirement for request queuing/smoothing here.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var settings = Settings(context);
                return RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.General.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.General.WindowSeconds),
                        QueueLimit = 0,
                    });
            });

            // Enforced in addition to the GlobalLimiter above (ASP.NET Core
            // applies both when an endpoint opts into a named policy via
            // RequireRateLimiting) — a login attempt must clear the general
            // budget and the tighter auth-specific one.
            options.AddPolicy(AuthPolicyName, context =>
            {
                var settings = Settings(context);
                return RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Auth.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Auth.WindowSeconds),
                        QueueLimit = 0,
                    });
            });

            // Response shape mirrors GlobalExceptionHandler/ResultMapping.ToApiResult()
            // so a caller sees the same ApiResponse envelope regardless of
            // which layer rejected the request. ErrorType.Infrastructure is
            // the closest existing fit (docs/adr/ADR-008-error-handling.md's
            // Domain/Infrastructure/Authorization taxonomy) — a rate limit is
            // not a business-rule rejection about request content (Domain)
            // and not an ownership/permission failure (Authorization); no
            // fourth category is introduced.
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.ContentType = "application/json";

                var body = ApiResponse.Fail(new ApiError(
                    "RateLimit.TooManyRequests",
                    "Too many requests. Please try again later.",
                    ErrorType.Infrastructure,
                    context.HttpContext.TraceIdentifier));

                await context.HttpContext.Response.WriteAsJsonAsync(body, cancellationToken);
            };
        });

        return services;
    }

    private static RateLimitingSettings Settings(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<RateLimitingSettings>>().Value;

    // RemoteIpAddress is null only for a small set of non-HTTP or
    // synthetic-connection scenarios (TutorFlowWebApplicationFactory's
    // in-memory TestServer among them) — those all share one "unknown"
    // partition rather than being unlimited, so a limiter is still
    // meaningfully in effect for them.
    private static string ClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
