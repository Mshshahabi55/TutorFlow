namespace TutorFlow.Application.Scheduling.Queries;

// Availability List/Calendar (Backend Completion Phase, Track A, Phase A2).
// "Calendar" is not a distinct backend capability — both frontend views
// render this same collection differently; no separate query was introduced
// for it. Mirrors GetTutorScheduleQuery's shape exactly.
public sealed record GetTutorAvailabilitySlotsQuery(Guid TutorId);
