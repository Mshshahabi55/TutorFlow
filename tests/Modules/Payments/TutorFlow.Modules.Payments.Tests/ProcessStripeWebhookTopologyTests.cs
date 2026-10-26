using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Modules.Payments.Domain;
using TutorFlow.Modules.Payments.Persistence;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Payments.Tests;

// A44 #105: proves (not merely asserts) that ProcessStripeWebhookConsumer's Eager-mode
// transactional middleware routes its outgoing PaymentSucceeded/PaymentFailed envelope to the
// correct "payments" schema even when Payments' database is a genuinely separate Postgres
// instance from every other module — see ProcessStripeWebhookTopologyFixture's own remarks for
// why PaymentsApiFixture's shared-container topology cannot rule this out on its own.
[Collection(PaymentsHostCollection.Name)]
public sealed class ProcessStripeWebhookTopologyTests(ProcessStripeWebhookTopologyFixture fixture)
    : IClassFixture<ProcessStripeWebhookTopologyFixture>
{
    private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ProcessStripeWebhookTopologyFixture.CreateAccessToken(userId, roles));
        return client;
    }

    private async Task<(Guid BookingId, Guid StudentId)> SeedPendingPaymentBookingAsync(int priceAmountMinorUnits = 2500)
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        using var scope = fixture.Services.CreateScope();

        var availabilityDb = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
        availabilityDb.TutorBookabilityRecords.Add(TutorBookability.Create(tutorId, isBookable: true, TimeProvider.System));
        await availabilityDb.SaveChangesAsync();

        var tutorsDb = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var application = TutorFlow.Modules.Tutors.Domain.TutorApplication.CreateDraft(tutorId, TimeProvider.System);
        application.ApplyDraftUpdate(
            new TutorFlow.Modules.Tutors.Domain.DraftUpdate(DisplayName: "Topology Test Tutor", HourlyRate: new TutorFlow.Modules.Tutors.Domain.Money(6000, "EUR")),
            TimeProvider.System);
        tutorsDb.TutorApplications.Add(application);
        await tutorsDb.SaveChangesAsync();

        var bookingsDb = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var booking = Booking.CreateHeld(
            studentId, tutorId, 25, startUtc, DateTimeOffset.UtcNow.AddMinutes(10),
            new TutorFlow.Modules.Bookings.Domain.Money(priceAmountMinorUnits, "EUR"), TimeProvider.System, isTrial: true).Value;
        bookingsDb.Bookings.Add(booking);
        await bookingsDb.SaveChangesAsync();
        await bookingsDb.Bookings.Where(b => b.Id == booking.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BookingStatus.PendingPayment));

        return (booking.Id, studentId);
    }

    private static string ComputeStripeSignatureHeader(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload)));
        return $"t={timestamp},v1={signature}";
    }

    private static string BuildPaymentIntentEventJson(string eventId, string eventType, string paymentIntentId) => $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "2020-08-27",
          "created": 1700000000,
          "data": { "object": { "id": "{{paymentIntentId}}", "object": "payment_intent", "status": "succeeded" } },
          "livemode": false,
          "pending_webhooks": 0,
          "request": { "id": null, "idempotency_key": null },
          "type": "{{eventType}}"
        }
        """;

    private async Task<Guid> CreatePaymentIntentAsync(Guid studentId, Guid bookingId)
    {
        var client = CreateAuthenticatedClient(studentId, "Student");
        var response = await client.PostAsJsonAsync("/api/payments/intents", new { bookingId }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        using var scope = fixture.Services.CreateScope();
        var paymentsDb = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var payment = await paymentsDb.Payments.FirstAsync(p => p.BookingId == bookingId, TestContext.Current.CancellationToken);
        return payment.Id;
    }

    private async Task<int> CountRowsAsync(string connectionString, string sql)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(TestContext.Current.CancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PaymentSucceeded_WithGenuinelySeparatePaymentsDatabase_RoutesEnvelopeToPaymentsSchema_NotElsewhere()
    {
        var (bookingId, studentId) = await SeedPendingPaymentBookingAsync();
        var paymentId = await CreatePaymentIntentAsync(studentId, bookingId);

        string stripePaymentIntentId;
        using (var scope = fixture.Services.CreateScope())
        {
            var paymentsDb = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            stripePaymentIntentId = (await paymentsDb.Payments.FirstAsync(p => p.Id == paymentId, TestContext.Current.CancellationToken)).StripePaymentIntentId!;
        }

        var eventId = $"evt_{Guid.NewGuid():N}";
        var payload = BuildPaymentIntentEventJson(eventId, "payment_intent.succeeded", stripePaymentIntentId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhooks/stripe")
        {
            Content = new StringContent(payload),
        };
        request.Headers.Add("Stripe-Signature", ComputeStripeSignatureHeader(payload, ProcessStripeWebhookTopologyFixture.WebhookSigningSecret));

        var client = fixture.CreateClient();
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // The decisive assertion: if Eager-mode misrouted the outgoing envelope to any schema
        // other than "payments" on the Payments container, that schema does not exist on the
        // decoy container at all, and the awaited bus.InvokeAsync call inside the endpoint
        // throws — surfacing here as something other than 200 OK, exactly as it did for
        // ExpireBookingHoldCommand against BookingsApiFixture's genuinely separate containers.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = fixture.Services.CreateScope())
        {
            var paymentsDb = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            var payment = await paymentsDb.Payments.AsNoTracking().FirstAsync(p => p.Id == paymentId, TestContext.Current.CancellationToken);
            Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        }

        // End-to-end proof the PaymentSucceeded event was actually, durably delivered across
        // containers: Bookings' own Lightweight ConfirmBooking consumer — running against the
        // decoy container — only flips the booking to Confirmed if it genuinely received the
        // event, not merely if the HTTP call happened to return 200.
        var deadline = DateTime.UtcNow.AddSeconds(20);
        BookingStatus? observed = null;
        while (DateTime.UtcNow < deadline)
        {
            using var scope = fixture.Services.CreateScope();
            var bookingsDb = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            var booking = await bookingsDb.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId, TestContext.Current.CancellationToken);
            observed = booking.Status;
            if (booking.Status == BookingStatus.Confirmed)
            {
                break;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }

        Assert.Equal(BookingStatus.Confirmed, observed);

        // No dead letters on either container — a misroute-that-got-caught-and-retried-forever
        // would otherwise still pass the 200-OK assertion above on a later successful retry
        // while leaving dead-letter evidence of the earlier failures.
        Assert.Equal(0, await CountRowsAsync(fixture.PaymentsConnectionString, "select count(*) from payments.wolverine_dead_letters"));
        Assert.Equal(0, await CountRowsAsync(fixture.DecoyConnectionString, "select count(*) from bookings.wolverine_dead_letters"));
    }

    [Fact]
    public async Task PaymentFailed_WithGenuinelySeparatePaymentsDatabase_RoutesEnvelopeToPaymentsSchema_AndCancelsBooking()
    {
        var (bookingId, studentId) = await SeedPendingPaymentBookingAsync();
        var paymentId = await CreatePaymentIntentAsync(studentId, bookingId);

        string stripePaymentIntentId;
        using (var scope = fixture.Services.CreateScope())
        {
            var paymentsDb = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            stripePaymentIntentId = (await paymentsDb.Payments.FirstAsync(p => p.Id == paymentId, TestContext.Current.CancellationToken)).StripePaymentIntentId!;
        }

        var eventId = $"evt_{Guid.NewGuid():N}";
        var payload = BuildPaymentIntentEventJson(eventId, "payment_intent.payment_failed", stripePaymentIntentId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhooks/stripe")
        {
            Content = new StringContent(payload),
        };
        request.Headers.Add("Stripe-Signature", ComputeStripeSignatureHeader(payload, ProcessStripeWebhookTopologyFixture.WebhookSigningSecret));

        var client = fixture.CreateClient();
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var deadline = DateTime.UtcNow.AddSeconds(20);
        BookingStatus? observed = null;
        while (DateTime.UtcNow < deadline)
        {
            using var scope = fixture.Services.CreateScope();
            var bookingsDb = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            var booking = await bookingsDb.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId, TestContext.Current.CancellationToken);
            observed = booking.Status;
            if (booking.Status == BookingStatus.Cancelled)
            {
                break;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }

        Assert.Equal(BookingStatus.Cancelled, observed);
    }

    [Fact]
    public async Task DuplicateWebhookDelivery_ThroughTheRealEndpointAndEagerConsumer_StaysIdempotent()
    {
        var (bookingId, studentId) = await SeedPendingPaymentBookingAsync();
        var paymentId = await CreatePaymentIntentAsync(studentId, bookingId);

        string stripePaymentIntentId;
        using (var scope = fixture.Services.CreateScope())
        {
            var paymentsDb = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            stripePaymentIntentId = (await paymentsDb.Payments.FirstAsync(p => p.Id == paymentId, TestContext.Current.CancellationToken)).StripePaymentIntentId!;
        }

        var eventId = $"evt_{Guid.NewGuid():N}";
        var payload = BuildPaymentIntentEventJson(eventId, "payment_intent.succeeded", stripePaymentIntentId);
        var client = fixture.CreateClient();

        for (var i = 0; i < 2; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhooks/stripe")
            {
                Content = new StringContent(payload),
            };
            request.Headers.Add("Stripe-Signature", ComputeStripeSignatureHeader(payload, ProcessStripeWebhookTopologyFixture.WebhookSigningSecret));
            var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var finalScope = fixture.Services.CreateScope();
        var finalPaymentsDb = finalScope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        Assert.Equal(1, await finalPaymentsDb.PaymentWebhookEvents.CountAsync(e => e.StripeEventId == eventId, TestContext.Current.CancellationToken));
    }
}
