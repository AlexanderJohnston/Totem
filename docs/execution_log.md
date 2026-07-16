# Execution Log

Last updated: 2026-06-30

## Implemented Features

### 2026-06-30 - P3 roll-scoped Microfilm resources and targeted audit [Product → Execution: P3-S1, P3-S2, P3-S3, P3-S4, P3-S5] [Design → Execution]

- Added roll-scoped table commands/events/topic routed by `rollId`:
  - `ReplaceRollMicrofilmTableColumns`
  - `CreateRollMicrofilmRow`
  - `UpdateRollMicrofilmRowCell`
  - `RollMicrofilmTableColumnsChanged`
  - `RollMicrofilmRowCreated`
  - `RollMicrofilmRowCellChanged`
- Added audit value types for durable actor metadata and per-cell audit state: `MicrofilmAuditActorStamp`, `MicrofilmCellAudit`, `tracked`, and `notTrackedYet`.
- Added roll-scoped projections:
  - `RollMicrofilmLookupQuery`
  - `RollMicrofilmColumnsQuery`
  - `RollMicrofilmRowsQuery`
  - `RollMicrofilmRowQuery`
  - `RollMicrofilmTableQuery`
  - `LegacyRowRoutingIndexQuery`
- Updated legacy client-wide columns, regular rows, and custom rows queries to also project roll-scoped events during migration.
- Added canonical roll-scoped API routes for columns, rows, table aggregate, row lookup, regular/custom row creation, and targeted cell patching.
- Added legacy route adapters:
  - legacy create routes may accept `rollId` to create via the roll-scoped model when the roll belongs to the same `{clientId}` route;
  - legacy PATCH routes use `LegacyRowRoutingIndexQuery` to dispatch targeted roll cell changes when the row has migrated, preserve the legacy `{ row }` response envelope, and otherwise fall back to existing client-wide behavior.
- Roll-scoped patch commands carry the expected route row kind and reject regular/custom route mismatches before changing cells.
- Added server-resolved actor stamping for roll-scoped writes using `IInteractionIdentityResolver`; client actor fields remain unsupported and ignored.
- Preserved tracking-not-authorization scope: no route gates, roles, QueryHub auth, command blocking, or write rejection for `unmapped`/`unidentified` actor states were added.
- Added focused tests for roll-scoped topic behavior, targeted cell facts, audit projection metadata, roll lookup, row lookup, legacy routing index, and legacy client projection compatibility.

### 2026-06-30 - P2 backend-owned ASP.NET Core cookie identity [Product → Execution: P2-S1, P2-S2, P2-S3, P2-S4, P2-S5, P2-S7] [Design → Execution]

- Added backend-owned auth endpoints: `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout`, and supporting `GET /api/auth/csrf`.
- Added minimal file-backed `ApplicationUser` store with ASP.NET Core `IPasswordHasher<ApplicationUser>` password hashing. The default store path is `App_Data\interaction-users.json`, and `App_Data/` is ignored by git.
- Added cookie authentication under scheme `TotemInteractionCookie`; API redirects are suppressed to `401`/`403`, and cookies are `HttpOnly`, `SameSite=Lax`, 8-hour sliding, non-persistent, and `Secure` outside development.
- Updated `/api/session` resolution so backend cookie claims are preferred and report `trackingSource: "backend-cookie"` before falling back to the passive host-principal resolver.
- Added same-origin/explicit-origin CORS handling for credentials and a double-submit CSRF token strategy for authenticated unsafe requests, including logout.
- Preserved tracking-not-authorization scope: no `[Authorize]`, `UseAuthorization`, QueryHub auth, role gates, permission policies, or command blocking were added.
- Preserved no manual/delegated `ProcessUserID` override: register/login request DTOs do not accept `processUserId`; the server derives `processUserId` from normalized username.
- Added focused tests for backend cookie session resolution, user registration/login manager behavior, auth controller register/login/logout behavior, and cookie-backed CSRF enforcement.

### 2026-06-30 - Phase 1 backend session tracking [Product → Execution: P1-S1, P1-S2, P1-S3, P1-S4, P1-S5, P1-S6] [Design → Execution]

- Added same-origin `GET /api/session` endpoint in `Outermind.Web`.
- Added `InteractionIdentityResolver` web-infrastructure service that passively consumes `HttpContext.User` and configuration-backed `InteractionIdentity` account mappings.
- Added resolver unit tests for `identified`, `unmapped`, `unidentified`, UPN matching, Windows-account normalization, and duplicate mapping failures.
- Added safe `InteractionIdentity` appsettings defaults with no personal mappings committed.
- Added `UserSecretsId` for `Outermind.Web` so local/deployment account mappings can be supplied without committing secrets.
- Updated frontend-facing docs with the final tracking-only `/api/session` contract.

## Code Changes & Decisions

- `GET /api/session` always returns `200 OK` for normal tracking states and `Cache-Control: no-store`.
- Response fields are `status`, `displayLabel`, `windowsAccount`, `userPrincipalName`, `processUserId`, and `trackingSource`; existing MVC camelCase behavior is used.
- Status values are string constants: `identified`, `unmapped`, `unidentified`.
- The resolver is scoped to `Outermind.Web/IdentityTracking` because it depends on request-host principal semantics and is not a domain permission model.
- Principal capture remains passive. No `[Authorize]`, `UseAuthorization`, QueryHub auth, Negotiate package/middleware, login UI, or command blocking was added.
- Account mapping uses `InteractionIdentity:AccountMappings[]` with multiple aliases per process user. Blank mappings are ignored; duplicate normalized account aliases that map to different `processUserId` values throw a configuration error.
- Windows account lookup normalizes `/` to `\` and compares case-insensitively; response `windowsAccount` uses canonical `DOMAIN\user` when observed.
- `tests/Quantum.Tests` references `Quantum.Web` for resolver coverage and uses `LangVersion=latest` because the referenced Web/OpenAPI source generator emits newer C# features.

## Integration Notes

- Backend cookie identity uses open self-registration for the MVP and immediately signs in successful registrations.
- Fetch `GET /api/auth/csrf` and send `X-CSRF-TOKEN` for authenticated unsafe methods. The endpoint sets a readable `Totem.Csrf` cookie and returns the same token/header metadata in JSON.
- Credentialed CORS is only enabled when `Cors:AllowedOrigins` is explicitly configured; wildcard CORS remains non-credentialed.
- Configure mappings through user secrets, environment variables, or deployment configuration, not source-controlled personal values.
- With the current local project settings (`windowsAuthentication: false`, anonymous enabled, and no auth middleware), QA should expect `unidentified` unless the host supplies an authenticated request principal.
- `trackingSource` is `backend-cookie` for backend-owned auth cookies, `windows-integrated-auth` when a passive host principal is observed, and `none` when unidentified.

## Current Write Endpoint Actor-Field Audit

Audited current Phase 1 surfaces:

- `PATCH /api/microfilm/rows/{clientId}/{rowId}` (`UpdateMicrofilmTableCellRequest`): no actor fields.
- `PATCH /api/microfilm/custom-rows/{clientId}/{rowId}` (`UpdateMicrofilmTableCellRequest`): no actor fields.
- `POST /api/microfilm/rows/{clientId}` (`CreateMicrofilmRegularRowRequest`): no actor fields.
- `POST /api/microfilm/custom-rows/{clientId}` (`CreateMicrofilmCustomRowRequest`): no actor fields.
- `PUT /api/microfilm/columns/{clientId}` (`ReplaceMicrofilmTableColumnsRequest`): no actor fields.
- `POST /api/microfilm/client-profiles` and `PUT /api/microfilm/client-profiles/{profileId}` (`SaveMicrofilmClientProfileRequest`): no actor fields.
- `POST /api/microfilm/wasp/import/force` (`ForceWaspImport`): trigger only, no actor field.

No `ProcessUserID`, `ScanUserID`, `userId`, process operator, or equivalent tracking actor fields were found on current Miller/Microfilm table/profile write DTOs. Unknown extra JSON actor-like properties are ignored by current DTO binding and no durable command/event actor metadata was added in Phase 1.

## Technical Debt

- P3 compatibility keeps the legacy client-wide table topic/routes available. Existing legacy rows have no persisted `rollId`; they remain on the legacy path until recreated or explicitly migrated with a `rollId`.
- Legacy client-wide column projection can become lossy if Product later allows divergent schemas per roll within one client. Sunset legacy `/columns/{clientId}` before enabling divergent per-roll schemas.
- QueryHub fan-out relies on query projection routing to update both roll-scoped and legacy client-scoped query buckets from roll events; no QueryHub authorization behavior was added.
- Host principal capture remains a validation item. If `identified` is required under Kestrel/IIS Express, Research should verify passive Windows/Negotiate behavior before adding any auth middleware.
- Duplicate mapping errors currently surface as infrastructure/configuration exceptions; a future hardening pass could add startup validation and controlled problem details without exposing sensitive internals.
- There is no automated Web/API integration test harness yet; resolver unit tests cover the state algorithm and manual HTTP validation covers the default route/header/serialization behavior.
- Further audit hardening may add event export/reporting views, but targeted cell audit state and server-resolved actor stamps are now implemented for roll-scoped rows.
- Lockout/rate limiting remains explicitly risk-accepted for the local/internal MVP; add framework rate limiting or lockout before broader non-local exposure if Product/Governance requires it.

## Suggested Tests

[Execution → QA]

1. Run `dotnet build .\Totem.sln -c Release`.
2. Run `dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release`.
3. Start Quantum Web without principal capture and confirm `GET /api/session` returns `200 OK`, `status: "unidentified"`, non-empty `displayLabel`, null account/process fields, and `trackingSource: "none"`.
4. Configure local user-secret mapping for the observed account and host-supplied principal; confirm `status: "identified"` and populated `processUserId`.
5. Remove/alter mapping while principal is present; confirm `status: "unmapped"`, `processUserId: null`, and no `401/403`.
6. Send current Microfilm write requests with extra fake `ProcessUserID`, `ScanUserID`, or `userId` JSON properties and confirm command behavior remains unchanged and does not persist client-selected actor identity.
7. Confirm no QueryHub auth/login/permission-gating behavior was introduced.
8. Register a backend user, confirm `Set-Cookie` for `Totem.Auth`, and confirm `GET /api/session` returns `trackingSource: "backend-cookie"` with the server-generated `processUserId`.
9. Fetch `/api/auth/csrf`, send `X-CSRF-TOKEN` on authenticated unsafe requests, and confirm logout clears cookie identity and returns `unidentified`.
10. For P3, run focused Microfilm tests and verify roll-scoped rows/cells project audit metadata and legacy client queries still update from roll events.


## Validation Results

[Execution → QA]

- `dotnet build .\Totem.sln -c Release`: Passed on 2026-06-30. Existing warnings observed: `NU1510` on `Outermind/Quantum.csproj` `System.Collections` package reference and `xUnit1026` unused theory parameter in `GenericClientPathParserTests`.
- `dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release`: Failed on 2026-06-30 due to an unrelated `ClientProfileRegistryTests.MatchesCorrectProfile` expectation for NARA path `2-Frames2`; `ClientProfileRegistry` currently configures NARA final-output signal as `1-originals`. This failure is outside the Phase 1 session-tracking changes.
- `dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release --filter InteractionIdentityResolverTests`: Passed on 2026-06-30 (6 tests).
- `dotnet build .\Outermind.Web\Quantum.Web.csproj -c Release`: Passed on 2026-06-30. Existing warning observed: `NU1510` on `Outermind/Quantum.csproj` `System.Collections` package reference.
- HTTP validation attempt for `GET http://127.0.0.1:5057/api/session`: Blocked before request on 2026-06-30 because local `Quantum.Web` startup failed with `InvalidOperationException: WASP bearer token not found. Provide "Wasp:Token" in configuration or place a token.txt file in the application directory.` No HTTP response evidence was produced in this run.
- Startup command attempted before failure: `dotnet run --project .\Outermind.Web\Quantum.Web.csproj -c Release --no-build --urls http://127.0.0.1:5057`. Follow-up manual validation should provide a local `Wasp:Token`/`Wasp__Token` value and then verify `200 OK`, `Cache-Control: no-store`, camelCase fields, and the default `unidentified` body.
- HTTP validation rerun for `GET http://127.0.0.1:5057/api/session`: Passed on 2026-06-30 after starting `Quantum.Web` with non-production environment variable `Wasp__Token=local-session-validation-stub-token` and `--urls http://127.0.0.1:5057`. Evidence: `StatusCode=200`; `ContentType=application/json; charset=utf-8`; `CacheControl=no-store`; body used camelCase fields and returned the expected default unidentified state: `{"status":"unidentified","displayLabel":"Unidentified user","windowsAccount":null,"userPrincipalName":null,"processUserId":null,"trackingSource":"none"}`. The temporary process PID was stopped after validation.
- `dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release --filter "InteractionIdentityResolverTests|ApplicationUserManagerTests|AuthControllerTests|CookieBackedCsrfFilterTests" --no-restore`: Passed on 2026-06-30 (17 tests).
- `dotnet build .\Totem.sln -c Release --no-restore`: Passed on 2026-06-30. Existing warning observed: `NU1510` on `Outermind/Quantum.csproj` `System.Collections` package reference.
- `dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release --filter "FullyQualifiedName~Microfilm" --no-restore`: Passed on 2026-06-30 (38 tests).

## Scaffolding Updates

- Created `/docs/execution_log.md` for Execution traceability. [Product → Execution: FND-S1]

### 2026-07-13 - Client-owned Microfilm column catalog and durable inactive fields [Product → Execution: Microfilm schema and retention] [Design → Execution]

- Made the client Microfilm column catalog authoritative for roll create/update validation. Canonical and legacy-dispatched roll writes now receive an immutable client-catalog snapshot from the Web API; old command callers without a snapshot retain the historical roll fallback.
- Merged guaranteed `boxName` and `rollName` baseline definitions into effective roll catalogs without overriding client definitions.
- Changed roll reads at the API boundary to return the effective client catalog and durable roll values. The roll columns compatibility PUT now replaces the owning client catalog rather than creating per-roll mappings.
- Preserved inactive `Cells` and `CellAudits` during reconciliation, removed roll-column-event mutation from client projections, and stopped historical roll column events from reconciling roll rows.
- Added focused regression coverage for client-catalog roll writes, unknown-field rejection, inactive-field retention, aggregate durability, and old roll-column projection isolation.
- Added `docs/frontend-microfilm-column-catalog-migration.md` for already-integrated frontend teams. [Execution → QA]

### 2026-07-13 - Validation remediation [Product → Execution: Microfilm schema and retention] [Design → Execution]

- Reworked effective roll-catalog merging to avoid mutating `List<T>.Reverse()`, keep `boxName` then `rollName` in deterministic order, and retain client definitions for either baseline ID.
- Added regression coverage for baseline ordering, client baseline overrides, and merge cloning. Existing topic/query coverage verifies client catalog snapshot writes and historical roll-column-event isolation.
- Controller-level tests were not added: the existing test project has no Microfilm API/query-host harness, and exercising the controller's `IQueryDb` extension-based reads would require a Timeline `AreaMap`/query database fixture rather than a focused mock. [Execution → QA]

## Suggested Tests (2026-07-13)

[Execution → QA]

1. Run the focused Microfilm test suite and verify catalog-snapshot writes accept client fields absent from historical roll defaults and reject unknown IDs.
2. Remove then re-add a client catalog ID after a roll cell update; verify table, rows, single-row, and legacy projections retain the value/audit.
3. Confirm `PUT /api/microfilm/rolls/{rollId}/columns` updates the client catalog for all client rolls and creates no roll-local mapping.
4. Replay a historical `RollMicrofilmTableColumnsChanged` event and confirm it cannot change client catalog or unrelated client row values.

## Validation Results (2026-07-13)

[Execution → QA]

- Not run by Execution per delegation instruction; validation is pending the Luna agent.