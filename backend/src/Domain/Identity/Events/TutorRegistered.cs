using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity.Events;

// A Tutor account is self-registered (PRODUCT_REQUIREMENTS.md IDR-1;
// DOMAIN_MODEL.md: Domain Events).
public sealed record TutorRegistered(AccountId TutorId) : DomainEvent;
