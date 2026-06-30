# Formatic Windows Auth Implementation Handoff

## Goal

Implement frontend/backend support for authenticating the frontend Formatic table with Quantum Web using the logged-in Windows identity.

Formatic Web currently relies on manual/demo `ProcessUserID` values. Replace that behavior with a backend-authenticated session flow so the frontend uses the logged-in Windows user's mapped Formatic operator/process user ID.

## Confirmed Decisions

- Formatic Web is guaranteed to be hosted locally on the same origin as Quantum Web.
- Add or use a same-origin backend session endpoint: `GET /api/session`.
- QueryHub authentication is out of scope for this phase. Do not implement auth for `/hubs/query` right now.
- UI should display the user's `displayName`.
- Authenticated users with no Formatic operator mapping should receive `403` with an `unmapped` reason.
- Authenticated users who are not allowed to use Formatic should receive `403` with a `forbidden` reason.
- Manual/delegated `ProcessUserID` override is not supported.
  - A logged-in user must not submit work as another operator.
  - Do not add override UI.
  - Do not preserve demo/manual fallback for authenticated workflows.
  - Backend command endpoints should reject, ignore, or overwrite client-submitted identity fields so clients cannot spoof another operator.
- QA will be performed locally on the dev machine using the logged-in Windows user `ajohnston`.

## Backend Target Contract

Implement or wire a session endpoint:

```http
GET /api/session
```

Successful response:

```json
{
  "authenticated": true,
  "windowsAccount": "DOMAIN\\ajohnston",
  "userPrincipalName": "ajohnston@example.com",
  "displayName": "Alex Johnston",
  "processUserId": "AJOHNSTON"
}
```

Recommended semantics:

| Status | Meaning |
|---|---|
| `200` | Windows user is authenticated and has a valid Formatic operator mapping. `processUserId` must be present. |
| `401` | User is unauthenticated or Windows auth challenge is required. |
| `403` + `reason: "unmapped"` | Windows user authenticated, but no Formatic operator mapping exists. |
| `403` + `reason: "forbidden"` | Windows user authenticated and known, but not allowed to use Formatic. |

If the codebase has an established API error shape, use it. Otherwise use a small stable JSON error shape:

```json
{
  "reason": "unmapped",
  "message": "Authenticated Windows account is not mapped to a Formatic operator."
}
```

Avoid returning stack traces, infrastructure details, group membership, or sensitive auth internals.

## Backend Implementation Expectations

Investigate existing Quantum Web / Outermind / Microfilm/Formatic API patterns first. Follow current controller, error, DTO, and test conventions.

The backend should:

1. Enable or consume Windows/Negotiate authentication if already configured.
2. Read the authenticated principal from the server request context.
3. Normalize the Windows identity into a canonical account key.
4. Map that account to the Formatic `processUserId`.
5. Return only minimal frontend-needed identity fields.
6. Ensure command endpoints that write or rely on `ProcessUserID` do not blindly trust client-submitted values.
7. Prefer deriving or validating the effective `ProcessUserID` server-side from the authenticated session.

If actual operator mapping storage does not exist yet, implement the smallest appropriate repository/service abstraction consistent with the codebase and document any temporary dev mapping needed for `ajohnston`.

## Frontend Implementation Expectations

Find the Formatic Web frontend code and existing API helper conventions.

Add:

1. A session API helper under the existing `src/api` pattern.
2. A session composable, store, or boot-time loader, depending on existing frontend architecture.
3. Explicit frontend states:
   - loading
   - authenticated
   - unauthenticated
   - unmapped
   - forbidden
   - network/backend error
4. Formatic table integration so table workflows use the backend-resolved `session.processUserId`.
5. UI display of `session.displayName`.
6. No manual/delegated `ProcessUserID` override UI.
7. No silent fallback to demo/manual identity if `/api/session` fails.

For same-origin requests, use normal same-origin browser credential behavior. If using `fetch`, `credentials: "same-origin"` is acceptable.

## UX Behavior

Recommended behavior:

- While session is loading: block table actions and show loading/skeleton state.
- `200`: render Formatic table using resolved `processUserId`; display `displayName`.
- `401`: show sign-in/authentication-required state with retry.
- `403 unmapped`: show account-not-mapped state with support guidance.
- `403 forbidden`: show access denied state.
- Network, `5xx`, or malformed payload: show retryable service/session error.
- Never continue with demo/manual `ProcessUserID` in production/authenticated flows.

## Tests and Validation

Before changing code, inspect existing test commands and conventions. Run relevant baseline tests if feasible, then run targeted tests after implementation.

### Backend

Add or update tests for:

- Authenticated mapped user returns `200` with `processUserId`.
- Unauthenticated request returns `401` or challenge behavior consistent with framework setup.
- Authenticated unmapped user returns `403` with `reason: "unmapped"`.
- Authenticated forbidden user returns `403` with `reason: "forbidden"`.
- Session response does not expose unnecessary identity/security details.
- Command endpoints do not trust spoofed client `ProcessUserID`.

### Frontend

Add or update tests for:

- Session helper maps `200`, `401`, `403 unmapped`, `403 forbidden`, network failure, and malformed payload.
- Formatic table waits for session before submitting identity-sensitive work.
- `displayName` is shown when authenticated.
- No override UI is present.
- No fallback to demo/manual `ProcessUserID` occurs when session fails.

### Manual Local QA

Validate on the dev machine as logged-in Windows user `ajohnston`:

1. Start the app locally in the normal same-origin configuration.
2. Call or load `GET /api/session`.
3. Confirm the response resolves `displayName` and `processUserId`.
4. Confirm the Formatic table uses the resolved `processUserId`.
5. Confirm attempts to spoof another `ProcessUserID` from the client are not accepted.

## Documentation Updates After Implementation

Update the appropriate docs/handoff area in `docs/` with the implemented contract and decisions, including:

- `/api/session` response contract.
- `401`, `403 unmapped`, and `403 forbidden` behavior.
- Same-origin assumption.
- No QueryHub auth in this phase.
- No manual/delegated `ProcessUserID` override.
- QA notes for local `ajohnston` validation.

## Important Constraints

- Preserve unrelated existing worktree changes. Do not reset, checkout, or revert files you did not intentionally modify.
- Make surgical changes that follow existing repository patterns.
- Do not implement QueryHub auth.
- Do not add manual override support.
- Do not trust client-submitted `ProcessUserID` as an audit/security identity.
