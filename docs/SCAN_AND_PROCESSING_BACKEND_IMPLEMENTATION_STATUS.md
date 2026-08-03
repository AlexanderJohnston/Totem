# Scan and Processing Backend Implementation Status

Last updated: 2026-08-03

Authoritative policy: `SCAN_AND_PROCESSING_BACKEND_POLICY.md`

## Current package

Work package 1 establishes the canonical row boundary. The backend now supports only roll-scoped regular and custom rows addressed by `rollId + rowId`.

Implemented:

- retained roll row/table reads, regular/custom creates, and targeted cell patches;
- retained one roll-owned topic/query model for both origins;
- removed client-scoped row reads, creates, and patches;
- removed client row commands/events/topic, client row projections, and the legacy routing index/fallback dispatch;
- removed the client-scoped demo table seed hosted service and configuration;
- removed tests whose only purpose was client/roll coexistence;
- added contract coverage for route absence, regular/custom rule parity, and wrong-roll isolation without `rollName` cell fallback.

Removed HTTP routes:

- `GET|POST /api/microfilm/rows/{clientId}`
- `PATCH /api/microfilm/rows/{clientId}/{rowId}`
- `GET|POST /api/microfilm/custom-rows/{clientId}`
- `PATCH /api/microfilm/custom-rows/{clientId}/{rowId}`

The generated OpenAPI surface follows the controller and therefore no longer advertises these routes. No alias, name lookup, or migration bridge replaces them.

## External consumer coordination

The related Formatic checkout was inspected read-only at branch `master-path-two`, commit `e2b61ff73b86e88fd5363e53d915f85b4d1ee74a`. It was not edited, and its existing untracked Scan/Processing handoff was preserved.

That frontend already contains canonical roll-scoped reads/writes, but active code still contains client-scoped dependencies:

- `src/api/microfilmTable.ts` retains client-scoped custom-row reads and explicit regular/custom fallback creates and patches;
- `src/composables/millerShowcase/microfilmTableQueries.ts` still defines client-scoped row query URLs;
- live/readiness and showcase tests still exercise client-scoped routes.

Deploying this backend package before those consumers are migrated will break those calls. Coordinate the frontend change separately; do not restore backend inference from names or cells.

## Data conversion gate

No deployment data conversion was performed by this package. Before Scan or Process is enabled in an environment containing client-only row events, an operator-owned conversion must:

1. assign every retained row to one durable `rollId` through an explicit manifest;
2. reject missing or ambiguous assignments;
3. preserve regular/custom provenance, cell values, and existing audit facts;
4. avoid fabricating audit history;
5. verify counts and values after conversion.

`boxName`, `rollName`, profile membership, and all other cells are prohibited as identity fallbacks.

## Verification

- Pre-change focused baseline with .NET SDK `10.0.300`: 63 passed, 0 failed.
- Post-change focused Microfilm tests: 44 passed, 0 failed.
- Full `Totem.sln` Release build: succeeded with 0 errors and 3 existing warnings.
- Broader `Quantum.Tests`: 110 passed, 2 failed. The failures are the unchanged `ClientProfileRegistryTests.MatchesCorrectProfile` case and `WaspAssetServiceTests.GetClientBatchAsync_FetchesAdditionalPagesWhenTotalCountExceedsCurrentPageWindow`; both match the recorded unrelated baseline and neither test/source area was changed by this package.
- Static removed-symbol/route scan: no client row commands, events, topics, projections, routing index, seed types, or client-scoped row route attributes remain in active code/tests.
- `Outermind.Service/appsettings.json` parses successfully after seed removal.
- `git diff --check`: passed.

## Next package

After this package is focused-test green, the next smallest dependency boundary is work package 2: durable roll-owned scan state and resource version, canonical operation context for both row origins, structured errors, read-only discovery/context HTTP contracts, OpenAPI, and deterministic fixtures. Mutation and filesystem work remain out of scope.

Start, Finish, Preview, and Apply remain production-disabled. Authorization, protected QueryHub access, worker identity/storage roots, idempotency, leases, path safety, backup failure, restart reconciliation, and live Windows-service evidence remain open gates.
