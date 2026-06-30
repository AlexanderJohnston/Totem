# Vision: Quantum Web / Formatic Backend Identity & Cookie Session Tracking

Last updated: 2026-06-30
Owner: Vision
Lifecycle: Vision → Product → Design → Execution → QA → Governance
Work classification: New identity/tracking initiative
Phase status: Phase 1 passive backend session tracking is the completed baseline; the next initiative is backend-owned ASP.NET Core cookie login/register.

Source inputs:
- User decision on 2026-06-30: replace the next Windows/Negotiate direction with backend-owned ASP.NET Core cookie login/register.
- Original plan: `C:\Users\ajohnston\.copilot\session-state\f0fd027a-2db0-4e34-bfa0-ffa121ee6bc8\plan.md`
- `docs/product_backlog.md`
- `docs/design.md`
- `docs/qa_plan.md`
- `docs/governance_traceability.md`
- ASP.NET Core cookie authentication and Identity documentation should be used by Product/Design for implementation validation. [Research → Vision] [Vision → Research: https://learn.microsoft.com/aspnet/core/security/authentication/cookie]

## Problem Statement

Quantum Web / Formatic needs a reliable backend-owned way to know who is using the application for tracking-sensitive workflows. The completed Phase 1 baseline passively reads the server-observed `HttpContext.User` and exposes `identified`, `unmapped`, or `unidentified` from `GET /api/session`; that remains valuable as the current tracking contract, but it is not reliable enough when hosting does not provide a Windows/Negotiate principal.

The problem is now reframed from passive identity observation to explicit backend authentication: users should be able to register, log in, and log out through ASP.NET Core cookie authentication so the backend can establish the interaction identity it reports. The cookie says who the backend believes the current browser session belongs to; it does not automatically prove permission to perform domain actions, does not introduce role or policy authorization, and does not allow users to submit work as another operator.

This vision updates the next-phase direction while preserving the earlier tracking-only boundary: identity is for consistent actor tracking and UI context first, not access control, unless Product later explicitly scopes authorization as a separate feature. [Vision → Product]

## Project Vision & Goals

### Vision statements

- V1: Keep Phase 1 passive backend session tracking as the completed baseline and compatibility contract. [Vision → Product]
- V2: Add a backend-owned ASP.NET Core cookie login/register flow so identity does not depend on Windows/Negotiate being available. [Vision → Product]
- V3: Report the backend-authenticated user through the session contract in a way the UI can display and QA can validate. [Vision → Product]
- V4: Preserve the tracking-versus-authorization distinction: cookie authentication establishes identity, but does not add permission checks, QueryHub authorization, command blocking, or role gates by itself. [Vision → Product]
- V5: Preserve the no manual/delegated `ProcessUserID` override decision for tracking-sensitive workflows. [Vision → Product]
- V6: Maintain SDLC traceability and require Product, Design, QA, and Governance to check whether their docs need updates for this changed goal. [Vision → Product]

### Completed baseline: Phase 1 passive tracking

Phase 1 remains the accepted baseline:

1. `GET /api/session` exposes the current backend-observed tracking state. [Vision → Product: V1]
2. Normal tracking states remain `identified`, `unmapped`, and `unidentified`, returned as `200 OK` by the session endpoint. [Vision → Product: V1]
3. The stable JSON contract includes `status`, `displayLabel`, `windowsAccount`, `userPrincipalName`, `processUserId`, and `trackingSource`. [Vision → Product: V1]
4. Missing or unmapped identity is a tracking state, not an authorization failure. [Vision → Product: V4]
5. Client-provided actor identity must not become a delegated/manual override for real tracking-sensitive workflows. [Vision → Product: V5]

### Next initiative goals: backend-owned cookie login/register

1. Provide same-origin backend endpoints for register, login, logout, and current session inspection. [Vision → Product: V2, V3]
2. Use ASP.NET Core cookie authentication to issue, validate, renew as appropriate, and clear an HTTP cookie owned by the backend. [Vision → Product: V2] [Research → Vision]
3. Ensure `GET /api/session` reflects the cookie-authenticated principal when present, while still returning explicit `unidentified` or `unmapped` tracking states when appropriate. [Vision → Product: V1, V3]
4. Establish any `processUserId` association server-side through registration, controlled configuration, or another Product-approved mapping; do not accept arbitrary client-selected operator IDs. [Vision → Product: V5]
5. Keep authorization and permission gating out of this initiative unless Product explicitly creates a separate authorization scope. [Vision → Product: V4]
6. Document security expectations for credential handling, cookie settings, CSRF/session fixation protections, and local development configuration before execution. [Vision → Product] [Vision → Research]
7. Trigger Product, Design, QA, and Governance review of their own docs because the next goal changed from Windows/Negotiate reliance to backend-owned cookie login/register. [Vision → Product]

## User Personas & Scenarios

### Personas

- Application operator: needs to use Quantum Web and have work attributed to the backend-recognized identity without manually choosing another operator.
- New or returning user: needs a clear way to register, log in, and log out when Windows/Negotiate identity is not available or not desired.
- Frontend integrator: needs stable session semantics and display labels so the UI can show who the backend believes is active.
- Backend maintainer: needs a small, host-owned authentication and session model that does not leak into domain authorization decisions prematurely.
- QA: needs deterministic scenarios for anonymous, registered, logged-in, invalid-login, logout, unmapped, and identified states.
- Governance/Product stakeholders: need traceability that the direction changed and that authorization remains deliberately out of scope.

### Representative scenarios

1. Anonymous visit: a browser with no valid auth cookie calls `GET /api/session` and receives `unidentified` with a neutral non-empty display label.
2. Registration: a new user submits registration details; the backend creates the account/profile using approved validation and safe credential storage, then either signs the user in or returns a clear next step as Product defines.
3. Login: an existing user submits valid credentials; the backend issues its auth cookie and subsequent `GET /api/session` identifies the user according to server-owned account and process-user mapping rules.
4. Invalid login: wrong credentials do not create or preserve an authenticated session and do not reveal sensitive account details.
5. Logout: the backend clears the auth cookie; subsequent `GET /api/session` returns `unidentified` unless another valid backend-observed identity exists by design.
6. Unmapped authenticated user: a cookie-authenticated account without a process-user association is represented as `unmapped`, not rejected as forbidden.
7. Tracking-sensitive write: if a future/current payload includes `ProcessUserID`, `userId`, or an equivalent actor field, the backend ignores or overwrites it with the resolved session actor rather than allowing delegated/manual override.

## Prioritized High-Level Requirements

1. P0 - preserve Phase 1 `/api/session` compatibility and tracking-only semantics. [Vision → Product]
2. P0 - implement backend-owned register/login/logout flow using ASP.NET Core cookie authentication. [Vision → Product]
3. P0 - prevent client-selected `ProcessUserID` or equivalent actor override in tracking-sensitive workflows. [Vision → Product]
4. P1 - define server-owned mapping from registered account to `processUserId` and display metadata. [Vision → Product]
5. P1 - add security controls appropriate for credential and cookie handling, including password hashing, safe cookie settings, anti-forgery/CSRF strategy, and logout/session invalidation behavior. [Vision → Product] [Vision → Research]
6. P1 - update Product, Design, QA, and Governance docs to align with the cookie login/register direction. [Vision → Product]
7. P2 - evaluate whether Windows/Negotiate remains useful only as an optional identity source or migration bridge, not as the next primary goal. [Vision → Product]

## Success Criteria

### Baseline success remains true when

1. `GET /api/session` remains callable from the Quantum Web origin and returns `200 OK` for normal tracking states. [Vision → Product: V1] [Product → QA]
2. Existing response fields remain camelCase and preserve current semantics for `identified`, `unmapped`, and `unidentified`. [Vision → Product: V1] [Product → QA]
3. No baseline behavior treats missing identity or missing mapping as an authorization denial. [Vision → Product: V4] [Product → Design]

### Cookie login/register initiative succeeds when

1. Users can register, log in, and log out through backend-owned same-origin endpoints without depending on Windows/Negotiate. [Vision → Product: V2] [Product → QA]
2. A successful login results in a valid ASP.NET Core auth cookie and `GET /api/session` reflects the backend-authenticated user. [Vision → Product: V2, V3] [Product → QA]
3. Invalid credentials, expired cookies, and logout states are deterministic and do not create an authenticated tracking identity. [Vision → Product: V2] [Product → QA]
4. Authenticated-but-unmapped users are represented explicitly as a tracking data-quality state unless Product later chooses a different registration/mapping rule. [Vision → Product: V3, V4]
5. No implementation in this initiative adds permission checks, role gates, QueryHub authorization, or command blocking by default. [Vision → Product: V4] [Product → Design]
6. Tracking-sensitive writes do not trust client-chosen `ProcessUserID`, `userId`, operator ID, or equivalent actor fields. [Vision → Product: V5] [Product → Design]
7. Product, Design, QA, and Governance artifacts are reviewed and updated or explicitly marked current for the changed goal. [Vision → Product: V6]

## Scope & Constraints

### In scope

- Preserve the completed Phase 1 session endpoint and its tracking statuses.
- Backend-owned user registration and login/logout using ASP.NET Core cookie authentication.
- Backend session inspection that reports the auth-cookie principal through the existing tracking model.
- Server-owned account-to-process-user/display mapping decision, including explicit `unmapped` handling where needed.
- Security requirements and acceptance criteria for credentials and cookies at the Product/Design/QA level.
- Documentation and cross-agent handoff for the changed direction.

### Out of scope unless separately scoped

- Relying on Windows/Negotiate as the next primary identity goal.
- Authorization policy design, role/permission checks, `[Authorize]` gates for domain commands, QueryHub authorization, or command blocking.
- Manual/delegated process-user selection for tracking-sensitive workflows.
- External identity providers, SSO/OIDC, password reset email flows, MFA, admin account-management screens, and account approval workflows unless Product adds them.
- Durable roll-row audit/resource migration and per-cell audit projections, which remain future Product/Design work.
- Frontend source changes in this repository; frontend integration remains a handoff unless a frontend repo is brought into scope.

### Constraints

- Cookie authentication is authentication/tracking infrastructure, not proof of domain permission.
- Any real password or secret material must not be committed to source control.
- Production deployments should use HTTPS and secure cookie settings; local development exceptions must be deliberate and documented.
- Existing domain operator concepts may not equal registered web accounts and need a Product/Design mapping decision.

## Key Assumptions & Risks

### Assumptions

- The backend can persist registered user/account data or can introduce a suitable store as part of the next phase.
- Product will decide whether registration automatically creates a process-user mapping or creates an authenticated-but-unmapped account pending configuration/approval.
- Frontend calls remain same-origin or otherwise compatible with ASP.NET Core cookie behavior and any CSRF protections selected by Design.
- Phase 1 tests and evidence remain valid as baseline evidence, but new tests/evidence are required for cookie login/register.
- Authorization remains out of scope until Product explicitly creates an authorization initiative.

### Risks and mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Cookie login is mistaken for permission gating. | Scope creep and unsafe product semantics. | Repeat that cookies establish backend-recognized identity only; keep authorization out of Product/Design/QA acceptance unless separately scoped. |
| Credential handling is under-designed. | Security exposure through weak storage, enumeration, or missing lockout/rate controls. | Use ASP.NET Core Identity or equivalent proven primitives; require password hashing, validation, and safe error messages in Design/QA. [Research → Vision] |
| Cookie/session security is incomplete. | CSRF, session fixation, or insecure cookie transport. | Define cookie flags, HTTPS expectations, sign-in regeneration, logout clearing, and CSRF/anti-forgery strategy before execution. |
| Registered account to `processUserId` mapping is unclear. | Incorrect actor attribution or too many `unmapped` states. | Product/Design must choose server-owned mapping rules and keep client override forbidden. |
| Registration creates duplicate or unapproved identities. | Data quality and support burden. | Product should define uniqueness, display-name, approval, and recovery expectations before broad rollout. |
| Existing Windows/Negotiate baseline docs conflict with new direction. | Agents implement or test the wrong next goal. | Cross-agent docs review is required; mark Phase 1 as completed baseline and cookie login/register as the next initiative. |
| Frontend integration assumes manual operator selection remains available. | Spoofed or delegated tracking identity. | Frontend handoff must remove manual/delegated `ProcessUserID` fallback for real tracking-sensitive workflows. |

## Cross-Agent Handoff

- [Vision → Product] Product should update `docs/product_backlog.md` to make backend-owned ASP.NET Core cookie login/register the next identity goal and to confirm what registration captures, how account-to-process-user mapping works, and whether any authorization remains out of scope.
- [Vision → Design] Design should update `docs/design.md` for cookie auth architecture, endpoint shapes, credential storage, cookie settings, CSRF/session protection, and interaction with the existing session resolver.
- [Vision → QA] QA should update `docs/qa_plan.md` with registration, login, logout, invalid credential, expired cookie, anonymous, identified, and unmapped scenarios.
- [Vision → Governance] Governance should update `docs/governance_traceability.md` to trace the changed direction and distinguish completed Phase 1 baseline evidence from next-phase evidence.

## Traceability

| Vision item | Product backlog link | Design link | QA/Governance link |
| --- | --- | --- | --- |
| V1 completed passive session baseline | Existing Phase 1 backlog items in `docs/product_backlog.md` [Vision → Product] | Existing session resolver/controller design in `docs/design.md` [Product → Design] | Existing Phase 1 QA/Governance evidence [Product → QA] |
| V2 backend-owned cookie login/register | Product backlog update required [Vision → Product] | Cookie auth design update required [Product → Design] | Login/register/logout QA scenarios required [Product → QA] |
| V3 session reflects authenticated cookie principal | Product should define response compatibility and mapping semantics [Vision → Product] | Design should integrate cookie principal with session resolver [Product → Design] | QA should validate session after login/logout and unmapped account states [Product → QA] |
| V4 tracking without authorization | Product should keep authz out of scope unless separately approved [Vision → Product] | Design should avoid permission policies/command blocking by default [Product → Design] | Governance should verify no authorization scope creep |
| V5 no manual/delegated process-user override | Product should keep override forbidden for tracking-sensitive workflows [Vision → Product] | Design should overwrite/ignore client actor fields where applicable [Product → Design] | QA should include spoofed actor-field checks when write DTOs expose such fields |
| V6 cross-agent documentation currency | Product backlog update/check required [Vision → Product] | Design update/check required [Product → Design] | QA and Governance update/check required [Product → QA] |

## Product Decisions

- PD-VIS-001: Phase 1 passive backend session tracking remains the completed baseline.
- PD-VIS-002: The next identity goal is backend-owned ASP.NET Core cookie login/register, not deeper reliance on Windows/Negotiate.
- PD-VIS-003: Cookie authentication establishes who the backend believes the current browser session belongs to; it does not automatically create authorization, permission checks, QueryHub auth, or command blocking.
- PD-VIS-004: `identified`, `unmapped`, and `unidentified` remain tracking states; missing identity or mapping is not automatically forbidden.
- PD-VIS-005: Manual/delegated `ProcessUserID` override remains out of scope for tracking-sensitive workflows.
- PD-VIS-006: Product, Design, QA, and Governance agents should check and update their own docs for the changed goal.
