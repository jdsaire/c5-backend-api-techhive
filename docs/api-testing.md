# API endpoints and Activity 1 testing

This file documents every endpoint the User Management API exposes and records the results of
testing each one. **Every request and response below was captured from a running instance of this
API** — nothing here is illustrative or invented.

Tested against the `http` launch profile at `http://localhost:5139`, with the store in its seeded
state at the start of the run.

> **The `localhost` links in this file are not public URLs.** This API is not hosted anywhere.
> `localhost` means "this computer", so those links do nothing until you start the API on your own
> machine. Two commands do it:
>
> ```bash
> git clone https://github.com/jdsaire/c5-backend-api-techhive.git
> cd c5-backend-api-techhive
> dotnet run --project src/UserManagementAPI --launch-profile http
> ```
>
> Then <http://localhost:5139/swagger> opens the interactive Swagger UI, where you can send every
> request below yourself and compare what you get against what is recorded here. Full instructions
> are in [how-to-run.md](how-to-run.md).

---

## The endpoints

| Verb | Path | Purpose | Success response |
|---|---|---|---|
| `GET` | `/users` | Retrieve the list of users | `200 OK` — array of user objects |
| `GET` | `/users/{id}` | Retrieve a single user by id | `200 OK` — one user object |
| `POST` | `/users` | Add a new user | `201 Created` + `Location` header — the created user |
| `PUT` | `/users/{id}` | Update an existing user's details | `200 OK` — the updated user |
| `DELETE` | `/users/{id}` | Remove a user by id | `204 No Content` — no body |

A user object has four fields:

```json
{ "id": 1, "name": "Ada Reyes", "email": "ada.reyes@techhive.example", "department": "HR" }
```

`id` is assigned by the store when the user is created. Clients never supply it — a value sent in
a `POST` body is overwritten.

---

## How to exercise the endpoints

### Through Swagger UI

Start the API and open **<http://localhost:5139/swagger>**. Every endpoint is listed there with
its parameters and schema. Expand one, choose **Try it out**, fill in the fields, and choose
**Execute** — the response body, status code, and headers appear directly beneath. This is the
quickest way to see the API working without any other tool installed.

The raw OpenAPI document that drives that page is served at
**<http://localhost:5139/openapi/v1.json>**.

### Through an HTTP client

[`../src/UserManagementAPI/UserManagementAPI.http`](../src/UserManagementAPI/UserManagementAPI.http)
holds a ready-to-send request for each endpoint. Visual Studio, VS Code with the REST Client
extension, and JetBrains Rider can all send the requests in that file directly. The same requests
work unchanged in Postman — set the method and URL, and paste the JSON body where one is shown.

Every example below is also a working `curl` command.

---

## Observed results

### 1. Retrieve the list of users

```bash
curl http://localhost:5139/users
```

```
HTTP/1.1 200 OK
```
```json
[
  { "id": 1, "name": "Ada Reyes", "email": "ada.reyes@techhive.example", "department": "HR" },
  { "id": 2, "name": "Marcus Boateng", "email": "marcus.boateng@techhive.example", "department": "IT" },
  { "id": 3, "name": "Lena Fischer", "email": "lena.fischer@techhive.example", "department": "Engineering" }
]
```

### 2. Retrieve a single user by id

```bash
curl http://localhost:5139/users/2
```

```
HTTP/1.1 200 OK
```
```json
{ "id": 2, "name": "Marcus Boateng", "email": "marcus.boateng@techhive.example", "department": "IT" }
```

### 3. Add a new user

```bash
curl -X POST http://localhost:5139/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Priya Nair","email":"priya.nair@techhive.example","department":"IT"}'
```

```
HTTP/1.1 201 Created
Location: /users/4
```
```json
{ "id": 4, "name": "Priya Nair", "email": "priya.nair@techhive.example", "department": "IT" }
```

The store assigned id `4`, and the `Location` header points at the new record.

### 4. Update an existing user's details

```bash
curl -X PUT http://localhost:5139/users/4 \
  -H "Content-Type: application/json" \
  -d '{"name":"Priya Nair","email":"priya.nair@techhive.example","department":"Engineering"}'
```

```
HTTP/1.1 200 OK
```
```json
{ "id": 4, "name": "Priya Nair", "email": "priya.nair@techhive.example", "department": "Engineering" }
```

The department changed from `IT` to `Engineering`; the id was preserved.

### 5. Remove a user by id

```bash
curl -X DELETE http://localhost:5139/users/4
```

```
HTTP/1.1 204 No Content
```

A follow-up `GET /users` returned the three seeded users, confirming user `4` was gone.

---

## What the tests exposed

Testing did not only confirm the happy paths. Three responses were wrong, and are recorded here as
observed so the fixes can be measured against them.

### Retrieving a user that does not exist returns `200 OK` with `null`

```bash
curl http://localhost:5139/users/99
```

```
HTTP/1.1 200 OK
```
```json
null
```

A caller asking for a user who is not in the directory is told the request succeeded and handed an
empty answer. This matches the reported problem that "errors occurred when retrieving non-existent
users."

### Updating a user that does not exist also returns `200 OK` with `null`

```bash
curl -X PUT http://localhost:5139/users/99 \
  -H "Content-Type: application/json" \
  -d '{"name":"Nobody","email":"nobody@techhive.example","department":"IT"}'
```

```
HTTP/1.1 200 OK
```
```json
null
```

Nothing was written, but the caller was told the update succeeded.

### Deleting a user that does not exist returns `204 No Content`

```bash
curl -X DELETE http://localhost:5139/users/99
```

```
HTTP/1.1 204 No Content
```

Identical to a successful delete. The caller cannot tell the two apart.

### Any user data is accepted, valid or not

```bash
curl -X POST http://localhost:5139/users \
  -H "Content-Type: application/json" \
  -d '{"name":"","email":"not-an-email","department":""}'
```

```
HTTP/1.1 201 Created
```
```json
{ "id": 5, "name": "", "email": "not-an-email", "department": "" }
```

A user with no name and an email address that is not an email address was stored without
complaint. This matches the reported problem that "users were being added without proper
validation."

All four of these are addressed in Activity 2 and recorded in
[debugging-notes.md](debugging-notes.md).

### One case that already behaves correctly

A non-integer id does not reach the handler at all:

```bash
curl http://localhost:5139/users/abc
```

```
HTTP/1.1 404 Not Found
```

The `{id:int}` route constraint means the request never matches a route, so the framework returns
`404` before any of this project's code runs. No fix is needed.

---

## How this code was produced

The endpoints in this project were written with the help of an AI coding assistant, and then read,
corrected, and tested by hand. Its most useful contributions to Activity 1 were:

- **Scaffolding the shape quickly.** The route group, the extension-method registration pattern,
  and the `Results.Created` / `Results.NoContent` result helpers were suggested rather than looked
  up, which kept the focus on the API's behavior instead of on syntax.
- **Consistency across five handlers.** Once the first handler was settled, the remaining four
  followed the same argument order and naming without drift.
- **Surfacing the `{id:int}` route constraint**, which is what turns a malformed id into a clean
  `404` instead of a parsing failure inside the handler.

What it did not do is decide correctness. Every response in this file was verified against a
running instance, and that testing is exactly what exposed the four defects listed above — all of
which were present in the generated code and none of which the tool flagged on its own.

---

## Related

- [README.md](README.md) — index of this folder
- [how-to-run.md](how-to-run.md) — starting the API locally
- [debugging-notes.md](debugging-notes.md) — the Activity 2 fixes to the defects above
- [middleware-pipeline.md](middleware-pipeline.md) — the Activity 3 middleware
