# Governance Traceability: Phase 1 Backend Session Tracking and P2 Cookie Identity Planning

Last updated: 2026-06-30 13:57 EDT
Governance owner: Governance Agent
Repository: `AlexanderJohnston/Totem`
Lifecycle: Intake -> Product -> Design -> Execution -> QA -> Governance
Work classification: New initiative / feature; P2 planning update only

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

## Next Phase Governance Planning Status: Backend-Owned ASP.NET Core Cookie Login/Register

**Planning status: Governance traceability updated; Phase 1 conditional pass is preserved.** This update records the stakeholder decision that the next identity initiative is backend-owned ASP.NET Core cookie register/login/logout/session identity. It does **not** implement code and does **not** convert the completed Phase 1 gate into a failed gate.

Governance observes that `/docs/vision.md`, `/docs/product_backlog.md`, and `/docs/design.md` have been updated for the cookie-auth direction. `/docs/qa_plan.md` remains primarily Phase 1-focused and must be refreshed by QA before P2 implementation readiness or release signoff is requested.

P2 governance guardrails:
- Cookie login establishes the backend-recognized browser identity for tracking; it does not add permission checks, QueryHub authorization, role gates, command blocking, or access-denied UI unless Product separately scopes authorization.
- No manual/delegated `ProcessUserID` override may be reintroduced. Registration/login must assign tracking identity from server-owned policy and storage.
- Implementation entry requires documented decisions for user store/password hashing, cookie flags/lifetime, CSRF/CORS, registration policy, account-to-`processUserId` mapping, QA validation strategy, and frontend handoff readiness. See GD-GOV-004.
- Future release requires executable evidence for authentication/session security controls and no authorization scope creep. See GD-GOV-005.

## Source Artifact Inventory

| Artifact | Status | Governance notes |
| --- | --- | --- |
| Original plan: `C:\Users\ajohnston\.copilot\session-state\f0fd027a-2db0-4e34-bfa0-ffa121ee6bc8\plan.md` | Present by lifecycle reference | Intake/source scope referenced by Product, Vision, and Design. |
| `/docs/vision.md` | Present / Pass | Product-owned blocker closed; now also identifies backend-owned ASP.NET Core cookie login/register as the next identity goal. |
| `/docs/product_backlog.md` | Present / Pass | Contains product stance, guardrails, completed P1 baseline, P2 cookie-auth stories/acceptance criteria, and trace labels. |
| `/docs/design.md` | Present / Pass | Documents tracking-only design, passive principal consumption, config mapping, and P2 cookie-auth architecture/security decisions required before implementation. |
| `/docs/execution_log.md` | Present / Pass | Captures implementation, write DTO audit, build/test evidence, HTTP validation rerun evidence, and technical debt. |
| `/docs/qa_plan.md` | Present / Conditional Pass for P1; P2 update pending | QA accepted Phase 1 HTTP evidence and focused tests. QA plan still needs P2 register/login/logout/session, security, CSRF/CORS, and no-authz-scope coverage before implementation/release gating. |
| `/docs/governance_traceability.md` | Updated | This artifact records the Phase 1 re-gate decision, closed blockers, risks, waivers, and P2 cookie-auth entry/release gate criteria. |
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
| P2 backend-owned register/login/logout/session identity | Vision V2/V3; Product `P2-S1` through `P2-S4` | Cookie-auth architecture, endpoint shapes, cookie-principal preference in resolver | Pending; no code implementation expected in this planning update | QA plan update pending for auth transitions and session states | Entry criterion. Execution must provide register/login/logout/session evidence before release; QA must validate success, invalid, logout, expired/no-cookie, identified/unmapped/unidentified states. |
| P2 cookie/session security evidence | Vision security requirement; Product `P2-S5` | Cookie settings, logout behavior, CSRF/CORS considerations | Pending | QA plan update pending for cookie flags and CSRF/CORS checks | Entry/release criterion. Evidence required for `HttpOnly`, `Secure` outside dev, `SameSite`, lifetime/sliding/remember-me policy, redirect suppression, logout clearing, and no sensitive logging. |
| P2 user store and password hashing | Product `P2-S1`, `P2-S2`; Design decisions required | ASP.NET Core Identity or minimal store with `IPasswordHasher<TUser>` | Pending | QA plan update pending for duplicate/invalid registration and credential safety | Entry criterion. Design/Execution must document durable store, uniqueness, password hashing, security-stamp/session invalidation, and no plaintext/config credentials before implementation. |
| P2 CSRF/CORS and frontend handoff | Product `P2-S5`, `P2-S6`; Design CSRF/CORS topology | Same-origin preferred, explicit credentialed origins only, antiforgery for unsafe methods | Pending | QA plan update pending | Entry/release criterion. Frontend-facing docs must define routes, credential behavior, CSRF token/header pattern if used, session refresh timing, and no manual identity override. |
| P2 no authorization scope creep | Vision V4/V5; Product guardrails and `P2-S7` | No global fallback authorization, no `[Authorize]` gates on existing Microfilm APIs by default | Pending static/diff evidence | QA plan update pending | Blocking release criterion. Any route gating, QueryHub auth, roles, permissions, or write blocking requires Product re-scope and new traceability. |

## Process Compliance Status

| SDLC area | Status | Assessment | Owner / next action |
| --- | --- | --- | --- |
| Intake | Pass | Original plan exists and remains referenced by downstream artifacts. | None. |
| Product | Pass | `/docs/vision.md` and `/docs/product_backlog.md` establish scope, non-goals, risks, and trace labels. | None for this gate. |
| Design | Pass | Design records tracking-only guardrails, component design, config rules, and Research loopback condition for Windows capture. | None for Phase 1. |
| Execution | Pass with waiver dependency | Feature code and execution evidence are present; HTTP validation rerun evidence is recorded. | Execution: fix unrelated `ClientProfileRegistryTests.MatchesCorrectProfile` before any no-waiver release. |
| QA | Conditional Pass | QA accepted HTTP evidence and focused tests; full suite not rerun because Governance waiver remains active. | QA/Documentation: clean stale README Vision-link note at next touch. |
| Governance | Conditional Pass | Prior blockers are closed; feature gate passes with explicit waiver and release-packaging condition. P2 planning traceability is now recorded. | Governance: monitor Phase 1 waiver if scope changes; enforce P2 entry/release gates before implementation or production signoff. |
| P2 Product/Design planning | Pass for planning | Vision, Product backlog, and Design now identify backend-owned ASP.NET Core cookie auth as the next tracking identity goal and preserve tracking-not-authorization boundaries. | Product/Design: close open decisions before implementation entry. |
| P2 QA planning | Pending update | QA plan remains Phase 1-focused and has not yet added cookie-auth scenarios/security evidence. This does not reopen Phase 1, but blocks P2 implementation/release readiness if unresolved. | QA: update `/docs/qa_plan.md` with P2 coverage before implementation/release gate. |
| P2 Execution readiness | Not started / not assessed | No code implementation is expected in this planning update. | Execution: do not begin P2 coding until GD-GOV-004 entry evidence is available. |

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
| R-GOV-009 | Cookie authentication could be mistaken for authorization, causing accidental `[Authorize]`, role gates, QueryHub auth, or command blocking. | High for P2 scope | Open / P2 gate risk | Product, Design, Execution, Governance | Product must explicitly re-scope authorization before any permission behavior is added; Execution/QA must provide static/diff evidence that existing app/data routes remain ungated for the cookie-auth MVP. |
| R-GOV-010 | Authentication cookie/session security may be under-evidenced. | High for P2 release | Open / P2 release gate | Design, Execution, QA | Evidence required: `HttpOnly`, `Secure` outside dev, appropriate `SameSite`, named scheme, API redirect suppression, lifetime/sliding/remember-me policy, logout cookie clearing, no sensitive auth logging, and `Cache-Control: no-store` on session/logout responses. |
| R-GOV-011 | User store/password hashing approach is not finalized before coding. | High for P2 entry | Open / implementation entry criterion | Design, Execution | Document chosen store, unique normalized identifiers, stable immutable user ID, password hashing via ASP.NET Core Identity/`IPasswordHasher<TUser>`, no plaintext/config credentials, security-stamp or disable/session invalidation behavior. |
| R-GOV-012 | CSRF/CORS controls are missing or inconsistent for cookie-backed unsafe requests. | High for P2 release | Open / P2 gate risk | Design, Execution, QA | Prefer same-origin; for credentialed CORS use explicit origins only. Define antiforgery/token/header and Origin/Referer/rate-limit strategy for unsafe methods, including register/login/logout where applicable; validate by QA. |
| R-GOV-013 | Registration policy and account-to-`processUserId` mapping remain ambiguous. | Medium-High for P2 entry | Open / implementation entry criterion | Product, Design | Product must decide open vs invite/admin-seeded registration, username/email/display-name rules, duplicate handling, whether registration auto-signs-in, and server-owned process-user mapping/unmapped behavior. |
| R-GOV-014 | Frontend handoff may be incomplete, causing clients to reintroduce manual identity selection or mishandle CSRF/session refresh. | Medium for P2 release | Open | Design, Execution/Documentation, Frontend integrator | Backend integration docs must list final register/login/logout/session endpoints, cookie/CSRF expectations, session refresh timing, tracking states, and no manual/delegated `ProcessUserID` override. |
| R-GOV-015 | QA plan is not yet updated for P2 cookie-auth coverage. | Medium / blocking if P2 gate requested | Open | QA | Add QA scenarios for registration, duplicate/invalid registration, login, invalid login, logout, expired/no cookie, cookie flags, CSRF/CORS, no authorization scope creep, and frontend handoff evidence. |

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
| 2026-06-30 13:57 EDT | Governance recorded the changed next identity goal: backend-owned ASP.NET Core cookie register/login/logout/session tracking. Phase 1 Conditional Pass preserved; P2 implementation entry and release gate criteria added. | `docs/vision.md`; `docs/product_backlog.md`; `docs/design.md`; this file. QA plan observed as P1-focused and queued for QA update before P2 gate. |

## Stage Readiness Checks

| Gate / criterion | Result | Notes |
| --- | --- | --- |
| Documentation completeness gate | **Pass for feature gate** | Required docs exist: Vision, Product, Design, Execution, QA, Governance. README contains quick start and links. Adjacent-stage labels are present across artifacts. Stale QA note is non-blocking and documented as R-GOV-008. |
| Repository hygiene gate | **Pass** | `.gitignore`, `src/`, `tests/`, and `docs/` observed. |
| Test and quality gate | **Conditional Pass** | Build and focused resolver tests passed; manual HTTP evidence accepted. Full suite remains under W-GOV-001 waiver for this feature gate only. |
| Traceability completeness | **Pass for scoped feature** | Requirement-to-design-to-execution-to-QA links are present; closed blockers are recorded. |
| Scope control | **Pass** | No auth/authorization/QueryHub/login/command-blocking expansion observed. Future roll-row audit migration not implemented. |
| Release readiness checklist | **Conditional / not full production signoff** | No open critical Phase 1 gaps remain. Full-suite waiver and release notes/CHANGELOG condition must be addressed or accepted by a broader release owner before no-waiver production release. |
| P2 cookie-auth implementation entry | **Conditional readiness criteria defined; not a code-start signoff yet** | Before implementation, Product/Design/QA/Execution must satisfy GD-GOV-004: registration policy, user store/password hashing, cookie flags/lifetime, CSRF/CORS, process-user mapping, QA scenarios, no authorization scope creep, and frontend handoff plan. |
| P2 cookie-auth release gate | **Not assessed; future blocking gate conditions defined** | Release cannot pass until GD-GOV-005 evidence exists for auth/session security, cookie flags, credential storage, CSRF/CORS, register/login/logout/session behavior, no manual `ProcessUserID` override, no unauthorized route gating, QA coverage, and release notes. |

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

### GD-GOV-004 - P2 cookie-auth implementation entry gate

**Decision: PLANNING CONDITIONAL READY; implementation entry criteria are blocking for P2.** This is not a release signoff and does not reopen Phase 1. Execution may enter P2 implementation only when the following evidence is documented or explicitly waived with owner/date:

1. **Product owner:** Registration policy, account identifier/display-name rules, duplicate handling, successful-registration sign-in behavior, and server-owned account-to-`processUserId`/unmapped policy are decided.
2. **Design owner:** User store and password hashing approach is selected; cookie scheme settings, lifetime/sliding/remember-me policy, logout/session invalidation, CSRF/CORS topology, and API DTO/route shapes are finalized.
3. **QA owner:** `/docs/qa_plan.md` includes P2 test coverage and evidence expectations for register/login/logout/session, cookie flags, CSRF/CORS, invalid credentials, duplicate/invalid registration, no-cookie/expired-cookie, no authorization scope creep, and no manual identity override.
4. **Execution owner:** Implementation plan confirms no general authorization, `[Authorize]` gates on existing Microfilm/data routes, QueryHub auth, command blocking, or client-submitted `ProcessUserID` trust will be added without Product re-scope.
5. **Documentation/frontend handoff owner:** Backend integration docs have an update plan for final endpoint shapes, credentialed requests, CSRF token/header use if selected, session refresh timing, and tracking-not-authorization messaging.

Required re-entry evidence for Governance: updated Product/Design/QA docs or linked execution planning notes that close each item above.

### GD-GOV-005 - P2 cookie-auth release gate conditions

**Decision: FUTURE RELEASE GATE NOT ASSESSED; conditions are blocking when P2 release is requested.** Release cannot pass without evidence that:

- Register, login, logout, and `GET /api/session` behavior match Product/Design contracts, including identified/unmapped/unidentified states and `trackingSource = backend-cookie` where applicable.
- Passwords are hashed with ASP.NET Core Identity/`IPasswordHasher<TUser>` or approved equivalent; no plaintext credentials, hashes, auth tickets, real secrets, or cookie values are committed or logged.
- Auth cookie flags and behavior are validated for the target environment: `HttpOnly`, `Secure` outside development, appropriate `SameSite`, configured lifetime, redirect suppression for JSON APIs, logout clearing, and `Cache-Control: no-store` where required.
- CSRF/CORS controls are implemented and tested for cookie-backed unsafe requests; credentialed CORS, if any, is explicit-origin only.
- Static/diff review shows no authorization scope creep into existing application/data routes, QueryHub, command blocking, role gates, or access-denied UX unless Product created a separate authorization scope.
- Client-submitted `ProcessUserID`, `processUserId`, `userId`, operator ID, or equivalent actor override is ignored/overwritten for tracking-sensitive workflows.
- QA evidence covers automated tests or documented manual validation for all P2 acceptance criteria; any test harness gaps are explicitly waived.
- Frontend-facing handoff docs and versioned CHANGELOG/release notes are complete and linked to Product/Design/Execution/QA evidence.

## Remaining Owner Actions

1. **Execution owner:** Fix `ClientProfileRegistryTests.MatchesCorrectProfile` and rerun full `Quantum.Tests` before any no-waiver release gate, or obtain broader release-owner acceptance of W-GOV-001.
2. **Product/Execution owners:** Prepare versioned release notes or CHANGELOG links before broader production release readiness review.
3. **QA/Documentation owner:** Update `docs/qa_plan.md` to remove the stale README Vision-link attention note at the next QA document refresh.
4. **Research/Execution owners, only if re-scoped:** Validate real Windows/Negotiate principal capture before introducing auth middleware.
5. **Product owner before P2 implementation:** Confirm registration policy, account identifier/display-name rules, duplicate handling, auto-sign-in behavior, and server-owned `processUserId`/unmapped policy.
6. **Design owner before P2 implementation:** Finalize user store/password hashing choice, cookie scheme flags/lifetime, logout/session invalidation, CSRF/CORS topology, route/DTO shapes, and no-authorization-scope boundaries.
7. **QA owner before P2 implementation/release gate:** Refresh `docs/qa_plan.md` with P2 cookie-auth scenarios, security checks, CSRF/CORS evidence, no-authz-scope verification, and manual/automated evidence plan.
8. **Execution owner before P2 release:** Provide build/test/auth endpoint evidence, cookie flag evidence, credential storage evidence, static no-scope-creep review, and no manual `ProcessUserID` override evidence.
9. **Documentation/frontend handoff owner:** Update frontend-facing docs after Design finalizes endpoints and CSRF/credential handling; ensure the frontend handoff does not reintroduce manual/delegated identity selection.

## Retrospective / Process Improvement Notes

- Add a lightweight ASP.NET endpoint test harness for `/api/session` so route/header/serialization evidence is automated instead of manual.
- Treat `/docs/vision.md` and README SDLC links as intake/product prerequisites before the first Governance review.
- Record unrelated baseline test failures before feature work begins so waivers are smaller and easier to verify.
- Add a release-note checklist item to Execution handoff whenever feature work changes API contracts.
- For auth-related initiatives, require a pre-implementation security evidence checklist covering cookie settings, CSRF/CORS, credential storage, logging redaction, and route-authorization diff review.
- Keep the tracking/authentication/authorization vocabulary explicit in Product, Design, QA, and Governance docs to prevent accidental scope drift.



