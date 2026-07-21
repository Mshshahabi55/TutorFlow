using TutorFlow.Application.Common;

namespace TutorFlow.Web.Endpoints;

// Named, OpenAPI-nameable response shapes replacing the anonymous objects
// ToApiResult previously returned (Backend Gap Closure Plan, Sprint 3:
// OpenAPI Response-Type Annotations). Anonymous types cannot be referenced
// by Produces<T>(), so the generated OpenAPI document could not describe any
// response schema until these existed. Property names are PascalCase C#
// convention; ASP.NET Core's default web JSON options apply a camelCase
// naming policy automatically, so the wire format is byte-for-byte
// unchanged from the anonymous-type shape it replaces.
public sealed record ApiError(string Code, string Message, ErrorType Type, string? TraceId = null)
{
    public static ApiError FromDomain(Error error) => new(error.Code, error.Message, error.Type);
}

public sealed record ApiResponse(bool IsSuccess, bool IsFailure, ApiError? Error)
{
    public static ApiResponse Ok() => new(true, false, null);

    public static ApiResponse Fail(ApiError error) => new(false, true, error);
}

public sealed record ApiResponse<TValue>(bool IsSuccess, bool IsFailure, ApiError? Error, TValue? Value)
{
    public static ApiResponse<TValue> Ok(TValue value) => new(true, false, null, value);
}
