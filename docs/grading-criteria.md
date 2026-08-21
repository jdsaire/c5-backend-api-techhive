# Assignment grading criteria, recorded for reference

This file records the criteria the project assignment lists, so that anyone reading this
repository can see what it was built against.

> **This file records the criteria. It does not answer them.** Nothing here is a claim that a
> criterion has been met, and no score is asserted. The evidence for what this project does is in
> the code, in the commit history, and in the testing documents linked below. Assessment is the
> reviewer's to make.

One criterion below refers to using an AI coding assistant. The assignment names a specific
commercial product; this repository does not name AI products anywhere, so it is recorded here in
neutral wording. The substance of the criterion is unchanged.

---

## The criteria

The assignment allocates **25 points total**, in five equal parts:

| Points | Criterion |
|---|---|
| 5 | Did you create a GitHub repository for your project? |
| 5 | Does your code include CRUD endpoints for managing users, such as GET, POST, PUT, and DELETE? |
| 5 | Did you use an AI coding assistant to debug your code? |
| 5 | Does your code include additional functionality, such as validation to process only valid user data? |
| 5 | Did you implement middleware into your project, such as logging or authentication middleware? |

## Submission fields

- Project title
- GitHub URL

---

## Where the relevant material lives

Offered as a reading map, not as an answer key.

| Topic | Where to look |
|---|---|
| Repository and history | The commit log — one commit per assignment step, in order |
| CRUD endpoints | [`Endpoints/UserEndpoints.cs`](../src/UserManagementAPI/Endpoints/UserEndpoints.cs), documented with observed responses in [api-testing.md](api-testing.md) |
| Debugging | [debugging-notes.md](debugging-notes.md) — each reported bug, what the code did before, what it does now, and the edge cases run |
| Validation | [`Validation/UserValidator.cs`](../src/UserManagementAPI/Validation/UserValidator.cs), tested in [debugging-notes.md](debugging-notes.md) |
| Middleware | [`Middleware/`](../src/UserManagementAPI/Middleware/), documented in [middleware-pipeline.md](middleware-pipeline.md) |

---

## The three activities the project was built through

The assignment is structured as three activities, each building on the last:

1. **Writing and enhancing API code** — create the project, generate CRUD endpoints, test them.
2. **Debugging API code** — find and fix validation gaps, missing-user handling, unhandled
   exceptions, and performance issues.
3. **Implementing and managing middleware** — logging, error handling, and authentication
   middleware, configured in a specified pipeline order.

Plain-language walkthroughs of all three are in
[../learning-mode/README.md](../learning-mode/README.md).

---

## Related

- [README.md](README.md) — index of this folder
- [../README.md](../README.md) — the project README
- [api-testing.md](api-testing.md) · [debugging-notes.md](debugging-notes.md) · [middleware-pipeline.md](middleware-pipeline.md)
