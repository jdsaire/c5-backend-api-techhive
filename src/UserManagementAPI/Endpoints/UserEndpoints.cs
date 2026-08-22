using UserManagementAPI.Models;
using UserManagementAPI.Storage;
using UserManagementAPI.Validation;

namespace UserManagementAPI.Endpoints;

/// <summary>
/// Registers every route the User Management API exposes.
/// </summary>
/// <remarks>
/// The route handlers live here rather than in <c>Program.cs</c> so that startup stays thin and
/// readable: <c>Program.cs</c> says which features are switched on, this file says what the API
/// actually does.
///
/// Every handler wraps its work in a try-catch. If anything inside a handler throws, the caller
/// receives a 500 with a short JSON message instead of the request failing mid-flight, and the
/// exception is written to the log with its stack trace. This is the endpoint-level guard; the
/// application also has error-handling middleware that catches anything thrown outside a handler.
/// </remarks>
public static class UserEndpoints
{
    /// <summary>The log category used for exceptions caught at the endpoint layer.</summary>
    private const string LogCategory = "UserManagementAPI.Endpoints.UserEndpoints";

    /// <summary>Maps the user routes onto the application's route table.</summary>
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var users = routes.MapGroup("/users").WithTags("Users");

        // Retrieve every user in the directory.
        users.MapGet("", (UserStore store, ILoggerFactory logs) =>
             {
                 try
                 {
                     return Results.Ok(store.GetAll());
                 }
                 catch (Exception exception)
                 {
                     return ServerError(exception, logs);
                 }
             })
             .WithName("GetUsers")
             .WithSummary("Retrieve the list of users.")
             .Produces<IReadOnlyList<User>>(StatusCodes.Status200OK)
             .Produces(StatusCodes.Status500InternalServerError);

        // Retrieve a single user by id. Answered with 404 if no such user is held.
        users.MapGet("/{id:int}", (int id, UserStore store, ILoggerFactory logs) =>
             {
                 try
                 {
                     var user = store.GetById(id);
                     return user is null ? NoSuchUser(id) : Results.Ok(user);
                 }
                 catch (Exception exception)
                 {
                     return ServerError(exception, logs);
                 }
             })
             .WithName("GetUserById")
             .WithSummary("Retrieve a single user by id.")
             .Produces<User>(StatusCodes.Status200OK)
             .Produces(StatusCodes.Status404NotFound)
             .Produces(StatusCodes.Status500InternalServerError);

        // Add a new user to the directory. Rejected with 400 if the submitted record is invalid.
        users.MapPost("", (User? user, UserStore store, ILoggerFactory logs) =>
             {
                 try
                 {
                     // The body is bound as nullable so that a missing or literal-null body
                     // arrives here instead of failing during parameter binding. Minimal API has
                     // no equivalent of the automatic ModelState check that [ApiController] runs
                     // in a controller-based project, so the only checks a submitted body gets
                     // are the ones written in this file and in UserValidator.
                     if (user is null)
                     {
                         return NoBodySubmitted();
                     }

                     if (!UserValidator.TryValidate(user, out var errors))
                     {
                         return Results.ValidationProblem(errors);
                     }

                     var created = store.Add(user);
                     return Results.Created($"/users/{created.Id}", created);
                 }
                 catch (Exception exception)
                 {
                     return ServerError(exception, logs);
                 }
             })
             .WithName("CreateUser")
             .WithSummary("Add a new user.")
             .Produces<User>(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .Produces(StatusCodes.Status500InternalServerError);

        // Update an existing user's details. Rejected with 400 if the submitted record is invalid.
        users.MapPut("/{id:int}", (int id, User? user, UserStore store, ILoggerFactory logs) =>
             {
                 try
                 {
                     // Bound as nullable for the same reason as the create handler above: Minimal
                     // API leaves this check to us.
                     if (user is null)
                     {
                         return NoBodySubmitted();
                     }

                     if (!UserValidator.TryValidate(user, out var errors))
                     {
                         return Results.ValidationProblem(errors);
                     }

                     var updated = store.Update(id, user);
                     return updated is null ? NoSuchUser(id) : Results.Ok(updated);
                 }
                 catch (Exception exception)
                 {
                     return ServerError(exception, logs);
                 }
             })
             .WithName("UpdateUser")
             .WithSummary("Update an existing user's details.")
             .Produces<User>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .Produces(StatusCodes.Status404NotFound)
             .Produces(StatusCodes.Status500InternalServerError);

        // Remove a user from the directory. Answered with 404 if no such user is held.
        users.MapDelete("/{id:int}", (int id, UserStore store, ILoggerFactory logs) =>
             {
                 try
                 {
                     return store.Delete(id) ? Results.NoContent() : NoSuchUser(id);
                 }
                 catch (Exception exception)
                 {
                     return ServerError(exception, logs);
                 }
             })
             .WithName("DeleteUser")
             .WithSummary("Remove a user by id.")
             .Produces(StatusCodes.Status204NoContent)
             .Produces(StatusCodes.Status404NotFound)
             .Produces(StatusCodes.Status500InternalServerError);

        return routes;
    }

    /// <summary>
    /// The single 400 answer used when a request arrives with no body at all.
    /// </summary>
    /// <remarks>
    /// This is a flat <c>{ "error": ... }</c> rather than the validation problem format used for
    /// field-level failures, and deliberately so. The problem format exists to carry one entry
    /// per invalid field; a body that was never sent has no fields to report. It is a single
    /// statement, so it takes the same shape as the API's other single-statement errors.
    /// </remarks>
    private static IResult NoBodySubmitted() =>
        Results.BadRequest(new { error = "A user record is required in the request body." });

    /// <summary>
    /// The single 404 answer used by every route that addresses one user by id, so a caller sees
    /// the same shape and the same wording whichever verb they used.
    /// </summary>
    private static IResult NoSuchUser(int id) =>
        Results.NotFound(new { error = $"No user with id {id} was found." });

    /// <summary>
    /// Logs an exception caught inside a handler and answers the caller with a 500.
    /// </summary>
    /// <remarks>
    /// The response deliberately carries no exception detail. The stack trace goes to the log,
    /// where the team can read it; the caller is told only that the request failed. The wording
    /// differs from the error-handling middleware's response so that, while testing, it is
    /// obvious which of the two layers answered.
    /// </remarks>
    private static IResult ServerError(Exception exception, ILoggerFactory logs)
    {
        logs.CreateLogger(LogCategory)
            .LogError(exception, "Unhandled exception caught at the endpoint layer.");

        return Results.Json(
            new { error = "An unexpected error occurred while processing the user request." },
            statusCode: StatusCodes.Status500InternalServerError);
    }
}
