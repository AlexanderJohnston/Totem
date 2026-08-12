# Scan and Processing Next Steps Plan

Last updated: 2026-08-11

Status: Working plan for team review. This document separates settled direction from decisions that still need Product, Backend, Security, Operations, or QA input. It does not claim production readiness.

Authoritative policy: `SCAN_AND_PROCESSING_BACKEND_POLICY.md`

## Settled decisions

1. Miller operation identity is `rollId + rowId`. Cells and profiles are presentation only.
2. Regular and custom rows have identical operation identity, permissions, and behavior. `origin` is provenance only.
3. QueryHub remains Totem's identity-independent ETag subscription mechanism. It will not be redesigned or protected as part of Scan and Processing.
4. QueryHub is invalidation only. HTTP remains the state-retrieval fallback, and query ETags never become business concurrency tokens.
5. Authorization of general HTTP query/fetch endpoints is a separate possible future change and is not part of this plan.
6. Scan and Processing operations are authorized domain actions. Registered or authenticated status alone grants no operation authority.
7. Managers define roles and assign them globally to registered users. Role definitions, assignments, and permission changes are durable events; Scan/Processing authorization outcomes are topic decisions rather than ASP.NET endpoint authorization.
8. The HTTP layer resolves the authenticated registered actor, validates transport input, appends commands, and maps durable outcomes to responses. It does not make the authoritative role/permission decision.
9. `Quantum.Web` never touches production Formatic storage. `Quantum.Service` is the only filesystem worker.
10. Production mutation stays disabled until the applicable storage, idempotency, concurrency, recovery, filesystem-safety, and target-host gates pass.
11. A client is the Version 1 workspace, and each client has exactly one active logical storage binding.
12. The existing Server entity remains a separate identifier and does not resolve storage.
13. Developer-admin API commands may create and activate logical binding generations for now.
14. The initial production share root is `\\sbsr-film\film\`, and the approved worker identity is `CMGX\appdevsvc`.
15. Labels are the primary resource presentation. An operation result or deliberately client/workspace-scoped query may return a full informational path for work verification; the path never becomes valid operation input.
16. Version 1 resource references do not expire automatically. Binding-generation changes, scope changes, permission changes, explicit revocation, or a future explicit expiry still invalidate their use.
17. Configured parent choices are sufficient for Version 1; arbitrary filesystem browsing is deferred.
18. Managers define roles and choose their permission sets. Role assignments are global in Version 1.
19. Role-definition and assignment endpoints exist for frontend use. Backend manager-only enforcement for those management endpoints is deferred for the demo.
20. A temporary admin endpoint bootstraps any existing registered user as a manager when the request body contains the correct hard-coded plaintext demo secret. The secret is never persisted or logged, and this mechanism is not production-ready.
21. Full informational paths are normal output from the scoped Version 1 contracts and never become path authority.

## Current implementation baseline

- Work package 1 retired client-scoped row compatibility and established the canonical roll boundary.
- Work package 2 added durable read-side scan state, the operation context, a business `resourceVersion`, structured issues, fixtures, and tests.
- Work package 3 now adds the durable role/permission authority, global assignment and revocation, temporary bootstrap manager grant, request-bound operation decisions/rejections, audit facts, projections, frontend management routes, and deterministic fixtures.
- Work package 4 now adds client-routed logical bindings, explicit configuration generations, fixed logical capabilities, developer-admin configuration routes, durable rejection/audit facts, opaque-reference validation contracts, projections, and deterministic fixtures.
- No Start, Finish, Preview, Apply, physical mapping/resolution, filesystem, idempotency, worker lease, recovery, or production-enablement implementation exists yet.
- Registered operation actors now use the stable application user ID from server-issued cookie claims. The existing file-backed user store remains a Web deployment boundary and has not been qualified for shared multi-host use.

### Proposed sequence

```text
Logical storage-binding foundation (implemented)
        ↓
Roll command authority + idempotent Start acceptance
        ↓
Worker claim/lease + scan-folder creation
        ↓
Finish/Abandon + restart reconciliation
        ↓
Processing discovery/Preview
        ↓
QPF/Frames Apply and safe filesystem mutation
        ↓
Live Windows qualification
        ↓
Production enablement
```

## 1. Role, permission, and QueryHub boundary

### Implemented demo foundation

- The stable Version 1 permission catalog is:
  - `scan.discover`
  - `scan.start`
  - `scan.finish-own`
  - `scan.finish-any`
  - `scan.abandon`
  - `processing.discover`
  - `processing.preview.qpf-settings`
  - `processing.preview.frames-paths`
  - `processing.apply.qpf-settings`
  - `processing.apply.frames-paths`
  - `job.cancel-own`
  - `job.cancel-any`
  - `history.view-context`
  - `history.view-workspace`
  - `sensitive-error.view`
  - `backup.cleanup`
- Role definitions, role assignments, revocations, permission changes, manager grants, operation decisions/rejections, and access audit entries are durable facts owned by one globally ordered access topic.
- Roles are named and versioned, role assignments are global, and role replacements use expected-version concurrency.
- Backend manager-only enforcement remains deliberately deferred for the demo. Role and assignment management still requires a registered authenticated cookie actor.
- The temporary bootstrap endpoint validates the runtime-supplied fixed secret before username lookup, appends a secret-free manager command for an existing registered user, and returns stable secret-free errors. It must still be removed or hardened before production enablement.
- Operation authorization uses the stable registered user ID and current durable assigned roles. Manager status, usernames, display labels, process-user labels, registration, and authentication alone grant no operation permission.
- Decision facts bind one request ID, actor, fixed catalog permission, server-resolved scope, effective role IDs, and authorization revision. Forward-only revocation is proven for decisions ordered after assignment revocation or permission removal.
- Audit facts carry the available acting identity, target identity, previous/resulting roles or permissions, result, safe code, revision, and timestamp without credentials or physical paths.
- QueryHub remains unchanged. It accepts ETag subscriptions regardless of caller identity and grants no fetch or operation authority.

### Implemented ordering and later cross-topic boundary

Work package 3 implements the first three stages below in one single-instance access topic. Work package 5 must add the roll stage without weakening request binding or roll-level atomicity:

```text
HTTP request with server-resolved actor
        ↓
durable operation request
        ↓
role/permission topic emits authorized or rejected decision
        ↓
roll topic consumes the authorized request and atomically checks
resourceVersion, scan state, row identity, and idempotency
        ↓
accepted or rejected roll facts
```

The authorization fact is bound to one operation request ID, actor, permission, resolved scope, effective roles, and authorization revision, so it cannot authorize a different request. In-memory replay and independent-reducer evidence is complete; deployed multi-instance and cross-topic roll-acceptance evidence remain open.

### Remaining implementation decisions

- Define the exact access-decision-to-roll-acceptance routing for work package 5, including how the roll validates request binding and authorization revision without moving permission evaluation into HTTP.
- Qualify durable ordering under multiple deployed Web/Service instances and replace or qualify the current file-backed registered-user deployment boundary.
- Remove or harden the temporary bootstrap route and enforce manager-only role management before production enablement.

## 2. Worker identity and storage-root policy

### Implemented logical contract

- One client-routed topic owns each Version 1 workspace's logical binding state and projects exactly one active binding/generation pair.
- Create uses a server-generated stable binding ID and path-free labels for exactly four distinct logical capabilities: Scan parent, QPF, grayscale Frames, and bitonal Frames.
- Activate requires the current `storageRevision` and the next generation for that binding. Repeating the active pair is unchanged; switching back to an earlier binding requires its next generation.
- Durable state and audit contain logical IDs and generations only. The existing Server entity, roots, paths, and credentials do not participate.
- Registered developer-admin read/create/activate routes live under `/api/scan-processing/storage-bindings/clients/{clientId}`. Production authorization hardening for these configuration routes remains open.
- A server-side opaque-reference record binds actor, exact resolved scope, purpose, permission, access revision, binding, generation, and capability. Current replayed access and binding state are required at use time.
- Reference revocation, permission/access-revision change, scope change, binding switch, or generation change invalidates use. Version 1 references have no automatic expiry.
- Configured logical capability choices replace arbitrary browsing. An output-only informational path shape exists, but Web does not resolve it and no package 4 request accepts a path.

### Remaining physical and deployment work

- Keep physical local or UNC roots exclusively in Operations-owned `Quantum.Service` configuration and map them by binding ID, generation, and logical capability.
- Define and implement capability-relative physical configuration beneath the approved root without persisting paths in domain state.
- Have a later worker publish scoped informational paths and resolve opaque IDs; `Quantum.Web` must continue to avoid physical resolution.
- Run `Quantum.Service` as `CMGX\appdevsvc` only after target-host service/share evidence is captured; do not store credentials in application configuration or KurrentDB.
- Use `\\sbsr-film\film\` only as the approved future Operations input. Package 4 did not inspect or access it.
- Prove the configured service logon plus share and NTFS access on the target host before production mutation.

## 3. Idempotency and durable Start Scan

Idempotency is part of the first mutation slice, not a later enhancement.

### Proposal

- Establish one roll-scoped command authority for current operation revision, scan state, active scan identity, accepted actions, and eventually the roll mutation lease.
- Share or centralize the roll-state/version reducer so command decisions and `RollOperationQuery` cannot drift.
- After topic-owned permission approval, atomically validate:
  - exact `rollId + rowId` identity;
  - expected business `resourceVersion`;
  - idle scan state;
  - opaque parent-resource reference and binding generation;
  - folder name and notes;
  - idempotency key and semantic-input hash.
- Persist `ScanStartAccepted`, scan ID, idempotency record, audit, new roll version, and `scanState=starting` before returning `202 Accepted`.
- Scope idempotency by stable actor ID, command kind, roll, and key.
- Same key and same semantic input returns the original action.
- Same key with different semantic input returns `IDEMPOTENCY_KEY_MISMATCH`.
- Retain the proposed record for 30 days after the action becomes terminal.
- Prove reconciliation when acceptance is durable but the HTTP response is lost.

This package performs no filesystem work and remains behind a disabled mutation-intake gate.

## 4. Filesystem mutation

Filesystem implementation is divided into Scan folder creation and later Processing Apply.

### Scan folder creation

- The worker claims an accepted action using a durable fenced lease.
- It resolves the opaque binding using the worker-owned configuration generation.
- It rejects traversal, rooted child input, invalid Windows names, unexpected normalization, root escape, reparse points, junctions, mount points, and symlinks.
- An existing target folder is a collision. It does not imply resume or reuse.
- The worker creates the folder and then records `ScanStarted` and `scanState=active`.
- A proven failure before creation records `ScanStartFailed` and safely returns the roll to idle.
- An uncertain outcome becomes explicit reconciliation work rather than an automatic retry or state reset.

### Processing Apply

- Select exactly one deterministic top-level QPF.
- Capture and compare the source hash/version.
- Parse with DTD and external entity resolution disabled.
- Preserve unknown XML and apply only versioned schema operations.
- Write and validate a same-directory staging file.
- Create and verify the required backup.
- Recheck the source hash and lease fencing token.
- Atomically replace the source on the same volume.
- Persist the resulting hash/version and item outcome.
- A backup failure leaves the affected QPF unchanged.
- Required Frames directories are created before QPF replacement; failure blocks the affected QPF update.

## 5. Recovery

### Proposal

- On worker startup, find non-terminal actions and jobs.
- Acquire a new fenced lease before continuing.
- Compare durable checkpoints with folders, source hashes, staging files, backups, and expected resulting hashes.
- Resume only effects proven not to have committed.
- Never blindly repeat an uncertain folder creation or file replacement.
- Record ambiguous effects as `WORK_REQUIRES_RECONCILIATION`.
- Provide an operator workflow to inspect, retry only where proven safe, abandon, or record terminal reconciliation failure.
- Keep HTTP status/history retrieval available when QueryHub notifications are missed.

### Unresolved Scan ownership evidence

If the worker creates a folder and crashes before recording `ScanStarted`, finding the folder afterward does not prove who created it. Candidate ownership evidence includes:

- a small worker-owned marker containing the `scanId`, created with create-new semantics;
- a controlled staging-directory and rename protocol where the target filesystem proves the required semantics;
- another Operations-approved durable correlation mechanism.

Without reliable ownership evidence, the outcome must require reconciliation rather than automatic reuse.

## 6. Production enablement

Use separate intake and execution controls rather than one global switch:

- Scan discovery
- Scan Start/Finish/Abandon intake
- Processing discovery/Preview
- Processing Apply
- Worker execution
- Emergency worker pause

Disabling new intake must not silently strand accepted work. Read/status/history remains available, and emergency pause preserves explicit queued, transitioning, or reconciliation states.

QueryHub protection is not an enablement gate. QueryHub functional behavior and HTTP refetch fallback still require regression coverage.

## 7. Live Windows-service evidence

Final qualification occurs under the real worker identity and approved roots, not only on a development machine. Evidence should cover:

- local and UNC access under the actual service account or gMSA;
- service restart before and after every filesystem checkpoint;
- network/share loss, ACL changes, and root unavailability;
- collisions and path-containment attacks;
- reparse points, junctions, mount points, and symlinks;
- multi-instance lease and fencing behavior;
- backup failure, staging failure, malformed QPFs, and partial completion;
- direct inspection of folders, QPFs, staging files, backups, durable history, idempotency, and audit;
- QueryHub subscription/reconnect behavior and HTTP fallback, without adding subscription authorization;
- logs, correlation IDs, worker heartbeat, stuck-work alerts, and recovery rehearsal;
- explicit Backend, Product, Security, Operations, and QA approval.

## Proposed implementation packages

1. **Role and permission foundation — implemented:** manager-defined versioned roles, global assignments/revocations, temporary demo bootstrap contract, durable decisions/rejections/audit, projections, frontend management routes, fixtures, and focused tests. No operation mutation.
2. **Storage-binding contract — implemented:** client-routed logical bindings, sequential configuration generations, distinct capabilities, server-side opaque-reference validation, redaction rules, deterministic fixtures, and no filesystem access.
3. **Durable Start acceptance — next:** add topic-owned permission decision, command-side roll authority, business-version checks, idempotency, accepted/rejected facts, audit, and lost-response tests. No folder creation.
4. **Scan worker slice:** add worker claim/lease, approved-root resolution, path policy, folder creation, collision handling, and fault/restart reconciliation tests in controlled storage.
5. **Finish and Abandon:** add ownership rules, notes, permission decisions, durable history, version changes, reconciliation, and tests.
6. **Processing discovery and Preview:** expose only QPF settings and Frames paths initially; generate versioned deterministic plans without mutation.
7. **Shared Apply/job platform:** add leases, durable jobs, cancellation, item results, paging, history, and restart recovery.
8. **QPF and Frames mutation:** implement staged/validated/backup-protected replacement and directory-creation ordering.
9. **Production qualification:** execute the target-host evidence plan and enable each intake gate only after applicable approval.

## Decisions for the team to work through next

1. Exact authorization topic/operation topic interaction.
2. Forward-only revocation implementation and ordering evidence.
3. Scan defaults: editable folder name, collision failure, notes limit, Finish file checks, finish-any reason, and Abandon behavior.
4. Scan-folder ownership evidence for restart reconciliation.
5. Lease duration, heartbeat, fencing, and multi-instance policy.
6. Production gate ownership, emergency pause semantics, monitoring, role-management hardening, bootstrap removal, and operator recovery workflow.
