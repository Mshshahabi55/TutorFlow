namespace TutorFlow.Application.Scheduling.Commands;

// Corresponds to Session.Reschedule(DateTime) (PRODUCT_REQUIREMENTS.md SCH-7).
public sealed record RescheduleSessionCommand(Guid SessionId, DateTime NewScheduledTimeUtc);
