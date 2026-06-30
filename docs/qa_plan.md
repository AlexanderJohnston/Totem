# QA Plan: Phase 1 Backend Session Tracking and Next-Phase Cookie Identity

Last updated: 2026-06-30 13:58 EDT
QA owner: QA Agent
Lifecycle: Intake → Product → Design → Execution → QA → Governance
Recommendation: Phase 1 conditional pass remains; QA planning is current for next-phase backend-owned cookie identity

## 1. Test Strategy & Approach

[Product → QA] Re-validate the Phase 1 backend session tracking slice against `docs/product_backlog.md` acceptance criteria P1-S1 through P1-S6 and FND-S1 after Governance loopback.

[Design → QA] Confirm the design guardrails from `docs/design.md` remain intact: tracking-only behavior, passive request principal consumption, configuration-backed normalized mapping, no authorization scope creep, no future roll-row audit migration, and frontend-facing documentation updates.

[Execution → QA] Use focused automated tests plus documented Execution HTTP evidence. Expensive full-suite rerun was intentionally not performed because Governance has already waived the unrelated baseline failure for this feature gate.

[Vision → QA] [Product → QA] [Design → QA] This 2026-06-30 13:58 EDT update is planning-only for the next identity initiative: backend-owned ASP.NET Core cookie register/login/logout integrated with `/api/session`. No code was implemented and no tests were run for this document refresh. Phase 1 evidence below remains preserved as the accepted baseline; P2 cookie identity requires new implementation evidence.

Re-validation scope on 2026-06-30:
- Reviewed `docs/vision.md`, `docs/product_backlog.md`, `docs/design.md`, `docs/execution_log.md`, `docs/governance_traceability.md`, `docs/qa_plan.md`, and `readme.md`.
- Verified required repository/documentation paths exist: `.gitignore`, `/src`, `/tests`, `/docs`, `/docs/vision.md`, `/docs/product_backlog.md`, `/docs/design.md`, `/docs/execution_log.md`, `/docs/qa_plan.md`, `/docs/governance_traceability.md`, and `readme.md`.
- Accepted the newly recorded manual HTTP evidence in `docs/execution_log.md` for default local `unidentified` behavior; QA did not restart the temporary web process because the required response evidence and process stop were already recorded.
- Reran focused resolver tests only: `dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release --filter InteractionIdentityResolverTests`.
- Rechecked README SDLC links and documentation freshness risks.

## 2. Test Cases & Scenarios

| ID | Scenario | Traceability | Method | Result |
| --- | --- | --- | --- | --- |
| QA-P1-S1-001 | `GET /api/session` route exists and returns `Ok(...)` for normal resolver output | [Product → QA: P1-S1] [Design → QA] | Prior code inspection plus Execution HTTP evidence | Pass |
| QA-P1-S1-002 | Endpoint has no `[Authorize]`, challenge, forbid, login, or QueryHub auth dependency | [Product → QA: P1-S1] [Design → QA] | Static/code inspection from prior QA pass; no contrary loopback evidence | Pass |
| QA-P1-S1-003 | Response DTO fields are `status`, `displayLabel`, `windowsAccount`, `userPrincipalName`, `processUserId`, `trackingSource` | [Product → QA: P1-S1] | DTO review plus HTTP body in execution log | Pass |
| QA-P1-S1-004 | HTTP route/header/serialization: `200 OK`, `application/json`, `Cache-Control: no-store`, camelCase JSON | [Product → QA: P1-S1] [Execution → QA] | Manual HTTP evidence in `docs/execution_log.md` | Pass for default `unidentified` state |
| QA-P1-S1-005 | `displayLabel` non-empty for every normal tracking state | [Product → QA: P1-S1] | Resolver tests plus code path review | Pass |
| QA-P1-S2-001 | Unauthenticated/no observed principal resolves to `unidentified`, null account fields, null `processUserId`, `trackingSource` `none` | [Product → QA: P1-S2] | Focused resolver test and HTTP evidence | Pass |
| QA-P1-S2-002 | Authenticated Windows account with mapping resolves to `identified` with configured `processUserId` | [Product → QA: P1-S2] | Focused resolver test | Pass |
| QA-P1-S2-003 | Authenticated UPN with mapping resolves to `identified` | [Product → QA: P1-S2] | Focused resolver test | Pass |
| QA-P1-S2-004 | Authenticated principal without mapping resolves to `unmapped` and does not deny access | [Product → QA: P1-S2] | Focused resolver test and controller review | Pass |
| QA-P1-S2-005 | Account mappings are configuration-backed and normalized for case and `/` versus `\` | [Product → QA: P1-S2] [Design → QA] | Focused resolver duplicate/normalization coverage | Pass |
| QA-P1-S3-001 | Principal capture remains passive; no Negotiate/auth middleware added | [Product → QA: P1-S3] | Prior static search and artifact review | Pass |
| QA-P1-S4-001 | Current Miller/Microfilm write DTOs contain no trusted/used actor fields | [Product → QA: P1-S4] | Execution write DTO audit plus prior QA inspection | Pass |
| QA-P1-S4-002 | No durable actor metadata or future roll-row audit migration added | [Product → QA: P1-S4] [Design → QA] | Design/execution review | Pass |
| QA-P1-S5-001 | Frontend-facing docs list final tracking-only contract and normal 200 states | [Product → QA: P1-S5] | `backend-integration-guide.md`, handoff supersession, execution log | Pass |
| QA-P1-S5-002 | Docs state QueryHub auth, login, permission checks, command blocking, and manual/delegated override are out of scope | [Product → QA: P1-S5] | Documentation review | Pass |
| QA-P1-S6-001 | Focused resolver test suite passes | [Product → QA: P1-S6] [Execution → QA] | `dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release --filter InteractionIdentityResolverTests` | Pass: 6/6 on QA rerun |
| QA-P1-S6-002 | Full solution build evidence exists | [Product → QA: P1-S6] [Execution → QA] | Execution log evidence; focused QA test compiled affected projects | Pass by execution evidence |
| QA-P1-S6-003 | Full `Quantum.Tests` suite succeeds | [Product → QA: P1-S6] | Execution/Governance evidence | Waived for feature gate only; still not proven green |
| QA-FND-S1-001 | Required SDLC docs exist | [Product → QA: FND-S1] | Path verification and artifact review | Pass |
| QA-FND-S1-002 | README has quick start/build/test/start guidance and SDLC links | [Product → QA: FND-S1] | README review | Pass: key links present, including `docs/vision.md` |
| QA-FND-S1-003 | Governance traceability artifact is present and Phase 1 gate is refreshed | [Product → QA: FND-S1] | Artifact review | Pass for Phase 1; Governance should add P2 cookie identity traceability during its own next-goal review |

### Happy path scenarios
- Identified mapped Windows account returns `status=identified`, non-empty `displayLabel`, configured `processUserId`, observed account fields when present, and `trackingSource=windows-integrated-auth`. Covered by focused resolver tests.
- Identified mapped UPN returns `status=identified`, configured `processUserId`, and `userPrincipalName`. Covered by focused resolver tests.
- Same-origin `GET /api/session` returns `200 OK`, `Cache-Control: no-store`, JSON, camelCase fields, and default `unidentified` state when no host principal is present. Covered by Execution manual HTTP evidence recorded in `docs/execution_log.md`.

### Edge cases
- Unmapped authenticated principal returns `status=unmapped`, non-empty observed display label, null `processUserId`, and no access failure. Covered by resolver tests; HTTP validation remains dependent on a host-supplied principal and is not required unless real Windows capture becomes Phase 1 scope.
- Unauthenticated or no observed identity returns `status=unidentified`, neutral display label, null account fields, null `processUserId`, and `trackingSource=none`. Covered by resolver tests and manual HTTP evidence.
- Duplicate normalized aliases that point to different process users throw a configuration error. Covered by resolver tests.
- Blank mappings and blank process user IDs are ignored by implementation review from prior QA.

### Error conditions
- Normal tracking states do not return 401 or 403 by implementation design.
- `5xx` is reserved for infrastructure/configuration failures. Duplicate mapping conflict currently throws an infrastructure exception; production error-shaping is accepted as a low-priority hardening item for post-Phase 1.
- Local startup requires a WASP token. Execution supplied a non-production `Wasp__Token=local-session-validation-stub-token` for HTTP validation and stopped the temporary process afterward.

### Performance tests
- No dedicated performance test was run. Resolver work is in-memory and O(configured aliases) with no database or EventStore call. Risk is low for expected Phase 1 mapping size.

### Next-phase P2 cookie identity QA planning scenarios

[Vision → QA] [Product → QA: P2-S1 through P2-S7] [Design → QA: cookie auth architecture]

Planning status: future test plan only. These scenarios are not Phase 1 regressions and are not pass/fail evidence until the cookie implementation exists.

| ID | Scenario | Traceability | Preferred validation method | Planned evidence / expected result |
| --- | --- | --- | --- | --- |
| QA-P2-S1-001 | Successful backend registration with MVP fields; client cannot submit `processUserId`, operator ID, or delegated actor fields | [Product → QA: P2-S1] [Design → QA] | Web/API integration test or manual HTTP transcript | Account created by backend store; no trusted client actor override; response signs in immediately or follows documented login-next behavior. |
| QA-P2-S1-002 | Invalid/duplicate registration | [Product → QA: P2-S1] | Integration test plus response/log review | Required fields, duplicate identifier, and password policy failures are deterministic and do not reveal sensitive internals. |
| QA-P2-S1-003 | Password hashing and user-store validation | [Product → QA: P2-S1, P2-S5] [Design → QA] | Code/config inspection plus storage/log evidence in a non-production test store | Passwords are not stored or logged in plaintext; framework-supported hashing and account persistence are used. |
| QA-P2-S2-001 | Successful login issues ASP.NET Core auth cookie and establishes session identity | [Product → QA: P2-S2, P2-S4] | Integration test asserting `Set-Cookie` and follow-up `GET /api/session` | Valid credentials produce an HttpOnly auth cookie; session returns `identified` when mapped or `unmapped` when server mapping is absent. |
| QA-P2-S2-002 | Failed login with invalid credentials | [Product → QA: P2-S2] | Integration test/manual HTTP | Generic failure; no auth cookie issued; existing valid session behavior is explicitly defined; no account enumeration. |
| QA-P2-S3-001 | Logout clears backend identity and is safe while already logged out | [Product → QA: P2-S3] | Integration test with cookie jar | Sign-out clears/invalidates cookie; repeat logout is predictable; next `GET /api/session` returns `unidentified` unless another documented server identity source is active. |
| QA-P2-S4-001 | `/api/session` cookie integration preserves Phase 1 contract | [Product → QA: P2-S4] [Design → QA] | Integration test | Valid cookie principal is preferred; `200 OK`, `Cache-Control: no-store`, camelCase fields, non-empty `displayLabel`, and `identified`/`unmapped`/`unidentified` semantics remain. |
| QA-P2-S4-002 | Expired, tampered, or missing cookie | [Product → QA: P2-S4, P2-S5] | Integration test/manual HTTP where feasible | Invalid cookie does not create identity, does not return 500 for normal auth failures, and resolves to `unidentified` when no other principal exists. |
| QA-P2-S5-001 | Cookie security flags and lifetime policy | [Product → QA: P2-S5] [Design → QA] | Response header/config inspection in Development and target deployment config | `HttpOnly`; `Secure` outside local HTTP development; appropriate `SameSite`; lifetime/sliding/remember-me behavior documented; no sensitive cookie/ticket values logged. |
| QA-P2-S5-002 | CSRF and CORS strategy for cookie-backed state-changing endpoints | [Product → QA: P2-S5] [Design → QA] | Design review plus integration/manual negative tests when selected | Auth POSTs and logout follow documented anti-forgery/same-origin approach; MVP does not broaden CORS for credentialed cross-origin use. |
| QA-P2-S6-001 | Frontend coordination and handoff | [Product → QA: P2-S6] | Documentation review | Frontend-facing docs list register/login/logout/session contracts, session refresh timing after auth transitions, and no manual/delegated `ProcessUserID` fallback. |
| QA-P2-S7-001 | No authorization gate regressions | [Product → QA: P2-S7] [Design → QA] | Static scan plus route smoke/manual tests | Existing application/data routes, QueryHub behavior, and write commands are not newly blocked by `[Authorize]`, role gates, policy checks, or unauthenticated/unmapped command rejection. |
| QA-P2-S7-002 | Rate limiting, lockout, or explicit risk acceptance | [Product → QA: P2-S2, P2-S7] [Design → QA] | Design/config review and targeted tests if scoped | Before non-local exposure, login/register abuse controls are implemented or risk-accepted in Product/Governance. |
| QA-P2-S7-003 | Build and automated test evidence for P2 | [Product → QA: P2-S7] [Execution → QA] | `dotnet build` plus focused auth/session tests; full suite per release policy | Implementation compiles and targeted tests cover register/login/logout/session transitions; unrelated baseline failures are fixed or explicitly waived by release owner. |

Draft automation approach for Execution consideration:
- Add a lightweight ASP.NET Core Web/API integration harness, preferably `WebApplicationFactory`/`TestServer` with an isolated test user store and cookie container.
- Candidate tests: `Register_SignsInOrEnablesLogin_AndSessionReflectsUser`, `DuplicateRegistration_ReturnsValidationWithoutPlaintextSecrets`, `Login_SetsHttpOnlyCookie_ThenSessionIdentified`, `FailedLogin_DoesNotIssueCookie`, `Logout_ClearsCookie_ThenSessionUnidentified`, `CookieFlags_MatchConfiguredEnvironment`, `Session_WithUnmappedCookieUser_ReturnsUnmapped200`, and `ExistingRoutes_AreNotNewlyAuthorizationGated`.
- If a harness is not feasible for the first P2 release candidate, require documented manual HTTP evidence for every P2 auth transition and explicitly retain the automation gap in this plan.

## 3. Bug Reports & Status

No implementation-caused blocking defects were found in the Phase 1 session tracking slice.

No P2 cookie identity implementation bugs are reported because this is a planning/documentation update only. The following P2 planning risks should be resolved or explicitly accepted before release signoff.

| ID | Type | Status | Details | Required action |
| --- | --- | --- | --- | --- |
| QA-RISK-001 | Host identity capture | Accepted / monitor | Current launch settings and middleware do not populate Windows identity by default. Local HTTP validation correctly returned `unidentified`. | [QA → Governance] Accept for Phase 1. Loop to Research/Execution only if real Windows/Negotiate capture becomes a Phase 1 requirement. |
| QA-RISK-002 | Endpoint integration coverage | Closed for Phase 1 manual evidence | Execution recorded HTTP evidence for `GET http://127.0.0.1:5057/api/session`: `StatusCode=200`, JSON content type, `CacheControl=no-store`, camelCase body, and expected default `unidentified` state. | [QA → Execution] Future improvement: add lightweight automated endpoint harness to avoid manual evidence dependency. |
| QA-RISK-003 | Full suite failure | Waived for feature gate only | Full `dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release` previously failed due unrelated `ClientProfileRegistryTests.MatchesCorrectProfile` NARA expectation mismatch. Governance documented W-GOV-001 waiver. QA did not rerun the full suite. | [QA → Governance] Reaffirm waiver for this feature gate, or require Execution fix if final release policy is no-waiver/full-suite-green. |
| QA-DOC-001 | Vision artifact | Closed | `/docs/vision.md` now exists and includes problem statement, goals, stakeholders, scope, success criteria, assumptions, risks, and traceability links. | None for QA. |
| QA-DOC-002 | README/docs index completeness | Closed | README includes quick start/build/test/start guidance and links Vision, Product, Design, Execution, QA, Governance, and backend guide artifacts. | None for QA. |
| QA-DOC-003 | Governance traceability freshness | Closed for Phase 1 / P2 pending | `/docs/governance_traceability.md` records the Phase 1 conditional pass and closed loopback. It does not yet trace the new P2 cookie identity goal. | [QA → Governance] Add P2 cookie identity traceability during Governance current-goal review. |
| QA-RISK-004 | Config error response hardening | Accepted / monitor | Duplicate mapping errors throw as infrastructure/configuration exceptions. Behavior is acceptable for Phase 1 but controlled production problem details were not API-validated. | [QA → Execution] Optional hardening after Phase 1 or include in future endpoint integration coverage. |
| QA-P2-RISK-001 | Web/API harness gap | Open / plan before P2 QA | Current evidence shows no dedicated Web/API integration test project. Cookie auth is difficult to validate safely with resolver-only unit tests. | [QA → Execution] Add lightweight ASP.NET Core integration tests, or document manual HTTP evidence as a temporary release gap. |
| QA-P2-RISK-002 | User store and mapping ambiguity | Open / Design dependency | P2 success depends on a backend-owned account store, password hashing, and account-to-`processUserId` mapping rule. | [QA → Design] Finalize store, hashing, uniqueness, display label, and server-side mapping before Execution. |
| QA-P2-RISK-003 | Cookie/CSRF/CORS configuration | Open / Design dependency | Auth cookies introduce transport/session risks that Phase 1 did not have. | [QA → Design] Document `HttpOnly`, `Secure`, `SameSite`, lifetime, session fixation/logout behavior, anti-forgery, and no broadened credentialed CORS. |
| QA-P2-RISK-004 | Authorization scope creep | Open / monitor | Adding authentication middleware may accidentally add `[Authorize]`, QueryHub auth, role gates, or command blocking. | [QA → Execution] Include static scan and route smoke coverage proving existing app/data behavior remains tracking-only. |
| QA-P2-RISK-005 | Brute-force controls | Open / scope decision | Rate limiting/lockout is product-scoped as required or risk-accepted before non-local/internal exposure. | [QA → Product/Governance] Implement controls if scoped, or record risk acceptance. |
| QA-P2-RISK-006 | Frontend transition gap | Open / handoff dependency | Cookie auth requires frontend UX and session refresh timing, but frontend source is outside this repo. | [QA → Product/Design] Update frontend-facing docs and require handoff evidence; keep manual/delegated `ProcessUserID` fallback forbidden. |

## 4. Quality Metrics

| Metric | Value | Notes |
| --- | --- | --- |
| Focused new tests | 6 passed, 0 failed | QA rerun on 2026-06-30: `InteractionIdentityResolverTests` filter. |
| Resolver state coverage | Identified, unmapped, unidentified covered | Includes Windows account, UPN, no observed identity, duplicate mapping. |
| Manual HTTP evidence | Passed for default `unidentified` | Recorded in `docs/execution_log.md`: `200`, JSON, `Cache-Control: no-store`, camelCase body. |
| Static forbidden-auth scan | Pass by prior QA/Governance evidence | No Phase 1 evidence of `[Authorize]`, auth middleware, Negotiate, Challenge, Forbid, QueryHub auth, login, or command blocking. |
| Static actor-field audit | Pass by prior QA/Execution evidence | Current write DTOs do not contain trusted client actor fields. |
| Full build | Passed by Execution evidence | `dotnet build .\Totem.sln -c Release` passed per execution log. |
| Full test suite | Not proven green | Known unrelated failure remains waived for feature gate only unless release owner requires fix. |
| Endpoint automation | 0 automated HTTP tests | Manual evidence accepted for loopback closure; automation recommended for regression. |
| Documentation baseline | Current for QA | Required docs exist. README Vision link verified. Product/Design/Vision now describe P2 cookie identity; Governance remains Phase 1-gated and should add next-phase traceability when Governance performs its own update. |
| P2 acceptance criteria mapping | Planned | P2-S1 through P2-S7 are mapped to planned QA scenarios in this document; no P2 execution evidence exists yet. |
| P2 automated auth/session tests | 0 implemented | Planning recommendation only; add Web/API harness or document manual validation gap before P2 release signoff. |

## 5. Regression Testing Plan

[Execution → QA]

For each future change touching session tracking, Program startup, JSON serialization, Microfilm write DTOs, or authentication middleware:

1. Run `dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release --filter InteractionIdentityResolverTests`.
2. Run `dotnet build .\Totem.sln -c Release`.
3. If Governance/release policy requires full-suite green, run `dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release` after the unrelated ClientProfileRegistry issue is fixed or explicitly waived.
4. Manually or automatically validate `GET /api/session`:
   - no principal → 200 + `unidentified`
   - principal with mapping → 200 + `identified`
   - principal without mapping → 200 + `unmapped`
   - all states have non-empty `displayLabel`
   - no 401 or 403 for normal tracking states
   - `Cache-Control: no-store`
   - camelCase fields: `status`, `displayLabel`, `windowsAccount`, `userPrincipalName`, `processUserId`, `trackingSource`
5. Repeat static auth scan before release if any auth package/middleware is introduced.
6. If future write DTOs add actor-like fields, add tests proving controllers ignore or overwrite client-submitted actor values with server-resolved tracking context where durable stamping is supported.
7. Keep future roll-row audit/resource migration behind separate Product and Design approval with domain/topic/query tests.
8. Add a lightweight ASP.NET endpoint test harness when practical so route/header/serialization checks are automated rather than manually evidenced.

Additional P2 cookie identity regression gate when implementation begins:

1. Run `dotnet build .\Totem.sln -c Release`.
2. Run focused auth/session Web/API tests for registration, duplicate/invalid registration, login success/failure, logout, expired/tampered cookie handling, and `/api/session` after each transition.
3. Verify cookie flags and auth response headers for the configured environment: `HttpOnly`, `Secure` outside local HTTP development, appropriate `SameSite`, documented lifetime/sliding expiration, and no sensitive cookie values in logs.
4. Verify CSRF/anti-forgery behavior for cookie-backed POST/logout endpoints and confirm CORS is not broadened for credentialed cross-origin use unless Product/Design explicitly approve it.
5. Re-run a static auth scan and smoke existing application/data routes and QueryHub connection behavior to prove no authorization gate, role gate, policy, or command blocking regression was introduced.
6. Verify no request DTO accepts/trusts `ProcessUserID`, `userId`, operator ID, or equivalent delegated actor override for tracking-sensitive workflows.
7. If rate limiting/lockout is scoped, run targeted tests for lockout thresholds and recovery. If not scoped, require Product/Governance risk acceptance before non-local exposure.
8. Update frontend-facing docs with final endpoint shapes, session refresh timing, and local QA account setup before QA signoff.

## 6. Documentation Verification

| Artifact | Status | Notes |
| --- | --- | --- |
| `.gitignore` | Present | Repository hygiene baseline present. |
| `/src` | Present | Repository baseline present. |
| `/tests` | Present | `tests/Quantum.Tests` includes resolver coverage. |
| `/docs` | Present | Planning docs directory exists. |
| `/docs/vision.md` | Present / Pass | Includes Phase 1 baseline and next P2 backend-owned cookie login/register direction with QA handoff. |
| `/docs/product_backlog.md` | Present / Pass | P1 baseline and P2 cookie identity stories P2-S1 through P2-S7 include QA acceptance labels. |
| `/docs/design.md` | Present / Pass | Documents Phase 1 resolver design and next-phase cookie auth architecture/security considerations. |
| `/docs/execution_log.md` | Present / Pass | Includes build/test evidence, HTTP rerun evidence, technical debt, and process-stop note. |
| `/docs/qa_plan.md` | Present / Updated | This document preserves Phase 1 QA results and adds P2 cookie identity QA planning. |
| `/docs/governance_traceability.md` | Present / Phase 1 pass; P2 Governance update pending | Governance closed the Phase 1 loopback. It should add next-phase cookie identity traceability during its own current-goal review. |
| README/docs index | Pass | Quick start/build/test/start guidance exists and links Vision, Product, Design, Execution, QA, Governance, and backend guide artifacts. |
| Frontend-facing contract docs | Present / Phase 1 pass; P2 update pending | `backend-integration-guide.md` covers Phase 1; register/login/logout endpoint shapes and frontend QA setup should be added after Design finalizes P2 contracts. |

## 7. Acceptance Criteria Coverage Summary

[Product → QA]

- Tracking-only, not authorization: Pass.
- No permission checks, `[Authorize]`, QueryHub auth, login screens, or command blocking: Pass by inspection/static evidence.
- No Windows identity/Formatic mapping as permission proof: Pass.
- No manual/delegated `ProcessUserID` override support: Pass by scope and DTO audit.
- Explicit tracking state when identity unavailable; no demo/manual fallback: Pass by resolver tests and HTTP evidence.
- `GET /api/session` same-origin endpoint returning 200 for normal tracking states: Pass for default HTTP `unidentified`; resolver coverage passes for `identified` and `unmapped`.
- Response status enum `identified`, `unmapped`, `unidentified`: Pass.
- Required response fields, camelCase serialization, non-empty `displayLabel`, and `Cache-Control: no-store`: Pass for default HTTP `unidentified`; resolver and DTO coverage pass for all states.
- `unmapped` account fields may exist and `processUserId` null: Pass by resolver tests.
- `unidentified` account fields null, `processUserId` null, `trackingSource` `none`, neutral label: Pass by resolver tests and HTTP evidence.
- Mapping configuration-backed and normalized: Pass.
- Current write DTOs audited; actor fields not trusted/used: Pass.
- Future roll-row audit/resource migration not implemented in Phase 1: Pass.
- Frontend-facing docs updated with final contract: Pass.
- Product Vision documentation gap: Closed.
- HTTP validation gap: Closed for the required default `unidentified` route/header/serialization evidence.
- Full focused validation: Pass. Full suite remains waived/conditional due unrelated failure.

### P2 cookie identity planning coverage (not execution evidence)

[Product → QA]

- P2-S1 registration: Planned coverage for successful, invalid, duplicate, minimal-field, no-client-`processUserId`, password hashing, user-store, and response semantics.
- P2-S2 login: Planned coverage for success cookie issuance, generic failure, no client actor override, and rate limiting/lockout or risk acceptance.
- P2-S3 logout: Planned coverage for cookie clearing/invalidation, idempotent logged-out behavior, post-logout `/api/session`, and CSRF consistency.
- P2-S4 `/api/session` cookie integration: Planned coverage for cookie-principal preference, backward-compatible fields, `identified`/`unmapped`/`unidentified`, `Cache-Control: no-store`, expired/tampered cookies, and no 401/403 for normal tracking states.
- P2-S5 cookie security: Planned coverage for `HttpOnly`, `Secure`, `SameSite`, lifetime/sliding expiration, no credentialed CORS broadening, anti-forgery strategy, and no sensitive auth logging.
- P2-S6 frontend handoff: Planned coverage for final endpoint documentation, session refresh timing after register/login/logout, and no manual/delegated `ProcessUserID` fallback.
- P2-S7 validation/no authorization expansion: Planned coverage for build/test evidence, auth/session automated or manual evidence, existing-route/QueryHub no-gate regression checks, and domain tests only if actor metadata enters commands/events/topics/queries.

## 8. Recommendation

[QA → Governance]

Phase 1 backend session tracking remains conditionally passed for the scoped feature/stage gate. The Phase 1 QA results above are preserved and are not reclassified as failed or outdated.

[QA → Product/Design/Execution]

QA documentation is current for the next backend-owned ASP.NET Core cookie login/register planning goal. Before P2 execution/release signoff, QA expects Product/Design/Execution evidence for:

1. Final register/login/logout endpoint shapes and response DTOs.
2. Backend-owned account store, password hashing, uniqueness, display label, and server-side `processUserId` mapping rules.
3. Cookie options and session behavior: `HttpOnly`, `Secure`, `SameSite`, lifetime/sliding expiration, logout invalidation, and no sensitive auth logging.
4. CSRF/anti-forgery and CORS decisions for same-origin cookie-backed POST/logout endpoints.
5. Frontend handoff for session refresh after auth transitions and no manual/delegated identity override.
6. Rate limiting/lockout implementation or explicit Product/Governance risk acceptance before non-local exposure.
7. Regression evidence that authentication did not become authorization: no new existing-route `[Authorize]` gates, QueryHub auth, role/policy gates, or command blocking by default.
8. A Web/API integration test harness where practical; otherwise complete manual HTTP evidence plus an explicit automation gap.

No tests were run and no implementation changes were made for this document-only QA update. No QA-requested code rework is required for the completed Phase 1 tracking slice based on the current evidence.
