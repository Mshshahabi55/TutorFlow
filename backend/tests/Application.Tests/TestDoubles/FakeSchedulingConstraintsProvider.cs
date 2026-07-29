using TutorFlow.Application.Scheduling.Interfaces;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class FakeSchedulingConstraintsProvider : ISchedulingConstraintsProvider
{
    public TimeSpan MinimumBookingNotice { get; init; } = TimeSpan.FromHours(24);

    public TimeSpan MaximumBookingHorizon { get; init; } = TimeSpan.FromDays(90);
}
