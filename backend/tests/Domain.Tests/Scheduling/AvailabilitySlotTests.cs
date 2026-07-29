using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.Events;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Tests.Scheduling;

public class AvailabilitySlotTests
{
    private static AvailabilitySlot DeclareSlot() => AvailabilitySlot.Declare(
        TutorId.From(Guid.NewGuid()),
        DateTime.UtcNow.AddDays(1),
        SessionDuration.Of(TimeSpan.FromHours(1)),
        DeliveryMode.Online,
        existingSlotsForTutor: []);

    [Fact]
    public void Declaring_raises_AvailabilityDeclared()
    {
        var slot = DeclareSlot();

        Assert.Contains(
            slot.DomainEvents,
            e => e is AvailabilityDeclared declared && declared.AvailabilitySlotId == slot.Id);
    }

    [Fact]
    public void Slot_can_produce_one_Session()
    {
        var slot = DeclareSlot();

        var session = slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);

        Assert.NotNull(session);
        Assert.Equal(slot.Id, session.AvailabilitySlotId);
    }

    [Fact]
    public void Booking_marks_the_slot_consumed()
    {
        var slot = DeclareSlot();

        slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);

        Assert.True(slot.IsConsumed);
    }

    [Fact]
    public void Second_booking_of_a_consumed_slot_throws()
    {
        var slot = DeclareSlot();
        slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);

        Assert.Throws<InvalidOperationException>(
            () => slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null));
    }

    [Fact]
    public void Declaring_with_an_unset_start_time_throws()
    {
        Assert.Throws<ArgumentException>(() => AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()),
            default,
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online,
            existingSlotsForTutor: []));
    }

    // Phase 8a: closes the gap BookSessionCommandHandler's own comment
    // flagged as "explicitly deferred to a later phase" — a Tutor could
    // previously declare two time-overlapping slots.
    [Fact]
    public void Declaring_a_slot_that_overlaps_an_existing_one_throws()
    {
        var tutorId = TutorId.From(Guid.NewGuid());
        var start = DateTime.UtcNow.AddDays(1);
        var existing = AvailabilitySlot.Declare(
            tutorId, start, SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online, existingSlotsForTutor: []);

        Assert.Throws<InvalidOperationException>(() => AvailabilitySlot.Declare(
            tutorId,
            start.AddMinutes(30),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online,
            existingSlotsForTutor: [existing]));
    }

    [Fact]
    public void Declaring_a_slot_fully_contained_inside_an_existing_one_throws()
    {
        var tutorId = TutorId.From(Guid.NewGuid());
        var start = DateTime.UtcNow.AddDays(1);
        var existing = AvailabilitySlot.Declare(
            tutorId, start, SessionDuration.Of(TimeSpan.FromHours(2)), DeliveryMode.Online, existingSlotsForTutor: []);

        Assert.Throws<InvalidOperationException>(() => AvailabilitySlot.Declare(
            tutorId,
            start.AddMinutes(15),
            SessionDuration.Of(TimeSpan.FromMinutes(30)),
            DeliveryMode.Online,
            existingSlotsForTutor: [existing]));
    }

    [Fact]
    public void Declaring_a_back_to_back_slot_that_only_touches_an_existing_one_succeeds()
    {
        var tutorId = TutorId.From(Guid.NewGuid());
        var start = DateTime.UtcNow.AddDays(1);
        var duration = SessionDuration.Of(TimeSpan.FromHours(1));
        var existing = AvailabilitySlot.Declare(tutorId, start, duration, DeliveryMode.Online, existingSlotsForTutor: []);

        var next = AvailabilitySlot.Declare(
            tutorId, existing.EndTimeUtc, duration, DeliveryMode.Online, existingSlotsForTutor: [existing]);

        Assert.NotNull(next);
    }

    [Fact]
    public void Declaring_against_no_existing_slots_succeeds()
    {
        var slot = AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online,
            existingSlotsForTutor: []);

        Assert.NotNull(slot);
    }

    // Phase 4.6: DOMAIN_MODEL.md Open Question 7, resolved — cancelling
    // reopens the slot.
    [Fact]
    public void Reopening_a_consumed_slot_clears_IsConsumed()
    {
        var slot = DeclareSlot();
        var session = slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);

        slot.Reopen(session.Id);

        Assert.False(slot.IsConsumed);
    }

    [Fact]
    public void Reopening_raises_AvailabilitySlotReopened()
    {
        var slot = DeclareSlot();
        var session = slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);

        slot.Reopen(session.Id);

        Assert.Contains(
            slot.DomainEvents,
            e => e is AvailabilitySlotReopened reopened
                && reopened.AvailabilitySlotId == slot.Id
                && reopened.CancelledSessionId == session.Id);
    }

    [Fact]
    public void A_reopened_slot_can_be_booked_again()
    {
        var slot = DeclareSlot();
        var firstSession = slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);
        slot.Reopen(firstSession.Id);

        var secondSession = slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);

        Assert.NotNull(secondSession);
        Assert.NotEqual(firstSession.Id, secondSession.Id);
        Assert.True(slot.IsConsumed);
    }

    [Fact]
    public void Reopening_a_never_consumed_slot_throws()
    {
        var slot = DeclareSlot();

        Assert.Throws<InvalidOperationException>(() => slot.Reopen(SessionId.From(Guid.NewGuid())));
    }

    [Fact]
    public void Reopening_twice_in_a_row_throws_the_second_time()
    {
        var slot = DeclareSlot();
        var session = slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);
        slot.Reopen(session.Id);

        Assert.Throws<InvalidOperationException>(() => slot.Reopen(session.Id));
    }

    // Phase 4.7: the "book the new slot" half of rescheduling an existing
    // Session onto this slot — distinct from Book(), which also constructs
    // a brand-new Session.
    [Fact]
    public void Consume_marks_the_slot_consumed()
    {
        var slot = DeclareSlot();

        slot.Consume();

        Assert.True(slot.IsConsumed);
    }

    [Fact]
    public void Consuming_an_already_consumed_slot_throws()
    {
        var slot = DeclareSlot();
        slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);

        Assert.Throws<InvalidOperationException>(() => slot.Consume());
    }

    [Fact]
    public void A_reopened_slot_can_be_Consumed_again()
    {
        var slot = DeclareSlot();
        var session = slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);
        slot.Reopen(session.Id);

        slot.Consume();

        Assert.True(slot.IsConsumed);
    }
}
