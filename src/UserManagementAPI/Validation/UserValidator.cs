using System.Net.Mail;
using UserManagementAPI.Models;

namespace UserManagementAPI.Validation;

/// <summary>
/// Checks that a submitted user record is complete and well formed before the store accepts it.
/// </summary>
/// <remarks>
/// The rules live here rather than inside the route handlers so that create and update apply
/// exactly the same checks, and so a rule can be read, changed, or added in one place.
/// </remarks>
public static class UserValidator
{
    /// <summary>
    /// Validates a submitted user. Returns <c>true</c> when the record is acceptable; otherwise
    /// returns <c>false</c> and fills <paramref name="errors"/> with one entry per invalid field,
    /// keyed by field name so the caller is told exactly what to correct.
    /// </summary>
    public static bool TryValidate(User user, out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(user.Name))
        {
            errors[nameof(User.Name)] = ["Name is required and cannot be empty or whitespace."];
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            errors[nameof(User.Email)] = ["Email is required and cannot be empty or whitespace."];
        }
        else if (!IsWellFormedEmail(user.Email))
        {
            errors[nameof(User.Email)] = ["Email must be a valid email address, such as name@example.com."];
        }

        if (string.IsNullOrWhiteSpace(user.Department))
        {
            errors[nameof(User.Department)] = ["Department is required and cannot be empty or whitespace."];
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// A pragmatic email check: the address must parse, and its domain must contain a dot.
    /// </summary>
    /// <remarks>
    /// This is deliberately not full RFC 5322 validation, and it does not confirm that the
    /// mailbox exists. It rejects the kind of input this API actually receives by mistake —
    /// a bare word, a missing "@", or a domain with no dot in it.
    /// </remarks>
    private static bool IsWellFormedEmail(string email)
    {
        if (email.Contains(' ') || !MailAddress.TryCreate(email, out var parsed))
        {
            return false;
        }

        return parsed.Host.Contains('.') && !parsed.Host.StartsWith('.') && !parsed.Host.EndsWith('.');
    }
}
