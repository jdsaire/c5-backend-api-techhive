# Glossary

Every term used in the [walkthroughs](README.md), in plain language. Alphabetical.

---

**API** — *Application Programming Interface.* A program that other programs talk to over the
network. It has no screen; it receives requests and sends back answers. This project is one.

**ASP.NET Core** — Microsoft's framework for building web applications and APIs in C#. It supplies
the machinery that listens for requests, matches them to code, and sends responses.

**Bearer token** — A string sent with a request to say "I am allowed to do this," in the header
`Authorization: Bearer <token>`. "Bearer" means whoever holds it can use it, like a train ticket —
it identifies no one. [The check in this project is simulated](03-Middleware-and-the-Pipeline.md#authentication).

**Body** — The data carried by a request or response, after the headers. In this API it is always
[JSON](#json). A `POST` sends the new user in the body; a `GET` response returns users in it.

**Concurrent** — Happening at the same time. An API handles many requests concurrently, which is
why shared data needs collections built to be touched by several at once.

**CRUD** — Create, Read, Update, Delete: the four operations almost any record-keeping system
needs. Mapped to `POST`, `GET`, `PUT`, `DELETE`.

**Deserialize** — Turn text into an object the program can use — reading incoming JSON into a
`User`. The reverse, *serialize*, turns an object into text to send back. The failure in
[bug 4](02-Validation-and-Error-Handling.md#bug-4-the-one-nobody-reported) happened during
deserialization, before any handler ran.

**Dictionary** — A collection that stores values against keys and finds one by key directly,
without scanning. This project stores users in a dictionary keyed by id.

**Dependency injection** — The framework supplying an object what it needs rather than having it
construct its own. A handler asks for the user store by naming it as a parameter, and receives it.

**Endpoint** — One combination of a [verb](#verb) and a [path](#path) the API answers —
`GET /users` is an endpoint. This API has five.

**Exception** — What happens when code hits something it cannot handle and stops. Left alone it
propagates outward until something catches it or the request fails.

**Extension method** — C# syntax for adding a method to an existing type without modifying it.
`app.MapUserEndpoints()` is one; it lets the route definitions live in their own file while
reading as though built in.

**Handler** — The code that runs for one endpoint and produces its answer.

**Header** — A line of metadata on a request or response, separate from the body. `Content-Type`
says what format the body is in; `Authorization` carries the token; `Location` points at a newly
created record.

**HTTP** — The protocol web requests use. Defines the [verbs](#verb), the [status
codes](#status-code), and the shape of requests and responses.

**In-memory** — Stored in the running program's memory rather than on disk. Fast, simple, and gone
when the program stops. [This project's storage is in-memory by design](01-Building-the-CRUD-Surface.md#where-the-records-are-kept).

**JSON** — *JavaScript Object Notation.* The text format this API uses for data:

```json
{ "id": 1, "name": "Ada Reyes" }
```

Readable by people, parseable by nearly every language.

**Log** — The running record an application writes about what it is doing. This API logs each
request's method, path, and status code, and writes full details of any exception.

**Middleware** — A component every request passes through on its way to an endpoint, and every
response passes back through on the way out. Used for work that applies to all requests: errors,
authentication, logging. See [walkthrough 3](03-Middleware-and-the-Pipeline.md).

**Minimal API** — A style of writing ASP.NET Core endpoints where a route maps directly to a
function, rather than to a method on a controller class. This project uses it.

**Model** — A class describing the shape of some data. `User` is this project's only model.

**NuGet** — .NET's package manager. This project uses three packages, all fetched automatically.

**OpenAPI** — A standard format for describing what an API offers — its endpoints, their inputs,
their outputs. This API generates one at `/openapi/v1.json`. **Swagger** is the older name for the
same standard, still widely used. [Swagger UI](#swagger-ui) reads that description.

**Path** — The part of a URL identifying what is being addressed: `/users`, `/users/2`. The path
names the thing; the [verb](#verb) says what to do to it.

**Pipeline** — The ordered series of [middleware](#middleware) components a request passes
through. Order matters because each wraps the ones after it.

**Route** — The pattern a request path is matched against. `/users/{id}` is a route where `{id}`
is a placeholder, so `/users/2` matches it with `id` set to `2`.

**Singleton** — A service the framework creates once and shares across every request. The user
store is a singleton, which is what lets records added by one request be visible to the next — and
why it must be safe for [concurrent](#concurrent) access.

**Stack trace** — The list of function calls that led to an [exception](#exception). Essential for
diagnosis, which is why it goes to the [log](#log) — and why it is not sent to the caller, since it
describes how the application is built internally.

**Status code** — The three-digit number on every HTTP response saying how it went. The ones this
API uses:

| Code | Meaning | When |
|---|---|---|
| `200 OK` | Success | A read or update worked |
| `201 Created` | Success, something new exists | A user was created |
| `204 No Content` | Success, nothing to return | A user was deleted |
| `400 Bad Request` | The request was wrong | Invalid data, or unreadable JSON |
| `401 Unauthorized` | No valid credentials | Token missing or wrong |
| `404 Not Found` | The thing does not exist | No user with that id |
| `500 Internal Server Error` | The server broke | An unhandled exception |

The first digit is the summary: **2xx** it worked, **4xx** the caller's problem, **5xx** the
server's problem.

**Swagger UI** — A web page, generated from the [OpenAPI](#openapi) description, listing every
endpoint with a form to send real requests and see real responses. This project serves it at
`http://localhost:5139/swagger` while it is running locally — it is a development tool, not a
hosted site. See [how to run it](../docs/how-to-run.md).

**try/catch** — The C# construct for handling [exceptions](#exception). Code in `try` runs; if it
throws, control jumps to `catch` instead of the failure propagating outward.

**Validation** — Checking that submitted data meets the rules before acting on it. This project
validates name, email, and department, and answers `400` listing everything that is wrong.

**Verb** — The word at the start of an HTTP request saying what kind of operation it is: `GET`
(read), `POST` (create), `PUT` (update), `DELETE` (remove).

---

[Back to learning-mode](README.md) · [Walkthrough 1](01-Building-the-CRUD-Surface.md) ·
[2](02-Validation-and-Error-Handling.md) · [3](03-Middleware-and-the-Pipeline.md)
