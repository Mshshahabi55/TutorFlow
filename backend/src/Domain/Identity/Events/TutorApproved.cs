using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity.Events;

// An Admin approves a Tutor, making them discoverable
// (PRODUCT_REQUIREMENTS.md ADM-1; DOMAIN_MODEL.md: Domain Events).
public sealed record TutorApproved(AccountId TutorId) : DomainEvent;
