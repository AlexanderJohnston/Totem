# Scan and Processing Backend Implementation Status

Last updated: 2026-08-10

Authoritative policy: `SCAN_AND_PROCESSING_BACKEND_POLICY.md`

## Completed package 1

Work package 1 establishes the canonical row boundary. The backend now supports only roll-scoped regular and custom rows addressed by `rollId + rowId`.

Committed on the current branch as `c4815cb Retire client-scoped microfilm rows`.

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

## Current package 2

Work package 2 adds the durable, read-only operation-context foundation:

- `RollOperationQuery` projects `idle`, `starting`, `active`, and `finishing` from durable roll-scoped scan facts;
- `isScanning` is derived from that state and is emitted as a JSON Boolean independent of cells and profiles;
- an opaque roll-business `resourceVersion` advances with durable roll facts and remains distinct from QueryHub/query ETags;
- `GET /api/microfilm/rolls/{rollId}/rows/{rowId}/operation-context` resolves the canonical roll-to-box-to-client chain and the exact composite row;
- regular and custom rows share the same operation rules; origin is provenance only;
- invalid roll, row, mapping, row context, stale version, and ineligible action cases use the stable `ProcessingIssue` envelope;
- deterministic v1 regular, custom, and error fixtures live under `docs/contracts/scan-processing/v1`.

The scan transition facts are contract/projection foundations. There is no public command route or command handler that emits them in this package.

### Package 2 verification

- Focused operation-context, route, lookup, and wrong-roll tests: 16 passed, 0 failed.
- Focused Microfilm/roll-operation tests: 47 passed, 0 failed.
- Full `Totem.sln` Release build: succeeded with 0 errors and 3 existing warnings.
- Broader `Quantum.Tests`: 121 passed, 2 failed. The failures are the same unrelated `ClientProfileRegistryTests.MatchesCorrectProfile` and `WaspAssetServiceTests.GetClientBatchAsync_FetchesAdditionalPagesWhenTotalCountExceedsCurrentPageWindow` baseline cases recorded for package 1.
- `git diff --check`: passed.

## Settled policy after package 2

The following decisions are documented policy and planning inputs, not implemented runtime capability:

- a client is the Version 1 workspace, with exactly one active logical storage binding per client;
- the existing Server entity stays separate from storage resolution;
- developer-admin API commands may create and activate binding generations;
- the initial production share root is `\\sbsr-film\film\` and the approved worker identity is `CMGX\appdevsvc`;
- labels are primary, while an operation result or deliberately workspace-scoped query may return an informational full path that never becomes path authority;
- Version 1 resource references do not expire automatically;
- configured parent choices replace arbitrary browsing in Version 1;
- managers assign durable roles/permissions to registered users, and topics—not ASP.NET endpoint authorization—decide Scan and Processing authority;
- QueryHub remains an unchanged, identity-independent ETag subscription mechanism.

## Stop boundary

Start, Finish, Preview, and Apply remain production-disabled. Durable manager-assigned roles and topic-enforced operation permissions, logical binding implementation, deployed worker configuration/identity evidence, idempotency, leases, path safety, backup failure, restart reconciliation, and live Windows-service evidence remain open gates. Totem QueryHub ETag subscriptions intentionally remain identity-independent and are not an authorization gate.

No filesystem mutation or production enablement was implemented. No storage-binding runtime, worker identity/configuration deployment, role/permission topics, idempotency, recovery flow, or Windows-service evidence was added or claimed.
