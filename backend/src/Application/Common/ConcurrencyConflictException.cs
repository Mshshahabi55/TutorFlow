namespace TutorFlow.Application.Common;

// Application-owned signal for a storage-layer concurrency rejection
// (docs/adr/ADR-014-concurrency-control-strategy.md: unique-constraint-based
// enforcement). Infrastructure translates a provider-specific unique-
// constraint violation into this type so a handler can react without
// depending on any Infrastructure or database-provider type
// (docs/adr/ADR-005-application-boundary.md).
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
