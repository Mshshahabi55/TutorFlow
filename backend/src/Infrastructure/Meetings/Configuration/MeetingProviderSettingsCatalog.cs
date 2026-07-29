using Microsoft.Extensions.Options;
using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Domain.Meetings.ValueObjects;

namespace TutorFlow.Infrastructure.Meetings.Configuration;

internal sealed class MeetingProviderSettingsCatalog : IMeetingProviderSettingsCatalog
{
    private readonly MeetingProviderSettings _settings;

    public MeetingProviderSettingsCatalog(IOptions<MeetingProviderSettings> options) => _settings = options.Value;

    public MeetingProviderOption DefaultProvider => _settings.DefaultProvider;

    public bool IsConfigured(MeetingProviderOption provider) => provider switch
    {
        MeetingProviderOption.GoogleMeet => _settings.Google.IsConfigured,
        MeetingProviderOption.MicrosoftTeams => _settings.Microsoft.IsConfigured,
        MeetingProviderOption.Zoom => _settings.Zoom.IsConfigured,
        // Mock needs no credentials at all — only ever selected in
        // Development (see MeetingProviderOption.Mock's own remarks).
        MeetingProviderOption.Mock => true,
        _ => false,
    };
}
