namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Tutor.SetOfferedDurations(IEnumerable of TimeSpan)
// (PRODUCT_REQUIREMENTS.md SCH-3; User Journey 5.3 step 3).
public sealed record SetTutorOfferedDurationsCommand(Guid TutorId, IReadOnlyCollection<TimeSpan> Durations);
