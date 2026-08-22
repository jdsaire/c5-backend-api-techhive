# Completion report — v2

The token-hardening pass: two code changes taken from a peer review of another student's
submission of the same assignment, the documentation that records them, and a learning-mode
chapter on the review itself.

Branch `deploy/v2-token-hardening`, cut from `main` at `a895afe`. Pull request **#2**, open and
unmerged — merging it is a manual step.

---

## 1. Commits

Six, in order, each pushed as it was made. `dotnet build` returned zero errors and zero warnings
immediately after every one.

| # | SHA | Message |
|---|---|---|
| 1 | `1046f15` | `feat(auth): compare bearer tokens in constant time` |
| 2 | `1a2e8a6` | `fix(validation): reject null request bodies on create and update` |
| 3 | `91da93a` | `docs: record the peer submission that informed this pass` |
| 4 | `f1162ba` | `docs: update documentation and test evidence for the v2 changes` |
| 5 | `7a09169` | `docs: add learning-mode chapter on the peer review pass` |
| 6 | *this commit* | `docs: archive v2 plan and completion report` |

The pull request was opened immediately after commit 1, as the push policy for a v2 run requires.
Author and committer on all six are `jdsaire`; no trailers of any kind.

---

## 2. Outcome

Two code changes landed, both traceable to a specific finding in a specific peer submission, and
neither of them a feature. The bearer-token comparison now hashes the supplied and configured
tokens with SHA-256 and compares the digests with `CryptographicOperations.FixedTimeEquals`,
replacing an ordinal string comparison whose duration correlated with how many leading characters
a guess got right; hashing first also keeps the configured token's length out of the timing, which
comparing raw bytes would not have done. Create and update now bind the request body as nullable
and check it for null before validating, so a body that was never sent is answered with a message
about the body being absent instead of the message a truncated body produces.

**The route-surface invariant held.** The five CRUD paths, their verbs, their route names, and
their declared response types are byte-identical to `a895afe`, verified by diff at gate 1 and
again at the verify task. No successful response body changed. The only difference in the route
registration lines is the body parameter's nullability on `POST /users` and `PUT /users/{id}`.

**The honesty invariant held.** The token check is still labeled `THIS IS A SIMULATED TOKEN CHECK.
IT IS NOT REAL AUTHENTICATION.` on line 7 of its own source file. The "what it does NOT do" list
was edited rather than shortened: the timing item is gone because it is no longer true, and the
five remaining items — no signature, no issuance, no expiry, no user identity, plaintext token in
version control — are all still there, with an added statement that constant-time comparison does
not make the check authentication. No file in the repository claims the API is secure, hardened,
or production-ready; the five phrase matches across the tree are all negations. No AI product is
named anywhere in the working tree, the commit messages, the branch name, or the pull request.

The most useful thing this pass produced was not either code change. It was the discovery, at
task 4, that the null check as originally specified could never have executed.

---

## 3. Success criteria

| # | Criterion | Result | Evidence |
|---|---|---|---|
| 1 | Constant-time comparison via SHA-256 digests and `FixedTimeEquals`, every pre-existing middleware behavior preserved | **PASS** | `TokenAuthenticationMiddleware.cs:141–147`. Re-tested: exempt paths `/openapi/v1.json` and `/swagger/index.html` still 200 without a token; four distinct rejection warnings still logged; 401 body, status, and `WWW-Authenticate: Bearer` unchanged in all four rejection cases. |
| 2 | Create and update reject a null body with `400` in the API's existing error shape | **PASS** | `UserEndpoints.cs:67, 76–79, 101, 107–110, 162`. `POST` and `PUT` with a literal `null` and with an empty body all return `400 {"error":"A user record is required in the request body."}` — the flat shape the API already uses for single-statement errors. |
| 3 | `dotnet build` clean after every commit | **PASS** | Zero errors, zero warnings verified individually after each of the six commits, and once more at the verify task. SDK 10.0.201. |
| 4 | Route-surface invariant held, verified by diff at gate 1 and at verify | **PASS** | Diff of all `users.Map*` registrations plus every `.WithName`, `.Produces`, and `.ProducesValidationProblem` against `a895afe`: identical at both checks. |
| 5 | Header block still states accurately and completely what the check does not do; no file claims the API is secure | **PASS** | Five-item list intact and extended, `TokenAuthenticationMiddleware.cs:7–35`. Tree-wide grep for security and production claims returns five lines, every one a negation. |
| 6 | `docs/references.md` names the peer repository, the two ideas, that no code was copied, and where this implementation differs | **PASS** | `docs/references.md` — names the URL and the author's handle, sets out both ideas with their reasoning, states the four things explicitly not taken, and devotes a section to why this project hashes before comparing where the reference does not. |
| 7 | Every affected document agrees with the code, evidence re-captured from a running instance; unchanged documents verified rather than assumed | **PASS** | All six error strings quoted anywhere in the documentation exist verbatim in the `.cs` files. Twenty documented behaviors replayed against a running instance at gate 2: 20/20 pass. Fifteen documents were checked and deliberately left unchanged — listed in §6 below. |
| 8 | `learning-mode/04` exists, is indexed, explains timing side-channels and the Minimal-API validation gap in plain language | **PASS** | 252 lines. Side-channel built up from a combination lock that clicks; the length leak explained separately; `[ApiController]` model validation contrasted with Minimal API; indexed in `learning-mode/README.md` with forward navigation added to chapter 03. |
| 9 | All internal markdown links resolve, reported against v1's 172 baseline | **PASS** | **205/205** links with a file target resolve, against v1's baseline of 172 — the same counting method v1 used, confirmed by re-running the checker against `a895afe` and reproducing 172/172 exactly. The rise is this pass's new files and index rows. See §7 for the 13 pre-existing same-file anchors that neither count includes. |
| 10 | Zero AI product names in the tree, commit messages, branch name, or PR title/body; only `jdsaire` as author and committer | **PASS** | Four greps, zero hits each. `git log a895afe..HEAD` yields exactly one distinct author/committer pair: `jdsaire <88201583+jdsaire@users.noreply.github.com>`. Four permitted "AI coding assistant" lines, all pre-existing. |
| 11 | All commits on `deploy/v2-token-hardening`, pushed as made; one PR opened at the first commit, left unmerged | **PASS** | Branch name exact. Local and remote HEAD identical after every push. PR #2 `state=OPEN`, `mergedAt=null`. |
| 12 | Two gates hit, each stopping and summarizing before the next phase | **PASS** | Gate 1 after commit 2 — commits, PR number, build, invariant diff, every captured response, five CRUD operations. Gate 2 after commit 5 — documentation commits, changed versus verified-and-left-alone, each re-read against source. Both waited for approval. |
| 13 | Zero subagents; no PAT requested, printed, or referenced | **PASS** | Single agent context throughout. All GitHub access via `gh` CLI using its stored keychain credential; no token value was requested, printed, or echoed. |
| 14 | Plan, completion report, and folder README archived in `handoff/v2/`, with a second row in `handoff/README.md` | **PASS** | This commit. |

---

## 4. Authorized deviations

**4.1 — The null-body guard was implemented differently than `resolved_decisions` specified, with
the principal's approval.**

The prompt specified an explicit null check in the create and update handlers, before validation.
Testing the case before writing the check found that it could never run. Because the handlers
declared a non-nullable `User` parameter, the framework rejected a missing or literal-`null` body
while binding that parameter — before the handler was entered — raising:

```
Microsoft.AspNetCore.Http.BadHttpRequestException: Implicit body inferred for parameter "user"
but no body was provided. Did you mean to use a Service instead?
```

The error-handling middleware caught it and answered `400`. So the behavior was already safe, and
a guard placed inside the handler would have been unreachable code.

This was raised at the planning stage with three options — ship the guard as unreachable
defence-in-depth, make it reachable, or drop it and document the finding. The principal chose to
make it reachable. The body parameter is now `User?`, so binding no longer rejects the request and
the guard executes. The observable consequence is that an absent body is answered with
`A user record is required in the request body.` rather than the `could not be read` message a
malformed body produces. Malformed bodies are unaffected.

The prompt's own framing — that this run changes "what an invalid body is answered with" —
supports this reading. It is recorded as a deviation because the literal instruction said the
check would sit before the validator and change nothing else, and that was not what shipped.

**4.2 — Two lines of the archived plan were reworded before committing it.**

`handoff/v2/plan.md` is the plan as approved, with one exception: two lines that spelled out the
AI product names being grepped for were rewritten to describe the check without naming them. The
plan was approved in a chat window, where naming them is harmless; committing it would have put
those names in the working tree and failed the very check the lines describe. v1's archived plan
handles the same problem the same way. No substance was changed, and nothing was edited to match
the outcome.

---

## 5. Decisions resolved autonomously

**5.1 — The absent-body `400` uses the flat error shape, not the validation problem format.**

The API has two error shapes and `docs/debugging-notes.md` already recorded the rule behind the
split: the problem format is for failures carrying one entry per invalid field, and the flat
`{ "error": ... }` is for statements about the request as a whole. A body that was never sent has
no fields to report, so it takes the flat shape. This also keeps the response shape-compatible
with what the case already returned, since the error-handling middleware's answer was flat too.
Only the message changed.

**5.2 — The reference repository's own description was not quoted.**

That description names a commercial AI product. Quoting it, even inside an attribution, would have
introduced that name into the working tree. `docs/references.md` therefore names the repository,
its URL, and its author's handle, and describes the two ideas in this project's own words.

**5.3 — Activity 2's edge-case table was extended by reference rather than rewritten.**

The absent-body cases are documented in their own dated section rather than being folded into the
Activity 2 bug log, so that log stays a faithful record of what that pass found. The table now
carries a line confirming every row was re-run and still holds, and a pointer to the new section.

**5.4 — `learning-mode/03` was given a small factual correction.**

It described the token check as comparing "a string to another string," which stopped being
accurate once the comparison ran over digests. Chapter 03 documents v1, but a sentence that is now
wrong is drift regardless of which pass wrote it.

---

## 6. Documents verified and deliberately left unchanged

Each was read against the current source rather than assumed, and none needed a change:
`docs/api-testing.md`, `docs/how-to-run.md`, `docs/setup-guide.md`, `docs/grading-criteria.md`,
`learning-mode/01-Building-the-CRUD-Surface.md`,
`learning-mode/02-Validation-and-Error-Handling.md`, `learning-mode/Glossary.md`, `src/README.md`,
`src/UserManagementAPI/README.md`, and the `Endpoints`, `Validation`, `Models`, `Storage`, and
`Properties` folder READMEs.

`docs/api-testing.md` deserves a note, because it does contain the word `null`: those references
are to Activity 1's original defect, where a request for a missing user returned `200 OK` with a
`null` body. That was fixed in Activity 2 and is unrelated to this pass.

**No pre-existing behavioral drift was found.** Every response recorded in v1's documentation that
this pass re-ran still matches the code.

---

## 7. Open items carried forward

**7.1 — Thirteen same-file anchor links in `learning-mode/Glossary.md` do not resolve.**

The Glossary defines its terms as bold text (`**JSON** — ...`) rather than as headings, but is
linked to with heading-style anchors — `[JSON](#json)`, `[verb](#verb)`, and eleven more. Since
there are no headings with those names, the links do nothing when clicked.

This is **pre-existing and unchanged by this run**: re-running the link checker against `a895afe`
finds the same thirteen. It is not a contradiction of v1's `172/172` claim either — v1 counted
links with a file target, and all 172 of those did and still do resolve, as do all 205 that exist
now. These thirteen have no file target and were never in the count.

Not fixed here, because the scope of this run was fixed at two code changes and their
documentation. The fix is small: either give each term a real heading, or drop the thirteen
self-links.

**7.2 — Swagger UI still cannot send the bearer token**, so its "Try it out" button returns `401`
for every `/users` route. Carried forward from v1 unchanged; documented in `docs/how-to-run.md`
with three working alternatives.

**7.3 — The configured token remains in `appsettings.json` in plain text and in version control.**
Unchanged by this run by design, and still one of the stated reasons the check is not
authentication.

**7.4 — Nothing else was noticed and suppressed.** No further improvement was identified during
this pass that is not already listed above.

---

## 8. Final verification

| Check | Result |
|---|---|
| `dotnet build` | **PASS** — 0 errors, 0 warnings |
| Internal markdown links with a file target | **PASS** — 205/205 (v1 baseline 172/172, method reproduced) |
| Author and committer across the branch | **PASS** — `jdsaire` only, one distinct pair |
| AI product names in the working tree | **PASS** — zero hits |
| AI product names in commit messages | **PASS** — zero hits |
| AI product names in branch name, PR title, PR body | **PASS** — zero hits |
| Permitted "AI coding assistant" phrasing | **PASS** — 4 occurrences, all pre-existing |
| Security or production-readiness claims | **PASS** — zero; all five phrase matches are negations |
| Simulated label at the point of definition | **PASS** — `TokenAuthenticationMiddleware.cs:7` |
| Route surface versus `a895afe` | **PASS** — identical |
| Dependency set | **PASS** — `.csproj` byte-identical; no package added |
| Documented behaviors replayed live | **PASS** — 20/20 |
| Pull request state | **PASS** — #2 open, `mergedAt=null` |

---

## Related

- [plan.md](plan.md) — the plan as approved, before any code was written
- [README.md](README.md) — what this folder holds
- [../README.md](../README.md) — the handoff index
- [../v1/completion-report.md](../v1/completion-report.md) — the build this pass modified
- [../../docs/references.md](../../docs/references.md) — where the two ideas came from
