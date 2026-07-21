using Microsoft.AspNetCore.Diagnostics;
using TutorFlow.Application.Common;
using TutorFlow.Web.Endpoints;

namespace TutorFlow.Web.Middleware;

// Last-resort safety net for any exception a handler did not already
// translate into a Result (docs/adr/ADR-008-error-handling.md: Infrastructure
// Failure). Logs the full exception for Admin/Staff-equivalent diagnosis
// (PROJECT_CONSTITUTION.md: Success Criteria — detectable without reading
// code or querying a database directly) while returning only a general
// notice to the caller, with no technical detail
// (ADR-008: User Communication Principles). Response shape mirrors
// ResultMapping.ToApiResult() so callers see one consistent contract
// regardless of whether a failure was classified by a handler or caught here.
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path} (TraceId: {TraceId})",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        var body = ApiResponse.Fail(new ApiError(
            "Infrastructure.UnexpectedFailure",
            "The request could not be completed. Please try again.",
            ErrorType.Infrastructure,
            httpContext.TraceIdentifier));

        await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);

        return true;
    }
}
