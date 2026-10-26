namespace TutorFlow.Modules.Identity.Infrastructure;

/// <summary>
/// Google/Facebook OAuth client credentials — bound from configuration (user-secrets in
/// development, environment variables/a secret store in production), never hardcoded. Optional
/// at startup (unlike <see cref="JwtOptions"/>): an unconfigured provider simply fails the first
/// time it's actually challenged, which lets the app run in CI/local dev without real provider
/// credentials.
/// </summary>
public sealed class ExternalAuthOptions
{
    public const string SectionName = "Authentication";

    public ProviderCredentials Google { get; init; } = new();

    public ProviderCredentials Facebook { get; init; } = new();

    public sealed class ProviderCredentials
    {
        public string ClientId { get; init; } = string.Empty;
        public string ClientSecret { get; init; } = string.Empty;
    }
}
