# Endpoints

The routes this API exposes and the code that answers them.

| File | What it does |
|---|---|
| `UserEndpoints.cs` | Registers all five CRUD routes and holds their handlers. |

The handlers live here rather than in `Program.cs` so that startup stays readable: `Program.cs`
says which features are switched on, this file says what the API actually does. They are attached
to the application by one extension method, `MapUserEndpoints()`, called from `Program.cs`.

| Verb | Path | Success |
|---|---|---|
| `GET` | `/users` | `200` — array of users |
| `GET` | `/users/{id}` | `200` — one user |
| `POST` | `/users` | `201` + `Location` header |
| `PUT` | `/users/{id}` | `200` — the updated user |
| `DELETE` | `/users/{id}` | `204` — no body |

Each handler wraps its work in a `try`/`catch`, so a failure inside a handler becomes a `500` with
a short JSON message rather than an unhandled exception. That covers failures **inside** a
handler; failures before one is entered are caught by the error-handling middleware instead. Both
layers exist on purpose — see
[../../../docs/middleware-pipeline.md](../../../docs/middleware-pipeline.md).

## Related

- [../../../docs/api-testing.md](../../../docs/api-testing.md) — every endpoint with observed evidence
- [../Validation/README.md](../Validation/README.md) — the checks these handlers apply
- [../README.md](../README.md) — the project layout
