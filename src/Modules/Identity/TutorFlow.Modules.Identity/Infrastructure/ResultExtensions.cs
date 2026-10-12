using Microsoft.AspNetCore.Http;

namespace TutorFlow.Modules.Identity.Infrastructure;

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
