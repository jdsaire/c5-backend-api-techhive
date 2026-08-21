using System.Diagnostics;

namespace UserManagementAPI.Middleware;

/// <summary>
/// Writes one log line for every request that reaches it, recording the HTTP method, the request
/// path, and the status code the pipeline produced.
/// </summary>
/// <remarks>
/// The status code is only known after the rest of the pipeline has run, so the line is written
/// on the way back out rather than on the way in. A <c>finally</c> block does the writing, which
/// means a request that ends in an exception is still logged before that exception continues
/// outward to the error-handling middleware.
///
/// Registered last in the pipeline, per the configured order. One consequence of that placement
/// is recorded in the middleware documentation: requests rejected by the authentication
/// middleware never reach this component, so they do not appear in these lines. The
/// authentication middleware logs its own rejections for that reason.
/// </remarks>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    /// <summary>Runs the rest of the pipeline and logs the method, path, and resulting status.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path + context.Request.QueryString;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "{Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms",
                method,
                path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
