# UserManagementAPI

The application. An ASP.NET Core Web API that manages a directory of user records for TechHive
Solutions.

Run it from the repository root:

```bash
dotnet run --project src/UserManagementAPI --launch-profile http
```

then open <http://localhost:5139/swagger>. Full instructions in
[../../docs/how-to-run.md](../../docs/how-to-run.md).

## How the code is arranged

`Program.cs` is deliberately short. It says which features are switched on and in what order, and
nothing else — each concern lives in its own folder.

| File or folder | What it holds |
|---|---|
| `Program.cs` | Starts the application, registers services, and configures the middleware pipeline in order. |
| [Models/](Models/) | The shape of a user record. |
| [Storage/](Storage/) | The in-memory store that holds the records. |
| [Endpoints/](Endpoints/) | The five CRUD route handlers. |
| [Validation/](Validation/) | The rules a submitted user must satisfy. |
| [Middleware/](Middleware/) | Error handling, token authentication, and request logging. |
| [Properties/](Properties/) | Local run settings, including the port the API listens on. |
| `appsettings.json` | Configuration, including the log levels and the demonstration token. |
| `UserManagementAPI.http` | Ready-to-send requests for every endpoint. |

Reading it in that order — model, store, endpoints, validation, middleware — follows a request
from the outside in.

## Related

- [../../docs/api-testing.md](../../docs/api-testing.md) — what each endpoint does, with evidence
- [../../docs/middleware-pipeline.md](../../docs/middleware-pipeline.md) — the pipeline in detail
- [../../learning-mode/README.md](../../learning-mode/README.md) — the same material in plain language
