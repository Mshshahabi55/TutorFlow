namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Tutor.SetHourlyRate(HourlyRate) (PRODUCT_REQUIREMENTS.md DISC-2;
// User Journey 5.3 step 3).
public sealed record SetTutorHourlyRateCommand(Guid TutorId, decimal Amount);
