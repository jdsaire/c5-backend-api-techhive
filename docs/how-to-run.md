# How to run the API and inspect it in Swagger UI

This API is **not hosted anywhere**. There is no public URL, no deployed instance, and no GitHub
Pages site — a Web API has no page to render, and Swagger UI needs a running server behind it,
which GitHub Pages cannot provide.

To see the API working, you run it on your own machine. It takes two commands and about a minute.
Everything below works the same on Windows, macOS, and Linux.

---

## 1. Install the prerequisite

You need the **.NET SDK, version 10.0 or newer**. Check whether you already have it:

```bash
dotnet --version
```

If that prints a version number starting with `10.`, you are ready. If the command is not found or
prints something older, install the SDK from <https://dotnet.microsoft.com/download> — see
[setup-guide.md](setup-guide.md) for more detail.

## 2. Get the code

```bash
git clone https://github.com/jdsaire/c5-backend-api-techhive.git
cd c5-backend-api-techhive
```

## 3. Start the API

```bash
dotnet run --project src/UserManagementAPI --launch-profile http
```

The first run restores packages and builds, so it takes a few seconds. When it is ready the
console prints something like:

```
Now listening on: http://localhost:5139
Application started. Press Ctrl+C to shut down.
```

**Read the URL it prints.** The port is set in
[`launchSettings.json`](../src/UserManagementAPI/Properties/launchSettings.json) and is normally
`5139`, but if another program on your machine is already using that port the console will show a
different one. Use whatever it prints.

Leave this terminal running. The API stops when you press `Ctrl+C` or close the window.

## 4. Open Swagger UI

With the API running, open this in a browser:

### 👉 **<http://localhost:5139/swagger>**

That link only works while the API is running on **your own machine** — `localhost` means "this
computer", so it will not open anything until step 3 is done. It is not a link to a server on the
internet.

You should see an interactive page listing all five endpoints.

### Using it

1. Click any endpoint to expand it — for example **GET `/users`**.
2. Click **Try it out**.
3. Fill in any parameters. For `POST` and `PUT`, edit the JSON body shown.
4. Click **Execute**.

The response body, status code, and headers appear directly underneath. This is the quickest way
to see every endpoint working without installing anything else.

The raw OpenAPI document behind that page is at <http://localhost:5139/openapi/v1.json>.

> **One thing to expect.** Requests you send from Swagger UI's **Try it out** button do not carry
> an authentication token, so calls to `/users` come back as `401 Unauthorized`. That is the
> authentication middleware doing its job. The next section shows how to send a request with a
> token.

---

## 5. Sending an authenticated request

Every `/users` route requires a bearer token. The token this project is configured with is in
[`appsettings.json`](../src/UserManagementAPI/appsettings.json):

```
techhive-local-development-token
```

That value is in version control on purpose — it is a demonstration token for a simulated check,
not a secret. See [middleware-pipeline.md](middleware-pipeline.md) for exactly what that check
does and does not do.

**With `curl`** (available by default on macOS, Linux, and Windows 10 or newer):

```bash
curl -H "Authorization: Bearer techhive-local-development-token" http://localhost:5139/users
```

Creating a user:

```bash
curl -X POST http://localhost:5139/users \
  -H "Authorization: Bearer techhive-local-development-token" \
  -H "Content-Type: application/json" \
  -d '{"name":"Priya Nair","email":"priya.nair@techhive.example","department":"IT"}'
```

**With Postman:** set the method and URL, open the **Authorization** tab, choose **Bearer Token**,
and paste the token. For `POST` and `PUT`, put the JSON in **Body → raw → JSON**.

**With an editor:** open
[`src/UserManagementAPI/UserManagementAPI.http`](../src/UserManagementAPI/UserManagementAPI.http).
Visual Studio, VS Code with the REST Client extension, and JetBrains Rider can all send those
requests directly from the file.

**Without a token**, to see the rejection:

```bash
curl -i http://localhost:5139/users
```

```
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer
{"error":"An Authorization header with a bearer token is required."}
```

---

## What to expect from the data

The API starts with three seeded users. **Everything is stored in memory**, so anything you add,
change, or delete lasts only until you stop the API. Restart it and you are back to the same three
users. That is by design — this project has no database.

---

## If something does not work

| Symptom | Cause and fix |
|---|---|
| `dotnet: command not found` | The SDK is not installed or not on your PATH. See [setup-guide.md](setup-guide.md). |
| The browser cannot reach the page | The API is not running, or it is on a different port. Check the terminal from step 3 for the URL it printed. |
| `/users` returns `401` | Expected. The request needs the bearer token — see section 5. |
| `Failed to determine the https port for redirect` in the console | Harmless. You started the `http` profile, so there is no HTTPS port to redirect to and the redirect does nothing. |
| The port is already in use | Another program has `5139`. Either stop it, or edit the port in [`launchSettings.json`](../src/UserManagementAPI/Properties/launchSettings.json). |

---

## Related

- [README.md](README.md) — index of this folder
- [setup-guide.md](setup-guide.md) — prerequisites and first-time setup in more detail
- [api-testing.md](api-testing.md) — every endpoint with sample requests and observed responses
- [middleware-pipeline.md](middleware-pipeline.md) — the token check, error handling, and logging
- [../learning-mode/README.md](../learning-mode/README.md) — plain-language walkthroughs
