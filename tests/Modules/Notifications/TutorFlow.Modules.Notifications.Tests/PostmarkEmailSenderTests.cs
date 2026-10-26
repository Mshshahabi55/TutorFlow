using System.IO;
using System.Net;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Notifications.Infrastructure;

namespace TutorFlow.Modules.Notifications.Tests;

/// <summary>
/// ADR-0029 Addendum A (Batch 55): proves <c>PostmarkEmailSender</c>'s own classification of a
/// send attempt into Sent / ConfirmedNotSent / Ambiguous, by injecting a fake
/// <see cref="HttpMessageHandler"/> directly into the <see cref="HttpClient"/> it receives from
/// <see cref="IHttpClientFactory"/> — no live Postmark account, no real network, no DB. This is
/// the narrowest reliable seam: <c>PostmarkEmailSender</c> depends only on
/// <see cref="IHttpClientFactory"/>, and <see cref="HttpClient"/>'s public constructor accepts any
/// <see cref="HttpMessageHandler"/>, so the real production class is exercised unmodified.
/// </summary>
public class PostmarkEmailSenderTests
{
    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }

    private sealed class SingleClientHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    // Simulates a status line being received but the response body stream failing before it can
    // be read — the exact gap Batch 59 closes. Throwing from SerializeToStreamAsync is what
    // HttpContent's own buffered-read path (used internally by ReadAsStringAsync) invokes.
    private sealed class ThrowingHttpContent(Func<Exception> exceptionFactory) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            throw exceptionFactory();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private static PostmarkEmailSender CreateSender(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.postmarkapp.com/") };
        var options = Options.Create(new NotificationsOptions { PostmarkServerToken = "test-token" });
        return new PostmarkEmailSender(new SingleClientHttpClientFactory(client), options);
    }

    private static EmailMessage SampleMessage() => new("student@example.com", "Subject", "<p>Body</p>");

    [Fact]
    public async Task SendAsync_TransportFailureWhileReadingResponse_ReturnsAmbiguous_NotConfirmedFailure()
    {
        // Simulates the exact scenario the audit flagged: the request was already transmitted
        // (Postmark may have already processed it) but the connection failed before this client
        // could confirm the outcome — surfaces as HttpRequestException, same as a real dropped
        // connection reading the response body.
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("Simulated connection reset while reading the response."));
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(SampleMessage(), TestContext.Current.CancellationToken);

        Assert.Equal(EmailDeliveryOutcome.Ambiguous, result.Outcome);
        Assert.NotNull(result.ErrorMessage);
    }

    // Batch 58 Finding A / Batch 59: the status line can be received (proving a server responded)
    // while the connection still fails before the body — carrying the ErrorCode/Message detail —
    // can be read. That failure does not rule out Postmark having already queued/sent the email,
    // so it must be Ambiguous regardless of which status code was on the status line, never a
    // confirmed outcome derived from that unread body.
    [Fact]
    public async Task SendAsync_NonSuccessResponse_BodyReadThrowsHttpRequestException_ReturnsAmbiguous_NotConfirmedFailure()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = new ThrowingHttpContent(() => new HttpRequestException("Simulated connection reset while reading the response body."))
        });
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(SampleMessage(), TestContext.Current.CancellationToken);

        Assert.Equal(EmailDeliveryOutcome.Ambiguous, result.Outcome);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task SendAsync_NonSuccessResponse_BodyReadThrowsIOException_ReturnsAmbiguous_NotConfirmedFailure()
    {
        // A different, equally realistic transport-failure shape for a dropped connection
        // mid-stream — proves the fix isn't narrowly coupled to one exception type.
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new ThrowingHttpContent(() => new IOException("Simulated stream reset while reading the response body."))
        });
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(SampleMessage(), TestContext.Current.CancellationToken);

        Assert.Equal(EmailDeliveryOutcome.Ambiguous, result.Outcome);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task SendAsync_NonSuccessResponse_BodyReadThrowsCancellation_PropagatesCancellation_NeverReturnsAResult()
    {
        // Cancellation during body reading must never be converted into Ambiguous or any other
        // definitive EmailSendResult — same "never resolve" treatment as every other cancellation
        // path in this feature.
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = new ThrowingHttpContent(() => new OperationCanceledException("Simulated cancellation while reading the response body."))
        });
        var sender = CreateSender(handler);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            sender.SendAsync(SampleMessage(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendAsync_422UnprocessableEntity_ReturnsConfirmedNotSent()
    {
        // Real Postmark semantics (postmarkapp.com/support/article/troubleshooting-422-unprocessable-entity-errors):
        // a 422 means Postmark fully received and validated the request and rejected it before
        // any attempt to queue/send it — a genuine, pre-delivery, confirmed rejection.
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent("""{"ErrorCode":300,"Message":"Invalid 'From' address: not a verified sender signature."}""")
        });
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(SampleMessage(), TestContext.Current.CancellationToken);

        Assert.Equal(EmailDeliveryOutcome.ConfirmedNotSent, result.Outcome);
    }

    // Batch 56 audit finding: 408 is syntactically a 4xx, but it means the opposite of "Postmark
    // evaluated the request" — the server (or an intermediary in front of it) stopped waiting for
    // a request it had not yet fully received. Unlike 422/401, nothing here proves Postmark's own
    // application ever saw this request, so it must never be classified as a confirmed rejection.
    [Fact]
    public async Task SendAsync_408RequestTimeout_ReturnsAmbiguous_NotConfirmedFailure()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.RequestTimeout)
        {
            Content = new StringContent("Request Timeout")
        });
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(SampleMessage(), TestContext.Current.CancellationToken);

        Assert.Equal(EmailDeliveryOutcome.Ambiguous, result.Outcome);
    }

    [Fact]
    public async Task SendAsync_401Unauthorized_ReturnsConfirmedNotSent()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("""{"ErrorCode":10,"Message":"Invalid API key."}""")
        });
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(SampleMessage(), TestContext.Current.CancellationToken);

        Assert.Equal(EmailDeliveryOutcome.ConfirmedNotSent, result.Outcome);
    }

    [Fact]
    public async Task SendAsync_500InternalServerError_ReturnsAmbiguous_NotConfirmedFailure()
    {
        // Real Postmark semantics for 500: "In most cases the message is lost during the process"
        // — Postmark's own documentation does not claim to know whether the email was sent, so
        // this must never be classified as a confident non-delivery.
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal error")
        });
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(SampleMessage(), TestContext.Current.CancellationToken);

        Assert.Equal(EmailDeliveryOutcome.Ambiguous, result.Outcome);
    }

    [Fact]
    public async Task SendAsync_200Ok_ReturnsSent()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"ErrorCode":0,"Message":"OK"}""")
        });
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(SampleMessage(), TestContext.Current.CancellationToken);

        Assert.Equal(EmailDeliveryOutcome.Sent, result.Outcome);
    }

    [Fact]
    public async Task SendAsync_CancellationSurfacesDuringTheCall_PropagatesCancellation_NeverReturnsAResult()
    {
        // A client-side HttpClient timeout, or the caller's own token firing mid-request, both
        // surface as an OperationCanceledException-derived exception — must never be caught and
        // converted into any EmailSendResult (not even Ambiguous), since the caller treats
        // cancellation as its own distinct "never resolve" case.
        var handler = new StubHttpMessageHandler(_ => throw new TaskCanceledException("Simulated client-side timeout."));
        var sender = CreateSender(handler);

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            sender.SendAsync(SampleMessage(), TestContext.Current.CancellationToken));
    }
}
