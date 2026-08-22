# Activity 3 — the middleware pipeline

TechHive Solutions asked for three things from this API, to comply with corporate policy:

- log all incoming requests and outgoing responses for auditing,
- enforce standardized error handling across all endpoints,
- secure the API's endpoints using token-based authentication.

This file describes the three middleware components that answer those requirements, the order
they run in, and the results of testing them. **Every response and log line quoted here was
captured from a running instance.**

> **The `localhost` links in this file are not public URLs.** This API is not hosted anywhere.
> `localhost` means "this computer", so those links do nothing until you start the API on your own
> machine. Two commands do it:
>
> ```bash
> git clone https://github.com/jdsaire/c5-backend-api-techhive.git
> cd c5-backend-api-techhive
> dotnet run --project src/UserManagementAPI --launch-profile http
> ```
>
> Then <http://localhost:5139/swagger> opens the interactive Swagger UI, where you can send every
> request below yourself and compare what you get against what is recorded here. Full instructions
> are in [how-to-run.md](how-to-run.md).

> **The token check in this API is simulated.** It compares a bearer token against a value in a
> configuration file. The comparison is constant-time, which closes one specific weakness and
> nothing more: the check still verifies no signature, issues no tokens, and has no expiry. It is
> not production authentication and this project does not claim the API is secure. The full statement
> of what it does and does not do is at the top of
> [`TokenAuthenticationMiddleware.cs`](../src/UserManagementAPI/Middleware/TokenAuthenticationMiddleware.cs).

---

## What middleware is, in one paragraph

A request does not arrive directly at a route handler. It passes through a series of components,
each of which can inspect it, act on it, pass it along, or stop it. The response travels back out
through the same components in reverse. Because each component wraps the ones registered after it,
the order they are registered in decides what each one can see.

---

## The three components

### 1. Error handling — [`ErrorHandlingMiddleware.cs`](../src/UserManagementAPI/Middleware/ErrorHandlingMiddleware.cs)

Wraps the rest of the pipeline in a `try`/`catch` and converts anything that escapes into a
consistent JSON response.

| Situation | Status | Body |
|---|---|---|
| Any unhandled exception | `500` | `{ "error": "Internal server error." }` |
| An unreadable request (`BadHttpRequestException`) | its own code, normally `400` | `{ "error": "The request could not be read. Check that the body is valid JSON and that each field has the expected type." }` |

The two cases are separated on purpose. An unreadable body is the caller's mistake, not a server
fault, so it keeps its `400` and is logged as a warning. Blanket-converting it to `500` would tell
the caller the server broke when it did not.

A body that is *unreadable* is not the same as a body that is *absent*. Truncated JSON and fields
of the wrong type still reach this middleware. A missing or literal-`null` body no longer does —
the create and update handlers check for it themselves and answer with a message about the body
being absent. See [debugging-notes.md](debugging-notes.md#the-absent-body--added-after-activity-3).

No exception detail is ever sent to the caller. The stack trace goes to the log.

### 2. Authentication — [`TokenAuthenticationMiddleware.cs`](../src/UserManagementAPI/Middleware/TokenAuthenticationMiddleware.cs)

Requires an `Authorization: Bearer <token>` header whose token matches the value configured at
`Authentication:Token` in [`appsettings.json`](../src/UserManagementAPI/appsettings.json). Anything
else is answered `401 Unauthorized` with a `WWW-Authenticate: Bearer` header.

Three behaviors worth stating:

- **The comparison is constant-time.** Both the supplied and the configured token are hashed with
  SHA-256 and the two digests are compared with `CryptographicOperations.FixedTimeEquals`. An
  ordinary string comparison stops at the first differing character, so how long it takes
  correlates with how many leading characters were correct — information a caller can measure even
  though the API never tells them how close they were. Hashing first also keeps the configured
  token's *length* out of the timing, because digests are always 32 bytes. Why this is done this
  way, and where the idea came from, is in [references.md](references.md).

- **It fails closed.** If no token is configured at all, every request is rejected and the reason
  is logged. A missing configuration value must never mean "let everything through."
- **The documentation paths are exempt.** `/openapi` and `/swagger` are served without a token.
  Swagger UI is a page a browser loads directly and cannot attach an Authorization header to its
  own asset requests, so guarding those paths would make the documentation unreachable. Every
  `/users` route is guarded. This is a deliberate trade for a development tool, and it is one more
  reason this check is not production authentication.

### 3. Logging — [`RequestLoggingMiddleware.cs`](../src/UserManagementAPI/Middleware/RequestLoggingMiddleware.cs)

Writes one line per request recording the HTTP method, the request path, the response status code,
and how long it took:

```
GET /users responded 200 in 3 ms
GET /users/99 responded 404 in 1 ms
```

---

## The configured order

The assignment specifies this order, and it is what
[`Program.cs`](../src/UserManagementAPI/Program.cs) registers:

```
1. Error handling   (outermost)
2. Authentication
3. Logging          (innermost)
   ↓
   routing → the /users endpoints
```

A request travels down that list; the response travels back up it.

**Why error handling is outermost.** It can only catch what happens inside it. Registered first,
everything below it — both other middleware components and every endpoint — is inside its `try`
block. Registered anywhere else, whatever came before it would be unprotected.

**Why authentication comes before logging.** A request with a bad token is rejected immediately,
without any further work being done on it.

### One consequence of this order, stated plainly

Because logging is registered *after* authentication, a request that authentication rejects never
reaches the logging middleware. Rejected requests therefore produce no `responded` line.

This is visible in the test output below: three `401` responses were returned, and none of them
produced a `responded 401` line. The authentication middleware logs its own rejections precisely
because of this, so nothing goes unrecorded — but the two kinds of entry look different, and
anyone reading the log or building an audit report from it needs to know that.

Putting logging outermost instead would give one uniform line for every request including
rejections. The assignment specifies this order, so this is what is implemented; the observation
is recorded rather than acted on.

### A defect this ordering caused, and how it was fixed

The logging middleware originally wrote its line from a `finally` block, reading
`Response.StatusCode` on the way out. Testing showed it logging:

```
POST /users responded 200 in 3 ms
```

for a request that actually received `400`. The cause is the ordering: logging sits *inside* error
handling, so when an exception escapes, it passes through the logging middleware **before** the
error handler has set the status code. At that instant the response still carried its default of
`200`, and the logging middleware faithfully recorded a number that was never sent.

A log that reports the wrong status code is worse than one that reports nothing, especially for an
audit trail. The fix was to stop guessing: the logging middleware now catches the exception, logs
that the request failed and which exception type escaped, and rethrows it for the error handler.

```
POST /users failed after 4 ms with BadHttpRequestException; the error-handling middleware produces the response
```

That line is true. The previous one was not.

---

## Two layers of exception handling, and why both exist

The endpoints have their own `try`/`catch` blocks, added in Activity 2. The error-handling
middleware is a second layer. Both are needed, and the test results show why:

| Where the failure happens | Caught by | Response |
|---|---|---|
| Inside a route handler | The handler's own `try`/`catch` | `500` — `An unexpected error occurred while processing the user request.` |
| Before a handler is entered — e.g. deserializing a malformed body | The error-handling middleware | `400` — `The request could not be read...` |
| A missing or `null` body on create or update | The handler's own null check | `400` — `A user record is required in the request body.` |

The second row is the case the endpoint layer provably cannot reach. Deserializing the request
body into the handler's `User` parameter happens before the handler is entered, so there is no
handler frame on the stack and no `catch` inside one can see it. Before the middleware existed,
that case returned a plain-text dump of the exception and about thirty stack frames to the caller
— recorded as bug 4 in [debugging-notes.md](debugging-notes.md). It now returns clean JSON.

The two messages are worded differently so that, while testing, it is obvious which layer
answered.

---

## Test results

Run against `http://localhost:5139` with the configured token
`techhive-local-development-token`.

### Authentication

| Request | Status | Response |
|---|---|---|
| `GET /users` with no `Authorization` header | **401** | `{"error":"An Authorization header with a bearer token is required."}` |
| `GET /users` with `Authorization: Basic abc123` | **401** | `{"error":"The Authorization header must use the Bearer scheme."}` |
| `GET /users` with `Authorization: Bearer wrong-token` | **401** | `{"error":"The supplied token is not valid."}` |
| `GET /users` with a wrong token of the **same length** as the configured one | **401** | `{"error":"The supplied token is not valid."}` |
| `GET /users` with the correct bearer token | **200** | the user list |

All four rejections included the header `WWW-Authenticate: Bearer`.

The last two rejections are the point of the constant-time comparison. A wrong token that matches
the configured value's length, and one that does not, produce the same status, the same body, and
the same header — and now also take the same time to reject. The response never said how close an
attempt was; previously the duration did.

### Documentation paths, sent with no token

| Request | Status | Result |
|---|---|---|
| `GET /openapi/v1.json` | **200** | the OpenAPI document |
| `GET /swagger/index.html` | **200** | the Swagger UI page |

Exempt as designed, so the documentation stays readable in a browser.

### Error handling

| Request | Status | Response |
|---|---|---|
| `GET /users/500` — a throw temporarily added inside the handler | **500** | `{"error":"An unexpected error occurred while processing the user request."}` |
| `POST /users` with truncated JSON `{"name": "Sam",` | **400** | `{"error":"The request could not be read..."}` |
| `POST /users` with `{"name": 42, ...}` — wrong field type | **400** | `{"error":"The request could not be read..."}` |
| `POST /users` with a literal `null` body | **400** | `{"error":"A user record is required in the request body."}` |
| `POST /users` with an empty body | **400** | `{"error":"A user record is required in the request body."}` |
| `PUT /users/2` with a literal `null` or empty body | **400** | `{"error":"A user record is required in the request body."}` |
| `GET /users` immediately afterwards | **200** | the user list — the process stayed up |

The two `could not be read` rows are answered by this middleware. The absent-body rows are
answered by the handlers themselves, which is why the wording differs.

The exception was triggered by temporarily adding this line to the top of the `GET /users/{id}`
handler:

```csharp
if (id == 500) throw new InvalidOperationException("TEMPORARY test throw.");
```

It was removed before this documentation was committed. Paste it back to reproduce the test.

### Log accuracy

These are the lines the run above produced, in order:

```
GET /openapi/v1.json responded 200 in 67 ms
Rejected GET /users: no Authorization header was sent.
Rejected GET /users: the Authorization header did not use the Bearer scheme.
Rejected GET /users: the supplied token did not match the configured value.
GET /users responded 200 in 3 ms
GET /openapi/v1.json responded 200 in 1 ms
GET /swagger/index.html responded 200 in 19 ms
Unhandled exception caught at the endpoint layer.
GET /users/500 responded 500 in 4 ms
POST /users failed after 4 ms with BadHttpRequestException; the error-handling middleware produces the response
Rejected a malformed request to POST /users.
POST /users failed after 0 ms with BadHttpRequestException; the error-handling middleware produces the response
Rejected a malformed request to POST /users.
GET /users responded 200 in 0 ms
```

Reading them against what was actually sent:

- Every method, path, and status code recorded matches the response the caller received.
- The three `401` rejections appear as `Rejected …` warnings from the authentication middleware
  and produce no `responded` line — the ordering consequence described above, visible in practice.
- `GET /users/500` shows both layers doing their jobs: the endpoint's own catch logged
  `Unhandled exception caught at the endpoint layer.`, and because the handler converted the
  exception into a normal `500` response before it left, the logging middleware saw a real status
  code and recorded it.
- The two malformed `POST` requests show the other path: logging reports the failure without
  claiming a status, and the error-handling middleware reports what it rejected.

No line in that output states something that did not happen.

---

## Sending an authenticated request

```bash
curl -H "Authorization: Bearer techhive-local-development-token" http://localhost:5139/users
```

In Swagger UI the endpoints are reachable at <http://localhost:5139/swagger>, but requests sent
from that page do not carry the header, so `/users` calls made through **Try it out** return
`401`. Use `curl`, the
[`.http` file](../src/UserManagementAPI/UserManagementAPI.http), or Postman to exercise the
endpoints with a token. [how-to-run.md](how-to-run.md) covers this.

---

## Related

- [README.md](README.md) — index of this folder
- [api-testing.md](api-testing.md) — the endpoint surface and its testing evidence
- [debugging-notes.md](debugging-notes.md) — the Activity 2 fixes, including bug 4
- [how-to-run.md](how-to-run.md) — starting the API and sending an authenticated request
