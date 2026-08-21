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
/// </remarks>
public static class UserEndpoints
{
    /// <summary>Maps the user routes onto the application's route table.</summary>
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var users = routes.MapGroup("/users").WithTags("Users");

        // Retrieve every user in the directory.
        users.MapGet("", (UserStore store) => store.GetAll())
             .WithName("GetUsers")
             .WithSummary("Retrieve the list of users.");

        // Retrieve a single user by id. Answered with 404 if no such user is held.
        users.MapGet("/{id:int}", (int id, UserStore store) =>
             {
                 var user = store.GetById(id);
                 return user is null ? NoSuchUser(id) : Results.Ok(user);
             })
             .WithName("GetUserById")
             .WithSummary("Retrieve a single user by id.")
             .Produces<User>(StatusCodes.Status200OK)
             .Produces(StatusCodes.Status404NotFound);

        // Add a new user to the directory. Rejected with 400 if the submitted record is invalid.
        users.MapPost("", (User user, UserStore store) =>
             {
                 if (!UserValidator.TryValidate(user, out var errors))
                 {
                     return Results.ValidationProblem(errors);
                 }

                 var created = store.Add(user);
                 return Results.Created($"/users/{created.Id}", created);
             })
             .WithName("CreateUser")
             .WithSummary("Add a new user.")
             .Produces<User>(StatusCodes.Status201Created)
             .ProducesValidationProblem();

        // Update an existing user's details. Rejected with 400 if the submitted record is invalid.
        users.MapPut("/{id:int}", (int id, User user, UserStore store) =>
             {
                 if (!UserValidator.TryValidate(user, out var errors))
                 {
                     return Results.ValidationProblem(errors);
                 }

                 var updated = store.Update(id, user);
                 return updated is null ? NoSuchUser(id) : Results.Ok(updated);
             })
             .WithName("UpdateUser")
             .WithSummary("Update an existing user's details.")
             .Produces<User>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .Produces(StatusCodes.Status404NotFound);

        // Remove a user from the directory. Answered with 404 if no such user is held.
        users.MapDelete("/{id:int}", (int id, UserStore store) =>
             {
                 return store.Delete(id) ? Results.NoContent() : NoSuchUser(id);
             })
             .WithName("DeleteUser")
             .WithSummary("Remove a user by id.")
             .Produces(StatusCodes.Status204NoContent)
             .Produces(StatusCodes.Status404NotFound);

        return routes;
    }

    /// <summary>
    /// The single 404 answer used by every route that addresses one user by id, so a caller sees
    /// the same shape and the same wording whichever verb they used.
    /// </summary>
    private static IResult NoSuchUser(int id) =>
        Results.NotFound(new { error = $"No user with id {id} was found." });
}
