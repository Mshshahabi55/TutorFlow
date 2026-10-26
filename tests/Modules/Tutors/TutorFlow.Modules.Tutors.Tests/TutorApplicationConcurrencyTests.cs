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

namespace TutorFlow.Modules.Tutors.Tests;

/// <summary>Audit finding F1 regression coverage — before this fix, <c>TutorApplication</c> (and
/// therefore its owned-JSON <c>CertificateEntries</c> column) had no concurrency token, so two
/// admin decisions loaded from the same pre-conflict snapshot would silently lost-update one
/// another on <c>SaveChangesAsync</c>: whichever commits last overwrites the other's decision with
/// no exception, no log, and both callers seeing success. Same deterministic
/// two-DbContext-scope technique as <c>Bookings.ConcurrencyTests.HoldCreation_WithGenuinelyConcurrentTransactions_SecondGetsExclusionViolation</c>
/// — forces the exact interleaving rather than racing real concurrent HTTP calls, which would only
/// *sometimes* land in the conflicting order.</summary>
[Collection(TutorsHostCollection.Name)]
public class TutorApplicationConcurrencyTests(TutorsApiFixture fixture) : IClassFixture<TutorsApiFixture>
{
    private static readonly byte[] ValidJpegBytes =
        [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0xFF, 0xD9];

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

    /// <summary>One application, two independent PendingReview certificates — the realistic shape
    /// of the race: two admins reviewing different certificates on the same tutor's application at
    /// the same time, not two admins deciding the exact same certificate.</summary>
    private async Task<(Guid ApplicationId, Guid FirstCertificateId, Guid SecondCertificateId)> CreateApplicationWithTwoPendingReviewCertificatesAsync()
    {
        var tutorClient = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await tutorClient.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await tutorClient.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                certificateEntries = new[]
                {
                    new { name = "TEFL Certificate", issuingOrganization = "ITA", description = (string?)null, subjectId = (Guid?)null, startYear = (int?)null, endYear = (int?)null },
                    new { name = "CELTA Certificate", issuingOrganization = "Cambridge", description = (string?)null, subjectId = (Guid?)null, startYear = (int?)null, endYear = (int?)null },
                },
            },
            TestContext.Current.CancellationToken);

        var current = await tutorClient.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var app = await current.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        var firstCertificateId = app!.CertificateEntries[0].Id;
        var secondCertificateId = app.CertificateEntries[1].Id;

        await tutorClient.PostAsync(
            $"/api/tutors/me/certificates/{firstCertificateId}/document", BuildUpload(ValidJpegBytes, "cert1.jpg", "image/jpeg"), TestContext.Current.CancellationToken);
        await tutorClient.PostAsync(
            $"/api/tutors/me/certificates/{secondCertificateId}/document", BuildUpload(ValidJpegBytes, "cert2.jpg", "image/jpeg"), TestContext.Current.CancellationToken);

        return (app.Id, firstCertificateId, secondCertificateId);
    }

    [Fact]
    public async Task ConcurrentDecisionsOnDifferentCertificates_SecondWriterGetsConcurrencyConflict_NeverSilentlyOverwritesTheFirst()
    {
        var (applicationId, firstCertificateId, secondCertificateId) = await CreateApplicationWithTwoPendingReviewCertificatesAsync();
        var clock = TimeProvider.System;

        using var scopeA = fixture.Services.CreateScope();
        using var scopeB = fixture.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<TutorsDbContext>();

        // Both "admins" load the application before either one writes — the exact precondition
        // for a lost update: each holds its own in-memory snapshot of the other's certificate too.
        var appA = await dbA.TutorApplications.FirstAsync(a => a.Id == applicationId, TestContext.Current.CancellationToken);
        var appB = await dbB.TutorApplications.FirstAsync(a => a.Id == applicationId, TestContext.Current.CancellationToken);

        Assert.True(appA.VerifyCertificate(firstCertificateId, clock).IsSuccess);
        await dbA.SaveChangesAsync(TestContext.Current.CancellationToken);

        // appB's snapshot is now stale — committing it would overwrite firstCertificateId back to
        // PendingReview (appB never saw A's Verify) purely because EF rewrites the *entire*
        // owned-JSON CertificateEntries column on every save. Without the xmin concurrency token
        // this fix adds, this SaveChangesAsync would have silently succeeded and erased A's
        // decision. With it, Postgres's own xmin no longer matches what appB read, so EF throws.
        Assert.True(appB.RejectCertificate(secondCertificateId, "Blurry.", clock).IsSuccess);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync(TestContext.Current.CancellationToken));

        using var verifyScope = fixture.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var finalApp = await verifyDb.TutorApplications.AsNoTracking().FirstAsync(a => a.Id == applicationId, TestContext.Current.CancellationToken);

        // A's decision survives intact; B's rejected (never-committed) write left the second
        // certificate exactly where it was — not silently reverted, not silently half-applied.
        Assert.Equal(CertificateVerificationStatus.Verified, finalApp.CertificateEntries.First(c => c.Id == firstCertificateId).VerificationStatus);
        Assert.Equal(CertificateVerificationStatus.PendingReview, finalApp.CertificateEntries.First(c => c.Id == secondCertificateId).VerificationStatus);
    }
}
