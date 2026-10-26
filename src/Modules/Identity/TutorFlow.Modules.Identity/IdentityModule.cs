using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Identity.Features.CurrentUser;
using TutorFlow.Modules.Identity.Infrastructure;
using TutorFlow.Modules.Identity.Persistence;
using TutorFlow.Shared;
using BecomeTutorFeature = TutorFlow.Modules.Identity.Features.BecomeTutor.BecomeTutor;
using CompleteExternalLoginFeature = TutorFlow.Modules.Identity.Features.ExternalLogin.CompleteExternalLogin;
using LoginFeature = TutorFlow.Modules.Identity.Features.Login.Login;
using RegisterFeature = TutorFlow.Modules.Identity.Features.Register.Register;
using GetProfileFeature = TutorFlow.Modules.Identity.Features.Profile.GetProfile;
using UpdateProfileFeature = TutorFlow.Modules.Identity.Features.Profile.UpdateProfile;

namespace TutorFlow.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("IdentityDb"), npgsql => npgsql.EnableRetryOnFailure()));

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

        // Unlike JwtOptions above, deliberately not ValidateOnStart — an unconfigured provider
        // (no real Google/Facebook credentials yet) must still let the app start cleanly in
        // CI/local dev; it only fails the first time that specific provider is actually
        // challenged, which is the correct place for that failure to surface.
        services.AddOptions<ExternalAuthOptions>()
            .Bind(config.GetSection(ExternalAuthOptions.SectionName));

        // Deliberately not ValidateOnStart: every module's tests build this same real
        // Program.cs host via WebApplicationFactory<Program> (TutorsApiFixture,
        // BookingsApiFixture, etc.) — failing startup here would break all of them, not just
        // Identity's. An empty BaseUrl only matters at the one call site that actually builds a
        // redirect from it (HandleExternalLoginCallbackAsync), which guards against it directly.
        services.AddOptions<FrontendOptions>()
            .Bind(config.GetSection(FrontendOptions.SectionName));

        // Applies the bound ExternalAuthOptions to the schemes Program.cs registers via bare
        // .AddGoogle()/.AddFacebook() calls — same two-step pattern already used for
        // JwtBearerOptions below (Host owns AddAuthentication()'s scheme chain; this module owns
        // binding/validating the configuration that feeds it, since it's the Identity module's
        // own concern).
        services.AddOptions<GoogleOptions>(GoogleDefaults.AuthenticationScheme)
            .Configure<IOptions<ExternalAuthOptions>>((options, externalAuth) =>
            {
                options.ClientId = externalAuth.Value.Google.ClientId;
                options.ClientSecret = externalAuth.Value.Google.ClientSecret;
                options.SignInScheme = ExternalLoginConstants.TemporaryScheme;
                options.CallbackPath = "/api/identity/external/google/signin";
                options.Scope.Add("email");
                // Google's UserInfo endpoint includes this field; not mapped by the package's own
                // defaults, so it must be requested explicitly to enforce approved decision #5
                // (reject an unverified email rather than trusting presence alone).
                options.ClaimActions.MapJsonKey("email_verified", "email_verified");
            });

        services.AddOptions<FacebookOptions>(FacebookDefaults.AuthenticationScheme)
            .Configure<IOptions<ExternalAuthOptions>>((options, externalAuth) =>
            {
                options.AppId = externalAuth.Value.Facebook.ClientId;
                options.AppSecret = externalAuth.Value.Facebook.ClientSecret;
                options.SignInScheme = ExternalLoginConstants.TemporaryScheme;
                options.CallbackPath = "/api/identity/external/facebook/signin";
                // Facebook never returns an email at all unless it already confirmed one for
                // that account (documented Facebook Login behavior) — both the permission grant
                // (Scope) and the profile-field request (Fields) are required for the email to
                // appear in the response at all.
                options.Scope.Add("email");
                options.Fields.Add("email");
            });

        services.AddOptions<CookieAuthenticationOptions>(ExternalLoginConstants.TemporaryScheme)
            .Configure(options =>
            {
                options.Cookie.Name = "TutorFlowExternalLogin";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                // Lax, not None: the browser reaches this cookie only via a top-level GET
                // navigation (the provider redirecting back to us), which Lax already permits —
                // None would need SameSite=None's own CSRF considerations for no actual benefit.
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            });

        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<IValidator<RegisterFeature.Command>, RegisterFeature.Validator>();
        services.AddScoped<RegisterFeature.Handler>();

        services.AddScoped<IValidator<LoginFeature.Command>, LoginFeature.Validator>();
        services.AddScoped<LoginFeature.Handler>();

        services.AddScoped<BecomeTutorFeature.Handler>();

        services.AddScoped<GetProfileFeature.Handler>();

        services.AddScoped<IValidator<UpdateProfileFeature.Command>, UpdateProfileFeature.Validator>();
        services.AddScoped<UpdateProfileFeature.Handler>();

        services.AddScoped<CompleteExternalLoginFeature.Handler>();

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

        group.MapGet("/me/profile", GetProfileAsync)
            .WithName("GetProfile")
            .WithSummary("Return the caller's own First/Last name and email.")
            .RequireAuthorization();

        group.MapPatch("/me/profile", UpdateProfileAsync)
            .WithName("UpdateProfile")
            .WithSummary("Set the caller's own First and/or Last name.")
            .AddEndpointFilter<ValidationFilter<UpdateProfileFeature.Command>>()
            .RequireAuthorization();

        group.MapGet("/external/google", (IAuthenticationSchemeProvider schemeProvider, string? returnTo) =>
                ChallengeExternalLoginAsync(schemeProvider, GoogleDefaults.AuthenticationScheme, "/api/identity/external/google/callback", returnTo))
            .WithName("ChallengeGoogleLogin")
            .WithSummary("Start the Google sign-in redirect.")
            .AllowAnonymous();

        group.MapGet("/external/google/callback", (
                HttpContext httpContext,
                CompleteExternalLoginFeature.Handler handler,
                IOptions<FrontendOptions> frontendOptions,
                string? returnTo,
                CancellationToken ct) =>
            HandleExternalLoginCallbackAsync(
                GoogleDefaults.AuthenticationScheme, httpContext, handler, frontendOptions, returnTo, ct))
            .WithName("GoogleLoginCallback")
            .AllowAnonymous();

        group.MapGet("/external/facebook", (IAuthenticationSchemeProvider schemeProvider, string? returnTo) =>
                ChallengeExternalLoginAsync(schemeProvider, FacebookDefaults.AuthenticationScheme, "/api/identity/external/facebook/callback", returnTo))
            .WithName("ChallengeFacebookLogin")
            .WithSummary("Start the Facebook sign-in redirect.")
            .AllowAnonymous();

        group.MapGet("/external/facebook/callback", (
                HttpContext httpContext,
                CompleteExternalLoginFeature.Handler handler,
                IOptions<FrontendOptions> frontendOptions,
                string? returnTo,
                CancellationToken ct) =>
            HandleExternalLoginCallbackAsync(
                FacebookDefaults.AuthenticationScheme, httpContext, handler, frontendOptions, returnTo, ct))
            .WithName("FacebookLoginCallback")
            .AllowAnonymous();

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

    private static async Task<IResult> GetProfileAsync(
        ClaimsPrincipal user,
        GetProfileFeature.Handler handler,
        CancellationToken ct)
    {
        var current = GetCurrentUser.FromPrincipal(user);
        if (current.IsFailure)
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(current.Value.UserId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> UpdateProfileAsync(
        ClaimsPrincipal user,
        UpdateProfileFeature.Command command,
        UpdateProfileFeature.Handler handler,
        CancellationToken ct)
    {
        var current = GetCurrentUser.FromPrincipal(user);
        if (current.IsFailure)
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(current.Value.UserId, command, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// <paramref name="returnTo"/> is re-sanitized here (not just at the frontend's own link
    /// construction) because this value is about to round-trip through an external OAuth
    /// provider and back — <see cref="ExternalLoginReturnTo"/> is the one place that validates
    /// it. The provider itself never interprets <see cref="AuthenticationProperties.RedirectUri"/>;
    /// it is framework-internal state, encrypted in the correlation cookie, used only by this
    /// app's own middleware once the handshake completes.
    /// </summary>
    /// <summary>
    /// Checks the scheme is actually registered before challenging it — <c>Program.cs</c> only
    /// registers Google/Facebook when real credentials are configured (an unconfigured provider
    /// registered with empty credentials breaks every request in the app, not just this one; see
    /// the comment there), so an unconfigured provider's scheme genuinely does not exist here.
    /// Challenging a non-existent scheme throws; this turns that into a clean response instead.
    /// </summary>
    private static async Task<IResult> ChallengeExternalLoginAsync(
        IAuthenticationSchemeProvider schemeProvider, string scheme, string callbackPath, string? returnTo)
    {
        if (await schemeProvider.GetSchemeAsync(scheme) is null)
        {
            return TypedResults.Problem(title: $"{scheme} sign-in is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var safeReturnTo = ExternalLoginReturnTo.Sanitize(returnTo);
        var properties = new AuthenticationProperties
        {
            RedirectUri = $"{callbackPath}?returnTo={Uri.EscapeDataString(safeReturnTo)}"
        };
        return TypedResults.Challenge(properties, [scheme]);
    }

    /// <summary>
    /// Shared by both providers' callback routes. Reads the principal the provider-specific
    /// handler already authenticated and stored in <see cref="ExternalLoginConstants.TemporaryScheme"/>
    /// (framework-handled — see each <c>CallbackPath</c> configured in <c>AddIdentityModule</c>;
    /// no OAuth protocol logic is hand-rolled here), signs that temporary principal out
    /// immediately, then hands the extracted claims to <see cref="CompleteExternalLoginFeature.Handler"/>
    /// for the actual account-resolution decision. Every exit path is a redirect to the frontend
    /// — never a JSON body containing the token, and the token itself only ever reaches the
    /// browser via <see cref="AuthCookie.Issue"/>'s Set-Cookie header, never the redirect URL.
    /// </summary>
    private static async Task<IResult> HandleExternalLoginCallbackAsync(
        string provider,
        HttpContext httpContext,
        CompleteExternalLoginFeature.Handler handler,
        IOptions<FrontendOptions> frontendOptions,
        string? returnTo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(frontendOptions.Value.BaseUrl))
        {
            // Nothing safe to redirect to — a missing Frontend:BaseUrl is a deployment
            // configuration gap, not a per-request condition, so this reports it as a server
            // error rather than attempting a malformed redirect.
            return TypedResults.Problem(title: "External sign-in is not configured.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var safeReturnTo = ExternalLoginReturnTo.Sanitize(returnTo);
        var frontendBaseUrl = frontendOptions.Value.BaseUrl.TrimEnd('/');

        var authResult = await httpContext.AuthenticateAsync(ExternalLoginConstants.TemporaryScheme);
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            // Covers both an actual provider error and the user cancelling on the
            // provider's own consent screen — ASP.NET Core's remote-auth handlers surface both
            // the same way at this point, and neither case should expose provider-internal detail.
            return TypedResults.Redirect($"{frontendBaseUrl}/login?externalAuthError=cancelled");
        }

        await httpContext.SignOutAsync(ExternalLoginConstants.TemporaryScheme);

        var principal = authResult.Principal;
        var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(providerKey))
        {
            return TypedResults.Redirect($"{frontendBaseUrl}/login?externalAuthError=provider_error");
        }

        // Google explicitly marks verification via its own `email_verified` claim (mapped in
        // AddIdentityModule); Facebook's API never returns an email at all unless it already
        // confirmed one for that account, so presence alone is Facebook's own verification gate.
        var emailVerified = provider == GoogleDefaults.AuthenticationScheme
            ? string.Equals(principal.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase)
            : !string.IsNullOrWhiteSpace(email);

        var outcome = await handler.HandleAsync(new CompleteExternalLoginFeature.Command(provider, providerKey, email, emailVerified), ct);

        return outcome switch
        {
            CompleteExternalLoginFeature.Outcome.Success success => IssueSessionAndRedirect(httpContext, success, frontendBaseUrl, safeReturnTo),
            CompleteExternalLoginFeature.Outcome.EmailRequired => TypedResults.Redirect($"{frontendBaseUrl}/login?externalAuthError=email_required"),
            CompleteExternalLoginFeature.Outcome.EmailConflict => TypedResults.Redirect($"{frontendBaseUrl}/login?externalAuthError=email_conflict"),
            _ => TypedResults.Redirect($"{frontendBaseUrl}/login?externalAuthError=failed")
        };
    }

    private static IResult IssueSessionAndRedirect(
        HttpContext httpContext, CompleteExternalLoginFeature.Outcome.Success success, string frontendBaseUrl, string safeReturnTo)
    {
        AuthCookie.Issue(httpContext, success.Session.AccessToken, success.Session.ExpiresAtUtc);
        return TypedResults.Redirect($"{frontendBaseUrl}{safeReturnTo}");
    }
}
