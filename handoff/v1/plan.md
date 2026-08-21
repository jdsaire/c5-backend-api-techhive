# DEPLOY-C5-TechHiveUserManagementAPI-v1_0 — Build Plan

> **Archived record.** This is the build plan exactly as it was approved before any code was
> written, kept as a record of what was intended. Execution diverged from it in a few places —
> the real launch ports turned out to be 5139/7285 rather than 5000/5001, and one unplanned commit
> was added. Every divergence is recorded in [completion-report.md](completion-report.md); this
> file has not been rewritten to match the outcome.


## Context

`jdsaire/c5-backend-api-techhive` is an empty public repository. This run performs the first and
complete build of the Course 5 back-end capstone — a **User Management API for TechHive
Solutions** — into it, executed as three gates, one per graded Activity, with one commit per
assignment step.

The deliverable is graded on a 25-point peer rubric: a GitHub repository, CRUD endpoints,
evidence of debugging, validation, and middleware. The commit history is itself a deliverable —
a human reads it, a peer reviewer scans it — so no step is batched into another and no broken
build survives into the next commit.

Scope ceiling (absolute): ASP.NET Core Web API, in-memory storage only. No EF Core, no database,
no real cryptographic auth, no front-end, no test project, no GitHub Pages workflow.

---

## Preflight results — all checks PASS

| Check | Result |
|---|---|
| `gh` CLI | v2.96.0 at `~/bin/gh`, authenticated as **jdsaire** (keyring), scopes `gist, read:org, repo, workflow` |
| Assignment brief | Present and readable |
| .NET SDK | **10.0.201** at `/usr/local/share/dotnet/dotnet`; `webapi` template present |
| Repository state | `isEmpty: true`, `defaultBranchRef: ""`, `git ls-remote` returns **zero refs** — confirmed empty, matching `verified_state` |
| `handoff/` | Does not exist. Archival destination is `handoff/v1/` by construction. |

**Git identity — action required at task 2.** No `user.name` or `user.email` exists in the global
or system gitconfig (`~/.gitconfig` holds only `gh` credential helpers). Identity will be set
**repo-locally only**, leaving the machine's global config untouched:

```
git config user.name  "jdsaire"
git config user.email "88201583+jdsaire@users.noreply.github.com"
```

The GitHub account keeps its email private; the noreply address (id 88201583) attributes every
commit to `jdsaire` without publishing a personal address in a public repository's history.

**Local working copy:** `/Users/jd-mac/Downloads/c5-backend-capstone/c5-backend-api-techhive/`
(a subdirectory — the deploy XML and syllabus in the parent are run inputs and are never
committed).

---

## Confirmed scaffolder output vs. `architecture`

Verified against the real template package (`microsoft.dotnet.web.projecttemplates.10.0.10.0.5`),
not assumed. `dotnet new webapi -n UserManagementAPI` defaults to **minimal APIs** (matching
`resolved_decisions`) with OpenAPI enabled, and emits:

```
Program.cs   Properties/launchSettings.json   UserManagementAPI.csproj
UserManagementAPI.http   appsettings.json   appsettings.Development.json
```

**Three differences from the prompt's target tree — tool output is adopted, per guardrails:**

1. `UserManagementAPI.http` is emitted and is not in the target tree. **Kept**, weather-forecast
   sample stripped at task 3, repopulated with real CRUD requests at task 9. It is the brief's
   Step 4 "Postman or a similar tool."
2. `appsettings.Development.json` is emitted and is not in the target tree. **Kept** as-is.
3. The `WeatherForecast` record is declared **inline at the bottom of `Program.cs`**, not in a
   separate `WeatherForecast.cs` (that file belongs to the `--use-controllers` variant). Both the
   `/weatherforecast` endpoint and the record are removed at task 3.

`launchSettings.json` binds **`http://localhost:5000`** and `https://localhost:5001`. The `http`
profile is the gate inspection URL.

**Swagger UI — approved resolution.** .NET 10's template ships `Microsoft.AspNetCore.OpenApi`
only (`AddOpenApi()` / `MapOpenApi()`), serving a raw JSON document at `/openapi/v1.json` with no
interactive UI; Swashbuckle has not been in the template since .NET 9. Since the prompt references
"Swagger UI" in tasks 3, 9, and 23 and a browsable gate URL was requested, a single package —
**`Swashbuckle.AspNetCore.SwaggerUI`** — is added at task 3, pointed at the template's own OpenAPI
document. This is the one package added beyond raw scaffolder output, taken under the scope
ceiling's "Swagger/OpenAPI support" clause, and is recorded as an authorized deviation in the
Completion Report.

### Final file tree

```
README.md                                   .gitignore
src/UserManagementAPI/
  UserManagementAPI.csproj    Program.cs    UserManagementAPI.http
  appsettings.json            appsettings.Development.json
  Properties/launchSettings.json
  Models/User.cs              Storage/UserStore.cs        Endpoints/UserEndpoints.cs
  Validation/UserValidator.cs
  Middleware/ErrorHandlingMiddleware.cs
  Middleware/TokenAuthenticationMiddleware.cs
  Middleware/RequestLoggingMiddleware.cs
docs/            README.md  how-to-run.md  setup-guide.md  grading-criteria.md
                 api-testing.md  debugging-notes.md  middleware-pipeline.md
learning-mode/   README.md  01-Building-the-CRUD-Surface.md
                 02-Validation-and-Error-Handling.md  03-Middleware-and-the-Pipeline.md
                 Glossary.md
handoff/         README.md  v1/README.md  v1/plan.md  v1/completion-report.md
```

`ux-ui/` and `.github/workflows/` are deliberately absent — no rendered interface, and a Web API
cannot be statically hosted on Pages.

---

## Frozen route surface (established Activity 1, frozen at Gate 1)

| Verb | Path | Success |
|---|---|---|
| GET | `/users` | 200 — array of user objects |
| GET | `/users/{id}` | 200 — one user object |
| POST | `/users` | 201 + `Location` — created user object |
| PUT | `/users/{id}` | 200 — updated user object |
| DELETE | `/users/{id}` | 204 — no body |

`User` = `Id` (int, store-assigned; the brief routes by ID), `Name` (string; the brief's named
validation target), `Email` (string; the brief's named validation target), `Department` (string;
the brief's HR/IT scenario is what makes this a TechHive user record). Four fields, each traceable
to a brief line.

Activities 2 and 3 may add 400/401/404/500 and middleware around these routes and may change
internals freely. They may **not** rename a route, change a verb, remove a route, or alter a
successful response shape.

---

## Complete commit sequence — 21 commits

**On `main` (bootstrap only):**

| # | Message | Task |
|---|---|---|
| 1 | `chore: initialize repository` | 2 |

**Then** cut `deploy/v1-techhive-user-management-api` from commit 1. Push commit 2, then
**immediately** open the PR against `main`. Every later commit pushes to that branch and
accumulates into that one PR. Push after each commit — never batched.

**On `deploy/v1-techhive-user-management-api`:**

| # | Message | Task | Brief step |
|---|---|---|---|
| 2 | `chore: scaffold UserManagementAPI web API project` | 3 | A1 S2 |
| 3 | `feat(activity1): add user model and in-memory user store` | 4 | A1 S3 |
| 4 | `feat(activity1): add GET endpoints for user list and user by id` | 5 | A1 S3 |
| 5 | `feat(activity1): add POST endpoint to create a user` | 6 | A1 S3 |
| 6 | `feat(activity1): add PUT endpoint to update a user` | 7 | A1 S3 |
| 7 | `feat(activity1): add DELETE endpoint to remove a user` | 8 | A1 S3 |
| 8 | `docs: document API endpoints and Activity 1 testing` | 9 | A1 S4 |
| — | **▶ GATE 1 — stop, summarize, report localhost URL, await approval** | 10 | |
| 9 | `feat(activity2): add input validation for user create and update` | 11 | A2 S3 |
| 10 | `fix(activity2): return 404 for requests targeting non-existent users` | 12 | A2 S3 |
| 11 | `fix(activity2): handle unhandled exceptions in user endpoints` | 13 | A2 S3 |
| 12 | `perf(activity2): optimize user lookup with id-keyed storage` | 14 | A2 S3 |
| 13 | `docs: document Activity 2 fixes and edge-case testing` | 15 | A2 S2/S4 |
| — | **▶ GATE 2 — stop, summarize, invariant diff, await approval** | 16 | |
| 14 | `feat(activity3): add error-handling middleware` | 17 | A3 S3 |
| 15 | `feat(activity3): add token authentication middleware` | 18 | A3 S4 |
| 16 | `feat(activity3): add request and response logging middleware` | 19 | A3 S2 |
| 17 | `chore(activity3): configure middleware pipeline order` | 20 | A3 S5 |
| 18 | `docs: document middleware pipeline and testing` | 21 | A3 S6 |
| — | **▶ GATE 3 — stop, summarize, invariant diff, await approval** | 22 | |
| 19 | `docs: add repository README, folder READMEs, and project documentation` | 23 | |
| 20 | `docs: add learning-mode walkthroughs and glossary` | 24 | |
| 21 | `docs: archive v1 build plan and completion report` | 26 | A3 S7 |

No step is batched. No commit is split. `dotnet build` must return **zero errors and zero
warnings** immediately after each commit — verified individually, not only at the end.

**The PR is never merged.** It is left open for manual merge after task 26.

---

## Interpretations recorded (not guessed silently)

- **I1 — Swagger UI.** As above; one package, approved, recorded as an authorized deviation.
- **I2 — `docs/grading-criteria.md` vs. the honesty invariant.** The brief's third rubric line
  names an AI product by name. No file in this repository may name any AI product. The breakdown
  is therefore recorded with that line rendered as *"Did you use an AI coding assistant to debug
  your code?"*, and the file is explicitly marked as recording the criteria, not answering them.
  The brief itself is never copied into the repository.
- **I3 — Auth middleware exemptions.** The token middleware exempts `/openapi/*` and `/swagger/*`
  so the documentation surface stays reachable in a browser; `/users*` is guarded. The exemption
  is stated in the middleware source and in `docs/middleware-pipeline.md` — not hidden.
- **I4 — "Failed database lookups" / "performance bottlenecks" (brief A2 S2).** No database exists
  per `resolved_decisions`. Read as: missing-id lookups against the in-memory store (commit 10),
  and the store's linear scan (commit 12). `docs/debugging-notes.md` states plainly that at seed
  scale the lookup change is a correctness-of-structure improvement, not a measurable speedup.
- **I5 — Activity 3 step order.** The brief numbers logging (S2), error handling (S3), auth (S4);
  the prompt orders the commits errors → auth → logging to mirror pipeline order. Prompt order
  governs; every step still maps to its own commit. Noted in the Completion Report.
- **I6 — Pipeline-order consequence.** With error-handling → authentication → logging as the brief
  specifies, the logging middleware sits *inside* the auth gate, so requests rejected with 401 are
  never reached by it. Per `resolved_decisions` this is implemented **exactly as specified** and
  the observation is recorded in the Completion Report's "Decisions resolved autonomously" — it is
  not "corrected" toward conventional practice.
- **I7 — Simulated auth, labeled at definition.** The token check compares a bearer token against a
  configured value in `appsettings.json`. No signature verification, no issuance, no expiry. The
  file's own header states what it checks, what it does not, and that it is not production
  authentication. No file claims the API is secure or production-ready.

## Authorized deviations

- **Swagger UI package** (I1) — one NuGet package beyond raw scaffolder output.
- **Push policy** — principal-directed deviation from the standing "v1 pushes to main" rule, as
  specified in the prompt: bootstrap on `main`, all work on the named branch, one PR, unmerged.

---

## Gate protocol

At each gate I stop, write nothing further, and report:

1. That Activity's commits — short SHA + message each.
2. `dotnet build` result (zero errors / zero warnings).
3. Endpoint / edge-case / middleware test results — **observed** responses, never invented.
4. **Localhost inspection URL**, live while the gate is open, for you to click through:
   - `http://localhost:5000/swagger` — browsable Swagger UI
   - `http://localhost:5000/openapi/v1.json` — raw OpenAPI document
   - After commit 17, `/users` requires header `Authorization: Bearer <configured-token>`;
     the Swagger UI and OpenAPI paths stay reachable per I3.
5. The PR number (from Gate 1 onward).

**Route-surface invariant check** (Gates 2 and 3): the Gate 1 OpenAPI document is captured to the
scratchpad; each later gate diffs paths, verbs, and success-response schemas out of the live
document against that snapshot with `jq`, and the diff output is reported. Added 400/401/404
responses are expected and permitted; any path, verb, or success-shape change is a stop condition.

---

## Final verification (task 25)

- `dotnet build` — zero errors, zero warnings.
- Every internal markdown link in the repository resolves — reported as **N/N**.
- `git log --format='%an|%ae|%cn|%ce'` shows **only** `jdsaire` as author and committer, on every
  commit, with no co-authorship or tooling-attribution trailer of any kind.
- Working tree grepped for every AI product name and for attribution trailers — zero hits
  outside permitted "AI coding assistant" phrasing. Branch name, PR title, and PR body included.
- Token middleware is labeled simulated in its own source file; no file claims the API is secure.
- PR confirmed **open and unmerged**.
- PASS/FAIL reported per check, then the `handoff/v1/` archive is written (task 26) with a
  PASS/FAIL table carrying evidence in each cell, not a bare "PASS".

## Execution constraints held throughout

Single agent — **zero subagents**. `gh` CLI is the only GitHub access method; no PAT is ever
requested, printed, or referenced. No merge of the pull request under any circumstance.
