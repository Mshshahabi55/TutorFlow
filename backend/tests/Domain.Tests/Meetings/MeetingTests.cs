using TutorFlow.Domain.Meetings;
using TutorFlow.Domain.Meetings.Events;
using TutorFlow.Domain.Meetings.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Tests.Meetings;

public class MeetingTests
{
    private static readonly SessionId SampleSessionId = SessionId.New();
    private static readonly DateTime Now = new(2026, 8, 1, 14, 0, 0, DateTimeKind.Utc);

    private static Meeting CreateSample() => Meeting.Create(
        SampleSessionId,
        MeetingProviderOption.GoogleMeet,
        "provider-meeting-1",
        "https://meet.example.com/join/abc",
        "https://meet.example.com/host/abc",
        Now,
        Now.AddHours(1),
        Now);

    [Fact]
    public void Creating_records_every_field_and_defaults_to_Scheduled()
    {
        var meeting = CreateSample();

        Assert.Equal(SampleSessionId, meeting.SessionId);
        Assert.Equal(MeetingProviderOption.GoogleMeet, meeting.Provider);
        Assert.Equal("provider-meeting-1", meeting.ProviderMeetingId);
        Assert.Equal("https://meet.example.com/join/abc", meeting.JoinUrl);
        Assert.Equal("https://meet.example.com/host/abc", meeting.HostUrl);
        Assert.Equal(Now, meeting.StartsAtUtc);
        Assert.Equal(Now.AddHours(1), meeting.EndsAtUtc);
        Assert.Equal(MeetingStatus.Scheduled, meeting.Status);
        Assert.Equal(Now, meeting.CreatedAtUtc);
        Assert.Equal(Now, meeting.UpdatedAtUtc);
    }

    [Fact]
    public void Creating_with_no_host_url_is_allowed()
    {
        var meeting = Meeting.Create(
            SampleSessionId, MeetingProviderOption.Zoom, "p1", "https://zoom.example.com/j/1", null, Now, Now.AddMinutes(30), Now);

        Assert.Null(meeting.HostUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Creating_with_an_empty_provider_meeting_id_fails(string providerMeetingId)
    {
        Assert.Throws<ArgumentException>(() => Meeting.Create(
            SampleSessionId, MeetingProviderOption.GoogleMeet, providerMeetingId, "https://meet.example.com/join/abc", null, Now, Now.AddHours(1), Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Creating_with_an_empty_join_url_fails(string joinUrl)
    {
        Assert.Throws<ArgumentException>(() => Meeting.Create(
            SampleSessionId, MeetingProviderOption.GoogleMeet, "p1", joinUrl, null, Now, Now.AddHours(1), Now));
    }

    [Fact]
    public void Creating_with_an_end_time_not_after_the_start_time_fails()
    {
        Assert.Throws<ArgumentException>(() => Meeting.Create(
            SampleSessionId, MeetingProviderOption.GoogleMeet, "p1", "https://meet.example.com/join/abc", null, Now, Now, Now));
    }

    [Fact]
    public void Creating_raises_MeetingCreated()
    {
        var meeting = CreateSample();

        Assert.Contains(
            meeting.DomainEvents,
            e => e is MeetingCreated created && created.MeetingId == meeting.Id && created.SessionId == SampleSessionId
                && created.Provider == MeetingProviderOption.GoogleMeet);
    }

    [Fact]
    public void Reschedule_updates_times_and_raises_MeetingUpdated()
    {
        var meeting = CreateSample();
        var newStart = Now.AddDays(1);
        var newEnd = newStart.AddHours(1);
        var updatedAt = Now.AddMinutes(5);

        meeting.Reschedule(newStart, newEnd, updatedAt);

        Assert.Equal(newStart, meeting.StartsAtUtc);
        Assert.Equal(newEnd, meeting.EndsAtUtc);
        Assert.Equal(updatedAt, meeting.UpdatedAtUtc);
        Assert.Contains(meeting.DomainEvents, e => e is MeetingUpdated updated && updated.MeetingId == meeting.Id);
    }

    [Fact]
    public void Reschedule_with_an_end_time_not_after_the_start_time_fails()
    {
        var meeting = CreateSample();

        Assert.Throws<ArgumentException>(() => meeting.Reschedule(Now, Now, Now));
    }

    [Fact]
    public void Reschedule_after_cancellation_fails()
    {
        var meeting = CreateSample();
        meeting.Cancel(Now);

        Assert.Throws<InvalidOperationException>(() => meeting.Reschedule(Now.AddDays(1), Now.AddDays(1).AddHours(1), Now));
    }

    [Fact]
    public void Cancel_sets_status_and_raises_MeetingCancelled()
    {
        var meeting = CreateSample();
        var cancelledAt = Now.AddMinutes(10);

        meeting.Cancel(cancelledAt);

        Assert.Equal(MeetingStatus.Cancelled, meeting.Status);
        Assert.Equal(cancelledAt, meeting.UpdatedAtUtc);
        Assert.Contains(meeting.DomainEvents, e => e is MeetingCancelled cancelled && cancelled.MeetingId == meeting.Id);
    }

    [Fact]
    public void Cancel_is_idempotent_and_does_not_raise_a_second_event()
    {
        var meeting = CreateSample();
        meeting.Cancel(Now.AddMinutes(10));
        meeting.ClearDomainEvents();

        meeting.Cancel(Now.AddMinutes(20));

        Assert.DoesNotContain(meeting.DomainEvents, e => e is MeetingCancelled);
    }

    [Fact]
    public void ApplyProviderResult_updates_provider_fields_without_raising_an_event()
    {
        var meeting = CreateSample();
        meeting.ClearDomainEvents();

        meeting.ApplyProviderResult("provider-meeting-2", "https://meet.example.com/join/xyz", null, Now.AddMinutes(1));

        Assert.Equal("provider-meeting-2", meeting.ProviderMeetingId);
        Assert.Equal("https://meet.example.com/join/xyz", meeting.JoinUrl);
        Assert.Null(meeting.HostUrl);
        Assert.Empty(meeting.DomainEvents);
    }
}
