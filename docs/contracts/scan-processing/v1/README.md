# Scan and Processing Operation Context Contract v1

This package defines the read-only context needed to open Scan or Process for one Miller row.

## Endpoint

`GET /api/microfilm/rolls/{rollId}/rows/{rowId}/operation-context`

The endpoint resolves the canonical durable roll, box, client, and exact composite row. It never derives identity from `cells.rollName`, profile membership, visible columns, or any other presentation value.

A successful response is `200 OK` with `OperationRowContext`. The checked-in regular and custom JSON files are deterministic examples of the wire format. Both origins use the same identity and capability rules. `eligibleActions` identifies workflows that can be opened in the current state: Scan remains available for scan status/continuation, while Process is omitted during `starting`, `active`, and `finishing`.

`isScanning` is a JSON Boolean derived only from durable `scanState`:

- `idle` -> `false`
- `starting`, `active`, or `finishing` -> `true`

`resourceVersion` is an opaque roll-business concurrency token. The v1 projection advances it for ordered durable facts that change the roll operation context: roll creation, table metadata, row creation, cell changes, and scan transitions. Consumers must compare and echo the whole token without parsing it. QueryHub/query ETags remain cache and invalidation tokens and are not interchangeable with this value.

## Errors

Errors use `{ "issues": ProcessingIssue[] }`. This read endpoint currently returns:

- `404` / `ROLL_NOT_FOUND` when the durable roll does not exist;
- `404` / `ROW_NOT_FOUND` when the exact `rollId + rowId` pair does not exist;
- `409` / `ROLL_MAPPING_INVALID` when the durable roll-to-box-to-client relationship is incomplete or inconsistent;
- `409` / `ROW_CONTEXT_INVALID` when the stored row contradicts its canonical address or has an unsupported origin.

The shared v1 catalog also defines `RESOURCE_VERSION_STALE` and `ACTION_INELIGIBLE` for later commands. This package does not expose those commands.

## Deliberate boundary

The scan transition events and projection are durable context foundations only. No public Start, Finish, Preview, Confirm, Apply, recovery, or mutation endpoint emits them yet.

This contract contains no path, storage root, workspace binding, worker identity, resource reference, or filesystem authority. Production enablement, authorization enforcement, QueryHub protection, worker/storage policy, idempotency, recovery, filesystem mutation, and live Windows-service proof remain separate gates.
