# Execution Log

Last updated: 2026-06-30

## Implemented Features

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

- Configure mappings through user secrets, environment variables, or deployment configuration, not source-controlled personal values.
- With the current local project settings (`windowsAuthentication: false`, anonymous enabled, and no auth middleware), QA should expect `unidentified` unless the host supplies an authenticated request principal.
- `trackingSource` is `windows-integrated-auth` when a principal is observed and `none` when unidentified.

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

- Host principal capture remains a validation item. If `identified` is required under Kestrel/IIS Express, Research should verify passive Windows/Negotiate behavior before adding any auth middleware.
- Duplicate mapping errors currently surface as infrastructure/configuration exceptions; a future hardening pass could add startup validation and controlled problem details without exposing sensitive internals.
- There is no automated Web/API integration test harness yet; resolver unit tests cover the state algorithm and manual HTTP validation covers the default route/header/serialization behavior.
- Durable row/cell audit actor metadata remains deferred to Phase 2 design.

## Suggested Tests

[Execution → QA]

1. Run `dotnet build .\Totem.sln -c Release`.
2. Run `dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release`.
3. Start Quantum Web without principal capture and confirm `GET /api/session` returns `200 OK`, `status: "unidentified"`, non-empty `displayLabel`, null account/process fields, and `trackingSource: "none"`.
4. Configure local user-secret mapping for the observed account and host-supplied principal; confirm `status: "identified"` and populated `processUserId`.
5. Remove/alter mapping while principal is present; confirm `status: "unmapped"`, `processUserId: null`, and no `401/403`.
6. Send current Microfilm write requests with extra fake `ProcessUserID`, `ScanUserID`, or `userId` JSON properties and confirm command behavior remains unchanged and does not persist client-selected actor identity.
7. Confirm no QueryHub auth/login/permission-gating behavior was introduced.


## Validation Results

[Execution → QA]

- `dotnet build .\Totem.sln -c Release`: Passed on 2026-06-30. Existing warnings observed: `NU1510` on `Outermind/Quantum.csproj` `System.Collections` package reference and `xUnit1026` unused theory parameter in `GenericClientPathParserTests`.
- `dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release`: Failed on 2026-06-30 due to an unrelated `ClientProfileRegistryTests.MatchesCorrectProfile` expectation for NARA path `2-Frames2`; `ClientProfileRegistry` currently configures NARA final-output signal as `1-originals`. This failure is outside the Phase 1 session-tracking changes.
- `dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release --filter InteractionIdentityResolverTests`: Passed on 2026-06-30 (6 tests).
- `dotnet build .\Outermind.Web\Quantum.Web.csproj -c Release`: Passed on 2026-06-30. Existing warning observed: `NU1510` on `Outermind/Quantum.csproj` `System.Collections` package reference.
- HTTP validation attempt for `GET http://127.0.0.1:5057/api/session`: Blocked before request on 2026-06-30 because local `Quantum.Web` startup failed with `InvalidOperationException: WASP bearer token not found. Provide "Wasp:Token" in configuration or place a token.txt file in the application directory.` No HTTP response evidence was produced in this run.
- Startup command attempted before failure: `dotnet run --project .\Outermind.Web\Quantum.Web.csproj -c Release --no-build --urls http://127.0.0.1:5057`. Follow-up manual validation should provide a local `Wasp:Token`/`Wasp__Token` value and then verify `200 OK`, `Cache-Control: no-store`, camelCase fields, and the default `unidentified` body.
- HTTP validation rerun for `GET http://127.0.0.1:5057/api/session`: Passed on 2026-06-30 after starting `Quantum.Web` with non-production environment variable `Wasp__Token=local-session-validation-stub-token` and `--urls http://127.0.0.1:5057`. Evidence: `StatusCode=200`; `ContentType=application/json; charset=utf-8`; `CacheControl=no-store`; body used camelCase fields and returned the expected default unidentified state: `{"status":"unidentified","displayLabel":"Unidentified user","windowsAccount":null,"userPrincipalName":null,"processUserId":null,"trackingSource":"none"}`. The temporary process PID was stopped after validation.

## Scaffolding Updates

- Created `/docs/execution_log.md` for Execution traceability. [Product → Execution: FND-S1]
