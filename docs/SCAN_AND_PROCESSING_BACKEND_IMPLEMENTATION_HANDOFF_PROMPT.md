# Scan and Processing Backend Implementation Handoff Prompt

Last updated: 2026-08-10

Use this document to resume Miller Scan and Processing backend work in a fresh context at:

```text
C:\Users\ajohnston\Desktop\Refactor\Totem
```

## Required reading order

Read these documents completely before editing code:

1. `docs/SCAN_AND_PROCESSING_BACKEND_IMPLEMENTATION_HANDOFF_PROMPT.md`
2. `docs/SCAN_AND_PROCESSING_BACKEND_POLICY.md` — authoritative behavior and safety policy
3. `docs/SCAN_AND_PROCESSING_BACKEND_IMPLEMENTATION_STATUS.md` — completed implementation and verification evidence
4. `docs/SCAN_AND_PROCESSING_NEXT_STEPS_PLAN.md` — proposed sequence, open decisions, and package boundaries
5. `docs/contracts/scan-processing/v1/README.md` — implemented operation-context contract

`docs/SCAN_AND_PROCESSING_RECOMMENDED_BACKEND_PLAN.md` is supporting architectural background. Where documents differ, the policy wins; where a proposed plan conflicts with current code or status evidence, verify the repository and update the documentation rather than assuming either is current.

## Current checkpoint

Verify this checkpoint at the start of the session; do not treat it as a substitute for `git status`, `git log`, or reading the code.

- Expected branch at handoff preparation: `user-auth`.
- Work package 1 is committed as `c4815cb Retire client-scoped microfilm rows`.
- Work package 2 is committed as `cb03223 Add durable roll operation context`.
- Work package 1 removed legacy client-scoped row routes and compatibility behavior.
- Work package 2 added the read-only durable roll operation context, scan-state projection, JSON Boolean `isScanning`, and business `resourceVersion`.
- Start, Finish, Preview, Apply, role/permission topics, storage-binding code, idempotency, worker execution, recovery, and production enablement are not implemented.
- The policy, status, recommended plan, this handoff, and the next-steps plan were prepared as a separate documentation continuation after work package 2. Verify the actual commit/worktree state before doing more work.
- The recorded broad test baseline is 121 passing and 2 unrelated existing failures; re-run relevant tests rather than presenting this historical result as fresh evidence.

The user previously supplied an EventStoreDB instance for development. Check whether it is already running before starting another instance; do not assume the old process state is still current.

## Resume objective and stop boundary

Continue after completed work package 2. The next implementation target is the smallest coherent portion of work package 3: durable role definitions, assignments, permissions, and topic-owned authorization decisions. Keep it independent of filesystem work.

Then proceed only in the small verified packages listed below. Do not compress role authority, storage configuration, Start acceptance, filesystem mutation, and recovery into one demo change.

Unless the user explicitly expands the scope, stop before:

- any real local or UNC filesystem mutation;
- production mutation enablement;
- QPF or Frames Apply;
- claims of recovery, multi-instance, target-host, or Windows-service qualification.

QueryHub protection is not a work item. Totem intentionally provides identity-independent ETag subscriptions. Do not add subscription authorization or replace QueryHub. General HTTP fetch-endpoint authorization is also deferred.

## Settled domain and infrastructure decisions

Carry these decisions forward without reopening them:

1. Canonical operation identity is `rollId + rowId`.
2. The frontend does not provide names, client IDs, box IDs, workspace IDs, cell values, roles, permissions, or filesystem paths as operation authority.
3. The backend resolves the canonical roll, box, client/workspace, and roll name from `rollId`, then verifies that `rowId` belongs to the roll.
4. For Version 1, a client is the workspace and `workspaceId` is the resolved `clientId`.
5. Each client/workspace has exactly one active logical storage binding. The binding may contain distinct Scan-parent, QPF, grayscale-Frames, and bitonal-Frames capabilities, even when capabilities resolve beneath the same root.
6. The existing Server entity remains a separate identifier and does not select or resolve the storage binding.
7. Developer-admin API commands may create and activate a logical binding ID and configuration generation. Durable domain state stores only that ID and generation; Operations-owned `Quantum.Service` configuration maps them to physical locations.
8. The initial production share root is `\\sbsr-film\film\`.
9. The approved worker identity is `CMGX\appdevsvc`, with access already configured. This is policy input, not live Windows-service evidence.
10. Labels are the primary resource presentation. An authorized operation result or deliberately client/workspace-scoped query may sometimes expose an informational full path so a user can verify work. Returned paths never become valid operation input.
11. Version 1 resource references do not automatically expire. Use-time authorization and binding-generation validation remain mandatory, and explicit revocation, permission/scope changes, or generation changes may still invalidate use.
12. Configured parent choices are sufficient for Version 1. Arbitrary filesystem browsing is deferred.
13. Profiles and visible cells are presentation only. `cells.rollName` and `cells.isScanning` are never authoritative.
14. Regular and custom rows are equally eligible. `origin` is provenance only and does not change identity, permissions, capabilities, or behavior.
15. `isScanning` is durable roll-owned state, projected independently of profiles and cells.
16. Scan and Processing are authorized domain actions. Managers assign roles to registered users; role definitions, assignments, permission changes, and authorization outcomes are events and decisions in topics, not ASP.NET endpoint authorization.
17. Query ETags and QueryHub notifications are invalidation infrastructure, never business concurrency or operation authority.
18. `Quantum.Web` does not resolve or touch production storage. `Quantum.Service` is the only filesystem boundary.

## Role-policy decisions still open

These are the only current product/security decisions that materially affect the first role package:

1. Are Version 1 role definitions fixed by the application, or may managers create roles and choose permissions?
2. Are role assignments global in Version 1, or scoped to a client/workspace?
3. How is the first manager bootstrapped, and which registered user should be used for the demo?
4. Is full-path disclosure a normal workspace result, or must the actor have `sensitive-path.view`?

Recommended demo defaults, if the user approves them, are fixed built-in `manager`, `scan-operator`, and `processing-operator` roles; global Version 1 assignments; a developer-only manual bootstrap command/API that records an audited initial-manager fact; and `sensitive-path.view` for full-path disclosure. Revocation should prevent authorization decisions ordered after the revocation; it should not rewrite already accepted operation history.

The exact cross-topic routing, ordering, replay, and projection design is a backend implementation decision to derive from current Totem patterns. Do not burden the user with it unless repository evidence exposes a real business tradeoff.

## Start-of-session procedure

1. Read any applicable `AGENTS.md` files.
2. Run `git status --short`, record the current branch and HEAD, and preserve all existing user changes and untracked files.
3. Read `global.json` before building. The recorded requirement is .NET SDK `10.0.300`; obey the current file if it changed.
4. Read the required documents above and inspect the completed package code/tests before designing the next slice.
5. Trace current registered-user identity, commands/events/topics/queries, command dispatch, authorization-related code, HTTP mapping, replay behavior, and multi-instance assumptions.
6. Write a short working plan, implement one coherent package, run focused verification, and report what remains proposed.

Do not switch branches, reset the worktree, discard changes, edit another checkout, or mutate the production share without explicit authorization.

## Proposed implementation sequence

### Completed package 1: canonical row boundary

Do not reimplement it. Verify the status document and commit when needed. The supported model contains only roll-scoped regular and custom rows addressed by `rollId + rowId`; there is no name/cell fallback or client-scoped compatibility route.

### Completed package 2: durable operation context

Do not reimplement it. Verify the status document and commit when needed. The current read-only contract owns scan state on the roll, projects a real Boolean `isScanning`, uses a business `resourceVersion` distinct from query ETags, and treats both row origins equally.

### Work package 3: durable role and permission foundation

Implement no operation or filesystem mutation in this package:

- stable registered actor identity from authenticated server context;
- versioned role definitions and permission catalog;
- manager assignment and revocation requests;
- durable role-definition, assignment, revocation, decision, rejection, and audit facts;
- topic-owned decisions that never trust client-authored roles or permissions;
- bootstrap/self-elevation protection consistent with the approved Version 1 decisions;
- focused replay, ordering, assignment-authority, revocation, forged-input, and multi-instance tests.

HTTP may resolve the authenticated registered actor, append a request, and map durable outcomes. It does not become the authoritative permission evaluator.

### Work package 4: logical storage-binding contract

Implement contracts and durable configuration only—no physical path resolution or filesystem mutation:

- exactly one active binding per client/workspace;
- stable binding ID, configuration generation, labels, and capability set;
- developer-admin create/activate API commands and durable facts;
- opaque resource references with no automatic Version 1 expiry;
- configured parent choices, with arbitrary browsing deferred;
- informational full-path response shape that never accepts a returned path as authority;
- mapping/generation invalidation, redaction, wrong-scope, and Server-separation tests;
- no physical root in browser-authoritative input or durable domain identity.

### Work package 5: durable Start acceptance

Only after packages 3 and 4 are focused-test green, implement the durable command side of Start Scan without folder creation:

- topic-owned authorization bound to one request, actor, permission, scope, and authorization revision;
- exact `rollId + rowId`, expected `resourceVersion`, idle state, binding generation, folder-name, and notes validation;
- idempotency key plus semantic-input hash and lost-response reconciliation;
- one active or transitioning scan per roll;
- durable scan ID, `ScanStartAccepted`, roll version change, `starting` state, rejection, and audit facts;
- worker-owned asynchronous boundary for a later package.

Do not claim an atomic transaction between KurrentDB and storage. Stop before folder creation unless the user explicitly authorizes the filesystem package.

## Architectural boundary

```text
Browser
   |
   | HTTP operations + identity-independent QueryHub invalidations
   v
Quantum.Web
   | resolve authenticated actor, validate transport, append commands, serve projections
   v
KurrentDB / durable topics
   | roles, permissions, decisions, rolls, bindings, scans, versions, idempotency, audit
   v
Quantum.Service
   | later: resolve configured bindings and perform filesystem work
   v
Approved Windows storage roots
```

## Verification expectations

For every work package:

1. Run focused tests first and record exact pass/fail counts.
2. Prove both regular and custom row behavior where operation context is involved.
3. Prove wrong `rollId + rowId` combinations fail without name matching.
4. Prove roles, permissions, client/workspace IDs, Server IDs, and paths supplied by a client never become authority.
5. Prove replay and ordering behavior for new durable topic decisions.
6. Run the narrowest relevant build and broader tests practical without hiding unrelated baseline failures.
7. Run `git diff --check`.

Do not silently build with a different SDK. Do not present fixture, mock-filesystem, or development-machine results as production evidence.

## Documentation and end-of-session report

Keep the policy, status, next-steps plan, machine-readable contracts, and deterministic fixtures synchronized only with decisions or implementation actually made. Preserve the distinction between completed evidence, approved policy, recommended defaults, and future work.

Report:

1. the implemented package and dependency boundary;
2. files, routes, events, contracts, and projections changed;
3. focused and broader verification with exact results;
4. unrelated failures separately identified;
5. the next smallest package;
6. remaining human decisions and production gates;
7. explicit confirmation that no filesystem or production mutation occurred.

Do not describe the Scan/Processing backend as complete or production-ready until all applicable gates have live evidence.
