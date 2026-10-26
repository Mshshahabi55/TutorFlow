using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Notifications.Infrastructure;
using TutorFlow.Shared;
using Wolverine.Tracking;
using GetCurrentApplicationResponse = TutorFlow.Modules.Tutors.Features.GetCurrentApplication.GetCurrentApplication.Response;

namespace TutorFlow.Modules.Tutors.Tests;

/// <summary>Audit finding F6 regression coverage — proves <c>TutorCertificateRejected</c> actually
/// reaches Notifications' consumer through the real Wolverine local-queue dispatch, asserted
/// directly via Wolverine's own <c>ITrackedSession</c> (never moved to the error queue), not
/// inferred from whether an email happened to show up. This is the exact path that was silently
/// moving every such envelope to the error queue under Dynamic TypeLoadMode's concurrent
/// first-dispatch codegen race (root cause fixed by switching to Static TypeLoadMode — see
/// Program.cs). <see cref="TestEmailSender"/> is a deterministic in-memory fake (see its own doc
/// comment) — this additionally proves the notification *sender* was invoked with the right
/// recipient/content, not that a real email was delivered through Postmark. Real-provider delivery
/// is out of scope for every automated test in this codebase — PostmarkEmailSender is only ever
/// exercised against a real Postmark account, never in CI.</summary>
[Collection(TutorsHostCollection.Name)]
public class CertificateRejectionNotificationTests(TutorsApiFixture fixture) : IClassFixture<TutorsApiFixture>
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

    /// <summary>A real Identity row, not a bare random Guid — <c>IUserEmailQuery</c> (which
    /// <c>SendCertificateRejectedEmail.Handler</c> calls after the real bus dispatch) resolves
    /// against actual data, same "real DI wiring, not a mocked substitute" precedent as
    /// <c>SendAbandonedCheckoutRecoveryEmailTests</c>.</summary>
    private async Task<(Guid UserId, string Email)> SeedIdentityUserAsync()
    {
        var email = $"tutor-{Guid.NewGuid():N}@example.com";
        using var scope = fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email, CreatedAtUtc = DateTimeOffset.UtcNow };
        var result = await userManager.CreateAsync(user, "Password123!");
        Assert.True(result.Succeeded);
        return (user.Id, email);
    }

    [Fact]
    public async Task RejectCertificate_DispatchesThroughTheRealBus_AndInvokesTheNotificationSender()
    {
        var (tutorUserId, email) = await SeedIdentityUserAsync();
        var tutorClient = CreateAuthenticatedClient(tutorUserId, "Tutor");

        await tutorClient.PostAsync("/api/tutors/applications", null, TestContext.Current.CancellationToken);
        await tutorClient.PatchAsJsonAsync(
            "/api/tutors/applications/me",
            new
            {
                certificateEntries = new[]
                {
                    new { name = "TEFL Certificate", issuingOrganization = "ITA", description = (string?)null, subjectId = (Guid?)null, startYear = (int?)null, endYear = (int?)null },
                },
            },
            TestContext.Current.CancellationToken);

        var current = await tutorClient.GetAsync("/api/tutors/applications/me", TestContext.Current.CancellationToken);
        var app = await current.Content.ReadFromJsonAsync<GetCurrentApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        var certificateId = app!.CertificateEntries.Single().Id;

        using var upload = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(ValidJpegBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        upload.Add(fileContent, "file", "cert.jpg");
        await tutorClient.PostAsync($"/api/tutors/me/certificates/{certificateId}/document", upload, TestContext.Current.CancellationToken);

        var admin = CreateAuthenticatedClient(Guid.NewGuid(), "Admin");

        // Wolverine's own official test-tracking API (Wolverine.Tracking.ITrackedSession, core
        // package — no extra dependency) replaces a hand-rolled polling loop here: it hooks the
        // runtime's own message-lifecycle tracker for the duration of the delegate and waits
        // until every message touched by it (including ones published asynchronously from deep
        // inside the HTTP handler, off the request's own call stack) has fully settled —
        // success, failure, or moved to the error queue — rather than guessing a deadline.
        var session = await fixture.Services.ExecuteAndWaitAsync(async () =>
        {
            var rejectResponse = await admin.PostAsJsonAsync(
                $"/api/tutors/admin/applications/{app.Id}/certificates/{certificateId}/reject",
                new { reason = "Document is illegible." },
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NoContent, rejectResponse.StatusCode);
        }, timeoutInMilliseconds: 20_000);

        // Proves the event was actually processed by the real consumer — not silently moved to
        // Wolverine's local error queue, the exact F6 regression this test guards against. This
        // is a direct assertion on the dispatch outcome itself, not an inference from whether an
        // email happened to arrive.
        Assert.NotEmpty(session.MessageSucceeded.MessagesOf<TutorCertificateRejected>());
        Assert.Empty(session.MovedToErrorQueue.MessagesOf<TutorCertificateRejected>());

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();

        // At-least-once, not exactly-once: a live run against the real local-queue dispatch
        // observed the same envelope delivered twice (two identical sends to the same address)
        // for a single Reject call — Wolverine's local transport is not exactly-once, and
        // SendCertificateRejectedEmail.Handler has no idempotency guard against it, by its own
        // documented, deliberate design choice (a duplicate rejection email is an accepted minor
        // inconvenience, unlike the financial/booking-flow email which does guard against this).
        // This test asserts the guaranteed behavior — at least one correct send reaches the real
        // recipient — not a stronger exactly-once guarantee the architecture never promised.
        Assert.Contains(sender.SentMessages, m => m.ToEmail == email);
        var sent = sender.SentMessages.First(m => m.ToEmail == email);
        Assert.Contains("TEFL Certificate", sent.Subject);
        Assert.Contains("Document is illegible.", sent.HtmlBody);
    }
}
