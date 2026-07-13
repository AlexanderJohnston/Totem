# Frontend User/Auth Feedback 2 Response

Date: 2026-06-30

Scope note: this MVP is **tracking identity**, not authorization. It does **not** add roles, permission gates, QueryHub auth, route blocking, command blocking, or access-denied UI. Manual/delegated/client-supplied `ProcessUserID` override remains unsupported.

## 1. Auth Endpoint Contracts

Also included here: `GET /api/auth/csrf`, because frontend requests need it for cookie-backed unsafe calls.

| Endpoint | Request body | Success response | Notes |
|---|---|---|---|
| `GET /api/auth/csrf` | none | `200 OK` with `{ "token": "...", "headerName": "X-CSRF-TOKEN" }` | Also sets readable `Totem.Csrf` cookie. `Cache-Control: no-store`. |
| `POST /api/auth/register` | `{ "userName": "ajohnston", "displayName": "Alex Johnston", "password": "..." }` | `200 OK` with `InteractionSession` | Required: `userName`, `password`. Optional: `displayName`. Sets auth cookie and signs in immediately. No success message field. |
| `POST /api/auth/login` | `{ "userName": "ajohnston", "password": "..." }` | `200 OK` with `InteractionSession` | Required: `userName`, `password`. Sets auth cookie. No success message field. |
| `POST /api/auth/logout` | none | `200 OK` with unidentified `InteractionSession` | Clears auth cookie if present. No success message field. |
| `GET /api/session` | none | `200 OK` with `InteractionSession` | Canonical session/tracking read endpoint. `Cache-Control: no-store`. |

`InteractionSession` shape:

```json
{
  "status": "identified",
  "displayLabel": "Alex Johnston",
  "windowsAccount": null,
  "userPrincipalName": null,
  "processUserId": "AJOHNSTON",
  "trackingSource": "backend-cookie"
}
```

Notes:
- `processUserId` is generated server-side from normalized username; the client does not send it.
- For backend-cookie users, `windowsAccount` and `userPrincipalName` are currently `null`.
- Successful `register`, `login`, `logout`, `csrf`, and `session` responses set `Cache-Control: no-store`.

## 2. Register Flow Behavior

After successful `POST /api/auth/register`, the backend **creates an authenticated session immediately**:
- account is created
- auth cookie is issued
- response body is the current `InteractionSession`

No separate login call is required.

## 3. Error Envelope And Status Codes

Auth/CSRF errors currently use this shape:

```json
{
  "message": "...",
  "errors": []
}
```

| Scenario | Status | Body |
|---|---|---|
| registration validation failure | `400 Bad Request` | `{ "message": "Validation failed.", "errors": ["..."] }` |
| duplicate registration | `409 Conflict` | `{ "message": "Registration could not be completed for that username.", "errors": [] }` |
| invalid credentials | `401 Unauthorized` | `{ "message": "Invalid username or password.", "errors": [] }` |
| authenticated unsafe request missing/invalid CSRF token | `400 Bad Request` | `{ "message": "CSRF token is required for this request.", "errors": [] }` |
| unsafe request with rejected `Origin`/`Referer` | `403 Forbidden` | empty body by current implementation |
| logout with no active session | `200 OK` | unidentified `InteractionSession` |
| `GET /api/session` with missing/expired cookie | `200 OK` | normal tracking body, usually `unidentified` unless another server-observed principal is active |

There is not a broader shared problem-details envelope for these auth endpoints today; use the shapes above.

Validation rules currently enforced on register:
- `userName`: required, 3-64 chars, letters/numbers plus `.`, `_`, `-`, `@`, and must contain at least one letter or number
- `password`: required, min 8 chars, max 256 chars
- `displayName`: optional

## 4. CSRF Requirements

Current strategy: double-submit token for cookie-backed unsafe requests.

Frontend behavior:
1. Call `GET /api/auth/csrf`
2. Read `{ token, headerName }`
3. Send header `X-CSRF-TOKEN: <token>` on authenticated unsafe requests

Current backend rules:
- `logout`: **yes**, CSRF header required when a backend auth cookie session is present
- `register`: not normally required before first sign-in; if called while already authenticated with a backend cookie, current global filter would require it
- `login`: same as register
- existing/future unsafe API methods (`POST`, `PUT`, `PATCH`, `DELETE`): same strategy applies when a backend-cookie principal is present
- unsafe requests also validate `Origin`/`Referer` when present

## 5. Cookie Settings By Environment

### Auth cookie (`Totem.Auth` by default)

| Setting | Development | Production / non-development |
|---|---|---|
| `HttpOnly` | `true` | `true` |
| `Secure` | `SameAsRequest` by default (so local `http://` dev is allowed unless `RequireSecureCookies=true`) | always secure |
| `SameSite` | `Lax` by default | `Lax` by default |
| domain | not explicitly set (host-only cookie) | not explicitly set (host-only cookie) |
| path | `/` | `/` |
| expiry | 8-hour auth ticket lifetime by default | 8-hour auth ticket lifetime by default |
| sliding expiration | `true` by default | `true` by default |
| persistence | non-persistent/session cookie (`IsPersistent=false`), no remember-me | same |

Frontend should **not** read the auth cookie directly; use `GET /api/session`.

### CSRF cookie (`Totem.Csrf` by default)

- readable by frontend (`HttpOnly=false`)
- path `/`
- `SameSite` matches auth cookie setting
- secure on HTTPS, or when `RequireSecureCookies=true`

## 6. `/api/session` Fetch Timing

Your preference is aligned with the backend contract.

Recommended:
- app boot: **yes**
- Miller page entry: **yes** if session state is page-scoped or may be stale
- after `register`: **yes recommended** (even though `register` already returns the session payload)
- after `login`: **yes recommended** (same note)
- after `logout`: **yes recommended**
- after any `401` or session-recovery event: **yes**

In short: treat `GET /api/session` as the authoritative session/tracking read after auth transitions.

## 7. `/api/session` Response Semantics

For this tracking-only phase, `GET /api/session` should be treated as:
- **`200 OK` for normal states**
- not a `401`/`403` endpoint for `identified` / `unmapped` / `unidentified`

Current contract remains:

```json
{
  "status": "identified | unmapped | unidentified",
  "displayLabel": "Unidentified user",
  "windowsAccount": null,
  "userPrincipalName": null,
  "processUserId": null,
  "trackingSource": "none"
}
```

Tracking-source semantics:
- `backend-cookie`: backend-owned cookie identity
- `windows-integrated-auth`: passive host principal observed
- `none`: unidentified

## 8. Cacheability And Refresh Expectations

Treat `GET /api/session` as **non-cacheable**.

Backend behavior:
- `Cache-Control: no-store`
- no ETag / conditional-fetch contract is defined for this endpoint

Frontend guidance:
- okay to memoize briefly in memory for UI state
- do a fresh fetch on startup and after auth transitions/recovery events

## 9. Dev Deployment Topology

Preferred/expected topology: **same-origin under relative `/api/...`**.

That means the current frontend relative `/api` pattern is correct.

Cross-origin cookie auth is only supported when:
- backend `Cors:AllowedOrigins` is explicitly configured, and
- frontend sends credentialed requests (`credentials: "include"`)

If `Cors:AllowedOrigins` is **not** configured, backend CORS falls back to wildcard/non-credentialed mode, which is **not** suitable for cross-origin cookie auth.

## 10. Identity Field Handling In Existing Write APIs

Frontend should treat client actor override fields as unsupported.

Current MVP behavior:
- do **not** send `ProcessUserID`, `processUserId`, `userId`, operator IDs, or delegated actor fields to choose tracking identity
- current Miller/Microfilm write DTOs do not expose actor fields for this purpose
- stray extra identity-like JSON properties are not persisted by the current DTOs
- this slice does **not** add authorization or reject normal writes based on `unmapped` / `unidentified`

## 11. Auth Response Messaging

No separate user-facing success message is returned today.

Use:
- the returned `InteractionSession` from `register` / `login` / `logout`, and/or
- a follow-up `GET /api/session`

## 12. Rate Limiting Or Lockout Behavior

Not implemented in the current MVP.

Current status:
- no login throttling contract
- no temporary lockout contract
- no `Retry-After` contract
- no registration cool-down contract

This is an explicit risk acceptance for the local/internal MVP only.

## 13. QueryHub Interaction

No change in this phase.

Frontend should assume QueryHub remains unchanged and is **not** being newly authenticated/authorized by this work.

## 14. Future Actor/Audit Metadata

Deferred / out of scope for this MVP.

This work does **not** add `lastChangedBy`, `lastChangedAt`, command actor stamping, or query-side audit metadata. If that is needed later, it should be scoped as separate work.
