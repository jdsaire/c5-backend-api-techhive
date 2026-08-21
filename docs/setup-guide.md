# Setup guide

First-time setup for working with this project. If you only want to run it and look at it in
Swagger UI, [how-to-run.md](how-to-run.md) is the shorter path.

---

## Prerequisites

### The .NET SDK, 10.0 or newer

This is the only required prerequisite. It includes everything needed to build and run the
project — no separate runtime, package manager, or database to install.

Check what you have:

```bash
dotnet --version
```

If that prints `10.` followed by anything, you are set. If not, download the SDK for your
operating system from <https://dotnet.microsoft.com/download>. Choose the **SDK**, not the
Runtime — the Runtime can only run applications, not build them.

After installing, open a **new** terminal window and run `dotnet --version` again. A newly
installed SDK is not visible to terminals that were already open.

### Git

Needed only to clone the repository. You can also download the code as a ZIP from the GitHub page.

### A browser

Any modern browser, for Swagger UI.

### Optional

- **An editor.** Visual Studio, Visual Studio Code with the C# Dev Kit extension, or JetBrains
  Rider all understand this project without configuration. Any text editor works too.
- **Postman**, or VS Code's REST Client extension, for sending requests outside the browser.
  Neither is required — `curl` and Swagger UI cover everything.

### Not needed

No database server. No Docker. No cloud account. No package beyond what `dotnet restore` fetches
automatically. The data lives in memory inside the running process.

---

## First-time setup

```bash
git clone https://github.com/jdsaire/c5-backend-api-techhive.git
cd c5-backend-api-techhive
dotnet build src/UserManagementAPI
```

`dotnet build` restores the three NuGet packages on its first run, which needs an internet
connection. Expect:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

A clean build is the standard this project holds itself to — every commit in its history builds
with zero errors **and** zero warnings.

Then start it:

```bash
dotnet run --project src/UserManagementAPI --launch-profile http
```

and open <http://localhost:5139/swagger> in your browser. Full detail in
[how-to-run.md](how-to-run.md).

---

## The packages this project uses

Three, all fetched automatically:

| Package | Why it is here |
|---|---|
| `Microsoft.AspNetCore.OpenApi` | Generates the OpenAPI document describing the endpoints. Comes with the project template. |
| `Microsoft.OpenApi` | Pinned directly to a patched version. The template's transitive version carries a published security advisory, and pinning keeps the build free of warnings. |
| `Swashbuckle.AspNetCore.SwaggerUI` | Serves the interactive Swagger UI page that reads the document above. The .NET 10 template generates the document but ships no page to view it in. |

Nothing else was added.

---

## The project layout

```
src/UserManagementAPI/
  Program.cs        starts the app and sets up the middleware pipeline
  Models/           the shape of a user record
  Storage/          the in-memory store that holds the records
  Endpoints/        the five CRUD route handlers
  Validation/       the rules a submitted user must satisfy
  Middleware/       error handling, authentication, and logging
```

Each of those folders has its own short README. The root
[README.md](../README.md) has the full index.

---

## Working on the code

Rebuild after a change:

```bash
dotnet build src/UserManagementAPI
```

Or run with automatic rebuild-and-restart on save:

```bash
dotnet watch --project src/UserManagementAPI run --launch-profile http
```

There is no test project in this repository — see the out-of-scope list in the
[root README](../README.md).

---

## Related

- [README.md](README.md) — index of this folder
- [how-to-run.md](how-to-run.md) — running the API and opening Swagger UI
- [api-testing.md](api-testing.md) — every endpoint with observed requests and responses
- [grading-criteria.md](grading-criteria.md) — the assignment's criteria, recorded for reference
