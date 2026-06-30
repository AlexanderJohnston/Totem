# WASP Import Query Subscription Handoff

## Goal

The frontend needs to know when a WASP import has changed state so it can pull imported boxes and reconcile them into Miller regular rows.

The backend exposes WASP import status through:

```http
GET /api/microfilm/wasp/import/status
```

This endpoint is backed by `WaspImportStatusQuery`.

## QueryHub subscription

Frontend clients can subscribe to `WaspImportStatusQuery` changes through the existing SignalR QueryHub.

Hub path:

```text
/hubs/query
```

Recommended flow:

1. Fetch current WASP import status.
2. Read the response `ETag` header.
3. Connect to `/hubs/query`.
4. Invoke `SubscribeToChanged` with that ETag.
5. Listen for `onChanged`.
6. When `WaspImportStatusQuery` changes, re-fetch `GET /api/microfilm/wasp/import/status`.

Example:

```ts
const response = await fetch('/api/microfilm/wasp/import/status')
const etag = response.headers.get('ETag')
const status = await response.json()

connection.on('onChanged', async (newEtag: string) => {
  if (!newEtag.startsWith('WaspImportStatusQuery')) {
    return
  }

  const latest = await fetch('/api/microfilm/wasp/import/status')
  const latestStatus = await latest.json()

  // Decide whether to pull boxes and reconcile rows.
})

await connection.start()

if (etag) {
  await connection.invoke('SubscribeToChanged', etag)
}
```

The backend hub method is:

```csharp
SubscribeToChanged(string etag)
```

The backend notification event is:

```text
onChanged
```

with the updated ETag as the first argument.

## What the current status query can signal

Recent logs show `WaspImportStatusQuery` changing on intermediate and completion events:

```text
#94 => WaspImportStatusQuery   // WaspLegacyAssetsIgnored updated status
#96 => WaspImportStatusQuery   // WaspImportCompleted updated status
```

That means a frontend subscription can reliably learn that WASP import status changed, but the current payload does not reliably distinguish "intermediate update" from "import completed."

Current useful fields include:

| Field | Meaning |
| --- | --- |
| `lastImportedAssetCount` | Count of accepted assets recorded for the latest run. |
| `lastImportedAssetIds` | Asset IDs accepted during the latest run. |
| `lastIgnoredAssetCount` | Count of ignored legacy assets during the latest run. |
| `lastError` | Error text when an import/client failure was recorded. |
| `lastFailureStep` | Failure step when `lastError` is present. |

## Current frontend-safe behavior

Until the backend exposes explicit completion state, the frontend should treat `onChanged` for `WaspImportStatusQuery` as a **status changed** notification, not a guaranteed completion notification.

Safe approach:

1. Trigger import with `POST /api/microfilm/wasp/import/force`.
2. Subscribe to `WaspImportStatusQuery` changes.
3. On each `onChanged`, re-fetch import status.
4. Debounce reconciliation so intermediate changes do not cause repeated work.
5. Pull boxes and reconcile rows only when the status looks successful for the current run.

The current practical success heuristic is:

```ts
status.lastError == null && status.lastImportedAssetCount > 0
```

This is useful, but it is still a heuristic because the status payload does not expose an explicit `importInProgress` or `completed` flag.

## Recommended backend improvement

Add explicit run lifecycle fields to `WaspImportStatusQuery`, for example:

```json
{
  "importInProgress": false,
  "lastRunStartedAt": "2026-06-10T00:03:21-04:00",
  "lastRunCompletedAt": "2026-06-10T00:03:21-04:00",
  "lastCompletedPosition": 96,
  "lastError": null,
  "lastImportedAssetCount": 32
}
```

With that contract, frontend completion logic can be deterministic:

```ts
if (!status.importInProgress && status.lastRunCompletedAt && status.lastError == null) {
  await pullBoxesAndReconcileRows()
}
```

## Important distinction

QueryHub notifies that a query changed. It does not send the full query payload and it does not currently send a semantic event name like `WaspImportCompleted`.

The frontend should always re-fetch:

```http
GET /api/microfilm/wasp/import/status
```

after receiving `onChanged`.

## Reconciliation after completion

Once the frontend decides the import has completed successfully:

1. Fetch imported boxes:

   ```http
   GET /api/microfilm/boxes/by-client/{clientId}
   ```

2. Fetch existing Miller regular rows:

   ```http
   GET /api/microfilm/rows/{clientId}
   ```

3. Create missing regular rows:

   ```http
   POST /api/microfilm/rows/{clientId}
   ```

4. Re-fetch regular rows:

   ```http
   GET /api/microfilm/rows/{clientId}
   ```

This keeps the frontend aligned with the current backend contract: WASP import populates the box read model first, and frontend reconciliation creates the Miller regular rows.
