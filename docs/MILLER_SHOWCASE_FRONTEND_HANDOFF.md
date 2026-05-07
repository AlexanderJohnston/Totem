# Miller Showcase Frontend Integration Handoff

This handoff describes the backend APIs now available for the Miller Showcase table workflow at `/#/miller-showcase`.

The backend keeps the existing Microfilm selector payload shapes for successful responses and adds the missing table APIs for columns, regular rows, custom rows, and cell persistence. Unknown selector/table IDs now return JSON error envelopes instead of plain/default query responses.

## Integration summary

Use the existing selector-first flow:

1. Load servers with `GET /api/microfilm/servers`.
2. After server selection, load clients with `GET /api/microfilm/clients/{serverId}`.
3. After client selection, load table data:
   - `GET /api/microfilm/columns/{clientId}`
   - `GET /api/microfilm/rows/{clientId}`
   - `GET /api/microfilm/custom-rows/{clientId}`
4. Render regular rows first and custom rows after regular rows.
5. Persist table edits through the write APIs below and trust the returned payload as the authoritative updated state.

Base path:

```text
/api/microfilm
```

All request and response bodies are JSON.

## Backend decisions and completion status

The backend blocker/open-question items are resolved for this integration:

| Concern | Decision |
| --- | --- |
| Schema storage | Column schemas are stored as durable Totem timeline state per client. `PUT /columns/{clientId}` replaces the full persisted schema and returns the authoritative schema. |
| Regular row source/mapping | Regular rows are generic backend table rows for this feature, independent of Box/Roll domain rows. Do not infer Box/Roll meaning from row IDs or cells unless a future backend importer explicitly adds that mapping. |
| Custom row persistence | Custom rows are persisted separately from regular rows as durable table state. They use backend-issued stable IDs and are returned through `customRows`. |
| Seed data | The current Miller demo client is configured with durable seed data: starter columns and one regular row. Known clients without seed data return empty arrays, not 404. |

Completion criteria for this stage are met from the frontend contract perspective:

- The backend team has made the required data ownership decisions.
- The selector flow remains compatible with the current frontend normalizers.
- Missing table endpoints are now defined for columns, regular rows, custom rows, and write persistence.
- Error semantics are documented with JSON envelopes and stable error codes.
- Edge cases and QA smoke checks are listed below.

## Seeded demo client

The backend service is configured to seed the current Miller demo client through durable timeline seed commands/events:

```text
serverId: 2945ffb4-da10-40ca-ad31-c1228f578f8a
clientId: e83ddf61-1dd2-4584-8def-762338863a59
label: UniversityofDE (202618852)
seedId: miller-showcase-demo
```

Seeded columns:

```json
[
  { "id": "boxName", "name": "Box", "type": "text", "width": 160, "dropdownOptions": [] },
  { "id": "rollName", "name": "Roll", "type": "text", "width": 160, "dropdownOptions": [] },
  {
    "id": "status",
    "name": "Status",
    "type": "dropdown",
    "width": 160,
    "dropdownOptions": ["New", "In Progress", "Done"]
  },
  { "id": "reviewed", "name": "Reviewed", "type": "checkbox", "width": 120, "dropdownOptions": [] }
]
```

Seeded regular row:

```json
{
  "id": "row-1",
  "origin": "regular",
  "cells": {
    "boxName": "Box 01",
    "rollName": "Roll A",
    "status": "New",
    "reviewed": false
  }
}
```

Seed notes:

- Seed state is durable timeline state, not live read-time config.
- If a known client has no seed/schema yet, table endpoints return `200` with empty arrays, not `404`.
- The configured seed is idempotent: once table state exists, seed commands do not overwrite user-edited schema or rows.

## Selector endpoints

### `GET /api/microfilm/servers`

Existing backend shape remains:

```json
{
  "servers": [
    {
      "serverName": "\\\\sbsr-film\\film\\",
      "serverId": "2945ffb4-da10-40ca-ad31-c1228f578f8a"
    }
  ],
  "whenCreated": "0001-01-01T00:00:00+00:00",
  "whenChanged": "2026-04-22T11:11:59.3237252-04:00"
}
```

Frontend should continue normalizing:

```ts
id = server.serverId
name = server.serverName
```

### `GET /api/microfilm/clients/{serverId}`

Existing successful backend shape remains:

```json
{
  "clients": [
    {
      "jobName": "UniversityofDE",
      "jobNumber": "202618852",
      "clientId": "e83ddf61-1dd2-4584-8def-762338863a59",
      "serverId": "2945ffb4-da10-40ca-ad31-c1228f578f8a"
    }
  ]
}
```

Frontend should continue normalizing:

```ts
id = client.clientId
serverId = client.serverId
name = `${client.jobName} (${client.jobNumber})`
```

Known server with no clients:

```json
{
  "clients": []
}
```

Unknown server returns `404`:

```json
{
  "error": {
    "code": "UNKNOWN_SERVER",
    "message": "Server was not recognized.",
    "details": {
      "serverId": "unknown-server-id"
    }
  }
}
```

## Table read endpoints

### `GET /api/microfilm/columns/{clientId}`

Returns the current authoritative column schema.

Response `200`:

```json
{
  "columns": [
    {
      "id": "boxName",
      "name": "Box",
      "type": "text",
      "dropdownOptions": [],
      "width": 160
    },
    {
      "id": "status",
      "name": "Status",
      "type": "dropdown",
      "dropdownOptions": ["New", "In Progress", "Done"],
      "width": 160
    }
  ]
}
```

Known client with no configured columns:

```json
{
  "columns": []
}
```

Unknown client returns `404` with `UNKNOWN_CLIENT`.

### `GET /api/microfilm/rows/{clientId}`

Returns backend-owned regular rows.

Response `200`:

```json
{
  "rows": [
    {
      "id": "row-1",
      "origin": "regular",
      "cells": {
        "boxName": "Box 01",
        "rollName": "Roll A",
        "status": "New",
        "reviewed": false
      }
    }
  ]
}
```

Known client with no regular rows:

```json
{
  "rows": []
}
```

Unknown client returns `404` with `UNKNOWN_CLIENT`.

### `GET /api/microfilm/custom-rows/{clientId}`

Returns user-created custom rows.

Response `200`:

```json
{
  "customRows": [
    {
      "id": "a-backend-issued-id",
      "origin": "custom",
      "cells": {
        "boxName": "Manual row",
        "rollName": null,
        "status": null,
        "reviewed": false
      }
    }
  ]
}
```

Known client with no custom rows:

```json
{
  "customRows": []
}
```

Unknown client returns `404` with `UNKNOWN_CLIENT`.

## Table write endpoints

Write responses should be treated as authoritative. Update frontend local state from the returned `columns` or `row` payload rather than assuming the request body is exactly what persisted.

### `PUT /api/microfilm/columns/{clientId}`

Replaces the full column schema for the client.

Request:

```json
{
  "columns": [
    {
      "id": "boxName",
      "name": "Archive Box",
      "type": "text",
      "width": 180
    },
    {
      "id": "status",
      "name": "Status",
      "type": "dropdown",
      "dropdownOptions": ["New", "Done"],
      "width": 160
    }
  ]
}
```

Response `200`:

```json
{
  "columns": [
    {
      "id": "boxName",
      "name": "Archive Box",
      "type": "text",
      "dropdownOptions": [],
      "width": 180
    },
    {
      "id": "status",
      "name": "Status",
      "type": "dropdown",
      "dropdownOptions": ["New", "Done"],
      "width": 160
    }
  ]
}
```

Rules:

- This is a full replacement. Always send the full ordered column array.
- Array order is persisted and should be used for table order.
- `width` must be positive when present.
- Supported `type` values are `text`, `number`, `dropdown`, and `checkbox`.
- `id` values must be unique and non-empty per client.
- For non-dropdown columns, backend returns/keeps `dropdownOptions` as an empty array.
- If columns are removed, subsequent row projections only include cells for current columns.

Expected errors:

- Unknown client: `404 UNKNOWN_CLIENT`
- Duplicate column ID: `400 DUPLICATE_COLUMN_ID`
- Empty/missing column ID: `400 INVALID_COLUMN_ID`
- Unsupported type: `400 UNSUPPORTED_COLUMN_TYPE`
- Invalid width: `400 INVALID_COLUMN_WIDTH`

### `PATCH /api/microfilm/rows/{clientId}/{rowId}`

Updates one cell on a regular row.

Request:

```json
{
  "columnId": "status",
  "value": "Done"
}
```

Response `200`:

```json
{
  "row": {
    "id": "row-1",
    "origin": "regular",
    "cells": {
      "boxName": "Box 01",
      "rollName": "Roll A",
      "status": "Done",
      "reviewed": false
    }
  }
}
```

Expected errors:

- Unknown client: `404 UNKNOWN_CLIENT`
- Unknown row: `404 UNKNOWN_ROW`
- Unknown column: `400 UNKNOWN_COLUMN`
- Invalid value/type: `400 INVALID_CELL_VALUE`

### `POST /api/microfilm/custom-rows/{clientId}`

Creates a custom row.

Request body may be omitted. If sent, it may include initial cells:

```json
{
  "cells": {
    "boxName": "Manual row"
  }
}
```

Response `201`:

```json
{
  "row": {
    "id": "backend-issued-stable-id",
    "origin": "custom",
    "cells": {
      "boxName": "Manual row",
      "rollName": null,
      "status": null,
      "reviewed": false
    }
  }
}
```

Default cell initialization:

| Column type | Default |
| --- | --- |
| `text` | `null` |
| `number` | `null` |
| `dropdown` | `null` |
| `checkbox` | `false` |

Expected errors:

- Unknown client: `404 UNKNOWN_CLIENT`
- Unknown initial cell column: `400 UNKNOWN_COLUMN`
- Invalid initial value/type: `400 INVALID_CELL_VALUE`

### `PATCH /api/microfilm/custom-rows/{clientId}/{rowId}`

Updates one cell on a custom row.

Request:

```json
{
  "columnId": "reviewed",
  "value": true
}
```

Response `200`:

```json
{
  "row": {
    "id": "backend-issued-stable-id",
    "origin": "custom",
    "cells": {
      "boxName": "Manual row",
      "rollName": null,
      "status": null,
      "reviewed": true
    }
  }
}
```

Expected errors:

- Unknown client: `404 UNKNOWN_CLIENT`
- Unknown custom row: `404 UNKNOWN_ROW`
- Unknown column: `400 UNKNOWN_COLUMN`
- Invalid value/type: `400 INVALID_CELL_VALUE`

## Cell value rules

Cell values are raw JSON scalar values. Do not wrap them in `{ kind, value }`.

| Column type | Accepted values |
| --- | --- |
| `text` | string or `null` |
| `number` | finite JSON number or `null` |
| `dropdown` | string contained in `dropdownOptions`, or `null` |
| `checkbox` | boolean |

Examples:

```json
{ "columnId": "boxName", "value": "Box 02" }
```

```json
{ "columnId": "rollCount", "value": 42 }
```

```json
{ "columnId": "status", "value": "Done" }
```

```json
{ "columnId": "reviewed", "value": false }
```

Invalid examples:

```json
{ "columnId": "reviewed", "value": null }
```

```json
{ "columnId": "status", "value": "Archived" }
```

```json
{ "columnId": "boxName", "value": { "text": "Box 01" } }
```

## Error envelope

Expected API errors return:

```json
{
  "error": {
    "code": "UNKNOWN_CLIENT",
    "message": "Client was not recognized.",
    "details": {
      "clientId": "e83ddf61-1dd2-4584-8def-762338863a59"
    }
  }
}
```

Frontend handling guidance:

- Use `error.message` for displayable messages.
- Use `error.code` for branching/retry logic.
- Treat `details` as optional diagnostic context.
- Do not parse plain-text error bodies for expected Microfilm table errors.

Known error codes:

| Code | Typical status | Meaning |
| --- | --- | --- |
| `UNKNOWN_SERVER` | 404 | Server ID is not known. |
| `UNKNOWN_CLIENT` | 404 | Client ID is not known. |
| `UNKNOWN_ROW` | 404 | Row ID is not known for the requested row collection. |
| `UNKNOWN_COLUMN` | 400 | Column ID is not in the current schema. |
| `INVALID_REQUEST` | 400 | Required request body was missing. |
| `INVALID_COLUMN_ID` | 400 | Column ID was missing/empty. |
| `DUPLICATE_COLUMN_ID` | 400 | Column ID appears more than once in a schema PUT. |
| `UNSUPPORTED_COLUMN_TYPE` | 400 | Column type is not supported. |
| `INVALID_COLUMN_WIDTH` | 400 | Width is not positive/finite. |
| `INVALID_CELL_VALUE` | 400 | Cell value does not match the current column definition. |

## Frontend best practices

- Load `columns`, `rows`, and `customRows` after client selection. If any table read returns `404 UNKNOWN_CLIENT`, clear the table and prompt the user to reselect a client.
- Use backend row IDs exactly as returned. Do not synthesize permanent IDs on the frontend.
- For custom row creation, optimistic temporary IDs are fine, but replace the temp row with the backend `row.id` from the `POST` response.
- For cell edits, update local state from the returned full `row`, not just the edited value.
- For column edits, update local state from the returned full `columns` array.
- Debounced column reorder/resize saves should still send the full column array.
- Avoid sending writes before `columns` has loaded; writes validate against the current backend schema.
- If a schema PUT removes or renames columns, refetch or reconcile row data because backend row projections will only include current schema columns.
- Since strict optimistic concurrency is not implemented yet, treat this iteration as last-write-wins. Avoid firing overlapping writes for the same row/column if possible.
- GET responses may include ETags from Totem query serving. Browser/proxy caching can use them, but frontend logic should not require ETags for writes in this iteration.
- Regular rows are generic backend table rows for this feature. Do not assume they are Box/Roll domain rows unless a future backend importer explicitly maps them.
- No delete endpoint is currently exposed for columns, regular rows, or custom rows. Column removal is done by sending a replacement columns array without the removed column.

## Edge cases to cover in QA

1. Known server with no clients returns `200` and an empty `clients` array.
2. Unknown server for clients returns `404 UNKNOWN_SERVER`.
3. Known client with no configured columns returns `200` and `{ "columns": [] }`.
4. Known client with no regular rows returns `200` and `{ "rows": [] }`.
5. Known client with no custom rows returns `200` and `{ "customRows": [] }`.
6. Unknown client on any table read/write returns `404 UNKNOWN_CLIENT`.
7. Duplicate column IDs are rejected on `PUT /columns`.
8. Unsupported column types are rejected on `PUT /columns`.
9. Invalid dropdown values are rejected on row PATCH.
10. Checkbox values must be boolean; `null` is rejected for checkbox PATCH.
11. `POST /custom-rows` returns a backend-issued stable row ID and full initialized row.
12. Refresh after successful column rename/reorder/resize preserves backend-returned schema.
13. Refresh after successful regular/custom cell edit preserves backend-returned row state.

## Recommended live smoke test

1. Start backend service and web host so the timeline is running and the configured seed can be emitted.
2. Start the frontend dev server.
3. Open `http://127.0.0.1:9000/#/miller-showcase`.
4. Select server `\\sbsr-film\film\` or the current available server.
5. Select client `UniversityofDE (202618852)` if present.
6. Confirm table reads do not return `404`.
7. Confirm seeded columns render: Box, Roll, Status, Reviewed.
8. Confirm seeded row renders.
9. Edit a text cell and refresh.
10. Edit dropdown status and refresh.
11. Toggle Reviewed and refresh.
12. Add a custom row and refresh.
13. Rename/reorder/resize a column and refresh.

## Implementation notes for frontend maintainers

- Successful selector response shapes are intentionally unchanged for compatibility.
- Table response shapes are frontend-native and should not require domain-specific Microfilm knowledge.
- The backend is authoritative for validation. Keep frontend validation for UX, but always handle backend rejection envelopes.
- Durable table state lives in Totem timeline events. A short delay between a write response and a subsequent query refresh is possible in event-sourced systems; prefer applying the write response directly to local state, then refetch only when necessary.
