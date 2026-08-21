using UserManagementAPI.Models;
using UserManagementAPI.Storage;

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

        // Retrieve a single user by id.
        users.MapGet("/{id:int}", (int id, UserStore store) => store.GetById(id))
             .WithName("GetUserById")
             .WithSummary("Retrieve a single user by id.");

        // Add a new user to the directory.
        users.MapPost("", (User user, UserStore store) =>
             {
                 var created = store.Add(user);
                 return Results.Created($"/users/{created.Id}", created);
             })
             .WithName("CreateUser")
             .WithSummary("Add a new user.");

        return routes;
    }
}
