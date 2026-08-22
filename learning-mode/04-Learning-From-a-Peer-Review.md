# 4. Learning from a peer review

*What reading another person's solution to the same assignment revealed, and the two things it changed.*

Terms in **bold** are defined in the [Glossary](Glossary.md).

---

## Reading someone else's answer to the same question

The three chapters before this one describe building something. This one describes reading —
specifically, reading another student's submission of the same assignment, and what happened to
this project as a result.

That is a more useful exercise than it sounds, and for a reason worth being precise about.

When you re-read your own code, you bring your own assumptions with you. The choices you made
deliberately are visible, because you remember making them. The choices you made *without
noticing* are not, because from the inside they do not look like choices at all — they look like
the way the thing is done. You cannot spot a decision you never registered making.

A second solution to the same brief removes that problem. The requirements were identical, so
every difference between the two is somebody's decision. When their code does something
differently, one of two things is true: either they made a choice you did not know was available,
or you made one you did not know you were making. Both are worth finding.

The submission read here is
[github.com/mzubanovic/UserManagementAPI](https://github.com/mzubanovic/UserManagementAPI), by
mzubanovic. Two of its choices changed this project. What was taken, and what was deliberately not
taken, is recorded in [../docs/references.md](../docs/references.md) — the short version is that
the *ideas* were adopted and no code was copied.

---

## The first idea: a clock is a way of reading a secret

Their authentication middleware compared the bearer **token** using something called a
constant-time comparison. This project compared it with `string.Equals`. To see why that matters,
forget code for a moment.

### A lock that tells you when you are close

Imagine a combination lock with four dials, and imagine it has a flaw: it clicks faintly the
moment you set a dial correctly, before you have finished the rest.

You do not know the combination. But you no longer need to guess it. You turn the first dial
through every digit and listen for the click. Now you know digit one. You move to the second dial
and do the same. Four dials, ten digits each: at most forty attempts, instead of the ten thousand
combinations the lock is supposed to require.

Nothing told you the combination. The lock never displayed it. You read it out of the lock's
*behavior* rather than its output.

### Why comparing two strings behaves like that lock

Here is how a computer normally compares two pieces of text:

```
compare "techhive-..." with the token that was supplied
   ↓
is character 1 the same?  no  → stop. answer: not equal.
```

It stops at the first character that differs. There is no reason to keep going — one difference
already settles it. Every string comparison in every language works this way, and normally that is
simply efficient.

Now watch what it costs here:

```
supplied "aaaaaaaa..."   → differs at character 1 → 1 comparison  → fastest
supplied "techXXXX..."   → differs at character 5 → 5 comparisons → slower
supplied "techhive-loXX" → differs at character 12 → 12 comparisons → slower still
```

The closer a wrong guess is, the longer the rejection takes. That is the clicking lock.

### The part that surprises people

The API is careful about what it says. Every rejection returns exactly the same sentence — `The
supplied token is not valid.` — with the same status code, every time. It never reveals how close
an attempt was.

And yet the information escapes anyway, because **how long a response takes is also something the
caller receives**. It is not in the body. Nobody printed it. It leaks through a channel that runs
alongside the intended one, which is why this is called a **side-channel** — and specifically, a
**timing side-channel**.

The differences are tiny, a few billionths of a second. That does not save you. An attacker does
not measure once; they send the same guess thousands of times and take the average, and random
network noise averages out while a consistent difference does not. Guessing a 32-character token
outright is hopeless. Recovering it one character at a time is a few thousand requests.

### The fix

A constant-time comparison always looks at every position, even after it already knows the answer,
and reports the result only at the very end. Same work, same duration, every time — whether the
guess was perfect or nothing like it. .NET provides this as
`CryptographicOperations.FixedTimeEquals`.

The clicking lock stops clicking.

---

## Why this project hashes first

The reference passes the raw bytes of the two tokens to `FixedTimeEquals`. That is the right idea,
and it closes the leak described above. This project does something slightly different, and the
reason is a second leak hiding underneath the first.

`FixedTimeEquals` compares two sequences of bytes — and if those sequences are different lengths,
it does not compare them at all. It returns `false` immediately, because two things of different
sizes cannot be equal. That check happens *before* the constant-time part begins.

Which puts the clicking lock back, in a smaller form:

```
supplied token 5 characters long  → lengths differ → instant rejection
supplied token 32 characters long → lengths match  → full comparison → slower
```

An attacker cannot read the token's characters this way, but they can read its **length**: try one
guess of each length, and the slow one is the real size. That is a genuine head start.

So this project hashes both values before comparing them:

```
"techhive-local-development-token"  ──SHA-256──→  32 bytes
"x"                                 ──SHA-256──→  32 bytes
"a much much longer wrong guess..." ──SHA-256──→  32 bytes
```

A **hash** turns an input of any size into an output of a fixed size — for SHA-256, always exactly
32 bytes. Hash both tokens and the two digests are *always* the same length, so the length check
can never fail early, and the comparison runs identically every single time. Neither the content
nor the length of the configured token affects the timing.

> **A detail, for honesty's sake.** Hashing the *supplied* token does take slightly longer for a
> longer input. But that is the attacker's own value, whose length they already know. The
> configured token is hashed identically on every request. Nothing about it leaks.

> **And the larger caveat.** None of this makes the token check secure. It is still a
> **simulated** check: it verifies no signature, issues no tokens, never expires, identifies no
> user, and reads its token from a file committed to this repository in plain text. Constant-time
> comparison fixed one specific weakness in one specific line. Everything chapter 3 said about
> what this check is not remains exactly as true.

---

## The second idea: a check that could not fire

The other thing their submission does is check the request body for null on create and update,
before doing anything else with it. This project did not. Following that up produced the more
interesting finding of the two — because the fix did not work the way it was supposed to.

### Two ways to build the same API

ASP.NET Core offers two styles. Their project uses **controllers**: classes marked with attributes,
one method per operation. This project uses **Minimal API**: routes registered directly, each with
a small function attached.

Both are supported, and both are normal. But they do not come with the same conveniences.

A controller marked `[ApiController]` gets **automatic model validation**. Before your method runs,
the framework inspects what arrived, and if it is missing or malformed, it answers `400` on its
own. Your method is never called. You do not write that check because the attribute writes it
for you.

Minimal API has no equivalent. Nothing inspects the incoming record before your handler receives
it. Whatever checking happens is checking you wrote.

> This is the general lesson, and it outlives this project: **a convenience is not a property of
> the language or the framework — it is a property of the particular style you chose within it.**
> Move from one style to another and protections you had stopped thinking about quietly stop being
> there. Knowing which of your checks are yours is not pedantry; it is the difference between a
> guard that runs and a guard you assumed was running.

### What testing actually found

The plan was straightforward: add `if (user is null)` at the top of the create and update
handlers, ahead of the validator, since `UserValidator` reads `user.Name` without checking and
would throw on a null record.

Then the case was actually tested — sending a request with no body — and no crash happened.
Instead:

```
400 Bad Request
{"error":"The request could not be read. Check that the body is valid JSON and that each field has the expected type."}
```

The log explained it. Because the handlers declared their parameter as a plain `User` — a type that
is not allowed to be null — the framework refused the request while **binding** that parameter,
before the handler was entered at all. The error-handling middleware from chapter 3 caught the
resulting exception and turned it into that message.

So the check was already covered — but the planned fix would have been **dead code**. Placed
inside the handler, it sat behind a door the request never got through. It could never have run,
and nothing would have revealed that except trying it.

### What was actually wrong, and what changed

The behavior was safe. The *message* was wrong. `The request could not be read` is what a truncated
or malformed body deserves. A body that was never sent is a different mistake, and telling someone
to check their JSON syntax when they sent no JSON sends them looking in the wrong place.

The fix was to let the request through so it could be answered properly: the parameter is now
declared `User?` — allowed to be null — so binding no longer rejects it, the handler is entered,
and an explicit check answers the caller directly.

```
POST /users with no body
400 Bad Request
{"error":"A user record is required in the request body."}
```

Malformed bodies are untouched and still produce the old message, which is now the only thing it
means.

> **Worth keeping.** The idea was right and came from the reference. The reasoning behind it was
> wrong — the gap was not where it was expected to be. Testing the assumption is what found that,
> and the only reason it was tested is that someone else's code prompted the question.

---

## What the review did not find

It would be dishonest to end without this part.

A review is not automatically a list of failures, and this one was not. Two things changed, both
small: one line of comparison logic, and one guard that had to be made reachable before it could
guard anything. Nothing else did.

The error handling was already right — two layers, deliberately separated, with a documented
reason for the split. The logging already recorded what it was supposed to, in the position that
made its output truthful. The validation already reported every invalid field instead of stopping
at the first. The documentation already matched the code, with evidence captured from a running
instance rather than written from memory. All of it was re-run against the changes made here, and
all of it still holds.

That is a normal outcome, and it is worth saying plainly, because the opposite expectation makes
reviews harder to ask for than they should be. The purpose of reading someone else's solution is
not to find out that yours is bad. It is to find the small number of places where a second person,
solving the same problem, reached for something you did not know to reach for.

Here that number was two. Both are now in the code, and both are credited in
[../docs/references.md](../docs/references.md).

---

**Previous:** [3. Middleware and the pipeline](03-Middleware-and-the-Pipeline.md) ·
[Glossary](Glossary.md) · [Back to learning-mode](README.md)
