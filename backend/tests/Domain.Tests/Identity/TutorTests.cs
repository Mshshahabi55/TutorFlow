using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.Events;

namespace TutorFlow.Domain.Tests.Identity;

public class TutorTests
{
    [Fact]
    public void Tutor_starts_not_approved()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.False(tutor.IsApproved);
        Assert.False(tutor.IsDiscoverable);
    }

    [Fact]
    public void Approval_makes_tutor_discoverable()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        tutor.Approve();

        Assert.True(tutor.IsApproved);
        Assert.True(tutor.IsDiscoverable);
    }

    [Fact]
    public void Suspension_removes_discoverability()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.Approve();

        tutor.Suspend();

        Assert.True(tutor.IsSuspended);
        Assert.False(tutor.IsDiscoverable);
    }

    [Fact]
    public void Registering_raises_TutorRegistered()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Contains(tutor.DomainEvents, e => e is TutorRegistered registered && registered.TutorId == tutor.Id);
    }

    [Fact]
    public void Approving_raises_TutorApproved()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        tutor.Approve();

        Assert.Contains(tutor.DomainEvents, e => e is TutorApproved approved && approved.TutorId == tutor.Id);
    }

    [Fact]
    public void Suspending_raises_TutorSuspended()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.Approve();

        tutor.Suspend();

        Assert.Contains(tutor.DomainEvents, e => e is TutorSuspended suspended && suspended.TutorId == tutor.Id);
    }

    [Fact]
    public void Approving_an_already_approved_tutor_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.Approve();

        Assert.Throws<InvalidOperationException>(() => tutor.Approve());
    }

    [Fact]
    public void Suspending_an_already_suspended_tutor_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.Suspend();

        Assert.Throws<InvalidOperationException>(() => tutor.Suspend());
    }

    [Fact]
    public void SetOfferedDurations_with_null_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Throws<ArgumentNullException>(() => tutor.SetOfferedDurations(null!));
    }

    [Fact]
    public void SetOfferedDurations_with_empty_collection_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Throws<ArgumentException>(() => tutor.SetOfferedDurations(Array.Empty<TimeSpan>()));
    }

    [Fact]
    public void SetOfferedDurations_with_a_non_positive_duration_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => tutor.SetOfferedDurations(new[] { TimeSpan.FromHours(1), TimeSpan.Zero }));
    }

    [Fact]
    public void SetOfferedDurations_with_valid_durations_sets_them()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        var durations = new[] { TimeSpan.FromMinutes(30), TimeSpan.FromHours(1) };

        tutor.SetOfferedDurations(durations);

        Assert.Equal(durations, tutor.OfferedDurations);
    }

    [Fact]
    public void SetHourlyRate_with_null_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Throws<ArgumentNullException>(() => tutor.SetHourlyRate(null!));
    }

    [Fact]
    public void SetSubject_with_null_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Throws<ArgumentNullException>(() => tutor.SetSubject(null!));
    }

    [Fact]
    public void SetLanguage_with_null_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Throws<ArgumentNullException>(() => tutor.SetLanguage(null!));
    }

    [Fact]
    public void SetLocation_with_null_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Throws<ArgumentNullException>(() => tutor.SetLocation(null!));
    }
}
