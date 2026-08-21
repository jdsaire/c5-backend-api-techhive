# Storage

Where the user records are kept while the API is running.

| File | What it does |
|---|---|
| `UserStore.cs` | Holds the records in memory and provides read, add, update, and delete operations. |

**There is no database.** The store is a dictionary keyed by user id, registered as a singleton so
every request shares one instance. Records live as long as the process does — stop the API and the
data is gone; start it again and it returns to its three seeded users. That is expected for this
project and is not a limitation to work around.

The collection is keyed by id because every route that addresses a single user does so by id, so a
lookup goes straight to the record instead of scanning. It is a concurrent dictionary because the
store is shared across requests, and a plain list would allow two simultaneous requests to corrupt
it. That reasoning, and an honest account of what it is worth at this data scale, is in
[../../../docs/debugging-notes.md](../../../docs/debugging-notes.md).

## Related

- [../Models/README.md](../Models/README.md) — the shape of what is stored
- [../Endpoints/README.md](../Endpoints/README.md) — what calls into this store
- [../README.md](../README.md) — the project layout
