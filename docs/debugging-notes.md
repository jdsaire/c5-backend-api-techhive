# Activity 2 — bugs found, fixes made, and edge cases tested

TechHive Solutions reported three problems after the first version of the API went out:

1. Users were being added without proper validation.
2. Errors occurred when retrieving non-existent users.
3. The API occasionally crashed due to unhandled exceptions.

This file records what the code did before, what it does now, and the edge cases run to confirm
each fix. **Every response quoted here was captured from a running instance** — see
[api-testing.md](api-testing.md) for the Activity 1 behavior these fixes replace.

---

## Bug 1 — any user data was accepted, valid or not

**Before.** The `POST` handler passed whatever it received straight to the store. A record with an
empty name and an email address that was not an email address was stored and answered with
`201 Created`:

```json
{ "id": 5, "name": "", "email": "not-an-email", "department": "" }
```

**Fix.** [`Validation/UserValidator.cs`](../src/UserManagementAPI/Validation/UserValidator.cs)
now holds the rules, and both `POST` and `PUT` call it before touching the store. The rules are:

| Field | Rule |
|---|---|
| `Name` | Required; cannot be empty or whitespace |
| `Email` | Required; must parse as an email address whose domain contains a dot |
| `Department` | Required; cannot be empty or whitespace |

A rejected request gets `400 Bad Request` and is told which fields are wrong and why — one entry
per field, so a caller fixing a form can correct everything in one pass rather than discovering
problems one at a time.

The rules live in their own file rather than inline in the handlers so that create and update
cannot drift apart. That mattered immediately: `PUT` was accepting invalid data even after `POST`
was fixed, and a shared validator is what closed that gap.

The email check is deliberately pragmatic — it parses the address and requires a dot in the
domain. It is not full RFC 5322 validation and it does not confirm the mailbox exists. It rejects
the mistakes this API actually receives: a bare word, a missing `@`, a domain with no dot.

### Edge cases tested

| Request body | Status | Response |
|---|---|---|
| `{"name":"","email":"a.b@techhive.example","department":"IT"}` | `400` | `Name is required and cannot be empty or whitespace.` |
| `{"name":"   ", ...}` (whitespace only) | `400` | `Name is required and cannot be empty or whitespace.` |
| `{"name":"Sam Okafor","email":"not-an-email", ...}` | `400` | `Email must be a valid email address, such as name@example.com.` |
| `{"name":"Sam Okafor","email":"sam@localhost", ...}` | `400` | `Email must be a valid email address, such as name@example.com.` |
| `{}` (no fields at all) | `400` | all three fields reported |
| `{"name":"","email":"nope","department":""}` | `400` | all three fields reported |
| Same invalid body sent to `PUT /users/2` | `400` | all three fields reported |
| A valid record | `201` | the created user |

Every row above was re-run after the later hardening pass and still behaves exactly as recorded.
A body that is absent altogether is a different case and is covered
[further down](#the-absent-body--added-after-activity-3).

The `{}` case is worth calling out. The fields are plain strings that default to empty, so a body
with nothing in it deserializes into an empty user rather than failing. Without the validator that
empty record would have been stored. With it, the caller gets:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["Name is required and cannot be empty or whitespace."],
    "Email": ["Email is required and cannot be empty or whitespace."],
    "Department": ["Department is required and cannot be empty or whitespace."]
  }
}
```

---

## Bug 2 — requests for users that do not exist reported success

**Before.** Three routes lied to the caller:

| Request | Before | What the caller was told |
|---|---|---|
| `GET /users/99` | `200 OK` with body `null` | The lookup succeeded and the user is nothing |
| `PUT /users/99` | `200 OK` with body `null` | The update succeeded |
| `DELETE /users/99` | `204 No Content` | The delete succeeded |

The `DELETE` case was the worst of the three: a successful delete and a delete of a user who was
never there returned byte-for-byte identical responses.

**Fix.** All three now answer `404 Not Found` with a message naming the id:

```json
{ "error": "No user with id 99 was found." }
```

All three share one helper, `NoSuchUser`, in
[`Endpoints/UserEndpoints.cs`](../src/UserManagementAPI/Endpoints/UserEndpoints.cs), so the
wording and the shape cannot drift between verbs.

### Edge cases tested

| Request | Status | Response |
|---|---|---|
| `GET /users/99` | `404` | `No user with id 99 was found.` |
| `PUT /users/99` | `404` | `No user with id 99 was found.` |
| `DELETE /users/99` | `404` | `No user with id 99 was found.` |
| `DELETE /users/3` then `DELETE /users/3` again | `204` then `404` | the second call is now distinguishable |
| `GET /users/-1` | `404` | `No user with id -1 was found.` |
| `GET /users/abc` | `404` | empty body — see below |

`GET /users/abc` never reaches this project's code. The route is declared as `/users/{id:int}`, so
a non-integer id matches no route at all and the framework answers `404` on its own. That was
already correct and was left alone.

### A note on error shapes

Two shapes appear in this API, and the split is deliberate:

- **`400` validation failures** use the framework's standard problem format, because they carry
  per-field detail that a single message cannot express.
- **`404`, `401`, and `500`** use a flat `{ "error": "..." }`, because each is a single statement
  about the request as a whole. This is also the shape the Activity 3 middleware returns.

The dividing line is per-field detail, not the status code. A `400` for an *absent* body is a
single statement — there are no fields to report — so it takes the flat shape too. That case was
added later and is described [below](#the-absent-body--added-after-activity-3).

---

## Bug 3 — unhandled exceptions could take down a request

**Before.** No handler had any exception guard. Anything thrown inside a handler propagated out of
the API's own code.

**Fix.** Every one of the five handlers now wraps its work in a `try`/`catch`. A caught exception
is written to the log with its full stack trace, and the caller receives:

```
HTTP/1.1 500 Internal Server Error
```
```json
{ "error": "An unexpected error occurred while processing the user request." }
```

The response deliberately carries no exception detail. The stack trace belongs in the log where
the team can read it, not in a response body where a caller can.

### How this was tested

A throw was temporarily added to the top of the `GET /users/{id}` handler:

```csharp
if (id == 500) throw new InvalidOperationException("TEMPORARY test throw.");
```

`GET /users/500` then returned `500` with the JSON above, the log recorded
`Unhandled exception caught at the endpoint layer.` followed by the stack trace, and a subsequent
`GET /users` still returned `200` — the process stayed up. The temporary line was removed before
the fix was committed; it is reproducible by pasting it back.

### Why Activity 3 adds error-handling middleware as well

The `try`/`catch` blocks only cover code **inside** a handler. Anything that fails before a
handler is reached — reading the request body, matching a route, another middleware component —
is outside their reach. Activity 3 adds error-handling middleware as an outer net for exactly
those cases. Both layers exist on purpose: the endpoint layer catches what it can and answers with
its own wording, the middleware catches everything that escapes. The two messages are worded
differently so that during testing it is obvious which layer answered.

The next section is a case the endpoint layer provably cannot reach.

---

## Bug 4 — found during this testing pass: malformed payloads leak internals

This one was not on TechHive's list. It was found by sending deliberately broken payloads.

**Sending truncated JSON:**

```bash
curl -X POST http://localhost:5139/users -H "Content-Type: application/json" -d '{"name": "Sam",'
```

**Sending a field of the wrong type:**

```bash
curl -X POST http://localhost:5139/users -H "Content-Type: application/json" \
  -d '{"name": 42, "email":"a.b@techhive.example","department":"IT"}'
```

Both return `400`, which is the right status. But the body is not JSON — it is a plain-text dump
of `Microsoft.AspNetCore.Http.BadHttpRequestException`, its inner `System.Text.Json` exceptions,
about thirty frames of stack trace, and a list of the request's own headers.

Two things are wrong with that. An API that documents JSON responses is answering with something
else, so a client parsing the response fails on the error path. And the body describes the
internals of the application to whoever sent the bad request.

**Why the `try`/`catch` blocks do not catch it.** The failure happens while the request body is
being deserialized into the `User` parameter — that is, *before* the handler is entered. There is
no handler frame on the stack yet, so no `catch` in this project's code can see it.

**Where it is fixed.** Activity 3's error-handling middleware sits outside the routing layer and
does catch it. It is recorded here because this is where it was found, and the fix and its test
results are in [middleware-pipeline.md](middleware-pipeline.md).

---

## The absent body — added after Activity 3

A later hardening pass looked at a case none of the bugs above covered: a request that carries no
body at all, or the literal JSON value `null`.

**What was expected.** That `UserValidator.TryValidate` would be handed a null record and throw,
because it reads `user.Name` without checking. The planned fix was an explicit null check in the
create and update handlers, ahead of the validator.

**What testing actually found.** No such crash existed, and the planned check would never have
run. Because the handlers declared a non-nullable `User` parameter, the framework rejected the
request while binding that parameter — before the handler was entered — and the log recorded:

```
Microsoft.AspNetCore.Http.BadHttpRequestException: Implicit body inferred for parameter "user"
but no body was provided. Did you mean to use a Service instead?
```

The error-handling middleware caught it and answered `400` with the same
`The request could not be read...` message that truncated JSON produces. Safe, but misleading: a
body that was never sent is not a body that could not be parsed, and a caller debugging their
client is told to check their JSON syntax when there is no JSON to check.

**What changed.** The body parameter on create and update is now declared nullable, so a missing
or `null` body reaches the handler instead of failing during binding, and an explicit check
answers it directly:

| Request | Status | Response |
|---|---|---|
| `POST /users` with `-d 'null'` | `400` | `{"error":"A user record is required in the request body."}` |
| `POST /users` with an empty body | `400` | `{"error":"A user record is required in the request body."}` |
| `PUT /users/2` with `-d 'null'` | `400` | `{"error":"A user record is required in the request body."}` |
| `PUT /users/2` with an empty body | `400` | `{"error":"A user record is required in the request body."}` |
| `POST /users` with truncated JSON | `400` | `{"error":"The request could not be read..."}` — unchanged |
| `POST /users` with a wrong field type | `400` | `{"error":"The request could not be read..."}` — unchanged |
| `POST /users` with a valid record | `201` | the created user — unchanged |

The two problems now read differently, which is the whole benefit. Nothing about the malformed-body
path changed.

**Worth keeping in mind.** Minimal API has no equivalent of the automatic model-state validation
that `[ApiController]` runs in a controller-based project. Where that convenience exists, a null
body is checked for you; here, every check on a submitted body is one this project writes. The
idea for this check came from reading a controller-based solution to the same assignment — see
[references.md](references.md).

---

## Performance: the `GET /users` and lookup path

**Before.** The store held a `List<User>`. Every lookup, update, and delete walked the list from
the beginning comparing ids until it found a match — `_users.FirstOrDefault(user => user.Id == id)`,
repeated in three methods.

**After.** The store holds a `ConcurrentDictionary<int, User>` keyed by id. Every route that
addresses a single user does so by id, so the collection is now keyed the same way the API is
addressed. A lookup goes straight to the record instead of scanning.

**What this is honestly worth.** With three seeded users, this change is not measurable. Nobody
will perceive a scan of three items. The reasons to make it anyway are structural:

- The cost of a single-user lookup no longer depends on how many users are stored. At three
  records that is irrelevant; the point is that it stays irrelevant as the number grows.
- The store is registered as a singleton, so every request shares one instance. A plain `List<T>`
  is not safe for two requests mutating it at once, and this API had no protection against that
  at all. A concurrent dictionary is, and id assignment now uses an atomic increment.

The second reason is the one that actually mattered here. It was a correctness problem under
concurrent load, not a speed problem, and it was found while looking at the lookup code.

**One cost, stated plainly.** A dictionary has no guaranteed enumeration order, so `GET /users`
now sorts by id before returning. That makes listing slightly more work than reading a list would
be, and it is what keeps the response order stable. Three consecutive calls were checked and each
returned ids in the order `[1, 2, 3]`.

**External behavior is unchanged.** All five operations were re-run after the change and matched
their documented responses exactly: `200` for both reads, `201` with a `Location` header for
create, `200` for update, `204` for delete, and `404` for every missing-id case.

---

## How this code was debugged

An AI coding assistant was used throughout this pass. What it was good for:

- **Reading the endpoint file and naming suspects.** Asked what could go wrong in the handlers, it
  pointed at the unguarded `null` returns from the store and at the repeated
  `FirstOrDefault` scans — both of which turned out to be real, and both of which are fixed above.
- **Generating the edge-case list.** The whitespace-only name, the domain with no dot, the empty
  `{}` body, and the wrong-type field are all cases it proposed. The `{}` case was the useful one:
  it exposed that the fields default to empty strings and so deserialize into a silently empty
  record.
- **Boilerplate.** The validator's shape and the `ConcurrentDictionary` conversion were both
  drafted faster than they would have been written by hand.

What it did not do:

- **It did not find bug 4.** The stack-trace leak turned up by sending a broken payload at a
  running instance, not by reading the code. Nothing suggested that failures before the handler
  are unreachable from a `catch` inside it — that came from reading the stack trace in the
  response.
- **It did not judge the performance change.** Asked to optimize, it produced the dictionary
  conversion and described it as a performance improvement. At three records it is not one. It
  also initially dropped the ordering guarantee, which would have changed what `GET /users`
  returns; the sort was added after re-running the tests and noticing.

Every fix above was confirmed against a running instance before being committed.

---

## Related

- [README.md](README.md) — index of this folder
- [api-testing.md](api-testing.md) — the endpoint surface and Activity 1 evidence
- [middleware-pipeline.md](middleware-pipeline.md) — Activity 3, including the fix for bug 4
- [how-to-run.md](how-to-run.md) — starting the API locally
