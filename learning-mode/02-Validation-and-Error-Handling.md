# 2. Validation and error handling

*Why the first version was wrong in ways that looked right, and what fixing it involved.*

Terms in **bold** are defined in the [Glossary](Glossary.md).

---

## Bugs that do not crash

The first version of this API worked. All five endpoints did what they were supposed to. Nothing
threw an error, nothing fell over, and every test of the expected cases passed.

It was also wrong in four ways, and TechHive reported three of them after using it.

This is the more common kind of bug. A crash is obvious — something stops, somebody notices, you
go and look. A wrong answer delivered confidently just gets believed, and the problem surfaces
somewhere else entirely, much later, as a mystery.

All four bugs below share a shape: **the API said "that worked" when it had not.**

## Bug 1: it accepted anything

Sending this:

```json
{ "name": "", "email": "not-an-email", "department": "" }
```

got back `201 Created`. A person with no name, no department, and an email address that is not an
email address was now in the company directory.

Nothing had gone wrong from the code's point of view. It was asked to store a record, and it did.
The code had no opinion about what makes a user record valid, because nobody had given it one.

**The fix** is a **validator**: code that runs before anything is stored and checks the record
against rules.

| Field | Rule |
|---|---|
| Name | Required, and not just spaces |
| Email | Required, and must look like an email address |
| Department | Required, and not just spaces |

A record that fails gets `400 Bad Request` — the status code meaning "the problem is with what you
sent me" — and a body naming every field that is wrong:

```json
{
  "errors": {
    "Name": ["Name is required and cannot be empty or whitespace."],
    "Email": ["Email must be a valid email address, such as name@example.com."]
  }
}
```

Two decisions inside that are worth explaining.

**Every problem at once, not the first one.** It would be simpler to stop at the first failure.
But then a caller with three bad fields fixes one, resubmits, is told about the second, fixes it,
resubmits… Reporting everything at once means one round trip.

**The rules live in their own file**, not inside the two endpoints that use them. Create and update
must apply *identical* rules — a record you cannot create should not be a record you can update
into existence. Two copies of the same rules drift apart. This is not hypothetical: while fixing
this, `POST` was corrected first, and `PUT` went on accepting invalid data until it was pointed at
the same validator.

### How much validation is enough

The email check parses the address and requires a dot in the domain. It rejects `not-an-email`,
`sam@localhost`, and anything with spaces.

It is **not** exhaustive. Fully validating an email address against the specification is famously
difficult, and even a perfect check cannot tell you the mailbox exists — the only way to know that
is to send mail to it.

So the check is deliberately pragmatic: catch real mistakes, do not pretend to a certainty that is
not available. Being clear about that limit in the documentation is part of the fix, not an excuse
for it.

## Bug 2: it lied about things that were not there

Three requests for a user who did not exist:

| Request | Answer | What that means |
|---|---|---|
| `GET /users/99` | `200 OK`, body `null` | "Here is user 99. It is nothing." |
| `PUT /users/99` | `200 OK`, body `null` | "Updated." (Nothing was updated.) |
| `DELETE /users/99` | `204 No Content` | "Deleted." (Nothing was deleted.) |

The `DELETE` case is the worst. Deleting a real user and deleting a user who never existed
returned **byte-for-byte identical responses**. Nothing the caller could inspect distinguished
them.

Think about what a caller builds on that. A script deleting departed employees reports complete
success whether it worked or silently matched nothing at all.

**The fix** is `404 Not Found` on all three, with a message that says which id:

```json
{ "error": "No user with id 99 was found." }
```

`404` is the same code a browser gets for a page that does not exist, and it means exactly that
here.

All three routes share one helper function that builds this response, for the same reason the
validation rules are shared: three separately-written 404 messages become three slightly different
404 messages.

### The status code is the answer

The general point behind bugs 1 and 2: **the status code is not decoration, it is the part of the
answer other software reads.** A caller does not read your error message — it checks whether the
number starts with a 2, a 4, or a 5, and branches. Returning `200` for a failure means every caller
takes the success path with nonsense in hand.

Roughly:

- **2xx** — it worked
- **4xx** — the request was wrong, and the caller can fix it
- **5xx** — the server broke, and the caller can only try again

## Bug 3: unhandled exceptions

Sometimes code genuinely fails — an **exception**. Something unexpected happened and the code
cannot continue.

The first version had no handling for this at all. An exception inside a handler propagated out of
the project's code, and what the caller received depended on the framework's defaults.

**The fix** is a `try`/`catch` in each of the five handlers. If anything inside throws, the API:

1. writes the full exception, with its **stack trace**, to the **log**, and
2. answers the caller with `500 Internal Server Error` and a short message.

Note which piece of information goes where. The stack trace goes to the log, where the team can
read it. The caller gets one sentence saying the request failed. It is tempting to return the
exception to the caller — it makes debugging easy — but a stack trace describes how the
application is built internally, and that is not something to hand to whoever asked.

### Testing something that is not supposed to happen

An exception guard is difficult to test, because by definition nothing normally triggers it. So one
was triggered on purpose: a line was temporarily added to a handler making it throw on a specific
id. The API returned `500` with the expected JSON, the log recorded the exception, and — the part
worth checking — the *next* request still worked. One failed request had not damaged the running
application. Then the temporary line was removed.

That technique is worth remembering. Deliberately break something, confirm the safety net catches
it, put it back.

## Bug 4: the one nobody reported

This one was found by sending deliberately broken input. Not *invalid* data — data that was not
valid JSON at all:

```
{"name": "Sam",
```

The response was `400`, which is correct. But the body was not JSON. It was about thirty lines of
plain text: an exception type, its inner exceptions, a stack trace naming internal framework
classes, and a copy of the request's own headers.

Two things wrong with that. An API that documents JSON responses answered with something else, so
a caller parsing the response fails on the error path — exactly when it most needs to work. And the
response described the application's internals to whoever sent the broken request.

**Why the `try`/`catch` blocks did not catch it.** This is the interesting part, and it is worth
understanding rather than memorising.

The failure happens while the request body is being turned into a `User` object — which happens
**before** the handler runs. The handler is not on the stack yet. A `catch` inside a handler can
only catch things thrown while that handler is executing, and this was thrown while preparing to
call it.

No amount of care inside the handlers can fix this, because the problem is outside them. Fixing it
needs something that wraps the handlers from the outside — which is **middleware**, and
[the next walkthrough](03-Middleware-and-the-Pipeline.md).

## The performance change, honestly

The reported issues mentioned performance in the `GET /users` endpoint, so the storage code was
looked at.

The original store kept users in a **list**, and finding one by id meant walking the list from the
beginning comparing ids until it matched. The store was changed to a **dictionary** keyed by id,
where looking one up goes straight to it.

Here is the honest part. **With three users, this changes nothing measurable.** Nobody perceives
the difference between checking three items and going straight to one. Claiming a speed improvement
would be a claim nobody could verify.

The change was worth making for a different reason, found while looking at that code. The store is
shared by every request simultaneously, and the original list had **no protection against two
requests changing it at the same time** — which can corrupt it outright, not merely produce a stale
read. The replacement is a collection built for concurrent access, and id assignment now uses an
operation that cannot hand the same id to two requests.

So: a change made for performance turned out to matter for correctness. That happens, and it is
worth reporting as what it was rather than as what it was labelled.

It also carried a cost worth stating. A dictionary has no guaranteed order, so `GET /users` now
sorts by id before answering — slightly more work than reading a list, and what keeps the response
order stable. That cost was noticed by re-running the tests, not by reasoning about it.

## What actually found these bugs

Worth being precise about, because it is the transferable part.

Bugs 1, 2, and 3 were reported by TechHive — users hit them in practice.

Bug 4 and the concurrency problem were found by **sending requests to a running instance and
reading the answers carefully**, including requests designed to fail. Not by reading the code.
Reading the code tells you what it was written to do; running it tells you what it does.

The most useful habit here: for every endpoint, ask *what happens if I send this the wrong thing?*
— then send the wrong thing and look.

---

**Previous:** [1. Building the CRUD surface](01-Building-the-CRUD-Surface.md) ·
**Next:** [3. Middleware and the pipeline](03-Middleware-and-the-Pipeline.md) ·
[Glossary](Glossary.md) · [Back to learning-mode](README.md)
