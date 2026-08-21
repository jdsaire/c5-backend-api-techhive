# Properties

Settings that apply when the project is launched locally. Created by the project template.

| File | What it does |
|---|---|
| `launchSettings.json` | Defines the launch profiles, including the URLs the API listens on. |

Two profiles are defined:

| Profile | Listens on |
|---|---|
| `http` | `http://localhost:5139` |
| `https` | `https://localhost:7285` and `http://localhost:5139` |

The `http` profile is the one the documentation uses, because it needs no development certificate:

```bash
dotnet run --project src/UserManagementAPI --launch-profile http
```

If port `5139` is already taken on your machine, either change it here or read the URL the console
prints on startup and use that instead.

These settings apply to local development only. They are not used when the application is
published.

## Related

- [../../../docs/how-to-run.md](../../../docs/how-to-run.md) — running the API
- [../README.md](../README.md) — the project layout
