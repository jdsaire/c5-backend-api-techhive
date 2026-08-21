# docs

Everything about how this API works, how to run it, and how it was tested.

These are the reference documents. For the same material explained from scratch, in prose, for
someone who has not worked with web APIs before, see [../learning-mode/README.md](../learning-mode/README.md).

## What is here

| File | What it covers |
|---|---|
| [how-to-run.md](how-to-run.md) | **Start here.** Running the API and opening Swagger UI in your browser, plus sending an authenticated request. |
| [setup-guide.md](setup-guide.md) | Prerequisites, first-time setup, the packages used, and the project layout. |
| [api-testing.md](api-testing.md) | Every endpoint with its verb, path, sample request, and the response actually observed from a running instance. |
| [debugging-notes.md](debugging-notes.md) | Each bug that was reported or found, what the code did before, what it does now, and the edge cases run to confirm the fix. |
| [middleware-pipeline.md](middleware-pipeline.md) | The three middleware components, the order they run in, why that order was chosen, and the test results. |
| [grading-criteria.md](grading-criteria.md) | The assignment's criteria, recorded for reference. It records them; it does not answer them. |

## A note on the evidence in these files

Every request and response quoted in `api-testing.md`, `debugging-notes.md`, and
`middleware-pipeline.md` was captured from a running instance of this API. None of it is
illustrative or written from memory. Where a test required deliberately breaking something — such
as triggering an exception — the exact change is shown so the test can be repeated.

## Related

- [../README.md](../README.md) — the project README
- [../learning-mode/Glossary.md](../learning-mode/Glossary.md) — plain-language definitions
