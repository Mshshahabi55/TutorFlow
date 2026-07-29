using System.Diagnostics;

namespace TutorFlow.Web.Middleware;

// Production-readiness gap: nothing previously logged a request/response
// pair at all — GlobalExceptionHandler only ever logs the unhandled-
// exception case, and the AuditDomainEventHandler trail (ADR-016) only
// covers the six named business events, not "a request happened." Every
// request now gets exactly one structured log line, regardless of outcome
// (success, a Result-mapped failure, a rate-limit rejection, or an
// unhandled exception the exception handler already turned into a
// response) — PROJECT_CONSTITUTION.md's own Success Criteria: an
// Admin/Staff member must be able to detect a problem without reading code
// or querying a database directly.
//
// Registered first (Program.cs), wrapping every other middleware, so the
// elapsed-time measurement and the final status code are both accurate no
// matter which later middleware (rate limiter, exception handler,
// authorization) is what actually produced the response.
internal sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Lets a caller correlate their own client-side error report back
        // to this exact server-side log line — the same TraceIdentifier
        // GlobalExceptionHandler already surfaces in its ApiError.TraceId,
        // now visible before a request even fails, not only after.
        context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var logLevel = statusCode >= 500 ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(
                logLevel,
                "{Method} {Path} responded {StatusCode} in {ElapsedMs}ms (TraceId: {TraceId})",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                context.TraceIdentifier);
        }
    }
}
