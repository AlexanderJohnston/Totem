# Scan and Processing Demo Frontend Handoff

Last updated: 2026-08-11

## Purpose

This branch supplies a disposable HTTP surface so the frontend team can build and demonstrate the Scan and Process interfaces before the durable backend workflow is finished.

It is intentionally fake. State is process-local memory, informational locations use the fictional `demo://` scheme, and restarting Quantum.Web clears everything. No route resolves a physical path, touches a filesystem, or performs production work.

Branch:

```text
demo/scan-processing-9am
```

## Starting the demo

The normal `Quantum.Web` launch profile uses `Development`, where the demo is enabled and anonymous callers receive a synthetic actor. The API adds these headers to every enabled demo response:

```text
X-Scan-Processing-Demo: true
Cache-Control: no-store
```

For a non-Development demo environment, set:

```text
ScanProcessingDemo__Enabled=true
ScanProcessingDemo__AllowSyntheticActor=true
```

Startup deliberately fails if `ScanProcessingDemo:Enabled` is true while `ASPNETCORE_ENVIRONMENT=Production`.

An optional request header gives separate browsers independent demo ownership:

```text
X-Scan-Processing-Demo-Actor: frontend-alice
```

Without it, anonymous Development requests use `demo-frontend`. A registered application user still takes precedence.

## Routes

All request and response JSON uses camel case.

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/scan-processing/rolls/{rollId}/rows/{rowId}/discovery` | Read row state, resource version, opaque configured resources, and processing settings |
| `POST` | `/api/scan-processing/rolls/{rollId}/rows/{rowId}/scan/start` | Accept a fake Start and enter `starting` |
| `POST` | `/api/scan-processing/rolls/{rollId}/rows/{rowId}/scans/{scanId}/finish` | Accept a fake Finish and enter `finishing` |
| `POST` | `/api/scan-processing/rolls/{rollId}/rows/{rowId}/processing/qpf-settings/preview` | Create a QPF settings preview plan |
| `POST` | `/api/scan-processing/rolls/{rollId}/rows/{rowId}/processing/frames-paths/preview` | Create a Frames destinations preview plan |
| `POST` | `/api/scan-processing/plans/{planId}/apply` | Queue a fake job from a current plan |
| `GET` | `/api/scan-processing/jobs/{jobId}` | Poll `queued`, `running`, `completed`, or `cancelled` |
| `POST` | `/api/scan-processing/jobs/{jobId}/cancel` | Cancel a queued or running job |
| `POST` | `/api/scan-processing/demo/reset` | Clear all demo state |

## Fast integration workflow

1. Call discovery with the row's canonical `rollId + rowId`.
2. Keep the returned `context.resourceVersion` and opaque resource-reference IDs.
3. For Scan, post Start with the discovery version and `scan.parent.id`. Poll discovery after about one second to see `active`. Post Finish with the newest discovery version, then poll to `idle`.
4. For Process, post either preview request with the newest resource version. Show the returned effects and issues, then post Apply with the returned plan ID/version.
5. Poll the job about once per second. It advances `queued` to `running` to `completed`.
6. Call reset whenever the demo needs to return to a clean state.

Every mutation needs a non-empty `idempotencyKey`. Retrying the same semantic request with the same key returns the original acceptance. Reusing a key with different input returns `IDEMPOTENCY_KEY_MISMATCH`.

## Minimal examples

Discovery:

```http
GET /api/scan-processing/rolls/roll-123/rows/row-456/discovery
```

Start:

```json
{
  "expectedResourceVersion": "rv1-0000000000000001",
  "parentResourceRefId": "demo-ref-use-the-value-from-discovery",
  "folderName": "Roll_123",
  "notes": "Morning demo",
  "idempotencyKey": "start-row-456-1"
}
```

QPF settings preview:

```json
{
  "expectedResourceVersion": "rv1-0000000000000001",
  "settings": {
    "polarity": "negative",
    "compression": "lossless",
    "rotation": "90"
  }
}
```

Apply:

```json
{
  "expectedPlanVersion": "pv1-use-the-value-from-preview",
  "acknowledgedIssueCodes": [],
  "idempotencyKey": "apply-row-456-1"
}
```

See [demo.frontend-surface.json](contracts/scan-processing/v1/demo.frontend-surface.json) for complete example shapes.

## Frontend behavior to preserve

- `starting` must not display Finish. Finish becomes available only in `active`.
- Process is eligible only in `idle`.
- Treat `resourceVersion`, resource-reference IDs, plan IDs/versions, scan IDs, and job IDs as opaque.
- Never parse `informationalFullPath` or send it back as authority.
- Render `issues[]` by `severity`, `code`, `message`, and optional `field`.
- Refresh discovery after transitions and after a processing job completes.

## Disposable boundary

This branch does not implement durable Start/Finish acceptance, KurrentDB operation topics, Operations-owned physical mapping, folder creation, QPF/Frames mutation, workers, crash recovery, production authentication qualification, or multi-instance state. Those remain post-demo work. Revert/delete this branch after the durable implementation replaces the façade.
