using System.Text;

namespace TutorFlow.Modules.Tutors.Domain;

// Pure domain helper — the Id-derived suffix (see TutorApplication.Submit) makes the result
// unique by construction, so no DB round-trip or uniqueness check is ever needed here or at
// any call site (ADR-0022). Deliberately lives in Domain, not Infrastructure: slug assignment
// is a domain invariant of Submit(), not a side effect layered on top of it.
internal static class SlugGenerator
{
    public static string Slugify(string displayName)
    {
        var lowered = displayName.Trim().ToLowerInvariant();
        var builder = new StringBuilder(lowered.Length);
        var lastWasHyphen = false;

        foreach (var ch in lowered)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && builder.Length > 0)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length == 0 ? "tutor" : slug;
    }
}
