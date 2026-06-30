# Design Specification: Quantum Web Session Tracking, Cookie Identity, and Future Roll-Row Audit Boundaries

Last updated: 2026-06-30

Source inputs:
- `C:\Users\ajohnston\.copilot\session-state\f0fd027a-2db0-4e34-bfa0-ffa121ee6bc8\plan.md`
- `docs/product_backlog.md`
- User direction on 2026-06-30 to make backend-owned ASP.NET Core cookie login/register the next identity goal
- `docs/FORMATIC_WINDOWS_AUTH_IMPLEMENTATION_HANDOFF.md`
- `backend-integration-guide.md`
- Code inspection of `Outermind.Web/Program.cs`, `Outermind.Web/Controllers/MicrofilmController.cs`, `Outermind.Web/appsettings.json`, `Outermind.Web/Properties/launchSettings.json`, `Outermind/Microfilm/TableCommands.cs`, `Outermind/Microfilm/TableEvents.cs`, and `Outermind/Microfilm/TableTypes.cs`

## Architecture Overview

### Product-aligned scope

Phase 1 implements a backend tracking contract, not authorization. [Product → Design: P1-S1, P1-S2, P1-S3]

Phase 1 backend session tracking is now the current baseline/fallback: `GET /api/session` passively resolves `HttpContext.User` and returns explicit tracking states without accepting manual/delegated `ProcessUserID` overrides. The next identity initiative should add backend-owned ASP.NET Core cookie register/login/logout so browser sessions can produce reliable request principals for tracking. This next phase remains **tracking-first, not authorization-by-default**: cookies identify the actor; they do not imply permission gates unless Product later scopes authorization separately. [Product → Design: Next identity goal] [Design → Execution]
The completed Phase 1 baseline preserves these guardrails:
- No permission checks, `[Authorize]` gates, QueryHub auth, or command blocking.
- No login screens in Phase 1; the next phase deliberately adds backend-owned login/register for identity capture only, not authorization.
- Windows identity and Formatic/process-user mapping are tracking facts, not permission proof.
- Clients must not choose another operator/process user for tracking-sensitive workflows.
- Missing identity is explicit (`unidentified`), not a silent demo/manual fallback.
- Future roll-scoped row resources and per-cell audit projections stay out of Phase 1 unless Product re-scopes the work. [Product → Design: P2]

### Current-state validation

Observed repository state relevant to Phase 1:
- `Outermind.Web/Program.cs` configures OpenAPI, WASP API services, CORS, MVC, and SignalR/QueryHub, but no authentication/authorization services or middleware.
- `Outermind.Web/Properties/launchSettings.json` has IIS Express `windowsAuthentication: false` and `anonymousAuthentication: true`.
- `Outermind.Web/Controllers/MicrofilmController.cs` owns `api/microfilm` write/read endpoints and constructs Microfilm commands directly.
- Current Miller DTOs in `Outermind/Microfilm/TableTypes.cs` contain no `ProcessUserID`, `ScanUserID`, `userId`, or equivalent actor fields for table writes.
- Current table commands/events in `Outermind/Microfilm/TableCommands.cs` and `Outermind/Microfilm/TableEvents.cs` contain no actor/session metadata.
- Existing domain operator concepts (`KnownOperator`, `CreateOperator`, `AssignOperator`) are domain entities with Totem IDs and are not Windows/process-user mapping records.
- No Web/API integration test project was found; existing test coverage is domain/service oriented under `tests/Quantum.Tests`. [Design → QA]

### High-level component diagram

```text
Browser / frontend repo (outside this repo)
  |
  | GET /api/session
  v
Outermind.Web SessionController
  |
  | ClaimsPrincipal + InteractionIdentity config
  v
InteractionIdentityResolver (Outermind.Web host infrastructure)
  |
  | InteractionSession DTO
  v
200 OK tracking state: identified | unmapped | unidentified

Browser write commands
  |
  | Existing Miller/Microfilm payloads (no actor fields today)
  v
MicrofilmController
  |
  | Unknown/future client identity fields are ignored or overwritten by server identity when supported
  v
Existing Outermind.Microfilm commands/events (unchanged in Phase 1)
```


### Next-phase cookie-auth architecture

[Product → Design: Next identity goal] [Design → Execution]

```text
Browser / frontend repo (outside this repo)
  |
  | POST /api/auth/register or POST /api/auth/login
  v
Outermind.Web AuthController
  |
  | validate credentials / create user
  v
Backend-owned user store + ASP.NET Core password hasher
  |
  | SignInAsync(cookie scheme) creates HttpOnly auth cookie
  v
Browser stores secure cookie only; no ProcessUserID in client state

Subsequent browser calls
  |
  | Cookie: backend auth ticket
  v
ASP.NET Core Cookie Authentication middleware
  |
  | ClaimsPrincipal with stable user/process claims
  v
SessionController + InteractionIdentityResolver
  |
  | cookie claims preferred; passive principal resolver remains fallback
  v
GET /api/session -> identified | unmapped | unidentified, Cache-Control: no-store
```

Identity source-of-truth model:
- The durable backend user store is the source of truth for credentials, registration state, stable user ID, and any server-owned `processUserId`/tracking alias.
- The ASP.NET Core cookie is a signed, encrypted, HttpOnly transport for a snapshot of server-issued claims. It is not the durable user record and should be refreshable/invalidatable through security-stamp or equivalent store metadata when available.
- `GET /api/session` should prefer authenticated cookie claims (`backend-cookie`) when present, then fall back to the completed passive principal resolver for host-supplied Windows/other principals, then return `unidentified`.
- Clients must never submit, choose, or override `ProcessUserID`; registration/login assign tracking identity from server-side policy only.

### Request/response sequence: `GET /api/session`

```text
Frontend -> SessionController: GET /api/session
SessionController -> InteractionIdentityResolver: Resolve(HttpContext.User)
InteractionIdentityResolver -> IConfiguration/IOptions: load account mappings
alt no authenticated principal or no observed identity
  Resolver -> SessionController: status=unidentified, displayLabel="Unidentified user", source=none
else authenticated principal, no mapping
  Resolver -> SessionController: status=unmapped, observed account fields, processUserId=null
else authenticated principal, mapping found
  Resolver -> SessionController: status=identified, observed account fields, processUserId populated
SessionController -> Frontend: 200 OK + JSON, Cache-Control: no-store
```

## Component Designs

### 1. Identity tracking service/interface shape and location

[Product → Design: P1-S2] [Design → Execution]

Add host/request infrastructure under `Outermind.Web`, not under shared `Outermind` domain code:

```text
Outermind.Web/
  Controllers/
    SessionController.cs
  IdentityTracking/
    IInteractionIdentityResolver.cs
    InteractionIdentityResolver.cs
    InteractionIdentityOptions.cs
    InteractionSession.cs
    InteractionTrackingStatus.cs       (optional constants)
    InteractionTrackingSources.cs      (optional constants)
```

Recommended interface:

```csharp
public interface IInteractionIdentityResolver
{
  InteractionSession Resolve(ClaimsPrincipal principal);
}
```

Rationale:
- The resolver is synchronous for Phase 1 because it uses request principal + configuration only.
- Passing `ClaimsPrincipal` keeps the component unit-testable and avoids hiding request coupling behind `IHttpContextAccessor`.
- If mapping later moves to a database/service, the interface can evolve to `ValueTask<InteractionSession> ResolveAsync(...)` as a contained web-infrastructure change.

DI registration in `Outermind.Web/Program.cs`:

```csharp
services.Configure<InteractionIdentityOptions>(context.Configuration.GetSection("InteractionIdentity"));
services.AddSingleton<IInteractionIdentityResolver, InteractionIdentityResolver>();
```

If the resolver precomputes normalized mappings, use `IOptionsMonitor<InteractionIdentityOptions>` or rebuild the lookup from options on each resolve. For the expected small mapping set, either is acceptable. Prefer clarity unless profiling shows a need to optimize.

### 2. Session controller

[Product → Design: P1-S1] [Design → Execution]

Add a small controller:

```csharp
[ApiController]
[Route("api/session")]
public class SessionController : ControllerBase
{
  readonly IInteractionIdentityResolver _identity;

  public SessionController(IInteractionIdentityResolver identity) => _identity = identity;

  [HttpGet]
  public ActionResult<InteractionSession> Get()
  {
    Response.Headers.CacheControl = "no-store";
    return Ok(_identity.Resolve(HttpContext.User));
  }
}
```

Controller behavior:
- Always returns `200 OK` for normal tracking states: `identified`, `unmapped`, `unidentified`.
- Does not call `Challenge()`, `Forbid()`, or require `[Authorize]`.
- Uses existing camelCase JSON conventions from ASP.NET Core MVC.
- Returns `5xx` only for infrastructure/configuration failures; do not expose stack traces or sensitive auth internals in response bodies.
- Adds `Cache-Control: no-store` because session identity is per interaction and should not be stored by shared caches. The frontend may memoize in memory for a page/session if desired.

### 3. Principal extraction and normalization rules

[Product → Design: P1-S2] [Design → QA]

Resolver status algorithm:

1. If `principal?.Identity?.IsAuthenticated != true`, return `unidentified`.
2. Extract observed identity candidates from claims and identity name:
   - `ClaimTypes.WindowsAccountName` when available.
   - `ClaimTypes.Upn` and common `"upn"` claim when available.
   - `ClaimTypes.Name`, `ClaimTypes.NameIdentifier`, and `principal.Identity.Name` as fallbacks.
3. Classify candidates:
   - Values containing `\` are Windows account candidates.
   - Values containing `@` are UPN candidates.
   - Bare values are alias candidates for mapping/display only; do not invent a domain.
4. Normalize all candidate keys for lookup by trimming whitespace, replacing `/` with `\` for Windows account candidates, and comparing with `StringComparer.OrdinalIgnoreCase`.
5. Output account fields should be stable but not over-normalized:
   - `windowsAccount`: canonical `DOMAIN\user` when a domain-qualified account is observed; domain uppercased is acceptable, user casing may remain as observed.
   - `userPrincipalName`: observed UPN trimmed; lower-casing for lookup is okay, but response may preserve observed casing.
6. If no non-empty candidate remains, return `unidentified` even if `IsAuthenticated` was true.
7. If any candidate maps to a configured process user, return `identified`.
8. If candidates exist but no mapping exists, return `unmapped`.

Display label rules:
- `identified`: use configured mapping `displayLabel` when present; otherwise use `processUserId`; otherwise use observed UPN/account/name. `displayLabel` must be non-empty.
- `unmapped`: use observed friendly label/account when present; otherwise `Unmapped user`. This is a data quality label, not an authorization message.
- `unidentified`: always use neutral `Unidentified user`.

### 4. Mapping configuration schema

[Product → Design: P1-S2] [Design → Execution]

Use an array-based schema rather than a dictionary keyed by account. It is more future-proof for multiple aliases per person and easier to provide through user secrets/environment variables than dictionary keys containing `DOMAIN\user`.

Recommended shape:

```json
{
  "InteractionIdentity": {
    "unidentifiedDisplayLabel": "Unidentified user",
    "trackingSourceWhenPrincipalPresent": "windows-integrated-auth",
    "accountMappings": [
      {
        "accounts": [
          "DOMAIN\\ajohnston",
          "ajohnston@example.com",
          "ajohnston"
        ],
        "processUserId": "AJOHNSTON",
        "displayLabel": "Alex Johnston"
      }
    ]
  }
}
```

Recommended C# options:

```csharp
public sealed class InteractionIdentityOptions
{
  public string UnidentifiedDisplayLabel { get; set; } = "Unidentified user";
  public string TrackingSourceWhenPrincipalPresent { get; set; } = "windows-integrated-auth";
  public List<InteractionIdentityMappingOptions> AccountMappings { get; set; } = new();
}

public sealed class InteractionIdentityMappingOptions
{
  public List<string> Accounts { get; set; } = new();
  public string ProcessUserId { get; set; }
  public string DisplayLabel { get; set; }
}
```

Configuration rules:
- Do not commit machine-specific personal mappings unless they are clearly dummy examples. Use user secrets, environment variables, or deployment configuration for `ajohnston` and real users. [Design → QA]
- Blank `accounts` or blank `processUserId` entries should be ignored and optionally logged as configuration warnings.
- If duplicate normalized accounts map to different `processUserId` values, treat this as a configuration error or deterministic startup/resolve failure; do not silently choose one. This protects tracking data quality without becoming an authorization rule.
- Mapping is not a permission model and must not permit/deny access.

Example user-secret command for local QA documentation, not to be committed verbatim:

```powershell
dotnet user-secrets set "InteractionIdentity:AccountMappings:0:Accounts:0" "DOMAIN\ajohnston" --project .\Outermind.Web\Quantum.Web.csproj
dotnet user-secrets set "InteractionIdentity:AccountMappings:0:ProcessUserId" "AJOHNSTON" --project .\Outermind.Web\Quantum.Web.csproj
dotnet user-secrets set "InteractionIdentity:AccountMappings:0:DisplayLabel" "Alex Johnston" --project .\Outermind.Web\Quantum.Web.csproj
```

### 5. Authentication/Negotiate stance

[Product → Design: P1-S3] [Design → Execution] [Design → QA]

Phase 1 should be designed as **passive principal consumption first**:
- Implement `/api/session` and the resolver so they work with whatever `HttpContext.User` the host provides.
- Do not add authorization middleware, fallback policies, route attributes, or QueryHub auth.
- Do not challenge anonymous callers from `/api/session`; return `unidentified` instead.

Current local project settings do not appear to populate a Windows principal by default (`windowsAuthentication: false`, anonymous enabled, no auth middleware). Therefore:
- The endpoint/resolver can be implemented without Research.
- Producing an `identified` result in a real browser requires the host or client to supply an authenticated principal.
- If Execution is asked to add Microsoft Negotiate/Kestrel Windows auth package/middleware, insert a short Research/host-validation step first unless the team has already verified the behavior locally. The key uncertainty is whether the chosen hosting path can populate `HttpContext.User` without changing the Product-required normal `200 OK` semantics for anonymous/unmapped states.

Optional follow-up approach if validated:
- Add `Microsoft.AspNetCore.Authentication.Negotiate` aligned with the target ASP.NET Core version.
- Register `AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate()` only behind an explicit configuration switch such as `InteractionIdentity:PrincipalCapture:Mode = negotiate-passive`.
- Call `app.UseAuthentication()` before MVC routing executes (the existing `BeforeMvcApp` hook is the likely insertion point, after CORS and before `UseMvc`).
- Still do not add `UseAuthorization`, `[Authorize]`, fallback policies, `Challenge()`, or `Forbid()`.
- If passive Negotiate does not populate browser identity without a challenge, do not work around it by changing `/api/session` to `401`/`403`; loop back to Research/Product. [Design → QA]

Trade-off: this design is conservative and preserves Product semantics. It may require hosting configuration to observe Windows identity in production/local QA.

### 6. Current write endpoint actor-field audit behavior

[Product → Design: P1-S4] [Design → Execution] [Design → QA]

Audited current endpoints and DTO behavior:

| Endpoint | Current body | Actor fields today | Phase 1 behavior |
| --- | --- | --- | --- |
| `PATCH /api/microfilm/rows/{clientId}/{rowId}` | `{ columnId, value }` | none | Keep existing command shape; unknown client identity fields are ignored by binding. |
| `PATCH /api/microfilm/custom-rows/{clientId}/{rowId}` | `{ columnId, value }` | none | Keep existing command shape; unknown client identity fields are ignored by binding. |
| `POST /api/microfilm/custom-rows/{clientId}` | `{ cells }` | none | Keep existing command shape. |
| `POST /api/microfilm/rows/{clientId}` | `{ rowId, cells }` | none | Keep existing command shape. |
| `PUT /api/microfilm/columns/{clientId}` | `{ columns }` | none | Keep existing command shape. |
| `POST /api/microfilm/client-profiles` | `{ name, description, columns }` | none | Keep existing command shape. |
| `PUT /api/microfilm/client-profiles/{profileId}` | `{ name, description, columns }` | none | Keep existing command shape. |
| `POST /api/microfilm/wasp/import/force` | `{ trigger }` | none; trigger is not identity | Keep existing command shape. |

Decision for Phase 1:
- Do not add actor metadata to `Outermind/Microfilm` commands/events in Phase 1 because the current command/event surfaces do not support it and doing so would create a broad domain migration. [Product → Design: P1-S4]
- Do not reject writes solely because identity is `unmapped` or `unidentified`.
- Unknown extra JSON properties such as `ProcessUserID`, `processUserId`, `userId`, `operatorId`, or `scanUserId` should be ignored for current DTO-based endpoints. This avoids treating malformed/future client identity fields as permission violations and matches the existing trusted-command model.
- If a future DTO adds an identity field for tracking-sensitive workflows, the controller must ignore or overwrite the client value with the server-resolved `InteractionSession` before constructing the command. It should not accept delegated/manual actor identity from the client.
- Do not overwrite domain `operatorId` fields that are part of explicit domain operations such as operator assignment; those are not the same as request actor tracking.

Future durable stamping trigger:
- If Product re-scopes work to persist actor metadata on events, add a domain value object such as `TrackedInteractionActor`/`InteractionActor` and update commands, events, topics, queries, and tests deliberately. Loop back to Design for this broader change. [Design → QA]

### 7. Frontend-facing documentation updates

[Product → Design: P1-S5] [Design → Execution]

Execution should update `backend-integration-guide.md` and either amend or supersede `docs/FORMATIC_WINDOWS_AUTH_IMPLEMENTATION_HANDOFF.md` to remove stale authorization semantics.

Docs must state:
- `GET /api/session` is same-origin and tracking-only.
- Normal states return `200 OK`: `identified`, `unmapped`, `unidentified`.
- No QueryHub auth, login screens, permission checks, or command blocking are part of this phase.
- `displayLabel` is non-empty for all states; `processUserId` is nullable.
- Manual/delegated `ProcessUserID` override is unsupported for tracking-sensitive workflows.
- Frontend source changes happen outside this repo.
- Local `ajohnston` mapping should use user secrets/environment/deployment config, not committed secrets.
- If local hosting does not populate `HttpContext.User`, QA should expect `unidentified` until principal capture is configured/validated. [Design → QA]


## Next Phase: Backend-Owned ASP.NET Core Cookie Login/Register

[Product → Design: Next identity goal] [Design → Execution] [Design → QA]

### Goal and boundaries

Implement ASP.NET Core cookie authentication owned by `Outermind.Web` so the backend can reliably identify the current browser user for tracking. This replaces the need to depend on ambient Windows principal capture for normal identity, while preserving the completed passive resolver as a fallback for deployments that still supply a principal.

Boundaries:
- Add register/login/logout/session identity flows; do not add general authorization, permissions, `[Authorize]` gates on existing Microfilm writes, QueryHub auth, or command blocking by default.
- Do not reintroduce manual/delegated `ProcessUserID`. The server assigns `processUserId` or equivalent tracking identity from the user store/registration policy.
- Keep existing Microfilm command/event shapes unchanged unless Product separately scopes durable actor stamping.

### Recommended components and file locations

```text
Outermind.Web/
  Controllers/
    AuthController.cs                  # register/login/logout and optional CSRF token endpoint
    SessionController.cs               # existing endpoint, updated to prefer cookie claims
  IdentityTracking/
    InteractionIdentityResolver.cs     # current baseline; add cookie-claim path first
    InteractionSession.cs              # add backend-cookie source, optional stable user id
  Identity/
    ApplicationUser.cs                 # shape depends on store choice
    IApplicationUserStore.cs           # only if not using ASP.NET Core Identity directly
    AuthOptions.cs
```

Prefer the framework cookie middleware rather than custom cookie parsing:

```csharp
services
  .AddAuthentication(options =>
  {
    options.DefaultAuthenticateScheme = "TotemInteractionCookie";
    options.DefaultSignInScheme = "TotemInteractionCookie";
  })
  .AddCookie("TotemInteractionCookie", options => { /* secure settings below */ });
```

If Windows/passive principal capture remains enabled, avoid accidental challenge behavior by not setting a global fallback authorization policy and by keeping `/api/session` anonymous-friendly.

### Endpoint shape

Initial API contract, subject to final Product registration policy:

| Endpoint | Purpose | Normal responses | Notes |
| --- | --- | --- | --- |
| `POST /api/auth/register` | Create a backend-owned user and optionally sign in. | `201 Created` or `200 OK` + `InteractionSession`; `400` validation; `409` duplicate; `403` if registration closed. | Password never returned. Whether registration auto-signs-in is a policy decision. |
| `POST /api/auth/login` | Validate credentials and issue auth cookie. | `200 OK` + `InteractionSession`; `400` validation; `401` invalid credentials; `423`/`429` if lockout/rate limiting is implemented. | Invalid login responses should be generic. |
| `POST /api/auth/logout` | Clear auth cookie/session. | `204 No Content` or `200 OK` + unidentified `InteractionSession`. | Must call `SignOutAsync` for the cookie scheme and return `Cache-Control: no-store`. |
| `GET /api/session` | Current identity/tracking state. | Always `200 OK` for normal states. | Prefer cookie claims, then passive principal fallback, then `unidentified`. |
| `GET /api/auth/csrf` (optional/supporting) | Issue antiforgery token/header value for SPA unsafe requests. | `200 OK` + token metadata or set readable antiforgery cookie. | Needed if ASP.NET Core antiforgery is enabled for cookie-backed unsafe methods. |

Request DTO sketches:

```csharp
public sealed class RegisterRequest
{
  public string UserName { get; init; }        // or Email; final choice pending
  public string Email { get; init; }           // optional until registration policy is chosen
  public string DisplayName { get; init; }
  public string Password { get; init; }
}

public sealed class LoginRequest
{
  public string UserNameOrEmail { get; init; }
  public string Password { get; init; }
  public bool RememberMe { get; init; }        // optional; default false
}
```

### Cookie claims and `/api/session` behavior

Cookie-issued principals should contain only the stable claims needed for tracking and UI display:

```text
ClaimTypes.NameIdentifier or "sub"     stable backend user id
ClaimTypes.Name or "name"              display label / username
ClaimTypes.Email or "email"            optional email if collected
"totem:process_user_id"                server-owned tracking/process id when available
"totem:tracking_source" = "backend-cookie"
```

`InteractionIdentityResolver` should resolve in this order:
1. If an authenticated cookie principal has a stable backend user id and/or `totem:process_user_id`, return `identified` with `trackingSource = "backend-cookie"`.
2. If the cookie principal is authenticated but lacks required tracking claims, either load the user record to complete the session or return `unmapped` with a diagnostic-safe display label. Do not trust client-provided identity fields to fill the gap.
3. If no cookie principal is authenticated, run the existing passive principal mapping logic for Windows/host-supplied identities.
4. If neither path yields identity, return `unidentified`.

`/api/session` remains cache-disabled and anonymous-friendly. It must not call `Challenge()` for missing cookies.

### Cookie settings

Recommended planning baseline:

```csharp
options.Cookie.Name = "__Host-Totem.Auth";       // if HTTPS, Path=/, no Domain
options.Cookie.HttpOnly = true;
options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // allow dev exception only via explicit local config
options.Cookie.SameSite = SameSiteMode.Lax;      // Strict if UX allows; None only for true cross-site + HTTPS
options.SlidingExpiration = true;                // exact lifetime pending Product/security decision
options.ExpireTimeSpan = TimeSpan.FromHours(8);  // placeholder; do not hard-code without signoff
options.LoginPath = PathString.Empty;            // APIs return JSON, not redirects
options.AccessDeniedPath = PathString.Empty;
options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
```

Notes:
- Use `__Host-` only when the app is served over HTTPS and the cookie has `Path=/` and no `Domain`; otherwise use a non-prefixed development name locally.
- `HttpOnly` means frontend code cannot read the auth cookie; identity comes from `GET /api/session`.
- `SameSite=Lax` is a reasonable same-origin SPA default. If the frontend and API are on different sites, Product/Execution must explicitly design CORS + `SameSite=None; Secure` and CSRF controls.
- Cookie redirects must be suppressed for JSON APIs so auth endpoints do not return HTML/login redirects.

### Password hashing and user store planning

Do not build custom password crypto. Use ASP.NET Core Identity's `PasswordHasher<TUser>`/Identity stack or an equivalent implementation using the framework hasher.

Two viable implementation approaches:

| Approach | Pros | Cons | Design stance |
| --- | --- | --- | --- |
| ASP.NET Core Identity with a durable relational store | Mature password hashing, lockout, security stamp, validation hooks, future MFA/email confirmation path. | Adds schema/migrations and Identity concepts that may be larger than the immediate tracking need. | Preferred if the repo/deployment can accept a relational auth store. |
| Minimal custom user store + `IPasswordHasher<ApplicationUser>` | Smaller surface; can fit an existing persistence choice. | Must implement uniqueness, lockout/rate limiting, security-stamp/session invalidation, password reset policy, and admin tooling deliberately. | Acceptable only with explicit design/QA coverage before coding. |

Do not store credentials in `appsettings.json`, user secrets, or static files. The durable store must support unique normalized username/email lookup and a stable immutable user ID. Whether `processUserId` equals this immutable user ID, a generated alias, or an admin-managed Formatic-compatible ID is a decision required before implementation.

### CSRF and CORS considerations

Cookie authentication changes CSRF risk because browsers attach cookies automatically. Even if the backend is still tracking-only, a cross-site request could perform writes that are attributed to the signed-in user.

Planning baseline:
- Keep the frontend/API same-origin when possible.
- For any credentialed CORS, allow only explicit origins; never combine wildcard origins with credentials.
- Add antiforgery protection for unsafe cookie-backed methods (`POST`, `PUT`, `PATCH`, `DELETE`) before relying on cookies for tracking on write APIs. A common SPA pattern is an antiforgery endpoint that sets a non-HttpOnly CSRF token cookie and requires an `X-CSRF-TOKEN`/`X-XSRF-TOKEN` header on unsafe requests.
- Apply Origin/Referer validation and rate limiting to login/register even though they are not authenticated operations.
- Decide whether logout requires a CSRF token; recommended default is yes for consistency, with `SameSite=Lax` as an additional defense.

### Logout behavior

Logout must:
- Call `HttpContext.SignOutAsync("TotemInteractionCookie")` and expire the cookie.
- Return `204 No Content` or an `unidentified` `InteractionSession` with `Cache-Control: no-store`.
- Clear or rotate antiforgery/session metadata when applicable.
- Not delete the durable user record.
- If the selected user store supports security stamps/server-side session versioning, update validation so password changes/admin disables can invalidate existing cookies.

### Decisions required before implementation

[Design → Product] [Design → Execution]

1. User store choice: ASP.NET Core Identity + relational database vs minimal custom store using `IPasswordHasher<TUser>`.
2. Registration policy: open self-registration, invite-only, admin-seeded users, or disabled registration with login only.
3. Username/email policy: username vs email login, uniqueness rules, email confirmation requirement, display-name rules.
4. `processUserId` policy: generated immutable backend ID, normalized username, or admin-managed external/Formatic-compatible value.
5. Cookie lifetime/remember-me policy and whether persistent cookies are allowed.
6. CSRF implementation pattern and whether existing Microfilm write endpoints must enforce antiforgery in the same execution slice.
7. CORS topology: same-origin preferred; explicit credentialed origins only if frontend/API are separated.
8. Account recovery, lockout/rate limiting, password complexity, and admin disable/delete requirements.

## Data Models

### API DTO: `InteractionSession`

[Product → Design: P1-S1]

```csharp
public sealed class InteractionSession
{
  public string Status { get; init; }          // identified | unmapped | unidentified
  public string DisplayLabel { get; init; }    // non-empty for all statuses
  public string WindowsAccount { get; init; }  // nullable
  public string UserPrincipalName { get; init; } // nullable
  public string ProcessUserId { get; init; }   // nullable
  public string TrackingSource { get; init; }  // backend-cookie | windows-integrated-auth | none
}
```

Use strings rather than serialized enums unless the repo already has a string-enum convention. Constants can reduce typos internally.

### Internal mapping DTOs

```csharp
public sealed class InteractionIdentityMappingOptions
{
  public List<string> Accounts { get; set; } = new();
  public string ProcessUserId { get; set; }
  public string DisplayLabel { get; set; }
}
```


### Next-phase authentication DTOs and claims

[Product → Design: Next identity goal]

Planning-level DTOs:

```csharp
public sealed class RegisterRequest
{
  public string UserName { get; init; }
  public string Email { get; init; }
  public string DisplayName { get; init; }
  public string Password { get; init; }
}

public sealed class LoginRequest
{
  public string UserNameOrEmail { get; init; }
  public string Password { get; init; }
  public bool RememberMe { get; init; }
}
```

Planning-level user record:

```csharp
public sealed class ApplicationUser
{
  public string Id { get; init; }               // immutable backend user id
  public string NormalizedUserName { get; set; }
  public string NormalizedEmail { get; set; }
  public string DisplayName { get; set; }
  public string PasswordHash { get; set; }
  public string ProcessUserId { get; set; }     // server-owned; nullable only if registration policy allows unmapped
  public bool IsDisabled { get; set; }
  public string SecurityStamp { get; set; }     // or Identity equivalent
}
```

Cookie claims should be derived from this user record at sign-in and refreshed/invalidated according to the chosen store's security-stamp policy.

### Future Phase 2 actor value object boundary

Not part of Phase 1 execution. A future domain value may look conceptually like:

```csharp
public sealed class InteractionActor
{
  public string TrackingStatus { get; set; }
  public string DisplayLabel { get; set; }
  public string ProcessUserId { get; set; }
  public string WindowsAccount { get; set; }
  public string UserPrincipalName { get; set; }
  public string TrackingSource { get; set; }
}
```

Before persisting this, Design must decide which fields are appropriate for durable events versus transient diagnostics to avoid unnecessary PII retention. [Design → QA]

## API Specifications

### `GET /api/session`

[Product → Design: P1-S1]

Request:

```http
GET /api/session
Accept: application/json
```

Response: `200 OK` for all normal tracking states.

Identified example:

```json
{
  "status": "identified",
  "displayLabel": "Alex Johnston",
  "windowsAccount": "DOMAIN\\ajohnston",
  "userPrincipalName": "ajohnston@example.com",
  "processUserId": "AJOHNSTON",
  "trackingSource": "windows-integrated-auth"
}
```

Unmapped example:

```json
{
  "status": "unmapped",
  "displayLabel": "DOMAIN\\ajohnston",
  "windowsAccount": "DOMAIN\\ajohnston",
  "userPrincipalName": null,
  "processUserId": null,
  "trackingSource": "windows-integrated-auth"
}
```

Unidentified example:

```json
{
  "status": "unidentified",
  "displayLabel": "Unidentified user",
  "windowsAccount": null,
  "userPrincipalName": null,
  "processUserId": null,
  "trackingSource": "none"
}
```

HTTP semantics:
- `200 OK`: identified, unmapped, unidentified.
- `401/403`: not used for normal Phase 1 tracking states.
- `5xx`: infrastructure/configuration failure only.
- Recommended response header: `Cache-Control: no-store`.


### Next-phase cookie authentication APIs

[Product → Design: Next identity goal] [Design → Execution]

`POST /api/auth/login`

```http
POST /api/auth/login
Content-Type: application/json
Accept: application/json

{ "userNameOrEmail": "ajohnston@example.com", "password": "...", "rememberMe": false }
```

Success: `200 OK`, `Set-Cookie` for the backend auth cookie, and an `InteractionSession` with `trackingSource: "backend-cookie"`. Invalid credentials: `401 Unauthorized` with a generic problem response; do not reveal whether username/email exists.

`POST /api/auth/register`

```http
POST /api/auth/register
Content-Type: application/json
Accept: application/json

{ "userName": "ajohnston", "email": "ajohnston@example.com", "displayName": "Alex Johnston", "password": "..." }
```

Success: `201 Created` or `200 OK` depending on final Product policy. The response may include an `InteractionSession` if registration auto-signs-in. Duplicate username/email should return `409 Conflict`; closed registration should return `403 Forbidden` for this endpoint only.

`POST /api/auth/logout`

```http
POST /api/auth/logout
Accept: application/json
```

Success: `204 No Content` or `200 OK` with an unidentified session. The response expires the auth cookie and uses `Cache-Control: no-store`.

`GET /api/session` in the next phase keeps the existing response contract and adds `trackingSource: "backend-cookie"` when resolved from cookie claims. Missing cookies still return `200 OK` + `unidentified`; they do not trigger a challenge.

### Current write APIs

No request/response contract change in Phase 1 for the current Microfilm/Miller write endpoints. [Product → Design: P1-S4]

If clients send extra actor-like fields in JSON bodies today, the server should not persist or trust them because DTOs do not expose them. If future DTOs expose actor-like fields, controllers must overwrite/ignore them with server-resolved tracking context where actor metadata is supported.

## Trade-off Decisions

### Decision 1: Passive principal resolver vs. mandatory Negotiate auth

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| Passive resolver consuming existing `HttpContext.User` | Preserves `200 OK` tracking semantics; no authorization scope creep; works with host-supplied principal; easy to test. | Default local project likely returns `unidentified` until host principal capture is configured. | Choose for Phase 1. |
| Add Negotiate/Windows auth immediately | May populate Windows identity under some hosting paths. | Can require challenge behavior, package/middleware changes, and hosting-specific validation; risks accidental auth semantics. | Optional only after Research/host validation. |

### Decision 2: Array mapping schema vs. dictionary mapping schema

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| Array of mappings with aliases | Supports multiple account aliases per process user; user secrets/env-friendly; future-proof. | Requires building lookup and duplicate detection. | Choose. |
| Dictionary keyed by normalized account | Simple lookup. | Awkward escaping for `DOMAIN\user`; harder duplicate/alias handling; env vars less ergonomic. | Do not choose. |

### Decision 3: Ignore/overwrite client identity fields vs. reject malformed identity fields

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| Ignore unknown fields today; overwrite if future DTO exposes actor field | Preserves trusted-command behavior; prevents delegated/manual identity drift; avoids turning tracking into authorization. | Clients may not get immediate feedback that they sent obsolete fields. | Choose for Phase 1. |
| Reject any request containing actor-like fields | Strong signal to clients. | Requires custom raw-body validation, can break clients, and resembles command blocking. | Do not choose without Product re-scope. |

### Decision 4: No domain actor persistence in Phase 1 vs. immediate event changes

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| Keep Microfilm commands/events unchanged | Minimal, low-risk, aligned with current payloads; avoids broad topic/query migration. | Does not yet provide row/cell audit metadata. | Choose for Phase 1. |
| Add actor fields to existing table commands/events now | Starts audit trail sooner. | Existing events emit whole rows, not targeted deltas; broad tests/query changes; may conflict with future roll-row model. | Defer to Phase 2 or explicit re-scope. |


### Decision 5: Backend-owned cookie identity vs. continued Windows/principal-only capture

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| Backend-owned ASP.NET Core cookie register/login | Reliable browser identity across hosts; clear logout; no dependency on IIS/Negotiate; server controls `processUserId`. | Requires user store, password policy, CSRF controls, account lifecycle decisions. | Choose as next identity goal. |
| Continue passive Windows/principal-only capture | Minimal app-owned auth surface; current baseline already exists. | Host-dependent and unreliable in current local settings; no backend-owned register/login/logout. | Keep only as fallback/baseline. |

### Decision 6: Cookie identity as tracking source vs. authorization system

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| Use cookie only to populate tracking identity/session by default | Aligns Product direction; avoids accidental permission scope; preserves existing write compatibility. | Authenticated users are not yet protected by route-level authorization. | Choose until Product scopes authorization. |
| Add `[Authorize]`/permission gates with login | More conventional auth boundary. | Changes access semantics and may block existing flows; not requested. | Defer. |

### Decision 7: ASP.NET Core Identity store vs. minimal custom store

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| ASP.NET Core Identity-backed store | Mature hasher, lockout, security stamps, future account features. | Adds schema/dependency footprint. | Preferred pending store decision. |
| Minimal custom store using `IPasswordHasher<TUser>` | Smaller and tailored. | Easy to under-design security/account lifecycle. | Acceptable only after explicit Product/Execution decision. |

## Non-functional Requirements

[Product → Design: P1-S6] [Design → QA]

### Security and privacy
- Treat identity as tracking metadata only.
- Next-phase cookie authentication must use HttpOnly, Secure, SameSite-scoped cookies; suppress HTML redirects for API auth failures.
- Passwords must be hashed with ASP.NET Core Identity/`IPasswordHasher<TUser>` or equivalent framework hashing; never store plaintext or reversible passwords.
- Cookie-backed unsafe methods require a CSRF strategy before authenticated tracking is relied on for writes.
- Do not log secrets or committed personal mappings.
- Avoid logging full Windows/UPN values at high volume. If needed for support, use debug-level logging and be mindful of PII.
- Do not return group memberships, auth internals, stack traces, or permission language from `/api/session`.
- Do not use process identity (`Environment.UserName`) as a substitute for browser/request identity.

### Performance
- Resolver work is O(number of configured aliases) unless a lookup dictionary is precomputed; expected mapping size is small.
- `/api/session` should perform no database or EventStore calls in Phase 1.
- No change to QueryHub invalidation behavior.

### Reliability
- Explicit states prevent silent fallback to demo/manual identity.
- Cookie sessions should be invalidatable through security stamp/session versioning when users are disabled or credentials change, depending on selected user store.
- Duplicate mapping keys should fail clearly or produce a controlled configuration error.
- `displayLabel` must be non-empty for all states; add unit tests for this.

### Scalability and future-proofing
- Keep resolver in web infrastructure so future mapping storage changes do not affect domain model.
- Keep Phase 2 audit event design separate so row/column audit can use targeted facts rather than bolting actor metadata onto current whole-row events.

### Compatibility
- Existing Microfilm endpoints and payloads remain backward-compatible.
- Unknown extra JSON fields are ignored for current DTOs.
- Frontend integration can call `/api/session` at app boot or Miller page entry; no backend coupling to a specific frontend timing.

## Test Strategy

[Product → Design: P1-S6] [Design → QA]

Required validation after implementation:

```powershell
dotnet build .\Totem.sln -c Release
```

```powershell
dotnet test .\tests\Quantum.Tests\Quantum.Tests.csproj -c Release
```

Recommended targeted tests:
- Add lightweight resolver unit tests covering:
  - unauthenticated principal -> `unidentified`
  - authenticated domain account with mapping -> `identified`
  - authenticated UPN with mapping -> `identified`
  - authenticated principal with no mapping -> `unmapped`
  - display label non-empty for all states
  - duplicate mapping handling
- Because no Web/API integration harness exists, do not add heavy endpoint infrastructure unless consistent with repo conventions. If added, prefer a small `tests/Quantum.Web.Tests` project referencing `Outermind.Web` over mixing web-host tests into domain tests.
- If no endpoint automation is added, document manual QA steps and mark the gap for QA signoff. [Design → QA]


Next-phase cookie auth validation should add tests or documented manual QA for:
- Successful register/login sets an HttpOnly auth cookie and `GET /api/session` returns `identified` with `trackingSource = "backend-cookie"`.
- Invalid login returns a generic `401` and does not set an auth cookie.
- Logout expires the cookie and subsequent `GET /api/session` returns passive fallback or `unidentified`.
- Cookie claims cannot be overridden by request body `ProcessUserID`/`processUserId` fields.
- Cookie options are secure in non-development configuration (`HttpOnly`, `Secure`, expected `SameSite`).
- CSRF protection is enforced for unsafe cookie-backed operations once enabled.

Manual QA focus:
1. Start Quantum Web normally and call `GET /api/session`.
2. With no principal capture, confirm `200 OK` + `unidentified`.
3. With a host-supplied principal and matching user-secret mapping, confirm `200 OK` + `identified` and `processUserId`.
4. With a host-supplied principal and no mapping, confirm `200 OK` + `unmapped`.
5. Confirm no `401/403`, login flow, or command blocking was introduced.
6. Send current write payloads with extra fake `ProcessUserID`/`userId` fields and confirm behavior remains based on existing DTO command fields, not client actor values.

## Future Phase 2+ Roll-Row Audit and Resource Model Boundary

[Product → Design: P2-S1, P2-S2, P2-S3, P2-S4]

Phase 2 is a separate initiative. Do not include these changes in Phase 1 execution unless Product explicitly re-scopes.

### Target direction

```text
Box resource
  -> roll IDs / roll links
Roll resource
  -> row IDs / row links
Roll-row resource (addressed by rollId + rowId unless global row uniqueness is guaranteed)
  -> cells keyed by columnId
  -> optional audit metadata for changed/editable cells first
  -> schema/column-definition link
```

Conceptual future event:

```text
RollRowCellChanged(
  rollId,
  rowId,
  columnId,
  value,
  changedBy,
  changedAt
)
```

Conceptual future row response:

```json
{
  "rollId": "...",
  "rowId": "...",
  "cells": {
    "status": {
      "value": "Complete",
      "lastChangedAt": "2026-06-30T14:15:22Z",
      "lastChangedBy": {
        "displayLabel": "Alex Johnston",
        "processUserId": "AJOHNSTON"
      }
    }
  }
}
```

### Future design questions

- Confirm row addressing: frontend preference is `rollId + rowId` unless backend guarantees globally unique row IDs.
- Decide whether `changedAt` should come from Totem event metadata (`Event.When`) or an explicit event field.
- Decide how much actor PII belongs in durable events versus query-only projections.
- Define migration from current client-wide row buckets to roll-scoped resources without breaking filtering, selection, keyboard navigation, optimistic patch state, and QueryHub invalidation.
- Decide how custom rows relate to roll-scoped regular rows.
- Define QueryHub bucket semantics before changing invalidation behavior.

## Project Structure & Hygiene

[Design → Product] [Design → Execution]

Observed baseline:
- `src/`, `tests/`, and `docs/` exist.
- `.gitignore` exists and covers common .NET/IDE outputs including `bin/`, `obj/`, `.vs/`, `TestResults/`, and user files.
- `docs/product_backlog.md` exists.
- `docs/design.md` is created by this design update.

Recommended Phase 1 file layout:

```text
Outermind.Web/
  Controllers/
    SessionController.cs
  IdentityTracking/
    IInteractionIdentityResolver.cs
    InteractionIdentityResolver.cs
    InteractionIdentityOptions.cs
    InteractionSession.cs

docs/
  product_backlog.md
  design.md
  execution_log.md        (recommended before Execution evidence is required)

tests/
  Quantum.Web.Tests/      (optional lightweight resolver/controller tests if added)
```

Hygiene recommendations:
- Do not commit real account mappings, secrets, or machine-specific personal config.
- Add only empty/example-safe config shape to `appsettings.json` if useful; prefer user secrets for local `ajohnston` QA.
- If a new test project is added, include it in `Totem.sln` and document its command.
- Update README/docs index before release signoff if Governance requires planning artifact discoverability. [Design → QA]

## Risks and QA Focus Areas

[Design → QA]

| Risk | Impact | Mitigation / QA focus |
| --- | --- | --- |
| Local host does not populate `HttpContext.User` | `/api/session` returns `unidentified` even for logged-in Windows user. | Validate hosting path; Research before adding Negotiate if browser Windows identity is required. |
| Negotiate middleware accidentally changes HTTP semantics | Could introduce `401` challenge/blocking contrary to Product. | No `[Authorize]`, no `Challenge()`, no fallback policy; verify anonymous still gets `200 unidentified`. |
| Duplicate or stale mappings | Wrong `processUserId` tracking. | Duplicate detection, user-secret/deployment review, mapped/unmapped tests. |
| Old frontend handoff still says `403`/forbidden | Frontend may implement authorization UX by mistake. | Update docs to supersede old semantics. |
| Client submits actor-like extra fields | Tracking spoof/drift if accepted in future. | Current DTOs ignore; future DTOs overwrite/ignore with server identity. |
| Broad domain actor persistence attempted in Phase 1 | Scope creep and brittle event/query migration. | Defer to Phase 2 design unless explicitly re-scoped. |
| Cookie login ships without CSRF controls | Cross-site requests could be attributed to the signed-in user even if the system is tracking-only. | Decide/enforce antiforgery pattern for unsafe methods before relying on cookies for write tracking. |
| Weak/custom password storage | Account compromise and non-compliance. | Use ASP.NET Core Identity/`IPasswordHasher<TUser>`; no custom crypto or config-stored credentials. |
| Registration policy ambiguous | Uncontrolled account creation or missing `processUserId` mappings. | Product must choose open/invite/admin-seeded registration and server-owned process ID policy before implementation. |
| Cookie auth accidentally becomes route authorization | Existing workflows may start returning `401/403`. | Do not add `[Authorize]`/fallback policies to Microfilm APIs unless separately scoped; `/api/session` remains `200` for normal states. |

## Implementation Sequencing

[Design → Execution]

1. Add `InteractionIdentity` options, resolver, and DTO under `Outermind.Web/IdentityTracking`.
2. Register options and resolver in `Outermind.Web/Program.cs`.
3. Add `SessionController` for `GET /api/session` with `Cache-Control: no-store`.
4. Add safe config documentation or empty/example-safe config only; put real `ajohnston` mapping in user secrets/environment.
5. Add resolver unit tests if practical; otherwise document manual QA gap.
6. Audit Microfilm write endpoints in execution notes; do not change command/event shapes for actor metadata in Phase 1.
7. Update frontend-facing docs with final contract and old-handoff corrections.
8. Run solution build and targeted tests.
9. If Windows principal capture is required and not supplied by hosting, pause for Research/host validation before adding Negotiate package/middleware.


Next-phase cookie login/register sequencing:

1. Product/Design confirm user store, registration policy, `processUserId` policy, cookie lifetime, and CSRF/CORS topology.
2. Add cookie authentication services/middleware with secure API-friendly cookie settings and no global authorization fallback.
3. Implement durable user store and password hashing; add user uniqueness and security-stamp/disable behavior per selected approach.
4. Add `AuthController` register/login/logout endpoints and optional antiforgery token endpoint.
5. Update `InteractionIdentityResolver` so cookie claims are the preferred tracking source and passive principal mapping remains fallback.
6. Add tests for cookie issue/clear/session resolution, invalid credentials, no manual `ProcessUserID` override, and CSRF behavior.
7. Update frontend/backend integration docs with credentialed request and CSRF instructions.

## Change Log

| Date | Change |
| --- | --- |
| 2026-06-30 | Added next-phase backend-owned ASP.NET Core cookie login/register design covering endpoint shape, cookie/session source of truth, secure cookie settings, password/user-store planning, CSRF/CORS, logout, tracking-not-authorization boundary, decisions required, risks, tests, and sequencing. |
| 2026-06-30 | Created Phase 1 technical design for tracking-only `/api/session`, server-observed identity resolver, mapping schema, auth/Negotiate stance, write endpoint actor-field audit behavior, test/documentation strategy, and Phase 2 roll-row audit boundaries. |
