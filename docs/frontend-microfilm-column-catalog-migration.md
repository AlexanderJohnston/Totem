# Frontend Microfilm column-catalog migration

This supplements (and does not replace) `backend-integration-guide.md`.

## What changed

- A **client's Microfilm columns** (`GET`/`PUT /api/microfilm/columns/{clientId}`) are the single reusable field catalog for every roll owned by that client.
- `boxName` and `rollName` are always present in roll table responses. If absent from the client catalog, the API supplies baseline text definitions.
- Profiles are presentation-only saved layouts/mappings. They do not assign a schema to a client or roll and do not control write eligibility.
- Field values and per-field audits are durable. Removing a field from the active catalog/profile hides it from the active catalog; it does **not** remove `cells[fieldId]` or `cellAudits[fieldId]`. Re-adding the same ID exposes its prior value and audit.

## Required frontend changes

1. Load the client catalog before rendering or editing a roll. Use it for visible columns, labels, type controls, and frontend validation.
2. Treat roll table/rows/single-row reads as durable row data. Do not delete unknown or currently hidden keys during client-side normalization or save.
3. Use canonical roll writes (`POST /api/microfilm/rolls/{rollId}/rows`, `POST .../custom-rows`, and `PATCH .../cells/{columnId}`). The API resolves the current client catalog and validates the write against it.
4. Legacy client PATCH routes that dispatch to a migrated roll use the same client-catalog validation and retain the existing `{ row }` response envelope.
5. Keep profile selection and column ordering/visibility in frontend state. Do not send or expect a profile assignment on a roll.

## Read behavior

- `GET /api/microfilm/rolls/{rollId}/columns` and `GET .../table` return the effective client catalog plus baseline roll fields.
- Roll row list, single-row, table, and legacy client row projections return stored values, including values for inactive fields. Frontend visibility determines what is displayed.

## Deprecated roll columns compatibility route

`PUT /api/microfilm/rolls/{rollId}/columns` remains temporarily for integrated clients, but it now replaces the **owning client catalog**. It does not create a roll-local schema. Its response remains roll-shaped for compatibility, but the change affects all rolls for that client. Migrate callers to `PUT /api/microfilm/columns/{clientId}` and remove any roll-specific schema assumptions.

Historical `RollMicrofilmTableColumnsChanged` events are replay-compatible only; they do not overwrite client catalog projections or reconcile unrelated rows.

## Migration checklist

- [ ] Replace roll-local column state with client catalog state keyed by `clientId`.
- [ ] Preserve hidden cell and audit keys in local caches and outbound row models.
- [ ] Move profile handling to presentation-only visibility/order/mapping.
- [ ] Switch roll-column PUT callers to the client columns endpoint.
- [ ] Use canonical roll PATCH routes for targeted edits; retain legacy PATCH only while needed.
- [ ] Verify remove/re-add of a column ID restores the previous value and audit.