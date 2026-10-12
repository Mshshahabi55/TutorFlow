using TutorFlow.Modules.Tutors.Domain;

namespace TutorFlow.Modules.Tutors.Tests;

public class TutorApplicationTests
{
    private static readonly TimeProvider Clock = TimeProvider.System;

    private static TutorApplication CreateCompleteDraft()
    {
        var application = TutorApplication.CreateDraft(Guid.NewGuid(), Clock);
        application.ApplyDraftUpdate(
            new DraftUpdate(
                DisplayName: "Ada Lovelace",
                Headline: "Patient, experienced math tutor",
                ProfilePhotoReference: "https://example.com/photo.jpg",
                SubjectIds: [Guid.NewGuid()],
                SpokenLanguages: [new TutorSpokenLanguage("en", LanguageProficiency.Native)],
                HourlyRate: new Money(2500, "EUR")),
            Clock);
        return application;
    }

    [Fact]
    public void CreateDraft_SetsStatusToDraft()
    {
        var application = TutorApplication.CreateDraft(Guid.NewGuid(), Clock);

        Assert.Equal(TutorApplicationStatus.Draft, application.Status);
    }

    [Fact]
    public void Submit_WhenDraftIncomplete_ReturnsFailureAndStaysDraft()
    {
        var application = TutorApplication.CreateDraft(Guid.NewGuid(), Clock);

        var result = application.Submit(Clock);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
        Assert.Equal(TutorApplicationStatus.Draft, application.Status);
    }

    [Fact]
    public void Submit_WithoutAProfilePhoto_ReturnsFailure()
    {
        var application = TutorApplication.CreateDraft(Guid.NewGuid(), Clock);
        application.ApplyDraftUpdate(
            new DraftUpdate(
                DisplayName: "Ada Lovelace",
                Headline: "Patient, experienced math tutor",
                SubjectIds: [Guid.NewGuid()],
                SpokenLanguages: [new TutorSpokenLanguage("en", LanguageProficiency.Native)],
                HourlyRate: new Money(2500, "EUR")),
            Clock);

        var result = application.Submit(Clock);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("photo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Submit_WithoutAHeadline_ReturnsFailure()
    {
        var application = TutorApplication.CreateDraft(Guid.NewGuid(), Clock);
        application.ApplyDraftUpdate(
            new DraftUpdate(
                DisplayName: "Ada Lovelace",
                ProfilePhotoReference: "https://example.com/photo.jpg",
                SubjectIds: [Guid.NewGuid()],
                SpokenLanguages: [new TutorSpokenLanguage("en", LanguageProficiency.Native)],
                HourlyRate: new Money(2500, "EUR")),
            Clock);

        var result = application.Submit(Clock);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("headline", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Submit_WhenDraftComplete_TransitionsToSubmitted()
    {
        var application = CreateCompleteDraft();

        var result = application.Submit(Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(TutorApplicationStatus.Submitted, application.Status);
        Assert.NotNull(application.SubmittedAtUtc);
    }

    [Fact]
    public void Submit_AssignsAPublicSlug_DerivedFromDisplayNameAndId()
    {
        var application = CreateCompleteDraft();

        application.Submit(Clock);

        Assert.NotNull(application.PublicSlug);
        Assert.StartsWith("ada-lovelace-", application.PublicSlug);
        Assert.EndsWith(application.Id.ToString("N")[..8], application.PublicSlug);
    }

    [Fact]
    public void Submit_TwoApplicationsWithTheSameDisplayName_GetDifferentSlugs()
    {
        var first = CreateCompleteDraft();
        var second = TutorApplication.CreateDraft(Guid.NewGuid(), Clock);
        second.ApplyDraftUpdate(
            new DraftUpdate(
                DisplayName: "Ada Lovelace",
                Headline: "Patient, experienced math tutor",
                ProfilePhotoReference: "https://example.com/photo.jpg",
                SubjectIds: [Guid.NewGuid()],
                SpokenLanguages: [new TutorSpokenLanguage("en", LanguageProficiency.Native)],
                HourlyRate: new Money(2500, "EUR")),
            Clock);

        first.Submit(Clock);
        second.Submit(Clock);

        Assert.NotEqual(first.PublicSlug, second.PublicSlug);
        // Collision avoidance comes from the Id-derived suffix, not a counter — both still read
        // as the same human-friendly name prefix.
        Assert.StartsWith("ada-lovelace-", first.PublicSlug);
        Assert.StartsWith("ada-lovelace-", second.PublicSlug);
    }

    [Fact]
    public void PublicSlug_StaysStable_AcrossTheEntireLifecycleAfterSubmission()
    {
        var application = CreateCompleteDraft();
        application.Submit(Clock);
        var slugAfterSubmit = application.PublicSlug;

        application.BeginReview(Clock);
        application.Approve(Clock);
        application.Publish(Clock);

        // The slug is assigned once, in Submit() — later lifecycle transitions (including
        // Publish, which is what actually makes the profile publicly visible) must never
        // reassign or regenerate it, so a shared link never breaks.
        Assert.Equal(slugAfterSubmit, application.PublicSlug);
    }

    [Fact]
    public void IsAcceptingNewStudents_DefaultsToTrue_AndIsIndependentOfPublicationStatus()
    {
        var application = CreateCompleteDraft();

        Assert.True(application.IsAcceptingNewStudents);

        application.Submit(Clock);
        application.BeginReview(Clock);
        application.Approve(Clock);
        application.Publish(Clock);

        // Reaching Published must not implicitly flip this — it's a tutor-controlled flag,
        // orthogonal to the publication lifecycle (ADR-0022).
        Assert.True(application.IsAcceptingNewStudents);

        application.SetAcceptingNewStudents(false, Clock);

        Assert.False(application.IsAcceptingNewStudents);
        Assert.Equal(TutorApplicationStatus.Published, application.Status);
    }

    [Fact]
    public void Submit_WhenAlreadySubmitted_ReturnsFailure_InvalidStateTransition()
    {
        var application = CreateCompleteDraft();
        application.Submit(Clock);

        var result = application.Submit(Clock);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ApplyDraftUpdate_WhenNotDraft_ReturnsFailure_InvalidStateTransition()
    {
        var application = CreateCompleteDraft();
        application.Submit(Clock);

        var result = application.ApplyDraftUpdate(new DraftUpdate(DisplayName: "New Name"), Clock);

        Assert.False(result.IsSuccess);
        Assert.Equal("Ada Lovelace", application.DisplayName);
    }

    [Fact]
    public void BeginReview_FromSubmitted_Succeeds()
    {
        var application = CreateCompleteDraft();
        application.Submit(Clock);

        var result = application.BeginReview(Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(TutorApplicationStatus.UnderReview, application.Status);
    }

    [Fact]
    public void BeginReview_FromDraft_ReturnsFailure()
    {
        var application = CreateCompleteDraft();

        var result = application.BeginReview(Clock);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Approve_FromSubmitted_Succeeds()
    {
        var application = CreateCompleteDraft();
        application.Submit(Clock);

        var result = application.Approve(Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(TutorApplicationStatus.Approved, application.Status);
    }

    [Fact]
    public void Reject_FromUnderReview_Succeeds_AndRecordsReason()
    {
        var application = CreateCompleteDraft();
        application.Submit(Clock);
        application.BeginReview(Clock);

        var result = application.Reject("Certificate not verifiable", Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(TutorApplicationStatus.Rejected, application.Status);
        Assert.Equal("Certificate not verifiable", application.RejectionReason);
    }

    [Fact]
    public void ReopenForRevision_FromRejected_ReturnsToDraft_AndClearsRejectionReason()
    {
        var application = CreateCompleteDraft();
        application.Submit(Clock);
        application.BeginReview(Clock);
        application.Reject("Photo doesn't meet guidelines", Clock);

        var result = application.ReopenForRevision(Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(TutorApplicationStatus.Draft, application.Status);
        Assert.Null(application.RejectionReason);
    }

    [Fact]
    public void ReopenForRevision_AllowsEditingAndResubmitting_ReusingTheSamePublicSlug()
    {
        var application = CreateCompleteDraft();
        application.Submit(Clock);
        var originalSlug = application.PublicSlug;
        application.BeginReview(Clock);
        application.Reject("Needs a clearer headline", Clock);
        application.ReopenForRevision(Clock);

        var updateResult = application.ApplyDraftUpdate(new DraftUpdate(Headline: "Patient, experienced math tutor"), Clock);
        var submitResult = application.Submit(Clock);

        Assert.True(updateResult.IsSuccess);
        Assert.True(submitResult.IsSuccess);
        Assert.Equal(TutorApplicationStatus.Submitted, application.Status);
        Assert.Equal(originalSlug, application.PublicSlug);
    }

    [Fact]
    public void ReopenForRevision_FromDraft_ReturnsFailure_InvalidStateTransition()
    {
        var application = TutorApplication.CreateDraft(Guid.NewGuid(), Clock);

        var result = application.ReopenForRevision(Clock);

        Assert.False(result.IsSuccess);
        Assert.Equal(TutorApplicationStatus.Draft, application.Status);
    }

    [Fact]
    public void Publish_FromApproved_Succeeds()
    {
        var application = CreateCompleteDraft();
        application.Submit(Clock);
        application.Approve(Clock);

        var result = application.Publish(Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(TutorApplicationStatus.Published, application.Status);
        Assert.NotNull(application.PublishedAtUtc);
    }

    [Fact]
    public void Publish_FromDraft_ReturnsFailure_InvalidStateTransition()
    {
        var application = CreateCompleteDraft();

        var result = application.Publish(Clock);

        Assert.False(result.IsSuccess);
        Assert.Equal(TutorApplicationStatus.Draft, application.Status);
    }

    [Fact]
    public void Suspend_FromPublished_Succeeds_AndRecordsReason()
    {
        var application = CreateCompleteDraft();
        application.Submit(Clock);
        application.Approve(Clock);
        application.Publish(Clock);

        var result = application.Suspend("Policy violation", Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(TutorApplicationStatus.Suspended, application.Status);
        Assert.Equal("Policy violation", application.SuspensionReason);
    }

    [Fact]
    public void Suspend_FromDraft_ReturnsFailure_InvalidStateTransition()
    {
        var application = CreateCompleteDraft();

        var result = application.Suspend("n/a", Clock);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ApplyDraftUpdate_OnlyOverwritesProvidedFields()
    {
        var application = TutorApplication.CreateDraft(Guid.NewGuid(), Clock);
        application.ApplyDraftUpdate(new DraftUpdate(DisplayName: "First", Headline: "Original headline"), Clock);

        application.ApplyDraftUpdate(new DraftUpdate(DisplayName: "Second"), Clock);

        Assert.Equal("Second", application.DisplayName);
        Assert.Equal("Original headline", application.Headline);
    }

    [Fact]
    public void ApplyDraftUpdate_SetsCountryOfBirthCode_ButNeverTheBadges()
    {
        var application = TutorApplication.CreateDraft(Guid.NewGuid(), Clock);

        application.ApplyDraftUpdate(new DraftUpdate(CountryOfBirthCode: "ES"), Clock);

        Assert.Equal("ES", application.CountryOfBirthCode);
        Assert.False(application.IsProfessionalTutor);
        Assert.False(application.IsSuperTutor);
    }

    [Fact]
    public void SetProfessionalTutorStatus_IsIndependentOfDraftUpdate()
    {
        var application = TutorApplication.CreateDraft(Guid.NewGuid(), Clock);

        application.SetProfessionalTutorStatus(true, Clock);

        Assert.True(application.IsProfessionalTutor);
        Assert.False(application.IsSuperTutor);
    }

    [Fact]
    public void SetSuperTutorStatus_CanBeRevoked()
    {
        var application = TutorApplication.CreateDraft(Guid.NewGuid(), Clock);
        application.SetSuperTutorStatus(true, Clock);

        application.SetSuperTutorStatus(false, Clock);

        Assert.False(application.IsSuperTutor);
    }
}
