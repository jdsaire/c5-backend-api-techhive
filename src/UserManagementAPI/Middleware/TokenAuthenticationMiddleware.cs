namespace UserManagementAPI.Middleware;

// ---------------------------------------------------------------------------------------------
//  THIS IS A SIMULATED TOKEN CHECK. IT IS NOT REAL AUTHENTICATION.
//
//  What it does:
//      Reads the Authorization header, requires the "Bearer " scheme, and compares the token
//      that follows against a single fixed value read from configuration. If they match, the
//      request continues. If they do not, the request is answered with 401.
//
//  What it does NOT do:
//      - It does not verify a cryptographic signature. The token is a plain string compared for
//        equality; nothing proves who issued it.
//      - It does not issue tokens. There is no login endpoint and no way to obtain a token other
//        than reading the configured value.
//      - It does not handle expiry. The configured token is valid forever.
//      - It does not identify a user. A valid token grants access to everything; it does not say
//        who the caller is, and no user, role, or claim is attached to the request.
//      - It does not protect the token. The value lives in appsettings.json in plain text, in
//        version control. That is not secret storage.
//
//  This exists to demonstrate where authentication sits in a middleware pipeline and what a
//  rejected request looks like. It must not be used to protect anything real. Replacing it with
//  genuine authentication means replacing this file entirely, not extending it.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Simulates token-based authentication by comparing a bearer token against a configured value.
/// See the file header for exactly what this does and does not check.
/// </summary>
public class TokenAuthenticationMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<TokenAuthenticationMiddleware> logger)
{
    private const string BearerPrefix = "Bearer ";

    /// <summary>
    /// Paths served without a token, so the API's own documentation stays readable in a browser.
    /// </summary>
    /// <remarks>
    /// Swagger UI is a page the browser loads directly; it cannot attach an Authorization header
    /// to its own asset requests, so guarding these paths would make the documentation
    /// unreachable. Only the documentation is exempt — every /users route is guarded. This is a
    /// deliberate trade for a development tool, and it is one more reason this check is not
    /// production authentication.
    /// </remarks>
    private static readonly string[] ExemptPathPrefixes = ["/openapi", "/swagger"];

    /// <summary>Checks the request's token and either passes it on or rejects it with 401.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsExempt(context.Request.Path))
        {
            await next(context);
            return;
        }

        var expectedToken = configuration["Authentication:Token"];

        if (string.IsNullOrWhiteSpace(expectedToken))
        {
            // Fail closed. A missing configuration value must never mean "let everything through".
            logger.LogError(
                "No value is configured for Authentication:Token, so every request is being rejected.");
            await RejectAsync(context, "The API is not configured to accept requests.");
            return;
        }

        var authorizationHeader = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            logger.LogWarning(
                "Rejected {Method} {Path}: no Authorization header was sent.",
                context.Request.Method,
                context.Request.Path);
            await RejectAsync(context, "An Authorization header with a bearer token is required.");
            return;
        }

        if (!authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Rejected {Method} {Path}: the Authorization header did not use the Bearer scheme.",
                context.Request.Method,
                context.Request.Path);
            await RejectAsync(context, "The Authorization header must use the Bearer scheme.");
            return;
        }

        var suppliedToken = authorizationHeader[BearerPrefix.Length..].Trim();

        // A plain string comparison. Nothing here validates a signature or an expiry date.
        if (!string.Equals(suppliedToken, expectedToken, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Rejected {Method} {Path}: the supplied token did not match the configured value.",
                context.Request.Method,
                context.Request.Path);
            await RejectAsync(context, "The supplied token is not valid.");
            return;
        }

        await next(context);
    }

    /// <summary>True when the path is documentation and is served without a token.</summary>
    private static bool IsExempt(PathString path) =>
        ExemptPathPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>Answers the caller with 401 and a JSON message, in the API's usual error shape.</summary>
    /// <remarks>
    /// The message never says which part was wrong in a way that helps someone guess the token —
    /// it says what a correct request looks like, not how close the attempt was. The rejection is
    /// logged in more detail than it is reported.
    /// </remarks>
    private static async Task RejectAsync(HttpContext context, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Bearer";
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}
