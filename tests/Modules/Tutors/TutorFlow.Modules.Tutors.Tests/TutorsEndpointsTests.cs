using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using CreateApplicationResponse = TutorFlow.Modules.Tutors.Features.CreateApplication.CreateApplication.Response;
using GetCurrentApplicationResponse = TutorFlow.Modules.Tutors.Features.GetCurrentApplication.GetCurrentApplication.Response;
using GetCurrentStatusResponse = TutorFlow.Modules.Tutors.Features.GetCurrentStatus.GetCurrentStatus.Response;
using GetPublicProfileResponse = TutorFlow.Modules.Tutors.Features.GetPublicProfile.GetPublicProfile.Response;
using SubmitApplicationResponse = TutorFlow.Modules.Tutors.Features.SubmitApplication.SubmitApplication.Response;

namespace TutorFlow.Modules.Tutors.Tests;

[Collection(TutorsHostCollection.Name)]
public class TutorsEndpointsTests(TutorsApiFixture fixture) : IClassFixture<TutorsApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    // HttpClient's ReadFromJsonAsync has no knowledge of the server's DI-configured
    // JsonOptions (Program.cs's JsonStringEnumConverter) — it always needs options passed
    // explicitly to deserialize a string-enum wire value like "Draft" or "Native".
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

    [Fact]
    public async Task CreateApplication_WithTutorRole_ReturnsCreatedDraft()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(TutorApplicationStatus.Draft, body!.Status);
    }

    [Fact]
    public async Task CreateApplication_WithoutTutorRole_ReturnsForbidden()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateApplication_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateApplication_WhenOneAlreadyExists_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        var response = await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Closes the coverage gap flagged by the 2026-10-19 About-stage parity audit: every existing
    // Proficiency-referencing test used only LanguageProficiency.Native, so the owned-JSON
    // HasConversion<string>() mapping (TutorApplicationConfiguration.cs) was never actually proven
    // against the real Postgres container for a non-Native CEFR value. Two separate DbContext
    // scopes (and AsNoTracking on the reload) are deliberate — a single shared context would
    // satisfy the assertion purely from its own change-tracker identity map without ever
    // re-deserializing the stored JSONB, which would prove nothing about the converter.
    [Fact]
    public async Task SpokenLanguageProficiency_B2_SurvivesARealPersistenceRoundTrip_ThroughTheOwnedJsonConverter()
    {
        var userId = Guid.NewGuid();
        Guid applicationId;

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            var application = TutorApplication.CreateDraft(userId, TimeProvider.System);
            application.ApplyDraftUpdate(
                new DraftUpdate(SpokenLanguages: [new TutorSpokenLanguage("es", LanguageProficiency.B2)]),
                TimeProvider.System);
            db.TutorApplications.Add(application);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            applicationId = application.Id;
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            var reloaded = await db.TutorApplications
                .AsNoTracking()
                .FirstAsync(a => a.Id == applicationId, TestContext.Current.CancellationToken);

            var language = Assert.Single(reloaded.SpokenLanguages);
            Assert.Equal("es", language.LanguageCode);
            Assert.Equal(LanguageProficiency.B2, language.Proficiency);
        }
    }

    [Fact]
    public async Task GetCurrentApplication_WithoutExistingApplication_ReturnsNotFound()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await client.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Live-verified during the 2026-10-19 Student/Tutor role-separation audit (real browser
    // session, fresh Student-only account, before any Tutor role grant): a Student must not be
    // able to read Tutor-only application data merely by being authenticated — confirmed 403 live,
    // now locked in as a regression test. Mirrors CreateApplication_WithoutTutorRole_ReturnsForbidden.
    [Fact]
    public async Task GetCurrentApplication_WithoutTutorRole_ReturnsForbidden()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await client.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentApplication_AfterCreate_ReturnsDraftWithEmptyFields()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        var response = await client.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(TutorApplicationStatus.Draft, body!.Status);
        Assert.Null(body.DisplayName);
        Assert.Empty(body.SubjectIds);
    }

    [Fact]
    public async Task UpdateDraft_PersistsPartialChanges_AndSupportsResumingAcrossRequests()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        var firstUpdate = await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new { displayName = "Ada Lovelace", headline = "Math tutor" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);

        // Simulates resuming the wizard in a later request/session: only a different
        // subset of fields is sent, and the first update's fields must still be there.
        var mathSubjectId = Guid.NewGuid();
        var secondUpdate = await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new { subjectIds = new[] { mathSubjectId }, spokenLanguages = new[] { new { languageCode = "en", proficiency = "Native" } } },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, secondUpdate.StatusCode);

        var response = await client.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.Equal("Ada Lovelace", body!.DisplayName);
        Assert.Equal("Math tutor", body.Headline);
        Assert.Contains(mathSubjectId, body.SubjectIds);
        Assert.Contains(body.SpokenLanguages, l => l.LanguageCode == "en");
    }

    // Stage 1 Certification-step fields (docs/product/preply-reverse-engineering/evidence/
    // Preply_Certifications) — a real HTTP round trip through UpdateDraft/GetCurrentApplication,
    // not just a domain-level unit test, proving the full command → validator → handler →
    // persistence → response path accepts and returns Subject/Description/StartYear/EndYear, and
    // that a null EndYear ("Present") survives the trip rather than being coerced to 0 or dropped.
    [Fact]
    public async Task UpdateDraft_WithCertificateEntries_PersistsSubjectDescriptionAndYearRange()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        var subjectId = Guid.NewGuid();

        var update = await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                certificateEntries = new[]
                {
                    new
                    {
                        name = "TEFL Certificate",
                        issuingOrganization = "International TEFL Academy",
                        description = "120-hour course",
                        subjectId,
                        startYear = 2020,
                        endYear = (int?)null,
                        fileReference = (string?)null,
                    },
                },
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var response = await client.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);

        var entry = Assert.Single(body!.CertificateEntries);
        Assert.Equal("TEFL Certificate", entry.Name);
        Assert.Equal("International TEFL Academy", entry.IssuingOrganization);
        Assert.Equal("120-hour course", entry.Description);
        Assert.Equal(subjectId, entry.SubjectId);
        Assert.Equal(2020, entry.StartYear);
        Assert.Null(entry.EndYear);
    }

    [Fact]
    public async Task UpdateDraft_WithMoreThanTenCertificateEntries_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        var elevenEntries = Enumerable.Range(0, 11)
            .Select(i => new { name = $"Cert {i}", issuingOrganization = "Org", description = (string?)null, subjectId = (Guid?)null, startYear = (int?)null, endYear = (int?)null, fileReference = (string?)null })
            .ToArray();

        var response = await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new { certificateEntries = elevenEntries },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDraft_WithCertificateStartYearAfterEndYear_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        var response = await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                certificateEntries = new[]
                {
                    new { name = "Cert", issuingOrganization = "Org", description = (string?)null, subjectId = (Guid?)null, startYear = 2024, endYear = (int?)2020, fileReference = (string?)null },
                },
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDraft_WithEmptyGuidCertificateSubjectId_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        var response = await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                certificateEntries = new[]
                {
                    new { name = "Cert", issuingOrganization = "Org", description = (string?)null, subjectId = Guid.Empty, startYear = (int?)null, endYear = (int?)null, fileReference = (string?)null },
                },
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDraft_WithoutExistingApplication_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new { displayName = "No Draft Yet" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WhenIncomplete_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        var response = await client.PostAsync("/api/tutors/applications/me/submit", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WhenComplete_TransitionsToSubmitted()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                displayName = "Complete Tutor",
                headline = "Patient, experienced tutor",
                profilePhotoReference = "https://example.com/photo.jpg",
                introVideoReference = "https://example.com/intro.mp4",
                subjectIds = new[] { Guid.NewGuid() },
                spokenLanguages = new[] { new { languageCode = "en", proficiency = "Native" } },
                hourlyRate = new { amountMinorUnits = 2500, currency = "EUR" }
            },
            TestContext.Current.CancellationToken);

        var response = await client.PostAsync("/api/tutors/applications/me/submit", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SubmitApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(TutorApplicationStatus.Submitted, body!.Status);
        Assert.NotNull(body.SubmittedAtUtc);
    }

    [Fact]
    public async Task Submit_WhenAlreadySubmitted_ReturnsBadRequest_ForInvalidStateTransition()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                displayName = "Complete Tutor",
                headline = "Patient, experienced tutor",
                profilePhotoReference = "https://example.com/photo.jpg",
                introVideoReference = "https://example.com/intro.mp4",
                subjectIds = new[] { Guid.NewGuid() },
                spokenLanguages = new[] { new { languageCode = "en", proficiency = "Native" } },
                hourlyRate = new { amountMinorUnits = 2500, currency = "EUR" }
            },
            TestContext.Current.CancellationToken);
        await client.PostAsync("/api/tutors/applications/me/submit", null, TestContext.Current.CancellationToken);

        var response = await client.PostAsync("/api/tutors/applications/me/submit", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDraft_AfterSubmission_ReturnsBadRequest_ForInvalidStateTransition()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                displayName = "Complete Tutor",
                headline = "Patient, experienced tutor",
                profilePhotoReference = "https://example.com/photo.jpg",
                introVideoReference = "https://example.com/intro.mp4",
                subjectIds = new[] { Guid.NewGuid() },
                spokenLanguages = new[] { new { languageCode = "en", proficiency = "Native" } },
                hourlyRate = new { amountMinorUnits = 2500, currency = "EUR" }
            },
            TestContext.Current.CancellationToken);
        await client.PostAsync("/api/tutors/applications/me/submit", null, TestContext.Current.CancellationToken);

        var response = await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new { displayName = "Trying to edit after submit" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentStatus_ReturnsLightweightSummary()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new { displayName = "Status Check Tutor" },
            TestContext.Current.CancellationToken);

        var response = await client.GetAsync("/api/tutors/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentStatusResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(TutorApplicationStatus.Draft, body!.Status);
        Assert.Equal("Status Check Tutor", body.DisplayName);
    }

    [Fact]
    public async Task DualRoleUser_WithStudentAndTutorRoles_CanStillManageTutorApplication()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student", "Tutor");

        var response = await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Drives an application through Create -&gt; Patch -&gt; Submit via the real HTTP endpoints,
    /// then BeginReview -&gt; Approve -&gt; Publish via direct domain mutation — there is still no
    /// HTTP path for the admin-gated steps (`OPEN_QUESTIONS.md` A35), so this mirrors the exact
    /// pattern already used in <c>TutorPublishedPipelineTests</c> (<c>TutorFlow.Host.Tests</c>)
    /// rather than inventing a different one.
    /// </summary>
    private async Task<string> CreatePublishedTutorAsync(Guid userId, string displayName)
    {
        var client = CreateAuthenticatedClient(userId, "Tutor");
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
        var application = await db.TutorApplications.FirstAsync(a => a.UserId == userId, TestContext.Current.CancellationToken);
        application.BeginReview(TimeProvider.System);
        application.Approve(TimeProvider.System);
        application.Publish(TimeProvider.System);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return application.PublicSlug!;
    }

    [Fact]
    public async Task GetCurrentApplication_AfterSubmit_ExposesThePublicSlug_SoAPublishedTutorCanFindTheirOwnProfileLink()
    {
        var userId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(userId, "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                displayName = "Slug Visible Tutor",
                headline = "Patient, experienced tutor",
                profilePhotoReference = "https://example.com/photo.jpg",
                introVideoReference = "https://example.com/intro.mp4",
                subjectIds = new[] { Guid.NewGuid() },
                spokenLanguages = new[] { new { languageCode = "en", proficiency = "Native" } },
                hourlyRate = new { amountMinorUnits = 2500, currency = "EUR" }
            },
            TestContext.Current.CancellationToken);
        var submitResponse = await client.PostAsync("/api/tutors/applications/me/submit", null, TestContext.Current.CancellationToken);
        var submitted = await submitResponse.Content.ReadFromJsonAsync<SubmitApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);

        var response = await client.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body!.PublicSlug);
        Assert.Equal(submitted!.PublicSlug, body.PublicSlug);
    }

    [Fact]
    public async Task ReopenForRevision_FromRejected_AllowsEditingAndResubmitting()
    {
        var userId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(userId, "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                displayName = "Needs Revision Tutor",
                headline = "Patient, experienced tutor",
                profilePhotoReference = "https://example.com/photo.jpg",
                introVideoReference = "https://example.com/intro.mp4",
                subjectIds = new[] { Guid.NewGuid() },
                spokenLanguages = new[] { new { languageCode = "en", proficiency = "Native" } },
                hourlyRate = new { amountMinorUnits = 2500, currency = "EUR" }
            },
            TestContext.Current.CancellationToken);
        await client.PostAsync("/api/tutors/applications/me/submit", null, TestContext.Current.CancellationToken);

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            var application = await db.TutorApplications.FirstAsync(a => a.UserId == userId, TestContext.Current.CancellationToken);
            application.BeginReview(TimeProvider.System);
            application.Reject("Headline needs work", TimeProvider.System);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var reopenResponse = await client.PostAsync("/api/tutors/applications/me/reopen", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, reopenResponse.StatusCode);
        var reopened = await reopenResponse.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(TutorApplicationStatus.Draft, reopened!.Status);
        Assert.Null(reopened.RejectionReason);

        var editResponse = await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new { headline = "Revised headline" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);

        var resubmitResponse = await client.PostAsync("/api/tutors/applications/me/submit", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, resubmitResponse.StatusCode);
    }

    [Fact]
    public async Task ReopenForRevision_FromDraft_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);

        var response = await client.PostAsync("/api/tutors/applications/me/reopen", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReopenForRevision_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/tutors/applications/me/reopen", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicProfile_ForPublishedTutor_ReturnsOkAnonymously()
    {
        var slug = await CreatePublishedTutorAsync(Guid.NewGuid(), "Published Public Tutor");

        var response = await _client.GetAsync($"/api/tutors/{slug}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetPublicProfileResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("Published Public Tutor", body!.DisplayName);
        Assert.Equal(slug, body.Slug);
        Assert.True(body.IsAcceptingNewStudents);
    }

    [Fact]
    public async Task GetPublicProfile_ForUnknownSlug_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/tutors/no-such-tutor-ever", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicProfile_ForDraftApplication_ReturnsNotFound_NeverLeaksInternalStatus()
    {
        var userId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(userId, "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new { displayName = "Still A Draft" },
            TestContext.Current.CancellationToken);

        // A Draft has never been Submitted, so it has no PublicSlug at all yet — confirms there
        // is no way to address an unpublished application publicly, not even by guessing a slug
        // derived from its (settable, pre-submission) display name.
        var guessedSlug = "still-a-draft";
        var response = await _client.GetAsync($"/api/tutors/{guessedSlug}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicProfile_ForApprovedButNotYetPublished_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(userId, "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                displayName = "Approved Not Published",
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
        var application = await db.TutorApplications.FirstAsync(a => a.UserId == userId, TestContext.Current.CancellationToken);
        application.BeginReview(TimeProvider.System);
        application.Approve(TimeProvider.System);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var slug = application.PublicSlug!;

        // Already has a real PublicSlug (assigned at Submit) but is only Approved, not yet
        // Published — must still 404, the same as any other non-Published status.
        var response = await _client.GetAsync($"/api/tutors/{slug}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicProfile_ForPublishedButNotAcceptingNewStudents_StillReturnsOk_NeverA404()
    {
        var userId = Guid.NewGuid();
        var slug = await CreatePublishedTutorAsync(userId, "Not Accepting Tutor");

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            var application = await db.TutorApplications.FirstAsync(a => a.UserId == userId, TestContext.Current.CancellationToken);
            application.SetAcceptingNewStudents(false, TimeProvider.System);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await _client.GetAsync($"/api/tutors/{slug}", TestContext.Current.CancellationToken);

        // This is the one case ADR-0022/the Preply benchmark explicitly requires to stay public
        // — "not accepting new students" is a booking-CTA concern, never a 404.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetPublicProfileResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("Not Accepting Tutor", body!.DisplayName);
        Assert.False(body.IsAcceptingNewStudents);
    }

    [Fact]
    public async Task GetPublicProfile_DoesNotExposeInternalFields()
    {
        var slug = await CreatePublishedTutorAsync(Guid.NewGuid(), "No Leaky Fields Tutor");

        var response = await _client.GetAsync($"/api/tutors/{slug}", TestContext.Current.CancellationToken);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // RejectionReason/SuspensionReason/raw Status and the onboarding-only
        // AvailabilitySettingsConfigured/BookingSettingsConfigured flags are internal-only
        // (GetPublicProfile.Response deliberately omits them) — confirm none leak onto the wire.
        Assert.DoesNotContain("rejectionReason", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("suspensionReason", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("availabilitySettingsConfigured", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bookingSettingsConfigured", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"status\"", json, StringComparison.OrdinalIgnoreCase);
    }

    // The actual security gap identified and fixed in this change: GetPublicProfile previously
    // returned the raw domain CertificateEntry (including FileReference, a server storage
    // path/reference) directly to an anonymous caller. Nothing in today's upload surface sets
    // FileReference yet (Stage 2), but the field already exists on the domain record and is
    // already settable via UpdateDraft — so this must be proven now, not deferred until Stage 2
    // ships, matching the same real-HTTP-round-trip rigor as every other test in this file.
    [Fact]
    public async Task GetPublicProfile_DoesNotExposeCertificateFileReference()
    {
        var userId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(userId, "Tutor");
        await client.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await client.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                displayName = "Certificate Exposure Check",
                headline = "Patient, experienced tutor",
                profilePhotoReference = "https://example.com/photo.jpg",
                introVideoReference = "https://example.com/intro.mp4",
                subjectIds = new[] { Guid.NewGuid() },
                spokenLanguages = new[] { new { languageCode = "en", proficiency = "Native" } },
                hourlyRate = new { amountMinorUnits = 2500, currency = "EUR" },
                certificateEntries = new[]
                {
                    new
                    {
                        name = "TEFL Certificate",
                        issuingOrganization = "International TEFL Academy",
                        description = "120-hour course",
                        subjectId = (Guid?)null,
                        startYear = 2020,
                        endYear = (int?)null,
                        fileReference = "/uploads/tutor-certificates/should-never-be-public.pdf",
                    },
                },
            },
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

        var response = await _client.GetAsync($"/api/tutors/{slug}", TestContext.Current.CancellationToken);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("fileReference", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("should-never-be-public", json, StringComparison.OrdinalIgnoreCase);
        // Confirms this isn't just an empty/absent certificateEntries array hiding the gap —
        // the legitimately-public fields for the same entry really are present on the wire.
        Assert.Contains("TEFL Certificate", json);
        Assert.Contains("International TEFL Academy", json);
    }
}
