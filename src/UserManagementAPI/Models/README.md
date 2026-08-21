# Models

The shape of the data this API works with.

| File | What it defines |
|---|---|
| `User.cs` | A user record: `Id`, `Name`, `Email`, and `Department`. |

The model is deliberately small. Every field earns its place from the project's requirements — the
API addresses users by id, name and email are the two fields the requirements name as validation
targets, and department is what makes a record meaningful to the HR and IT departments this API
serves.

`Id` is assigned by the store when a user is created. A value sent by a client is overwritten.

## Related

- [../Storage/README.md](../Storage/README.md) — where these records are kept
- [../Validation/README.md](../Validation/README.md) — the rules applied to them
- [../README.md](../README.md) — the project layout
