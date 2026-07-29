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

    // Phase 4.7: Reschedule needs both the originating slot and its Tutor
    // exposed, to build same-Tutor/different-Tutor target-slot scenarios.
    private static (Session Session, AvailabilitySlot OriginalSlot, TutorId TutorId) BookSessionWithSlot()
    {
        var tutorId = TutorId.From(Guid.NewGuid());
        var slot = AvailabilitySlot.Declare(
            tutorId,
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online);
        var session = slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);
        return (session, slot, tutorId);
    }

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

    // Phase 4.7: rescheduling targets an existing, open AvailabilitySlot —
    // exactly like booking — not an arbitrary timestamp.
    [Fact]
    public void Reschedule_moves_the_Session_onto_the_new_slots_time_duration_and_delivery_mode()
    {
        var (session, _, tutorId) = BookSessionWithSlot();
        var newSlot = AvailabilitySlot.Declare(
            tutorId,
            DateTime.UtcNow.AddDays(2),
            SessionDuration.Of(TimeSpan.FromMinutes(90)),
            DeliveryMode.InPerson);

        session.Reschedule(newSlot.Id, tutorId, newSlot.StartTimeUtc, newSlot.Duration, newSlot.DeliveryMode);

        Assert.Equal(newSlot.Id, session.AvailabilitySlotId);
        Assert.Equal(newSlot.StartTimeUtc, session.ScheduledTimeUtc);
        Assert.Equal(newSlot.Duration, session.Duration);
        Assert.Equal(newSlot.DeliveryMode, session.DeliveryMode);
    }

    [Fact]
    public void Reschedule_raises_SessionRescheduled_naming_both_the_old_and_new_slot()
    {
        var (session, originalSlot, tutorId) = BookSessionWithSlot();
        var newSlot = AvailabilitySlot.Declare(
            tutorId, DateTime.UtcNow.AddDays(2), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);

        session.Reschedule(newSlot.Id, tutorId, newSlot.StartTimeUtc, newSlot.Duration, newSlot.DeliveryMode);

        Assert.Contains(
            session.DomainEvents,
            e => e is SessionRescheduled rescheduled
                && rescheduled.SessionId == session.Id
                && rescheduled.OldAvailabilitySlotId == originalSlot.Id
                && rescheduled.NewAvailabilitySlotId == newSlot.Id
                && rescheduled.NewScheduledTimeUtc == newSlot.StartTimeUtc
                && rescheduled.NewEndTimeUtc == newSlot.StartTimeUtc + newSlot.Duration.Value);
    }

    [Fact]
    public void Reschedule_does_not_change_Price()
    {
        var tutorId = TutorId.From(Guid.NewGuid());
        var slot = AvailabilitySlot.Declare(
            tutorId, DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        var price = SessionPrice.Of(500_000m);
        var session = slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null, price);
        var newSlot = AvailabilitySlot.Declare(
            tutorId, DateTime.UtcNow.AddDays(2), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);

        session.Reschedule(newSlot.Id, tutorId, newSlot.StartTimeUtc, newSlot.Duration, newSlot.DeliveryMode);

        Assert.Equal(price, session.Price);
    }

    [Fact]
    public void Rescheduling_a_cancelled_Session_throws()
    {
        var (session, _, tutorId) = BookSessionWithSlot();
        session.Cancel();
        var newSlot = AvailabilitySlot.Declare(
            tutorId, DateTime.UtcNow.AddDays(2), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);

        Assert.Throws<InvalidOperationException>(
            () => session.Reschedule(newSlot.Id, tutorId, newSlot.StartTimeUtc, newSlot.Duration, newSlot.DeliveryMode));
    }

    [Fact]
    public void Rescheduling_onto_a_different_Tutors_slot_throws()
    {
        var (session, _, _) = BookSessionWithSlot();
        var otherTutorId = TutorId.From(Guid.NewGuid());
        var otherTutorsSlot = AvailabilitySlot.Declare(
            otherTutorId, DateTime.UtcNow.AddDays(2), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);

        Assert.Throws<InvalidOperationException>(() => session.Reschedule(
            otherTutorsSlot.Id, otherTutorId, otherTutorsSlot.StartTimeUtc, otherTutorsSlot.Duration, otherTutorsSlot.DeliveryMode));
    }

    [Fact]
    public void Rescheduling_onto_the_Sessions_current_slot_throws()
    {
        var (session, originalSlot, tutorId) = BookSessionWithSlot();

        Assert.Throws<InvalidOperationException>(() => session.Reschedule(
            originalSlot.Id, tutorId, originalSlot.StartTimeUtc, originalSlot.Duration, originalSlot.DeliveryMode));
    }
}
