namespace TutorFlow.Web.Middleware;

// Baseline, product-agnostic response headers — no CORS policy here, since
// no approved document establishes a frontend origin to configure it
// against; inventing one would be guessing product information, not
// implementing an architecture decision.
internal sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";

        await _next(context);
    }
}
