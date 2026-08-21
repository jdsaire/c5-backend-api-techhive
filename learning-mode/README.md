# learning-mode

The same project as the rest of this repository, explained from the beginning.

These files are written for someone who can read code but has not built a web API before. They
explain what was built and **why** — in prose, not as code listings. If you already know ASP.NET
Core, [../docs/](../docs/) is the faster route: it is reference material rather than explanation.

Read them in order. Each one assumes the one before it.

| File | What it covers |
|---|---|
| [01-Building-the-CRUD-Surface.md](01-Building-the-CRUD-Surface.md) | What an API is, what CRUD means, and how the five endpoints were built. |
| [02-Validation-and-Error-Handling.md](02-Validation-and-Error-Handling.md) | Why the first version was wrong in ways that looked right, and what fixing it involved. |
| [03-Middleware-and-the-Pipeline.md](03-Middleware-and-the-Pipeline.md) | What middleware is, the three components added here, and why their order matters. |
| [Glossary.md](Glossary.md) | Every term used across these files, defined in plain language. |

Keep the [Glossary](Glossary.md) open beside them. Terms are defined there rather than repeatedly
in the text.

## Trying it while you read

These walkthroughs make far more sense with the API running in front of you. It takes two
commands and needs only the [.NET SDK 10.0+](https://dotnet.microsoft.com/download):

```bash
git clone https://github.com/jdsaire/c5-backend-api-techhive.git
cd c5-backend-api-techhive
dotnet run --project src/UserManagementAPI --launch-profile http
```

Then open **<http://localhost:5139/swagger>**, which gives you a page where you can send real
requests and watch what comes back. Nothing is hosted online — that address is your own machine,
and it only works while the command above is running. See
[../docs/how-to-run.md](../docs/how-to-run.md) if anything goes wrong.

## Related

- [../README.md](../README.md) — the project README
- [../docs/README.md](../docs/README.md) — the reference documentation
