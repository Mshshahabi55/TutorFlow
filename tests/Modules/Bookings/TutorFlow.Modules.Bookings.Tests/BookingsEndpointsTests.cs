using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Modules.Lessons.Domain;
using TutorFlow.Modules.Lessons.Persistence;
using ApplyRefundFeature = TutorFlow.Modules.Bookings.Features.ApplyRefund.ApplyRefund;
using CancelBookingForPaymentFailureFeature = TutorFlow.Modules.Bookings.Features.CancelBookingForPaymentFailure.CancelBookingForPaymentFailure;
using CheckoutBookingFeature = TutorFlow.Modules.Bookings.Features.CheckoutBooking.CheckoutBooking;
using ConfirmBookingFeature = TutorFlow.Modules.Bookings.Features.ConfirmBooking.ConfirmBooking;
using CreateTrialHoldResponse = TutorFlow.Modules.Bookings.Features.CreateTrialHold.CreateTrialHold.Response;
using GetBookingResponse = TutorFlow.Modules.Bookings.Features.GetBooking.GetBooking.Response;
using GetMyBookingsAsStudentResponse = TutorFlow.Modules.Bookings.Features.GetMyBookingsAsStudent.GetMyBookingsAsStudent.Response;
using GetMyBookingsAsTutorResponse = TutorFlow.Modules.Bookings.Features.GetMyBookingsAsTutor.GetMyBookingsAsTutor.Response;
using RescheduleBookingResponse = TutorFlow.Modules.Bookings.Features.RescheduleBooking.RescheduleBooking.Response;
using TrialEligibilityResponse = TutorFlow.Modules.Bookings.Features.GetTrialEligibility.GetTrialEligibility.Response;

namespace TutorFlow.Modules.Bookings.Tests;

[Collection(BookingsHostCollection.Name)]
public class BookingsEndpointsTests(BookingsApiFixture fixture) : IClassFixture<BookingsApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BookingsApiFixture.CreateAccessToken(userId, roles));
        return client;
    }

    private static DateTimeOffset NearFutureSlotStart() => new DateTimeOffset(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2).ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);

    private async Task<(Guid TutorId, Guid StudentId, HttpClient StudentClient, HttpClient TutorClient)> ArrangeBookableTutorAsync()
    {
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        return (tutorId, studentId, CreateAuthenticatedClient(studentId, "Student"), CreateAuthenticatedClient(tutorId, "Tutor"));
    }

    [Fact]
    public async Task CreateTrialHold_WithValidSlot_ReturnsCreated()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();

        var response = await studentClient.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = NearFutureSlotStart() },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateTrialHoldResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(BookingStatus.Held, body!.Status);
        Assert.NotNull(body.HoldExpiresAtUtc);
        Assert.True(body.PriceAmountMinorUnits > 0);
    }

    [Fact]
    public async Task CreateTrialHold_WithUnbookableTutor_ReturnsBadRequest()
    {
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        await TestTutorSetup.MakeTutorUnbookableAsync(fixture, tutorId, TestContext.Current.CancellationToken);
        var studentClient = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await studentClient.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = NearFutureSlotStart() },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTrialHold_WithNoBookabilityRecordAtAll_FailsClosed()
    {
        // No TestTutorSetup call at all — proves the fail-closed default (ADR-0014
        // correction): absence of a TutorBookability record must never be treated as bookable.
        var tutorId = Guid.NewGuid();
        var studentClient = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await studentClient.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = NearFutureSlotStart() },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTrialHold_WithDisallowedDuration_ReturnsBadRequest()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();

        var response = await studentClient.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 37, startUtc = NearFutureSlotStart() },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTrialHold_BookingSelf_ReturnsBadRequest()
    {
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        var client = CreateAuthenticatedClient(tutorId, "Student", "Tutor");

        var response = await client.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = NearFutureSlotStart() },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTrialHold_AfterTrialAlreadyUsedWithSameTutor_ReturnsBadRequest()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        await ForceConfirmedAsync(created.Id);

        var response = await studentClient.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = NearFutureSlotStart().AddDays(3) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTrialHold_WithAnExpiredPriorTrialWithSameTutor_IsAllowed()
    {
        // A missed/cancelled/expired trial never "used up" the allowance — mirrors Preply's own
        // model (BOOKINGS_SPEC.md, GetTrialEligibility): only Held never progressed is expired
        // here, which must never block a fresh attempt with the same tutor.
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        await CreateHoldAsync(studentClient, tutorId);

        var response = await studentClient.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = NearFutureSlotStart().AddDays(3) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateTrialHold_WithThreeActiveHoldsAlready_RejectsAFourth()
    {
        var studentId = Guid.NewGuid();
        var studentClient = CreateAuthenticatedClient(studentId, "Student");

        for (var i = 0; i < 3; i++)
        {
            var tutorId = Guid.NewGuid();
            await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
            await CreateHoldAsync(studentClient, tutorId, NearFutureSlotStart().AddDays(i));
        }

        var fourthTutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, fourthTutorId, ct: TestContext.Current.CancellationToken);

        var response = await studentClient.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = fourthTutorId, durationMinutes = 25, startUtc = NearFutureSlotStart().AddDays(4) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTrialHold_WithoutStudentRole_ReturnsForbidden()
    {
        var (tutorId, _, _, _) = await ArrangeBookableTutorAsync();
        var tutorOnlyClient = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await tutorOnlyClient.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = NearFutureSlotStart() },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTrialHold_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = Guid.NewGuid(), durationMinutes = 25, startUtc = NearFutureSlotStart() },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdjacentBookings_BackToBack_BothSucceed()
    {
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        var clientA = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var clientB = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var start = NearFutureSlotStart();

        var responseA = await clientA.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = start },
            TestContext.Current.CancellationToken);
        var responseB = await clientB.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            // Starts exactly when A ends — [) half-open semantics mean this is NOT a conflict.
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = start.AddMinutes(25) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.Created, responseB.StatusCode);
    }

    [Fact]
    public async Task OverlappingBookings_DifferentStudents_SecondRejected()
    {
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        var clientA = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var clientB = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var start = NearFutureSlotStart();

        var responseA = await clientA.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = start },
            TestContext.Current.CancellationToken);
        var responseB = await clientB.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = start.AddMinutes(10) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, responseB.StatusCode);
    }

    [Fact]
    public async Task GetBooking_Owner_ReturnsOk()
    {
        var (tutorId, _, studentClient, tutorClient) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);

        var studentView = await studentClient.GetAsync($"/api/bookings/{created.Id}", TestContext.Current.CancellationToken);
        var tutorView = await tutorClient.GetAsync($"/api/bookings/{created.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, studentView.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tutorView.StatusCode);
        var body = await studentView.Content.ReadFromJsonAsync<GetBookingResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(created.Id, body!.Id);
    }

    [Fact]
    public async Task GetBooking_NonParty_ReturnsForbidden()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        var strangerClient = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await strangerClient.GetAsync($"/api/bookings/{created.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetBooking_NonExistent_ReturnsNotFound()
    {
        var studentClient = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await studentClient.GetAsync($"/api/bookings/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMyBookings_AsStudentAndAsTutor_ReturnOwnBookings()
    {
        var (tutorId, _, studentClient, tutorClient) = await ArrangeBookableTutorAsync();
        await CreateHoldAsync(studentClient, tutorId);

        var studentList = await studentClient.GetFromJsonAsync<GetMyBookingsAsStudentResponse>(
            "/api/bookings/me/as-student", BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        var tutorList = await tutorClient.GetFromJsonAsync<GetMyBookingsAsTutorResponse>(
            "/api/bookings/me/as-tutor", BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(studentList);
        Assert.Single(studentList!.Bookings);
        Assert.NotNull(tutorList);
        Assert.Single(tutorList!.Bookings);
    }

    [Fact]
    public async Task DualRoleUser_CanBookAsStudentAndAppearInOwnTutorList()
    {
        var dualRoleUserId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        await TestTutorSetup.SeedBookableTutorAsync(fixture, dualRoleUserId, ct: TestContext.Current.CancellationToken);
        var dualClient = CreateAuthenticatedClient(dualRoleUserId, "Student", "Tutor");

        await CreateHoldAsync(dualClient, tutorId);

        var asStudent = await dualClient.GetFromJsonAsync<GetMyBookingsAsStudentResponse>(
            "/api/bookings/me/as-student", BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        var asTutor = await dualClient.GetFromJsonAsync<GetMyBookingsAsTutorResponse>(
            "/api/bookings/me/as-tutor", BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.Single(asStudent!.Bookings);
        Assert.Empty(asTutor!.Bookings);
    }

    [Fact]
    public async Task Checkout_FromHeld_TransitionsToPendingPayment_AndReturnsARealClientSecret()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);

        var response = await studentClient.PostAsync($"/api/bookings/{created.Id}/checkout", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CheckoutBookingFeature.Response>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.PaymentReferenceId);
        Assert.False(string.IsNullOrWhiteSpace(body.ClientSecret));

        var getResponse = await studentClient.GetAsync($"/api/bookings/{created.Id}", TestContext.Current.CancellationToken);
        var getBody = await getResponse.Content.ReadFromJsonAsync<GetBookingResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatus.PendingPayment, getBody!.Status);
    }

    [Fact]
    public async Task Checkout_NonOwner_ReturnsForbidden()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        var strangerClient = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await strangerClient.PostAsync($"/api/bookings/{created.Id}/checkout", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_FromAlreadyPendingPayment_CallsPaymentsAgain_ReturningTheSamePaymentReference()
    {
        // ADR-0019: retries are never short-circuited — Payments' client_secret is never
        // persisted, so a retry must ask again, and gets the same Payment back via Payments'
        // own BookingId-derived idempotency key.
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        var first = await studentClient.PostAsync($"/api/bookings/{created.Id}/checkout", null, TestContext.Current.CancellationToken);
        var firstBody = await first.Content.ReadFromJsonAsync<CheckoutBookingFeature.Response>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        var second = await studentClient.PostAsync($"/api/bookings/{created.Id}/checkout", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondBody = await second.Content.ReadFromJsonAsync<CheckoutBookingFeature.Response>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(firstBody!.PaymentReferenceId, secondBody!.PaymentReferenceId);
    }

    [Fact]
    public async Task Checkout_FromConfirmed_ReturnsConflict()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        await ForceConfirmedAsync(created.Id);

        var response = await studentClient.PostAsync($"/api/bookings/{created.Id}/checkout", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_FromExpiredHold_ReturnsConflict()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            await db.Bookings.Where(b => b.Id == created.Id).ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, BookingStatus.Expired)
                .SetProperty(b => b.HoldExpiresAtUtc, (DateTimeOffset?)null), TestContext.Current.CancellationToken);
        }

        var response = await studentClient.PostAsync($"/api/bookings/{created.Id}/checkout", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_FromStillHeldRowPastItsOwnTtl_ReturnsConflict()
    {
        // Regression test: the lazy sweep only runs when a *new* hold is requested for the same
        // tutor, so a stale Held row can sit in the table, unswept, past HoldExpiresAtUtc, with
        // Status still literally "Held". Checkout must still reject it — a row the sweep hasn't
        // gotten to yet is not the same as a genuinely still-open hold.
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            await db.Bookings.Where(b => b.Id == created.Id).ExecuteUpdateAsync(s => s
                .SetProperty(b => b.HoldExpiresAtUtc, DateTimeOffset.UtcNow.AddMinutes(-1)), TestContext.Current.CancellationToken);
        }

        var response = await studentClient.PostAsync($"/api/bookings/{created.Id}/checkout", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            var current = await db.Bookings.AsNoTracking().FirstAsync(b => b.Id == created.Id, TestContext.Current.CancellationToken);
            Assert.Equal(BookingStatus.Held, current.Status);
        }
    }

    [Fact]
    public async Task Cancel_FromHeld_ReturnsOkAndRecordsActor()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);

        var response = await studentClient.PostAsJsonAsync($"/api/bookings/{created.Id}/cancel", new { reason = "changed my mind" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await studentClient.GetFromJsonAsync<GetBookingResponse>($"/api/bookings/{created.Id}", BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatus.Cancelled, getResponse!.Status);
        Assert.NotNull(getResponse.CancelledByUserId);
        Assert.NotNull(getResponse.CancelledAtUtc);
    }

    [Fact]
    public async Task Cancel_ByTutor_IsAllowed()
    {
        var (tutorId, _, studentClient, tutorClient) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);

        var response = await tutorClient.PostAsJsonAsync($"/api/bookings/{created.Id}/cancel", new { reason = (string?)null }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_Twice_SecondReturnsConflictNotIdempotentSuccess()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        await studentClient.PostAsJsonAsync($"/api/bookings/{created.Id}/cancel", new { reason = (string?)null }, TestContext.Current.CancellationToken);

        var second = await studentClient.PostAsJsonAsync($"/api/bookings/{created.Id}/cancel", new { reason = (string?)null }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Cancel_NonParty_ReturnsForbidden()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        var strangerClient = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await strangerClient.PostAsJsonAsync($"/api/bookings/{created.Id}/cancel", new { reason = (string?)null }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reschedule_FromConfirmed_CreatesLinkedReplacement()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        await ForceConfirmedAsync(created.Id);

        var response = await studentClient.PostAsJsonAsync(
            $"/api/bookings/{created.Id}/reschedule",
            new { newStartUtc = NearFutureSlotStart().AddDays(1) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RescheduleBookingResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(BookingStatus.Held, body!.Status);
        Assert.Equal(created.Id, body.RescheduledFromBookingId);

        var original = await studentClient.GetFromJsonAsync<GetBookingResponse>($"/api/bookings/{created.Id}", BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatus.Rescheduled, original!.Status);
        Assert.Equal(body.Id, original.RescheduledToBookingId);
    }

    [Fact]
    public async Task Reschedule_AlreadyRescheduledOnce_RejectsASecondReschedule()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        await ForceConfirmedAsync(created.Id);

        var first = await studentClient.PostAsJsonAsync(
            $"/api/bookings/{created.Id}/reschedule",
            new { newStartUtc = NearFutureSlotStart().AddDays(1) },
            TestContext.Current.CancellationToken);
        var firstBody = await first.Content.ReadFromJsonAsync<RescheduleBookingResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        await ForceConfirmedAsync(firstBody!.Id);

        var second = await studentClient.PostAsJsonAsync(
            $"/api/bookings/{firstBody.Id}/reschedule",
            new { newStartUtc = NearFutureSlotStart().AddDays(2) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Reschedule_WithinTwelveHoursOfStart_ReturnsConflict()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        await ForceConfirmedAsync(created.Id);
        // Direct DB manipulation to force a near-term StartUtc (matching this suite's existing
        // precedent for time-based edge cases, e.g. forcing a past HoldExpiresAtUtc) — avoids a
        // flaky AddHours(n)-from-now anchor that could land outside the seeded availability
        // window depending on wall-clock time at test-run moment.
        await ForceStartUtcAsync(created.Id, DateTimeOffset.UtcNow.AddHours(6));

        var response = await studentClient.PostAsJsonAsync(
            $"/api/bookings/{created.Id}/reschedule",
            new { newStartUtc = NearFutureSlotStart().AddDays(1) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Reschedule_FromHeld_ReturnsConflict()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);

        var response = await studentClient.PostAsJsonAsync(
            $"/api/bookings/{created.Id}/reschedule",
            new { newStartUtc = NearFutureSlotStart().AddDays(1) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Reschedule_NonOwner_ReturnsForbidden()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        await ForceConfirmedAsync(created.Id);
        var strangerClient = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await strangerClient.PostAsJsonAsync(
            $"/api/bookings/{created.Id}/reschedule",
            new { newStartUtc = NearFutureSlotStart().AddDays(1) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmBookingHandler_DuplicateWebhook_IsIdempotent()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        await ForcePendingPaymentAsync(created.Id, "pi_confirm_test");

        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ConfirmBookingFeature.Handler>();

        var first = await handler.HandleAsync(created.Id, "pi_confirm_test", TestContext.Current.CancellationToken);
        var second = await handler.HandleAsync(created.Id, "pi_confirm_test", TestContext.Current.CancellationToken);

        Assert.Equal(ConfirmBookingFeature.Outcome.ConfirmedNow, first);
        Assert.Equal(ConfirmBookingFeature.Outcome.AlreadyConfirmed, second);
    }

    [Fact]
    public async Task ConfirmBookingHandler_RedeliveredAfterCrashBetweenCommitAndPublish_RecoversBookingConfirmed()
    {
        // A44 replay-safety fix. Simulates the exact narrow window identified by decompiling
        // Wolverine 6.45.0's EfCoreEnvelopeTransaction/MessageContext.PersistOrSendAsync under
        // Lightweight transaction mode: the business write (ExecuteUpdateAsync, non-
        // transactional, commits immediately) can succeed while a process crash right
        // afterward prevents BookingConfirmed from ever being durably published — since
        // HandleAsync never returned, the incoming PaymentSucceeded envelope was never marked
        // handled, so Wolverine redelivers it. This test reproduces that persisted end-state
        // directly (booking already Confirmed, BookingConfirmed never sent, no Lesson exists)
        // rather than literally crashing a process, then proves the SAME handler call recovers
        // by publishing BookingConfirmed and Lessons converging to the correct state.
        var (tutorId, studentId, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        await ForcePendingPaymentAsync(created.Id, "pi_crash_recovery");

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            await db.Bookings.Where(b => b.Id == created.Id).ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, BookingStatus.Confirmed)
                .SetProperty(b => b.PaymentReferenceId, "pi_crash_recovery")
                .SetProperty(b => b.HoldExpiresAtUtc, (DateTimeOffset?)null), TestContext.Current.CancellationToken);
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var lessonsDbBefore = scope.ServiceProvider.GetRequiredService<LessonsDbContext>();
            Assert.False(await lessonsDbBefore.Lessons.AnyAsync(l => l.BookingId == created.Id, TestContext.Current.CancellationToken));
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ConfirmBookingFeature.Handler>();
            var outcome = await handler.HandleAsync(created.Id, "pi_crash_recovery", TestContext.Current.CancellationToken);
            Assert.Equal(ConfirmBookingFeature.Outcome.AlreadyConfirmed, outcome);
        }

        var lesson = await PollForLessonAsync(created.Id);
        Assert.NotNull(lesson);
        Assert.Equal(studentId, lesson!.StudentUserId);
        Assert.Equal(tutorId, lesson.TutorUserId);
    }

    [Fact]
    public async Task ConfirmBookingHandler_ConflictingPaymentReferenceOnAlreadyConfirmedBooking_DoesNotPublish()
    {
        // A44 replay-safety fix: the recovery path above must only fire for a genuine
        // redelivery of the SAME payment event — never for an unrelated/conflicting one that
        // happens to target an already-Confirmed booking.
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        await ForcePendingPaymentAsync(created.Id, "pi_original");

        using (var scope = fixture.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ConfirmBookingFeature.Handler>();
            var outcome = await handler.HandleAsync(created.Id, "pi_original", TestContext.Current.CancellationToken);
            Assert.Equal(ConfirmBookingFeature.Outcome.ConfirmedNow, outcome);
        }

        await PollForLessonAsync(created.Id);

        using var scope2 = fixture.Services.CreateScope();
        var handler2 = scope2.ServiceProvider.GetRequiredService<ConfirmBookingFeature.Handler>();
        var conflicting = await handler2.HandleAsync(created.Id, "pi_different", TestContext.Current.CancellationToken);
        Assert.Equal(ConfirmBookingFeature.Outcome.ConfirmedByDifferentPayment, conflicting);

        await Task.Delay(500, TestContext.Current.CancellationToken);

        var lessonsDb = scope2.ServiceProvider.GetRequiredService<LessonsDbContext>();
        var lessonCount = await lessonsDb.Lessons.CountAsync(l => l.BookingId == created.Id, TestContext.Current.CancellationToken);
        Assert.Equal(1, lessonCount);
    }

    [Fact]
    public async Task CancelBookingForPaymentFailureHandler_DuplicateWebhook_IsIdempotent()
    {
        // A44 topology fix: PaymentFailedConsumer now runs under
        // TransactionMiddlewareMode.Lightweight (Program.cs's AutoApplyTransactions no longer
        // wraps it in an ambient transaction shared with the Main store's envelope bookkeeping)
        // — this is the duplicate-delivery coverage that decision depended on. No prior test
        // exercised this handler at all.
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        await ForcePendingPaymentAsync(created.Id, "pi_fail_test");

        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CancelBookingForPaymentFailureFeature.Handler>();

        var first = await handler.HandleAsync(created.Id, "card_declined", TestContext.Current.CancellationToken);
        var second = await handler.HandleAsync(created.Id, "card_declined", TestContext.Current.CancellationToken);

        Assert.Equal(CancelBookingForPaymentFailureFeature.Outcome.Cancelled, first);
        Assert.Equal(CancelBookingForPaymentFailureFeature.Outcome.AlreadyTerminal, second);

        using var verifyScope = fixture.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var booking = await db.Bookings.AsNoTracking().FirstAsync(b => b.Id == created.Id, TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public async Task ConfirmBookingHandler_OnSuccess_PublishesBookingConfirmedExactlyOnce_CreatingExactlyOneLesson()
    {
        // ADR-0016 §1: proves the publish side through the real Wolverine pipeline, the same
        // way the Payments pipeline tests prove Bookings' consumer side — the only reliable
        // observable for "was BookingConfirmed published" is Lessons' own resulting row, since
        // there is no mocking library in this codebase and Wolverine dispatch is asynchronous.
        var (tutorId, studentId, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        await ForcePendingPaymentAsync(created.Id, "pi_confirm_test");

        using (var scope = fixture.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ConfirmBookingFeature.Handler>();
            var outcome = await handler.HandleAsync(created.Id, "pi_confirm_test", TestContext.Current.CancellationToken);
            Assert.Equal(ConfirmBookingFeature.Outcome.ConfirmedNow, outcome);
        }

        var lesson = await PollForLessonAsync(created.Id);
        Assert.NotNull(lesson);
        Assert.Equal(studentId, lesson!.StudentUserId);
        Assert.Equal(tutorId, lesson.TutorUserId);
        Assert.Equal(created.StartUtc, lesson.StartUtc);
        Assert.Equal(created.EndUtc, lesson.EndUtc);
        Assert.Equal(LessonStatus.Scheduled, lesson.Status);

        // A duplicate webhook delivery for the now-Confirmed booking must not re-publish —
        // give the (non-existent, by design) second event a moment it would need to arrive in,
        // then assert the Lesson count is still exactly one.
        using (var scope = fixture.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ConfirmBookingFeature.Handler>();
            var second = await handler.HandleAsync(created.Id, "pi_confirm_test", TestContext.Current.CancellationToken);
            Assert.Equal(ConfirmBookingFeature.Outcome.AlreadyConfirmed, second);
        }

        await Task.Delay(500, TestContext.Current.CancellationToken);

        using var finalScope = fixture.Services.CreateScope();
        var lessonsDb = finalScope.ServiceProvider.GetRequiredService<LessonsDbContext>();
        var lessonCount = await lessonsDb.Lessons.CountAsync(l => l.BookingId == created.Id, TestContext.Current.CancellationToken);
        Assert.Equal(1, lessonCount);
    }

    [Fact]
    public async Task ConfirmBookingHandler_OnFailedConfirmation_PublishesNoEvent_NoLessonIsEverCreated()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        // Still Held, never transitioned to PendingPayment — confirmation must fail (Conflict).
        var created = await CreateHoldAsync(studentClient, tutorId);

        using (var scope = fixture.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ConfirmBookingFeature.Handler>();
            var outcome = await handler.HandleAsync(created.Id, "pi_never_confirmed", TestContext.Current.CancellationToken);
            Assert.Equal(ConfirmBookingFeature.Outcome.Conflict, outcome);
        }

        // No deadline-poll needed for a negative assertion — give any (incorrect) async publish
        // a moment it would need, then assert nothing was ever created.
        await Task.Delay(500, TestContext.Current.CancellationToken);

        using var scope2 = fixture.Services.CreateScope();
        var lessonsDb = scope2.ServiceProvider.GetRequiredService<LessonsDbContext>();
        var lessonExists = await lessonsDb.Lessons.AnyAsync(l => l.BookingId == created.Id, TestContext.Current.CancellationToken);
        Assert.False(lessonExists);
    }

    private async Task<Lesson?> PollForLessonAsync(Guid bookingId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            using var scope = fixture.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LessonsDbContext>();
            var lesson = await db.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.BookingId == bookingId, TestContext.Current.CancellationToken);
            if (lesson is not null)
            {
                return lesson;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        using var finalScope = fixture.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<LessonsDbContext>();
        return await finalDb.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.BookingId == bookingId, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ApplyRefundHandler_OnlyReachableFromCancelled_NeverFromConfirmed()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var created = await CreateHoldAsync(studentClient, tutorId);
        await ForceConfirmedAsync(created.Id);

        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ApplyRefundFeature.Handler>();

        // Still Confirmed, never Cancelled — per BOOKINGS_SPEC.md §2 there is no direct
        // Confirmed -> Refunded edge.
        var attemptFromConfirmed = await handler.HandleAsync(created.Id, TestContext.Current.CancellationToken);
        Assert.Equal(ApplyRefundFeature.Outcome.Conflict, attemptFromConfirmed);

        await studentClient.PostAsJsonAsync($"/api/bookings/{created.Id}/cancel", new { reason = (string?)null }, TestContext.Current.CancellationToken);

        var attemptFromCancelled = await handler.HandleAsync(created.Id, TestContext.Current.CancellationToken);
        var duplicateAttempt = await handler.HandleAsync(created.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ApplyRefundFeature.Outcome.RefundedNow, attemptFromCancelled);
        Assert.Equal(ApplyRefundFeature.Outcome.AlreadyRefunded, duplicateAttempt);
    }

    [Fact]
    public async Task Booking_StartEndUtc_RoundTripsAcrossDstBoundary()
    {
        // DST *computation* correctness is Availability's AvailabilityCalculator's
        // responsibility (already hardened) and is not re-derived here — this only proves
        // Bookings' own UTC storage round-trips a DST-boundary instant exactly, with no
        // timezone-conversion drift introduced by EF/Npgsql mapping.
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();

        // 07:00:00 UTC exactly — chosen because it is the literal wall-clock instant of the US
        // spring-forward transition (2am EST -> 3am EDT) on whichever date it falls; DST
        // *computation* is not exercised here (this tutor's rules are plain UTC, no DST), only
        // that Bookings' own UTC storage round-trips the instant with no drift. Placed a fixed
        // number of days out (well inside the seeded 30-day booking window) rather than pinned
        // to a specific calendar date, so this test doesn't depend on which year it runs in.
        var dstBoundaryUtc = new DateTimeOffset(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10).ToDateTime(new TimeOnly(7, 0)), TimeSpan.Zero);
        var response = await studentClient.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = dstBoundaryUtc },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateTrialHoldResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(dstBoundaryUtc, body!.StartUtc);
        Assert.Equal(dstBoundaryUtc.AddMinutes(25), body.EndUtc);
    }

    private async Task<CreateTrialHoldResponse> CreateHoldAsync(HttpClient studentClient, Guid tutorId, DateTimeOffset? startUtc = null)
    {
        var response = await studentClient.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = startUtc ?? NearFutureSlotStart() },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateTrialHoldResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken))!;
    }

    private async Task ForcePendingPaymentAsync(Guid bookingId, string paymentReferenceId)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        await db.Bookings.Where(b => b.Id == bookingId).ExecuteUpdateAsync(s => s
            .SetProperty(b => b.Status, BookingStatus.PendingPayment)
            .SetProperty(b => b.PaymentReferenceId, paymentReferenceId));
    }

    private async Task ForceConfirmedAsync(Guid bookingId)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        await db.Bookings.Where(b => b.Id == bookingId).ExecuteUpdateAsync(s => s
            .SetProperty(b => b.Status, BookingStatus.Confirmed)
            .SetProperty(b => b.HoldExpiresAtUtc, (DateTimeOffset?)null));
    }

    private async Task ForceStartUtcAsync(Guid bookingId, DateTimeOffset startUtc)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        await db.Bookings.Where(b => b.Id == bookingId).ExecuteUpdateAsync(s => s
            .SetProperty(b => b.StartUtc, startUtc)
            .SetProperty(b => b.EndUtc, startUtc.AddMinutes(25)));
    }

    [Fact]
    public async Task GetTrialEligibility_WithNoPriorBooking_ReturnsHasUsedTrialFalse()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();

        var response = await studentClient.GetAsync($"/api/bookings/me/trial-eligibility/{tutorId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TrialEligibilityResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.False(body!.HasUsedTrial);
    }

    [Fact]
    public async Task GetTrialEligibility_AfterTrialConfirmed_ReturnsHasUsedTrialTrue()
    {
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        var createResponse = await studentClient.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = NearFutureSlotStart() },
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateTrialHoldResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        await ForceConfirmedAsync(created!.Id);

        var response = await studentClient.GetAsync($"/api/bookings/me/trial-eligibility/{tutorId}", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<TrialEligibilityResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.True(body!.HasUsedTrial);
    }

    [Fact]
    public async Task GetTrialEligibility_WithOnlyAHeldBooking_StillReturnsHasUsedTrialFalse()
    {
        // A mere Held hold (abandoned checkout, expired, never paid) must not consume the
        // one-trial allowance — only a booking that actually progressed past Held counts
        // (GetTrialEligibility.UsedTrialStatuses, ADR-0022).
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();
        await studentClient.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = NearFutureSlotStart() },
            TestContext.Current.CancellationToken);

        var response = await studentClient.GetAsync($"/api/bookings/me/trial-eligibility/{tutorId}", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<TrialEligibilityResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.False(body!.HasUsedTrial);
    }

    [Fact]
    public async Task GetTrialEligibility_IsReadOnly_DoesNotCreateOrMutateAnyBooking()
    {
        // Explicit guard against the exact failure mode item 8 of the hardening review calls
        // out: this endpoint must never create a booking, hold a slot, or touch payment state —
        // calling it twice in a row must leave the booking table untouched.
        var (tutorId, _, studentClient, _) = await ArrangeBookableTutorAsync();

        await studentClient.GetAsync($"/api/bookings/me/trial-eligibility/{tutorId}", TestContext.Current.CancellationToken);
        await studentClient.GetAsync($"/api/bookings/me/trial-eligibility/{tutorId}", TestContext.Current.CancellationToken);

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var count = await db.Bookings.CountAsync(b => b.TutorUserId == tutorId, TestContext.Current.CancellationToken);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetTrialEligibility_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync($"/api/bookings/me/trial-eligibility/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTrialEligibility_AsTutorRole_ReturnsForbidden()
    {
        var tutorClient = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await tutorClient.GetAsync($"/api/bookings/me/trial-eligibility/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
