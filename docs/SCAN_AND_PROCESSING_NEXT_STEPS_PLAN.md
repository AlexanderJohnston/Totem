# Scan and Processing Next Steps Plan

Last updated: 2026-08-10

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
- No Start, Finish, Preview, Apply, filesystem, role/permission topic, idempotency, worker lease, recovery, or production-enablement implementation exists yet.
- The existing authentication system can identify registered users, but it is not yet the durable role/permission authority described here.

### Proposed sequence

```text
Role/permission and storage-binding foundations
        ↓
Durable role assignment and operation-authorization topics
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

### Proposal

- Define a stable permission catalog for Scan and Processing. Initial candidates are:
  - `scan.discover`
  - `scan.start`
  - `scan.finish-own`
  - `scan.finish-any`
  - `scan.abandon`
  - `processing.discover`
  - `processing.preview`
  - operation-specific Apply permissions
  - `job.cancel-own`
  - `job.cancel-any`
  - sensitive-error, backup-cleanup, and future restore permissions
- Represent role definitions, role assignments, revocations, and permission changes as durable facts.
- Let managers define named roles and select permissions through HTTP contracts intended for the frontend team.
- Make role assignments global in Version 1.
- Defer backend manager-only enforcement for role-definition and assignment endpoints during the demo. Record this explicitly as production-hardening debt.
- Add a temporary admin bootstrap endpoint that accepts an existing username and plaintext secret, compares the secret to the hard-coded demo value, and grants manager status when the user exists.
- Never write the bootstrap secret to events, logs, metrics, responses, fixtures, or committed documentation. Remove or harden the endpoint before production enablement.
- Scan and Processing commands contain a server-resolved registered actor ID. They never accept client-supplied roles or permissions as authority.
- Operation authorization is decided in topics. Rejections produce stable durable facts such as forbidden operation or forbidden scope, which the HTTP layer maps to safe responses. Role-management and bootstrap validation use stable contract errors.
- Audit role and permission changes with the available acting identity, target user, previous and resulting roles, reason where required, and timestamp.
- QueryHub remains unchanged. It accepts ETag subscriptions regardless of caller identity and grants no fetch or operation authority.

### Topic composition to design before implementation

The exact event routing must preserve both durable authorization and roll-level atomicity. A candidate flow is:

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

The authorization fact would be bound to one operation request ID, actor, permission, scope, and authorization revision so it cannot be replayed for a different request. This is a candidate, not yet an approved topology; we must verify it against Totem routing, ordering, replay, revocation, and multi-instance behavior.

### Remaining implementation decisions

- Verify the exact topic topology for evaluating operation permissions without moving Scan/Processing authorization into HTTP code.
- Unless Product overrides it, use forward-only revocation: authorization decisions ordered after revocation fail, while already accepted operation history is not rewritten.
- Define the temporary bootstrap route and response envelope without exposing the fixed secret or whether a guessed username exists when the secret is wrong.

## 2. Worker identity and storage-root policy

### Proposal

- Introduce one active stable logical storage binding per client/workspace, containing a binding ID, configuration generation, and allowed capabilities.
- Store only the binding ID and generation in durable domain state.
- Keep physical local or UNC roots exclusively in Operations-owned `Quantum.Service` configuration.
- Run `Quantum.Service` as `CMGX\appdevsvc`, which has the required access configured. Do not store credentials in application configuration or KurrentDB, and still capture target-host service/share evidence before production mutation.
- Model distinct capabilities for the scan parent, QPF access, grayscale Frames, and bitonal Frames. Two capabilities may intentionally reference the same approved root.
- Let the worker publish safe discovery/resource results. `Quantum.Web` serves opaque references but never resolves or accesses the physical path.
- Invalidate discoveries and plans when the durable binding or configuration generation changes.
- Use developer-admin API commands to create and activate the logical binding generation. Match it to the Operations-owned worker configuration generation.
- Use `\\sbsr-film\film\` as the initial production share root. The existing Server entity is not part of storage resolution.
- Use configured parent choices in Version 1; do not implement arbitrary directory browsing.
- Prefer labels in normal responses. When users must check completed work, return an informational full path from an operation result or deliberately client/workspace-scoped query; never accept that path back as authority.
- Do not automatically expire resource references in Version 1. Continue use-time authorization and binding-generation validation.

### Implementation details to settle in code and contracts

- Define the logical binding command/event/projection names and developer-admin API route shapes.
- Define capability-relative configured locations beneath the approved root without persisting physical paths in domain state.
- Define the workspace-scoped path result/query shape without turning returned paths into authority.
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

1. **Role and permission foundation:** add manager-defined roles, global assignments, the temporary demo bootstrap contract, durable role/assignment events and projections, operation-authorization decisions, audit, and tests. No operation mutation yet.
2. **Storage-binding contract:** add logical bindings, configuration generations, capabilities, opaque references, redaction rules, deterministic fixtures, and no filesystem access.
3. **Durable Start acceptance:** add topic-owned permission decision, command-side roll authority, business-version checks, idempotency, accepted/rejected facts, audit, and lost-response tests. No folder creation.
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
