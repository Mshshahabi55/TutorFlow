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
using GetPublicProfileResponse = TutorFlow.Modules.Tutors.Features.GetPublicProfile.GetPublicProfile.Response;

namespace TutorFlow.Modules.Tutors.Tests;

/// <summary>Stage 2 — certificate document upload, private storage, admin verify/reject
/// lifecycle, public-profile badge exposure, and server-controlled-field protection.</summary>
[Collection(TutorsHostCollection.Name)]
public class CertificateDocumentTests(TutorsApiFixture fixture) : IClassFixture<TutorsApiFixture>
{
    private static readonly byte[] ValidJpegBytes =
        [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0xFF, 0xD9];

    private static readonly byte[] ValidPngBytes =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

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

    private static MultipartFormDataContent BuildUpload(byte[] bytes, string fileName, string contentType)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    /// <summary>Creates a Draft application with exactly one certificate entry and returns its
    /// server-assigned stable Id — every document/review test addresses the certificate by this
    /// Id, never by position.</summary>
    private async Task<Guid> CreateApplicationWithCertificateAsync(HttpClient client)
    {
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new { certificateEntries = new[] { new { name = "TEFL Certificate", issuingOrganization = "ITA", description = (string?)null, subjectId = (Guid?)null, startYear = (int?)null, endYear = (int?)null } } },
            TestContext.Current.CancellationToken);

        var response = await client.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        return body!.CertificateEntries.Single().Id;
    }

    [Fact]
    public async Task SubmitDocument_WithValidJpeg_MovesCertificateToPendingReview()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var certificateId = await CreateApplicationWithCertificateAsync(client);

        var response = await client.PostAsync(
            $"/api/tutors/me/certificates/{certificateId}/document",
            BuildUpload(ValidJpegBytes, "cert.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("PendingReview", body.GetProperty("verificationStatus").GetString());
    }

    [Fact]
    public async Task SubmitDocument_NeverAutoVerifies()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var certificateId = await CreateApplicationWithCertificateAsync(client);

        await client.PostAsync(
            $"/api/tutors/me/certificates/{certificateId}/document",
            BuildUpload(ValidJpegBytes, "cert.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken);

        var response = await client.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(CertificateVerificationStatus.PendingReview, body!.CertificateEntries.Single().VerificationStatus);
        Assert.NotEqual(CertificateVerificationStatus.Verified, body.CertificateEntries.Single().VerificationStatus);
    }

    [Fact]
    public async Task SubmitDocument_WithDisallowedFileContent_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var certificateId = await CreateApplicationWithCertificateAsync(client);
        var notAnImage = "this is plain text, not an image"u8.ToArray();

        var response = await client.PostAsync(
            $"/api/tutors/me/certificates/{certificateId}/document",
            BuildUpload(notAnImage, "fake.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitDocument_ExceedingSizeLimit_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var certificateId = await CreateApplicationWithCertificateAsync(client);
        var oversized = new byte[Infrastructure.LocalCertificateDocumentStorage.MaxFileSizeBytes + 1];
        Array.Copy(ValidJpegBytes, oversized, ValidJpegBytes.Length);

        var response = await client.PostAsync(
            $"/api/tutors/me/certificates/{certificateId}/document",
            BuildUpload(oversized, "big.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitDocument_ForNonexistentCertificate_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        var response = await client.PostAsync(
            $"/api/tutors/me/certificates/{Guid.NewGuid()}/document",
            BuildUpload(ValidJpegBytes, "cert.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitDocument_Unauthenticated_ReturnsUnauthorized()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsync(
            $"/api/tutors/me/certificates/{Guid.NewGuid()}/document",
            BuildUpload(ValidJpegBytes, "cert.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DownloadOwnDocument_AfterUpload_StreamsItBackWithNoStoreCacheHeader()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var certificateId = await CreateApplicationWithCertificateAsync(client);
        await client.PostAsync(
            $"/api/tutors/me/certificates/{certificateId}/document",
            BuildUpload(ValidJpegBytes, "cert.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"/api/tutors/me/certificates/{certificateId}/document", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? "");
        var bytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ValidJpegBytes, bytes);
    }

    [Fact]
    public async Task DownloadDocument_ForAnotherTutorsCertificate_ReturnsNotFound()
    {
        var ownerClient = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var certificateId = await CreateApplicationWithCertificateAsync(ownerClient);
        await ownerClient.PostAsync(
            $"/api/tutors/me/certificates/{certificateId}/document",
            BuildUpload(ValidJpegBytes, "cert.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken);

        var attackerClient = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await attackerClient.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        // The IDOR case: a different authenticated tutor tries to address the first tutor's
        // certificate by its (guessable-format but not guessable-value) Id.
        var response = await attackerClient.GetAsync($"/api/tutors/me/certificates/{certificateId}/document", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WithdrawDocument_RemovesItAndResetsToNone()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var certificateId = await CreateApplicationWithCertificateAsync(client);
        await client.PostAsync(
            $"/api/tutors/me/certificates/{certificateId}/document",
            BuildUpload(ValidJpegBytes, "cert.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken);

        var withdraw = await client.DeleteAsync($"/api/tutors/me/certificates/{certificateId}/document", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, withdraw.StatusCode);

        var response = await client.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(CertificateVerificationStatus.None, body!.CertificateEntries.Single().VerificationStatus);
    }

    [Fact]
    public async Task UpdateDraft_CannotSpoofVerificationStatusOrFileReference()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        // The raw JSON payload includes fields CertificateEntryDraftInput does not declare —
        // proves they're silently ignored (ASP.NET Core's default JSON deserialization), not
        // merely "not sent by our own client code" (which would prove nothing about the server).
        var response = await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                certificateEntries = new[]
                {
                    new
                    {
                        name = "Spoofed",
                        issuingOrganization = "Org",
                        description = (string?)null,
                        subjectId = (Guid?)null,
                        startYear = (int?)null,
                        endYear = (int?)null,
                        fileReference = "/private-storage/tutor-certificates/someone-elses-file.jpg",
                        verificationStatus = "Verified",
                        rejectionReason = "spoofed",
                    },
                },
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var current = await client.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await current.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        var entry = body!.CertificateEntries.Single();
        Assert.Equal(CertificateVerificationStatus.None, entry.VerificationStatus);
        Assert.False(entry.HasDocument);
        Assert.Null(entry.RejectionReason);
    }

    private async Task<(Guid ApplicationId, Guid CertificateId, HttpClient TutorClient)> CreatePendingReviewCertificateAsync()
    {
        var tutorClient = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var certificateId = await CreateApplicationWithCertificateAsync(tutorClient);
        await tutorClient.PostAsync(
            $"/api/tutors/me/certificates/{certificateId}/document",
            BuildUpload(ValidJpegBytes, "cert.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken);

        var current = await tutorClient.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await current.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        return (body!.Id, certificateId, tutorClient);
    }

    [Fact]
    public async Task VerifyCertificate_WhenPendingReview_SucceedsAndDeletesTheDocument()
    {
        var (applicationId, certificateId, tutorClient) = await CreatePendingReviewCertificateAsync();
        var admin = CreateAuthenticatedClient(Guid.NewGuid(), "Admin");

        var response = await admin.PostAsync(
            $"/api/tutors/admin/applications/{applicationId}/certificates/{certificateId}/verify", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var current = await tutorClient.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await current.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        var entry = body!.CertificateEntries.Single();
        Assert.Equal(CertificateVerificationStatus.Verified, entry.VerificationStatus);
        // Approved retention policy: the document is deleted on the final decision, only the
        // status is retained.
        Assert.False(entry.HasDocument);
    }

    [Fact]
    public async Task VerifyCertificate_WhenNotPendingReview_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var certificateId = await CreateApplicationWithCertificateAsync(client);
        var current = await client.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await current.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        var admin = CreateAuthenticatedClient(Guid.NewGuid(), "Admin");

        // Never uploaded a document — still at VerificationStatus.None.
        var response = await admin.PostAsync(
            $"/api/tutors/admin/applications/{body!.Id}/certificates/{certificateId}/verify", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VerifyCertificate_Twice_SecondCallFails()
    {
        var (applicationId, certificateId, _) = await CreatePendingReviewCertificateAsync();
        var admin = CreateAuthenticatedClient(Guid.NewGuid(), "Admin");

        var first = await admin.PostAsync(
            $"/api/tutors/admin/applications/{applicationId}/certificates/{certificateId}/verify", null, TestContext.Current.CancellationToken);
        var second = await admin.PostAsync(
            $"/api/tutors/admin/applications/{applicationId}/certificates/{certificateId}/verify", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task VerifyCertificate_WithoutAdminRole_ReturnsForbidden()
    {
        var (applicationId, certificateId, _) = await CreatePendingReviewCertificateAsync();
        var nonAdmin = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await nonAdmin.PostAsync(
            $"/api/tutors/admin/applications/{applicationId}/certificates/{certificateId}/verify", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RejectCertificate_WithReason_SucceedsAndDeletesTheDocument()
    {
        var (applicationId, certificateId, tutorClient) = await CreatePendingReviewCertificateAsync();
        var admin = CreateAuthenticatedClient(Guid.NewGuid(), "Admin");

        var response = await admin.PostAsJsonAsync(
            $"/api/tutors/admin/applications/{applicationId}/certificates/{certificateId}/reject",
            new { reason = "Document is illegible." },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var current = await tutorClient.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await current.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        var entry = body!.CertificateEntries.Single();
        Assert.Equal(CertificateVerificationStatus.Rejected, entry.VerificationStatus);
        Assert.Equal("Document is illegible.", entry.RejectionReason);
        Assert.False(entry.HasDocument);
    }

    [Fact]
    public async Task RejectCertificate_WithoutReason_ReturnsBadRequest()
    {
        var (applicationId, certificateId, _) = await CreatePendingReviewCertificateAsync();
        var admin = CreateAuthenticatedClient(Guid.NewGuid(), "Admin");

        var response = await admin.PostAsJsonAsync(
            $"/api/tutors/admin/applications/{applicationId}/certificates/{certificateId}/reject",
            new { reason = "" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RejectedCertificate_CanBeResubmittedWithAReplacementDocument()
    {
        var (_, certificateId, tutorClient) = await CreatePendingReviewCertificateAsync();
        var current = await tutorClient.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var app = await current.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        var admin = CreateAuthenticatedClient(Guid.NewGuid(), "Admin");
        await admin.PostAsJsonAsync(
            $"/api/tutors/admin/applications/{app!.Id}/certificates/{certificateId}/reject",
            new { reason = "Blurry." },
            TestContext.Current.CancellationToken);

        var resubmit = await tutorClient.PostAsync(
            $"/api/tutors/me/certificates/{certificateId}/document",
            BuildUpload(ValidPngBytes, "cert2.png", "image/png"),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, resubmit.StatusCode);

        var reloaded = await tutorClient.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await reloaded.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(CertificateVerificationStatus.PendingReview, body!.CertificateEntries.Single().VerificationStatus);
    }

    [Fact]
    public async Task EditingCertificateDetailsAfterVerification_ResetsVerificationToNone()
    {
        var (applicationId, certificateId, tutorClient) = await CreatePendingReviewCertificateAsync();
        var admin = CreateAuthenticatedClient(Guid.NewGuid(), "Admin");
        await admin.PostAsync(
            $"/api/tutors/admin/applications/{applicationId}/certificates/{certificateId}/verify", null, TestContext.Current.CancellationToken);

        // Edit a descriptive field via the ordinary draft-update contract — same Id, different name.
        await tutorClient.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                certificateEntries = new[]
                {
                    new { id = certificateId, name = "TEFL Certificate (renamed)", issuingOrganization = "ITA", description = (string?)null, subjectId = (Guid?)null, startYear = (int?)null, endYear = (int?)null },
                },
            },
            TestContext.Current.CancellationToken);

        var reloaded = await tutorClient.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await reloaded.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        var entry = body!.CertificateEntries.Single();
        Assert.Equal("TEFL Certificate (renamed)", entry.Name);
        Assert.Equal(CertificateVerificationStatus.None, entry.VerificationStatus);
    }

    [Fact]
    public async Task RemovingACertificateEntirely_OrphansItsFileForCleanup()
    {
        var (_, certificateId, tutorClient) = await CreatePendingReviewCertificateAsync();

        using var scope = fixture.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<Infrastructure.ICertificateDocumentStorage>();
        // The raw storage filename is deliberately not part of the HTTP response (audit finding
        // F3 — see GetCurrentApplication.CertificateEntryResponse's own doc comment), so this
        // test-only assertion reads it directly from the database instead.
        var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var applicationBeforeRemoval = await db.TutorApplications.AsNoTracking().FirstAsync(a => a.CertificateEntries.Any(c => c.Id == certificateId), TestContext.Current.CancellationToken);
        var fileReference = applicationBeforeRemoval.CertificateEntries.Single().FileReference!;

        // Submitting an empty certificateEntries list removes the only entry entirely.
        await tutorClient.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new { certificateEntries = Array.Empty<object>() },
            TestContext.Current.CancellationToken);

        // The handler's own post-save cleanup should already have deleted it synchronously.
        Assert.Null(storage.OpenRead(fileReference));
    }

    private async Task<(string Slug, Guid CertificateId)> CreatePublishedTutorWithCertificateAsync(Guid userId)
    {
        var client = CreateAuthenticatedClient(userId, "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                displayName = "Certificate Badge Tutor",
                headline = "Patient, experienced tutor",
                profilePhotoReference = "https://example.com/photo.jpg",
                introVideoReference = "https://example.com/intro.mp4",
                subjectIds = new[] { Guid.NewGuid() },
                spokenLanguages = new[] { new { languageCode = "en", proficiency = "Native" } },
                hourlyRate = new { amountMinorUnits = 2500, currency = "EUR" },
                certificateEntries = new[]
                {
                    new { name = "TEFL Certificate", issuingOrganization = "ITA", description = (string?)null, subjectId = (Guid?)null, startYear = (int?)null, endYear = (int?)null },
                },
            },
            TestContext.Current.CancellationToken);
        var current = await client.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var currentBody = await current.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        var localCertificateId = currentBody!.CertificateEntries.Single().Id;

        await client.PostAsync(
            $"/api/tutors/me/certificates/{localCertificateId}/document",
            BuildUpload(ValidJpegBytes, "cert.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken);
        await client.PostAsync("/api/tutors/applications/me/submit", null, TestContext.Current.CancellationToken);

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            var application = await db.TutorApplications.FirstAsync(a => a.UserId == userId, TestContext.Current.CancellationToken);
            application.BeginReview(TimeProvider.System);
            application.Approve(TimeProvider.System);
            application.VerifyCertificate(localCertificateId, TimeProvider.System);
            application.Publish(TimeProvider.System);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            return (application.PublicSlug!, localCertificateId);
        }
    }

    [Fact]
    public async Task PublicProfile_ShowsIsVerifiedTrue_ForAVerifiedCertificate_AndNoInternalFields()
    {
        var (slug, _) = await CreatePublishedTutorWithCertificateAsync(Guid.NewGuid());

        var response = await fixture.CreateClient().GetAsync($"/api/tutors/{slug}", TestContext.Current.CancellationToken);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var body = await JsonSerializer.DeserializeAsync<GetPublicProfileResponse>(
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), JsonOptions, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body!.CertificateEntries.Single().IsVerified);
        Assert.DoesNotContain("fileReference", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rejectionReason", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("verificationStatus", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyCertificate_EvictsThePublicProfileCache()
    {
        var userId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(userId, "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                displayName = "Cache Eviction Tutor",
                headline = "Patient, experienced tutor",
                profilePhotoReference = "https://example.com/photo.jpg",
                introVideoReference = "https://example.com/intro.mp4",
                subjectIds = new[] { Guid.NewGuid() },
                spokenLanguages = new[] { new { languageCode = "en", proficiency = "Native" } },
                hourlyRate = new { amountMinorUnits = 2500, currency = "EUR" },
                certificateEntries = new[]
                {
                    new { name = "TEFL Certificate", issuingOrganization = "ITA", description = (string?)null, subjectId = (Guid?)null, startYear = (int?)null, endYear = (int?)null },
                },
            },
            TestContext.Current.CancellationToken);
        var current = await client.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var currentBody = await current.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        var certificateId = currentBody!.CertificateEntries.Single().Id;
        await client.PostAsync(
            $"/api/tutors/me/certificates/{certificateId}/document",
            BuildUpload(ValidJpegBytes, "cert.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken);
        await client.PostAsync("/api/tutors/applications/me/submit", null, TestContext.Current.CancellationToken);

        string slug;
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            var application = await db.TutorApplications.FirstAsync(a => a.UserId == userId, TestContext.Current.CancellationToken);
            application.BeginReview(TimeProvider.System);
            application.Approve(TimeProvider.System);
            application.Publish(TimeProvider.System);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            slug = application.PublicSlug!;
        }

        var anon = fixture.CreateClient();
        var firstRead = await anon.GetAsync($"/api/tutors/{slug}", TestContext.Current.CancellationToken);
        var firstBody = await firstRead.Content.ReadFromJsonAsync<GetPublicProfileResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.False(firstBody!.CertificateEntries.Single().IsVerified);

        Guid applicationId;
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            applicationId = (await db.TutorApplications.AsNoTracking().FirstAsync(a => a.UserId == userId, TestContext.Current.CancellationToken)).Id;
        }

        var admin = CreateAuthenticatedClient(Guid.NewGuid(), "Admin");
        await admin.PostAsync(
            $"/api/tutors/admin/applications/{applicationId}/certificates/{certificateId}/verify", null, TestContext.Current.CancellationToken);

        // If eviction didn't run, this would still return the pre-verification cached response
        // within the 5-minute TTL — same HttpClient against the same output-cache store.
        var secondRead = await anon.GetAsync($"/api/tutors/{slug}", TestContext.Current.CancellationToken);
        var secondBody = await secondRead.Content.ReadFromJsonAsync<GetPublicProfileResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.True(secondBody!.CertificateEntries.Single().IsVerified);
    }

    [Fact]
    public async Task RejectCertificate_SelfDealingGuard_AdminCannotActOnTheirOwnApplication()
    {
        var adminUserId = Guid.NewGuid();
        var admin = CreateAuthenticatedClient(adminUserId, "Tutor", "Admin");
        var certificateId = await CreateApplicationWithCertificateAsync(admin);
        await admin.PostAsync(
            $"/api/tutors/me/certificates/{certificateId}/document",
            BuildUpload(ValidJpegBytes, "cert.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken);
        var current = await admin.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await current.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);

        var response = await admin.PostAsync(
            $"/api/tutors/admin/applications/{body!.Id}/certificates/{certificateId}/verify", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DownloadCertificateDocumentForAdmin_SelfDealingGuard_AdminCannotViewTheirOwnApplicationsDocument()
    {
        // Audit finding F4 — the admin "view document" endpoint was the one certificate-review
        // action missing the same ownerUserId == adminUserId guard every other admin action
        // (BeginReview/Approve/Reject/Publish/Suspend/VerifyCertificate/RejectCertificate) already
        // applies.
        var adminUserId = Guid.NewGuid();
        var admin = CreateAuthenticatedClient(adminUserId, "Tutor", "Admin");
        var certificateId = await CreateApplicationWithCertificateAsync(admin);
        await admin.PostAsync(
            $"/api/tutors/me/certificates/{certificateId}/document",
            BuildUpload(ValidJpegBytes, "cert.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken);
        var current = await admin.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await current.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);

        var response = await admin.GetAsync(
            $"/api/tutors/admin/applications/{body!.Id}/certificates/{certificateId}/document", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
