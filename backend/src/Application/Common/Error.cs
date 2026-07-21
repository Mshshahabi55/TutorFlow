namespace TutorFlow.Application.Common;

// A generic error shape only — no specific code or message belongs here,
// since those are owned by whichever bounded context's Domain layer raised
// the rejection (docs/adr/ADR-002-domain-boundaries.md: Rules That Prevent
// Business Logic Leakage).
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
}
