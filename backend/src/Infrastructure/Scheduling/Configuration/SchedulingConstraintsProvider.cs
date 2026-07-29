using Microsoft.Extensions.Options;
using TutorFlow.Application.Scheduling.Interfaces;

namespace TutorFlow.Infrastructure.Scheduling.Configuration;

internal sealed class SchedulingConstraintsProvider : ISchedulingConstraintsProvider
{
    private readonly SchedulingConstraintsSettings _settings;

    public SchedulingConstraintsProvider(IOptions<SchedulingConstraintsSettings> options) => _settings = options.Value;

    public TimeSpan MinimumBookingNotice => TimeSpan.FromHours(_settings.MinimumBookingNoticeHours);

    public TimeSpan MaximumBookingHorizon => TimeSpan.FromDays(_settings.MaximumBookingHorizonDays);
}
