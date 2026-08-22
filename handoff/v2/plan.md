# DEPLOY-C5-TechHiveHardeningPass-v2_0 — Plan

## Context

The v1 User Management API (`jdsaire/c5-backend-api-techhive`) is finished, merged, and graded.
Reviewing another student's submission of the same assignment —
[`mzubanovic/UserManagementAPI`](https://github.com/mzubanovic/UserManagementAPI) — surfaced two
ideas worth adopting. This run adopts the ideas, not the code, and records where they came from.

It is **not** a feature build: no new endpoint, field, or capability, no revisiting of v1's
architecture (in-memory storage, Minimal API, simulated authentication), no change to the route
surface.

**Scope:** two code changes, their documentation, re-captured test evidence, an attribution
document, and a learning-mode chapter. Six commits on `deploy/v2-token-hardening`, PR opened at
the first commit and left **unmerged**.

---

## Preflight — complete, all green

| Check | Result |
|---|---|
| `gh` CLI | v2.96.0 at `~/bin/gh`, authenticated as `jdsaire` (keyring), scopes include `repo` |
| Assignment brief | Present and readable; C4 filename, C5 capstone content, as expected |
| `main` HEAD | `a895afe` — **no drift**; 23 commits reachable; PR #1 merged |
| Baseline `dotnet build` | **Clean** — 0 errors, 0 warnings (SDK 10.0.201) |
| Reference repo | Reachable; both cited facts verified (see below) |
| AI-product audit | Zero hits for any AI product name in the tree; 4 permitted "AI coding assistant" lines |

The local clone at `~/Downloads/c5-backend-capstone/c5-backend-api-techhive` is on the v1 branch;
its tree is byte-identical to `a895afe`.

### Both current behaviors confirmed by reading source at `a895afe`

1. `TokenAuthenticationMiddleware.cs` compares with
   `string.Equals(suppliedToken, expectedToken, StringComparison.Ordinal)` — short-circuits on the
   first differing character.
2. `UserEndpoints.cs` create and update call `UserValidator.TryValidate(user, ...)` with no prior
   null check; `TryValidate` dereferences `user.Name` directly.

### Both peer-repo facts verified (read-only, via `gh api`)

- `Middleware/AuthenticationMiddleware.cs` uses
  `CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(...), Encoding.UTF8.GetBytes(...))`
  — raw UTF-8 bytes, which is exactly the length-leak our hash-then-compare decision addresses.
- `Controllers/UsersController.cs` is `[ApiController]` and has explicit `if (user is null) return
  BadRequest();` on create and update.

---

## Finding that changed task 4 (principal decided: make the guard real)

Probing the running v1 build, a literal `null` body and an empty body on `POST /users` and
`PUT /users/{id}` **already** return `400`:

```
{"error":"The request could not be read. Check that the body is valid JSON and that each field has the expected type."}
```

Server log shows the mechanism: `BadHttpRequestException: Implicit body inferred for parameter
"user" but no body was provided`, thrown by `RequestDelegateFactory` **before the handler runs**,
caught by `ErrorHandlingMiddleware`. Because `User user` is a non-nullable bound parameter, `user`
can never arrive null inside the handler — a guard placed before the validator would be dead code.

**Decision (approved):** make the guard genuinely reachable. This is faithful to the prompt's own
statement that the run changes "what an invalid body is answered with."

---

## Code change 1 — constant-time token comparison

**File:** `src/UserManagementAPI/Middleware/TokenAuthenticationMiddleware.cs`

Replace the `string.Equals` comparison with hash-then-compare: SHA-256 the supplied and configured
tokens (UTF-8), compare the two 32-byte digests with `CryptographicOperations.FixedTimeEquals`.
Digests are always equal-length, so neither the content nor the *length* of the configured token
affects timing — deliberately a step beyond the reference submission's raw-bytes approach.

`System.Security.Cryptography` and `System.Text` are base class library; **no package reference**.

Preserve exactly: exempt `/openapi` and `/swagger` prefixes, fail-closed on unconfigured token,
all per-case warning logs, the 401 body shape, the `WWW-Authenticate: Bearer` header, and the
message policy of never revealing how close an attempt was.

**Header block:** edit the "what it does NOT do" list, do not shorten it. The timing-correlation
weakness goes away; every other item stays — no signature verification, no token issuance, no
expiry, no user identity, plaintext token in version control. Add a plain statement that
constant-time comparison does not make this authentication.

**Evidence to capture:** valid token, invalid token of the *same* length, invalid token of a
*different* length, missing header, non-Bearer scheme. All five must return the same status,
body, and headers as before.

## Code change 2 — reachable null-body guard

**File:** `src/UserManagementAPI/Endpoints/UserEndpoints.cs`

Change the bound body parameter to `User? user` on `MapPost("")` and `MapPut("/{id:int}")` so the
framework passes null through instead of throwing, then guard immediately, before
`UserValidator.TryValidate`:

```csharp
if (user is null)
{
    return Results.BadRequest(new { error = "A user record is required in the request body." });
}
```

Flat `{ "error": ... }` is the right shape by the project's own documented policy
(`docs/debugging-notes.md:119–121`): per-field validation failures use ProblemDetails because they
carry detail a single string cannot express; single-statement errors use flat `{ error }`. A
missing body is a single statement, and today's null-body 400 is already flat.

Add a brief comment explaining why Minimal API needs this explicitly where an `[ApiController]`
project's automatic `ModelState` validation would cover it. Add `.Produces(400)` metadata only if
the existing `.ProducesValidationProblem()` does not already cover the status — do not otherwise
touch route metadata.

**Evidence to capture:** literal `null` body, empty body, and a valid body, on both create and
update; plus truncated JSON and a wrong-type field, which must *still* be answered by
`ErrorHandlingMiddleware` with the unchanged "could not be read" message.

### Route-surface invariant

Holds. Paths, verbs, and successful response bodies (`200`, `201` + `Location`, `204`) are
untouched. Only an error-response message changes, which the invariant explicitly permits.

---

## Affected documents (confirmed by reading, not assumed)

**Expected to change**

| File | Why |
|---|---|
| `docs/middleware-pipeline.md` | §1 error-handling table row for unreadable requests (null/empty body moves out from under it); §2 authentication description and the simulated-check callout |
| `docs/debugging-notes.md` | Bug 4 "malformed payloads leak internals" now excludes null/empty bodies; add re-captured null-body rows to the edge-case table |
| `README.md` | "A note on the authentication" — comparison mechanism |
| `src/UserManagementAPI/Middleware/README.md` | Simulated-check callout |
| `learning-mode/03-Middleware-and-the-Pipeline.md` | "It compares a string to another string" becomes factually wrong |
| `docs/README.md` | Index row for the new `references.md` |
| `learning-mode/README.md` | Table row for chapter 04 |
| `handoff/README.md` | Second version row |

**Expected to be verified and left alone** — re-checked against the running build, not rewritten:
`docs/api-testing.md` (Activity 1 success paths, unaffected), `middleware-pipeline.md` captured
401 evidence at lines 182–184 (identical responses), `middleware-pipeline.md` lines 203–204
(truncated JSON and wrong-type still route through the error middleware), `docs/how-to-run.md`,
`docs/setup-guide.md`, `docs/grading-criteria.md`, `learning-mode/02`, `learning-mode/Glossary.md`,
`src/UserManagementAPI/Validation/README.md`, `Endpoints/README.md`. Any that turns out to need a
change gets one; the Completion Report records which were left and why.

---

## Attribution constraint worth stating

The reference repo's own GitHub description contains a named AI product. **Do not quote it.**
`docs/references.md` names the repository URL and the author's handle, states the two ideas taken,
states plainly that no code was copied, and explains where this implementation deliberately
differs — appreciative and factual, no negative characterization. Quoting their description would
plant a banned product name in the working tree and fail the task-10 grep.

---

## Commit sequence

Branch `deploy/v2-token-hardening`, cut from `main` at `a895afe`. Author and committer `jdsaire`
only; no trailers. Build verified clean after **each** commit. Every commit pushed as it is made.

| # | Commit message | Task |
|---|---|---|
| 1 | `feat(auth): compare bearer tokens in constant time` | 3 — push, then **immediately open PR** against `main` |
| 2 | `fix(validation): reject null request bodies on create and update` | 4 — push |
| 3 | `docs: record the peer submission that informed this pass` | 6 — push |
| 4 | `docs: update documentation and test evidence for the v2 changes` | 7 — push |
| 5 | `docs: add learning-mode chapter on the peer review pass` | 8 — push |
| 6 | `docs: archive v2 plan and completion report` | 11 — push |

Commits 3 and 5 each carry their index update (`docs/README.md`, `learning-mode/README.md`)
because the document and its index entry are one deliverable.

**Gates — hard stops:**
- **Gate 1** after commit 2 — commits, PR number, build result, route-surface diff vs `a895afe`,
  all captured responses, five CRUD operations re-run end to end.
- **Gate 2** after commit 5 — documentation commits, which files changed, which were
  verified-and-left-alone, each re-read against the source.

---

## New files

- `docs/references.md` — the attribution document described above.
- `learning-mode/04-Learning-From-a-Peer-Review.md` — same plain-language register as 01–03:
  what a peer review of the same brief teaches; what a timing side-channel is from first
  principles, including why comparing two strings leaks information even when neither is printed;
  why hashing first removes the length leak raw-byte comparison leaves; why `[ApiController]`
  model validation exists in one style of ASP.NET project and not another, and what that means for
  where your own checks live — including the honest note that the guard had to be made reachable;
  and that the review found the existing error handling, logging, and documentation already sound.
  Links `docs/references.md`.
- `handoff/v2/plan.md` (this file), `handoff/v2/completion-report.md`, `handoff/v2/README.md`.

---

## Verification (task 10)

Run after gate 2, report PASS/FAIL per check:

```bash
dotnet build src/UserManagementAPI/UserManagementAPI.csproj
```

- Internal markdown links: count every one and confirm each resolves; report **N/N** against v1's
  172 baseline (the count will rise — new files and index rows add links).
- `git log` across the branch shows only `jdsaire` as author **and** committer.
- Grep working tree, commit messages, branch name, PR title and body for every AI product name
  covered by the repository's naming policy, and for authorship trailers — zero hits outside
  permitted "AI coding assistant" phrasing.
- No file claims the API is secure, hardened, or production-ready.
- Route-surface diff against `a895afe` one final time.
- PR is open and **unmerged**.

**End-to-end behavior check**, against a running instance on `http://localhost:5139`
(`dotnet run --project src/UserManagementAPI --launch-profile http`): five CRUD operations with a
valid token; the five token cases from change 1; the five body cases from change 2. Every response
quoted in documentation comes from this run, never edited to expectation.

---

## Open items policy

Anything noticed but out of scope gets recorded in the Completion Report's open-items section, not
fixed here. The code scope is fixed at the two changes above.

## Stop conditions

Stop and report if: the route-surface invariant is found broken; a commit breaks the build
unfixably within that commit; a change would need a new dependency or breach the scope ceiling; a
credential would need to be printed. **Never merge the PR** — the principal merges it manually
after archival.
