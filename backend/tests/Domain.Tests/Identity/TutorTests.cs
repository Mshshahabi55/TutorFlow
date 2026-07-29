using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.Events;
using TutorFlow.Domain.Identity.ValueObjects;

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

    // ADR-024 (Accepted, 2026-07-28) — Tutor Profile Enrichment & Onboarding Wizard.

    [Fact]
    public void A_new_tutor_starts_in_Draft_profile_status()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Equal(TutorProfileStatus.Draft, tutor.ProfileStatus);
    }

    [Fact]
    public void SetPersonalInfo_sets_every_field()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        tutor.SetPersonalInfo("Jane Doe", "Friendly Math Tutor", "I love teaching.", "Iran", "Tehran", new[] { "French", "German" });

        Assert.Equal("Jane Doe", tutor.DisplayName);
        Assert.Equal("Friendly Math Tutor", tutor.Headline);
        Assert.Equal("I love teaching.", tutor.Biography);
        Assert.Equal("Iran", tutor.Country);
        Assert.Equal("Tehran", tutor.City);
        Assert.Equal(new[] { "French", "German" }, tutor.OtherLanguages);
    }

    [Fact]
    public void SetPersonalInfo_with_a_field_exceeding_the_length_ceiling_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Throws<ArgumentException>(() => tutor.SetPersonalInfo(new string('a', 201), null, null, null, null, null));
    }

    [Fact]
    public void SetTeachingInfo_sets_every_field_including_multiple_subjects()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        var subjects = new[] { TutorSubjectEntry.Of("Mathematics", "Beginner"), TutorSubjectEntry.Of("Physics", null) };

        tutor.SetTeachingInfo(subjects, 5, "BSc Mathematics", "TEFL", "Socratic method", new[] { "Exam prep" });

        Assert.Equal(subjects, tutor.TutorSubjects);
        Assert.Equal(5, tutor.YearsOfExperience);
        Assert.Equal("BSc Mathematics", tutor.Education);
        Assert.Equal("TEFL", tutor.Certifications);
        Assert.Equal("Socratic method", tutor.TeachingMethodology);
        Assert.Equal(new[] { "Exam prep" }, tutor.LessonSpecialties);
    }

    [Fact]
    public void SetTeachingInfo_with_negative_years_of_experience_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Throws<ArgumentOutOfRangeException>(() => tutor.SetTeachingInfo(null, -1, null, null, null, null));
    }

    // Merge Readiness audit Critical 2: each of the four self-declared
    // collections is bounded at TutorProfileLimits.MaxCollectionEntries
    // (20) so a Tutor row's serialized size cannot grow unbounded.
    [Fact]
    public void SetPersonalInfo_with_more_than_the_max_collection_entries_in_otherLanguages_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        var tooManyLanguages = Enumerable.Range(0, 21).Select(i => $"Language{i}");

        Assert.Throws<ArgumentException>(() => tutor.SetPersonalInfo(null, null, null, null, null, tooManyLanguages));
    }

    [Fact]
    public void SetPersonalInfo_with_exactly_the_max_collection_entries_in_otherLanguages_succeeds()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        var maxLanguages = Enumerable.Range(0, 20).Select(i => $"Language{i}").ToList();

        tutor.SetPersonalInfo(null, null, null, null, null, maxLanguages);

        Assert.Equal(20, tutor.OtherLanguages.Count);
    }

    [Fact]
    public void SetTeachingInfo_with_more_than_the_max_collection_entries_in_tutorSubjects_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        var tooManySubjects = Enumerable.Range(0, 21).Select(i => TutorSubjectEntry.Of($"Subject{i}", null));

        Assert.Throws<ArgumentException>(() => tutor.SetTeachingInfo(tooManySubjects, null, null, null, null, null));
    }

    [Fact]
    public void SetTeachingInfo_with_more_than_the_max_collection_entries_in_lessonSpecialties_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        var tooManySpecialties = Enumerable.Range(0, 21).Select(i => $"Specialty{i}");

        Assert.Throws<ArgumentException>(() => tutor.SetTeachingInfo(null, null, null, null, null, tooManySpecialties));
    }

    [Fact]
    public void SetMedia_sets_every_url_field()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        tutor.SetMedia("https://example.com/photo.jpg", "https://example.com/intro.mp4", new[] { "https://example.com/1.jpg" });

        Assert.Equal("https://example.com/photo.jpg", tutor.PhotoUrl);
        Assert.Equal("https://example.com/intro.mp4", tutor.IntroVideoUrl);
        Assert.Equal(new[] { "https://example.com/1.jpg" }, tutor.GalleryImageUrls);
    }

    [Fact]
    public void SetMedia_with_more_than_the_max_collection_entries_in_galleryImageUrls_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        var tooManyUrls = Enumerable.Range(0, 21).Select(i => $"https://example.com/{i}.jpg");

        Assert.Throws<ArgumentException>(() => tutor.SetMedia(null, null, tooManyUrls));
    }

    // Merge Readiness audit High 1: a Tutor must never be able to persist a
    // media URL whose scheme would be unsafe to ever render.
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/photo.jpg")]
    [InlineData("not a url")]
    [InlineData("/relative/path.jpg")]
    public void SetMedia_with_a_non_http_or_malformed_photo_url_throws(string url)
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Throws<ArgumentException>(() => tutor.SetMedia(url, null, null));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    public void SetMedia_with_a_non_http_intro_video_url_throws(string url)
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Throws<ArgumentException>(() => tutor.SetMedia(null, url, null));
    }

    [Fact]
    public void SetMedia_with_a_non_http_gallery_image_url_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Throws<ArgumentException>(() => tutor.SetMedia(null, null, new[] { "javascript:alert(1)" }));
    }

    [Fact]
    public void SetMedia_with_an_invalid_gallery_url_does_not_leave_photoUrl_or_introVideoUrl_partially_updated()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.SetMedia("https://example.com/original.jpg", "https://example.com/original.mp4", null);

        Assert.Throws<ArgumentException>(() =>
            tutor.SetMedia("https://example.com/new.jpg", "https://example.com/new.mp4", new[] { "javascript:alert(1)" }));

        Assert.Equal("https://example.com/original.jpg", tutor.PhotoUrl);
        Assert.Equal("https://example.com/original.mp4", tutor.IntroVideoUrl);
    }

    [Fact]
    public void SetTrialLesson_sets_availability_and_price()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        tutor.SetTrialLesson(true, HourlyRate.Of(100_000));

        Assert.True(tutor.TrialLessonAvailable);
        Assert.Equal(100_000, tutor.TrialLessonPrice?.Amount);
    }

    [Fact]
    public void SubmitProfile_without_a_subject_or_hourly_rate_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.Throws<InvalidOperationException>(() => tutor.SubmitProfile());
    }

    [Fact]
    public void SubmitProfile_with_subject_and_hourly_rate_set_transitions_to_Submitted_and_raises_event()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.SetSubject(Subject.Of("Mathematics"));
        tutor.SetHourlyRate(HourlyRate.Of(100_000));

        tutor.SubmitProfile();

        Assert.Equal(TutorProfileStatus.Submitted, tutor.ProfileStatus);
        Assert.Contains(tutor.DomainEvents, e => e is TutorProfileSubmitted submitted && submitted.TutorId == tutor.Id);
    }

    [Fact]
    public void SubmitProfile_twice_throws()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.SetSubject(Subject.Of("Mathematics"));
        tutor.SetHourlyRate(HourlyRate.Of(100_000));
        tutor.SubmitProfile();

        Assert.Throws<InvalidOperationException>(() => tutor.SubmitProfile());
    }
}
