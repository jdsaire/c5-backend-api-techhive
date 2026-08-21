namespace UserManagementAPI.Models;

/// <summary>
/// A user record in TechHive Solutions' internal directory.
/// </summary>
/// <remarks>
/// The shape is deliberately small. Every field earns its place from the assignment scenario:
/// the API routes users by id, the HR and IT departments are the stated consumers, and name and
/// email are the two fields the brief names as validation targets.
/// </remarks>
public class User
{
    /// <summary>Identifier assigned by the store when the user is created. Clients never supply it.</summary>
    public int Id { get; set; }

    /// <summary>The user's full name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The user's work email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>The department the user belongs to, such as HR or IT.</summary>
    public string Department { get; set; } = string.Empty;
}
