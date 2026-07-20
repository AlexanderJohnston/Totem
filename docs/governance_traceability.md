# Governance Traceability: Backend Session Tracking, Cookie Identity, and Roll-Scoped Tables

Last updated: 2026-06-30 13:57 EDT
Governance owner: Governance Agent
Repository: `AlexanderJohnston/Totem`
Lifecycle: Intake -> Product -> Design -> Execution -> QA -> Governance
Work classification: New initiative / feature; backend identity plus roll-scoped Microfilm table implementation

## Gate Decision Summary

**Final Governance gate status: CONDITIONAL PASS for the Phase 1 feature/stage gate.**

Governance re-reviewed the loopback remediation and finds the prior blocking conditions closed for the Phase 1 backend session tracking slice. The feature may proceed from Governance for the scoped feature gate with the waivers and conditions below. This is not a blanket no-waiver production release signoff.

Closed blockers from prior Governance review:

1. **HTTP/API validation gap closed.** Execution recorded manual HTTP validation for `GET http://127.0.0.1:5057/api/session`: `StatusCode=200`, `ContentType=application/json; charset=utf-8`, `CacheControl=no-store`, and camelCase default `unidentified` body: `{"status":"unidentified","displayLabel":"Unidentified user","windowsAccount":null,"userPrincipalName":null,"processUserId":null,"trackingSource":"none"}`. QA accepted this evidence for Phase 1 route/header/serialization coverage.
2. **Vision artifact gap closed.** `/docs/vision.md` exists with problem statement, goals, stakeholders, scope, success criteria, assumptions, risks, and trace links.
3. **README/docs index gap closed.** `readme.md` now includes repository quick start/build/test/start guidance and links to Vision, Product backlog, Design, Execution log, QA plan, Governance traceability, and backend integration guide.

Feature-scope waiver and accepted scope conditions:

- **W-GOV-001 - Unrelated full-suite failure remains waived for this Phase 1 feature gate only.** `dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release` is still not proven green because of the unrelated `ClientProfileRegistryTests.MatchesCorrectProfile` NARA expectation mismatch. Focused resolver tests passed 6/6, and the unrelated failure does not block this feature gate. Any no-waiver release policy must supersede this waiver with an Execution fix and QA rerun.
- **A-GOV-001 - Real Windows identity capture remains out of Phase 1 unless re-scoped.** The endpoint contract allows `unidentified` as a valid tracking state. If stakeholders require real Windows/Negotiate capture during Phase 1, loop back to Research and Execution before adding authentication middleware.
- **Release packaging condition.** Versioned release notes or CHANGELOG links were not assessed in this re-gate. Product/Execution must compile release notes before any broader production release gate that requires them.
- **Non-blocking documentation freshness note.** `docs/qa_plan.md` still contains an attention note saying the README Vision link was missing, but Governance directly verified `readme.md` now includes `[Vision](docs/vision.md)`. QA/Documentation should reconcile that stale note at the next QA document touch; it does not block this gate because the required evidence exists.

## Backend-Owned ASP.NET Core Cookie Login/Register Governance Status

**Gate status: IMPLEMENTATION PASS for the focused tracking-identity MVP, with existing full-suite waiver still applying to broader release policy.**

Governance observes implementation evidence for backend-owned register/login/logout, cookie-backed `/api/session` identity preference, secure cookie defaults, CSRF/CORS strategy, and tracking-not-authorization guardrails.

P2 governance guardrails:
- Cookie login establishes the backend-recognized browser identity for tracking; it does not add permission checks, QueryHub authorization, role gates, command blocking, or access-denied UI unless Product separately scopes authorization.
- No manual/delegated `ProcessUserID` override may be reintroduced. Registration/login must assign tracking identity from server-owned policy and storage.
- User store, password hashing, cookie flags/lifetime, CSRF/CORS, registration policy, account-to-`processUserId` mapping, QA validation strategy, and frontend handoff are documented in current artifacts.
- Broader release still requires the repository-level release owner to accept or close the existing full-suite waiver and release-note conditions.

## Current Table Phase Governance Status: Roll-Scoped Microfilm Resources and Targeted Audit

**Gate status: IMPLEMENTATION PASS for focused Product P3 / table Phase 2 scope, with existing full-suite waiver still applying to broader release policy.**

Governance observes implementation evidence for roll-scoped table resources, targeted per-cell audit facts, legacy migration compatibility, and tracking-not-authorization guardrails. Roll-scoped work is scoped to Microfilm table addressing and audit; it does not introduce authorization, QueryHub gating, or client-selected actor identity.

Roll-scoped guardrails:
- Canonical row addressing is `rollId + rowId`; legacy client-wide routes remain compatibility surfaces during migration.
- Legacy create adapters must not use a `rollId` belonging to another `{clientId}` route.
- Regular/custom row routes must not mutate the opposite stored row kind.
- Actor stamping comes from backend session resolution; request DTOs must not accept or trust `ProcessUserID`, `processUserId`, operator ID, or delegated actor fields.
- Existing unmapped/unidentified tracking states do not block commands by default.

## Source Artifact Inventory

| Artifact | Status | Governance notes |
| --- | --- | --- |
| Original plan: `C:\Users\ajohnston\.copilot\session-state\f0fd027a-2db0-4e34-bfa0-ffa121ee6bc8\plan.md` | Present by lifecycle reference | Intake/source scope referenced by Product, Vision, and Design. |
| `/docs/vision.md` | Present / Pass | Product-owned blocker closed; now also identifies backend-owned ASP.NET Core cookie login/register as the next identity goal. |
| `/docs/product_backlog.md` | Present / Pass | Contains product stance, guardrails, completed P1 baseline, P2 cookie-auth stories/acceptance criteria, and trace labels. |
| `/docs/design.md` | Present / Pass | Documents tracking-only design, passive principal consumption, config mapping, cookie auth architecture/security decisions, and roll-scoped table design. |
| `/docs/execution_log.md` | Present / Pass | Captures implementation, write DTO audit, build/test evidence, HTTP validation rerun evidence, cookie identity evidence, roll-scoped table evidence, and technical debt. |
| `/docs/qa_plan.md` | Present / Updated | QA accepted Phase 1 HTTP evidence and focused tests; now includes cookie identity and roll-scoped table evidence. |
| `/docs/governance_traceability.md` | Updated | This artifact records Phase 1 re-gate decisions, cookie identity gate status, roll-scoped table gate status, risks, waivers, and release conditions. |
| `readme.md` / docs index | Present / Pass | Quick start/build/test/start guidance and SDLC artifact links observed, including direct Vision link. |
| `.gitignore`, `src/`, `tests/`, `docs/` | Present / Pass | Repository hygiene baseline observed. |

## Traceability Matrix

| Requirement / story | Product source | Design link | Execution evidence | QA evidence | Governance status |
| --- | --- | --- | --- | --- | --- |
| Tracking-only, not authorization; no permission checks, `[Authorize]`, QueryHub auth, login screens, command blocking, or permission proof from Windows/Formatic mapping | Vision V2; `P1-S1`, `P1-S2`, `P1-S3` | `docs/design.md` architecture guardrails and auth stance | `Outermind.Web/Program.cs`; `SessionController.cs`; no auth middleware added | QA static scan found no forbidden auth additions | Pass. Governance scan found no forbidden auth patterns under `Outermind.Web`. |
| Same-origin `GET /api/session` returns `200 OK` for normal tracking states | Vision V1/V3; `P1-S1` | Session controller design and sequence | `Outermind.Web/Controllers/SessionController.cs`; manual HTTP evidence in `docs/execution_log.md` | QA-P1-S1-001 and QA-P1-S1-004 pass | Pass for required Phase 1 HTTP default `unidentified`; `identified` and `unmapped` covered by resolver tests and remain host-principal dependent for HTTP. |
| Response fields: `status`, `displayLabel`, `windowsAccount`, `userPrincipalName`, `processUserId`, `trackingSource`; camelCase JSON; non-empty label | Vision V3; `P1-S1` | DTO and resolver rules | `InteractionSession.cs`; `InteractionIdentityResolver.cs`; manual HTTP body evidence | Resolver tests plus HTTP body evidence | Pass. |
| `unidentified`: null account/process fields, `trackingSource=none`, neutral label | Vision V3; `P1-S1`, `P1-S2` | Principal extraction rules | Resolver implementation and HTTP transcript | Focused resolver tests and manual HTTP evidence | Pass. |
| `unmapped`: observed account fields may exist, `processUserId=null`, no denial | Vision V3; `P1-S1`, `P1-S2` | Resolver status algorithm | Resolver implementation and tests | Focused resolver tests | Pass by resolver coverage; HTTP host-principal validation not required unless re-scoped. |
| `identified`: configured mapping resolves process user and display label | Vision V2; `P1-S1`, `P1-S2` | Mapping configuration schema | Resolver implementation, `InteractionIdentity` appsettings defaults, tests | Focused resolver tests | Pass by resolver/config coverage; real Windows capture accepted out of Phase 1 scope. |
| Passive principal consumption only; no Negotiate/auth middleware unless researched | `P1-S3` | Auth/Negotiate stance | `Program.cs` registers resolver only; no auth/Negotiate registration | QA static scan pass | Pass. If real Windows capture is required, loop to Research before adding middleware. |
| Config-backed account mapping, normalized; no committed personal mappings | `P1-S2` | Configuration schema and local QA secret guidance | `appsettings.json` empty `accountMappings`; `UserSecretsId`; duplicate/normalization tests | Focused tests pass | Pass. |
| Current write DTOs audited; actor fields not trusted/used; no future durable actor migration | Vision V4; `P1-S4`; future audit deferred | Component diagram and write-stamping boundaries | `docs/execution_log.md` write endpoint audit; no Microfilm command/event actor metadata added | QA actor-field audit pass | Pass. Existing domain operator concepts are outside this Phase 1 tracking mapping. |
| Frontend-facing docs updated with tracking-only contract | `P1-S5` | Design handoff and docs requirements | `backend-integration-guide.md`; handoff supersession note | QA docs check pass | Pass. |
| Build and tests documented/run | `P1-S6` | QA validation strategy | Solution build passed; focused resolver tests passed; full suite failed unrelated; HTTP validation passed manually | QA conditional pass; focused resolver tests 6/6 | Conditional Pass. Full-suite failure remains W-GOV-001 feature-gate waiver. |
| Repository SDLC documentation baseline | `FND-S1`; Vision V5 | Product/Design cross-reference requirements | Vision, Product, Design, Execution, QA, Governance docs present; README index updated | QA documentation verification plus Governance inspection | Pass for feature gate. Stale QA note about README Vision link is non-blocking and superseded by direct inspection. |
| P2 backend-owned register/login/logout/session identity | Vision V2/V3; Product `P2-S1` through `P2-S4` | Cookie-auth architecture, endpoint shapes, cookie-principal preference in resolver | `AuthController`, `InteractionAuth` services, `InteractionIdentityResolver` backend-cookie preference | Focused auth/session tests 17/17 | Pass for focused tracking-identity MVP. |
| P2 cookie/session security evidence | Vision security requirement; Product `P2-S5` | Cookie settings, logout behavior, CSRF/CORS considerations | Cookie config in `Program.cs`/options, CSRF service/filter, redirect suppression, explicit-origin credentialed CORS | Focused CSRF/auth tests and docs review | Pass for MVP; no lockout/rate limiting remains explicitly risk-accepted for local/internal use. |
| P2 user store and password hashing | Product `P2-S1`, `P2-S2`; Design decisions required | Minimal store with `IPasswordHasher<ApplicationUser>` | `FileApplicationUserStore`, `ApplicationUserManager`, `App_Data/` gitignore | Focused manager/controller tests | Pass for MVP file-backed store; durable production store remains future hardening. |
| P2 CSRF/CORS and frontend handoff | Product `P2-S5`, `P2-S6`; Design CSRF/CORS topology | Same-origin preferred, explicit credentialed origins only, double-submit CSRF for unsafe methods | `backend-integration-guide.md`, `frontend-userauth-feedback2-response.md`, CSRF implementation | Focused CSRF tests and docs review | Pass. |
| P2 no authorization scope creep | Vision V4/V5; Product guardrails and `P2-S7` | No global fallback authorization, no `[Authorize]` gates on existing Microfilm APIs by default | Static scan found no `[Authorize]`, `UseAuthorization`, `RequireAuthorization`, `AddAuthorization`; no QueryHub auth | QA static evidence | Pass. Any future route gating requires Product re-scope. |
| Roll-scoped resources replace client-wide table ownership for new rows | Product `P3-S1`, `P3-S3`; Design roll-row boundary | Roll-scoped commands, topic, query model, legacy compatibility adapters | `RollMicrofilmTableTopic`, `RollMicrofilmRowsQuery`, `RollMicrofilmRowQuery`, `LegacyRowRoutingIndexQuery`, `MicrofilmController` roll routes | Focused Microfilm tests 38/38 | Pass for focused scope. Existing legacy rows remain on legacy path until migrated/recreated with `rollId`. |
| Targeted per-cell audit with backend-resolved actor | Product `P3-S2`, `P3-S4`; Design targeted audit and actor stamping | `MicrofilmCellAudit`, `MicrofilmAuditActorStamp`, targeted cell events | Roll cell update commands/events/projections; actor from `IInteractionIdentityResolver` | Focused Microfilm tests and static no-client-actor scan | Pass. No manual/delegated `ProcessUserID` override support observed. |
| Preserve tracking-not-authorization during table migration | Product guardrails; Design scope control | No authorization or QueryHub gate added | Static scan found no `[Authorize]`, `UseAuthorization`, `RequireAuthorization`, `AddAuthorization` | QA static evidence | Pass. Cookie identity remains tracking only. |
| Legacy compatibility and safe migration adapters | Product `P3-S3`, `P3-S5`; Design migration/backfill strategy | Legacy row projections consume roll facts; adapters preserve row envelopes and route boundaries while profiles own presentation | `MicrofilmRegularRowsQuery`, `MicrofilmCustomRowsQuery`, controller adapters, optimistic-field migration guide | Focused Microfilm tests and docs review | Pass for focused compatibility scope. Deleted client/roll column endpoints are not adapters. |

## Process Compliance Status

| SDLC area | Status | Assessment | Owner / next action |
| --- | --- | --- | --- |
| Intake | Pass | Original plan exists and remains referenced by downstream artifacts. | None. |
| Product | Pass | `/docs/vision.md` and `/docs/product_backlog.md` establish scope, non-goals, risks, and trace labels. | None for this gate. |
| Design | Pass | Design records tracking-only guardrails, cookie identity architecture, and roll-scoped table design. | None for focused gates. |
| Execution | Pass with waiver dependency | Feature code and focused execution evidence are present for session tracking, cookie identity, and roll-scoped tables. | Execution: fix unrelated `ClientProfileRegistryTests.MatchesCorrectProfile` before any no-waiver release. |
| QA | Conditional Pass | QA accepted HTTP evidence and focused tests; full suite not rerun because Governance waiver remains active. QA plan now includes cookie identity and roll-scoped table evidence. | None for focused gates. |
| Governance | Conditional Pass | Prior blockers are closed; focused implementation gates pass with explicit full-suite waiver and release-packaging condition. | Governance: monitor waiver and release conditions if scope changes. |
| P2 cookie identity | Pass for focused MVP | Backend-owned register/login/logout/session identity is implemented with documented security controls and no authorization scope creep. | Broader release: decide durable store/rate limiting/lockout requirements. |
| Product P3 / table Phase 2 | Pass for focused implementation | Roll-scoped table resources, targeted audit, and legacy compatibility are implemented and focused-tested. | Broader release: document migration/backfill and legacy route limitations. |

## Risk Register

| ID | Risk | Severity | Status | Owner | Required evidence / mitigation |
| --- | --- | --- | --- | --- | --- |
| R-GOV-001 | `/api/session` HTTP route/status/header/camelCase serialization lacked direct evidence. | High / previously blocking | **Closed for Phase 1** | Execution -> QA | Manual HTTP validation recorded and QA accepted: 200, JSON content type, `Cache-Control: no-store`, camelCase default `unidentified` body. Future improvement: automate endpoint coverage. |
| R-GOV-002 | Required `/docs/vision.md` was missing. | High / previously blocking | **Closed** | Product | Vision artifact exists with required sections and traceability. |
| R-GOV-003 | README/docs index lacked quick start and SDLC artifact links. | High / previously blocking | **Closed** | Execution/Documentation | README now includes quick start/build/test/start guidance and links Vision, Product, Design, Execution, QA, Governance, and backend guide. |
| R-GOV-004 | Full `Quantum.Tests` suite is not green due unrelated `ClientProfileRegistryTests.MatchesCorrectProfile`. | Medium | **Waived for Phase 1 feature gate only** | Execution | Fix and rerun before any no-waiver release gate; retain waiver rationale if broader release owner accepts it. |
| R-GOV-005 | Host principal capture may remain `unidentified` under current local settings. | Medium | **Accepted for Phase 1 scope** | Research/Execution if re-scoped | If real Windows identity capture is required, validate hosting behavior before adding Negotiate/auth middleware. |
| R-GOV-006 | Duplicate mapping configuration errors surface as infrastructure exceptions; production error shaping not HTTP-validated. | Low | Accepted / monitor | Execution | Include in future hardening or endpoint integration test coverage. |
| R-GOV-007 | Release notes/CHANGELOG for the feature were not assessed/compiled in this gate. | Medium for release packaging | Open for production release readiness; not blocking this feature gate | Product/Execution | Compile release notes linked to backlog/design/execution items before broader production release. |
| R-GOV-008 | QA plan contains stale attention text about README Vision link after README remediation. | Low | Open / non-blocking | QA/Documentation | Reconcile `docs/qa_plan.md` on next update; Governance directly verified the README Vision link. |
| R-GOV-009 | Cookie authentication could be mistaken for authorization, causing accidental `[Authorize]`, role gates, QueryHub auth, or command blocking. | High for P2 scope | **Closed for focused MVP / monitor** | Product, Design, Execution, Governance | Static scan and implementation review show no authorization gates. Product must explicitly re-scope authorization before any permission behavior is added. |
| R-GOV-010 | Authentication cookie/session security may be under-evidenced. | High for P2 release | **Closed for focused MVP / broader release monitor** | Design, Execution, QA | Implemented/documented `HttpOnly`, `Secure` outside dev, `SameSite`, scheme, API redirect suppression, lifetime/sliding policy, logout clearing, CSRF/CORS strategy, and focused tests. |
| R-GOV-011 | User store/password hashing approach is not finalized before coding. | High for P2 entry | **Closed for MVP** | Design, Execution | MVP uses file-backed store plus `IPasswordHasher<ApplicationUser>`; durable production store remains future hardening. |
| R-GOV-012 | CSRF/CORS controls are missing or inconsistent for cookie-backed unsafe requests. | High for P2 release | **Closed for focused MVP / monitor** | Design, Execution, QA | Same-origin/double-submit CSRF and explicit-origin credentialed CORS are implemented and documented. |
| R-GOV-013 | Registration policy and account-to-`processUserId` mapping remain ambiguous. | Medium-High for P2 entry | **Closed for MVP** | Product, Design | Open self-registration, immediate sign-in, username/display rules, duplicate handling, and server-generated `processUserId` are documented and implemented. |
| R-GOV-014 | Frontend handoff may be incomplete, causing clients to reintroduce manual identity selection or mishandle CSRF/session refresh. | Medium for P2 release | **Closed for focused MVP / monitor** | Design, Execution/Documentation, Frontend integrator | Backend guide and frontend questionnaire response document endpoints, cookie/CSRF expectations, tracking states, and no manual/delegated `ProcessUserID` override. |
| R-GOV-015 | QA plan did not include P2 cookie-auth coverage. | Medium / blocking if P2 gate requested | **Closed** | QA | QA plan now includes cookie identity implementation evidence and regression expectations. |

## Audit Trail

| Time | Event / decision | Evidence |
| --- | --- | --- |
| 2026-06-30 | Product framed Phase 1 as tracking-only backend session tracking and deferred authorization/future audit model. | `docs/product_backlog.md`, original plan. |
| 2026-06-30 | Design selected passive `HttpContext.User` resolver, config-backed mapping, `/api/session`, and no auth scope creep. | `docs/design.md`. |
| 2026-06-30 | Execution implemented controller/resolver/config/tests/docs and audited current write DTOs. | `docs/execution_log.md`, code files. |
| 2026-06-30 | Execution build passed; targeted resolver tests passed; full suite failed unrelated ClientProfileRegistry expectation. | `docs/execution_log.md`. |
| 2026-06-30 12:16 EDT | QA issued Conditional Pass with HTTP validation, full-suite waiver/fix, and documentation baseline conditions. | `docs/qa_plan.md`. |
| 2026-06-30 12:24 EDT | Governance created traceability artifact, waived unrelated full-suite failure for feature gate only, accepted real Windows capture as out of Phase 1, and blocked signoff pending HTTP and documentation evidence. | Prior version of this file. |
| 2026-06-30 12:36 EDT | QA re-reviewed loopback evidence, accepted HTTP validation and focused resolver tests, and recommended Conditional Pass for Governance re-review. | `docs/qa_plan.md`; `docs/execution_log.md`. |
| 2026-06-30 13:02 EDT | Governance re-reviewed artifacts and code, verified README Vision link and no forbidden auth patterns, closed prior blockers, and changed the gate to Conditional Pass. | This file; `readme.md`; code inspection; Governance scan. |
| 2026-06-30 13:57 EDT | Governance recorded the changed next identity goal: backend-owned ASP.NET Core cookie register/login/logout/session tracking. Phase 1 Conditional Pass preserved; P2 implementation and release criteria added. | `docs/vision.md`; `docs/product_backlog.md`; `docs/design.md`; this file. |
| 2026-06-30 | Execution implemented Product P3 / table Phase 2 roll-scoped Microfilm resources, targeted audit, legacy compatibility adapters, and focused tests. | `docs/execution_log.md`; `backend-integration-guide.md`; Microfilm commands/events/topics/queries/controllers/tests. |
| 2026-06-30 | Governance recorded focused implementation pass for roll-scoped table scope, including compatibility fixes for legacy create route ownership, legacy PATCH response shape, and regular/custom row-kind enforcement. | This file; `docs/qa_plan.md`; focused Microfilm tests. |

## Stage Readiness Checks

| Gate / criterion | Result | Notes |
| --- | --- | --- |
| Documentation completeness gate | **Pass for feature gate** | Required docs exist: Vision, Product, Design, Execution, QA, Governance. README contains quick start and links. Adjacent-stage labels are present across artifacts. Stale QA note is non-blocking and documented as R-GOV-008. |
| Repository hygiene gate | **Pass** | `.gitignore`, `src/`, `tests/`, and `docs/` observed. |
| Test and quality gate | **Conditional Pass** | Build and focused resolver tests passed; manual HTTP evidence accepted. Full suite remains under W-GOV-001 waiver for this feature gate only. |
| Traceability completeness | **Pass for scoped feature** | Requirement-to-design-to-execution-to-QA links are present; closed blockers are recorded. |
| Scope control | **Pass** | No auth/authorization/QueryHub/login/command-blocking expansion observed. Future roll-row audit migration not implemented. |
| Release readiness checklist | **Conditional / not full production signoff** | No open critical Phase 1 gaps remain. Full-suite waiver and release notes/CHANGELOG condition must be addressed or accepted by a broader release owner before no-waiver production release. |
| P2 cookie-auth implementation gate | **Pass for focused scope** | Register/login/logout/session, file-backed store, password hashing, cookie settings, CSRF/CORS, no manual `ProcessUserID` override, and no authorization scope creep are implemented and focused-tested. |
| P2 cookie-auth broader release gate | **Conditional / not full production signoff** | Durable production store, rate limiting/lockout, release notes, and the existing full-suite waiver remain broader release considerations. |
| Roll-scoped table implementation gate | **Pass for focused scope** | Focused Microfilm tests pass, solution build passes, static no-authz scan passes, and frontend/backend contract docs describe roll-scoped routes and compatibility behavior. Full-suite waiver remains separate. |

## Gating Decisions and Waivers

### GD-GOV-001 - Phase 1 feature/stage gate

**Decision: CONDITIONAL PASS.** Prior blockers are closed. The feature can exit Governance for the scoped Phase 1 backend session tracking gate with W-GOV-001 and A-GOV-001 explicitly recorded.

### GD-GOV-002 - Documentation completeness loopback

**Decision: PASS.** `/docs/vision.md` exists and README has quick start plus SDLC artifact links including Vision. QA stale attention text is documented as non-blocking R-GOV-008.

### GD-GOV-003 - HTTP/API validation loopback

**Decision: PASS for Phase 1.** Manual HTTP evidence validates default local `unidentified` route/status/header/serialization. `identified` and `unmapped` HTTP validation are not required unless a host-supplied principal becomes a Phase 1 requirement; resolver tests cover those states.

### W-GOV-001 - Full-suite failure

**Decision: Waived for Phase 1 feature gate only.** Rationale: the failing `ClientProfileRegistryTests.MatchesCorrectProfile` assertion is unrelated to session tracking per Execution and QA evidence. This waiver does not waive a broader requirement for a green full suite before production release.

### A-GOV-001 - Missing real Windows identity capture

**Decision: Accepted for Phase 1 scope.** The contract supports passive principal consumption; `unidentified` is a valid tracking state. If stakeholders require real Windows identity capture in Phase 1, loop back to Research/Execution before adding Negotiate/auth middleware.

### GD-GOV-004 - P2 cookie-auth implementation gate

**Decision: PASS for focused tracking-identity MVP.** Execution provided register/login/logout/session implementation, backend-owned user store and password hashing, cookie flags/lifetime, CSRF/CORS controls, server-owned `processUserId` assignment, frontend handoff docs, focused tests, and static no-authorization evidence.

### GD-GOV-005 - P2 cookie-auth release gate conditions

**Decision: CONDITIONAL for broader release.** Focused MVP evidence exists. Broader release cannot pass without evidence or owner acceptance for:

- Register, login, logout, and `GET /api/session` behavior match Product/Design contracts, including identified/unmapped/unidentified states and `trackingSource = backend-cookie` where applicable.
- Passwords are hashed with ASP.NET Core Identity/`IPasswordHasher<TUser>` or approved equivalent; no plaintext credentials, hashes, auth tickets, real secrets, or cookie values are committed or logged.
- Auth cookie flags and behavior are validated for the target environment: `HttpOnly`, `Secure` outside development, appropriate `SameSite`, configured lifetime, redirect suppression for JSON APIs, logout clearing, and `Cache-Control: no-store` where required.
- CSRF/CORS controls are implemented and tested for cookie-backed unsafe requests; credentialed CORS, if any, is explicit-origin only.
- Static/diff review shows no authorization scope creep into existing application/data routes, QueryHub, command blocking, role gates, or access-denied UX unless Product created a separate authorization scope.
- Client-submitted `ProcessUserID`, `processUserId`, `userId`, operator ID, or equivalent actor override is ignored/overwritten for tracking-sensitive workflows.
- QA evidence covers automated tests or documented manual validation for all P2 acceptance criteria; any test harness gaps are explicitly waived.
- Frontend-facing handoff docs and versioned CHANGELOG/release notes are complete and linked to Product/Design/Execution/QA evidence.

### GD-GOV-006 - Product P3 / table Phase 2 roll-scoped implementation gate

**Decision: PASS for focused implementation scope.** Execution provided roll-scoped Microfilm commands/events/topic/query/controller changes, legacy compatibility adapters, backend session actor stamping, frontend-facing contract updates, focused tests, static no-authorization evidence, and a passing solution build.

Conditions for broader release remain:

- Full `Quantum.Tests` remains subject to W-GOV-001 unless the unrelated baseline failure is fixed or a broader release owner accepts the waiver.
- Existing client-wide rows are not automatically backfilled with `rollId`; release notes must state that legacy rows remain on legacy paths until migrated/recreated with `rollId`.
- Profile presentation changes must not be treated as schemas or used to mutate durable cells/audits; frontend joins remain responsible for interpretation.
- Any future authorization, role, permission, QueryHub auth, or command-blocking behavior requires a separate Product/Design/Governance scope.

## Remaining Owner Actions

1. **Execution owner:** Fix `ClientProfileRegistryTests.MatchesCorrectProfile` and rerun full `Quantum.Tests` before any no-waiver release gate, or obtain broader release-owner acceptance of W-GOV-001.
2. **Product/Execution owners:** Prepare versioned release notes or CHANGELOG links before broader production release readiness review.
3. **QA/Documentation owner:** Update `docs/qa_plan.md` to remove the stale README Vision-link attention note at the next QA document refresh.
4. **Research/Execution owners, only if re-scoped:** Validate real Windows/Negotiate principal capture before introducing auth middleware.
5. **Documentation/release owner:** Include roll-scoped migration notes in release notes: new rows should prefer roll-scoped routes, legacy rows remain legacy until migrated/recreated, and legacy create with `rollId` must match the route client.
6. **Product/Design owner before profile expansion:** Preserve profile-owned presentation semantics and ensure frontend joins do not delete omitted durable fields or audits.
7. **Execution owner before broader release:** Fix or waive the unrelated full-suite failure under the applicable release policy.
8. **Documentation/frontend handoff owner:** Keep frontend-facing docs aligned with cookie CSRF/credential handling and roll-scoped table route preferences; ensure the handoff does not reintroduce manual/delegated identity selection.

## Retrospective / Process Improvement Notes

- Add a lightweight ASP.NET endpoint test harness for `/api/session` so route/header/serialization evidence is automated instead of manual.
- Treat `/docs/vision.md` and README SDLC links as intake/product prerequisites before the first Governance review.
- Record unrelated baseline test failures before feature work begins so waivers are smaller and easier to verify.
- Add a release-note checklist item to Execution handoff whenever feature work changes API contracts.
- For auth-related initiatives, require a pre-implementation security evidence checklist covering cookie settings, CSRF/CORS, credential storage, logging redaction, and route-authorization diff review.
- Keep the tracking/authentication/authorization vocabulary explicit in Product, Design, QA, and Governance docs to prevent accidental scope drift.
