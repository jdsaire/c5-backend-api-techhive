# handoff / v2

A small hardening pass on the finished v1 API, driven by reviewing another student's submission of
the same assignment.

Two code changes, and nothing else: the bearer-token comparison was made constant-time, and the
create and update handlers were given a working check for a request body that was never sent. No
new endpoint, no new field, no new capability, no change to the route surface, no new dependency.

| File | What it is |
|---|---|
| [plan.md](plan.md) | The plan **as approved before any code was written**, not edited to match the outcome. Two lines were reworded to avoid naming AI products in the repository; that is disclosed in the report. |
| [completion-report.md](completion-report.md) | What actually happened: the commit list, results against every success criterion, the two deviations and why they were taken, the decisions made without asking, and the items left open. |

**Read the completion report first** if you want the state of the project.

## The short version

Six commits, gathered into pull request **#2**, which is open and unmerged — merging it is a
manual step. The build was clean after every one. Both invariants held: the route surface is
identical to `a895afe`, and no AI product is named anywhere. Two deviations were taken and
disclosed.

The finding worth knowing about is in the report's §4.1. The null-body check this pass set out to
add would never have executed as specified — the framework was already rejecting those requests
during parameter binding, before any handler ran. The check had to be made reachable before it
could do anything, and the real defect turned out to be a misleading error message rather than a
missing guard.

## Related

- [completion-report.md](completion-report.md) · [plan.md](plan.md)
- [../README.md](../README.md) — the handoff index
- [../v1/README.md](../v1/README.md) — the original build
- [../../docs/references.md](../../docs/references.md) — the peer submission that informed this pass
- [../../learning-mode/04-Learning-From-a-Peer-Review.md](../../learning-mode/04-Learning-From-a-Peer-Review.md) — the same material explained from scratch
