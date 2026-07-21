using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.Events;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Tests.Scheduling;

public class SessionTests
{
    // Session.Book is internal to Domain.Scheduling (Phase 11: AvailabilitySlot
    // owns booking eligibility), so every test here creates a Session the only
    // way anything outside that internal call can: via AvailabilitySlot.Book.
    private static Session BookSession() => AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online)
        .Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);

    [Fact]
    public void Session_starts_Scheduled()
    {
        var session = BookSession();

        Assert.Equal(SessionStatus.Scheduled, session.Status);
    }

    [Fact]
    public void Session_can_Complete()
    {
        var session = BookSession();

        session.Complete();

        Assert.Equal(SessionStatus.Completed, session.Status);
    }

    [Fact]
    public void Session_can_Cancel()
    {
        var session = BookSession();

        session.Cancel();

        Assert.Equal(SessionStatus.Cancelled, session.Status);
    }

    [Fact]
    public void Session_can_MarkNoShow()
    {
        var session = BookSession();

        session.MarkNoShow();

        Assert.Equal(SessionStatus.NoShow, session.Status);
    }

    [Fact]
    public void Completing_an_already_completed_Session_throws()
    {
        var session = BookSession();
        session.Complete();

        Assert.Throws<InvalidOperationException>(() => session.Complete());
    }

    [Fact]
    public void Cancelling_an_already_cancelled_Session_throws()
    {
        var session = BookSession();
        session.Cancel();

        Assert.Throws<InvalidOperationException>(() => session.Cancel());
    }

    [Fact]
    public void Completing_a_cancelled_Session_throws()
    {
        var session = BookSession();
        session.Cancel();

        Assert.Throws<InvalidOperationException>(() => session.Complete());
    }

    [Fact]
    public void Marking_NoShow_on_a_completed_Session_throws()
    {
        var session = BookSession();
        session.Complete();

        Assert.Throws<InvalidOperationException>(() => session.MarkNoShow());
    }

    [Fact]
    public void Booking_raises_SessionBooked()
    {
        var session = BookSession();

        Assert.Contains(session.DomainEvents, e => e is SessionBooked booked && booked.SessionId == session.Id);
    }

    [Fact]
    public void Completing_raises_SessionCompleted()
    {
        var session = BookSession();

        session.Complete();

        Assert.Contains(
            session.DomainEvents,
            e => e is SessionCompleted completed && completed.SessionId == session.Id);
    }

    [Fact]
    public void Cancelling_raises_SessionCancelled()
    {
        var session = BookSession();

        session.Cancel();

        Assert.Contains(
            session.DomainEvents,
            e => e is SessionCancelled cancelled && cancelled.SessionId == session.Id);
    }

    [Fact]
    public void MarkingNoShow_raises_SessionMarkedNoShow()
    {
        var session = BookSession();

        session.MarkNoShow();

        Assert.Contains(
            session.DomainEvents,
            e => e is SessionMarkedNoShow markedNoShow && markedNoShow.SessionId == session.Id);
    }
}
