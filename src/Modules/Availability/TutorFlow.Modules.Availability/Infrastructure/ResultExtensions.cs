using Microsoft.AspNetCore.Http;
using TutorFlow.Modules.Availability.Domain;

namespace TutorFlow.Modules.Availability.Infrastructure;

public static class ResultExtensions
{
    public static IResult ToProblemDetails(this Result result, int statusCode = StatusCodes.Status400BadRequest)
    {
        return TypedResults.Problem(
            title: "One or more errors occurred.",
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>
            {
                ["errors"] = result.Errors
            });
    }
}
