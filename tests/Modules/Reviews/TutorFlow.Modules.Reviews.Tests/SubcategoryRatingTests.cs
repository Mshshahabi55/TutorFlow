using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Lessons.Domain;
using TutorFlow.Modules.Lessons.Persistence;
using GetSubcategoryRatingEligibilityResponse = TutorFlow.Modules.Reviews.Features.GetSubcategoryRatingEligibility.GetSubcategoryRatingEligibility.Response;
using SubmitSubcategoryRatingResponse = TutorFlow.Modules.Reviews.Features.SubmitSubcategoryRating.SubmitSubcategoryRating.Response;
using TutorReviewSummaryResponse = TutorFlow.Modules.Reviews.Features.GetTutorReviewSummary.GetTutorReviewSummary.Response;

namespace TutorFlow.Modules.Reviews.Tests;

// DOCUMENTED (help.preply.com/articles/11196386): a genuinely separate flow from the written
// Review — eligible after >=1 completed lesson with the tutor (not 3), at most one submission per
// calendar month, each dimension (Clarity/Progress/Preparation/Reassurance) independently
// optional, never student-editable afterward (a new row each eligible month, never an upsert).
[Collection(ReviewsHostCollection.Name)]
public class SubcategoryRatingTests(ReviewsApiFixture fixture) : IClassFixture<ReviewsApiFixture>
{
    private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ReviewsApiFixture.CreateAccessToken(userId, roles));
        return client;
    }

    private async Task SeedCompletedLessonAsync(Guid studentUserId, Guid tutorUserId)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LessonsDbContext>();
        var start = DateTimeOffset.UtcNow.AddDays(30 + Random.Shared.Next(1, 1000));
        var lesson = Lesson.CreateScheduled(Guid.NewGuid(), studentUserId, tutorUserId, start, start.AddMinutes(25), TimeProvider.System);
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await db.Lessons.Where(l => l.Id == lesson.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, LessonStatus.Completed), TestContext.Current.CancellationToken);
    }

    // ---- Eligibility ----

    [Fact]
    public async Task Eligibility_WithNoCompletedLessons_IsNotEligible()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.GetAsync($"/api/reviews/tutors/{tutorId}/me/subcategory-eligibility", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetSubcategoryRatingEligibilityResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.False(body!.IsEligible);
        Assert.False(body.HasCompletedAtLeastOneLesson);
    }

    [Fact]
    public async Task Eligibility_WithExactlyOneCompletedLesson_IsEligible_UnlikeTheWrittenReviewsThreeLessonRule()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonAsync(studentId, tutorId);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.GetAsync($"/api/reviews/tutors/{tutorId}/me/subcategory-eligibility", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetSubcategoryRatingEligibilityResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.True(body!.IsEligible);
        Assert.True(body.HasCompletedAtLeastOneLesson);
        Assert.False(body.AlreadySubmittedThisMonth);
    }

    [Fact]
    public async Task Eligibility_AnotherStudentsCompletedLessonOrSubmission_DoesNotAffectMine()
    {
        var studentId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonAsync(otherStudentId, tutorId);
        await CreateAuthenticatedClient(otherStudentId, "Student")
            .PostAsJsonAsync($"/api/reviews/tutors/{tutorId}/me/subcategory-ratings", new { clarity = 5 }, TestContext.Current.CancellationToken);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.GetAsync($"/api/reviews/tutors/{tutorId}/me/subcategory-eligibility", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetSubcategoryRatingEligibilityResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.False(body!.IsEligible);
        Assert.False(body.HasCompletedAtLeastOneLesson);
    }

    // ---- Submission ----

    [Fact]
    public async Task Submit_WithAllFourValues_Succeeds()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonAsync(studentId, tutorId);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.PostAsJsonAsync(
            $"/api/reviews/tutors/{tutorId}/me/subcategory-ratings",
            new { clarity = 5, progress = 4, preparation = 5, reassurance = 3 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SubmitSubcategoryRatingResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(5, body!.Clarity);
        Assert.Equal(4, body.Progress);
        Assert.Equal(5, body.Preparation);
        Assert.Equal(3, body.Reassurance);
    }

    [Fact]
    public async Task Submit_WithSomeValuesOmitted_Succeeds_EachDimensionIsIndependentlyOptional()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonAsync(studentId, tutorId);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.PostAsJsonAsync(
            $"/api/reviews/tutors/{tutorId}/me/subcategory-ratings",
            new { clarity = 5 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SubmitSubcategoryRatingResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(5, body!.Clarity);
        Assert.Null(body.Progress);
        Assert.Null(body.Preparation);
        Assert.Null(body.Reassurance);
    }

    [Fact]
    public async Task Submit_WithAllValuesOmitted_StillSucceeds_MatchingTheDocumentedNoneOption()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonAsync(studentId, tutorId);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.PostAsJsonAsync($"/api/reviews/tutors/{tutorId}/me/subcategory-ratings", new { }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WithInvalidRatingValue_IsRejected()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonAsync(studentId, tutorId);

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.PostAsJsonAsync(
            $"/api/reviews/tutors/{tutorId}/me/subcategory-ratings",
            new { clarity = 6 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await fixture.CreateClient().PostAsJsonAsync(
            $"/api/reviews/tutors/{Guid.NewGuid()}/me/subcategory-ratings", new { clarity = 5 }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WithoutAnyCompletedLesson_IsForbidden()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.PostAsJsonAsync(
            $"/api/reviews/tutors/{tutorId}/me/subcategory-ratings", new { clarity = 5 }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Submit_ASecondTimeInTheSameCalendarMonth_IsRejectedAsConflict_DocumentedOncePerMonthRule()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonAsync(studentId, tutorId);
        var client = CreateAuthenticatedClient(studentId, "Student");

        var first = await client.PostAsJsonAsync($"/api/reviews/tutors/{tutorId}/me/subcategory-ratings", new { clarity = 5 }, TestContext.Current.CancellationToken);
        var second = await client.PostAsJsonAsync($"/api/reviews/tutors/{tutorId}/me/subcategory-ratings", new { clarity = 1 }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Persistence.ReviewsDbContext>();
        var count = await db.SubcategoryRatings.CountAsync(r => r.StudentUserId == studentId && r.TutorUserId == tutorId, TestContext.Current.CancellationToken);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Eligibility_AfterSubmittingThisMonth_ReflectsAlreadySubmitted()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonAsync(studentId, tutorId);
        var client = CreateAuthenticatedClient(studentId, "Student");
        await client.PostAsJsonAsync($"/api/reviews/tutors/{tutorId}/me/subcategory-ratings", new { clarity = 5 }, TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"/api/reviews/tutors/{tutorId}/me/subcategory-eligibility", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetSubcategoryRatingEligibilityResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.False(body!.IsEligible);
        Assert.True(body.AlreadySubmittedThisMonth);
    }

    [Fact]
    public async Task Submit_ConcurrentFirstSubmissions_ResultInExactlyOnePersistedRatingForThisMonth()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedCompletedLessonAsync(studentId, tutorId);
        var clientA = CreateAuthenticatedClient(studentId, "Student");
        var clientB = CreateAuthenticatedClient(studentId, "Student");

        var responses = await Task.WhenAll(
            clientA.PostAsJsonAsync($"/api/reviews/tutors/{tutorId}/me/subcategory-ratings", new { clarity = 5 }, TestContext.Current.CancellationToken),
            clientB.PostAsJsonAsync($"/api/reviews/tutors/{tutorId}/me/subcategory-ratings", new { clarity = 1 }, TestContext.Current.CancellationToken));

        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses, r => Assert.True(r.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict));

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Persistence.ReviewsDbContext>();
        var count = await db.SubcategoryRatings.CountAsync(r => r.StudentUserId == studentId && r.TutorUserId == tutorId, TestContext.Current.CancellationToken);
        Assert.Equal(1, count);
    }

    // ---- Public aggregate display ----

    [Fact]
    public async Task GetTutorReviewSummary_WithNoSubcategoryRatings_ReturnsNullSubRatings()
    {
        var response = await fixture.CreateClient().GetAsync($"/api/reviews/tutors/{Guid.NewGuid()}/summary", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<TutorReviewSummaryResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.Null(body!.SubRatings);
    }

    [Fact]
    public async Task GetTutorReviewSummary_WithSubcategoryRatings_ReturnsPerDimensionAverageAndIndependentCounts()
    {
        var tutorId = Guid.NewGuid();

        var studentA = Guid.NewGuid();
        await SeedCompletedLessonAsync(studentA, tutorId);
        await CreateAuthenticatedClient(studentA, "Student")
            .PostAsJsonAsync($"/api/reviews/tutors/{tutorId}/me/subcategory-ratings", new { clarity = 4, progress = 5 }, TestContext.Current.CancellationToken);

        var studentB = Guid.NewGuid();
        await SeedCompletedLessonAsync(studentB, tutorId);
        await CreateAuthenticatedClient(studentB, "Student")
            .PostAsJsonAsync($"/api/reviews/tutors/{tutorId}/me/subcategory-ratings", new { clarity = 2 }, TestContext.Current.CancellationToken);

        var response = await fixture.CreateClient().GetAsync($"/api/reviews/tutors/{tutorId}/summary", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<TutorReviewSummaryResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.NotNull(body!.SubRatings);
        // Clarity: (4+2)/2 = 3, based on 2 submissions. Progress: only 1 submission (student A).
        Assert.Equal(3.0, body.SubRatings!.Clarity.Average);
        Assert.Equal(2, body.SubRatings.Clarity.Count);
        Assert.Equal(5.0, body.SubRatings.Progress.Average);
        Assert.Equal(1, body.SubRatings.Progress.Count);
        Assert.Null(body.SubRatings.Preparation.Average);
        Assert.Equal(0, body.SubRatings.Preparation.Count);
    }

    [Fact]
    public async Task GetTutorReviewSummary_SubcategoryRatings_OnlyCountThisTutor()
    {
        var tutorId = Guid.NewGuid();
        var otherTutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        await SeedCompletedLessonAsync(studentId, otherTutorId);
        await CreateAuthenticatedClient(studentId, "Student")
            .PostAsJsonAsync($"/api/reviews/tutors/{otherTutorId}/me/subcategory-ratings", new { clarity = 1 }, TestContext.Current.CancellationToken);

        var response = await fixture.CreateClient().GetAsync($"/api/reviews/tutors/{tutorId}/summary", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<TutorReviewSummaryResponse>(ReviewsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.Null(body!.SubRatings);
    }
}
