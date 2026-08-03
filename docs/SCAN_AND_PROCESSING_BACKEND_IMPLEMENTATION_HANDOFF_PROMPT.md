# Scan and Processing Backend Implementation Handoff Prompt

You are picking up backend implementation work for Miller Scan and Processing in:

```text
C:\Users\ajohnston\Desktop\Refactor\Totem
```

The authoritative design guidance is:

- `docs/SCAN_AND_PROCESSING_BACKEND_POLICY.md`

Read that document completely before editing code. Use `docs/SCAN_AND_PROCESSING_RECOMMENDED_BACKEND_PLAN.md` only as supporting background. Where the two differ, the policy wins.

## Objective

Begin implementation with the smallest coherent backend foundation for Scan and Processing. Establish the canonical roll-scoped model and durable operation context before implementing filesystem mutation.

Do not attempt the entire policy in one change. Work in small, independently verified packages and report precisely what is implemented versus still proposed.

## Corrected assumptions that must carry forward

1. Canonical operation identity is `rollId + rowId`.
2. The frontend does not send `rollName`, `boxName`, or cell values to resolve or authorize a Scan/Process operation.
3. The backend resolves the canonical roll, box, client, workspace, and roll name from `rollId`, then verifies that `rowId` belongs to that roll.
4. Profiles and visible cells are presentation only. `cells.rollName` is never an operation lookup key.
5. Regular and custom rows are equally eligible. `origin` records provenance only:
   - regular rows came from WASP data;
   - custom rows were manually entered when the data was unavailable from WASP.
6. Origin must not change operation identity, authorization, capabilities, or behavior.
7. The supported table model consists only of roll-scoped regular and custom rows. Legacy client-scoped rows, routes, routing indexes, and compatibility behavior should be removed, with any remaining data converted before Scan and Process are enabled.
8. `isScanning` is durable roll-owned operational state. It is not a cell, profile field, UI-local flag, or value inferred from folder contents.

## Start-of-session procedure

1. Read any applicable `AGENTS.md` files.
2. Run `git status --short`, record the current branch and HEAD, and preserve all existing user changes and untracked documents.
3. Read `global.json` before building. This checkout requires .NET SDK `10.0.300` unless the file has changed.
4. Do a shallow inventory of the solution, relevant projects, documents, and tests before tracing implementation details.
5. Inspect the current roll/table model, commands/events/topics/queries, HTTP routes, command execution path, QueryHub behavior, authentication/authorization state, and worker hosting boundary.
6. Write a short working plan with small verifiable packages, then begin implementation unless current evidence reveals a genuine blocker.

Do not switch branches, reset the worktree, discard changes, or edit the related frontend checkout without explicit authorization.

At the time of this handoff, the policy and recommended-plan documents may be untracked. Preserve them. Re-check current state rather than assuming the recorded branch or commit is still current.

## Initial implementation sequence

### Work package 1: canonical row boundary

Make the roll-scoped table model the only supported backend row model:

- retain regular and custom row routes addressed by `rollId + rowId`;
- give regular and custom rows the same ownership and operation eligibility rules;
- remove legacy client-scoped row endpoints and compatibility dispatch;
- remove legacy row-routing indexes, adapters, tests, and documentation that exist only for coexistence;
- do not derive a missing `rollId` from `boxName`, `rollName`, or any other cells;
- identify any external frontend dependency that still calls a removed route and record it as a coordinated follow-up rather than editing another checkout silently.

Before deleting compatibility code, trace its callers and tests so removal is intentional and complete. Do not introduce a migration bridge based on cell matching.

### Work package 2: durable operation context

Add the backend foundation needed to open Scan or Process for either row origin:

- durable roll-owned scan state;
- a real JSON Boolean `isScanning` projected independently of cells and profiles;
- an explicit transitional scan state if needed to distinguish `idle`, `starting`, `active`, and `finishing`;
- an opaque business `resourceVersion` for command concurrency, separate from query ETags;
- canonical row/roll context resolved from `rollId + rowId`;
- equal Scan/Process eligibility for regular and custom rows;
- structured invalid-row, invalid-roll, mapping, stale-version, and ineligible-action errors;
- a read-only discovery/context HTTP contract and deterministic fixtures or contract examples.

Prefer extending the current durable roll model cleanly. The model was designed to carry durable roll information such as `isScanning`, but existing shapes are guidance rather than a restriction if a clearer design emerges.

Do not use `cells.isScanning`. Do not read `cells.rollName` to establish identity. Do not use QueryHub ETags as the mutation concurrency version.

### Work package 3: first Scan mutation slice

Proceed only after the first two packages are focused-test green and the security boundary is explicit.

Implement the durable command side of Start Scan before filesystem work:

- authenticated actor and operation authorization;
- expected roll resource version;
- idempotency key and reconciliation behavior;
- one active or transitioning scan per roll;
- durable scan ID, accepted state, roll version change, and audit facts;
- a worker-owned asynchronous boundary for later folder creation.

Do not claim an atomic transaction between KurrentDB and local/SMB storage. Do not expose production-enabled filesystem mutation in this package.

Stop after a coherent, verified slice if completing the next package would mix unfinished security, storage, or recovery concerns into otherwise proven work.

## Architectural boundaries

```text
Browser
   |
   | authenticated HTTP and protected invalidation notifications
   v
Quantum.Web
   | authorize, validate, append commands, serve projections/contracts
   v
Durable application state
   | rolls, scans, versions, idempotency, jobs, audit
   v
Quantum.Service
   | resolve opaque resources and perform filesystem work
   v
Approved Windows storage roots
```

- `Quantum.Web` must not touch production Formatic storage.
- `Quantum.Service` is the filesystem worker boundary.
- A durable append and completed processing are different states.
- QueryHub is notification infrastructure; HTTP refetch remains the state-retrieval fallback.
- The browser never supplies an authoritative local or UNC path.
- Profile membership is not a schema, identity rule, or authorization boundary.
- Current tracking identity must not be mistaken for production authorization.

## Production safety boundary

Keep Start, Finish, Preview, and Apply disabled in production until the applicable policy gates exist. In particular, do not claim readiness without:

- enforced actor/resource authorization;
- protected QueryHub access;
- approved worker service identity and storage roots;
- idempotency and multi-instance concurrency evidence;
- path-containment and reparse-point tests;
- backup-failure protection for QPF writes;
- durable job/restart reconciliation;
- live evidence under the real Windows service identity.

Contract and domain work may proceed behind an explicit disabled feature gate or without exposing mutation routes.

## Verification expectations

For every work package:

1. Run focused tests first.
2. Add tests for regular and custom rows wherever operation context or eligibility is exercised.
3. Prove that cell/profile visibility does not affect identity or `isScanning`.
4. Prove that wrong `rollId + rowId` combinations fail without falling back to name matching.
5. Prove JSON Boolean and enum/version contracts at the HTTP boundary.
6. Run the narrowest relevant build and broader tests that are practical without hiding unrelated existing failures.
7. Run `git diff --check`.

If the required SDK is unavailable from the normal `dotnet` on `PATH`, locate the installed `10.0.300` SDK using the established repository/deployment guidance. Do not silently build with a different SDK.

## Documentation and contract handling

- Keep `docs/SCAN_AND_PROCESSING_BACKEND_POLICY.md` synchronized only when implementation uncovers a real contract decision or discrepancy.
- Do not rewrite policy sections merely to describe unfinished code.
- Add or update OpenAPI/machine-readable contracts and deterministic fixtures with the implementation slice.
- Record intentional removals of legacy routes and any external client coordination required.
- Preserve the difference between completed implementation evidence and future production-readiness work.

## End-of-session report

Report:

1. the implemented work package and why it was the correct dependency boundary;
2. files and contracts changed;
3. legacy paths removed and any remaining references;
4. focused and broader verification with exact pass/fail counts;
5. any current failures proven unrelated;
6. the next smallest work package;
7. remaining security, filesystem, recovery, and live-environment gates.

Do not describe the overall Scan/Processing backend as complete merely because the first domain or HTTP slice is implemented.
