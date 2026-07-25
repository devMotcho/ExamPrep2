# `user-registered` (v1)

**Producer:** Auth.Api
**Consumers:** Exams.Api (creates local `user_projection` row), Analytics.Api (inserts `user_lifecycle` row)
**Fires when:** A new user completes registration.

## Fields

| Field | Type | Description |
|---|---|---|
| `Id` | string | User id (matches `AspNetUsers.Id`) |
| `Email` | string | User's email at registration time |
| `CreatedAt` | ISO 8601 datetime (UTC) | Registration timestamp |

## Example

```json
{
  "Id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "Email": "test@example.com",
  "CreatedAt": "2026-07-24T10:15:00Z"
}
```