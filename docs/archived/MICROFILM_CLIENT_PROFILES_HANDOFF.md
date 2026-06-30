# Microfilm Client Profiles Frontend Handoff

The backend profile API MVP is implemented for reusable Miller client profiles.

| Method | Route | Notes |
| --- | --- | --- |
| `GET` | `/api/microfilm/client-profiles` | Returns `{ "profiles": [] }`; backed by `MicrofilmClientProfilesQuery`, so normal Totem ETag/QueryHub behavior applies. |
| `POST` | `/api/microfilm/client-profiles` | Creates a global profile and returns `201 { "profile": ... }`. |
| `PUT` | `/api/microfilm/client-profiles/{profileId}` | Replaces `name`, `description`, and the full ordered `columns` array; preserves `id` and `createdAt`. |
| `DELETE` | `/api/microfilm/client-profiles/{profileId}` | Deletes from the active catalog and returns `204 No Content`. |

Profiles are global for MVP and are not tied to server, client, user, or tenant. The `columns` payload uses the exact same `MicrofilmTableColumn` shape and validation as `PUT /api/microfilm/columns/{clientId}`.

Expected structured errors include:

| Code | Status | Meaning |
| --- | --- | --- |
| `UNKNOWN_PROFILE` | 404 | Profile ID was not found. |
| `INVALID_PROFILE_NAME` | 400 | Profile name was missing or empty. |
| `DUPLICATE_PROFILE_NAME` | 409 | Profile name already exists, case-insensitively. |
| `INVALID_REQUEST` | 400 | Request body was missing. |
| `INVALID_COLUMN_ID`, `DUPLICATE_COLUMN_ID`, `UNSUPPORTED_COLUMN_TYPE`, `INVALID_COLUMN_WIDTH` | 400 | Profile columns failed the same validation used by table columns. |

No backend apply-profile command was added. To apply a profile, copy `selectedProfile.columns` into:

```http
PUT /api/microfilm/columns/{clientId}
```

Then refresh columns/rows as usual. Applying a profile should not directly mutate regular rows, custom rows, WASP boxes, or WASP-imported regular rows.
