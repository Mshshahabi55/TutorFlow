using TutorFlow.Application.Common;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.Events;
using TutorFlow.Domain.Scheduling.Events;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Audit;

// Implements docs/adr/ADR-016-audit-durability-strategy.md's accepted
// same-transaction durability: staging an AuditEntry into the same Scoped
// TutorFlowDbContext that EfUnitOfWork.SaveChangesAsync will commit next means
// the audit write and the aggregate mutation succeed or fail together, in one
// database transaction, with no additional persistence call and no new
// infrastructure. This class performs no I/O itself — Add(...) only stages a
// tracked entity; the actual write happens inside EfUnitOfWork's own
// SaveChangesAsync call, per docs/adr/ADR-012-domain-event-dispatch-semantics.md
// Decision 1 (dispatch runs synchronously, in-process, before that call).
//
// Only the six events docs/adr/ADR-009-audit-and-observability.md's Business
// Event Audit Rules name are audited (ADR-016's Scope Note); every other
// IDomainEvent — including the six not yet resolved as auditable
// (ADR-006 Open Question 4 / ADR-009 Open Question 4) — is silently not
// recorded here. This is expected behavior, not a gap: recording them would
// resolve an open question this ADR explicitly declines to decide.
internal sealed class AuditDomainEventHandler : IDomainEventHandler
{
    private readonly TutorFlowDbContext _dbContext;
    private readonly ICurrentUserProvider _currentUserProvider;

    public AuditDomainEventHandler(TutorFlowDbContext dbContext, ICurrentUserProvider currentUserProvider)
    {
        _dbContext = dbContext;
        _currentUserProvider = currentUserProvider;
    }

    public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var entry = TryCreateAuditEntry(domainEvent);
        if (entry is not null)
        {
            _dbContext.Add(entry);
        }

        return Task.CompletedTask;
    }

    private AuditEntry? TryCreateAuditEntry(IDomainEvent domainEvent)
    {
        var (subjectId, action) = domainEvent switch
        {
            SessionBooked e => ((Guid?)e.SessionId.Value, (string?)domainEvent.GetType().Name),
            SessionRescheduled e => ((Guid?)e.SessionId.Value, domainEvent.GetType().Name),
            SessionCancelled e => ((Guid?)e.SessionId.Value, domainEvent.GetType().Name),
            AvailabilityDeclared e => ((Guid?)e.AvailabilitySlotId.Value, domainEvent.GetType().Name),
            // Phase 4.6: the Availability Slot's own lifecycle (declared,
            // consumed, reopened) gets a direct audit subject keyed by its
            // own id — distinct from SessionCancelled's Session-keyed
            // record, which does not itself surface which slot was freed.
            AvailabilitySlotReopened e => ((Guid?)e.AvailabilitySlotId.Value, domainEvent.GetType().Name),
            TutorApproved e => ((Guid?)e.TutorId.Value, domainEvent.GetType().Name),
            TutorSuspended e => ((Guid?)e.TutorId.Value, domainEvent.GetType().Name),
            // Both outcomes share one event type; the Action string is what
            // distinguishes them (docs/adr/ADR-017-authentication-mechanism-decision.md).
            // SubjectId is null only when no Account matched the attempted email.
            LoginAttempted e => (e.AccountId?.Value, e.Succeeded ? "LoginSucceeded" : "LoginFailed"),
            AccountLocked e => ((Guid?)e.AccountId.Value, domainEvent.GetType().Name),
            PasswordReset e => ((Guid?)e.AccountId.Value, domainEvent.GetType().Name),
            _ => (null, null),
        };

        if (action is null || domainEvent is not DomainEvent baseEvent)
        {
            return null;
        }

        return new AuditEntry(
            Guid.NewGuid(),
            baseEvent.OccurredOnUtc,
            action,
            subjectId,
            _currentUserProvider.UserId,
            _currentUserProvider.Role);
    }
}
