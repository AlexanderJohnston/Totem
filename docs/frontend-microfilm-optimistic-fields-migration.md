# Frontend Microfilm optimistic-field migration

For teams that already completed `backend-integration-guide.md`: this guide **supersedes that guide's catalog/roll-columns guidance**. Do not rewrite or reapply the old catalog flow.

## Contract change

Remove every call to these deleted endpoints:

- `GET` and `PUT /api/microfilm/columns/{clientId}`
- `GET` and `PUT /api/microfilm/rolls/{rollId}/columns`

Profiles independently own presentation definitions: field membership, order, width, labels/mappings, types, and dropdown options. A profile is not a row schema or write allow-list. Fields omitted from a selected profile stay omitted; the backend does not auto-append fields or profile defaults.

Rows accept arbitrary **trimmed, non-empty** field IDs. Roll-scoped row creates and updates accept only string, finite-number, boolean, and `null` cell values. Empty IDs and object/array values are rejected.

Rows are sparse and guarantee `boxName` and `rollName` keys (with `null` when not supplied). Those cells are presentation/import data and never operation identity.

Stored cells and cell audits are durable through profile removal, re-addition, or a profile type reinterpretation. Table, list, and single-row reads return these durable values. Join each returned row to the currently selected profile in the frontend to decide columns, labels, controls, and ordering; never delete unselected values from local state or outbound payloads.

Historical column events remain replay-compatible metadata and cannot mutate durable rows.

## Endpoint examples

```http
POST /api/microfilm/rolls/{rollId}/rows
Content-Type: application/json

{ "rowId": "scan-17", "cells": { "operatorNote": "needs review", "frameCount": 12, "isPriority": true } }
```

```http
PATCH /api/microfilm/rolls/{rollId}/rows/{rowId}/cells/operatorNote
Content-Type: application/json

{ "value": null }
```

Use `GET /api/microfilm/rolls/{rollId}/rows`, `GET /api/microfilm/rolls/{rollId}/rows/{rowId}`, or `GET /api/microfilm/rolls/{rollId}/table` for all row reads, then join rows to the selected profile. Client-scoped row reads, creates, patches, routing indexes, and fallback dispatch have been removed. Manage profiles through `/api/microfilm/client-profiles`.

## Client profile selection

Each client has one optional selected presentation profile. Read it before joining rows to profile definitions:

```http
GET /api/microfilm/clients/{clientId}/profile-selection

{ "clientId": "{clientId}", "profileId": null }
```

Set a selection with a profile ID, or clear it with JSON `null`:

```http
PUT /api/microfilm/clients/{clientId}/profile-selection
Content-Type: application/json

{ "profileId": "profile-id" }
```

```http
PUT /api/microfilm/clients/{clientId}/profile-selection
Content-Type: application/json

{ "profileId": null }
```

Non-null profile IDs are trimmed and must identify an existing profile; whitespace-only IDs are rejected. The selection is a soft reference: deleting a selected profile does not clear client selections or alter rows. Frontends must handle a selected profile ID that no longer appears in `/api/microfilm/client-profiles`.

## Migration checklist

- [ ] Delete client/roll columns endpoint calls, caches, and endpoint-specific error handling.
- [ ] Delete client-scoped row reads, creates, patches, QueryHub subscriptions, mocks, and fallback logic.
- [ ] Require a stable backend `rollId` before creating or editing either regular or custom rows.
- [ ] Treat profiles as independently saved presentation definitions, not backend schemas.
- [ ] Preserve sparse cells and `cellAudits`, including fields absent from the selected profile.
- [ ] Ensure roll-row UI always handles `boxName` and `rollName` as presentation fields, never identity.
- [ ] Allow arbitrary non-empty field IDs and scalar/null values in local models; block empty IDs and object/array values before submission.
- [ ] Verify a removed, re-added, or retyped profile field shows its original stored value and audit.
- [ ] Verify table, list, and single-row reads produce the same durable roll value before profile joining.
- [ ] Read the client profile selection and handle null or a soft-deleted profile before rendering profile-driven columns.
