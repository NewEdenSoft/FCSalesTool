namespace FCSalesTool.Api.Common;

/// <summary>
/// CSRF defence on top of SameSite=Lax cookies: state-changing requests to /api and /auth must
/// carry <c>X-Requested-With</c>. Browsers can't add custom headers to cross-site form posts,
/// and cross-origin fetches with it need a CORS preflight we never grant.
/// </summary>
public static class CsrfHeader
{
    public const string Name = "X-Requested-With";

    public static IApplicationBuilder UseCsrfHeaderCheck(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        var request = context.Request;
        var isUnsafe = !(HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method) || HttpMethods.IsTrace(request.Method));
        var isProtected = request.Path.StartsWithSegments("/api") || request.Path.StartsWithSegments("/auth");

        if (isUnsafe && isProtected && !request.Headers.ContainsKey(Name))
        {
            await TypedResults.Problem(
                    title: "Missing request header.",
                    detail: $"State-changing requests must include the {Name} header.",
                    statusCode: StatusCodes.Status400BadRequest,
                    extensions: new Dictionary<string, object?> { ["code"] = "missing_csrf_header" })
                .ExecuteAsync(context);
            return;
        }

        await next(context);
    });
}
