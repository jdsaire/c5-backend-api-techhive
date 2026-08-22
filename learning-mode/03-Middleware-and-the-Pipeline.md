# 3. Middleware and the pipeline

*What middleware is, the three components added here, and why their order matters.*

Terms in **bold** are defined in the [Glossary](Glossary.md).

---

## Three requirements that are not about users

TechHive's corporate policy asks for three things from this API:

- log every request and response, for auditing
- handle errors the same way everywhere
- require a token before anything can be accessed

None of these belong to any particular endpoint. Every request needs all three. Writing them into
each of the five handlers would mean five copies of the same code, five places to update, and five
chances for one to drift out of step.

What is needed is a way to say *"do this to every request, regardless of where it is going."* That
is **middleware**.

## The pipeline

A request does not arrive at a route handler directly. It passes through a series of components
first, and the response travels back out through the same components in reverse. That sequence is
the **pipeline**.

```
   request
      ↓
 ┌──────────────────────────────────┐
 │  error handling                  │
 │  ┌────────────────────────────┐  │
 │  │  authentication            │  │
 │  │  ┌──────────────────────┐  │  │
 │  │  │  logging             │  │  │
 │  │  │  ┌────────────────┐  │  │  │
 │  │  │  │  the endpoint  │  │  │  │
 │  │  │  └────────────────┘  │  │  │
 │  │  └──────────────────────┘  │  │
 │  └────────────────────────────┘  │
 └──────────────────────────────────┘
      ↓
   response
```

Each component gets the request, can do whatever it likes with it, and then chooses whether to
**pass it inward**. That choice is the important one. A component that passes the request on is a
step along the way. A component that answers instead — as authentication does when the token is
wrong — stops it, and nothing further in ever sees that request.

Nesting is the accurate mental picture. Each component *wraps* the ones after it. That is why
order is not a matter of taste.

## The three components

### Error handling

Wraps everything else in a `try`/`catch`. If anything inside throws — another middleware
component, the routing machinery, an endpoint — this catches it, writes it to the log, and returns
a consistent JSON error rather than whatever form the failure would otherwise have taken.

```json
{ "error": "Internal server error." }
```

It distinguishes one case. When the request itself could not be read — the truncated JSON from
[bug 4](02-Validation-and-Error-Handling.md#bug-4-the-one-nobody-reported) — that is the caller's
mistake, not a server fault, so it keeps its `400` instead of being converted to `500`. Reporting
a server error for a caller's malformed input would send them looking in the wrong place.

**This is the fix for bug 4.** The exception that no `try`/`catch` inside a handler could reach is
thrown *inside* this component, because this component wraps the entire routing layer. The same
broken request that used to return thirty lines of stack trace now returns one line of JSON.

That is worth pausing on, because it is the whole argument for middleware. The problem was not
that the handlers were careless. It was that the failure happened somewhere no handler could see —
and the only fix is something positioned further out.

### Authentication

Requires an `Authorization: Bearer <token>` header, compares the token to a configured value, and
answers `401 Unauthorized` if it is missing or wrong. The comparison is done in a way that takes
the same amount of time whether the supplied token is nearly right or nothing like it —
[chapter 04](04-Learning-From-a-Peer-Review.md) explains why that matters and how it works.

> ### This check is simulated, and that matters
>
> It compares one fixed string to another. It verifies **no cryptographic signature**, so nothing
> proves who issued the token. It **issues no tokens** — there is no login. It has **no expiry**.
> It **identifies no user** — a valid token opens everything and does not say who is calling. And
> the token sits in a configuration file committed to a public repository, so it is **not a
> secret**.
>
> It is here to show *where* authentication belongs in a pipeline and *what a rejected request
> looks like*. It does not make this API secure, and this project does not claim otherwise.
> Replacing it with real authentication means replacing it, not extending it.
>
> Saying this plainly is not a disclaimer bolted on afterwards. A simulated control described as a
> real one is worse than no control, because someone downstream will rely on it.

Two behaviours worth noting.

**It fails closed.** If no token is configured at all, it rejects everything and logs why. The
alternative — treating "no token configured" as "no checking needed" — is how security controls
quietly stop working after a deployment mistake. When a control cannot do its job, refusing is the
safe direction.

**The documentation pages are exempt.** `/swagger` and `/openapi` are served without a token,
because a browser loading Swagger UI cannot attach an Authorization header to its own page and
asset requests — guarding them would make the documentation unreachable. Every `/users` route is
guarded. This is a deliberate trade for a development tool, and it is one more reason this is not
production authentication.

### Logging

Writes one line per request: the method, the path, the status code, and how long it took.

```
GET /users responded 200 in 3 ms
GET /users/99 responded 404 in 1 ms
```

## Why this order

The requirements specify: **error handling first, authentication second, logging last.**

**Error handling outermost** follows from what a `try`/`catch` can do: it catches what happens
inside it. Registered first, everything else is inside it. Registered second, whatever ran first
would be unprotected — and a failure there would produce exactly the kind of raw error the
component exists to prevent.

**Authentication before logging** means a request with a bad token is stopped immediately, without
further work being done on it.

**Logging innermost** means the status code it records is the one the endpoint actually produced,
rather than one that might still be changed by something further out.

## Two consequences of that order

Ordering decisions have effects, and the honest thing is to find them and write them down rather
than discover them later.

### Rejected requests produce no log line

Logging sits *after* authentication. A request rejected with `401` never reaches it. So `401`s
produce no `responded` line.

This showed up immediately in testing: three `401` responses were returned, and none appeared in
the logging middleware's output.

Nothing goes unrecorded — the authentication component logs its own rejections, which is precisely
why it does so. But the two kinds of entry look different, and anyone building an audit report
from these logs has to know that, or they will count rejected requests as never having happened.

Putting logging outermost would give one uniform line for every request including rejections. The
requirements specify this order, so this is what is implemented, and the consequence is documented
rather than quietly corrected.

### A bug this ordering caused

The logging component originally recorded the status code on the way out, whatever it happened to
be. Testing produced this line:

```
POST /users responded 200 in 3 ms
```

for a request that actually received `400`.

The cause is the nesting. Logging is *inside* error handling. When an exception escapes an
endpoint, it travels outward through logging **before** reaching the error handler that sets the
status code. At that moment the response still carried its default of `200`, and the logging
component faithfully recorded a number that was never sent to anyone.

An audit log that reports the wrong status code is worse than one that reports nothing, because
nothing about it looks wrong. The fix was to stop guessing: when an exception passes through,
logging now records that the request *failed*, names the exception type, and passes it on.

```
POST /users failed after 4 ms with BadHttpRequestException; the error-handling middleware produces the response
```

That line is true. The previous one was not.

The general lesson: **a component can only report what it can actually see.** When it cannot see
something yet, the fix is to say less, not to guess.

## Two layers of exception handling

The endpoints have their own `try`/`catch` blocks, from
[walkthrough 2](02-Validation-and-Error-Handling.md). The error-handling middleware is a second
layer. Both exist deliberately:

| Where the failure happens | What catches it |
|---|---|
| Inside a route handler | That handler's own `try`/`catch` |
| Before a handler is entered — reading the body, matching a route, another middleware component | The error-handling middleware |

The endpoint layer catches what it can and produces a specific message. The middleware is the net
under everything, including the parts no handler can reach. The two messages are worded
differently so that during testing it is obvious which layer answered.

Two overlapping safety nets are not redundancy here — they cover genuinely different ground.

## See it for yourself

With the API running (see [../docs/how-to-run.md](../docs/how-to-run.md)), watch the terminal
where it is running while you send these:

```bash
# No token — rejected. Notice: nothing appears in the log from the logging component.
curl -i http://localhost:5139/users

# Wrong token — same.
curl -i -H "Authorization: Bearer wrong" http://localhost:5139/users

# Correct token — works, and a "responded 200" line appears.
curl -H "Authorization: Bearer techhive-local-development-token" http://localhost:5139/users

# Broken JSON — one clean line of JSON back, not a stack trace.
curl -H "Authorization: Bearer techhive-local-development-token" \
     -H "Content-Type: application/json" \
     -X POST http://localhost:5139/users -d '{"name": "Sam",'
```

Watching the console while sending those four is the fastest way to make the pipeline concrete.
The first two produce rejection warnings and no `responded` line; the third produces one; the
fourth shows both the logging component and the error handler reporting the same failure from
their different positions.

The full test results are in [../docs/middleware-pipeline.md](../docs/middleware-pipeline.md).

---

**Previous:** [2. Validation and error handling](02-Validation-and-Error-Handling.md) ·
**Next:** [4. Learning from a peer review](04-Learning-From-a-Peer-Review.md) ·
[Glossary](Glossary.md) · [Back to learning-mode](README.md)
