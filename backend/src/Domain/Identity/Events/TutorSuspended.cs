using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity.Events;

// An Admin suspends a Tutor (PRODUCT_REQUIREMENTS.md ADM-2;
// DOMAIN_MODEL.md: Domain Events).
public sealed record TutorSuspended(AccountId TutorId) : DomainEvent;
