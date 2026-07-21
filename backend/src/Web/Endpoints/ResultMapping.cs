using TutorFlow.Application.Common;

namespace TutorFlow.Web.Endpoints;

// Maps an Application Result/Result<T> to an HTTP response without ever
// reading Value when the Result is a failure. Result<T>.Value throws by
// design when IsFailure is true (Application/Common/Result.cs is
// unchanged); passing a failed Result<T> straight to Results.Ok(...) makes
// the JSON serializer reflect over Value anyway, which throws mid-response.
// This is presentation-layer response shaping only — it makes no business
// decision and does not reclassify what Application already decided
// (docs/adr/ADR-010-api-boundary.md: Error Boundary Principles). The status
// code chosen below is a mechanical mapping of the classification Application
// already produced (Error.Type, Error.Code); it invents no new error
// category and reclassifies nothing.
internal static class ResultMapping
{
    public static IResult ToApiResult(this Result result, ILogger logger, string handlerName)
    {
        if (result.IsFailure)
        {
            LogFailure(logger, handlerName, result.Error);
            return ToErrorResult(result.Error);
        }

        return Results.Ok(ApiResponse.Ok());
    }

    public static IResult ToApiResult<TValue>(this Result<TValue> result, ILogger logger, string handlerName)
    {
        if (result.IsFailure)
        {
            LogFailure(logger, handlerName, result.Error);
            return ToErrorResult(result.Error);
        }

        // Result<TValue>.Value throws if IsFailure — never reached here,
        // since the failure branch above already returned.
        return Results.Ok(ApiResponse<TValue>.Ok(result.Value));
    }

    private static IResult ToErrorResult(Error error) =>
        Results.Json(ApiResponse.Fail(ApiError.FromDomain(error)), statusCode: StatusCodeFor(error));

    // Every Domain error code already in use follows one of four shapes:
    // "<Use case>.NotFound" / "<Entity>NotFound" (a lookup failure),
    // "<Use case>.InvalidState" or a code naming a resource that already
    // exists in a conflicting state — "SlotAlreadyBooked" or
    // "EmailAlreadyRegistered" (the action conflicts with the resource's
    // current state — of which a storage-layer uniqueness/concurrency
    // rejection, per ADR-014, is one instance), "LoginCommand.*" (a failed
    // authentication attempt — the one category genuinely distinct from
    // "bad input," warranting 401 rather than 400,
    // docs/adr/ADR-017-authentication-mechanism-decision.md), or everything
    // else, which is a structural/input validation failure. Every existing
    // mapping above is unchanged. ErrorType.Authorization is new (Project
    // Director decision, Launch Preparation, Priority 2, WP4 Priority 3
    // Layer 2 status convention, 2026-07-21): a fine-grained,
    // resource-instance authorization rejection (ADR-003's two-tier model)
    // is neither a validation failure (400) nor a "does not exist" lookup
    // failure (404) — it is a rejection at the resource-ownership layer. A
    // handful of fine-grained handlers carry no RequirePermission metadata
    // (WP4 Priority 5's read endpoints), so they are the only ones that can
    // reach this layer while unauthenticated; those use the
    // ".Unauthenticated" code suffix (OwnershipExtensions.VerifyAuthenticated,
    // WP4 Final Cleanup, 2026-07-21) and map to 401, matching the same
    // contract AuthorizationMiddleware already applies to
    // RequirePermission-protected endpoints. Every other Authorization error
    // is an authenticated caller acting outside their permission on a
    // specific resource, which is what 403 means.
    private static int StatusCodeFor(Error error) => error.Type switch
    {
        ErrorType.Domain when error.Code.Contains("NotFound", StringComparison.Ordinal) =>
            StatusCodes.Status404NotFound,
        ErrorType.Domain when error.Code.EndsWith(".InvalidState", StringComparison.Ordinal)
            || error.Code.Contains("AlreadyBooked", StringComparison.Ordinal)
            || error.Code.Contains("AlreadyRegistered", StringComparison.Ordinal) =>
            StatusCodes.Status409Conflict,
        ErrorType.Domain when error.Code.StartsWith("LoginCommand.", StringComparison.Ordinal) =>
            StatusCodes.Status401Unauthorized,
        ErrorType.Domain => StatusCodes.Status400BadRequest,
        ErrorType.Authorization when error.Code.EndsWith(".Unauthenticated", StringComparison.Ordinal) =>
            StatusCodes.Status401Unauthorized,
        ErrorType.Authorization => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError,
    };

    private static void LogFailure(ILogger logger, string handlerName, Error error)
    {
        // Domain and Infrastructure failures only — a successful outcome is
        // never logged here. No request payload or Domain Actor identifier is
        // included, only the already-classified error shape.
        logger.LogWarning(
            "{HandlerName} returned a {ErrorType} error: {ErrorCode}",
            handlerName,
            error.Type,
            error.Code);
    }
}
