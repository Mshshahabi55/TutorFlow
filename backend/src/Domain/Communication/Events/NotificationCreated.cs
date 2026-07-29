using TutorFlow.Domain.Common;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Communication.Events;

// A Notification is created for a recipient
// (docs/adr/ADR-022-communication-and-notifications-architecture.md). Not
// itself audited — it is derivative of an already-audited (or intentionally
// not audited) triggering event, the same reasoning ADR-016 already applies
// to read-side projections generally.
public sealed record NotificationCreated(
    NotificationId NotificationId,
    AccountId RecipientId,
    NotificationType Type) : DomainEvent;
