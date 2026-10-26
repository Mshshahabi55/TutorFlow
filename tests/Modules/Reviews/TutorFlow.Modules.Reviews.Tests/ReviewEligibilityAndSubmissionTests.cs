using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Lessons.Domain;
using TutorFlow.Modules.Lessons.Persistence;
using GetReviewEligibilityResponse = TutorFlow.Modules.Reviews.Features.GetReviewEligibility.GetReviewEligibility.Response;
using SubmitReviewResponse = TutorFlow.Modules.Reviews.Features.SubmitReview.SubmitReview.Response;
using TutorReviewSummaryResponse = TutorFlow.Modules.Reviews.Features.GetTutorReviewSummary.GetTutorReviewSummary.Response;

namespace TutorFlow.Modules.Reviews.Tests;

// Product Owner decision (documented Preply behavior, not an intentional deviation): eligibility
// is a Student<->Tutor relationship gate (3rd completed lesson with that specific tutor), and
// there is exactly one editable review per relationship — never a per-Lesson review. Every test
// here seeds real Lesson rows in the real LessonsDbContext (never a mock), through the real
// ICompletedLessonCountQuery cross-module DI resolution, matching the "test against real
// persisted lesson relationships" requirement.
[Collection(ReviewsHostCollection.Name)]
public class ReviewEligibilityAndSubmissionTests(ReviewsApiFixture fixture) : IClassFixture<ReviewsApiFixture>
{
    private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ReviewsApiFixture.CreateAccessToken(userId, roles));
        return client;
    }

    private async Task SeedLessonAsync(Guid studentUserId, Guid tutorUserId, LessonStatus status)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LessonsDbContext>();
        // Always a future start: a seeded "Scheduled" lesson must actually stay Scheduled when
        // GetReviewEligibility's own lazy sweep runs (it sweeps EVERY due Scheduled lesson for
        // this student as a side effect of counting) — a past-dated Scheduled row would get
        // incidentally completed by that real sweep before the assertion even runs. Status is
        // always set explicitly below, never left to the sweep to produce.
        var start = DateTimeOffset.UtcNow.AddDays(30 + Random.Shared.Next(1, 1000));
        var lesson = Lesson.CreateScheduled(Guid.NewGuid(), studentUserId, tutorUserId, start, start.AddMinutes(25), TimeProvider.System);
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        if (status != LessonStatus.Scheduled)
        {
            await db.Lessons.Where(l => l.Id == lesson.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, status), TestContext.Current.CancellationToken);
        }
    }

    private async Task SeedCompletedLessonsAsync(Guid studentUserId, Guid tutorUserId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await SeedLessonAsync(studentUserId, tutorUserId, LessonStatus.Completed);
        }
    }

    // ---- Eligibility: completed-lesson-count threshold ----

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Eligibility_BelowThreeCompletedLessons_IsNotEligible(int completedCount)
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonsAsync(studentId, tutorId, completedCount);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.GetAsync($"/api/reviews/tutors/{tutorId}/me/eligibility", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetReviewEligibilityResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.False(body!.IsEligible);
        Assert.Equal(completedCount, body.CompletedLessonCount);
        Assert.Equal(3, body.RequiredCompletedLessonCount);
        Assert.Null(body.ExistingReview);
    }

    [Fact]
    public async Task Eligibility_ExactlyThreeCompletedLessons_IsEligible()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonsAsync(studentId, tutorId, 3);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.GetAsync($"/api/reviews/tutors/{tutorId}/me/eligibility", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetReviewEligibilityResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.True(body!.IsEligible);
        Assert.Equal(3, body.CompletedLessonCount);
    }

    [Fact]
    public async Task Eligibility_FourOrMoreCompletedLessons_RemainsEligible()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonsAsync(studentId, tutorId, 5);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.GetAsync($"/api/reviews/tutors/{tutorId}/me/eligibility", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetReviewEligibilityResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.True(body!.IsEligible);
        Assert.Equal(5, body.CompletedLessonCount);
    }

    [Fact]
    public async Task Eligibility_CompletedLessonsWithAnotherTutor_DoNotCount()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var otherTutorId = Guid.NewGuid();
        await SeedCompletedLessonsAsync(studentId, otherTutorId, 5);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.GetAsync($"/api/reviews/tutors/{tutorId}/me/eligibility", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetReviewEligibilityResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.False(body!.IsEligible);
        Assert.Equal(0, body.CompletedLessonCount);
    }

    [Fact]
    public async Task Eligibility_CancelledLessons_DoNotCount()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedLessonAsync(studentId, tutorId, LessonStatus.Completed);
        await SeedLessonAsync(studentId, tutorId, LessonStatus.Completed);
        await SeedLessonAsync(studentId, tutorId, LessonStatus.Cancelled);
        await SeedLessonAsync(studentId, tutorId, LessonStatus.Cancelled);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.GetAsync($"/api/reviews/tutors/{tutorId}/me/eligibility", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetReviewEligibilityResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.False(body!.IsEligible);
        Assert.Equal(2, body.CompletedLessonCount);
    }

    [Fact]
    public async Task Eligibility_ScheduledLessons_DoNotCount()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedLessonAsync(studentId, tutorId, LessonStatus.Completed);
        await SeedLessonAsync(studentId, tutorId, LessonStatus.Completed);
        await SeedLessonAsync(studentId, tutorId, LessonStatus.Scheduled);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.GetAsync($"/api/reviews/tutors/{tutorId}/me/eligibility", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetReviewEligibilityResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.False(body!.IsEligible);
        Assert.Equal(2, body.CompletedLessonCount);
    }

    [Fact]
    public async Task Eligibility_NoShowLessons_DoNotCount()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedLessonAsync(studentId, tutorId, LessonStatus.Completed);
        await SeedLessonAsync(studentId, tutorId, LessonStatus.Completed);
        await SeedLessonAsync(studentId, tutorId, LessonStatus.NoShow);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.GetAsync($"/api/reviews/tutors/{tutorId}/me/eligibility", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetReviewEligibilityResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.False(body!.IsEligible);
        Assert.Equal(2, body.CompletedLessonCount);
    }

    [Fact]
    public async Task Eligibility_AnotherStudentsCompletedLessons_DoNotCount()
    {
        var studentId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonsAsync(otherStudentId, tutorId, 5);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.GetAsync($"/api/reviews/tutors/{tutorId}/me/eligibility", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetReviewEligibilityResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.False(body!.IsEligible);
        Assert.Equal(0, body.CompletedLessonCount);
    }

    // ---- Submission: create, edit, duplicate prevention ----

    [Fact]
    public async Task SubmitReview_BelowThreshold_IsRejectedAsForbidden_NeverTrustsClientSideEligibility()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonsAsync(studentId, tutorId, 2);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.PutAsJsonAsync($"/api/reviews/tutors/{tutorId}/me", new { rating = 5, comment = "Great!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReview_FirstSubmission_CreatesOneReview()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonsAsync(studentId, tutorId, 3);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.PutAsJsonAsync($"/api/reviews/tutors/{tutorId}/me", new { rating = 5, comment = "Excellent tutor." }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SubmitReviewResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(5, body!.Rating);
        Assert.Equal("Excellent tutor.", body.Comment);
        Assert.Equal(body.CreatedAtUtc, body.UpdatedAtUtc);
    }

    [Fact]
    public async Task SubmitReview_SecondSubmission_UpdatesTheExistingReview_NeverCreatesASecondOne()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonsAsync(studentId, tutorId, 3);
        var client = CreateAuthenticatedClient(studentId, "Student");

        var first = await client.PutAsJsonAsync($"/api/reviews/tutors/{tutorId}/me", new { rating = 3, comment = "It was okay." }, TestContext.Current.CancellationToken);
        var second = await client.PutAsJsonAsync($"/api/reviews/tutors/{tutorId}/me", new { rating = 5, comment = "Actually, excellent after a few more lessons." }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var firstBody = await first.Content.ReadFromJsonAsync<SubmitReviewResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        var secondBody = await second.Content.ReadFromJsonAsync<SubmitReviewResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(firstBody!.Id, secondBody!.Id);
        Assert.Equal(5, secondBody.Rating);
        Assert.Equal("Actually, excellent after a few more lessons.", secondBody.Comment);

        using var scope = fixture.Services.CreateScope();
        var reviewsDb = scope.ServiceProvider.GetRequiredService<Persistence.ReviewsDbContext>();
        var count = await reviewsDb.Reviews.CountAsync(r => r.StudentUserId == studentId && r.TutorUserId == tutorId, TestContext.Current.CancellationToken);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SubmitReview_RepeatedIdenticalSubmission_StaysAtOneReview()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonsAsync(studentId, tutorId, 3);
        var client = CreateAuthenticatedClient(studentId, "Student");

        await client.PutAsJsonAsync($"/api/reviews/tutors/{tutorId}/me", new { rating = 4, comment = "Good." }, TestContext.Current.CancellationToken);
        await client.PutAsJsonAsync($"/api/reviews/tutors/{tutorId}/me", new { rating = 4, comment = "Good." }, TestContext.Current.CancellationToken);
        await client.PutAsJsonAsync($"/api/reviews/tutors/{tutorId}/me", new { rating = 4, comment = "Good." }, TestContext.Current.CancellationToken);

        using var scope = fixture.Services.CreateScope();
        var reviewsDb = scope.ServiceProvider.GetRequiredService<Persistence.ReviewsDbContext>();
        var count = await reviewsDb.Reviews.CountAsync(r => r.StudentUserId == studentId && r.TutorUserId == tutorId, TestContext.Current.CancellationToken);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SubmitReview_ConcurrentFirstSubmissions_ResultInExactlyOnePersistedReview()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonsAsync(studentId, tutorId, 3);
        var clientA = CreateAuthenticatedClient(studentId, "Student");
        var clientB = CreateAuthenticatedClient(studentId, "Student");

        var responses = await Task.WhenAll(
            clientA.PutAsJsonAsync($"/api/reviews/tutors/{tutorId}/me", new { rating = 5, comment = "A" }, TestContext.Current.CancellationToken),
            clientB.PutAsJsonAsync($"/api/reviews/tutors/{tutorId}/me", new { rating = 2, comment = "B" }, TestContext.Current.CancellationToken));

        Assert.All(responses, r => Assert.True(r.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK));

        using var scope = fixture.Services.CreateScope();
        var reviewsDb = scope.ServiceProvider.GetRequiredService<Persistence.ReviewsDbContext>();
        var count = await reviewsDb.Reviews.CountAsync(r => r.StudentUserId == studentId && r.TutorUserId == tutorId, TestContext.Current.CancellationToken);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SubmitReview_InvalidRating_IsRejected()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonsAsync(studentId, tutorId, 3);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.PutAsJsonAsync($"/api/reviews/tutors/{tutorId}/me", new { rating = 6, comment = "Too high" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReview_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await fixture.CreateClient().PutAsJsonAsync($"/api/reviews/tutors/{Guid.NewGuid()}/me", new { rating = 5 }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Eligibility_EachStudentsOwnReviewAndEligibility_IsIsolated_NoIDOR()
    {
        // The route only ever carries tutorUserId; studentUserId always comes from the JWT
        // principal, never a route/body value — there is no "whose eligibility" parameter an
        // attacker could tamper with in the first place.
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonsAsync(studentId, tutorId, 3);
        await CreateAuthenticatedClient(studentId, "Student")
            .PutAsJsonAsync($"/api/reviews/tutors/{tutorId}/me", new { rating = 5, comment = "Mine" }, TestContext.Current.CancellationToken);

        var otherStudentId = Guid.NewGuid();
        var otherClient = CreateAuthenticatedClient(otherStudentId, "Student");
        var response = await otherClient.GetAsync($"/api/reviews/tutors/{tutorId}/me/eligibility", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetReviewEligibilityResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.False(body!.IsEligible);
        Assert.Null(body.ExistingReview);
    }

    // ---- Public retrieval ----

    [Fact]
    public async Task GetTutorReviewSummary_WithNoReviews_ReturnsEmptyAndZeroedAggregate()
    {
        var response = await fixture.CreateClient().GetAsync($"/api/reviews/tutors/{Guid.NewGuid()}/summary", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TutorReviewSummaryResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(0, body!.ReviewCount);
        Assert.Equal(0, body.OverallRating);
        Assert.Empty(body.Reviews);
        Assert.Null(body.SubRatings);
    }

    [Fact]
    public async Task GetTutorReviewSummary_ReturnsOnlyThisTutorsReviews_WithCorrectAggregate()
    {
        var tutorId = Guid.NewGuid();
        var otherTutorId = Guid.NewGuid();

        var studentA = Guid.NewGuid();
        await SeedCompletedLessonsAsync(studentA, tutorId, 3);
        await CreateAuthenticatedClient(studentA, "Student")
            .PutAsJsonAsync($"/api/reviews/tutors/{tutorId}/me", new { rating = 5, comment = "Great" }, TestContext.Current.CancellationToken);

        var studentB = Guid.NewGuid();
        await SeedCompletedLessonsAsync(studentB, tutorId, 3);
        await CreateAuthenticatedClient(studentB, "Student")
            .PutAsJsonAsync($"/api/reviews/tutors/{tutorId}/me", new { rating = 3, comment = "Okay" }, TestContext.Current.CancellationToken);

        var studentC = Guid.NewGuid();
        await SeedCompletedLessonsAsync(studentC, otherTutorId, 3);
        await CreateAuthenticatedClient(studentC, "Student")
            .PutAsJsonAsync($"/api/reviews/tutors/{otherTutorId}/me", new { rating = 1, comment = "Not for this tutor" }, TestContext.Current.CancellationToken);

        var response = await fixture.CreateClient().GetAsync($"/api/reviews/tutors/{tutorId}/summary", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<TutorReviewSummaryResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.Equal(2, body!.ReviewCount);
        Assert.Equal(4.0, body.OverallRating);
        Assert.DoesNotContain(body.Reviews, r => r.Rating == 1);
    }
}
