# Scan and Processing Backend — Package 5 Handoff Prompt

Last updated: 2026-08-11

Use this prompt to continue Miller Scan and Processing backend work in a fresh session at:

```text
C:\Users\ajohnston\Desktop\Refactor\Totem
```

## Objective

Preserve and verify the completed-but-uncommitted package 3 and package 4 implementations, then implement only the smallest coherent package 5 slice: durable Start Scan acceptance without folder creation.

Package 5 may accept or reject Start durably and move the roll to `starting`. It must not resolve a physical path, touch a local or UNC filesystem, create a folder, emit `ScanStarted`, run a worker, implement recovery, or enable production mutation.

## Start-of-session procedure

1. Read every applicable `AGENTS.md` file.
2. Run:
   - `git status --short --branch`
   - `git log --oneline --decorate -10`
3. Do not switch branches, reset, discard, overwrite, clean, stage, or commit existing work unless the operator explicitly requests it.
4. Read `global.json` before building.
5. Read these documents completely, in order:
   1. `docs/SCAN_AND_PROCESSING_BACKEND_IMPLEMENTATION_HANDOFF_PROMPT.md`
   2. `docs/SCAN_AND_PROCESSING_BACKEND_POLICY.md`
   3. `docs/SCAN_AND_PROCESSING_BACKEND_IMPLEMENTATION_STATUS.md`
   4. `docs/SCAN_AND_PROCESSING_NEXT_STEPS_PLAN.md`
   5. `docs/contracts/scan-processing/v1/README.md`
   6. `docs/SCAN_AND_PROCESSING_RECOMMENDED_BACKEND_PLAN.md` as supporting background
6. Inspect packages 3 and 4 and re-run their focused tests before designing package 5.
7. Check whether EventStoreDB is already running before starting another instance.

Policy is authoritative. Status records completed implementation and evidence. Plans describe future work. Where policy, status, plans, code, or this handoff differ, verify the repository and correct the documentation rather than assuming any snapshot is current.

## Current Git checkpoint

Expected branch:

```text
user-auth
```

Expected HEAD:

```text
067e16c3 Set demo role management policy
```

At handoff preparation the branch was five commits ahead of `origin/user-auth`.

Packages 3 and 4 are complete in the worktree and intentionally uncommitted. Preserve all of their tracked and untracked files.

Expected modified tracked files:

```text
Outermind.Web/Identity/ApplicationUserManager.cs
Outermind.Web/Program.cs
docs/SCAN_AND_PROCESSING_BACKEND_IMPLEMENTATION_HANDOFF_PROMPT.md
docs/SCAN_AND_PROCESSING_BACKEND_IMPLEMENTATION_STATUS.md
docs/SCAN_AND_PROCESSING_BACKEND_POLICY.md
docs/SCAN_AND_PROCESSING_NEXT_STEPS_PLAN.md
docs/SCAN_AND_PROCESSING_RECOMMENDED_BACKEND_PLAN.md
docs/contracts/scan-processing/v1/README.md
tests/Quantum.Tests/ApplicationUserManagerTests.cs
```

Expected untracked package 3 files:

```text
Outermind.Web/Controllers/ScanProcessingAccessController.cs
Outermind.Web/ScanProcessing/DemoBootstrapSecretValidator.cs
Outermind.Web/ScanProcessing/RegisteredScanProcessingActorResolver.cs
Outermind/Microfilm/Queries/ScanProcessingAccessQuery.cs
Outermind/Microfilm/Queries/ScanProcessingAuthorizationDecisionQuery.cs
Outermind/Microfilm/ScanProcessingAccessCommands.cs
Outermind/Microfilm/ScanProcessingAccessEvents.cs
Outermind/Microfilm/ScanProcessingAccessTypes.cs
Outermind/Microfilm/Topics/ScanProcessingAccessTopic.cs
docs/contracts/scan-processing/v1/access.assignments.json
docs/contracts/scan-processing/v1/access.bootstrap.json
docs/contracts/scan-processing/v1/access.permissions.json
docs/contracts/scan-processing/v1/access.roles.json
tests/Quantum.Tests/ScanProcessingAccessControllerTests.cs
tests/Quantum.Tests/ScanProcessingAccessTests.cs
```

Expected untracked package 4 files:

```text
Outermind.Web/Controllers/ScanProcessingStorageBindingsController.cs
Outermind/Microfilm/Queries/ScanProcessingStorageBindingQuery.cs
Outermind/Microfilm/ScanProcessingStorageBindingCommands.cs
Outermind/Microfilm/ScanProcessingStorageBindingEvents.cs
Outermind/Microfilm/ScanProcessingStorageBindingTypes.cs
Outermind/Microfilm/Topics/ScanProcessingStorageBindingTopic.cs
docs/contracts/scan-processing/v1/storage.bindings.json
docs/contracts/scan-processing/v1/storage.resource-reference.json
tests/Quantum.Tests/ScanProcessingStorageBindingTests.cs
```

Re-run `git status`; these lists are preservation checkpoints, not permission to remove anything else.

## Completed packages

### Package 1 — committed

Canonical roll-scoped regular and custom rows addressed only by `rollId + rowId`. Legacy client-scoped rows, routes, routing indexes, and identity fallbacks were removed.

### Package 2 — committed

Read-only durable operation context with roll-owned `scanState`, JSON Boolean `isScanning`, exact row/roll/client resolution, and opaque business `resourceVersion` distinct from QueryHub/query ETags.

### Package 3 — uncommitted

Package 3 provides the durable demo authorization foundation:

- fixed Version 1 permission catalog;
- one globally ordered `ScanProcessingAccessTopic`;
- versioned roles and global role assignments/revocations;
- temporary manager bootstrap using a runtime-only secret;
- stable actors derived from server-issued registered-user IDs;
- request-bound authorization decisions/rejections and redacted audit facts;
- deterministic reducer and projections;
- frontend role/assignment/bootstrap routes.

Manager status, registration, authentication, usernames, labels, roles, permissions, client IDs, workspace IDs, Server IDs, and paths supplied by a browser are not operation authority.

### Package 4 — uncommitted

Package 4 provides only logical storage configuration:

- one client-routed binding topic per Version 1 workspace;
- server-generated stable binding IDs;
- one active binding/generation pair per configured workspace;
- sequential per-binding configuration generations guarded by `storageRevision`;
- path-free labels for distinct Scan-parent, QPF, grayscale-Frames, and bitonal-Frames capabilities;
- durable create, activation, unchanged, rejection, and redacted audit facts;
- deterministic reducer and read/audit projections;
- registered developer-admin read/create/activate routes;
- a server-side opaque resource-reference record and pure use-time validator;
- an output-only informational full-path response shape.

Package 4 deliberately does not expose operation discovery or public resource-reference issuance. It does not persist references, map bindings to physical roots, or resolve informational paths. A future caller must resolve an opaque ID to an authoritative server-side record and validate it against current access and binding state.

## Verification checkpoint

`global.json` requires .NET SDK `10.0.300`.

Normal `dotnet` previously selected `10.0.302`; do not use that as evidence. The exact SDK was available at:

```text
C:\Users\ajohnston\AppData\Local\Temp\formatic-dotnet-sdk\dotnet.exe
```

Use the build-server controls below to avoid the previously observed compiler/build-server fan-out:

```powershell
$taskTemp = [IO.Path]::GetTempPath()
$env:DOTNET_CLI_HOME = Join-Path $taskTemp 'formatic-dotnet-home'
$env:NUGET_PACKAGES = Join-Path $taskTemp 'formatic-nuget-packages'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 'true'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
$requiredDotnet = Join-Path $taskTemp 'formatic-dotnet-sdk\dotnet.exe'
& $requiredDotnet --version
```

Add these flags to builds and tests:

```text
--no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
```

Recorded results under SDK `10.0.300`:

- package 3 focused tests: 21 passed, 0 failed;
- package 4 focused tests: 17 passed, 0 failed;
- broader `Quantum.Tests`: 160 passed, 2 unrelated existing failures;
- full `Totem.sln` Release build: 0 errors and 3 existing dependency warnings;
- all 9 Scan/Processing JSON fixtures parsed;
- `git diff --check` and the separate untracked whitespace/newline scan passed;
- static package 4 scan found no filesystem API, physical-root configuration, production share, worker identity, or Server-selection code.

The two unrelated broader failures were:

```text
Quantum.Tests.ClientProfileRegistryTests.MatchesCorrectProfile
Quantum.Tests.WaspAssetServiceTests.GetClientBatchAsync_FetchesAdditionalPagesWhenTotalCountExceedsCurrentPageWindow
```

Treat all recorded results as historical until re-run. Focused tests used the in-memory harness. EventStoreDB was already running as `EventStore.ClusterNode` and was not changed or used by the focused suites.

## Package 5 implementation target

Implement durable Start Scan acceptance only.

The coherent Version 1 outcome is:

```text
authenticated HTTP request
        ↓
server resolves exact roll + row + client/workspace + actor
        ↓
durable access topic decides scan.start for one bound request
        ↓
roll-scoped durable authority validates current state and idempotency
        ↓
ScanStartAccepted + audit + idempotency + scanState=starting
        ↓
202 Accepted with durable scan ID and refreshed operation context
```

Stop at the final line. Do not create or inspect a folder.

Package 5 must include:

1. A Start transport contract addressed by the route's `rollId + rowId`.
2. Stable registered actor resolution from the server-issued cookie.
3. Canonical roll, box, client/workspace, roll-name, and exact row resolution from durable queries.
4. A durable `scan.start` authorization request and a matching topic-owned authorization decision or rejection.
5. Request binding that prevents an authorization fact from authorizing a different actor, permission, roll, row, resource reference, or Start payload.
6. One roll-routed command authority with deterministic replay.
7. Atomic validation and persistence of:
   - exact `rollId + rowId`;
   - expected business `resourceVersion`;
   - `scanState == idle`;
   - one current Scan-parent reference or its authoritative server-side record;
   - active binding ID and configuration generation;
   - folder name and notes;
   - idempotency key and semantic-input hash;
   - durable scan ID;
   - `ScanStartAccepted` and roll resource-version advance;
   - Start audit and accepted/rejected outcome.
8. `scanState=starting`, `isScanning=true`, and `activeScanId` immediately after durable acceptance.
9. `202 Accepted` for a new or reconciled accepted Start and structured stable issues for rejection.
10. Deterministic fixtures and tests for success, rejection, idempotency, replay, ordering, forged input, and redaction.

Package 5 must not emit `ScanStarted`. That fact belongs to the later worker/folder-creation package.

## Logical Start request boundary

The logical HTTP input should contain only values the operator is allowed to choose or echo:

```ts
interface StartScanRequest {
  expectedResourceVersion: string;
  parentResourceRefId: string;
  folderName: string;
  notes: string;
  idempotencyKey: string;
}
```

The route supplies `rollId + rowId`. The request must not contain:

- actor, username, process-user ID, role, or permission;
- client ID, workspace ID, box ID, roll name, or cell values;
- Server ID;
- binding ID or configuration generation as client-authored authority;
- physical root, full path, parent path, UNC path, or worker identity;
- scan ID.

Web resolves actor and canonical scope. Durable state resolves the authoritative reference, binding, generation, permission decision, and scan ID.

Folder-name validation may be pure deterministic string validation. Reject path separators, rooted input, traversal, invalid Windows filename characters, reserved names, trailing dots/spaces, empty normalized names, and configured length violations without probing a filesystem. Trim notes and enforce the policy limit of 2,000 characters. Do not persist rejected raw values in audit or error facts.

## Resource-reference dependency

Package 4 defines `ScanProcessingResourceReference` and `ScanProcessingResourceReferences.ValidateUse`, but it intentionally does not expose discovery, issue a public reference, or persist a reference registry.

Before implementing Start, determine the smallest durable way to resolve `parentResourceRefId` to an authoritative server-side record. Preserve these rules:

- the browser submits only the opaque ID;
- the browser never constructs or edits the authoritative record;
- reference resolution is logical only and performs no physical path lookup;
- the record remains actor-, purpose-, scope-, permission-, access-revision-, binding-, generation-, and capability-bound;
- use-time validation observes current authorization and binding state;
- permission removal, assignment revocation, scope mismatch, explicit reference revocation, binding switch, or generation advance rejects Start;
- Version 1 has no automatic reference expiry;
- no path is stored in the reference record.

If public Start cannot be coherent without a minimal logical issuance/lookup seam, implement only that bounded seam and its tests. Do not expand package 5 into filesystem discovery, arbitrary browsing, informational-path resolution, or worker configuration. Ask the operator only if repository evidence exposes a genuine product tradeoff that cannot be resolved while preserving the settled contract.

## Durable authorization and cross-topic ordering

Do not authorize Start in the controller.

The access decision must remain owned by `ScanProcessingAccessTopic` and bound to one request ID, stable actor ID, `scan.start`, exact server-resolved scope, effective role IDs, and authorization revision. The roll authority must consume or verify that bound decision without trusting an HTTP Boolean or client-authored claims.

Trace Totem's current cross-topic routing and command-response conventions before choosing the implementation. Prove that:

- a rejected or missing access decision cannot reach acceptance;
- a decision for another request, actor, permission, roll, row, or resource cannot be replayed for this Start;
- a decision ordered after permission removal or assignment revocation rejects;
- already accepted durable history is not rewritten by later revocation;
- the roll's state/version/idempotency decision is serialized in one roll-routed authority;
- deterministic replay produces the same accepted scan, state, and idempotency result;
- no in-memory controller lock is treated as concurrency authority.

Do not claim deployed multi-instance qualification unless live evidence is added separately.

## Roll state and existing event warning

`Outermind/Microfilm/RollOperationEvents.cs` already defines thin projection-foundation facts including `ScanStartAccepted`, `ScanStarted`, and `ScanStartFailed`. `RollOperationQuery` consumes them, but there is currently no public mutation command or command topic that emits them.

Package 5 may evolve or pair the existing `ScanStartAccepted` fact with richer durable Start, idempotency, and audit facts. Keep one shared deterministic roll-operation reducer so command decisions and `RollOperationQuery` cannot drift.

There are other legacy types named `ScanStarted` under `Outermind/_Events.cs` and `Outermind/Decisions/ScanOps.cs`. They are unrelated legacy models. Do not route package 5 through them or conflate their identity with the Microfilm roll-operation facts.

## Idempotency requirements

Idempotency is part of package 5, not a future enhancement.

- Scope the key by stable actor ID, command kind, roll ID, and idempotency key.
- Compute a deterministic semantic-input hash from normalized authoritative Start inputs.
- Same key plus same semantics returns the same accepted scan ID and current result.
- Same key plus different semantics rejects with `IDEMPOTENCY_KEY_MISMATCH`.
- Persist acceptance before returning `202 Accepted`.
- Prove lost-response reconciliation by idempotency key without appending a second acceptance.
- Persist records durably; retention cleanup is not part of package 5.
- Never place credentials, full paths, or unredacted rejected values in the idempotency record or audit.

## Settled policy — do not reopen

1. Canonical operation identity is `rollId + rowId`.
2. The backend resolves roll, box, client/workspace, and roll name from `rollId`, then verifies exact row membership.
3. Version 1 `workspaceId == clientId`.
4. Regular and custom rows are equally eligible; `origin` is provenance only.
5. Profile fields and cells, including `cells.rollName` and `cells.isScanning`, are presentation only.
6. Query ETags and QueryHub invalidations are not business concurrency or operation authority.
7. The existing Server entity does not select or resolve storage.
8. Durable domain state stores logical binding identity and generation, never physical roots.
9. `Quantum.Web` does not resolve or touch production storage.
10. Operation authorization is a durable topic decision, not ASP.NET endpoint authorization.
11. Registration, authentication, manager status, username, or process-user label alone grants no `scan.start` permission.
12. Forward-only revocation does not rewrite an already accepted Start.
13. Version 1 resource references have no automatic expiry but require current use-time validation.
14. Start acceptance makes the roll `starting`; folder creation later makes it `active`.
15. At most one `starting`, `active`, or `finishing` scan exists per roll.
16. Existing-folder collision behavior belongs to the worker package because package 5 does not inspect storage.
17. QueryHub authorization changes and general HTTP fetch-endpoint authorization remain out of scope.

The policy mentions production root `\\sbsr-film\film\` and worker identity `CMGX\appdevsvc`. They are policy inputs only. Do not resolve, inspect, access, persist, or mutate the share, and do not claim live worker qualification.

## Code investigation before editing

Trace these current seams:

- `Outermind/Microfilm/Queries/MicrofilmClientLookupQuery.cs`
- `Outermind/Microfilm/Queries/RollMicrofilmLookupQuery.cs`
- `Outermind/Microfilm/Queries/RollMicrofilmRowQuery.cs`
- `Outermind/Microfilm/Queries/RollOperationQuery.cs`
- `Outermind/Microfilm/RollOperationEvents.cs`
- `Outermind/Microfilm/RollOperationTypes.cs`
- `Outermind.Web/Controllers/MicrofilmController.cs`
- `Outermind/Microfilm/Topics/ScanProcessingAccessTopic.cs`
- `Outermind/Microfilm/Queries/ScanProcessingAuthorizationDecisionQuery.cs`
- `Outermind/Microfilm/ScanProcessingAccessCommands.cs`
- `Outermind/Microfilm/ScanProcessingAccessEvents.cs`
- `Outermind/Microfilm/Topics/ScanProcessingStorageBindingTopic.cs`
- `Outermind/Microfilm/Queries/ScanProcessingStorageBindingQuery.cs`
- `Outermind/Microfilm/ScanProcessingStorageBindingTypes.cs`
- current topic routing, `FlowCall`, controller-to-command dispatch, query registration, and expected-version patterns;
- current test harnesses for topics, queries, controllers, replay, and any live KurrentDB integration.

Search for idempotency before creating new abstractions. No current Microfilm Start idempotency implementation was found at this handoff, but verify the repository again.

## Required package 5 verification

At minimum, prove:

- registered cookie actor is required and client-authored actors are absent or ignored;
- `scan.start` permission is required through a durable access decision;
- revoked or removed permission rejects later Start requests;
- exact regular and custom `rollId + rowId` contexts both succeed under the same rules;
- wrong roll/row combinations fail without name or cell fallback;
- stale `resourceVersion` fails;
- non-idle roll state fails and no second active/transitioning scan is accepted;
- missing, forged, wrong-actor, wrong-purpose, wrong-scope, wrong-capability, revoked, or stale-generation resource references fail;
- client-authored roles, permissions, actors, client/workspace IDs, Server IDs, binding generations, roots, and paths never become authority;
- folder-name and notes validation is deterministic and redacted;
- new Start accepts exactly once and sets `starting`, `isScanning=true`, and `activeScanId`;
- same idempotency key and same semantic input reconciles to the original scan;
- same key with different semantic input fails;
- a simulated lost HTTP response does not create another acceptance;
- replay yields identical roll state, version, idempotency, rejection, and audit results;
- accepted/rejected/audit/idempotency facts contain no physical root, full path, credentials, secret, or rejected raw sensitive value;
- concurrent semantics rely on durable per-roll ordering and business versions, without claiming live deployed multi-instance qualification;
- no `ScanStarted`, folder creation, filesystem access, worker execution, or recovery behavior occurs.

Use focused tests first. Re-run package 3 and package 4 focused suites. Then run the narrowest relevant Release build, broader `Quantum.Tests`, JSON fixture parsing, static boundary scans, `git diff --check`, and a separate whitespace/newline scan for untracked files. Record exact pass/fail counts and identify unrelated failures separately.

## Stop boundary

Stop before all of the following:

- physical binding mapping or path resolution;
- local or UNC filesystem reads, probes, enumeration, or writes;
- production share access;
- folder creation or collision checks;
- `ScanStarted`, `ScanStartFailed`, worker claim, lease, or heartbeat;
- Finish or Abandon Scan;
- Processing discovery, Preview, Confirm, or Apply;
- QPF or Frames mutation;
- asynchronous worker execution;
- retry/recovery or crash-restart claims;
- target-host, Windows-service, multi-instance, or production qualification;
- QueryHub authorization changes;
- general HTTP fetch-endpoint authorization;
- production enablement.

Do not describe the Scan/Processing backend as complete or production-ready.

## Documentation

Update the existing policy, implementation status, next-steps plan, this handoff, recommended plan, machine-readable contracts, and deterministic fixtures only where package 5 implementation or verified evidence requires it.

Preserve the distinction between:

- approved policy;
- completed implementation;
- fresh verification evidence;
- recommended future design;
- human decisions;
- production gates.

Do not invent evidence for folder creation, filesystem safety, recovery, multi-instance deployment, or production readiness.

## End-of-session report

Report:

1. The exact package 5 boundary implemented.
2. Files, routes, commands, events, idempotency records, audit facts, contracts, reducers, and projections changed.
3. The authorization-to-roll routing and how request binding is enforced.
4. How the opaque reference is resolved and validated without physical path access.
5. Focused and broader verification with exact counts.
6. Unrelated failures and dependency warnings separately.
7. Whether packages 3, 4, and 5 remain uncommitted.
8. The next smallest package, expected to be worker claim/lease plus controlled scan-folder creation and collision handling.
9. Remaining human decisions and production gates.
10. Explicit confirmation that no Scan/Processing storage path was resolved or mutated and no production mutation occurred.
