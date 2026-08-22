# References — what this project learned from elsewhere

This project was built independently, but not in isolation. One external source changed the code,
and this file records exactly what it changed, so the borrowing is visible rather than quietly
absorbed.

> **The two changes credited here are ideas, not code.** Nothing in this repository was copied
> from the source below. Every line was written for this project's own structure, and one of the
> two ideas is deliberately implemented differently — the difference, and the reason for it, is
> spelled out below.

---

## The source

**[github.com/mzubanovic/UserManagementAPI](https://github.com/mzubanovic/UserManagementAPI)** —
another student's submission of the same assignment, by **mzubanovic**.

Two solutions built to one brief are unusually useful to compare. The requirements are identical,
so every difference between them is a decision someone made rather than a difference in the
problem. Reading a second solution surfaces choices your own version made silently — the places
where you did something a particular way without ever registering that there was a way to choose.
Reading your own code again cannot do that, because your own assumptions are invisible from
inside them.

That submission is a working implementation of the same brief, built controller-first where this
one is built on Minimal API. Both approaches are legitimate; the structural difference is what
made the second of the two findings below visible at all.

---

## Idea 1 — compare the bearer token in constant time

**What was observed.** That project's authentication middleware compares the supplied bearer token
against the configured one using `CryptographicOperations.FixedTimeEquals` rather than string
equality.

**Why it matters here.** This project was comparing with
`string.Equals(suppliedToken, expectedToken, StringComparison.Ordinal)`. An ordinal string
comparison stops at the first character that differs, so a token sharing more leading characters
with the configured value takes measurably longer to reject than one that differs immediately.
That timing difference is information, and it leaks even though the API never tells a caller how
close their attempt was. Given enough attempts, it can be used to recover the token one character
at a time.

**Where this implementation deliberately differs.** The reference passes the raw UTF-8 bytes of
each token to `FixedTimeEquals`. That fixes the character-by-character leak, and it is the right
core idea. But `FixedTimeEquals` returns immediately when its two inputs have different lengths —
that check happens before any constant-time comparison begins. Comparing raw token bytes therefore
still discloses how long the configured token is.

This project hashes both values with SHA-256 first and compares the two digests. SHA-256 digests
are always exactly 32 bytes, so the length check can never fail early and neither the content nor
the length of the configured token can affect how long the comparison takes. It is a small step
past the reference's approach, taken for that specific reason. The implementation is in
[`TokenAuthenticationMiddleware.cs`](../src/UserManagementAPI/Middleware/TokenAuthenticationMiddleware.cs).

> **This does not make the token check secure.** It closes one weakness in one comparison. The
> check still verifies no signature, issues no tokens, has no expiry, identifies no user, and
> reads its token from a file committed to this repository in plain text. It remains a
> **simulated** check, and the full statement of what it does not do is at the top of the
> middleware file.

## Idea 2 — check the request body for null explicitly

**What was observed.** That project's users controller checks the bound body parameter for null on
create and update, and answers `400` when it is null, in addition to the model validation the
framework runs for it.

**Why it matters here.** In a controller decorated with `[ApiController]`, ASP.NET Core runs
automatic model-state validation before the action method is entered. Minimal API has no
equivalent. This project's `UserValidator` was therefore the only thing inspecting a submitted
body — and it dereferences the record it is given, so it depends on that record existing.

**What testing actually found.** The behavior was already safe, but for a reason that had not been
noticed. Because the handlers declared a non-nullable `User` parameter, the framework rejected a
missing or `null` body during parameter binding, before the handler ran, raising
`BadHttpRequestException`. The error-handling middleware caught it and answered `400`. A null check
placed before the validator would never have executed.

So the fix was not the one originally planned. The body parameter is now declared nullable, which
lets a missing or `null` body reach the handler, where an explicit check answers `400` with a
message about the body being absent — rather than the generic "could not be read" message that a
truncated or malformed body produces. Two different problems now read differently. The
implementation is in [`UserEndpoints.cs`](../src/UserManagementAPI/Endpoints/UserEndpoints.cs).

The idea still came from the reference. What changed is where the gap actually was.

---

## What did not come from the reference

Stated plainly, because the credit above is only meaningful if its limits are too:

- **No code was copied.** No source file, no comment, no phrasing, no documentation.
- **No structural decision changed.** This project remains Minimal API with in-memory storage; that
  was settled during the original build and was not revisited.
- **Nothing else was adopted.** The two items above are the complete list.
- **Nothing here is a review of that submission.** It was read to learn from, and only the parts
  that changed this project are described. The absence of any other comment on it is deliberate.

---

## Why this file exists

Learning from someone else's solution to the same problem is ordinary engineering practice, and
citing it is what separates it from passing off someone else's thinking as your own. The
distinction is not whether you looked — it is whether you say so, whether you understood what you
took well enough to implement it yourself, and whether the reader can go and check.

All three are the point of this file. The second one is why Idea 1 is implemented differently than
the source: adopting it required understanding *why* constant-time comparison matters, and that
understanding is what surfaced the remaining length leak.

---

## Related

- [README.md](README.md) — index of this folder
- [middleware-pipeline.md](middleware-pipeline.md) — the authentication middleware and its test results
- [debugging-notes.md](debugging-notes.md) — the validation and error-handling behavior
- [../learning-mode/04-Learning-From-a-Peer-Review.md](../learning-mode/04-Learning-From-a-Peer-Review.md) — the same material explained from scratch
