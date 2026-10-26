namespace TutorFlow.Modules.Identity.Infrastructure;

public static class ExternalLoginConstants
{
    /// <summary>
    /// Temporary cookie scheme that holds the authenticated external principal only between the
    /// OAuth redirect round-trip and this app's own callback endpoint reading it — never the
    /// final session (that's the existing JWT-carrying <see cref="AuthCookie"/>). Short-lived and
    /// explicitly signed out of immediately after the callback reads it.
    /// </summary>
    public const string TemporaryScheme = "ExternalLogin";
}
