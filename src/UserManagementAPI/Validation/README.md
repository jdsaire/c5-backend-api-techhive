# Validation

The rules a submitted user record must satisfy before the store will accept it.

| File | What it does |
|---|---|
| `UserValidator.cs` | Checks a submitted user and reports every field that is wrong. |

The rules are here rather than inline in the route handlers for one practical reason: create and
update must apply exactly the same checks, and keeping them in one place is what stops the two
from drifting apart. That mattered in practice — update was still accepting invalid data after
create had been fixed.

| Field | Rule |
|---|---|
| `Name` | Required; cannot be empty or whitespace |
| `Email` | Required; must parse as an email address whose domain contains a dot |
| `Department` | Required; cannot be empty or whitespace |

A rejected request receives `400` and is told **every** field that is wrong, not just the first, so
a caller fixing a form can correct everything in one pass.

The email check is deliberately pragmatic rather than exhaustive. It is not full RFC 5322
validation and it does not confirm the mailbox exists — it rejects the mistakes this API actually
receives.

## Related

- [../Models/README.md](../Models/README.md) — the record being validated
- [../../../docs/debugging-notes.md](../../../docs/debugging-notes.md) — the edge cases tested
- [../README.md](../README.md) — the project layout
