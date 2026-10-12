using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Modules.Payments.Domain;
using TutorFlow.Modules.Payments.Persistence;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using CreatePaymentIntentResponse = TutorFlow.Modules.Payments.Features.CreatePaymentIntent.CreatePaymentIntent.Response;

namespace TutorFlow.Modules.Payments.Tests;

public class PaymentsEndpointsTests(PaymentsApiFixture fixture) : IClassFixture<PaymentsApiFixture>
{
    private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PaymentsApiFixture.CreateAccessToken(userId, roles));
        return client;
    }

    // Seeds a Booking directly in PendingPayment — bypassing Bookings' own hold/checkout HTTP
    // flow (already covered by Bookings.Tests) to keep these tests focused on Payments' own
    // endpoint/eligibility/webhook behavior.
    private async Task<Guid> SeedPendingPaymentBookingAsync(Guid studentUserId, Guid tutorUserId, int priceAmountMinorUnits = 2500)
    {
        using var scope = fixture.Services.CreateScope();

        var availabilityDb = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
        availabilityDb.TutorBookabilityRecords.Add(TutorBookability.Create(tutorUserId, isBookable: true, TimeProvider.System));
        await availabilityDb.SaveChangesAsync();

        var tutorsDb = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var application = TutorApplication.CreateDraft(tutorUserId, TimeProvider.System);
        application.ApplyDraftUpdate(
            new DraftUpdate(DisplayName: "Payments Test Tutor", HourlyRate: new TutorFlow.Modules.Tutors.Domain.Money(6000, "EUR")),
            TimeProvider.System);
        tutorsDb.TutorApplications.Add(application);
        await tutorsDb.SaveChangesAsync();

        var bookingsDb = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var booking = Booking.CreateHeld(
            studentUserId, tutorUserId, 25, startUtc, DateTimeOffset.UtcNow.AddMinutes(10),
            new TutorFlow.Modules.Bookings.Domain.Money(priceAmountMinorUnits, "EUR"), TimeProvider.System, isTrial: true).Value;
        bookingsDb.Bookings.Add(booking);
        await bookingsDb.SaveChangesAsync();
        await bookingsDb.Bookings.Where(b => b.Id == booking.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BookingStatus.PendingPayment));

        return booking.Id;
    }

    [Fact]
    public async Task CreateIntent_ForEligiblePendingPaymentBooking_CreatesARealPaymentRow()
    {
        var studentId = Guid.NewGuid();
        var bookingId = await SeedPendingPaymentBookingAsync(studentId, Guid.NewGuid());
        var client = CreateAuthenticatedClient(studentId, "Student");

        var response = await client.PostAsJsonAsync("/api/payments/intents", new { bookingId }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreatePaymentIntentResponse>(PaymentsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.PaymentReferenceId);
        Assert.False(string.IsNullOrWhiteSpace(body.ClientSecret));

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var payment = await db.Payments.FirstAsync(p => p.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.Equal(PaymentStatus.Created, payment.Status);
        Assert.Equal(2500, payment.AmountMinorUnits);
        Assert.Equal("EUR", payment.Currency);
    }

    [Fact]
    public async Task CreateIntent_ForHeldNotPendingPaymentBooking_IsRejected()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        using (var scope = fixture.Services.CreateScope())
        {
            var bookingsDb = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            var booking = Booking.CreateHeld(
                studentId, tutorId, 25, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddMinutes(10),
                new TutorFlow.Modules.Bookings.Domain.Money(2500, "EUR"), TimeProvider.System, isTrial: true).Value;
            bookingsDb.Bookings.Add(booking);
            await bookingsDb.SaveChangesAsync(TestContext.Current.CancellationToken);

            var client = CreateAuthenticatedClient(studentId, "Student");
            var response = await client.PostAsJsonAsync("/api/payments/intents", new { bookingId = booking.Id }, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }

    [Fact]
    public async Task CreateIntent_ForAnotherStudentsBooking_IsRejected()
    {
        var bookingId = await SeedPendingPaymentBookingAsync(Guid.NewGuid(), Guid.NewGuid());
        var strangerClient = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await strangerClient.PostAsJsonAsync("/api/payments/intents", new { bookingId }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateIntent_ForUnknownBooking_IsRejected()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await client.PostAsJsonAsync("/api/payments/intents", new { bookingId = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateIntent_Unauthenticated_ReturnsUnauthorized()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/payments/intents", new { bookingId = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateIntent_CalledTwiceForTheSameBooking_ReturnsTheSamePaymentReference()
    {
        var studentId = Guid.NewGuid();
        var bookingId = await SeedPendingPaymentBookingAsync(studentId, Guid.NewGuid());
        var client = CreateAuthenticatedClient(studentId, "Student");

        var first = await client.PostAsJsonAsync("/api/payments/intents", new { bookingId }, TestContext.Current.CancellationToken);
        var second = await client.PostAsJsonAsync("/api/payments/intents", new { bookingId }, TestContext.Current.CancellationToken);

        var firstBody = await first.Content.ReadFromJsonAsync<CreatePaymentIntentResponse>(PaymentsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        var secondBody = await second.Content.ReadFromJsonAsync<CreatePaymentIntentResponse>(PaymentsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(firstBody!.PaymentReferenceId, secondBody!.PaymentReferenceId);

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        Assert.Equal(1, await db.Payments.CountAsync(p => p.BookingId == bookingId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateIntent_ConcurrentRequestsForTheSameBooking_OnlyOnePaymentRowIsEverCreated()
    {
        var studentId = Guid.NewGuid();
        var bookingId = await SeedPendingPaymentBookingAsync(studentId, Guid.NewGuid());
        var clientA = CreateAuthenticatedClient(studentId, "Student");
        var clientB = CreateAuthenticatedClient(studentId, "Student");

        var responses = await Task.WhenAll(
            clientA.PostAsJsonAsync("/api/payments/intents", new { bookingId }, TestContext.Current.CancellationToken),
            clientB.PostAsJsonAsync("/api/payments/intents", new { bookingId }, TestContext.Current.CancellationToken));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        Assert.Equal(1, await db.Payments.CountAsync(p => p.BookingId == bookingId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PaymentSucceeded_ConfirmsExactlyOneBooking_ThroughTheRealWolverinePipeline()
    {
        var studentId = Guid.NewGuid();
        var bookingId = await SeedPendingPaymentBookingAsync(studentId, Guid.NewGuid());
        var client = CreateAuthenticatedClient(studentId, "Student");
        await client.PostAsJsonAsync("/api/payments/intents", new { bookingId }, TestContext.Current.CancellationToken);

        using (var scope = fixture.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<Features.ProcessStripeWebhook.ProcessStripeWebhook.Handler>();
            var paymentsDb = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            var payment = await paymentsDb.Payments.FirstAsync(p => p.BookingId == bookingId, TestContext.Current.CancellationToken);

            var outcome = await handler.ProcessEventAsync(
                $"evt_{Guid.NewGuid():N}", "payment_intent.succeeded", payment.StripePaymentIntentId, null, "{}", TestContext.Current.CancellationToken);

            Assert.Equal(Features.ProcessStripeWebhook.ProcessStripeWebhook.Outcome.Succeeded, outcome);
        }

        var confirmedBooking = await PollBookingStatusAsync(bookingId, BookingStatus.Confirmed);
        Assert.Equal(BookingStatus.Confirmed, confirmedBooking);
    }

    [Fact]
    public async Task PaymentFailed_CancelsTheBooking_ThroughTheRealWolverinePipeline()
    {
        var studentId = Guid.NewGuid();
        var bookingId = await SeedPendingPaymentBookingAsync(studentId, Guid.NewGuid());
        var client = CreateAuthenticatedClient(studentId, "Student");
        await client.PostAsJsonAsync("/api/payments/intents", new { bookingId }, TestContext.Current.CancellationToken);

        using (var scope = fixture.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<Features.ProcessStripeWebhook.ProcessStripeWebhook.Handler>();
            var paymentsDb = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            var payment = await paymentsDb.Payments.FirstAsync(p => p.BookingId == bookingId, TestContext.Current.CancellationToken);

            var outcome = await handler.ProcessEventAsync(
                $"evt_{Guid.NewGuid():N}", "payment_intent.payment_failed", payment.StripePaymentIntentId, "card_declined", "{}", TestContext.Current.CancellationToken);

            Assert.Equal(Features.ProcessStripeWebhook.ProcessStripeWebhook.Outcome.Failed, outcome);
        }

        var cancelledBooking = await PollBookingStatusAsync(bookingId, BookingStatus.Cancelled);
        Assert.Equal(BookingStatus.Cancelled, cancelledBooking);
    }

    [Fact]
    public async Task DuplicateWebhookEvent_IsIgnored_NeverReprocessed()
    {
        var studentId = Guid.NewGuid();
        var bookingId = await SeedPendingPaymentBookingAsync(studentId, Guid.NewGuid());
        var client = CreateAuthenticatedClient(studentId, "Student");
        await client.PostAsJsonAsync("/api/payments/intents", new { bookingId }, TestContext.Current.CancellationToken);

        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<Features.ProcessStripeWebhook.ProcessStripeWebhook.Handler>();
        var paymentsDb = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var payment = await paymentsDb.Payments.FirstAsync(p => p.BookingId == bookingId, TestContext.Current.CancellationToken);
        var eventId = $"evt_{Guid.NewGuid():N}";

        var first = await handler.ProcessEventAsync(eventId, "payment_intent.succeeded", payment.StripePaymentIntentId, null, "{}", TestContext.Current.CancellationToken);
        var second = await handler.ProcessEventAsync(eventId, "payment_intent.succeeded", payment.StripePaymentIntentId, null, "{}", TestContext.Current.CancellationToken);

        Assert.Equal(Features.ProcessStripeWebhook.ProcessStripeWebhook.Outcome.Succeeded, first);
        Assert.Equal(Features.ProcessStripeWebhook.ProcessStripeWebhook.Outcome.DuplicateEvent, second);
        Assert.Equal(1, await paymentsDb.PaymentWebhookEvents.CountAsync(e => e.StripeEventId == eventId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnrecognizedEventType_IsLoggedAndAcknowledged_NotErrored()
    {
        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<Features.ProcessStripeWebhook.ProcessStripeWebhook.Handler>();

        var outcome = await handler.ProcessEventAsync($"evt_{Guid.NewGuid():N}", "account.updated", null, null, "{}", TestContext.Current.CancellationToken);

        Assert.Equal(Features.ProcessStripeWebhook.ProcessStripeWebhook.Outcome.Unrecognized, outcome);
    }

    [Fact]
    public async Task Webhook_WithoutConfiguredSigningSecret_ReturnsServiceUnavailable_NeverBypassesVerification()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsync("/api/payments/webhooks/stripe", new StringContent("{}"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    // The signature-verification path (ADR-0018 §3) is the single most security-critical code
    // path in Payments — every other webhook test in this file calls the handler directly,
    // bypassing the HTTP endpoint and its signature check entirely. These two exercise the real
    // endpoint, with a configured signing secret, computing the Stripe-Signature header exactly
    // per Stripe's own documented algorithm (https://stripe.com/docs/webhooks/signatures) — no
    // real Stripe account is needed to verify this, since verification is pure HMAC math.
    private const string WebhookTestSecret = "whsec_test_signing_secret_for_verification";

    private static string ComputeStripeSignatureHeader(string payload, string secret, long? timestampOverride = null)
    {
        var timestamp = timestampOverride ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload)));
        return $"t={timestamp},v1={signature}";
    }

    private const string MinimalStripeEventJson = """
        {
          "id": "evt_signature_test",
          "object": "event",
          "api_version": "2020-08-27",
          "created": 1700000000,
          "data": { "object": { "id": "acct_test", "object": "account" } },
          "livemode": false,
          "pending_webhooks": 0,
          "request": { "id": null, "idempotency_key": null },
          "type": "account.updated"
        }
        """;

    [Fact]
    public async Task Webhook_WithValidStripeSignature_IsAcceptedAndRecorded()
    {
        using var signedFactory = fixture.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:StripeWebhookSigningSecret"] = WebhookTestSecret,
            })));
        var client = signedFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhooks/stripe")
        {
            Content = new StringContent(MinimalStripeEventJson),
        };
        request.Headers.Add("Stripe-Signature", ComputeStripeSignatureHeader(MinimalStripeEventJson, WebhookTestSecret));

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = signedFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        Assert.True(await db.PaymentWebhookEvents.AnyAsync(e => e.StripeEventId == "evt_signature_test", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Webhook_WithTamperedOrForgedSignature_ReturnsBadRequest_AndNeverRecordsTheEvent()
    {
        using var signedFactory = fixture.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:StripeWebhookSigningSecret"] = WebhookTestSecret,
            })));
        var client = signedFactory.CreateClient();

        // Deliberately a different event id from the valid-signature test above — both tests
        // share the same underlying Testcontainers database via `fixture`, so reusing the same
        // id would make this test's own absence-assertion depend on test execution order.
        var payload = MinimalStripeEventJson.Replace("evt_signature_test", "evt_tampered_signature_test");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhooks/stripe")
        {
            Content = new StringContent(payload),
        };
        // Valid shape, computed with the wrong secret — simulates a forged request from someone
        // who doesn't know the real signing secret, not a genuine Stripe delivery.
        request.Headers.Add("Stripe-Signature", ComputeStripeSignatureHeader(payload, "wrong-secret-entirely"));

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = signedFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        Assert.False(await db.PaymentWebhookEvents.AnyAsync(e => e.StripeEventId == "evt_tampered_signature_test", TestContext.Current.CancellationToken));
    }

    private async Task<BookingStatus> PollBookingStatusAsync(Guid bookingId, BookingStatus expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            using var scope = fixture.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            var booking = await db.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);
            if (booking.Status == expected)
            {
                return booking.Status;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        using var finalScope = fixture.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        return (await finalDb.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId)).Status;
    }
}
