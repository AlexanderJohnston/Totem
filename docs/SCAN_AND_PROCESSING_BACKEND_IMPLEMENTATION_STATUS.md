# Scan and Processing Backend Implementation Status

Last updated: 2026-08-11

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

## Completed package 2

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

## Completed package 3

Work package 3 implements the durable demo role and permission foundation without adding any operation or filesystem mutation:

- registered operation actors are resolved only from server-issued registered-user cookie claims and use the stable application user ID;
- a single globally ordered `ScanProcessingAccessTopic` owns the Version 1 permission catalog, versioned role definitions, global assignments/revocations, manager grants, operation decisions/rejections, and redacted audit facts;
- roles may contain only fixed Version 1 permissions, and replacements require the expected role version;
- assignments are keyed by stable registered user ID; usernames, display labels, process-user labels, manager status, registration, and authentication are not operation permission authority;
- each operation decision is bound to one request ID, stable actor ID, fixed permission, server-resolved client/workspace/roll/row scope, effective role IDs, and global authorization revision;
- forward-only revocation is implemented by global topic ordering: decisions after assignment revocation or permission removal fail, while previously accepted decision history is not rewritten;
- `ScanProcessingAccessQuery`, `ScanProcessingAuthorizationDecisionQuery`, and `ScanProcessingAccessAuditQuery` project management state, request-bound outcomes, and append-only audit history;
- frontend-facing permission, role, assignment, revocation, and temporary manager-bootstrap routes are implemented under `/api/scan-processing/access`;
- role and assignment management requires a registered authenticated actor but deliberately does not enforce manager status for the demo;
- the bootstrap secret has no committed default, is supplied at runtime through `ScanProcessingAccess:DemoBootstrapSecret`, is validated before username lookup, and is absent from durable commands, events, audit facts, responses, and fixtures.

No public operation-authorization or Start/Finish/Preview/Apply mutation endpoint was added. The existing file-backed registered-user store remains a Web deployment boundary and was not qualified for shared multi-host use.

### Package 3 verification

- Exact SDK: local .NET SDK `10.0.300`, invoked explicitly because the normal CLI would roll forward to installed SDK `10.0.302`.
- Focused access-authority, registered-user lookup, controller, route, fixture, bootstrap, replay, ordering, revocation, forged-identity, scope, and independent-replay tests: 21 passed, 0 failed.
- Full `Totem.sln` Release build: succeeded with 0 errors and 3 existing warnings.
- Broader `Quantum.Tests`: 143 passed, 2 failed. The failures are the unchanged `ClientProfileRegistryTests.MatchesCorrectProfile` case and `WaspAssetServiceTests.GetClientBatchAsync_FetchesAdditionalPagesWhenTotalCountExceedsCurrentPageWindow`; neither failing source area was changed by this package.
- Deterministic permission, role, assignment, and bootstrap response fixtures contain no secret field or value.
- Static scope scan found no storage root, UNC share, path-resolution, directory, or filesystem mutation code in the package.
- `git diff --check`: passed.

## Completed package 4

Work package 4 implements only the durable logical storage-binding contract and configuration boundary:

- `ScanProcessingStorageBindingTopic` is routed by the durable client ID, giving each Version 1 client/workspace one ordered binding stream seeded by `ClientCreated`;
- create commands receive a server-generated stable binding ID and persist path-free labels plus exactly four distinct logical capabilities: Scan parent, QPF, grayscale Frames, and bitonal Frames;
- activation requires the current opaque `storageRevision` and exactly the next configuration generation for the selected binding;
- each workspace projects exactly one active binding/generation pair; repeating that pair is durably unchanged, switching bindings replaces the pair, and switching back requires the previous binding's next generation;
- `ScanProcessingStorageBindingState` is the shared deterministic reducer used by the authority topic and `ScanProcessingStorageBindingQuery`;
- durable create, activation, unchanged, rejection, and redacted audit facts contain logical identity/generation only and never a physical root, path, Server ID, credential, or rejected raw value;
- registered developer-admin configuration routes are available under `/api/scan-processing/storage-bindings/clients/{clientId}` for read, create, and activate; the client route value is resolved through `MicrofilmClientLookupQuery`, and the existing Server assignment is not part of the binding command or state;
- create/activate request DTOs contain no actor, role, permission, workspace, Server, root, or path field;
- a server-side opaque resource-reference record and deterministic validator bind actor ID, exact resolved scope, purpose, permission, access revision, active binding, generation, and logical capability;
- Version 1 references have no automatic expiry, while explicit revocation, access revision/permission change, scope change, binding switch, or generation change invalidates later use;
- an output-only informational full-path response shape is defined, but no package 4 request accepts it and no Web code resolves it;
- deterministic binding and resource-reference fixtures live under `docs/contracts/scan-processing/v1`.

Package 4 does not expose operation discovery or issue public references. Reference records are a server-side contract for later operation packages; a future caller must resolve the opaque ID to the authoritative record and supply current replayed access and binding state to the validator.

### Package 4 verification

- Exact SDK: local .NET SDK `10.0.300` invoked explicitly with build servers disabled, single-node MSBuild, and shared compilation disabled.
- Focused logical-binding topic, generation, replay, reference validation, forged-input, redaction, Server-separation, route, and fixture tests: 17 passed, 0 failed.
- Full `Totem.sln` Release build: succeeded with 0 errors and 3 existing dependency warnings.
- Broader `Quantum.Tests`: 160 passed, 2 failed. The failures are the unchanged `ClientProfileRegistryTests.MatchesCorrectProfile` case and `WaspAssetServiceTests.GetClientBatchAsync_FetchesAdditionalPagesWhenTotalCountExceedsCurrentPageWindow`; neither failing source area was changed by this package.
- The package 3 focused access and registered-user lookup suite remains green at 21 passed, 0 failed, and all package 3 tests also pass in the broader run.
- Contract fixtures parse through typed tests; the binding fixture contains no Server authority, and the resource-reference fixture demonstrates an output-only, fixture-only informational path.
- Static package 4 source scan found no filesystem API, physical-root configuration, production share, worker identity, or Server-ID selection code.
- `git diff --check` and the separate untracked text whitespace/newline scan passed.
- No local or UNC filesystem access, path resolution, production-share access, Start/Finish/Preview/Apply mutation, or EventStoreDB test mutation occurred. Focused tests used the in-memory harness, and the already-running EventStoreDB instance was not changed.

## Current settled policy and implemented logical-binding boundary

The following decisions are documented policy and planning inputs, not implemented runtime capability:

- a client is the Version 1 workspace, with exactly one active logical storage binding per client; package 4 now implements this logical state;
- the existing Server entity stays separate from storage resolution; package 4 commands, state, and request DTOs contain no Server selector;
- developer-admin API commands create and activate logical bindings and sequential generations;
- the initial production share root is `\\sbsr-film\film\` and the approved worker identity is `CMGX\appdevsvc`;
- labels are primary, while an operation result or deliberately workspace-scoped query may return an informational full path that never becomes path authority;
- Version 1 resource references do not expire automatically;
- configured parent choices replace arbitrary browsing in Version 1;
- managers define roles and permission sets, assignments are global, and topics—not ASP.NET endpoint authorization—decide Scan and Processing authority;
- role-management endpoints intentionally lack backend manager-only enforcement for the demo;
- a temporary hard-coded-secret admin endpoint will bootstrap an existing registered user as manager; the secret must never be persisted or logged, and this mechanism must be removed or hardened before production;
- full paths are normal informational output from scoped Version 1 contracts, but returned paths never become operation authority;
- QueryHub remains an unchanged, identity-independent ETag subscription mechanism.

## Stop boundary

Start, Finish, Preview, and Apply remain production-disabled. Durable Start/roll acceptance, Operations-owned physical binding mapping, deployed worker configuration/identity evidence, idempotency, leases, path safety, backup failure, restart reconciliation, live multi-instance evidence, developer-admin/role-management hardening, bootstrap removal/replacement, and live Windows-service evidence remain open gates. Totem QueryHub ETag subscriptions intentionally remain identity-independent and are not an authorization gate.

No filesystem mutation or production enablement was implemented. No physical storage-binding runtime/resolver, roll mutation acceptance, worker identity/configuration deployment, idempotency, recovery flow, shared multi-host user-store qualification, or Windows-service evidence was added or claimed.
