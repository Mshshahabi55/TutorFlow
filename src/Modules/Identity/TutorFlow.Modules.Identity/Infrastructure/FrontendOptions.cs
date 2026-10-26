namespace TutorFlow.Modules.Identity.Infrastructure;

/// <summary>
/// The Next.js origin the external-login callback redirects back to after issuing a session —
/// deliberately a dedicated config key rather than reusing <c>Cors:AllowedOrigins[0]</c> (that
/// array exists for CORS, not for building a trusted redirect target; relying on array-index
/// semantics for an unrelated purpose would be implicit and fragile).
/// </summary>
public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    public required string BaseUrl { get; init; }
}
