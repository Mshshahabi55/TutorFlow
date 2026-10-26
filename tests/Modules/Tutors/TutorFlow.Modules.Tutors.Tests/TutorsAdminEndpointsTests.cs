using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using GetCurrentApplicationResponse = TutorFlow.Modules.Tutors.Features.GetCurrentApplication.GetCurrentApplication.Response;
using GetPendingApplicationsResponse = TutorFlow.Modules.Tutors.Features.GetPendingTutorApplications.GetPendingTutorApplications.Response;
using GetPublicProfileResponse = TutorFlow.Modules.Tutors.Features.GetPublicProfile.GetPublicProfile.Response;

namespace TutorFlow.Modules.Tutors.Tests;

/// <summary>
/// The admin review pipeline (ADR-0028): begin-review/approve/reject/publish/suspend, all
/// gated on <c>PolicyNames.AdminOnly</c>. Covers the explicit authorization invariants required
/// of this surface: unauthenticated -&gt; 401, non-admin -&gt; 403, invalid transitions -&gt; 400,
/// repeated approval safely rejected (not silently idempotent, matching every other transition
/// in this aggregate), and an admin can never act on their own tutor application.
/// </summary>
[Collection(TutorsHostCollection.Name)]
public class TutorsAdminEndpointsTests(TutorsApiFixture fixture) : IClassFixture<TutorsApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TutorsApiFixture.CreateAccessToken(userId, roles));
        return client;
    }

    /// Drives a real application to Submitted via the self-service HTTP endpoints (not a
    /// shortcut) so the admin-side tests below exercise the real end-to-end path.
    private async Task<Guid> CreateSubmittedApplicationAsync(Guid tutorUserId, string displayName = "Pending Tutor")
    {
        var client = CreateAuthenticatedClient(tutorUserId, "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                displayName,
                headline = "Patient, experienced tutor",
                profilePhotoReference = "https://example.com/photo.jpg",
                introVideoReference = "https://example.com/intro.mp4",
                subjectIds = new[] { Guid.NewGuid() },
                spokenLanguages = new[] { new { languageCode = "en", proficiency = "Native" } },
                hourlyRate = new { amountMinorUnits = 2500, currency = "EUR" }
            },
            TestContext.Current.CancellationToken);
        await client.PostAsync("/api/tutors/applications/me/submit", null, TestContext.Current.CancellationToken);

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var application = await db.TutorApplications.AsNoTracking().FirstAsync(a => a.UserId == tutorUserId, TestContext.Current.CancellationToken);
        return application.Id;
    }

    private HttpClient AdminClient() => CreateAuthenticatedClient(Guid.NewGuid(), "Admin");

    [Fact]
    public async Task GetPendingApplications_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/tutors/admin/applications", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPendingApplications_WithoutAdminRole_ReturnsForbidden()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await client.GetAsync("/api/tutors/admin/applications", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPendingApplications_WithAdminRole_ReturnsOnlySubmittedAndUnderReview()
    {
        var submittedId = await CreateSubmittedApplicationAsync(Guid.NewGuid(), "Submitted Tutor");
        var underReviewId = await CreateSubmittedApplicationAsync(Guid.NewGuid(), "Under Review Tutor");
        var draftUserId = Guid.NewGuid();
        var draftClient = CreateAuthenticatedClient(draftUserId, "Tutor");
        await draftClient.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            var application = await db.TutorApplications.FirstAsync(a => a.Id == underReviewId, TestContext.Current.CancellationToken);
            application.BeginReview(TimeProvider.System);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var admin = AdminClient();
        var response = await admin.GetAsync("/api/tutors/admin/applications?pageSize=50", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetPendingApplicationsResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Contains(body!.Items, a => a.Id == submittedId);
        Assert.Contains(body.Items, a => a.Id == underReviewId);
        Assert.DoesNotContain(body.Items, a => a.DisplayName == null);
    }

    [Fact]
    public async Task GetPendingApplications_WithExplicitStatusFilter_ReturnsOnlyThatStatus()
    {
        var rejectedId = await CreateSubmittedApplicationAsync(Guid.NewGuid(), "Rejected Tutor");
        var stillPendingId = await CreateSubmittedApplicationAsync(Guid.NewGuid(), "Still Pending Tutor");
        var admin = AdminClient();
        await admin.PostAsJsonAsync(
            $"/api/tutors/admin/applications/{rejectedId}/reject", new { reason = "Not a fit" }, TestContext.Current.CancellationToken);

        var response = await admin.GetAsync("/api/tutors/admin/applications?status=Rejected&pageSize=50", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetPendingApplicationsResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Contains(body!.Items, a => a.Id == rejectedId);
        Assert.DoesNotContain(body.Items, a => a.Id == stillPendingId);
        Assert.All(body.Items, a => Assert.Equal(TutorApplicationStatus.Rejected, a.Status));
    }

    [Fact]
    public async Task GetApplicationForAdmin_WithAdminRole_ReturnsFullDetail()
    {
        var applicationId = await CreateSubmittedApplicationAsync(Guid.NewGuid(), "Detail View Tutor");
        var admin = AdminClient();

        var response = await admin.GetAsync($"/api/tutors/admin/applications/{applicationId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("Detail View Tutor", body!.DisplayName);
        Assert.Equal(TutorApplicationStatus.Submitted, body.Status);
    }

    [Fact]
    public async Task GetApplicationForAdmin_UnknownId_ReturnsNotFound()
    {
        var admin = AdminClient();

        var response = await admin.GetAsync($"/api/tutors/admin/applications/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BeginReview_FromSubmitted_TransitionsToUnderReview()
    {
        var applicationId = await CreateSubmittedApplicationAsync(Guid.NewGuid());
        var admin = AdminClient();

        var response = await admin.PostAsync($"/api/tutors/admin/applications/{applicationId}/begin-review", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(TutorApplicationStatus.UnderReview, body!.Status);
    }

    [Fact]
    public async Task BeginReview_WithoutAdminRole_ReturnsForbidden()
    {
        var applicationId = await CreateSubmittedApplicationAsync(Guid.NewGuid());
        var tutorActingAsReviewer = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await tutorActingAsReviewer.PostAsync(
            $"/api/tutors/admin/applications/{applicationId}/begin-review", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Approve_FromSubmitted_TransitionsToApproved_ButDoesNotPublish()
    {
        var tutorUserId = Guid.NewGuid();
        var applicationId = await CreateSubmittedApplicationAsync(tutorUserId, "Approve Only Tutor");
        var admin = AdminClient();

        var response = await admin.PostAsync($"/api/tutors/admin/applications/{applicationId}/approve", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(TutorApplicationStatus.Approved, body!.Status);
        Assert.Null(body.PublishedAtUtc);

        // Approved alone must never make the profile publicly resolvable — Publish is a
        // deliberately separate admin step (ADR-0028).
        var publicResponse = await _client.GetAsync($"/api/tutors/{body.PublicSlug}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, publicResponse.StatusCode);
    }

    [Fact]
    public async Task Approve_CalledTwice_SecondCallIsSafelyRejected_NotSilentlyIdempotent()
    {
        var applicationId = await CreateSubmittedApplicationAsync(Guid.NewGuid());
        var admin = AdminClient();
        await admin.PostAsync($"/api/tutors/admin/applications/{applicationId}/approve", null, TestContext.Current.CancellationToken);

        var response = await admin.PostAsync($"/api/tutors/admin/applications/{applicationId}/approve", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Approve_FromDraft_ReturnsBadRequest_InvalidStateTransition()
    {
        var draftUserId = Guid.NewGuid();
        var draftClient = CreateAuthenticatedClient(draftUserId, "Tutor");
        var createResponse = await draftClient.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        var admin = AdminClient();

        var response = await admin.PostAsync($"/api/tutors/admin/applications/{created!.Id}/approve", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reject_WithoutReason_ReturnsValidationProblem()
    {
        var applicationId = await CreateSubmittedApplicationAsync(Guid.NewGuid());
        var admin = AdminClient();

        var response = await admin.PostAsJsonAsync(
            $"/api/tutors/admin/applications/{applicationId}/reject", new { reason = "" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reject_WithReason_TransitionsToRejected_AndRecordsReason()
    {
        var applicationId = await CreateSubmittedApplicationAsync(Guid.NewGuid());
        var admin = AdminClient();

        var response = await admin.PostAsJsonAsync(
            $"/api/tutors/admin/applications/{applicationId}/reject",
            new { reason = "Profile photo doesn't meet guidelines" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(TutorApplicationStatus.Rejected, body!.Status);
        Assert.Equal("Profile photo doesn't meet guidelines", body.RejectionReason);
    }

    [Fact]
    public async Task Publish_FromApproved_TransitionsToPublished_AndTutorBecomesPubliclyVisible()
    {
        var applicationId = await CreateSubmittedApplicationAsync(Guid.NewGuid(), "Publish Pipeline Tutor");
        var admin = AdminClient();
        await admin.PostAsync($"/api/tutors/admin/applications/{applicationId}/approve", null, TestContext.Current.CancellationToken);

        var response = await admin.PostAsync($"/api/tutors/admin/applications/{applicationId}/publish", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(TutorApplicationStatus.Published, body!.Status);

        var publicResponse = await _client.GetAsync($"/api/tutors/{body.PublicSlug}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        var publicBody = await publicResponse.Content.ReadFromJsonAsync<GetPublicProfileResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal("Publish Pipeline Tutor", publicBody!.DisplayName);
    }

    [Fact]
    public async Task Publish_FromSubmitted_ReturnsBadRequest_InvalidStateTransition()
    {
        var applicationId = await CreateSubmittedApplicationAsync(Guid.NewGuid());
        var admin = AdminClient();

        var response = await admin.PostAsync($"/api/tutors/admin/applications/{applicationId}/publish", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Suspend_WithoutReason_ReturnsValidationProblem()
    {
        var applicationId = await CreateSubmittedApplicationAsync(Guid.NewGuid());
        var admin = AdminClient();
        await admin.PostAsync($"/api/tutors/admin/applications/{applicationId}/approve", null, TestContext.Current.CancellationToken);
        await admin.PostAsync($"/api/tutors/admin/applications/{applicationId}/publish", null, TestContext.Current.CancellationToken);

        var response = await admin.PostAsJsonAsync(
            $"/api/tutors/admin/applications/{applicationId}/suspend", new { reason = "" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Suspend_FromPublished_TransitionsToSuspended_WithReason_AndDelistsFromPublicProfile()
    {
        var applicationId = await CreateSubmittedApplicationAsync(Guid.NewGuid(), "Suspend Pipeline Tutor");
        var admin = AdminClient();
        await admin.PostAsync($"/api/tutors/admin/applications/{applicationId}/approve", null, TestContext.Current.CancellationToken);
        var publishResponse = await admin.PostAsync($"/api/tutors/admin/applications/{applicationId}/publish", null, TestContext.Current.CancellationToken);
        var published = await publishResponse.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);

        var response = await admin.PostAsJsonAsync(
            $"/api/tutors/admin/applications/{applicationId}/suspend",
            new { reason = "Repeated no-show reports" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var application = await db.TutorApplications.AsNoTracking().FirstAsync(a => a.Id == applicationId, TestContext.Current.CancellationToken);
        Assert.Equal(TutorApplicationStatus.Suspended, application.Status);
        Assert.Equal("Repeated no-show reports", application.SuspensionReason);

        var publicResponse = await _client.GetAsync($"/api/tutors/{published!.PublicSlug}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, publicResponse.StatusCode);
    }

    [Fact]
    public async Task Approve_OnOwnApplication_ReturnsForbidden_AdminCannotActOnTheirOwnTutorApplication()
    {
        var adminTutorUserId = Guid.NewGuid();
        var applicationId = await CreateSubmittedApplicationAsync(adminTutorUserId, "Dual Role Applicant");
        var dualRoleClient = CreateAuthenticatedClient(adminTutorUserId, "Tutor", "Admin");

        var response = await dualRoleClient.PostAsync(
            $"/api/tutors/admin/applications/{applicationId}/approve", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reject_OnOwnApplication_ReturnsForbidden_AdminCannotActOnTheirOwnTutorApplication()
    {
        var adminTutorUserId = Guid.NewGuid();
        var applicationId = await CreateSubmittedApplicationAsync(adminTutorUserId, "Dual Role Rejecter");
        var dualRoleClient = CreateAuthenticatedClient(adminTutorUserId, "Tutor", "Admin");

        var response = await dualRoleClient.PostAsJsonAsync(
            $"/api/tutors/admin/applications/{applicationId}/reject",
            new { reason = "Self-review attempt" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Approve_OnAnotherTutorsApplication_WithAdminAndTutorDualRole_Succeeds()
    {
        // Confirms the self-action guard is scoped to "this exact application", not "any admin
        // who also happens to hold the Tutor role anywhere."
        var otherTutorApplicationId = await CreateSubmittedApplicationAsync(Guid.NewGuid(), "Someone Else's Application");
        var dualRoleAdmin = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor", "Admin");

        var response = await dualRoleAdmin.PostAsync(
            $"/api/tutors/admin/applications/{otherTutorApplicationId}/approve", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
