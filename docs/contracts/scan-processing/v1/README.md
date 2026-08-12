# Scan and Processing Contracts v1

This package defines the read-only context needed to open Scan or Process for one Miller row.

It also defines the Version 1 demo access-management responses implemented by work package 3. Access management remains separate from operation mutation and production enablement.

Work package 4 adds logical storage-binding configuration and server-side opaque-resource-reference contracts. It remains separate from physical storage mapping, path resolution, operation acceptance, and filesystem work.

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

The operation-context contract contains no path, storage root, workspace binding, worker identity, resource reference, or filesystem authority. Work package 3 adds role and permission decisions. Work package 4 adds a separate logical-binding contract; it does not change operation context or grant operation authority. Roll command acceptance, physical binding mapping/resolution, idempotency, worker/storage policy, recovery, filesystem mutation, live multi-instance evidence, and production enablement remain separate gates. QueryHub protection is intentionally not a work item.

## Access-management endpoints

The frontend-facing demo routes are:

- `GET /api/scan-processing/access/permissions`
- `GET|POST /api/scan-processing/access/roles`
- `PUT /api/scan-processing/access/roles/{roleId}`
- `GET|POST /api/scan-processing/access/assignments`
- `POST /api/scan-processing/access/assignments/revoke`
- `POST /api/scan-processing/access/admin/bootstrap`

Role and assignment reads/writes require a registered authenticated cookie actor. The demo deliberately does not enforce manager status on role-definition or assignment endpoints. The bootstrap route accepts `userName` and `secret`, validates the fixed operator-supplied secret before looking up the username, and appends only a secret-free manager-grant command. The configured secret has no source-controlled default and is supplied at runtime as `ScanProcessingAccess:DemoBootstrapSecret` (for example, the `ScanProcessingAccess__DemoBootstrapSecret` environment variable or .NET user-secrets). The secret is never returned or included in durable facts or fixtures.

Role assignments are global. Roles are versioned and contain only values from `access.permissions.json`. Replacements require `expectedVersion`; stale replacements conflict. Assignment and revocation requests target an existing registered username, which Web resolves to the stable registered user ID before appending a command.

The `authorizationRevision` is the global durable access-policy revision used by later operation decisions. It is not a QueryHub ETag. Manager status alone, registration alone, and authentication alone grant no Scan or Processing permission.

Operation-authorization decisions are currently durable internal facts, not a public HTTP authorization or mutation endpoint. Each fact is bound to one server-generated request ID, stable actor ID, fixed catalog permission, server-resolved client/workspace/roll/row scope, effective role IDs, and authorization revision. Forward-only revocation means decisions ordered after a revocation fail; earlier accepted history is not rewritten.

The deterministic response fixtures are:

- `access.permissions.json`
- `access.roles.json`
- `access.assignments.json`
- `access.bootstrap.json`

## Logical storage-binding endpoints

The registered developer-admin demo routes are:

- `GET /api/scan-processing/storage-bindings/clients/{clientId}`
- `POST /api/scan-processing/storage-bindings/clients/{clientId}/bindings`
- `POST /api/scan-processing/storage-bindings/clients/{clientId}/bindings/{bindingId}/activate`

The route client ID selects configuration scope only. Web resolves it against `MicrofilmClientLookupQuery`, and the durable topic is seeded and routed by that known client ID. Version 1 derives `workspaceId` from the client; no request DTO supplies a workspace ID. The existing Server entity and its client assignment are deliberately absent from binding commands, state, and responses.

Create accepts only a logical binding label and exactly one path-free label for each fixed capability:

- `scan-parent`
- `qpf`
- `frames-grayscale`
- `frames-bitonal`

The server generates the stable `bindingId`. Create does not activate the definition. Activate accepts `configurationGeneration` and `expectedStorageRevision`. The first generation for a binding is `1`; every later activation of that binding must use its next generation. This prevents switching away and later reviving an old binding/generation pair. Repeating the already-active pair is durably unchanged.

`storageRevision` is logical binding business concurrency, not a query ETag. Each client-routed topic serializes create and activation decisions, and the projection uses the same deterministic reducer. This supports durable per-workspace ordering but is not live deployed multi-instance qualification.

Create and activate request DTOs contain no actor, role, permission, workspace, Server, root, or path field. Labels containing path separators or a colon are rejected without copying the raw value into rejection or audit facts. Binding events contain logical identity, labels, capabilities, and generations only. Audit facts omit labels and all physical configuration.

## Opaque resource-reference boundary

Work package 4 defines the server-side record that will sit behind an opaque resource-reference ID. The authoritative record binds:

- stable actor ID;
- exact server-resolved client/workspace/roll/row scope;
- purpose and fixed permission;
- global access revision;
- active binding ID and configuration generation;
- one fixed logical capability.

Use-time validation requires current replayed access and binding state. Wrong actor/scope/purpose/capability, explicit reference revocation, permission or access-revision change, binding switch, or generation change fails. Version 1 `expiresAt` is null and there is no automatic expiry.

No public package 4 route issues operation references. A later discovery package will accept only the opaque ID back, resolve the authoritative server-side record, obtain a fresh topic-owned authorization decision, and revalidate current binding state. The browser must never send the record fields as authority.

`ScanProcessingInformationalPathResponse` is output-only. It allows a later deliberately scoped worker result to pair a label with a full informational path. No package 4 request accepts `fullPath`, binding state does not store it, and `Quantum.Web` does not resolve it.

The deterministic package 4 fixtures are:

- `storage.bindings.json`
- `storage.resource-reference.json`

The resource fixture uses a fixture-only output path to demonstrate shape. It is not a configured root, reusable authority, or evidence of filesystem access.
