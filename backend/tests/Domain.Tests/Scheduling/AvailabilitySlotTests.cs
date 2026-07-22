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
        DeliveryMode.Online);

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
            DeliveryMode.Online));
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
}
