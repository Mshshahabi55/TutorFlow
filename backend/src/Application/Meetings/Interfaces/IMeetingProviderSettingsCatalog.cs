using TutorFlow.Domain.Meetings.ValueObjects;

namespace TutorFlow.Application.Meetings.Interfaces;

// docs/adr/ADR-023-...: "Provider not configured — never a fabricated URL."
// CreateMeetingCommandHandler checks this before ever calling
// IMeetingProvider.CreateMeetingAsync — Infrastructure implements it by
// checking whether the provider's own configuration section (Google/
// Microsoft/Zoom) has every credential it needs.
public interface IMeetingProviderSettingsCatalog
{
    bool IsConfigured(MeetingProviderOption provider);

    MeetingProviderOption DefaultProvider { get; }
}
