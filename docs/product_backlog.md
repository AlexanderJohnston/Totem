# Product Backlog

Last updated: 2026-06-30

Source inputs:
- `C:\Users\ajohnston\.copilot\session-state\f0fd027a-2db0-4e34-bfa0-ffa121ee6bc8\plan.md`
- `docs/FORMATIC_WINDOWS_AUTH_IMPLEMENTATION_HANDOFF.md`
- `backend-integration-guide.md`
- Stakeholder decision on 2026-06-30: make backend-owned ASP.NET Core cookie login/register the next identity goal.

## Product stance and MVP

This backlog adapts the frontend handoff for Quantum Web / Formatic in this Totem repository. The completed baseline is backend session tracking via `GET /api/session` and passive `HttpContext.User` identity resolution. The next MVP is backend-owned ASP.NET Core cookie identity: users can register, log in, log out, and have `/api/session` report a reliable server-owned tracking identity.

The next initiative is still a tracking and identity reliability slice, not an authorization or permission system. Cookie authentication establishes who the backend believes the browser is; it does not, by default, decide what that user may access or change.

### Scope guardrails

- In scope for the next identity initiative: backend-owned register, login, logout, secure auth cookie issuance/clearing, and `/api/session` integration.
- Tracking only by default: do not add permission checks, role/claim policy gates, QueryHub auth, access-denied UI, or command blocking unless Product explicitly re-scopes authorization later.
- `[Authorize]` should not be added to existing application/data routes by default; if Design needs it for auth-management endpoints, it must not change existing write/read access semantics without Product approval.
- Do not treat Windows identity, cookie identity, user principal names, email addresses, or Formatic operator mapping as proof of permission.
- Do not allow manual, delegated, or client-supplied override of tracking-sensitive `ProcessUserID` values.
- If identity is unavailable, return an explicit tracking state rather than falling back to demo or manual identity.
- Keep roll-scoped row resources and per-cell audit projections out of the cookie identity initiative unless explicitly re-scoped.
- Frontend source is outside this repository; this repo owns backend endpoints, backend behavior, and frontend-facing contract documentation.

### Vision statements

- V1: Quantum Web exposes who the server observes for the current browser interaction so the UI can present reliable tracking context. [Vision → Product]
- V2: Tracking identity is resolved server-side and carried consistently where current command/event surfaces support it, without creating an authorization boundary. [Vision → Product]
- V3: Current frontend integration remains stable while backend data quality improves through explicit `identified`, `unmapped`, and `unidentified` states. [Vision → Product]
- V4: Longer term auditability moves toward roll-scoped row resources and targeted per-column change metadata, but only after separate Product and Design work. [Vision → Product]
- V5: Repository hygiene and SDLC traceability remain visible through docs, tests, and execution logs. [Vision → Product]
- V6: Backend-owned ASP.NET Core cookie login/register provides a reliable first-party identity for tracking without delegating `ProcessUserID` selection to the frontend. [Vision → Product]

## Current lifecycle status and readiness summary

Phase 1 backend session tracking is the baseline and is treated as complete/conditionally passed for Product planning purposes:

1. `GET /api/session` exists as the stable same-origin tracking endpoint.
2. Passive request-principal identity resolution is available from `HttpContext.User` when the host supplies a principal.
3. Missing, unmapped, or unidentified identity remains a tracking data quality state and does not reject writes.
4. Manual/delegated `ProcessUserID` override remains unsupported.

The next Product goal is Epic P2: backend-owned ASP.NET Core cookie identity. Product does not require another scope loop before Design/Execution if the following decisions remain accepted:

1. The backend owns account registration, credential validation, cookie issuance, cookie clearing, and session reporting.
2. The MVP may use framework-supported ASP.NET Core cookie authentication and password hashing/user storage selected by Design.
3. `GET /api/session` remains the canonical frontend-facing session contract and integrates cookie identity as the preferred tracking source when a valid auth cookie exists.
4. Existing application/data routes are not converted into an authorization system by default.
5. Authentication-management failures may use appropriate auth semantics, such as generic failed-login responses, but unmapped/unidentified session states remain normal tracking states.

Loop back to Product if stakeholders request authorization rules, account roles/permissions, admin user management, password reset/email verification, external identity providers, or command rejection for unauthenticated/unmapped users in this initiative.

## Product Epics, Features, and User Stories

### Epic FND: Repository Scaffolding and Hygiene

Purpose: Keep the repository aligned with Governance Definition of Done gates and make product/design/QA traceability discoverable. [Vision → Product: V5]

Status observed during planning: `.gitignore`, `src/`, `tests/`, and `docs/` exist. `docs/product_backlog.md` is maintained by Product. `docs/execution_log.md` status should be checked by Execution/Governance before release evidence is required.

#### Feature FND-1: SDLC documentation baseline

Priority: Should-have for this initiative; Must-have if Governance DoD gates are enforced before release.

##### Story FND-S1: Maintain repository scaffolding and docs skeleton

As the delivery team, I need standard repo folders and SDLC documents so Product, Design, Execution, and QA can trace decisions.

Acceptance Criteria:
- Language-appropriate `.gitignore` is present.
- `src/`, `tests/`, and `docs/` folders exist.
- `/docs/product_backlog.md` exists and captures scope, priorities, dependencies, blockers, and acceptance criteria. [Product → QA]
- README or equivalent docs index links to key `/docs/*` planning artifacts before release documentation signoff. [Product → Design]
- A smoke test or documented build command can run locally before code changes are accepted. [Product → QA]
- `/docs/execution_log.md` exists or an equivalent execution log convention is documented before execution evidence is required. [Product → QA]

### Epic P1: Backend Session Tracking and Server-Observed Actor Context Baseline

Purpose: Preserve the completed Phase 1 tracking contract as the baseline for later authentication work. [Vision → Product: V1, V2, V3]

Status: Complete/conditionally passed. Do not re-scope this passive tracking baseline into the cookie authentication end state.

MVP outcome delivered: The frontend can call `GET /api/session`, display a non-empty session label for all normal states, and stop relying on manual/demo identity fallback for tracking-sensitive workflows.

#### Feature P1-1: Same-origin session tracking endpoint

Priority: Completed baseline; maintain compatibility.

##### Story P1-S1: Return the current tracking session

As a frontend integrator, I need a stable `GET /api/session` endpoint so the UI can show who Quantum Web thinks it is interacting with.

Acceptance Criteria:
- `GET /api/session` is available from the same origin as Quantum Web.
- For normal states, the endpoint returns `200 OK` and a JSON body; it does not return `401` or `403` for unauthenticated, unmapped, or unidentified tracking states. [Product → QA]
- Response fields use camelCase and include: `status`, `displayLabel`, `windowsAccount`, `userPrincipalName`, `processUserId`, and `trackingSource`.
- `status` is one of `identified`, `unmapped`, or `unidentified`.
- `displayLabel` is server-provided and non-empty for every status.
- For `identified`, `processUserId` is populated when a configured mapping exists, and observed account fields may be populated.
- For `unmapped`, `processUserId` is `null`, observed account fields may be populated, and the response communicates a tracking data quality state rather than an access failure.
- For `unidentified`, `displayLabel` is a neutral value such as `Unidentified user`, observed account fields are `null` when unavailable, `processUserId` is `null`, and `trackingSource` is `none`.
- `5xx` responses are reserved for infrastructure or configuration failures and do not expose stack traces or sensitive auth internals. [Product → QA]
- The endpoint does not add authorization policy language or QueryHub requirements. [Product → Design]

#### Feature P1-2: Request identity resolution and mapping

Priority: Completed baseline; extend only as needed for P2 cookie identity.

##### Story P1-S2: Resolve server-observed identity from the request

As the backend, I need to resolve identity from the ASP.NET request principal so tracking context is observed by the server rather than chosen by the client.

Acceptance Criteria:
- Identity resolution lives in `Outermind.Web` as host/request infrastructure, not as a domain permission rule. [Product → Design]
- The resolver normalizes available principal data into canonical account fields, including `windowsAccount` when available and `userPrincipalName` when available.
- The resolver maps account keys to `processUserId` through backend-controlled configuration or storage.
- Mapping affects tracking metadata only and never permits or denies access.
- If no principal is available, the resolver returns an `unidentified` tracking result.
- If a principal is available but no mapping exists, the resolver returns an `unmapped` tracking result.
- Local validation can be supported through user secrets, environment variables, development seed data, or another configuration source selected by Design; machine-specific secrets are not committed. [Product → QA]

#### Feature P1-3: Server-side actor stamping preparation for current writes

Priority: Completed baseline guardrail; maintain for P2.

##### Story P1-S3: Audit and guard current write payloads against client-chosen identity

As Product, I need current Formatic/Microfilm writes to avoid spoofed or delegated tracking identity while not blocking work due to tracking state.

Acceptance Criteria:
- The implementation audits current write endpoints identified in the plan: regular row cell patch, custom row cell patch, regular row create, custom row create, column update, client profile create/update, and WASP import force. [Product → QA]
- Current Miller table payloads are treated as having no `ProcessUserID`, `userId`, operator ID, or equivalent actor fields unless Design documents otherwise.
- If current or near-term DTOs include client-submitted identity fields, controllers ignore or overwrite those values with server-resolved tracking identity for tracking-sensitive workflows. [Product → Design]
- Writes are not rejected solely because identity is `unmapped` or `unidentified` in the baseline tracking slice.
- Actor metadata is stamped or prepared only where current command/event surfaces support it without speculative broad domain migration.
- Any decision to add durable actor metadata to shared `Outermind/Microfilm` commands or events is documented as a Design decision and covered by domain tests. [Product → Design] [Product → QA]

### Epic P2: Backend-Owned ASP.NET Core Cookie Identity for Tracking

Purpose: Make backend-owned cookie authentication the next identity goal so browser sessions have reliable first-party tracking identity without introducing permission gating. [Vision → Product: V1, V2, V3, V6]

MVP outcome: A user can register, log in, log out, and refresh `/api/session` to see the backend-owned cookie identity reflected as the current tracking actor. Existing application/data behavior remains ungated by authorization unless later scoped.

#### Feature P2-1: Backend account registration

Priority: Must-have next initiative.

##### Story P2-S1: Register a backend-owned tracking account

As a user, I need to create an account owned by the backend so future interactions can be associated with a reliable server-controlled identity.

Acceptance Criteria:
- A backend registration endpoint exists, using a route shape selected by Design, for example `POST /api/auth/register`. [Product → Design]
- Registration accepts only the minimum fields required for MVP identity, such as username/email, display name if needed, and password; it does not accept `processUserId` from the client. [Product → Design]
- The backend validates required fields, duplicate account identifiers, and password policy using framework-supported mechanisms where practical. [Product → QA]
- Passwords are never stored or logged in plaintext; password hashing and account persistence use ASP.NET Core-compatible security practices selected by Design. [Product → QA]
- On successful MVP registration, the backend either signs the user in immediately and returns the current session DTO, or returns a documented response that lets the frontend perform login next; Product preference is immediate sign-in unless Design identifies a blocker. [Product → Design]
- The created account is mapped to tracking identity server-side so `/api/session` can return `identified` with a backend-derived `processUserId` when mapping exists. [Product → QA]
- Registration creates no roles, permissions, or access grants beyond establishing identity for tracking.
- Duplicate/invalid registration responses do not reveal sensitive account internals beyond normal validation messages. [Product → QA]

#### Feature P2-2: Cookie login

Priority: Must-have next initiative.

##### Story P2-S2: Log in with backend credentials and receive an auth cookie

As a registered user, I need to log in so the backend can issue a secure cookie and resolve my tracking identity on later requests.

Acceptance Criteria:
- A backend login endpoint exists, using a route shape selected by Design, for example `POST /api/auth/login`. [Product → Design]
- Login validates credentials server-side and, on success, issues an ASP.NET Core authentication cookie tied to the backend account.
- Login success returns the current session DTO or enough data for the frontend to immediately call `GET /api/session`; Product preference is to return the session DTO for a simple frontend handoff. [Product → Design]
- Failed login uses a generic failure response and does not reveal whether the account identifier or password was incorrect. [Product → QA]
- Login does not accept, trust, or derive `ProcessUserID` from client input.
- Login does not grant permissions, roles, or access to protected resources unless a future Product authorization initiative defines those rules.
- A rate limiting, lockout, or documented risk-acceptance strategy is defined before the feature is exposed outside local/internal use. [Product → Design] [Product → QA]

#### Feature P2-3: Logout and cookie clearing

Priority: Must-have next initiative.

##### Story P2-S3: Log out and clear backend identity

As a logged-in user, I need to log out so the browser no longer presents the backend-owned identity for tracking.

Acceptance Criteria:
- A backend logout endpoint exists, using a route shape selected by Design, for example `POST /api/auth/logout`. [Product → Design]
- Logout clears or invalidates the ASP.NET Core authentication cookie using framework-supported sign-out behavior.
- Logout is safe to call when already logged out and returns a predictable success response without exposing auth internals. [Product → QA]
- After logout, `GET /api/session` returns `unidentified` unless another valid server-observed identity source is intentionally active and documented. [Product → QA]
- Logout does not require permission checks or roles.
- CSRF handling for logout is addressed consistently with the selected cookie-auth and same-origin API strategy. [Product → Design] [Product → QA]

#### Feature P2-4: `/api/session` integration with cookie identity

Priority: Must-have next initiative.

##### Story P2-S4: Report cookie-authenticated users through the existing session contract

As a frontend integrator, I need `/api/session` to reflect the backend-owned cookie identity so the UI has one canonical way to understand tracking state.

Acceptance Criteria:
- `GET /api/session` remains the canonical session/tracking endpoint; the frontend does not need a second endpoint to determine current identity.
- When a valid backend auth cookie is present, the resolver treats the cookie principal/account as the preferred tracking source unless Design documents a higher-priority server identity source. [Product → Design]
- The Phase 1 response contract remains backward compatible: `status`, `displayLabel`, `windowsAccount`, `userPrincipalName`, `processUserId`, and `trackingSource` remain present. [Product → QA]
- For cookie-authenticated users with a server mapping, `/api/session` returns `identified`, a non-empty `displayLabel`, a backend-derived `processUserId`, and a `trackingSource` value that clearly indicates ASP.NET Core cookie identity or equivalent backend-owned auth.
- For a valid cookie account without a mapping, `/api/session` returns `unmapped` rather than treating the user as authorized or unauthorized.
- For no valid cookie and no other observed principal, `/api/session` returns `unidentified` with `200 OK`.
- `windowsAccount` may be `null` for cookie-authenticated users; `userPrincipalName` may carry the backend account identifier only if Design confirms that field remains semantically safe. [Product → Design]
- Additive fields, such as backend account ID or auth method, require Design documentation and QA coverage but must not break existing frontend consumers. [Product → Design] [Product → QA]

#### Feature P2-5: Cookie security and same-origin safeguards

Priority: Must-have next initiative.

##### Story P2-S5: Configure authentication cookies safely for this product context

As Product and QA, we need auth cookies configured with secure defaults so backend-owned identity is reliable and not casually exposed.

Acceptance Criteria:
- Authentication cookies are `HttpOnly` so frontend JavaScript does not read credential material. [Product → QA]
- Cookies use `Secure` for HTTPS/non-development deployments; any local-development exception is documented and not carried into production configuration. [Product → QA]
- Cookies use an appropriate `SameSite` mode for a same-origin app, with no broad cross-site behavior unless Product and Design explicitly approve it. [Product → Design]
- Cookie lifetime, sliding expiration, and optional remember-me behavior are defined by Design; remember-me is out of MVP unless explicitly scoped. [Product → Design]
- Auth endpoints do not broaden CORS policy for credentialed cross-origin use as part of this MVP.
- CSRF/anti-forgery expectations for cookie-backed state-changing endpoints are documented and covered by QA evidence appropriate to the selected ASP.NET Core approach. [Product → QA]
- Logs and error responses do not expose passwords, password hashes, auth tickets, cookie values, or sensitive claim contents. [Product → QA]

#### Feature P2-6: Frontend coordination and handoff

Priority: Must-have next initiative.

##### Story P2-S6: Coordinate frontend use of backend-owned identity

As a frontend integrator, I need clear backend contracts so the frontend can add register/login/logout UX without reintroducing manual identity selection.

Acceptance Criteria:
- Frontend-facing docs list register, login, logout, and session endpoint shapes once Design finalizes them. [Product → Design]
- Frontend calls `GET /api/session` at app boot or page entry and after register/login/logout transitions; exact timing is agreed with the frontend team. [Product → Design]
- Frontend does not send `ProcessUserID`, operator IDs, or delegated identity fields for tracking-sensitive workflows.
- Frontend displays `identified`, `unmapped`, and `unidentified` as tracking states, not permission decisions.
- Frontend may present login/register UI, but this repository does not own frontend source changes.
- No access-denied UI, route hiding, or permission gating is required by this initiative unless separately scoped.
- Handoff docs include local QA guidance for creating a test account, logging in, verifying `/api/session`, logging out, and confirming no manual identity override. [Product → QA]

#### Feature P2-7: Validation through existing build and test surfaces

Priority: Must-have next initiative.

##### Story P2-S7: Validate cookie identity without expanding into authorization testing

As QA and Product, we need confidence that register/login/logout/session behavior matches the contract and that existing application behavior remains ungated.

Acceptance Criteria:
- `dotnet build .\Totem.sln -c Release` succeeds after implementation. [Product → QA]
- Relevant automated tests cover successful registration, duplicate/invalid registration, successful login, failed login, logout, and `/api/session` states after each auth transition where a suitable Web/API test harness exists or can be added consistently. [Product → QA]
- Tests or documented manual QA verify that no existing application/data route was newly blocked by authorization as part of the MVP. [Product → QA]
- Tests or documented manual QA verify cookie security flags in the configured environment where feasible. [Product → QA]
- If no suitable Web/API test harness exists, manual validation steps are documented and the gap is called out to QA before release signoff.
- Domain/topic tests are added only if actor metadata is added to commands, events, topics, or queries.

### Epic P3: Future Roll-Row Audit and Resource Model

Purpose: Capture the broader target model without allowing it to expand the completed Phase 1 session baseline or the next cookie identity initiative. [Vision → Product: V4]

This future audit/resource work is not part of backend-owned cookie login/register. It requires separate Product and Design planning.

#### Feature P3-1: Roll-scoped resource model

Priority: Should-have future initiative.

##### Story P3-S1: Define box, roll, row, and column-definition resources

As a frontend data consumer, I need roll-scoped resources so large Miller/Formatic table data can be queried and addressed at the right grain.

Acceptance Criteria:
- Product and Design define whether row resources are addressed by `rollId + rowId` or globally unique `rowId`; current frontend preference is `rollId + rowId` unless backend guarantees global row uniqueness. [Product → Design]
- Box resources expose roll IDs or links rather than embedding full roll details by default.
- Roll resources expose row IDs or links rather than requiring client-wide row lists.
- Row resources expose links or schema references to column definitions needed to interpret cells.
- Hypermedia priority is documented as: box to rolls, roll to rows, row to column definitions, then mutation links for targeted cell update.
- Migration design accounts for current frontend assumptions around client-wide columns, regular rows, custom rows, filtering, selection, keyboard navigation, optimistic patch state, and QueryHub invalidation. [Product → Design]

#### Feature P3-2: Targeted per-cell audit facts

Priority: Should-have future initiative.

##### Story P3-S2: Persist targeted cell changes with actor and timestamp metadata

As a user reviewing row history, I need backend queries to expose who changed a cell and when.

Acceptance Criteria:
- A targeted command/fact shape is designed for changing one column on one row for one roll.
- Durable events carry server-observed actor metadata and timestamp where audit is required.
- Missing or unmapped identity remains a tracking state in event data, not a command rejection condition, unless Product explicitly changes workflow rules.
- Changed/editable cells can expose audit metadata first; untouched or not-yet-tracked cells may represent audit as absent or `not tracked yet`.
- Event and query changes include domain/topic tests. [Product → QA]

#### Feature P3-3: Audit projections for row queries

Priority: Could-have until P3-1 and P3-2 are designed.

##### Story P3-S3: Expose per-cell audit metadata in row query responses

As a frontend table, I need optional per-cell audit metadata so I can show changed date and user for cells that have been tracked.

Acceptance Criteria:
- Row query cell shapes can include `value`, `lastChangedAt`, and `lastChangedBy` metadata.
- `lastChangedBy` supports at least `displayLabel` and nullable `processUserId`.
- Query responses clearly distinguish missing metadata from known unidentified or unmapped tracking states.
- Backward compatibility or migration behavior is defined for existing rows and client-wide query consumers. [Product → Design]

#### Feature P3-4: Frontend migration and invalidation design

Priority: Could-have future initiative.

##### Story P3-S4: Plan frontend transition from client-wide buckets to roll-scoped resources

As the product team, I need a migration plan so improved audit resources do not break current table workflows.

Acceptance Criteria:
- Loading strategy is defined for roll-scoped rows, including aggregation or virtualization if the active table spans many rolls.
- Filtering and selection semantics across rolls are documented.
- Custom row relationship to roll-scoped resources is decided.
- QueryHub bucket and invalidation semantics are designed before endpoint changes depend on them. [Product → Design]
- Frontend and backend teams agree which resources change first and how compatibility is maintained during migration.

## Priority Matrix

| Priority | Items | Rationale |
| --- | --- | --- |
| Completed baseline / maintain | P1-S1 session endpoint; P1-S2 resolver and mapping; P1-S3 write payload audit and server-side tracking identity guard | Preserves the Phase 1 tracking contract as the foundation for cookie identity. |
| Must-have next initiative | P2-S1 registration; P2-S2 login; P2-S3 logout; P2-S4 `/api/session` cookie integration; P2-S5 cookie security; P2-S6 frontend handoff; P2-S7 validation | Delivers backend-owned ASP.NET Core cookie identity for reliable tracking without authorization scope creep. |
| Should-have next initiative / Governance | FND-S1 docs/scaffolding baseline; automated Web/API tests if harness fits; README/docs index updates; execution log convention | Strengthens SDLC evidence and maintainability, but should not expand feature behavior. |
| Should-have Future | P3-S1 roll-scoped resources; P3-S2 targeted cell audit facts | Needed for long-term row/column audit goal, but too large for cookie identity. |
| Could-have Future | P3-S3 audit projection enhancements; P3-S4 frontend migration and QueryHub invalidation redesign | Valuable after resource and event model decisions stabilize. |
| Explicitly out of scope for P2 cookie identity | Authorization/permission model, roles/claims administration, `[Authorize]` gates on existing application/data routes, QueryHub auth, access-denied UI, command rejection for unauthenticated/unmapped users, password reset, email verification, external identity providers, roll-row resource migration, per-cell audit projections | Prevents scope creep and preserves the clarified tracking-first product direction. |

## Dependencies, Blockers, and Open Questions

### Dependencies

- ASP.NET Core authentication middleware and cookie configuration in `Outermind.Web`. [Product → Design]
- Backend-owned account persistence and password hashing approach selected by Design. [Product → Design]
- Mapping between backend account identity and `processUserId` for tracking. [Product → Design]
- Existing `GET /api/session` resolver and DTO contract from the Phase 1 baseline. [Product → Design]
- `Outermind.Web/Program.cs` and relevant auth/session controllers for endpoint routing and middleware order. [Product → Design]
- Existing write endpoints remain audited so client-chosen identity is ignored/overwritten where tracking-sensitive fields appear. [Product → Design]
- Existing tests and solution build/test commands; Web/API harness availability determines automated coverage depth. [Product → QA]
- Frontend repository for register/login/logout UI, session fetch timing, and removal of manual identity fallback. This repo provides the backend contract and handoff.

### Blockers for P2 execution

- No Product blocker remains if the backend-owned cookie identity decisions above are accepted.
- Design loopback is required if the implementation cannot identify a safe account store/password hashing approach within the current architecture.
- Product loopback is required if stakeholders want roles, permissions, route gating, admin account management, password reset, email verification, external providers, or write blocking for unauthenticated users in this initiative.
- QA loopback is required if cookie behavior cannot be validated automatically or manually in a representative environment.

### Non-blocking open questions to document during Design or handoff

- Final route names and request/response DTOs for register, login, and logout.
- Whether successful registration signs the user in immediately or requires a separate login step; Product preference is immediate sign-in for MVP.
- What account identifier should be canonical for login and tracking display: username, email, or both?
- How should backend accounts map to existing `processUserId` values, and should registration create or link that mapping automatically?
- Should `/api/session` add an account ID/auth method field, or preserve only the Phase 1 fields for now?
- Cookie lifetime, sliding expiration, remember-me behavior, and local-development cookie exceptions.
- CSRF/anti-forgery implementation details for same-origin cookie-backed API calls.
- Should the frontend fetch `/api/session` at app boot, Miller page entry, after auth transitions, or all of the above?
- If a Web/API integration test project is absent, what manual QA evidence is sufficient for the first release candidate?

## Acceptance Criteria Summary for P2 Cookie Identity

P2 can be considered Product-complete when:

- Backend register, login, and logout endpoints exist and use ASP.NET Core-compatible cookie authentication behavior.
- Successful register/login creates a backend-owned identity that `/api/session` reports through the existing tracking contract.
- Logout clears backend identity and `/api/session` reflects the logged-out state.
- Cookie security expectations are implemented or explicitly documented for the target environment.
- No manual/delegated/client-supplied `ProcessUserID` override is accepted.
- Existing application/data routes are not converted into permission-gated routes by default.
- Frontend-facing docs/handoff describe endpoint usage, session refresh timing, and no-authorization semantics.
- Existing build/tests pass, with targeted auth/session tests or a documented QA gap.

## Change Log

| Date | Change |
| --- | --- |
| 2026-06-30 | Updated backlog for new stakeholder direction: backend-owned ASP.NET Core cookie login/register is now the next identity goal. Preserved completed Phase 1 passive session tracking as the baseline, added P2 stories for register/login/logout/session integration/cookie security/frontend coordination/no authorization gating, and moved roll-row audit/resource migration to future P3 scope. |
| 2026-06-30 | Created product backlog from session-state plan. Reframed original Windows auth handoff from authorization semantics to tracking-only session states. Split immediate Phase 1 backend session/identity work from future roll-row audit/resource migration. Captured current Miller write payload findings, frontend feedback, priorities, acceptance criteria, dependencies, blockers, and Product-lite readiness. |