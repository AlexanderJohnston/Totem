# Scan and Processing Backend Policy

Status: Draft for Backend, Product, Security, and Operations review  
Last updated: 2026-08-11
Frontend baseline: `406b352d996f101350a48d2636625ff312c6be12`

## 1. Purpose and scope

This document defines the proposed backend policy for the Miller **Scan** and **Process** actions. It is deliberately editable and is not evidence that the feature is implemented or production-ready.

The first production processing capabilities are:

1. QPF settings.
2. Frames output paths.

Later Formatic operations may reuse the same discovery, plan, job, history, security, and filesystem foundations only after their behavior is separately approved.

The words **MUST**, **MUST NOT**, **SHOULD**, and **MAY** are normative. Items marked **Proposed default** require the named reviewers to approve or edit them before implementation becomes a production contract.

## 2. Guiding policy

1. Miller remains the primary application surface. Scan and Process each operate on one captured Miller row identified by `rollId + rowId`.
2. The durable roll resource is the operational authority. Profile fields and visible table columns are presentation only.
3. `isScanning` is durable roll state. It is not a profile field, inferred value, or modal-local flag.
4. The frontend captures immutable operation context when a modal or panel opens. Later table selections cannot retarget it.
5. Browser closure never changes scan state and never stops durable processing work.
6. Browser code never performs authoritative path resolution, filesystem access, XML/INI mutation, backup handling, or collision resolution.
7. Browser input never supplies an arbitrary local or UNC path as authority. It refers only to backend-issued, opaque authorized resources.
8. Processing mutations follow `Discover -> Configure -> Preview -> Confirm -> Apply -> Durable job`.
9. Apply references an accepted server plan. It does not resubmit a client-interpreted list of effects.
10. Cancellation is cooperative. It does not roll back completed items.
11. Partial completion and per-item outcomes are durable first-class states.
12. Production mutation remains disabled until authorization, filesystem, concurrency, recovery, and target-host evidence passes the gates in this document.

## 3. Target architecture and ownership

```text
Browser
   |
   | HTTP operations + QueryHub ETag invalidations
   v
Quantum.Web
   |  resolve authenticated actor, validate DTOs, append commands, serve projections/OpenAPI
   v
KurrentDB / durable application state
   |  topic decisions, roles, permissions, rolls, scans, plans, jobs, idempotency, audit
   v
Quantum.Service
   |  resolve authorized resources and perform filesystem work
   v
Approved Windows storage roots / UNC shares
```

- `Quantum.Web` MUST NOT access production Formatic storage.
- `Quantum.Service` is the only application process permitted to resolve resource references and read or mutate production files.
- A durable command append and command processing are distinct events. Mutation APIs MUST support reconciliation when the command was durably accepted but the HTTP response was lost.
- QueryHub is an invalidation/notification transport, not the source of record. HTTP retrieval remains the bounded fallback.
- Totem QueryHub ETag subscriptions intentionally remain available regardless of caller identity. QueryHub is not an authorization boundary and subscribing does not grant authority to fetch data or perform an operation.
- Authorization of general HTTP query/fetch endpoints is a separate future decision and is not a production-mutation gate in this policy.
- Production storage access MUST use an Operations-approved domain service account or gMSA when remote shares are involved. Virtual service accounts MUST NOT be assumed to have the required remote identity.

## 4. Durable roll and Miller row policy

### 4.1 Source of authority

The durable roll model owns operational scan state and versioning. A Miller row projection exposes that state independently of profile visibility.

All Miller rows MUST be roll-scoped and addressed by `rollId + rowId`. For Version 1, the existing client is the workspace, so `workspaceId` is the resolved `clientId`. The backend resolves the canonical roll, box, client/workspace, and roll name from `rollId`; cell values, including `cells.rollName`, are never operation identity or lookup keys. The existing Server entity remains a separate identifier and MUST NOT be used to select or resolve the storage binding.

Regular and custom rows are equally eligible for Scan and Process. `origin` records provenance only: regular rows came from WASP data and custom rows were entered manually when WASP data was unavailable. Origin MUST NOT change operation identity, authorization, capabilities, or behavior.

The supported table model consists only of roll-scoped regular and custom rows. Remove legacy client-scoped rows, routes, and compatibility lookup paths, converting any remaining data before Scan and Process are enabled.

### 4.2 Operation row context

The logical response shape is:

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

Policy:

- `isScanning` MUST be encoded as a JSON Boolean.
- `isScanning` MUST be returned regardless of profile visibility.
- `isScanning` is true while `scanState` is `starting`, `active`, or `finishing`.
- `scanState` prevents clients from treating accepted-but-not-yet-created folders as active scans that can already be finished.
- `resourceVersion` is an opaque business concurrency token, normally derived from the durable roll stream revision.
- Query ETags are cache/invalidation tokens and MUST NOT substitute for `resourceVersion`.
- Successful commands SHOULD return refreshed operation context. Clients MUST also tolerate invalidation followed by refetch.

### 4.3 Roll-to-storage mapping

The server resolves a row using durable relationships:

```text
row -> roll -> box -> client (the Version 1 workspace) -> approved storage binding
```

The frontend supplies `rollId + rowId`. The backend verifies that the row belongs to the roll, resolves the remaining scope from the durable roll mapping, and does not accept names, client IDs, box IDs, or workspace IDs as alternate operation lookup keys.

Each client/workspace has exactly one active logical storage binding in Version 1. The binding may expose the distinct Scan parent, QPF, grayscale Frames, and bitonal Frames capabilities; capabilities may intentionally resolve beneath the same approved root. Domain state stores only the stable binding ID and configuration generation, never the physical root.

Developer-admin API commands create or activate a client's binding ID, generation, labels, and capability set. `Quantum.Service` configuration maps that same binding ID and generation to the physical root. Activating a different binding or generation invalidates outstanding discoveries and plans. The Server entity does not participate in this mapping.

Current implementation note: work package 4 now provides one client-routed durable logical-binding stream per Version 1 workspace. Create generates the stable binding ID on the server. Activation requires the current workspace `storageRevision` and exactly the next configuration generation for that binding; repeating the already-active pair is durably unchanged. Switching back to a previously active binding therefore requires its next generation and cannot revive an older reference. This is logical configuration evidence only; Operations mapping and all physical resolution remain unimplemented.

## 5. Scan policy

### 5.1 Discovery

Scan discovery MUST return:

- canonical row and roll context;
- current `resourceVersion`, `isScanning`, and `scanState`;
- active scan ID, folder name, and notes when applicable;
- one opaque parent directory reference with only the authorized capabilities;
- a suggested folder name derived from canonical roll data;
- display/redacted path only when the actor may view it;
- server folder-name rules;
- structured blockers and warnings.

Resource discovery does not grant lasting authority. Start and Finish MUST reauthorize and revalidate current versions.

### 5.2 Start Scan

Logical input:

```ts
interface StartScanCommand {
  rowId: string;
  rollId: string | number;
  expectedResourceVersion: string;
  parentResourceRefId: string;
  folderName: string;
  notes: string;
  idempotencyKey: string;
}
```

Start is a durable asynchronous action:

1. Web resolves the authenticated registered actor and validates the transport request; durable role/operation topic decisions authorize the operation from role and permission facts before the roll accepts it.
2. A `ScanStartAccepted` record, scan ID, idempotency record, audit record, new roll version, and `scanState=starting` are persisted before the response reports acceptance.
3. `isScanning` becomes true when the start is durably accepted, not after the share operation completes.
4. The worker resolves the parent reference, revalidates containment and access, then creates the folder.
5. Successful creation records `scanState=active` and the resolved resource identity.
6. Failure records `ScanStartFailed`, a safe reason, and returns the roll to `scanState=idle` and `isScanning=false` unless an already-created folder requires explicit operator reconciliation.

The API SHOULD return `202 Accepted` with the scan ID and current roll context. A repeated idempotency key for the same actor, command kind, roll, and semantic input returns the same accepted action. Reuse with different semantic input fails with `IDEMPOTENCY_KEY_MISMATCH`.

### 5.3 Finish Scan

Logical input:

```ts
interface FinishScanCommand {
  scanId: string;
  rowId: string;
  rollId: string | number;
  expectedResourceVersion: string;
  notes: string;
  idempotencyKey: string;
}
```

Finish MUST:

1. verify the scan belongs to the canonical row and roll;
2. authorize the actor;
3. require `scanState=active` and the expected current resource version;
4. persist final notes as an append-only scan fact;
5. persist the finish record, audit, new roll version, `scanState=idle`, and `isScanning=false` together;
6. retain the scan history and return refreshed roll context.

**Proposed default:** Version 1 does not require expected files to exist before Finish. The worker may capture counts or warnings for operator review, but a file-content policy must be defined before it can block Finish.

### 5.4 Scan decisions

The following are proposed Version 1 defaults:

| Topic | Proposed policy |
| --- | --- |
| Concurrent scan | At most one `starting`, `active`, or `finishing` scan per roll. |
| Folder name | Operator may edit the suggestion; the server validates Windows-invalid characters, reserved names, trailing dots/spaces, normalization, and configured length. |
| Existing folder | Fail. Do not infer resume or reuse from name alone. Reconciliation is an explicit operator workflow. |
| Notes | Persist at Start and Finish as append-only entries; trim surrounding whitespace; maximum 2,000 characters per entry. |
| Finish ownership | An actor may finish their own scan; finishing another actor's scan requires a separate permission and audit reason. |
| Abandon | Separate command and permission; records an outcome but never deletes the folder. |
| Processing while scanning | Discovery and history are allowed. New Preview plans and Apply are blocked. |
| Folder creation | Durable asynchronous worker action. |
| Idempotency retention | 30 days after the action becomes terminal. |

## 6. Processing discovery and capabilities

Discovery MUST return the canonical roll/workspace context, resource version, active related work, schema/configuration versions, opaque resources, warnings, and operation capabilities.

```ts
interface OperationCapability {
  kind:
    | 'qpf-settings'
    | 'frames-paths'
    | 'idf-to-qpf'
    | 'organize-qpf'
    | 'batch-create'
    | 'batch-combine'
    | 'batch-split'
    | 'backup-cleanup';
  availability: 'available' | 'blocked';
  schemaVersion: string;
  blockedBy: ProcessingIssue[];
}
```

- Unauthorized capabilities SHOULD be omitted rather than returned as `hidden`, so discovery does not disclose unavailable operations.
- `blocked` means the actor may know about the operation but current context prevents it.
- Version 1 returns only `qpf-settings` and `frames-paths` in production.
- An active or transitioning scan blocks creation of a new mutation plan and Apply.
- Discovery is read-only and MAY proceed while scanning.

## 7. Authorized resource and path policy

```ts
interface AuthorizedResourceRef {
  id: string;
  kind: 'workspace' | 'box' | 'roll' | 'directory' | 'qpf' | 'idf' | 'queue';
  displayName: string;
  displayPath?: string;
  version: string;
  expiresAt?: string;
  capabilities: Array<'read' | 'write' | 'create-child'>;
}
```

An authorized resource reference:

- is opaque, actor-bound, scope-bound, versioned, and purpose-bound;
- is not a substitute for authorization at use time;
- MUST NOT embed a client-editable authoritative path;
- MUST be rejected after an explicit expiry or revocation, scope change, configuration generation change, or permission change;
- uses labels as the primary display value;
- MAY expose a full display path as informational output in a deliberately workspace-scoped query or operation result so a user can check completed work; that path never becomes operation authority or valid path input.

Version 1 resource references do not expire automatically. Future automatic expiry may be added without changing the rule that use-time authorization and binding-generation validation are required.

Work package 4 defines the server-side Version 1 reference record and deterministic use-time validator. The authoritative record is bound to actor ID, exact resolved scope, purpose, permission, access revision, active binding, configuration generation, and one logical capability. The browser will later submit only the opaque reference ID. Package 4 does not issue references from a public discovery route and does not resolve informational paths; those operation and worker boundaries remain later packages.

Storage policy:

1. Operations owns the allow-listed roots, service identity, ACLs, availability expectations, and configuration change process.
   - The initial production share root is `\\sbsr-film\film\`.
   - The approved worker identity is `CMGX\appdevsvc`; its live service logon and share/NTFS access still require target-host evidence before production mutation is enabled.
2. Only explicit local absolute roots or UNC roots are allowed. Mapped drives, device paths, alternate data streams, and environment-expanded client input are forbidden.
3. The worker rejects traversal, rooted child input, invalid Windows names, unexpected case/normalization results, and any final target outside the configured root.
4. Reparse points, symlinks, mount points, and junctions are rejected by default. If later allowed, containment MUST be verified using resolved final handles for every traversed component.
5. Version 1 exposes configured parent choices only. Arbitrary child browsing is deferred. A future browsing contract may accept a parent reference and server-validated child selector, never a path.
6. Share unavailability and changed ACLs surface as structured, retry-aware errors without leaking unauthorized paths.
7. Cross-volume file moves are not assumed atomic and require a separately designed copy/verify/commit/delete workflow.

## 8. Preview plan policy

A server-generated plan is immutable and contains:

- plan ID and version;
- actor and captured row/roll/workspace scope;
- operation, schema, configuration, discovery, and resource versions;
- source content hashes where applicable;
- creation and expiry timestamps;
- bounded or paged effects;
- discovered/change/create/skip/block counts;
- collisions, warnings, blockers, and required acknowledgements;
- expected destinations and backup policy;
- partial-completion semantics;
- a confirmation ID and safe confirmation text.

**Proposed default:** Plans expire 15 minutes after creation.

Changing any configuration input invalidates the displayed plan. Preview MUST perform the same resolution and validation logic used by Apply wherever practical. A dry run that executes materially different selection, normalization, or collision logic is not an acceptable plan.

## 9. Apply, jobs, and idempotency

Apply input contains only:

- accepted plan ID and version;
- required acknowledgement IDs;
- expected captured resource version;
- idempotency key.

Apply MUST revalidate actor authorization, plan expiry, schema/configuration versions, roll/resource versions, current source hashes, locks, scan state, path policy, destinations, collisions, and required acknowledgements.

Before `202 Accepted` is returned, the backend MUST durably persist:

- the idempotency record;
- durable job ID;
- captured plan/scope;
- initial `queued` status and version;
- audit record.

Repeated Apply with the same key and semantic input returns the same job. The same key with different semantic input fails. A lost or timed-out response is reconciled by idempotency key before a new job may be created.

Required job states are:

```text
queued
running
cancel_requested
completed
completed_with_errors
failed
cancelled
```

Every state change increments a monotonic job version. Progress, where known, includes `total`, `attempted`, `succeeded`, `skipped`, `failed`, and `remaining`.

## 10. Locking and conflict policy

**Proposed Version 1 policy:**

- A roll has one exclusive mutation lease shared by Scan transitions and Processing Apply jobs.
- Discovery and history do not acquire the mutation lease.
- Plan creation validates that no scan is active and no conflicting mutation exists, but it does not reserve the roll until expiry.
- Apply revalidates and atomically acquires the roll lease before queuing execution. If it cannot, it returns `RESOURCE_LOCKED` rather than silently queueing behind an unknown change.
- The lease is durable, has an owner/fencing token and heartbeat, and works across service instances.
- File-level work also checks the source version/hash immediately before replacement.
- Stale resource or plan versions return a structured conflict and require rediscovery/replanning.
- A worker that loses its lease MUST stop before the next mutation checkpoint.

Operation-specific finer locks may be added later, but MUST preserve roll-level conflict safety until multi-resource behavior is proven.

## 11. Cancellation and per-item results

Cancellation is a request, not rollback:

- `cancel_requested` is non-terminal.
- The worker observes cancellation between items and before each irreversible commit.
- An item already committed remains succeeded.
- The terminal state is `cancelled` when unattempted work remains because cancellation was honored.
- A job with completed and failed items may be `completed_with_errors`; the job contract MUST not relabel completed effects as undone.

Each item result contains:

- stable item/resource ID and safe label;
- `succeeded`, `skipped`, `failed`, or `not-attempted` outcome;
- stable error code and redacted message;
- backup outcome;
- start/end timestamps where useful;
- resulting resource version/hash;
- retry eligibility.

Large results MUST be paged with stable ordering. Retrying selected failures creates a new plan and job linked to the original; it never mutates original history.

## 12. Safe file mutation policy

Every QPF mutation uses this item boundary:

1. Open and identify the authorized source without following an unapproved reparse point.
2. Capture its version/hash and compare it with the plan.
3. Parse with DTD and external entity resolution disabled.
4. Apply only versioned schema operations.
5. Write a same-directory staging file.
6. Flush and validate the staged document.
7. Create and verify the required backup.
8. Recheck the source version/hash and lease fencing token.
9. Atomically replace the original on the same volume.
10. Record the resulting hash/version and item outcome durably.

Backup failure MUST block the affected write. A backup is not a rollback claim. Restore requires its own manifest, authorization, audit, tests, and operator procedure.

After service restart, a worker MUST reconcile durable item checkpoints with source, staged file, backup, and expected resulting hashes before retrying or declaring failure. It MUST NOT blindly repeat an uncertain replacement.

## 13. Canonical operation behavior

| Operation | Version 1 policy | Later policy / boundary |
| --- | --- | --- |
| Start Scan | Durable async action; collision fails; one active/transitioning scan per roll. | Resume/reuse requires an explicit reconciliation design. |
| Finish Scan | Durable state transition; no mandatory file-content rule in Version 1. | File manifests or expected-file checks require Product definition. |
| QPF settings | Exactly one QPF directly under the canonical roll directory. Zero or multiple QPFs block Preview. | Nested/multi-QPF modes require an explicit deterministic selection policy. |
| Frames paths | Separate grayscale and bitonal base resource references; both may intentionally select the same resource. The server appends the canonical roll folder and previews effective paths. | Alternate shared-root UX may be added without changing server-owned path construction. |
| IDF-to-QPF | Not exposed in Version 1. Proposed later behavior: top-level deterministic discovery, collision fails, schema-owned defaults, staged write. | Must reconcile malformed input and user identity rules first. |
| Organize QPF | Not exposed in Version 1. | Must define normalization, cross-volume behavior, collision policy, rollback language, and item boundaries. |
| Queue create | Not exposed in Version 1. | Must define deterministic sort, naming, duplicate semantics, and overwrite behavior. |
| Queue combine | Not exposed in Version 1. | Must define input ordering, duplicate handling, reindexing, and overwrite behavior. |
| Queue split | Not exposed in Version 1. | Must define part validation, balanced ordering, naming, and collision behavior. |
| Backup cleanup | Not exposed in Version 1 and separately permission-gated. | Requires retention eligibility, archive behavior, restore dependencies, and legal/operations approval. |

Discovery order MUST be ordinal and deterministic after Windows path normalization. Filesystem enumeration order is never authoritative.

## 14. QPF setting schema Version 1

Schema ID: `qpf-settings/1`

The browser submits stable setting IDs and typed values. It never submits an XML attribute name, XPath, element-placement rule, or raw fragment.

Policy:

- Unknown XML elements and attributes are preserved.
- A known setting targets one exact element and attribute cardinality.
- Duplicate target elements or ambiguous placement block the affected file.
- A setting may be added only when its schema entry explicitly permits it.
- `ScanUserID` and `ProcessUserID` are derived from authenticated server policy; they are not operator-editable request values.
- Boolean-like QPF fields are typed as Boolean by the API and normalized to schema-approved `0` or `1` XML values.
- Output directory attributes are owned by the Frames operation, not the general settings form.

Proposed initial settings:

| Stable ID | Exact target | API type / allowed value | Add if absent |
| --- | --- | --- | --- |
| `scan-user-id` | `/Roll/@ScanUserID` | server-derived bounded string | Yes |
| `process-user-id` | `/Roll/@ProcessUserID` | server-derived bounded string | Yes |
| `grayscale-format` | `/Roll/ScannerSettings/@GrayscaleFileFormat` | integer enum `0..5` | No |
| `bitonal-format` | `/Roll/ScannerSettings/@BitonalFileFormat` | integer enum `0..4` | No |
| `grayscale-prefix` | `/Roll/ScannerSettings/@GrayscaleFilePrefix` | bounded safe string | No |
| `bitonal-prefix` | `/Roll/ScannerSettings/@BitonalFilePrefix` | bounded safe string | No |
| `contrast` | `/Roll/ScannerSettings/DetectionSettings/@ProcessContrast` | integer `-255..255` | No |
| `brightness` | `/Roll/ScannerSettings/DetectionSettings/@ProcessBrightness` | integer `-255..255` | No |
| `gamma` | `/Roll/ScannerSettings/DetectionSettings/@ProcessGamma` | integer `-255..255` | No |
| `sharpen` | `/Roll/ScannerSettings/DetectionSettings/@ProcessSharpen` | integer `0..10` | No |
| `deskew-quality` | `/Roll/ScannerSettings/DetectionSettings/@ProcessDeskewQuality` | integer `0..5` | No |
| `auto-crop` | `/Roll/ScannerSettings/DetectionSettings/@ProcessAutoCrop` | Boolean | No |
| `auto-deskew` | `/Roll/ScannerSettings/DetectionSettings/@ProcessAutoDeskew` | Boolean | No |
| `rotate` | `/Roll/ScannerSettings/DetectionSettings/@ProcessRotate` | enum `0`, `90`, `180`, `270` | No |
| `flip` | `/Roll/ScannerSettings/DetectionSettings/@ProcessFlip` | integer enum `0`, `1`, `2` | No |
| `crop-border` | `/Roll/ScannerSettings/DetectionSettings/@ProcessCropBorder` | integer `0..150` | No |
| `crop-threshold` | `/Roll/ScannerSettings/DetectionSettings/@ProcessCropThreshold` | integer `0..128` | No |
| `save-grayscale` | `/Roll/ScannerSettings/DetectionSettings/@ProcessSaveGrayscale` | Boolean | No |
| `save-bitonal` | `/Roll/ScannerSettings/DetectionSettings/@ProcessSaveBitonal` | Boolean | No |

The enum meanings, prefix length/character policy, and whether absent legacy attributes may be safely added MUST be verified against representative production QPFs before this schema is approved. The legacy sample catalog is evidence about existing inputs, not an approved public schema.

## 15. Frames policy

**Proposed default:** Use separate grayscale and bitonal base directory references. The user may choose the same authorized directory for both.

The backend:

- appends the canonical roll folder name for each enabled output mode;
- returns calculated effective display paths in Preview;
- treats existing directories as reusable only after confirming they are directories under the authorized root;
- creates missing directories during Apply, before the QPF replacement;
- fails the affected item without writing the QPF if required directory creation fails;
- never asks the browser to join or normalize paths.

Only one top-level QPF is eligible in Version 1. Nested recursive search and "first file found" behavior are prohibited.

## 16. Domain authorization and audit

Current identity/tracking alone is insufficient authority for Scan and Processing operations. The Version 1 demo authorization model is:

- users are registered and authenticated before they can receive Scan or Processing authority;
- managers define roles and choose their permission sets through HTTP contracts intended for the frontend team;
- role definitions, global role assignments, revocations, and permission changes are durable events;
- roles grant named permissions; registration or authentication alone grants no Scan or Processing permission;
- topic decisions resolve the authenticated actor's current durable roles and permissions rather than trusting client-supplied role or permission claims;
- role definition and assignment changes are audited, including the available actor identity, target user, previous/resulting roles, and timestamp;
- manager-only enforcement for the role-management HTTP endpoints is deferred for the demo and is primarily a frontend concern in Version 1;
- a temporary admin bootstrap endpoint accepts an existing username and a plaintext demo secret in the request body, verifies the fixed hard-coded demo secret, and grants that registered user manager status;
- the bootstrap secret MUST NOT appear in events, logs, metrics, responses, or committed documentation, and the demo endpoint and fixed secret MUST be removed or hardened before production enablement.

The permission catalog includes:

- `scan.discover`
- `scan.start`
- `scan.finish-own`
- `scan.finish-any`
- `scan.abandon`
- `processing.discover`
- `processing.preview.<operation>`
- `processing.apply.<operation>`
- `job.cancel-own`
- `job.cancel-any`
- `history.view-context`
- `history.view-workspace`
- `sensitive-error.view`
- `backup.cleanup`
- any future `backup.restore`

Authorization for Scan and Processing actions is a domain decision made in topics against actor, assigned roles/permissions, client/workspace, roll, operation, and resource. It is not ASP.NET endpoint authorization. The HTTP layer maps durable rejection facts to the appropriate response but does not make the authoritative permission decision. Full informational paths may be returned by the scoped contracts described in Section 7. Restricted diagnostics still require the corresponding permission.

QueryHub subscriptions remain identity-independent by Totem design and MUST NOT be treated as authorization or as evidence that a caller may retrieve or mutate the referenced resource. General HTTP fetch-endpoint authorization and backend enforcement for role-management endpoints may be added later without changing QueryHub. The temporary demo role-management and bootstrap posture is explicitly not production-ready.

Audit records retain at least:

- authenticated principal and effective server-derived operator ID;
- client, row, roll, box, and workspace identifiers;
- action/operation and validated inputs, with sensitive values redacted;
- schema, configuration, discovery, plan, and resource versions;
- acknowledgements and confirmation ID;
- idempotency key and reconciled action/job ID;
- timestamps, status transitions, and actor for Scan/Finish/Abandon/Cancel;
- per-item outcomes, backup outcomes, and resulting versions;
- safe error code/message plus restricted diagnostic correlation ID.

## 17. Error envelope and initial catalog

```ts
interface ProcessingIssue {
  code: string;
  severity: 'info' | 'warning' | 'blocker' | 'error';
  message: string;
  field?: string;
  resourceRefId?: string;
  requiresAcknowledgement?: boolean;
  retryable?: boolean;
  correlationId?: string;
}
```

Initial stable codes:

| Category | Codes |
| --- | --- |
| Context | `ROW_NOT_FOUND`, `ROLL_NOT_FOUND`, `ROLL_MAPPING_INVALID`, `ROW_CONTEXT_INVALID`, `ACTION_INELIGIBLE` |
| Scan | `SCAN_ALREADY_ACTIVE`, `SCAN_NOT_ACTIVE`, `SCAN_TRANSITION_IN_PROGRESS`, `SCAN_OWNERSHIP_MISMATCH`, `SCAN_FOLDER_COLLISION` |
| Authorization | `FORBIDDEN_OPERATION`, `FORBIDDEN_RESOURCE`, `SENSITIVE_VALUE_REDACTED` |
| Validation | `INVALID_FIELD`, `UNSUPPORTED_SETTING`, `SETTING_OUT_OF_RANGE`, `SETTING_COMBINATION_INVALID`, `SCHEMA_VERSION_UNSUPPORTED` |
| Resource/path | `RESOURCE_REF_EXPIRED`, `RESOURCE_VERSION_STALE`, `RESOURCE_LOCKED`, `ROOT_UNAVAILABLE`, `PATH_POLICY_VIOLATION`, `RESOURCE_COLLISION` |
| Logical storage binding | `STORAGE_BINDING_CLIENT_NOT_FOUND`, `STORAGE_BINDING_INVALID_REQUEST`, `STORAGE_BINDING_INVALID_LABEL`, `STORAGE_BINDING_INVALID_CAPABILITIES`, `STORAGE_BINDING_ALREADY_EXISTS`, `STORAGE_BINDING_NOT_FOUND`, `STORAGE_BINDING_REVISION_STALE`, `STORAGE_CONFIGURATION_GENERATION_STALE` |
| Files | `SOURCE_NOT_FOUND`, `SOURCE_AMBIGUOUS`, `SOURCE_MALFORMED`, `BACKUP_FAILED`, `STAGING_FAILED`, `ATOMIC_REPLACE_FAILED` |
| Plan/job | `PLAN_EXPIRED`, `PLAN_VERSION_STALE`, `PLAN_ACK_REQUIRED`, `JOB_NOT_FOUND`, `JOB_RETIRED`, `CANCEL_NOT_ALLOWED` |
| Idempotency | `IDEMPOTENCY_RECONCILED`, `IDEMPOTENCY_KEY_MISMATCH`, `IDEMPOTENCY_RECORD_RETIRED` |
| Service | `STORAGE_TEMPORARILY_UNAVAILABLE`, `SERVICE_TEMPORARILY_UNAVAILABLE`, `WORK_REQUIRES_RECONCILIATION` |

Messages MUST be safe for the actor's scope. Responses MUST NOT include stack traces, credentials, service-account details, unrestricted physical paths, or existence information about unauthorized resources. Restricted diagnostic detail belongs in protected logs keyed by `correlationId`.

## 18. Retention and operational ownership

The following are proposed defaults pending Governance, Security, Legal, and Operations approval:

| Record | Proposed retention |
| --- | --- |
| Scan records and notes | 7 years |
| Audit records | 7 years |
| Discoveries | 30 days |
| Expired plans and effects | 90 days |
| Idempotency records | 30 days after terminal action/job |
| Jobs, transitions, and per-item results | 1 year |
| Hot structured logs | 90 days |
| Exported result artifacts | 30 days |
| QPF backups | 30 days minimum, then policy-driven cleanup only |

No cleanup process may delete records or backups until the final retention schedule, legal hold behavior, restore dependency, and cleanup ownership are approved.

Operations owns:

- storage-root and service-identity configuration;
- ACL reviews and credential rotation;
- share availability and latency monitoring;
- queued/running/failed/partial/cancelled job metrics;
- backup/storage-access failure alerts;
- stuck-job age alerts;
- disk/capacity alerts;
- recovery and reconciliation runbooks;
- retention execution and evidence.

Logs and metrics correlate by scan ID, plan ID, job ID, client ID, row ID, roll ID, and diagnostic correlation ID. The bootstrap secret and credentials are never logged.

## 19. Recovery policy

Jobs and scan actions survive browser closure and service restart. On restart:

1. The worker finds non-terminal durable actions/jobs.
2. It acquires a new fenced lease.
3. It reconciles the last durable item checkpoint against filesystem hashes and artifacts.
4. It resumes only work proven not to have committed.
5. Ambiguous effects become `WORK_REQUIRES_RECONCILIATION`; they are not blindly replayed.
6. The operator runbook defines investigation, safe completion, and terminal-failure recording.

The frontend can retrieve active work by captured row/roll/workspace and reconnect by durable job or scan ID. QueryHub notification loss does not prevent recovery because bounded polling/refetch remains available.

## 20. Delivery sequence

1. **Security and Operations foundation:** production identity, manager-defined roles and global assignments, worker service identity, root configuration, ACLs, redaction, logs, metrics.
2. **Contracts and roll state:** durable scan state/version, operation context, errors, OpenAPI, deterministic fixtures, contract tests.
3. **Scan vertical slice:** discovery, idempotent Start, async folder creation, Finish, Abandon, audit, restart and collision tests.
4. **Shared processing platform:** resource references, Preview plans, Apply, leases, durable jobs, cancellation, paging, history, recovery.
5. **QPF schema and Preview:** representative-file validation, deterministic discovery, typed schema, exact before/after plans.
6. **QPF settings mutation:** backup/stage/validate/atomic replace, per-item outcomes, fault and restart tests.
7. **Frames paths:** separate authorized roots, effective Preview paths, directory creation ordering, QPF integration.
8. **Production qualification:** real service identity, approved roots, multi-instance tests, live file inspection, monitoring, recovery rehearsal, reviewer sign-off.

## 21. Production enablement gates

Start, Finish, Preview, and Apply remain disabled in production until all applicable evidence exists:

- machine-readable contracts and deterministic frontend fixtures;
- Boolean, enum, version, error, and pagination contract tests;
- contract tests for manager-defined roles, global assignments, bootstrap success/failure, and secret non-disclosure;
- authorization tests for Scan/Processing operation permissions, row/workspace scope, and sensitive diagnostic boundaries;
- QueryHub subscription and reconnect tests that preserve Totem's identity-independent ETag behavior and HTTP refetch fallback;
- traversal, reparse point/junction/symlink, UNC, normalization, and root-escape tests;
- idempotency tests covering a lost response after durable acceptance;
- multi-user, multi-tab, multi-instance lease and version tests;
- backup-failure tests proving the affected QPF is unchanged;
- malformed QPF and partial filesystem-failure tests;
- cancellation tests proving completed, failed, and not-attempted outcomes;
- worker-restart tests proving safe resume or explicit reconciliation failure;
- direct inspection of folders, QPFs, staging files, backups, history, and audit;
- live evidence under the real Windows service identity and approved roots;
- monitoring, stuck-job alert, and operator recovery rehearsal;
- Backend, Product, Security, Operations, and QA approval.

Passing fixture, mocked, or development-filesystem tests alone is not production evidence.

## 22. Approval record and open edits

Reviewers should edit this section rather than treating silence as approval.

| Owner | Decision needed | Status / notes |
| --- | --- | --- |
| Product | Editable scan folder names; collision failure; file checks at Finish; Frames separate-root UX; exact QPF setting meanings | Pending |
| Backend | Roll state machine; mapping resource; DTOs; stream/version model; idempotency scope; lease and recovery implementation | Pending |
| Security | Registered-user identity; future role-management hardening; bootstrap removal/replacement; revocation behavior; service identity; registration policy; audit/redaction | Demo policy accepted; production hardening pending |
| Operations | Approved roots and ACLs; service account/gMSA; retention; backups; monitoring; stuck-job and reconciliation runbooks | Pending |
| QA | Contract, security, filesystem fault, concurrency, restart, and target-host evidence plan | Pending |

Any change to operation selection, collision behavior, backup requirements, filesystem item boundaries, or authorization scope requires a new schema/contract version or an explicitly compatible policy revision.
