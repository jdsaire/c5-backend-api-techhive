# TechHive Solutions — User Management API

A User Management API built with ASP.NET Core, for TechHive Solutions' internal tools. The HR and
IT departments need to create, update, retrieve, and delete user records, and this API is the
service that lets them.

It exposes five endpoints over a directory of user records, validates what it is given, answers
predictably when asked for something that is not there, and runs every request through a
middleware pipeline that handles errors, checks a token, and logs the outcome.

---

## See it running

**This API is not hosted anywhere.** There is no public URL to visit — a Web API renders no page,
and Swagger UI needs a live server behind it, which static hosting cannot provide. To look at it,
you run it yourself. It takes two commands.

**1. Start it** (needs the [.NET SDK 10.0+](https://dotnet.microsoft.com/download)):

```bash
git clone https://github.com/jdsaire/c5-backend-api-techhive.git
cd c5-backend-api-techhive
dotnet run --project src/UserManagementAPI --launch-profile http
```

**2. Open Swagger UI** in your browser:

### 👉 **<http://localhost:5139/swagger>**

That is an interactive page listing all five endpoints. Expand one, click **Try it out**, then
**Execute**, and the real response appears underneath. `localhost` means "this computer", so the
link does nothing until step 1 is running on your own machine — it is not a link to a server on
the internet. If port `5139` is taken, the console prints the URL it actually used; use that one.

The raw OpenAPI document is at <http://localhost:5139/openapi/v1.json>.

> Calls to `/users` from Swagger UI's **Try it out** button return `401 Unauthorized`, because
> that page does not attach an authentication token. That is the middleware working as intended.
> To send an authenticated request:
>
> ```bash
> curl -H "Authorization: Bearer techhive-local-development-token" http://localhost:5139/users
> ```

**[docs/how-to-run.md](docs/how-to-run.md) has the full walkthrough**, including Postman, the
`.http` file, and what to do if something does not work.

---

## The endpoints

| Verb | Path | Purpose | Success |
|---|---|---|---|
| `GET` | `/users` | Retrieve the list of users | `200` — array of users |
| `GET` | `/users/{id}` | Retrieve a single user by id | `200` — one user |
| `POST` | `/users` | Add a new user | `201` + `Location` header |
| `PUT` | `/users/{id}` | Update an existing user's details | `200` — the updated user |
| `DELETE` | `/users/{id}` | Remove a user by id | `204` — no body |

Invalid input is rejected with `400` and told which fields are wrong. A request for a user that
does not exist gets `404`. A request without a valid token gets `401`.

Every endpoint, with real captured requests and responses, is in
[docs/api-testing.md](docs/api-testing.md).

---

## Tech stack

| | |
|---|---|
| **Language** | C# |
| **Framework** | ASP.NET Core 10 Web API, using minimal APIs |
| **Storage** | An in-memory dictionary. No database. |
| **API documentation** | OpenAPI, viewed through Swagger UI |
| **Packages** | `Microsoft.AspNetCore.OpenApi`, `Microsoft.OpenApi`, `Swashbuckle.AspNetCore.SwaggerUI` — and nothing else |

**Data does not survive a restart.** The records live in memory inside the running process, so
anything you add or change is gone when you stop the API, and it starts again from the same three
seeded users. That is deliberate: this project is about API behavior, not about data storage.

---

## How it is organised

```
src/UserManagementAPI/
  Program.cs        starts the app and configures the middleware pipeline
  Models/           the shape of a user record
  Storage/          the in-memory store
  Endpoints/        the five CRUD route handlers
  Validation/       the rules a submitted user must satisfy
  Middleware/       error handling, authentication, request logging
docs/               how to run it, how it was tested, what was fixed
learning-mode/      plain-language walkthroughs and a glossary
handoff/            the build plan and completion report
```

Every folder has its own README explaining why it exists.

---

## Documentation

**Start here**

- **[docs/how-to-run.md](docs/how-to-run.md)** — run the API and open Swagger UI
- [docs/setup-guide.md](docs/setup-guide.md) — prerequisites and first-time setup

**What it does and how it was tested**

- [docs/api-testing.md](docs/api-testing.md) — every endpoint, with observed requests and responses
- [docs/debugging-notes.md](docs/debugging-notes.md) — bugs found, what changed, edge cases run
- [docs/middleware-pipeline.md](docs/middleware-pipeline.md) — the three middleware components and their order

**Learning the concepts**

- [learning-mode/README.md](learning-mode/README.md) — walkthroughs written for someone new to web APIs
- **[learning-mode/Glossary.md](learning-mode/Glossary.md)** — plain-language definitions of the terms used here

**Reference**

- [docs/grading-criteria.md](docs/grading-criteria.md) — the assignment's criteria, recorded not answered
- [handoff/README.md](handoff/README.md) — the build plan and completion report

---

## A note on the authentication

The token check in this API is **simulated**. It compares a bearer token against a fixed value in
a configuration file. It verifies no cryptographic signature, issues no tokens, has no expiry, and
identifies no user — a valid token grants access to everything. The token itself is committed to
this repository in plain text, which means it is not a secret.

It exists to demonstrate where authentication belongs in a middleware pipeline and what a rejected
request looks like. **It does not make this API secure, and this project makes no such claim.**
The full statement is at the top of
[`TokenAuthenticationMiddleware.cs`](src/UserManagementAPI/Middleware/TokenAuthenticationMiddleware.cs).

---

## Out of scope

Deliberately not part of this project. Each is an omission by decision, not an oversight:

| Not included | Why |
|---|---|
| **A database, and Entity Framework Core** | The requirements never mention persistence. In-memory storage is the smallest thing that satisfies them, and it keeps the code about API behavior rather than data-access plumbing. |
| **Real authentication** — JWT, ASP.NET Core Identity, OAuth | The requirement is to demonstrate authentication middleware in a pipeline. Real token infrastructure is a much larger subject and is not what this project is teaching. |
| **A front end** | This is a back-end project. Swagger UI is the interface, and it is a development tool rather than a product surface. |
| **A GitHub Pages site** | Pages serves static files and runs no server process. An API has nothing to render statically, and Swagger UI needs the API running behind it. |
| **A test project** | Not part of the assignment's scope. Testing here is documented manual testing against a running instance, with observed responses recorded in `docs/`. |
| **Real-time features** — SignalR, WebSockets | Nothing in the requirements calls for them. |

---

## Attribution

"Back-End Development with .NET" — Course 5 of 12 in the Microsoft Full-Stack Developer
Specialization.
