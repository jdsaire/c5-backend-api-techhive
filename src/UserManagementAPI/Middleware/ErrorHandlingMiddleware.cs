namespace UserManagementAPI.Middleware;

/// <summary>
/// Catches exceptions that escape the rest of the pipeline and answers with a consistent JSON
/// error response instead of letting the failure reach the client in whatever form it took.
/// </summary>
/// <remarks>
/// This is the outer net. The route handlers in
/// <see cref="Endpoints.UserEndpoints"/> already guard their own work with try-catch, so most
/// failures never reach this class. What does reach it is everything that happens *outside* a
/// handler: a request body that cannot be deserialized into the handler's parameter, a failure in
/// another middleware component, anything thrown before routing picks an endpoint. Those cases
/// have no handler frame on the stack, so no catch inside a handler can see them.
///
/// Registered first in the pipeline, so it wraps every component that follows it.
/// </remarks>
public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    /// <summary>Runs the rest of the pipeline and converts any escaping exception into JSON.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BadHttpRequestException exception)
        {
            // The request itself was unreadable — a truncated body, a field of the wrong type.
            // That is the caller's mistake, not a server fault, so it keeps its own status code
            // (normally 400) and is logged as a warning rather than an error.
            logger.LogWarning(
                exception,
                "Rejected a malformed request to {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            await WriteErrorAsync(
                context,
                exception.StatusCode,
                "The request could not be read. Check that the body is valid JSON and that each field has the expected type.");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled exception caught by the error-handling middleware while handling {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "Internal server error.");
        }
    }

    /// <summary>
    /// Writes the error response, unless the response has already begun.
    /// </summary>
    /// <remarks>
    /// Once any bytes have gone to the client the status code and headers are fixed and cannot be
    /// rewritten. In that case there is nothing useful to send, so the failure is recorded and the
    /// connection is left to terminate — writing a second body would corrupt the first.
    ///
    /// The message sent to the caller never contains exception detail. The stack trace goes to the
    /// log, where the team can read it.
    /// </remarks>
    private async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
        {
            logger.LogWarning(
                "The response for {Method} {Path} had already started, so no error body could be written.",
                context.Request.Method,
                context.Request.Path);
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}
