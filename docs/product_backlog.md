# Product Backlog

Last updated: 2026-08-03

Source inputs:
- `C:\Users\ajohnston\.copilot\session-state\f0fd027a-2db0-4e34-bfa0-ffa121ee6bc8\plan.md`
- `docs/vision.md`
- `docs/design.md`
- `docs/qa_plan.md`
- `docs/governance_traceability.md`
- `backend-integration-guide.md`
- `docs/execution_log.md`
- User direction on 2026-06-30: proceed directly to the post-cookie phase focused on roll-scoped resources and targeted audit metadata.

Superseding direction on 2026-08-03: the Scan/Processing policy makes `rollId + rowId` the only supported row identity. The temporary client-scoped coexistence requirements below are retained as historical Product context, not current acceptance criteria. Client-scoped row routes, projections, routing indexes, seeds, and fallback dispatch are removed; any remaining deployment data requires explicit conversion before Scan or Process enablement.

## Product stance and MVP

The completed baseline is:
- Phase 1 backend session tracking via `GET /api/session`.
- Backend-owned cookie identity MVP for register/login/logout/session tracking.

That identity work is now **completed enabling infrastructure** for actor tracking. Do not reopen it in this phase except as separate release or production-readiness hardening work.

The completed table foundation is **P3: roll-scoped resources and targeted audit**. The current backend package retires its temporary compatibility layer so that:
1. Every regular or custom row is durably owned by one roll and addressed by `rollId + rowId`.
2. No runtime route, projection, routing index, or cell/name lookup derives missing roll identity.
3. Targeted per-cell audit facts continue to use backend-resolved tracking identity.

### Scope guardrails

- In scope: roll/row resource boundaries, profile-owned presentation definitions, custom-row parity, targeted cell-level audit facts, row-query audit metadata, roll-scoped QueryHub invalidation, explicit deployment-data conversion, and backend/frontend contract sequencing. [Vision → Product: V4]
- Use backend-resolved session identity (`/api/session` plus backend cookie/session infrastructure) as the actor source; do not trust client actor fields. [Vision → Product: V1, V2, V3]
- Missing or unmapped identity remains a tracking state unless Product later explicitly scopes write blocking. [Vision → Product: V1, V2]
- Client-scoped row compatibility is retired. Coordinate frontend consumers before deploying the breaking contract.
- Changed/editable cells first; no broad historical backfill is required for untouched cells in MVP.
- Frontend source implementation remains outside this repo; this backlog defines backend contract and migration expectations only.
- Keep authorization/permission gates, roles, QueryHub auth, full admin auth account management, and unrelated auth-store hardening out of this phase unless separately scoped.

### Vision statements

- V1: Keep `/api/session` and backend-owned cookie identity as the canonical server-owned actor source for tracking. [Vision → Product: V1, V2, V3, V6]
- V2: Replace client-wide Miller/Formatic row access with roll-scoped resources at the product/API contract level. [Vision → Product: V4]
- V3: Introduce targeted audit facts at the cell level, starting with changed/editable cells only. [Vision → Product: V4]
- V4: Coordinate frontend adoption of canonical roll reads, writes, and invalidation; do not preserve ambiguous client-scoped authority. [Vision → Product: V3, V4]
- V5: Keep tracking separate from authorization unless Product explicitly scopes authorization later. [Vision → Product: V1, V2, V6]
- V6: Keep repo hygiene and cross-agent traceability visible as scope changes. [Vision → Product: V5]

## Current lifecycle status and readiness summary

- **P1 backend session tracking:** Complete baseline.
- **P2 backend-owned cookie identity MVP:** Complete enabling infrastructure for actor tracking.
- **P2 hardening:** Separate release/production-readiness track only; not a P3 blocker by default.
- **P3 roll-scoped resources/audit phase:** Canonical row model implemented; current route-removal verification and external frontend coordination remain.

Loop back to Product before implementation if stakeholders request:
- authorization or permission gates,
- roles or claims management,
- QueryHub auth,
- full admin auth account management,
- broad frontend rewrite ownership in this repo,
- blocking writes for unmapped or unidentified users,
- or unrelated production auth-store hardening as part of this same phase.

## Product decisions for P3

- **PD-P3-001:** Treat backend-owned cookie/session identity as completed enabling infrastructure; do not reopen it except as separate hardening or release work.
- **PD-P3-002:** Default row resource addressing is `rollId + rowId`. Design may simplify to a globally unique `rowId` only if the backend guarantees global row uniqueness and documents compatibility impacts; current frontend preference remains `rollId + rowId`. [Product → Design]
- **PD-P3-003 (superseded):** Client-scoped endpoints and buckets are removed from the backend. Deployment sequencing must coordinate consumers and explicitly convert data without a runtime lookup bridge. [Product → Design]
- **PD-P3-004:** Custom rows must become roll-scoped resources or roll-scoped siblings; they may not remain only client-wide orphan buckets. [Product → Design]
- **PD-P3-005:** Rows retain arbitrary trimmed, non-empty field IDs and scalar/null values. Profiles independently own presentation membership, order, width, mappings, types, and dropdown options; they do not control row writes. [Product → Design]
- **PD-P3-006:** Audit starts with targeted per-cell facts and query metadata for changed/editable cells first, not full-row or all-cell history backfill. [Product → Design]
- **PD-P3-007:** Audit actor metadata comes from backend cookie/session tracking resolution; missing or unmapped identity is recorded as a tracking state unless Product later scopes command blocking. [Product → Design] [Product → QA]
- **PD-P3-008 (superseded):** QueryHub invalidation is roll-scoped only; HTTP remains the retrieval fallback. [Product → Design]
- **PD-P3-009:** Out of scope remains authorization, roles, QueryHub auth, full admin auth account management, broad frontend implementation, and unrelated auth-store hardening unless separately scoped.

## Product Epics, Features, and User Stories

### Epic FND: Repository Scaffolding and Hygiene

Purpose: Keep SDLC artifacts current and traceable as scope moves from identity delivery to resource/audit migration. [Vision → Product: V6]

#### Feature FND-1: SDLC documentation baseline

Priority: Should-have for every phase.

##### Story FND-S1: Maintain repository scaffolding and docs skeleton

Acceptance Criteria:
- Language-appropriate `.gitignore` is present.
- `src/`, `tests/`, and `docs/` folders exist.
- `/docs/product_backlog.md` exists and captures current scope, priorities, dependencies, blockers, and acceptance criteria. [Product → QA]
- `/docs/execution_log.md` exists before implementation evidence is requested. [Product → QA]
- README or equivalent docs index links key `/docs/*` artifacts before release signoff. [Product → QA]

### Epic P1: Backend Session Tracking and Server-Observed Actor Context Baseline

Purpose: Preserve the completed `/api/session` tracking contract as a non-breaking foundation for later audit work. [Vision → Product: V1, V5]

Status: Complete baseline. Maintain compatibility.

#### Feature P1-1: Same-origin session tracking endpoint

Priority: Completed baseline; maintain compatibility.

##### Story P1-S1: Return the current tracking session

Acceptance Criteria:
- `GET /api/session` remains available from the same origin as Quantum Web.
- Normal tracking states remain `identified`, `unmapped`, and `unidentified`, returned as `200 OK` rather than `401/403`. [Product → QA]
- Response fields remain backward compatible: `status`, `displayLabel`, `windowsAccount`, `userPrincipalName`, `processUserId`, and `trackingSource`. [Product → QA]
- Missing or unmapped identity remains a tracking state, not an authorization failure. [Product → Design]

#### Feature P1-2: Request identity resolution and mapping

Priority: Completed baseline; extend only where P3 depends on actor tracking.

##### Story P1-S2: Resolve server-observed identity from the request

Acceptance Criteria:
- Identity resolution remains host/web infrastructure, not a domain permission rule. [Product → Design]
- The backend continues to normalize and expose observed account fields where available.
- Mapping affects tracking metadata only and never permits or denies access.
- If no principal or no mapping exists, the endpoint still returns explicit tracking states. [Product → QA]

#### Feature P1-3: Server-side actor stamping guardrail for writes

Priority: Completed baseline guardrail; maintain for P3.

##### Story P1-S3: Do not trust client-chosen tracking identity

Acceptance Criteria:
- Current and future tracking-sensitive writes must not trust client `ProcessUserID`, `processUserId`, `userId`, or delegated actor fields. [Product → Design]
- Writes are not rejected solely because tracking identity is `unmapped` or `unidentified` unless Product later re-scopes workflow rules. [Product → QA]
- If durable actor metadata is introduced, it must come from server-resolved session identity. [Product → Design] [Product → QA]

### Epic P2: Backend-Owned ASP.NET Core Cookie Identity for Tracking

Purpose: Preserve the completed backend-owned cookie/session identity MVP as the actor-source infrastructure for P3. [Vision → Product: V1, V5]

Status: Complete enabling infrastructure. Do not reopen inside P3 scope.

#### Feature P2-1: Backend account registration

Priority: Completed enabling infrastructure.

##### Story P2-S1: Register a backend-owned tracking account

Acceptance Criteria:
- A backend-owned registration endpoint exists and does not accept client `processUserId`. [Product → Design]
- Successful registration establishes server-owned tracking identity according to the documented MVP policy.
- Registration does not add roles, permissions, or delegated actor selection. [Product → QA]

#### Feature P2-2: Cookie login

Priority: Completed enabling infrastructure.

##### Story P2-S2: Log in with backend credentials and receive an auth cookie

Acceptance Criteria:
- A backend login endpoint exists and issues backend-owned cookie identity on successful credential validation. [Product → QA]
- Login does not trust client actor input.
- Login does not grant permissions or change existing route access semantics by default. [Product → Design]

#### Feature P2-3: Logout and cookie clearing

Priority: Completed enabling infrastructure.

##### Story P2-S3: Log out and clear backend identity

Acceptance Criteria:
- A backend logout endpoint exists and clears the backend auth cookie/session.
- Logout is predictable and safe to call when already logged out. [Product → QA]
- Post-logout identity flows back through `/api/session` rather than frontend-managed actor state.

#### Feature P2-4: `/api/session` integration with cookie identity

Priority: Completed enabling infrastructure.

##### Story P2-S4: Report cookie-authenticated users through the existing session contract

Acceptance Criteria:
- `GET /api/session` remains the canonical tracking endpoint.
- When a valid backend auth cookie is present, cookie/session identity is the preferred tracking source. [Product → Design]
- `identified`, `unmapped`, and `unidentified` semantics remain backward compatible. [Product → QA]

#### Feature P2-5: Cookie security and same-origin safeguards

Priority: Completed MVP baseline; harden separately for broader release.

##### Story P2-S5: Configure authentication cookies safely for this product context

Acceptance Criteria:
- The MVP uses backend-owned cookie and CSRF/session safeguards documented for the current product context. [Product → QA]
- Auth cookies are not read by frontend code for actor selection.
- Any broader hardening beyond the delivered MVP is tracked separately from P3 scope.

#### Feature P2-6: Frontend coordination and handoff

Priority: Complete for backend contract/handoff in this repo; frontend implementation remains external.

##### Story P2-S6: Coordinate frontend use of backend-owned identity

Acceptance Criteria:
- Frontend-facing docs identify register, login, logout, and session endpoint usage for backend-owned identity. [Product → Design]
- Frontend consumers are expected to use `/api/session` rather than manual/delegated actor selection.
- This repo does not own the broader frontend implementation.

#### Feature P2-7: Validation through existing build and test surfaces

Priority: Completed MVP validation baseline; extend separately when hardening is scoped.

##### Story P2-S7: Validate cookie identity without expanding into authorization testing

Acceptance Criteria:
- Build/test/manual evidence exists for the delivered backend cookie identity MVP. [Product → QA]
- Existing application/data routes were not converted into permission-gated routes by default. [Product → QA]
- Any future expansion into broader endpoint automation or production auth hardening is treated as separate work.

### Epic P2R: Identity Hardening and Release Readiness

Purpose: Track non-blocking follow-on work for broader auth release readiness without letting it absorb P3 scope. [Vision → Product: V5]

Status: Future release work only.

#### Feature P2R-1: Production auth hardening

Priority: Should-have release work; not a P3 blocker by default.

##### Story P2R-S1: Harden the completed cookie identity MVP only when separately approved

Acceptance Criteria:
- Any future work on durable auth store choice, lockout/rate limiting, expanded endpoint automation, admin account management, or production auth hardening is scoped and approved separately.
- P3 may depend on the existing actor/session contract, but does not inherit unrelated auth hardening scope unless a specific dependency is approved.

### Epic P3: Roll-Scoped Resources and Targeted Audit Migration

Purpose: Make the next Product phase explicit: move away from client-wide Miller/Formatic tables toward roll-scoped row resources and targeted audit metadata without breaking current frontend workflows. [Vision → Product: V2, V3, V4, V5]

MVP outcome:
- Backend contracts expose only roll-scoped row resources with sparse durable cells; profiles supply independent presentation definitions.
- Regular and custom rows have identical ownership and capability rules.
- Targeted per-cell audit metadata appears first for changed/editable cells using backend-resolved tracking identity.

#### Feature P3-1: Roll-scoped row resource model

Priority: Must-have next phase.

##### Story P3-S1: Define roll-scoped row addressing and hierarchy

As a frontend data consumer, I need row resources addressed at the roll level so the table model matches the real domain boundaries instead of client-wide buckets.

Acceptance Criteria:
- Product contract defines box → roll → row hierarchy as the primary navigation path; roll resources expose row links or IDs rather than requiring client-wide row lists.
- Default row addressing is `rollId + rowId`; a globally unique `rowId` is acceptable only if the backend guarantees global uniqueness and the frontend/query impact is documented. Current frontend preference is `rollId + rowId`. [Product → Design]
- Row resources expose stable `rowId`, `rollId`, and row kind/context needed to query or mutate one row.
- Read and mutation contract planning assumes roll-scoped row resources rather than `rows/{clientId}` as the long-term primary surface. [Product → Design]
- Client-scoped row endpoints are absent from the supported contract.

#### Feature P3-2: Retire client-scoped endpoints and convert remaining data

Priority: Must-have next phase.

##### Story P3-S2: Migrate without breaking current frontend workflows

As the product team, we need the backend to reject ambiguous client-only identity and deployment owners to convert any retained data explicitly.

Acceptance Criteria:
- Client-scoped regular/custom row endpoints, projections, routing indexes, seeds, and fallback dispatch are removed; deleted column endpoints remain absent.
- A conversion manifest assigns each retained row to one durable roll and rejects missing or ambiguous assignments. [Product → Design]
- Conversion never matches `boxName`, `rollName`, or another cell to establish identity.
- Frontend owners receive a breaking-contract follow-up listing active client-scoped call sites; this backend repository does not edit that checkout silently.
- Scan and Process remain disabled until required data conversion and consumer coordination are complete.

#### Feature P3-3: Custom-row and profile-presentation relationships

Priority: Must-have next phase.

##### Story P3-S3: Place custom rows and profile presentation into the roll-scoped model

As a frontend table consumer, I need custom rows to relate cleanly to roll-scoped row resources while profiles independently define presentation so cells remain interpretable after migration.

Acceptance Criteria:
- Product defines custom rows as roll-scoped resources or roll-scoped siblings; they must be attributable to a roll and cannot remain client-wide only. [Product → Design]
- Design may choose a unified roll-row resource with `rowKind` (`regular` or `custom`) or separate roll-scoped custom-row resources, but the relationship to the roll is explicit. [Product → Design]
- Cells remain sparse and keyed by arbitrary trimmed, non-empty field IDs; object/array values are rejected while scalar/null values are durable.
- Frontends join row reads to the selected profile for labels, mappings, order, width, types, and dropdown options. [Product → Design]
- The deleted `/api/microfilm/columns/{clientId}` and roll `/columns` endpoints are not migration compatibility surfaces.

#### Feature P3-4: Targeted per-cell audit facts

Priority: Must-have next phase.

##### Story P3-S4: Persist targeted cell changes with backend-resolved actor metadata

As a user reviewing table history, I need each tracked cell change to carry who changed it and when, using backend-resolved identity rather than client-submitted actor fields.

Acceptance Criteria:
- The target command/fact/query model is one roll, one row, one column change at a time for tracked updates. [Product → Design]
- Durable audit metadata uses backend-resolved session identity as the actor source, including backend cookie identity when present; the client does not choose `processUserId` or another actor field. [Product → Design] [Product → QA]
- If identity is missing or unmapped, the result is recorded as a tracking state rather than rejected by default. [Product → Design] [Product → QA]
- MVP audit coverage is limited to changed/editable cells first; untouched cells may legitimately return no audit metadata or an explicit not-tracked-yet outcome. [Product → Design]
- Broad historical backfill and full-row event migration are out of MVP unless separately scoped.
- Domain, topic, and query test coverage is required when durable audit facts are introduced. [Product → QA]

#### Feature P3-5: Audit query projection and QueryHub invalidation semantics

Priority: Must-have next phase.

##### Story P3-S5: Expose targeted audit metadata and define invalidation behavior

As a frontend table, I need predictable row-query metadata and invalidation semantics so the UI can render audit facts without losing current refresh behavior.

Acceptance Criteria:
- Row query responses can expose per-cell metadata for tracked cells such as `value`, `lastChangedAt`, and `lastChangedBy` or an equivalent actor shape. [Product → Design]
- Query responses clearly distinguish not-tracked-yet from known `unidentified` or `unmapped` actor states. [Product → Design] [Product → QA]
- QueryHub bucket and invalidation semantics are defined at roll scope before execution depends on them. [Product → Design]
- A roll-scoped cell change invalidates only the affected roll query instances. [Product → Design]
- Compatibility expectations are documented for active tables spanning many rolls, including refresh and load behavior for the first migrated frontend consumers.

## Priority Matrix

| Priority | Items | Rationale |
| --- | --- | --- |
| Completed baseline / maintain | FND-S1; P1-S1; P1-S2; P1-S3; P2-S1 through P2-S7 | Preserves repo hygiene, canonical tracking contract, and completed backend-owned actor resolution. |
| Current table foundation | P3-S1; P3-S2; P3-S3; P3-S4; P3-S5 | Canonical roll resources, explicit conversion boundary, custom-row/profile presentation, targeted per-cell audit, and roll-scoped QueryHub semantics. |
| Should-have parallel release work | P2R-S1 | Keeps auth hardening separate so it does not quietly absorb the resource/audit phase. |
| Explicitly out of scope for P3 | Authorization/permission gates, roles/claims management, QueryHub auth unless separately scoped, full admin auth account management, broad frontend implementation, command blocking for unmapped or unidentified users, unrelated production auth-store hardening, full historical audit backfill, unrelated frontend redesign | Prevents scope creep and preserves a thin MVP around resource migration and targeted audit facts. |

## Dependencies, Blockers, and Open Decisions

### Dependencies

- Completed P1 and P2 session plus backend-cookie actor resolution baseline. [Product → Design]
- Canonical row endpoints and removed client-scoped contracts are documented in `backend-integration-guide.md` and `docs/frontend-microfilm-optimistic-fields-migration.md`. [Product → Design]
- Design inventory of current QueryHub bucket and invalidation behavior before changing resource scope. [Product → Design]
- Design confirmation of whether current row IDs are globally unique across rolls or only within a roll. [Product → Design]
- Design clarification of how custom rows map to rolls in the current domain model. [Product → Design]
- QA coverage for roll-scoped queries/mutations, regular/custom parity, wrong-roll isolation, route absence, audit metadata, and invalidation semantics. [Product → QA]
- Frontend consumer sequencing and handoff outside this repo for eventual adoption of the new resources.

### Blockers

- No Product blocker remains to begin the P3 Product/Design pass.
- Deployment must not consume the breaking contract until frontend coordination and any explicit data conversion are complete.
- Product loopback is required if stakeholders want authorization behavior, route gating, or blocking of unmapped or unidentified users inside this phase.
- Product or Execution loopback is required if P3 proves to depend on auth hardening beyond the completed cookie or session contract.

### Open decisions to close in Design

- Can the backend guarantee globally unique `rowId` values, or should `rollId + rowId` remain the public address permanently?
- Will custom rows share the same roll-row resource collection with a `rowKind` discriminator, or use a sibling roll-scoped custom-row resource?
- How do selected profiles supply presentation while preserving omitted durable fields in row caches and saves?
- Which frontend release removes every `/rows/{clientId}` and `/custom-rows/{clientId}` read/write/subscription dependency?
- Which deployment environments contain client-only row events that need an explicit conversion manifest?
- How should audit projections represent unidentified or unmapped actor states in a UI-safe way?

## Acceptance Criteria Summary for P3

P3 can be considered Product-complete when:
- Roll-scoped row resources and addressing are defined, including the row uniqueness decision.
- Client-wide row and custom-row endpoints are removed; consumer coordination and any data conversion are explicit deployment gates.
- Custom rows and profile presentation have an explicit relationship to roll-scoped resources without making profiles schemas.
- Targeted per-cell audit facts use backend-resolved actor tracking and changed/editable cells first.
- Query responses and QueryHub invalidation semantics are roll-scoped only.
- Missing or unmapped identity remains a tracking state unless separately re-scoped.
- Out-of-scope auth/authorization and broad frontend work have not crept into the phase.

## Change Log

| Date | Change |
| --- | --- |
| 2026-08-03 | Superseded the temporary coexistence requirement: canonical `rollId + rowId` is the only row identity, client-scoped runtime compatibility is removed, and remaining data requires explicit conversion before Scan/Process enablement. |
| 2026-06-30 | Shifted the active backlog from completed cookie-identity delivery to the next Product phase: roll-scoped resources and targeted audit facts. Preserved P1 and P2 as completed or enabling infrastructure, separated auth hardening as release work, and expanded P3 scope, guardrails, decisions, dependencies, and acceptance criteria for migration away from client-wide Miller/Formatic tables. |
| 2026-06-30 | Updated backlog for stakeholder direction: backend-owned ASP.NET Core cookie login/register became the next identity goal. Preserved completed Phase 1 passive session tracking as the baseline, added P2 stories for register/login/logout/session integration and security, and moved roll-row audit or resource migration to future P3 scope. |
| 2026-06-30 | Created product backlog from session-state plan. Reframed original Windows auth handoff from authorization semantics to tracking-only session states. Split immediate backend session or identity work from future roll-row audit or resource migration. |
