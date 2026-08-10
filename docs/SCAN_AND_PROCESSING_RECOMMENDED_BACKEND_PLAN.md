# Recommended backend plan

Status: Architecture recommendation for Backend, Product, Security, and Operations review  
Prepared: 2026-07-31  
Last updated: 2026-08-10
Frontend baseline: `406b352d996f101350a48d2636625ff312c6be12`

The handoff is sound. I recommend adopting it with three architectural amendments:

1. `isScanning` belongs to the durable roll's operational state and should be projected as a top-level row field. It should not live in `cells`, even though the current cell model can carry JSON Booleans.
2. `Quantum.Service` should be the only production process that touches Formatic storage. `Quantum.Web` should resolve the authenticated registered actor, validate, append commands, and serve projections; durable topics should make role/permission decisions.
3. Start Scan should be a durable asynchronous state transition. Do not promise an atomic transaction across KurrentDB and an SMB or local filesystem.

This is a target design and review recommendation, not production-readiness evidence.

## Evidence behind the recommendation

- The current Miller row already separates durable identity (`Id`, `RollId`, `Origin`) from presentation cells.
- The current domain maps roll to box/client and client to server, but it does not yet define an authorized filesystem binding. For Version 1, a client is the workspace; the existing Server entity remains a separate identifier.
- Query ETags represent projection checkpoints. They are useful for caching and QueryHub invalidation, but they are not business concurrency tokens.
- Commands can be durably appended before their processing response is observed. Lost-response reconciliation is therefore required for mutations.
- Current identity is tracking-oriented rather than the durable role/permission authority required for Scan and Processing topic decisions.
- The installed web and worker processes use separate Windows service identities. Remote share access must be deliberately assigned to the worker identity.
- The legacy Formatic implementations disagree on discovery, validation, Frames roots, and file behavior. Neither implementation should be declared wholly canonical.
- Both existing QPF editors correctly avoid rewriting the affected QPF when backup creation fails. The new backend must preserve or improve that safety property.

## Target architecture

```text
Browser
   |
   | authenticated HTTP commands and bounded polling
   | identity-independent QueryHub ETag invalidations
   v
Quantum.Web
   | resolve authenticated actor, validate, append commands, serve OpenAPI/projections
   v
KurrentDB
   | topic decisions, roles, permissions, roll state, scans, plans, jobs, idempotency, audit
   v
Quantum.Service
   | resolve opaque resources, execute jobs, reconcile after restart
   v
Approved Windows storage roots and UNC shares
```

HTTP remains the canonical state-retrieval path. QueryHub may notify the frontend that a roll, scan, or job changed, but notification loss must not prevent recovery.

## Durable roll and row context

The roll model should durably own scanning state. The Miller projection should emit an operation context independent of profile columns:

```ts
interface OperationRowContext {
  clientId: string;
  rowId: string;
  origin: 'regular' | 'custom';
  rollId: string | number;
  rollName: string;
  isScanning: boolean;
  scanState: 'idle' | 'starting' | 'active' | 'finishing';
  activeScanId?: string;
  resourceVersion: string;
  eligibleActions: Array<'scan' | 'process'>;
}
```

Recommended decisions:

- Emit `isScanning` as a real JSON Boolean regardless of profile visibility.
- Keep `scanState` as the richer durable state. `isScanning` is true for `starting`, `active`, and `finishing`.
- Make `resourceVersion` an opaque roll/business version, normally derived from the durable roll stream revision.
- Keep HTTP ETags separate; an ETag is a projection/cache version, not command concurrency authority.
- Address operations by `rollId + rowId`. Resolve the canonical roll, box, client/workspace, and roll name from `rollId`; never use `cells.rollName` or another cell as an operation lookup key.
- Support regular and custom rows equally. `origin` records whether the row came from WASP or was entered manually; it does not change identity, authorization, capabilities, or behavior.
- Remove legacy client-scoped rows, routes, routing indexes, and compatibility behavior, converting any remaining data before Scan and Process are enabled.
- Return refreshed operation context from successful commands and also issue/refetch through the normal row invalidation path.

The backend should resolve storage scope through durable relationships rather than client input:

```text
row -> roll -> box -> client (the Version 1 workspace) -> configured storage binding
```

The missing piece is a server-owned storage binding. Version 1 has exactly one active binding per client. Domain state should reference its stable binding ID and configuration generation; Operations-owned `Quantum.Service` configuration supplies the physical root and service identity. Developer-admin API commands may create and activate logical binding generations. The existing Server entity does not resolve storage.

The initial production root is `\\sbsr-film\film\`, and the approved worker identity is `CMGX\appdevsvc`. Labels remain the primary presentation value. A deliberately client/workspace-scoped query or operation result may return an informational full path so a user can verify work, but the path is never accepted back as operation authority. Version 1 uses configured parent choices rather than arbitrary browsing.

## Scan plan

### Discovery

Return the canonical row/roll and version, current scan state, active scan ID, persisted folder name and notes, an opaque parent resource reference, a safe display path when permitted, the suggested folder name, validation rules, and structured blockers.

### Start Scan

Start should be a durable asynchronous action:

1. Authorize the actor and resolve the selected roll.
2. Validate the expected roll version, resource reference, folder name, and idempotency key.
3. Persist `ScanStartAccepted`, allocate `scanId`, increment the roll version, and set `scanState=starting` and `isScanning=true`.
4. Return `202 Accepted` with the durable scan ID and current row context.
5. Let `Quantum.Service` resolve the parent resource, revalidate containment, and create the folder.
6. On success, persist `ScanStarted` and `scanState=active`.
7. On failure, persist `ScanStartFailed` and reset the roll to idle unless the filesystem outcome is uncertain and needs reconciliation.

This ordering ensures that a lost HTTP response can be reconciled and that the browser never has to guess whether work was accepted. It avoids claiming atomicity between the event store and storage share.

Recommended Version 1 scan behavior:

| Decision | Recommendation |
| --- | --- |
| Active scans | One `starting`, `active`, or `finishing` scan per roll. |
| Folder name | Start from canonical roll name; allow an operator edit subject to server validation. |
| Existing folder | Fail. Do not infer resume or reuse from a matching name. |
| Notes | Persist append-only notes at Start and Finish; 2,000-character limit per entry. |
| Finish another actor's scan | Separate permission and audited reason. |
| Finish file check | Do not require expected files in Version 1; capture counts/warnings if useful. |
| Abandon | Separate command; retain history and never delete the folder automatically. |
| Processing during scan | Allow discovery/history; block new processing plans and Apply. |
| Idempotency retention | 30 days after the action is terminal. |

### Finish Scan

Finish should verify the active scan belongs to the canonical row/roll, authorize the actor, check `scanState=active` and the expected roll version, append final notes, persist the finish record, set the roll to idle, increment its version, audit the outcome, and return refreshed row context.

The explicit transitional state prevents Finish from racing folder creation after Start has been accepted.

## Processing plan

### Initial capabilities

Expose only capabilities returned and authorized by discovery. The first production capabilities should be:

1. `qpf-settings`
2. `frames-paths`

Keep IDF conversion, loose-QPF organization, queue creation/combine/split, and backup cleanup unavailable until each operation has an approved canonical behavior and evidence suite.

Unauthorized operations should be omitted rather than disclosed as blocked. `blocked` should mean the actor may know about the operation but current roll/resource state prevents it.

### Opaque authorized resources

Resource references should be:

- opaque, versioned, and purpose-bound;
- bound to actor, workspace/roll, purpose, capabilities, version, and configuration generation;
- reauthorized when used;
- invalidated by explicit revocation or expiry, permission change, mapping change, or configuration change.

Version 1 does not automatically expire resource references. Automatic expiry may be added later without weakening use-time authorization or binding-generation validation.

The server should reject arbitrary paths, traversal, rooted child input, device paths, alternate data streams, mapped drives, and root escape. Reparse points, symlinks, and junctions should be rejected by default. If later permitted, containment must be verified using resolved final handles rather than string-prefix checks.

### Preview plans

A plan should be immutable and contain:

- plan ID and version;
- actor and captured scope;
- operation, schema, configuration, discovery, resource, and source-content versions;
- expiry;
- bounded or paged effects;
- discovered/change/create/skip/block counts;
- collisions, warnings, blockers, and required acknowledgements;
- backup behavior and expected destinations;
- partial-completion semantics;
- confirmation ID and safe confirmation text.

Proposed plan lifetime: 15 minutes.

Editing configuration invalidates the plan. Preview and Apply must share the same discovery, selection, normalization, and collision logic.

### Apply and idempotency

Apply should submit only the accepted plan ID/version, acknowledgements, expected captured resource version, and an idempotency key.

Apply must revalidate authorization, expiry, schema/configuration/resource versions, source hashes, scan state, locks, path policy, and collisions. It must persist the durable job before returning `202 Accepted`.

The same actor/key/plan combination returns the same job. Reusing a key for different semantic input returns an idempotency mismatch. The client reconciles a lost response by idempotency key before creating new work.

### Jobs, results, and cancellation

Required statuses:

```text
queued
running
cancel_requested
completed
completed_with_errors
failed
cancelled
```

Each transition increments a monotonic job version. Progress should include total, attempted, succeeded, skipped, failed, and remaining where known.

Cancellation is cooperative and checked between items and before irreversible commit. Completed items remain completed. Large results are paged in stable order. Retrying failures creates a new linked plan/job rather than rewriting original history.

## Concurrency and recovery

For Version 1, use one exclusive mutation lease per roll for Scan transitions and Processing Apply jobs:

- discovery and history remain read-only;
- plan creation checks for conflicts but does not reserve the roll;
- Apply revalidates and acquires the lease before queuing execution;
- conflicting work is rejected with `RESOURCE_LOCKED`, not silently queued;
- the durable lease has an owner, heartbeat, expiry, and fencing token that works across service instances;
- source version/hash is checked immediately before file replacement;
- losing the lease stops the worker before the next mutation checkpoint.

After restart, the worker reconciles durable checkpoints with source, staged file, backup, and resulting hashes. It resumes only effects proven not to have committed. An uncertain replacement becomes explicit reconciliation work; it is never blindly repeated.

## QPF file safety

Each file is an independently durable item:

1. Resolve and identify the authorized QPF.
2. Capture and compare its source hash/version.
3. Parse with DTD and external entity resolution disabled.
4. Apply only versioned schema operations.
5. Write a same-directory staging file.
6. Flush and validate the staged document.
7. Create and verify the required backup.
8. Recheck the source hash and lease fencing token.
9. Atomically replace the source on the same volume.
10. Persist the resulting hash/version and item outcome.

Backup failure blocks the affected QPF write. Backups must not be described as rollback unless a tested restore contract, manifest, authorization model, audit trail, and operator workflow exist.

## Canonical behavior decisions

| Operation | Recommended behavior |
| --- | --- |
| Scan | Use the new durable state machine and async folder creation contract. Do not inherit the draft legacy Scan events as-is. |
| QPF settings | Select exactly one QPF directly under the canonical roll directory. Zero or multiple QPFs block Preview. Preserve unknown XML. |
| Frames paths | Use separate grayscale and bitonal authorized base resources. Both may intentionally reference the same directory. The server appends the canonical roll folder. |
| IDF-to-QPF | Defer. Later: deterministic top-level discovery, collision failure, schema-owned defaults, staged atomic write. |
| Organize QPF | Defer until normalization, cross-volume movement, collision, item-boundary, and recovery behavior are defined. |
| Queue create | Defer until deterministic ordering, naming, duplicate, and overwrite rules are defined. |
| Queue combine | Defer until selected-input ordering, duplicate preservation/reindexing, naming, and overwrite rules are defined. |
| Queue split | Defer until part validation, balanced ordering, naming, and collision behavior are defined. |
| Backup cleanup | Defer and permission-gate separately. It depends on approved retention and restore policy. |

Filesystem enumeration order is never authoritative. All discovery should normalize and sort deterministically.

## QPF setting schema Version 1

Use schema ID `qpf-settings/1`. The client sends stable typed setting IDs, never XML attribute names, XPath expressions, placement rules, or XML fragments.

Recommended rules:

- Exact user-ID targets are `/Roll/@ScanUserID` and `/Roll/@ProcessUserID`.
- Processing settings target direct `/Roll/ScannerSettings/DetectionSettings` attributes.
- Unknown elements and attributes are preserved.
- Duplicate/ambiguous target structures block the file.
- Adding an absent setting is allowed only when that setting's schema explicitly permits it.
- `ScanUserID` and `ProcessUserID` are server-derived from authenticated identity and are not operator-editable values.
- Booleans are API Booleans normalized to the QPF's approved `0`/`1` encoding.
- Output-directory attributes belong to the Frames operation rather than general settings.

Proposed allowed values:

| Setting | Proposed value policy |
| --- | --- |
| Grayscale output format | Integer enum `0..5` |
| Bitonal output format | Integer enum `0..4` |
| Rotation | `0`, `90`, `180`, `270` |
| Flip | Integer enum `0`, `1`, `2` |
| Contrast, brightness, gamma | Integer `-255..255` |
| Sharpen | Integer `0..10` |
| Deskew quality | Integer `0..5` |
| Crop border | Integer `0..150` |
| Crop threshold | Integer `0..128` |
| Crop, deskew, save modes | Boolean |
| File prefixes | Bounded safe strings; exact length/characters require Product approval |

The legacy `ProcessCropThreshold` maximum of `1281` conflicts with its example and observed default behavior. Use `0..128` unless representative production evidence establishes another range.

The existing sample settings catalog is evidence about previous inputs, not an approved API schema. Keys without exact, safe placement rules should not be exposed.

## Frames decision

Use separate grayscale and bitonal base resource references. The UI can still select the same authorized root for both.

Preview returns the calculated effective path for each enabled mode. Apply creates missing output directories before the QPF write. If required directory creation fails, the affected QPF is not rewritten. Existing directories are reused only after the server verifies their type and containment.

Do not use recursive "first QPF found" discovery. Version 1 requires exactly one top-level QPF.

## Authorization and audit

Production mutation requires durable topic-enforced permissions separate from identity tracking. For the Version 1 demo, managers define roles and their permission sets, assignments are global, and role definitions, assignments, and permission changes are durable events. The frontend-facing role-management endpoints do not yet enforce manager-only access on the backend. A temporary admin endpoint may bootstrap an existing registered user as manager by checking a hard-coded plaintext request-body secret; that secret must never be persisted or logged, and the endpoint must be removed or hardened before production enablement. Registration or authentication alone grants no Scan or Processing permission:

- Scan discover/start/finish-own/finish-any/abandon;
- Processing discover and Preview;
- Apply per operation;
- Cancel own or another actor's job;
- View contextual or workspace history;
- View contextual or workspace full paths and restricted diagnostic values/errors;
- Backup cleanup and any future restore.

QueryHub subscriptions intentionally remain available regardless of caller identity and are not an authorization boundary. General HTTP fetch authorization is a separate future decision. Full paths are normal informational output from scoped Version 1 contracts, but they never become operation authority. The accepted demo role-management and bootstrap posture is not production-ready. Production storage roots must be reachable only by the approved worker identity.

Audit should retain the authenticated principal, server-derived operator identity, row/roll/box/workspace scope, validated inputs, schema/configuration/plan/resource versions, acknowledgements, idempotency key, status transitions, scan/cancel actor and reason, per-item and backup outcomes, resulting versions, and a redacted diagnostic correlation ID.

## Initial error catalog

Use one stable issue envelope with code, severity, safe message, optional field/resource reference, acknowledgement flag, retryability, and correlation ID.

Initial code groups should cover:

- invalid, missing, mismatched, or ineligible row/roll context;
- active, inactive, transitioning, or wrong-owner scan state;
- forbidden operation/resource and restricted diagnostic values;
- unsupported setting, range, combination, or schema version;
- expired resource reference, stale resource/plan, lock conflict, or root unavailability;
- invalid path, collision, missing/ambiguous/malformed source, backup/staging/replace failure;
- expired plan, missing acknowledgement, missing/retired job, or disallowed cancellation;
- idempotency reconciliation, mismatch, or retired record;
- transient service/storage failure and explicit reconciliation-required state.

Never return stack traces, credentials, service-account details, or arbitrary paths outside the scoped operation/workspace result.

## Delivery sequence

1. Security and Operations foundation: registered-user identity, manager-defined roles, global assignments, topic-enforced operation permissions, worker identity, approved roots/ACLs, redaction, logs, metrics, recovery ownership.
2. Canonical row boundary, contracts, and roll state: retire legacy row compatibility; preserve regular/custom parity; add durable scan state/version, row context, errors, OpenAPI, deterministic fixtures, and contract tests.
3. Scan vertical slice: discovery, idempotent Start, async folder creation, Finish, Abandon, audit, collision and restart tests.
4. Shared plan/job platform: resource references, Preview, Apply, leases, durable jobs, cancellation, results, paging, history, reconciliation.
5. QPF schema and Preview against representative production files.
6. QPF settings mutation with staged replacement, verified backups, failures, and restart tests.
7. Frames output paths and directory-creation ordering.
8. Production qualification under the real Windows service identity and approved roots.

## Required approvals

Product should approve:

- editable scan folder names and collision failure;
- non-blocking file checks at Finish;
- separate grayscale/bitonal roots;
- exact QPF enum meanings, ranges, prefixes, and absent-setting rules.

Security should approve:

- registered-user identity resolution plus role/permission events and topic decisions;
- operation/resource and restricted-diagnostic response policies; QueryHub remains identity-independent and general HTTP fetch authorization is deferred;
- production replacement for the intentionally unhardened demo role-management and bootstrap endpoints;
- revocation and error-visibility policy;
- worker identity, root ACLs, and audit/redaction rules.

Operations should approve:

- root configuration and ownership;
- service account or gMSA;
- backup and retention schedules;
- SMB availability expectations;
- monitoring, stuck-job handling, restart reconciliation, and recovery runbooks.

QA should approve and execute the contract, authorization, traversal/reparse-point, idempotency, multi-user/multi-instance, backup failure, malformed file, partial failure, cancellation, restart, and target-host evidence plan.

Start, Finish, Preview, and Apply should remain disabled in production until those approvals and the required live evidence exist.
