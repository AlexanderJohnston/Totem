# Scan and Processing Demo Frontend Handoff

Last updated: 2026-08-11

## Purpose

This branch supplies a disposable HTTP surface so the frontend team can build and demonstrate the Scan and Process interfaces before the durable backend workflow is finished. It also has an opt-in physical happy path for the morning demonstration.

Operation state remains process-local memory and restarting Quantum.Web clears it. With the physical workflow disabled, informational locations use the fictional `demo://` scheme and no route touches files. With it explicitly enabled, Quantum.Web synchronously creates one scan folder, reads one IDF at Finish, and backup-replaces one QPF at Apply. Production startup remains blocked.

Branch:

```text
demo/scan-processing-9am
```

## Starting the demo

The normal `Quantum.Web` launch profile uses `Development`, where the demo is enabled, anonymous callers receive a synthetic actor, and the physical workflow uses the local `App_Data\scan-processing-demo` root. The root is created on first Start. The accepted Start response always returns the resolved full folder path, so the frontend does not need to know the server working directory. The API adds these headers to every enabled demo response:

```text
X-Scan-Processing-Demo: true
Cache-Control: no-store
```

For a non-Development demo environment, set:

```text
ScanProcessingDemo__Enabled=true
ScanProcessingDemo__AllowSyntheticActor=true
```

To override the default local rehearsal root, set:

```text
ScanProcessingDemo__PhysicalWorkflowEnabled=true
ScanProcessingDemo__PhysicalScanRoot=C:\TotemDemoScans
```

The path is an example only. The Quantum.Web process identity needs read/write/create access to the selected root. Use the default or another dedicated local rehearsal root first; qualify any UNC root separately before relying on it during the demo.

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
| `POST` | `/api/scan-processing/rolls/{rollId}/rows/{rowId}/scan/start` | Enter `starting`; physical mode creates and returns a new direct-child scan folder |
| `POST` | `/api/scan-processing/rolls/{rollId}/rows/{rowId}/scans/{scanId}/finish` | Physical mode counts one top-level IDF and durably writes the row's numeric `imageCount` cell |
| `POST` | `/api/scan-processing/rolls/{rollId}/rows/{rowId}/processing/qpf-settings/preview` | Create a QPF settings preview plan |
| `POST` | `/api/scan-processing/rolls/{rollId}/rows/{rowId}/processing/frames-paths/preview` | Create a Frames destinations preview plan |
| `POST` | `/api/scan-processing/plans/{planId}/apply` | Apply a plan; physical QPF Apply completes synchronously with a verified backup |
| `GET` | `/api/scan-processing/jobs/{jobId}` | Poll `queued`, `running`, `completed`, or `cancelled` |
| `POST` | `/api/scan-processing/jobs/{jobId}/cancel` | Cancel a queued or running job |
| `POST` | `/api/scan-processing/demo/reset` | Clear all demo state |

## Fast integration workflow

1. Call discovery with the row's canonical `rollId + rowId`.
2. Keep the returned `context.resourceVersion` and opaque resource-reference IDs.
3. For Scan, post Start with the discovery version and `scan.parent.id`. In physical mode the accepted scan includes `informationalFullPath`; drop the pre-scanned files into that folder. Poll discovery after about one second to see `active`.
4. Leave exactly one top-level `.idf` and one top-level `.qpf` in that folder. Post Finish with the newest discovery version. Its response includes `imageCount` and `idfFileName`, and the numeric `imageCount` cell is appended durably to the canonical row.
5. Poll discovery to `idle`. Post the QPF settings preview with the newest resource version, show its effects/issues, then post Apply with the returned plan ID/version.
6. Physical QPF Apply returns a `completed` job with `qpfApply.qpfFileName`, `backupFileName`, and `updatedDetectionSettings`. Without physical mode, polling advances `queued` to `running` to `completed`.
7. Call reset whenever the demo needs to return to a clean state.

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
    "contrast": "72",
    "brightness": "0",
    "gamma": "-16",
    "sharpen": "6",
    "auto-crop": "true",
    "auto-deskew": "true",
    "rotate": "90",
    "save-grayscale": "true",
    "save-bitonal": "false"
  }
}
```

Supported demo QPF setting IDs are:

| ID | Accepted value |
| --- | --- |
| `contrast`, `brightness`, `gamma` | Integer `-255..255` |
| `sharpen` | Integer `0..10` |
| `auto-crop`, `auto-deskew`, `save-grayscale`, `save-bitonal` | `true`/`false` or `1`/`0` |
| `rotate` | `0`, `90`, `180`, or `270` |
| `flip` | Integer `0..2` |
| `grayscale-format` | Integer `0..5` |
| `bitonal-format` | Integer `0..4` |
| `crop-border` | Integer `0..150` |
| `crop-threshold` | Integer `0..128` |
| `deskew-quality` | Integer `0..5` |

Apply updates those attributes on every direct `/Roll/ScannerSettings/DetectionSettings` element, sets `ProcessSettingsExist=1`, preserves other XML, writes and validates a same-directory stage, verifies an exact timestamped backup, and then replaces the QPF. Any parse, backup, validation, or replacement failure returns an issue instead of reporting success.

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
- Ensure the selected client profile exposes a number column whose ID is exactly `imageCount`; the durable row cell exists even if a profile currently hides it.

## Disposable boundary

This branch does not implement durable Start/Finish acceptance, KurrentDB operation topics, Operations-owned physical mapping, workers, crash recovery, production authentication qualification, multi-instance state, or a general QPF schema. The optional physical path runs synchronously inside Quantum.Web and is deliberately unsuitable for production. Reset clears only in-memory state; it never deletes rehearsal folders, IDFs, QPFs, or backups. Revert/delete this branch after the durable implementation replaces the façade.
