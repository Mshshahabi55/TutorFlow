using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.DTOs;

public sealed record SessionDto(
    Guid SessionId,
    Guid TutorId,
    Guid StudentId,
    Guid? ParentGuardianId,
    Guid AvailabilitySlotId,
    DateTime ScheduledTimeUtc,
    DateTime EndTimeUtc,
    TimeSpan Duration,
    DeliveryMode DeliveryMode,
    SessionStatus Status)
{
    public static SessionDto FromDomain(Session session) => new(
        session.Id.Value,
        session.TutorId.Value,
        session.StudentId.Value,
        session.ParentGuardianId?.Value,
        session.AvailabilitySlotId.Value,
        session.ScheduledTimeUtc,
        session.EndTimeUtc,
        session.Duration.Value,
        session.DeliveryMode,
        session.Status);
}
