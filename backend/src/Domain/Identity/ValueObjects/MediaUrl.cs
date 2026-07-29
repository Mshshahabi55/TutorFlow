namespace TutorFlow.Domain.Identity.ValueObjects;

// ADR-024 Media section (added post-acceptance, Merge Readiness audit High
// 1). A Tutor pastes in a URL pointing at content hosted elsewhere — there
// is no upload pipeline to validate that content actually exists or is
// safe. The one invariant TutorFlow can and must enforce is the *shape* of
// the string: only http/https, so a Tutor can never persist a scheme
// (javascript:, data:, file:, ftp:, ...) that would be unsafe the moment
// any future screen renders it as an <img>/<a>/<video> src. Shared between
// Tutor (the enforcement point) and the Application-layer media validator
// (early rejection) so both layers apply exactly the same rule.
public static class MediaUrl
{
    public static bool IsValid(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
