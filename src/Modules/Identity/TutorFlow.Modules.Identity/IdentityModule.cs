using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Identity.Features.CurrentUser;
using TutorFlow.Modules.Identity.Infrastructure;
using TutorFlow.Modules.Identity.Persistence;
using TutorFlow.Shared;
using BecomeTutorFeature = TutorFlow.Modules.Identity.Features.BecomeTutor.BecomeTutor;
using LoginFeature = TutorFlow.Modules.Identity.Features.Login.Login;
using RegisterFeature = TutorFlow.Modules.Identity.Features.Register.Register;

namespace TutorFlow.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("IdentityDb")));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddOptions<JwtOptions>()
            .Bind(config.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer must be configured.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience must be configured.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Key), "Jwt:Key must be configured.")
            .Validate(o => o.ExpirationMinutes > 0, "Jwt:ExpirationMinutes must be greater than zero.")
            .ValidateOnStart();

        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<IValidator<RegisterFeature.Command>, RegisterFeature.Validator>();
        services.AddScoped<RegisterFeature.Handler>();

        services.AddScoped<IValidator<LoginFeature.Command>, LoginFeature.Validator>();
        services.AddScoped<LoginFeature.Handler>();

        services.AddScoped<BecomeTutorFeature.Handler>();

        // ADR-0029: the reciprocal read-only query Notifications depends on via DI to resolve a
        // recipient's email address server-side. No project reference to
        // TutorFlow.Modules.Notifications is added by this registration.
        services.AddScoped<IUserEmailQuery, UserEmailQuery>();

        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.StudentOnly, p => p.RequireRole(RoleNames.Student))
            .AddPolicy(PolicyNames.TutorOnly, p => p.RequireRole(RoleNames.Tutor))
            .AddPolicy(PolicyNames.AdminOnly, p => p.RequireRole(RoleNames.Admin));

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity").WithTags("Identity");

        group.MapPost("/register", RegisterAsync)
            .WithName("RegisterUser")
            .WithSummary("Register a new account with the default Student role.")
            .AddEndpointFilter<ValidationFilter<RegisterFeature.Command>>()
            .RequireRateLimiting("auth-register")
            .AllowAnonymous();

        group.MapPost("/login", LoginAsync)
            .WithName("LoginUser")
            .WithSummary("Authenticate and receive a JWT access token.")
            .AddEndpointFilter<ValidationFilter<LoginFeature.Command>>()
            .RequireRateLimiting("auth-login")
            .AllowAnonymous();

        group.MapGet("/me", GetMe)
            .WithName("GetCurrentUser")
            .WithSummary("Return the authenticated user's identity and roles.")
            .RequireAuthorization();

        group.MapPost("/logout", Logout)
            .WithName("LogoutUser")
            .WithSummary("Clear the session cookie.")
            .AllowAnonymous();

        group.MapPost("/me/become-tutor", BecomeTutorAsync)
            .WithName("BecomeTutor")
            .WithSummary("Grant the caller's own account the Tutor role, idempotently (ADR-0026).")
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterFeature.Command command,
        RegisterFeature.Handler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);
        if (result.IsFailure)
        {
            return result.ToProblemDetails(StatusCodes.Status400BadRequest);
        }

        AuthCookie.Issue(httpContext, result.Value.AccessToken, result.Value.ExpiresAtUtc);

        var session = new RegisterFeature.SessionResponse(
            result.Value.UserId, result.Value.Email, result.Value.Roles, result.Value.ExpiresAtUtc);
        return TypedResults.Created($"/api/identity/users/{session.UserId}", session);
    }

    private static async Task<IResult> LoginAsync(
        LoginFeature.Command command,
        LoginFeature.Handler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);
        if (result.IsFailure)
        {
            return result.ToProblemDetails(StatusCodes.Status401Unauthorized);
        }

        AuthCookie.Issue(httpContext, result.Value.AccessToken, result.Value.ExpiresAtUtc);

        var session = new LoginFeature.SessionResponse(
            result.Value.UserId, result.Value.Email, result.Value.Roles, result.Value.ExpiresAtUtc);
        return TypedResults.Ok(session);
    }

    private static IResult GetMe(ClaimsPrincipal user)
    {
        var result = GetCurrentUser.FromPrincipal(user);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status401Unauthorized);
    }

    private static IResult Logout(HttpContext httpContext)
    {
        AuthCookie.Clear(httpContext);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> BecomeTutorAsync(
        ClaimsPrincipal user,
        BecomeTutorFeature.Handler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var current = GetCurrentUser.FromPrincipal(user);
        if (current.IsFailure)
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(current.Value.UserId, ct);
        if (result.IsFailure)
        {
            return result.ToProblemDetails(StatusCodes.Status400BadRequest);
        }

        AuthCookie.Issue(httpContext, result.Value.AccessToken, result.Value.ExpiresAtUtc);

        var session = new BecomeTutorFeature.SessionResponse(
            result.Value.UserId, result.Value.Email, result.Value.Roles, result.Value.ExpiresAtUtc);
        return TypedResults.Ok(session);
    }
}
