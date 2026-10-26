using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Lessons.Domain;
using TutorFlow.Modules.Lessons.Persistence;
using GetMyLessonsResponse = TutorFlow.Modules.Lessons.Features.GetMyLessons.GetMyLessons.Response;

namespace TutorFlow.Modules.Lessons.Tests;

[Collection(LessonsHostCollection.Name)]
public class LessonsEndpointsTests(LessonsApiFixture fixture) : IClassFixture<LessonsApiFixture>
{
    private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", LessonsApiFixture.CreateAccessToken(userId, roles));
        return client;
    }

    private async Task<Lesson> SeedLessonAsync(Guid studentUserId, Guid tutorUserId, DateTimeOffset startUtc)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LessonsDbContext>();
        var lesson = Lesson.CreateScheduled(Guid.NewGuid(), studentUserId, tutorUserId, startUtc, startUtc.AddMinutes(25), TimeProvider.System);
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return lesson;
    }

    [Fact]
    public async Task GetMyLessons_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await fixture.CreateClient().GetAsync("/api/lessons/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyLessons_WithNoLessons_ReturnsEmptyList()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await client.GetAsync("/api/lessons/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetMyLessonsResponse>(LessonsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Empty(body!.Lessons);
    }

    [Fact]
    public async Task GetMyLessons_ReturnsOnlyTheCallersOwnLessons_NeverAnotherStudents()
    {
        var studentId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var mine = await SeedLessonAsync(studentId, tutorId, DateTimeOffset.UtcNow.AddDays(2));
        await SeedLessonAsync(otherStudentId, tutorId, DateTimeOffset.UtcNow.AddDays(3));

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.GetAsync("/api/lessons/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetMyLessonsResponse>(LessonsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        var summary = Assert.Single(body!.Lessons);
        Assert.Equal(mine.Id, summary.Id);
        Assert.Equal(tutorId, summary.TutorUserId);
        Assert.Equal(LessonStatus.Scheduled, summary.Status);
        Assert.Equal(25, summary.DurationMinutes);
    }

    [Fact]
    public async Task GetMyLessons_OrdersByStartTimeDescending_MatchingBookingsOwnConvention()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var earlier = await SeedLessonAsync(studentId, tutorId, DateTimeOffset.UtcNow.AddDays(1));
        var later = await SeedLessonAsync(studentId, tutorId, DateTimeOffset.UtcNow.AddDays(5));

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.GetAsync("/api/lessons/me", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<GetMyLessonsResponse>(LessonsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(2, body!.Lessons.Count);
        Assert.Equal(later.Id, body.Lessons[0].Id);
        Assert.Equal(earlier.Id, body.Lessons[1].Id);
    }

    [Fact]
    public async Task GetMyLessons_AsTutor_RoleIsRejected_ThisEndpointIsStudentOnly()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await client.GetAsync("/api/lessons/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyLessons_ForAPastDueLesson_ReflectsTheRealCompletionTransition_NoManualActionRequired()
    {
        // Real system clock, no fake — this lesson's EndUtc + 15min threshold has genuinely
        // already passed relative to wall-clock time (requirement: works for historical lessons
        // with no background job needed to have run at the exact threshold).
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var pastDue = await SeedLessonAsync(studentId, tutorId, DateTimeOffset.UtcNow.AddDays(-1));

        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.GetAsync("/api/lessons/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetMyLessonsResponse>(LessonsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        var summary = Assert.Single(body!.Lessons);
        Assert.Equal(pastDue.Id, summary.Id);
        Assert.Equal(LessonStatus.Completed, summary.Status);
    }

    [Fact]
    public async Task GetMyLessons_ForAPastDueLesson_OwnershipStillEnforced_AnotherStudentNeverSeesIt()
    {
        var studentId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await SeedLessonAsync(studentId, tutorId, DateTimeOffset.UtcNow.AddDays(-1));

        var otherClient = CreateAuthenticatedClient(otherStudentId, "Student");
        var response = await otherClient.GetAsync("/api/lessons/me", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<GetMyLessonsResponse>(LessonsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Empty(body!.Lessons);
    }
}
