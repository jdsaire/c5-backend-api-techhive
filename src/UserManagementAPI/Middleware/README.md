# Middleware

Components every request passes through before it reaches an endpoint, and that every response
passes back through on the way out.

| File | What it does |
|---|---|
| `ErrorHandlingMiddleware.cs` | Catches exceptions that escape everything else and returns a consistent JSON error. |
| `TokenAuthenticationMiddleware.cs` | Checks for a valid bearer token and rejects the request with `401` if it is missing or wrong. |
| `RequestLoggingMiddleware.cs` | Logs the HTTP method, the path, and the response status code for each request. |

They are registered in `Program.cs` in this order, which is the order the project's requirements
specify:

```
error handling  →  authentication  →  logging  →  the endpoints
```

Order matters, because each component wraps the ones registered after it. Error handling is
outermost so that everything below it is inside its `try` block. Logging is innermost so the status
code it records is the one the endpoint actually produced.

One consequence worth knowing: because logging comes after authentication, a request rejected with
`401` never reaches the logging middleware and produces no `responded` line. The authentication
middleware logs its own rejections for that reason. This and the rest of the reasoning is in
[../../../docs/middleware-pipeline.md](../../../docs/middleware-pipeline.md).

> **The token check is simulated.** It compares a bearer token against a fixed configured value.
> It verifies no signature, issues no tokens, and has no expiry. It is not production
> authentication. The full statement is at the top of `TokenAuthenticationMiddleware.cs`.

## Related

- [../../../docs/middleware-pipeline.md](../../../docs/middleware-pipeline.md) — the pipeline and its test results
- [../Endpoints/README.md](../Endpoints/README.md) — the endpoints these components wrap
- [../README.md](../README.md) — the project layout
