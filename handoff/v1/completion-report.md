# Completion report — v1

**Build:** User Management API for TechHive Solutions, first and complete build into an empty
repository.
**Branch:** `deploy/v1-techhive-user-management-api` → **pull request #1**, open and unmerged.
**Plan:** [plan.md](plan.md), as approved before execution.

---

## 1. Commits

22 commits: one bootstrap commit on `main`, and 21 on the deploy branch, all accumulated into
**pull request #1** — which therefore shows 21 commits, the bootstrap commit being the base it
was opened against.

### On `main`

| # | SHA | Message |
|---|---|---|
| 1 | `3e9758b` | `chore: initialize repository` |

### On `deploy/v1-techhive-user-management-api`

| # | SHA | Message | Activity step |
|---|---|---|---|
| 2 | `fb378e7` | `chore: scaffold UserManagementAPI web API project` | A1 S2 |
| 3 | `0297a62` | `feat(activity1): add user model and in-memory user store` | A1 S3 |
| 4 | `bd93e5d` | `feat(activity1): add GET endpoints for user list and user by id` | A1 S3 |
| 5 | `47b5feb` | `feat(activity1): add POST endpoint to create a user` | A1 S3 |
| 6 | `d50a841` | `feat(activity1): add PUT endpoint to update a user` | A1 S3 |
| 7 | `16bd689` | `feat(activity1): add DELETE endpoint to remove a user` | A1 S3 |
| 8 | `c95bf5a` | `docs: document API endpoints and Activity 1 testing` | A1 S4 |
| | | **▶ Gate 1 — stopped, summarized, approved** | |
| 9 | `cc88317` | `feat(activity2): add input validation for user create and update` | A2 S3 |
| 10 | `f2094d0` | `fix(activity2): return 404 for requests targeting non-existent users` | A2 S3 |
| 11 | `244e732` | `fix(activity2): handle unhandled exceptions in user endpoints` | A2 S3 |
| 12 | `6fd1387` | `perf(activity2): optimize user lookup with id-keyed storage` | A2 S3 |
| 13 | `e80f1df` | `docs: document Activity 2 fixes and edge-case testing` | A2 S2/S4 |
| | | **▶ Gate 2 — stopped, summarized, approved** | |
| 14 | `27206f1` | `feat(activity3): add error-handling middleware` | A3 S3 |
| 15 | `faa6264` | `feat(activity3): add token authentication middleware` | A3 S4 |
| 16 | `3043cd3` | `feat(activity3): add request and response logging middleware` | A3 S2 |
| 17 | `a9d715b` | `chore(activity3): configure middleware pipeline order` | A3 S5 |
| 18 | `dfa3d82` | `fix(activity3): log request outcome accurately when an exception escapes` | *unplanned — see §4* |
| 19 | `3a2e8a3` | `docs: document middleware pipeline and testing` | A3 S6 |
| | | **▶ Gate 3 — stopped, summarized, approved** | |
| 20 | `0d09f84` | `docs: add repository README, folder READMEs, and project documentation` | |
| 21 | `ded9475` | `docs: add learning-mode walkthroughs and glossary` | |
| 22 | *this commit* | `docs: archive v1 build plan and completion report` | A3 S7 |

---

## 2. Outcome

The API was built from an empty repository through three gated activities, and **both invariants
held**. The **route-surface invariant** — the five CRUD paths, their verbs, and their successful
response shapes, frozen at Gate 1 — was verified by diff at Gate 2 and again at Gate 3 and did not
move: paths and verbs are byte-identical across all three gates, and the runtime surface was
re-exercised live at each gate with matching status codes, body shapes, and `Location` header. The
only change to the *declared* OpenAPI document was a correction flagged at Gate 1 and approved
before it was made — the document had claimed `200` for `POST` and `DELETE` while the endpoints had
always returned `201` and `204`, and annotations were added in Activity 2 so the description
matched the long-standing behavior. The **honesty invariants** held in both directions: no AI
product is named anywhere in the repository, its commit messages, its branch name, or the pull
request, with the only AI reference being the permitted neutral phrase in four documentation lines;
and the token check is labeled as simulated in a twenty-line header at the top of its own source
file, stating that it verifies no signature, issues no tokens, has no expiry, identifies no user,
and keeps its token in plaintext in version control — with no file anywhere claiming the API is
secure or production-ready. `dotnet build` returned zero errors and zero warnings after every one
of the 22 commits individually, not only at the end. Three gates were hit, each stopping with a
commit summary, build result, and observed test evidence before the next activity began. Every
request and response recorded in the documentation was captured from a running instance; nothing
was written from memory or illustration, and the testing is what found two defects that reading
the code had not — a stack-trace leak on malformed payloads and a logging component reporting a
status code that was never sent.

---

## 3. Success criteria

| # | Criterion | Result | Evidence |
|---|---|---|---|
| 1 | Every activity step maps to a commit, in order, conventional messages, none batched | **PASS** | 22 commits, mapped step-by-step in §1. One split, disclosed in §4. No step covered by another commit. |
| 2 | `dotnet build` clean — zero errors, zero warnings — after every commit | **PASS** | Verified individually after each of the 22 commits. Required pinning `Microsoft.OpenApi` to a patched version at commit 2 — the raw template emitted 2 security warnings (§4). |
| 3 | All five CRUD operations work and are documented with observed evidence | **PASS** | `GET /users` 200, `GET /users/{id}` 200, `POST` 201 + `Location: /users/4`, `PUT` 200, `DELETE` 204 — captured live at all three gates. Recorded in `docs/api-testing.md`. |
| 4 | 400 on invalid input, 404 for missing users, exceptions caught at both endpoint and middleware layers | **PASS** | 400 with per-field messages on empty/whitespace name, malformed email, `{}` body; 404 on GET/PUT/DELETE of a missing id; endpoint catch returns 500 (verified with an injected throw), middleware catch converts pre-handler failures to 400 JSON. `docs/debugging-notes.md`. |
| 5 | Three middleware components, ordered error handling → authentication → logging | **PASS** | `ErrorHandlingMiddleware`, `TokenAuthenticationMiddleware`, `RequestLoggingMiddleware`, registered in that order in `Program.cs` at commit 17, exactly as the assignment specifies. |
| 6 | Route-surface invariant held, verified by diff at Gate 2 and Gate 3 | **PASS** | Gate 1 → Gate 3 paths and verbs: identical. Gate 2 → Gate 3 success schemas: identical. Gate 1 → Gate 2: the two approved documentation corrections only (§4). Runtime surface re-verified live at each gate. |
| 7 | Three gates hit, each stopping and summarizing before the next activity | **PASS** | Gate 1 after commit 8, Gate 2 after commit 13, Gate 3 after commit 19. Each reported commits, build result, and observed test results, and waited for approval. |
| 8 | Every folder has a README; all internal markdown links resolve, reported N/N | **PASS** | 13 READMEs — one for each of the 12 folders, plus the root. Verified by listing every directory and every README side by side. Link check: **N/N** resolved, recorded in §7. |
| 9 | Zero AI product names anywhere; token middleware labeled simulated in source; no security claims | **PASS** | Grep across the working tree, all commit messages, the branch name, and the PR title and body: zero hits. Four permitted "AI coding assistant" lines, all in `docs/`. Simulation labeled at `TokenAuthenticationMiddleware.cs:1–24`. |
| 10 | Only `jdsaire` as author and committer on every commit | **PASS** | `git log --all --format='%an\|%ae\|%cn\|%ce' \| sort -u` returns exactly one line: `jdsaire\|88201583+jdsaire@users.noreply.github.com` for both roles. No trailers of any kind. |
| 11 | Bootstrap on `main`, all else on the named branch, one PR accumulating the run, left unmerged | **PASS** | `3e9758b` alone on `main`. Branch `deploy/v1-techhive-user-management-api` — exact name, not generated. PR #1 opened at the branch's first commit (commit 2) and accumulated all 20 subsequent commits. `state: OPEN`, `mergedAt: null`. |
| 12 | Zero subagents; no PAT requested, printed, or referenced | **PASS** | Single agent throughout. All GitHub access via `gh` CLI with the Keychain-persisted credential. No token value was ever requested, echoed, or written. |
| 13 | Plan, completion report, and folder README archived in `handoff/v1/`, indexed by `handoff/README.md` | **PASS** | This commit creates `handoff/v1/plan.md`, `handoff/v1/completion-report.md`, `handoff/v1/README.md`, and `handoff/README.md`. |

**13 of 13 PASS.**

---

## 4. Authorized deviations

Four. Each was either directed by the principal, approved at a gate before being acted on, or
forced by a conflict between the prompt and the toolchain's actual behavior.

### 4.1 Push policy — principal-directed

The standing rule is that a v1 run pushes directly to the default branch with no pull request. The
principal directed otherwise for this repository. Executed as specified: one bootstrap commit
established `main`, the branch was cut from it, the branch's first commit was pushed and PR #1
opened immediately, and every later commit pushed to the same branch. The pull request was never
merged.

### 4.2 One NuGet package added beyond scaffolder output — `Swashbuckle.AspNetCore.SwaggerUI`

**Reason.** The prompt refers to Swagger UI in three separate tasks and requires a browsable local
surface for gate inspection. The .NET 10 `webapi` template no longer ships Swashbuckle — since
.NET 9 it emits `AddOpenApi()` / `MapOpenApi()` only, which serves a raw JSON document at
`/openapi/v1.json` with no interactive page. This was verified against the template package before
planning, not assumed. Raised with the principal at plan time with both options costed; the
principal chose to add the package under the scope ceiling's "Swagger/OpenAPI support" clause.
Result: an interactive UI at `/swagger`, which was the inspection URL at all three gates.

### 4.3 A second package pinned to clear the zero-warning rule — `Microsoft.OpenApi`

**Reason.** A conflict between two hard rules, discovered at commit 2. Raw
`dotnet new webapi` output does **not** build clean: `Microsoft.AspNetCore.OpenApi` 10.0.5 resolves
`Microsoft.OpenApi` 2.0.0 transitively, which carries advisory GHSA-v5pm-xwqc-g5wc (high severity,
first patched in 2.7.5), and the build emits two `NU1903` warnings. The "zero warnings after every
commit" rule could not be met by the scaffolder's own output.

Of the three ways out — suppress the warning, skip restore during builds, or pin the patched
version — the first two hide a security advisory to protect a metric. The patched version was
pinned directly. This is a version pin on a package already present in the dependency graph, not a
new capability. Recorded in a comment in the `.csproj` at the point of the pin.

### 4.4 One unplanned commit — `dfa3d82`

**Reason.** Wiring the pipeline at commit 17 exposed a defect in the logging middleware added at
commit 16. It wrote its log line from a `finally` block, reading `Response.StatusCode` on the way
out. Because logging is registered *inside* error handling, an escaping exception passes through it
**before** the error handler sets the status — so it read the response's default of `200` and
logged `POST /users responded 200` for a request that received `400`.

The assignment's Activity 3 Step 6 requires logs to be accurate, and a false status code in an
audit trail is worse than a missing one, because nothing about it looks wrong. The fix — catch,
log the failure and the exception type without claiming a status, rethrow — was given its own
commit rather than folded into the pipeline-order commit, so that the pipeline-order commit stays
a faithful record of the specified step and the defect and its fix stay legible in the history.
Disclosed at Gate 3 before the documentation pass.

### Not a deviation, but recorded: scaffolder output differed from the planned tree

`dotnet new webapi` also emits `UserManagementAPI.http` and `appsettings.Development.json`, neither
of which appears in the prompt's target tree, and declares the `WeatherForecast` record inline in
`Program.cs` rather than in a separate file. Per the guardrails the tool's real output was adopted
rather than renamed to force a match. The `.http` file was kept and repopulated with working
requests for every endpoint; the weather-forecast sample and its record were removed at commit 2.

---

## 5. Decisions resolved autonomously

### 5.1 The specified pipeline order has a real, observable consequence

`resolved_decisions` fixed the order as error handling → authentication → logging, taken verbatim
from the assignment, and directed that it be implemented as specified and any functional
consequence recorded here rather than "corrected."

There is one, and it was observed rather than predicted: **because logging is registered after
authentication, a request rejected with `401` never reaches the logging middleware and produces no
`responded` line.** Three `401` responses were returned during Gate 3 testing and none appears in
the logging component's output.

Nothing goes unrecorded — the authentication middleware logs its own rejections, which is why it
does so — but the two kinds of entry have different shapes, and anyone building an audit report
from these logs must know that or they will count rejected requests as never having happened.
Registering logging outermost would produce one uniform line per request including rejections. The
assignment specifies this order, so the order stands; the consequence is documented in
`docs/middleware-pipeline.md`, in the source remarks of both affected components, and here.

This is also the root cause of the defect in §4.4. The two are the same structural fact seen from
two angles.

### 5.2 Two error-body shapes, deliberately

`400` validation failures use the framework's standard problem format, because they carry
per-field detail a single string cannot express. `404`, `401`, and `500` use a flat
`{ "error": "..." }` — the shape the assignment specifies for the middleware — because each is one
statement about the request as a whole. The split is documented in `docs/debugging-notes.md`
rather than left for a reader to notice.

### 5.3 The authentication middleware exempts the documentation paths

`/openapi` and `/swagger` are served without a token; every `/users` route is guarded. A browser
loading Swagger UI cannot attach an `Authorization` header to its own page and asset requests, so
guarding those paths would make the documentation — and the gate inspection URL — unreachable.
Stated in the middleware's own source, in `docs/middleware-pipeline.md`, and called out as one
more reason the check is not production authentication.

### 5.4 `GET /users` sorts by id

The Activity 2 storage change moved from a list to a dictionary, which has no guaranteed
enumeration order. Left alone, the order of `GET /users` would have become unpredictable — a
change in observable behavior, and a breach of the route-surface invariant in spirit if not in
shape. The store now sorts by id before returning. The cost is stated plainly in the documentation
rather than omitted.

### 5.5 The performance commit is described as what it is

`perf(activity2)` replaced linear scans with a keyed lookup. At three seeded records this is **not
a measurable speedup**, and the documentation says so. Its real value was correctness: the store is
a singleton shared across requests and the previous `List<T>` had no protection against concurrent
mutation. The commit is honest about which of those two things actually mattered.

### 5.6 Activity 1 was left genuinely defective

Activity 1 shipped with no validation and no missing-user handling, and the observed defective
responses — `200 OK` with body `null` for a missing user, `204` for a delete that deleted nothing,
`201 Created` for a user with an empty name — are recorded in `docs/api-testing.md` as observed.
This makes the Activity 2 fixes measurable against a real prior state rather than a described one.
It was a deliberate reading of the assignment's arc, disclosed at Gate 1.

### 5.7 Deliberate exception tests are reproducible, not narrated

Testing the exception guards required a failure that nothing naturally triggers. Rather than add a
diagnostic endpoint to the shipped route surface, a throw was temporarily added to a handler, the
result captured, and the line removed before committing. The exact line is quoted in both
`docs/debugging-notes.md` and `docs/middleware-pipeline.md` so any reader can reproduce the test.

---

## 6. Open items carried forward

| Item | Note |
|---|---|
| **The pull request is unmerged** | PR #1 is open with all 21 branch commits. Merging is the principal's to do manually. Nothing in this run merged, or attempted to merge, it. |
| **Data does not survive a restart** | By design — in-memory storage, per `resolved_decisions`. Stated plainly in the root README, `docs/`, `learning-mode/`, and the store's own source. Not a defect and not scheduled for change. |
| **The token check is simulated** | Replacing it with real authentication means replacing the file, not extending it. Stated at the top of the middleware source. |
| **Swagger UI's "Try it out" returns 401 for `/users`** | The page does not attach the bearer token to its requests. Expected, and documented in `docs/how-to-run.md` with three working alternatives (`curl`, the `.http` file, Postman). Configuring Swagger UI to send an auth header would need Swashbuckle's full `Swagger` package, which the scope ceiling does not permit for this run. |
| **`UseHttpsRedirection` logs a startup notice under the `http` profile** | Harmless — with no HTTPS port configured the redirect is a no-op. Documented in the troubleshooting table in `docs/how-to-run.md`. |
| **No automated tests** | Out of scope per the prompt. Testing in this run was manual against a running instance, with observed responses recorded in `docs/`. A future version wanting regression safety would need a test project, which this scope ceiling forbids. |

---

## 7. Final verification

Run after the last documentation commit.

| Check | Result |
|---|---|
| `dotnet build` | **PASS** — 0 errors, 0 warnings |
| Internal markdown links resolve | **PASS** — reported in §8 |
| Author and committer on every commit | **PASS** — `jdsaire` only, both roles, all 22 commits |
| AI product names in working tree | **PASS** — zero hits |
| AI product names in commit messages | **PASS** — zero hits |
| AI product names in branch name | **PASS** — zero hits |
| AI product names in PR title and body | **PASS** — zero hits |
| Token middleware labeled simulated in its own source | **PASS** — `TokenAuthenticationMiddleware.cs:1–24` |
| No file claims the API is secure or production-ready | **PASS** |
| Pull request open and unmerged | **PASS** — `state: OPEN`, `mergedAt: null` |

## 8. Link check

Counted across every markdown file in the repository, excluding `.git`, `bin`, and `obj`.
External `http(s)` links and same-page anchors are excluded; every relative path is resolved
against the filesystem.

**All internal markdown links resolve: 172/172.**

---

## Related

- [plan.md](plan.md) — the plan as approved before execution
- [README.md](README.md) — index of this archive
- [../README.md](../README.md) — the handoff index
- [../../README.md](../../README.md) — the project README
