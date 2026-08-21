using System.Diagnostics;

namespace UserManagementAPI.Middleware;

/// <summary>
/// Writes one log line for every request that reaches it, recording the HTTP method, the request
/// path, and the status code the pipeline produced.
/// </summary>
/// <remarks>
/// The status code is only known after the rest of the pipeline has run, so the line is written
/// on the way back out rather than on the way in.
///
/// A request that ends in an exception is logged differently, and deliberately so. This component
/// sits inside the error-handling middleware, which means an escaping exception passes through
/// here on its way out <em>before</em> the error handler has set the status code. Reading
/// <c>Response.StatusCode</c> at that moment reports 200 — the default the response still carries
/// — which is not what the caller receives. Rather than record a status this component never
/// observed, a failed request is logged as a failure and the exception is rethrown for the error
/// handler to convert.
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
        catch (Exception exception)
        {
            stopwatch.Stop();

            // The status code is not known here — see the remarks on this class. Report what is
            // actually true: the request failed, and something further out will answer it.
            logger.LogWarning(
                "{Method} {Path} failed after {ElapsedMilliseconds} ms with {ExceptionType}; the error-handling middleware produces the response",
                method,
                path,
                stopwatch.ElapsedMilliseconds,
                exception.GetType().Name);

            throw;
        }

        stopwatch.Stop();
        logger.LogInformation(
            "{Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms",
            method,
            path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}
