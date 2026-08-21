using System.Collections.Concurrent;
using UserManagementAPI.Models;

namespace UserManagementAPI.Storage;

/// <summary>
/// Holds the user records in memory for the lifetime of the running process.
/// </summary>
/// <remarks>
/// There is no database behind this class and none is intended. The store is registered as a
/// singleton, so the data lives as long as the application does and is lost when it stops —
/// restarting the API returns it to the seeded records below. That is expected for this project.
///
/// Records are held in a dictionary keyed by id. Every route that addresses one user does so by
/// id, so keying the collection that way lets a lookup go straight to the record instead of
/// walking the collection until it finds a match. Using a concurrent dictionary also means two
/// requests arriving at the same time cannot corrupt the collection, which a plain list allows.
/// </remarks>
public class UserStore
{
    private readonly ConcurrentDictionary<int, User> _users = new(
    [
        new KeyValuePair<int, User>(1,
            new User { Id = 1, Name = "Ada Reyes", Email = "ada.reyes@techhive.example", Department = "HR" }),
        new KeyValuePair<int, User>(2,
            new User { Id = 2, Name = "Marcus Boateng", Email = "marcus.boateng@techhive.example", Department = "IT" }),
        new KeyValuePair<int, User>(3,
            new User { Id = 3, Name = "Lena Fischer", Email = "lena.fischer@techhive.example", Department = "Engineering" })
    ]);

    private int _lastId = 3;

    /// <summary>
    /// Returns every user currently held in the store, ordered by id.
    /// </summary>
    /// <remarks>
    /// A dictionary does not promise any particular enumeration order, so the result is sorted by
    /// id. That keeps the list the API returns stable and predictable from one call to the next.
    /// </remarks>
    public IReadOnlyList<User> GetAll() =>
        _users.Values.OrderBy(user => user.Id).ToList();

    /// <summary>Returns the user with the given id, or <c>null</c> if no such user is held.</summary>
    public User? GetById(int id) =>
        _users.TryGetValue(id, out var user) ? user : null;

    /// <summary>Assigns the next id to the supplied user, stores it, and returns the stored record.</summary>
    public User Add(User user)
    {
        user.Id = Interlocked.Increment(ref _lastId);
        _users[user.Id] = user;
        return user;
    }

    /// <summary>
    /// Overwrites the details of an existing user. Returns the updated record, or <c>null</c> if
    /// no user with that id is held.
    /// </summary>
    public User? Update(int id, User updated)
    {
        if (!_users.TryGetValue(id, out var existing))
        {
            return null;
        }

        existing.Name = updated.Name;
        existing.Email = updated.Email;
        existing.Department = updated.Department;
        return existing;
    }

    /// <summary>Removes the user with the given id. Returns false if no such user is held.</summary>
    public bool Delete(int id) => _users.TryRemove(id, out _);
}
