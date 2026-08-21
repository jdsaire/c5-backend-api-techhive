# 1. Building the CRUD surface

*What an API is, what CRUD means, and how the five endpoints in this project were built.*

Terms in **bold** are defined in the [Glossary](Glossary.md).

---

## The problem this project solves

TechHive Solutions keeps a list of its employees. The HR department needs to add people when they
join, correct their details, and remove them when they leave. The IT department needs to look
people up.

The obvious approach — give both departments a shared spreadsheet — falls apart quickly. Two
people editing at once overwrite each other. Nothing records who changed what. And no other
software can use the data, because a spreadsheet is a file, not a service.

What both departments actually need is one place that owns the list, and a defined way to ask it
questions and give it instructions. That is an **API**.

## What an API is, concretely

An API is a program that listens for messages over the network and answers them. It has no screen
and no buttons. Something else — a website, a mobile app, a script, another server — sends it a
request, and it sends back an answer.

The messages follow **HTTP**, the same protocol a browser uses to load a page. A request has three
parts that matter here:

- a **verb** saying what kind of operation this is — `GET`, `POST`, `PUT`, `DELETE`
- a **path** saying what it applies to — `/users`, `/users/2`
- optionally a **body**, carrying data, written in **JSON**

The answer comes back with a **status code** — a three-digit number saying how it went — and
usually a JSON body.

That is the whole vocabulary. Everything below is built from it.

## CRUD

Almost anything that manages a list of records needs the same four operations:

| | Meaning | HTTP verb |
|---|---|---|
| **C**reate | Add a new record | `POST` |
| **R**ead | Look at records | `GET` |
| **U**pdate | Change an existing record | `PUT` |
| **D**elete | Remove a record | `DELETE` |

These four are called **CRUD**. HTTP has a verb for each, which is not a coincidence — HTTP was
designed around the idea of doing things to addressable resources.

This project needs five endpoints, because reading splits in two: reading the whole list, and
reading one specific person.

| Verb | Path | What it does |
|---|---|---|
| `GET` | `/users` | Give me everyone |
| `GET` | `/users/{id}` | Give me the person with this id |
| `POST` | `/users` | Here is a new person, add them |
| `PUT` | `/users/{id}` | Here are new details for this person |
| `DELETE` | `/users/{id}` | Remove this person |

`{id}` is a placeholder. `GET /users/2` asks for the user whose id is 2.

Notice the paths repeat while the verbs change. That is the intended design: **the path names the
thing, the verb says what to do to it**. `/users/2` is one particular person, and `GET`, `PUT`, and
`DELETE` are three different things you can do to them.

## What a user record looks like

Before writing any endpoint, you have to decide what a user *is*. This project settled on four
fields:

```json
{
  "id": 1,
  "name": "Ada Reyes",
  "email": "ada.reyes@techhive.example",
  "department": "HR"
}
```

Each one earns its place. `id` is how the API addresses a single person — without it, `/users/2`
means nothing. `name` and `email` are the two fields the requirements call out as needing
validation, so they have to exist for that requirement to mean anything. `department` is what makes
the record useful to the HR and IT departments this API serves.

Nothing else was added. Extra fields are easy to add later and awkward to remove once something
depends on them.

**The `id` is assigned by the API, not by the caller.** When you create a user you send a name, an
email, and a department; the API decides the id and tells you what it chose. If callers picked
their own ids, two of them would eventually pick the same one.

## Where the records are kept

They are kept **in memory** — in a variable inside the running program. There is no database.

The honest consequence: **stop the API and the data is gone.** Start it again and it is back to
the same three example users it starts with.

That sounds like a flaw and is worth being clear about. For this project it is the right choice.
Adding a database means adding a database engine, a connection, a schema, a way to describe those
tables in code, and a way to keep the two in step — a large amount of machinery, none of which
teaches anything about how an API behaves. The requirements never mention storing data
permanently. So the storage here is the simplest thing that lets the endpoints be real, and the
attention goes to the endpoints.

Swapping it for a real database later means rewriting one file. That is the point of keeping it
behind its own class.

## How the code is arranged

Five files, each with one job:

```
Program.cs      starts the application and switches features on
Models/         what a user is
Storage/        where users are kept
Endpoints/      the five routes
```

The arrangement matters more than it looks. `Program.cs` is about fifty lines and contains no
business logic at all — it says which features are switched on and in what order, and stops there.
Someone opening this project can read `Program.cs` in a minute and know what the application
consists of, then go to the folder for whichever part they care about.

The alternative — writing all five route handlers directly in `Program.cs` — works fine at five
endpoints and becomes unreadable at twenty. The habit is worth forming while the project is small.

The endpoints are attached to the application by a single line in `Program.cs`:

```csharp
app.MapUserEndpoints();
```

Everything that phrase implies lives in `Endpoints/UserEndpoints.cs`.

## What each endpoint returns

The status code is not decoration. It is the part of the answer that other software reads first,
and choosing it correctly is most of what makes an API predictable.

**`GET /users`** returns `200 OK` and an array. If nobody is in the directory it returns an empty
array — still `200`, because the question "who is in the directory?" was answered successfully.
"Nobody" is a valid answer.

**`GET /users/{id}`** returns `200 OK` and one user.

**`POST /users`** returns `201 Created`, not `200`. There is a specific code meaning "your request
succeeded and something new now exists", and using it tells the caller more than `200` does. It
also sends a `Location` header containing the path of the new record — `/users/4` — so the caller
knows the id that was assigned without having to dig it out of the body.

**`PUT /users/{id}`** returns `200 OK` and the updated record. Returning the record matters:
the caller sees exactly what was stored, rather than assuming their request was applied verbatim.

**`DELETE /users/{id}`** returns `204 No Content`. The deletion succeeded, and there is nothing
sensible to send back — the thing you would describe no longer exists. `204` means precisely that:
success, no body.

## Testing it, and what testing found

Writing an endpoint is not evidence that it works. Every one of the five was run against a live
instance and the actual responses recorded — those are in
[../docs/api-testing.md](../docs/api-testing.md), copied from the terminal rather than written
from memory.

All five happy paths worked. But the same testing session found four things that were wrong, and
they only appeared because the tests included requests that were *supposed* to fail:

- Asking for a user who does not exist returned `200 OK` with a body of `null` — success, plus
  nothing.
- Updating a user who does not exist did the same.
- Deleting a user who does not exist returned `204`, identical to a real deletion.
- Creating a user with an empty name and `"not-an-email"` as the email returned `201 Created`.

None of these crash. Every one of them looks fine until a caller relies on it. They are the
subject of [the next walkthrough](02-Validation-and-Error-Handling.md).

The lesson is worth stating plainly: **testing only the cases you expect to work will not tell you
your API is wrong.** All five endpoints passed their happy-path tests while doing all four of
those things.

## Try it

With the API running (see [../docs/how-to-run.md](../docs/how-to-run.md)), open
<http://localhost:5139/swagger> — a page listing all five endpoints where you can send a real
request and see the real answer.

Try `GET /users` first. Then create someone with `POST`, and notice the id you get back and the
`Location` header. Then `GET` that id. Then delete it and `GET` it again.

That last step is the interesting one, and it is where the next walkthrough begins.

---

**Next:** [2. Validation and error handling](02-Validation-and-Error-Handling.md) ·
[Glossary](Glossary.md) · [Back to learning-mode](README.md)
