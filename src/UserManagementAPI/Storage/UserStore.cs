using UserManagementAPI.Models;

namespace UserManagementAPI.Storage;

/// <summary>
/// Holds the user records in memory for the lifetime of the running process.
/// </summary>
/// <remarks>
/// There is no database behind this class and none is intended. The store is registered as a
/// singleton, so the data lives as long as the application does and is lost when it stops —
/// restarting the API returns it to the seeded records below. That is expected for this project.
/// </remarks>
public class UserStore
{
    private readonly List<User> _users =
    [
        new User { Id = 1, Name = "Ada Reyes",     Email = "ada.reyes@techhive.example",     Department = "HR" },
        new User { Id = 2, Name = "Marcus Boateng", Email = "marcus.boateng@techhive.example", Department = "IT" },
        new User { Id = 3, Name = "Lena Fischer",  Email = "lena.fischer@techhive.example",  Department = "Engineering" }
    ];

    private int _nextId = 4;

    /// <summary>Returns every user currently held in the store.</summary>
    public IReadOnlyList<User> GetAll() => _users;

    /// <summary>Returns the user with the given id, or <c>null</c> if no such user is held.</summary>
    public User? GetById(int id) => _users.FirstOrDefault(user => user.Id == id);

    /// <summary>Assigns the next id to the supplied user, stores it, and returns the stored record.</summary>
    public User Add(User user)
    {
        user.Id = _nextId++;
        _users.Add(user);
        return user;
    }

    /// <summary>
    /// Overwrites the details of an existing user. Returns the updated record, or <c>null</c> if
    /// no user with that id is held.
    /// </summary>
    public User? Update(int id, User updated)
    {
        var existing = _users.FirstOrDefault(user => user.Id == id);
        if (existing is null)
        {
            return null;
        }

        existing.Name = updated.Name;
        existing.Email = updated.Email;
        existing.Department = updated.Department;
        return existing;
    }

    /// <summary>Removes the user with the given id. Returns false if no such user is held.</summary>
    public bool Delete(int id)
    {
        var existing = _users.FirstOrDefault(user => user.Id == id);
        if (existing is null)
        {
            return false;
        }

        _users.Remove(existing);
        return true;
    }
}
