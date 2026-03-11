# How To Fix Stopped Flows

When a flow (Topic or Query) encounters an unhandled exception, Totem marks it as **stopped**. A stopped flow will not process any further events — not even after restarting the service. This guide explains why that happens and provides two methods to fix it, depending on the size of your timeline.

> **Applies to:** Totem with EventStoreDB (ESDB) v20+ / KurrentDB, using the gRPC client (`EventStore.Client.Grpc` v23.x).

---

## Table of Contents

1. [Understanding Why Flows Stop](#1-understanding-why-flows-stop)
2. [Diagnosing Stopped Flows](#2-diagnosing-stopped-flows)
3. [Method 1: Full Reset](#3-method-1-full-reset-re-process-all-events)
4. [Method 2: Surgical Fix](#4-method-2-surgical-fix-no-projection-reset)
5. [Prevention](#5-prevention)
6. [Reference](#6-reference)

---

## 1. Understanding Why Flows Stop

### The Error Chain

When a `Given` or `When` method throws an unhandled exception, Totem executes the following chain:

**Step 1 — Exception caught by FlowScope**

The `ObserveNextPoint()` loop in `FlowScope` wraps every event observation in a try/catch. Any exception — whether from a `Given` handler, a `When` handler, or an injected dependency — is caught here and routed to the `Stop()` method.

> Source: `src\Totem.Timeline\Runtime\FlowScope.cs` — `ObserveNextPoint()` catch block

**Step 2 — Error stamped on the FlowContext**

`Stop()` calls `Flow.Context.SetError(Point.Position, error.ToString())`, which records:
- `ErrorPosition` — the timeline position of the event that caused the failure
- `ErrorMessage` — the full exception text (including stack trace)

> Source: `src\Totem.Timeline\Runtime\FlowContext.cs` — `SetError()`

**Step 3 — Error checkpoint written to ESDB**

A checkpoint event is appended to the flow's checkpoint stream (e.g., `MyTopic-checkpoint` or `MyQuery|abc123-checkpoint`). The checkpoint metadata carries:

```json
{
  "position": 4200,
  "errorPosition": 4205,
  "errorMessage": "Grpc.Core.RpcException: Status(StatusCode=DeadlineExceeded...)",
  "isDone": false
}
```

> Note: Totem serializes all metadata using **camelCase** (configured via `JsonNamingPolicy.CamelCase`). This matches the property names used in `resume-projection.js`.

> Source: `src\Totem.Timeline.EventStore\EventStoreContextExtensions.cs` — `GetCheckpointEventData()`

**Step 4 — Resume projection marks the flow as stopped (permanently)**

The `resume-projection.js` runs as a continuous ESDB projection over `$all`. When it processes the error checkpoint, it executes:

```javascript
function updateInstanceProgress(instances, key) {
    updateInstance(instances, key, instance => {
        instance[1] = metadata.position;
        instance[2] = instance[2] || metadata.errorPosition !== null;
        //           ^^^^^^^^^^^^^^^^
        //           Once true, ALWAYS true. The || operator makes isStopped sticky.
    });
}
```

> Source: `src\Totem.Timeline.EventStore\resume-projection.js` — line 192

**Step 5 — Flow excluded from future resume**

The projection's `isResumable()` function filters out stopped flows:

```javascript
function isResumable([latest, checkpoint, isStopped]) {
    return !isStopped &&
        latest !== null &&
        (checkpoint === null || checkpoint < latest);
}
```

The flow is never included in the `routes` array of the resume state. On the next service startup, it simply does not exist in the list of flows to resume.

> Source: `src\Totem.Timeline.EventStore\resume-projection.js` — line 237

**Step 6 — ReadFlowCommand also blocks the flow (two code paths)**

Even if the flow somehow appears in the resume list, `ReadFlowCommand` reads the checkpoint stream and returns `FlowInfo.Stopped` when `ErrorPosition` is present. This blocks the flow via **two independent paths**:

1. **Resume path:** `ReadFlowToResumeCommand.Execute()` throws an exception when it encounters `FlowInfo.Stopped`, and the `FlowScope` terminates immediately.
2. **Live event path:** If a new event routes to a stopped flow during live processing, `FlowScope.TryStart()` also checks for `FlowInfo.Stopped` and terminates the scope via `CompleteTask()`.

Both paths ensure that a stopped flow can never process events again without manual intervention.

> Source: `src\Totem.Timeline.EventStore\DbOperations\ReadFlowCommand.cs` — `ReadFlow()` method
> Source: `src\Totem.Timeline.EventStore\DbOperations\ReadFlowToResumeCommand.cs` — `Execute()` line 42
> Source: `src\Totem.Timeline\Runtime\FlowScope.cs` — `TryStart()` lines 152–153

### Why Writing a Clean Checkpoint Alone Doesn't Fix It

Because of the `||` operator in line 192 of `resume-projection.js`, writing a new checkpoint with `ErrorPosition = null` does **not** clear the `isStopped` flag:

```
instance[2] = true || false  →  still true
```

This is by design — stopped flows require deliberate manual intervention. You must reset the resume projection's internal state to clear the flag (see Methods 1 and 2 below).

### Common Causes

| Error | Cause | Fix |
|-------|-------|-----|
| `GrpcDeadlineExceeded` | ESDB operation took longer than `Connection.Timeout` (default: 10 seconds) | Increase `totem.timeline.eventStore:Connection:Timeout` in `appsettings.json` |
| `Grpc.Core.RpcException` (various) | ESDB node unreachable, network issue, leader failover | Check ESDB cluster health, verify connection string |
| `NullReferenceException` / `InvalidOperationException` | Bug in `Given` or `When` handler code | Fix the handler code |
| `JsonException` / serialization errors | Flow state contains types that can't be serialized | Fix the flow's field types or add custom converters |
| `InvalidOperationException: "Then is not allowed..."` | Called `Then()` outside of a `When` method (e.g., unawaited async) | Ensure all async code in `When` methods is properly awaited |

---

## 2. Diagnosing Stopped Flows

### Step 1: Check Service Logs

When a flow stops, Totem logs an error message. Look for entries like:

```
Flow MyTopic stopped at position 4205
Flow MyQuery|abc123 stopped at position 12040 with this error: ...
Error while resuming MyTopic: Flow is stopped at 4205 with this error: ...
```

The log message includes:
- **Flow name** and optional **instance ID** (e.g., `MyQuery|abc123`)
- **Position** — the timeline position of the event that caused the failure
- **Error message** — the full exception text

### Step 2: Read the Checkpoint Stream

Every flow's checkpoint is stored in a dedicated ESDB stream. Read it to see the current state:

**Single-instance flow** (e.g., `SmartScanner`):
```bash
curl -s http://localhost:2113/streams/SmartScanner-checkpoint/head/backward/1 \
  -H "Accept: application/json" \
  -u admin:changeit | python -m json.tool
```

**Multi-instance flow** (e.g., `RollActivityQuery` with ID `user-42`):
```bash
curl -s "http://localhost:2113/streams/RollActivityQuery|user-42-checkpoint/head/backward/1" \
  -H "Accept: application/json" \
  -u admin:changeit | python -m json.tool
```

The response includes the checkpoint event. Look at the **metadata** for:
- `position` — last successfully processed timeline position
- `errorPosition` — the position where the error occurred (non-null = stopped)
- `errorMessage` — the exception text
- `isDone` — whether the flow has completed its lifecycle

### Step 3: Read the Resume Projection State

The resume projection's output tells you which flows are active, resumable, or stopped:

```bash
curl -s http://localhost:2113/projection/resume/state \
  -H "Accept: application/json" \
  -u admin:changeit | python -m json.tool
```

The response looks like:

```json
{
  "checkpoint": 50000,
  "routes": ["ShiftScheduleTopic", ["RollActivityQuery", "user-1", "user-2"]],
  "schedule": [120, 350]
}
```

**Stopped flows will NOT appear in the `routes` array.** If you know a flow should be processing events but it's not listed here, it's stopped.

### Step 4: Identify the Causal Event

Once you know the `errorPosition` (e.g., `4205`), read that event from the timeline stream:

```bash
curl -s http://localhost:2113/streams/timeline/4205 \
  -H "Accept: application/json" \
  -u admin:changeit | python -m json.tool
```

This shows you the exact event that caused the flow to stop — its type, data, and metadata. This information is critical for deciding whether to fix the root cause and **retry** the event, or **skip** it.

---

## 3. Method 1: Full Reset (Re-process All Events)

**Use this when:** You can afford to re-process all events for the affected flow(s). This is the simplest approach and guarantees a clean state.

**Trade-offs:**
- ✅ Simplest procedure — clean slate, no risk of inconsistent state
- ✅ Works for any number of stopped flows at once
- ❌ The affected flow(s) re-process ALL events from position 0 (could be millions)
- ❌ The resume projection replays all events in `$all` (takes time on large databases)

### Prerequisites

- Access to the ESDB Admin UI or HTTP API (port `2113` by default)
- Admin credentials (default: `admin` / `changeit`)
- The Totem service must be **stopped** before making changes

### Procedure

#### 1. Stop the Totem Service

Shut down the service process. This ensures no new checkpoints are written while you're making changes.

#### 2. Fix the Root Cause

Before clearing the stopped state, fix whatever caused the error:
- **GrpcDeadlineExceeded** → increase `Connection.Timeout` in `appsettings.json` (see [Prevention](#5-prevention))
- **Code bug** → fix and redeploy the handler code
- **Transient issue** → if the error was a one-time network blip, no code change is needed

#### 3. Soft-Delete the Checkpoint Stream

Delete the checkpoint stream for each stopped flow. This removes the error record and causes Totem to treat the flow as brand new on next startup.

**Single-instance flow:**
```bash
curl -i -X DELETE http://localhost:2113/streams/SmartScanner-checkpoint \
  -u admin:changeit
```

**Multi-instance flow:**
```bash
curl -i -X DELETE "http://localhost:2113/streams/RollActivityQuery|user-42-checkpoint" \
  -u admin:changeit
```

> **Important:** This is a soft delete (the default). The stream can be recreated, but old events remain in `$all` until scavenged. Do **not** use the `ES-HardDelete: true` header — hard-deleted streams cannot be recreated, and Totem needs to write new checkpoints to this stream name.
>
> ESDB docs: [Soft delete and TruncateBefore](https://developers.eventstore.com/server/v23.10/features/streams.html#soft-delete-and-truncatebefore)

#### 4. Run Scavenge

Scavenge physically removes the soft-deleted checkpoint events from `$all`. This is critical because the resume projection reads from `$all` — without scavenge, it would still see the old error checkpoints when it replays.

```bash
curl -i -d {} -X POST http://localhost:2113/admin/scavenge \
  -u admin:changeit
```

Wait for scavenge to complete. You can monitor progress via the ESDB Admin UI (`http://localhost:2113` → Admin tab) or check the logs.

> **Note:** On large databases, scavenge can take significant time and adds IO load. Consider running during off-peak hours. See [Scavenging best practices](https://developers.eventstore.com/server/v23.10/operations/scavenge.html#scavenging-best-practices).

#### 5. Delete the Resume Projection

Delete the `resume` projection so it can be recreated fresh:

```bash
curl -i -X DELETE "http://localhost:2113/projection/resume?deleteCheckpointStream=true&deleteStateStream=true&deleteEmittedStreams=true" \
  -u admin:changeit
```

> **What this does:** Removes the projection definition and its internal checkpoint. When the Totem service starts, `ResumeProjection.Synchronize()` detects that the projection is missing and recreates it from the `resume-projection.js` script embedded in the `Totem.Timeline.EventStore` assembly.
>
> Source: `src\Totem.Timeline.EventStore\ResumeProjection.cs` — `Synchronize()` method

#### 6. Soft-Delete the Resume Result Stream

The projection's output stream also needs to be cleared:

```bash
curl -i -X DELETE http://localhost:2113/streams/resume \
  -u admin:changeit
```

Also delete the routes streams that the projection created for the affected flows, since they contain links that may reference scavenged events:

**Single-instance flow:**
```bash
curl -i -X DELETE http://localhost:2113/streams/SmartScanner-routes \
  -u admin:changeit
```

**Multi-instance flow:**
```bash
curl -i -X DELETE "http://localhost:2113/streams/RollActivityQuery|user-42-routes" \
  -u admin:changeit
```

#### 7. Start the Totem Service

When the service starts:
1. `ResumeProjection.Synchronize()` sees the projection is missing and creates it
2. The projection replays all events from `$all`, rebuilding its state from scratch
3. Since the old error checkpoints were scavenged, no flows are marked as `isStopped`
4. Flows that had their checkpoint streams deleted are treated as **new** — they process all events from the beginning via their routes streams (also rebuilt by the projection)

> **For timelines with tens of millions of events:** The resume projection replay and flow re-processing can take a long time. Monitor the projection's progress via the Admin UI (`http://localhost:2113` → Projections tab). The service will begin processing live events once the projection catches up.

---

## 4. Method 2: Surgical Fix (No Projection Reset)

**Use this when:** You have a timeline with tens of millions of events and cannot afford to reset the resume projection. This method clears the stopped state of a single flow using the `isDone` checkpoint flag — a forward-only operation that the projection processes as new events in `$all`. No projection reset. No scavenge. No replay of millions of events.

**Trade-offs:**
- ✅ No projection reset — the resume projection continues running undisturbed
- ✅ No scavenge required — operates purely through forward checkpoint events
- ✅ Flow only re-processes events after its last checkpoint (minimal work)
- ✅ Preserves accumulated flow state
- ❌ More complex procedure with more steps
- ❌ If you choose to **skip** the error event, downstream state may be incomplete
- ❌ Zero data loss requires new events to route to the flow before the service starts (see [Step 7](#7-wait-for-the-flow-to-become-resumable))

### How It Works

The resume projection's `isStopped` flag uses `||`, making it permanently sticky:

```js
instance[2] = instance[2] || metadata.errorPosition !== null;  // true || false = true
```

Writing a clean checkpoint does **not** clear this — `true || false` is still `true`. But writing a checkpoint with `isDone: true` causes the projection to **delete the entire instance** from its state:

```js
// resume-projection.js — updateSingleInstanceProgress()
if(metadata.isDone) {
  delete state.instances[type];  // Instance removed entirely
}
```

> Source: `src\Totem.Timeline.EventStore\resume-projection.js` — lines 165-171 (single-instance), lines 174-187 (multi-instance)

Then writing a **second** checkpoint — clean, with no error and `isDone: false` — causes the projection to create a **brand-new instance** where `isStopped` starts fresh at `false`:

```js
// resume-projection.js — updateInstance()
instance = [latest, checkpoint, isStopped];
//          [null,   position,  false     ]   ← fresh start
```

> Source: `src\Totem.Timeline.EventStore\resume-projection.js` — `updateInstance()` creates `[null, null, false]`, then `updateInstanceProgress()` sets `instance[1] = metadata.position`

Both of these checkpoint events are processed by the projection as normal **forward events** in `$all`. There is no reset, no replay, and no scavenge involved.

### Prerequisites

Same as Method 1, plus:
- Familiarity with JSON event structure
- **An external event source that continues to write events while the service is stopped.** This is the key architectural requirement. In Outermind, the web application (`Outermind.Web`) is a separate process that writes command events (`StartScan`, `CreateCard`, `UpdateScanRegistry`, etc.) directly to EventStoreDB via `IClientDb.WriteEvent()`. Users continue interacting with the web UI while the Service is stopped, and those interactions generate the timeline events the projection needs to make the flow resumable. See [Step 7](#7-wait-for-the-flow-to-become-resumable) for details.
  > Source: `Outermind.Web\Controllers\ScanController.cs` — calls `_commands.Execute(new StartScan(...), When<ScanStarted>(), ...)` which writes the command event directly to EventStoreDB, independent of the Service process

### Procedure

#### 1. Stop the Totem Service

Shut down the service process. If you have a separate web application (like `Outermind.Web`), **keep it running** — user interactions with the web UI (scanning documents, creating cards, etc.) write command events directly to EventStoreDB. These events are what the resume projection needs to make the flow resumable in Step 7.

> **Why keep the web app running?** The web application writes events to EventStoreDB independently of the Service. For example, when a user triggers a scan via `ScanController.Start()`, the Web process calls `IClientDb.WriteEvent(new StartScan(...))`, which appends a `StartScan` event directly to the timeline stream. The resume projection (running on ESDB, not on the Service) sees this event, routes it to `SmartScanner`, and sets `latest` — which is exactly what makes the flow resumable.
>
> Source: `Outermind.Web\Controllers\ScanController.cs` → `ICommandServer.Execute()` → `IClientDb.WriteEvent()`

#### 2. Fix the Root Cause

Same as Method 1 — fix whatever caused the error before proceeding.

#### 3. Read the Current Error Checkpoint

Read the latest checkpoint from the affected flow's checkpoint stream to capture the current state:

```bash
curl -s http://localhost:2113/streams/SmartScanner-checkpoint/head/backward/1 \
  -H "Accept: application/json" \
  -u admin:changeit
```

From the response, note:
- **Event body (`data`)** — the serialized flow state (JSON). You'll reuse this in Step 6.
- **Metadata `position`** — the last successfully processed timeline position (e.g., `4200`)
- **Metadata `errorPosition`** — the position that caused the failure (e.g., `4205`)
- **Metadata `errorMessage`** — the exception details

#### 4. Decide: Retry or Skip the Error Event

You have two choices:

| Strategy | When to Use | Position Value |
|----------|-------------|----------------|
| **Retry** | You fixed the code/config that caused the error and want the flow to re-process the event that failed | Set `position` to the same value as the current checkpoint's `position` (e.g., `4200`). The flow will re-process events starting from `4201` onward, including `4205`. |
| **Skip** | The error event is inherently problematic (e.g., malformed data) and you want to skip past it | Set `position` to the `errorPosition` value (e.g., `4205`). The flow will process events starting from `4206` onward. |

> **⚠️ Warning about Skip:** When you skip an event, the flow's state will not reflect that event's data. If downstream `Given` or `When` handlers depend on state set by the skipped event, the flow may behave incorrectly. Only skip if you understand the consequences.

#### 5. Write the `isDone` Checkpoint

Write a checkpoint event with `isDone: true` to delete the flow's instance from the projection's tracking. This breaks the sticky `isStopped` flag by removing the entire instance:

```bash
curl -i -X POST http://localhost:2113/streams/SmartScanner-checkpoint \
  -H "Content-Type: application/vnd.eventstore.events+json" \
  -u admin:changeit \
  -d '[{
    "eventId": "GENERATE-A-UUID-HERE",
    "eventType": "timeline:Checkpoint",
    "data": <PASTE_FLOW_STATE_JSON_HERE>,
    "metadata": {
      "position": 4200,
      "errorPosition": null,
      "errorMessage": null,
      "isDone": true
    }
  }]'
```

> **What this does in the projection:** The resume projection processes this event from `$all` and calls `delete state.instances["SmartScanner"]`. The flow instance is completely removed from tracking. The `isStopped` flag no longer exists because the instance no longer exists.
>
> Source: `src\Totem.Timeline.EventStore\resume-projection.js` — `updateSingleInstanceProgress()` deletes `state.instances[type]` when `metadata.isDone` is truthy
>
> For multi-instance flows, the projection calls `delete instanceIds[id]` — removing only the specific instance, not the entire flow type.

> **Generating a UUID:** Use `uuidgen` (Linux/macOS), `[guid]::NewGuid()` (PowerShell), or any UUID generator. Each event must have a unique ID.

#### 6. Write the Clean Checkpoint

Immediately write a second checkpoint event with the correct position, no error, and `isDone: false`:

```bash
curl -i -X POST http://localhost:2113/streams/SmartScanner-checkpoint \
  -H "Content-Type: application/vnd.eventstore.events+json" \
  -u admin:changeit \
  -d '[{
    "eventId": "GENERATE-ANOTHER-UUID-HERE",
    "eventType": "timeline:Checkpoint",
    "data": <PASTE_FLOW_STATE_JSON_HERE>,
    "metadata": {
      "position": 4200,
      "errorPosition": null,
      "errorMessage": null,
      "isDone": false
    }
  }]'
```

Adjust `position` based on your retry/skip decision from Step 4.

> **What this does in the projection:** Since the instance was deleted in Step 5, the projection creates a brand-new instance: `[null, null, false]`. It then sets `instance[1] = 4200` (position) and evaluates `instance[2] = false || false` → `false` (not stopped). The final state is `[null, 4200, false]`.
>
> Source: `src\Totem.Timeline.EventStore\resume-projection.js` — `updateInstance()` creates the fresh tuple, `updateInstanceProgress()` sets position and evaluates `isStopped`

> **Note on `maxCount: 1`:** Totem sets `$maxCount: 1` on checkpoint streams when they're first created. The clean checkpoint becomes the only logically visible event in the stream. When `ReadFlowCommand` reads this stream on the next startup, it sees a checkpoint with no error and `isDone: false` → returns `FlowInfo.Loaded`.
>
> Source: `src\Totem.Timeline.EventStore\TimelineDb.cs` — `TryWriteInitialMetadata()` sets `maxCount: 1`
>
> Source: `src\Totem.Timeline.EventStore\DbOperations\ReadFlowCommand.cs` — returns `FlowInfo.Loaded` when `!_metadata.IsDone && !_metadata.ErrorPosition.IsSome`

#### 7. Wait for the Flow to Become Resumable

After Steps 5-6, the flow instance exists in the projection as `[null, 4200, false]` — meaning `latest = null`. The projection's `isResumable()` function requires `latest !== null`:

```js
function isResumable([latest, checkpoint, isStopped]) {
  return !isStopped && latest !== null && (checkpoint === null || checkpoint < latest);
}
```

The flow **will not appear** in the resume `routes` until a new timeline event routes to it. When that happens, the projection sets `latest = state.checkpoint` (the `$all` position of that event), and the flow becomes resumable.

> Source: `src\Totem.Timeline.EventStore\resume-projection.js` — `isResumable()` at line 237; `updateInstanceLatest()` sets `instance[0] = state.checkpoint`

**Monitor the projection state** to verify the flow has become resumable:

```bash
curl -s http://localhost:2113/projection/resume/state \
  -H "Accept: application/json" \
  -u admin:changeit
```

Look for the flow name in the `routes` array. For a single-instance flow like `SmartScanner`, you should see it as a string entry:

```json
{
  "routes": [
    "SmartScanner",
    ...
  ]
}
```

For a multi-instance flow, look for an array entry where the first element is the flow type name:

```json
{
  "routes": [
    ["BoxInventory", "client:pallet:box"],
    ...
  ]
}
```

Once the flow appears in `routes`, proceed to Step 8.

> **Where do the new events come from?** The events must be written to EventStoreDB by a process **other than the stopped Service**. In Outermind's architecture:
> - `Outermind.Web` writes command events when users interact with the UI:
>   - `ScanController.Start()` → writes `StartScan` → routes to `SmartScanner`
>   - `ScanController.UpdateRegistry()` → writes `UpdateScanRegistry` → routes to `SmartScanner`
>   - `CardController.Create()` → writes `CreateCard` → routes to `CardManager`
> - These commands are written directly to EventStoreDB via `IClientDb.WriteEvent()` — the Service is not involved
> - The resume projection runs on ESDB itself, not on the Service — it processes these events and updates flow tracking immediately
>
> In other architectures, events might come from: other microservices writing to the shared EventStoreDB, scheduled tasks, external integrations, or any process with a `TimelineClient` connection.
>
> Source: `Outermind.Web\Controllers\ScanController.cs`, `CardController.cs` — all use `ICommandServer.Execute()` → `IClientDb.WriteEvent()`

> **⚠️ Critical — Why Waiting Matters**
>
> If you start the service before `latest` is set, the flow will **not** go through the resume path. Instead, when a live event arrives, `FlowScope.TryStart()` fires — it reads the clean checkpoint (`FlowInfo.Loaded`), hydrates the flow, and processes the live event directly. The resume path (`ReadFlowToResumeCommand`) is never called, and **all events between the checkpoint position and the first live event are permanently lost**.
>
> **Concrete example:** SmartScanner's clean checkpoint is at position 4200. While the Service was down, users scanned 15 documents via the web UI, generating command events at positions 4210, 4300, 4500, ... 20000. You start the Service without waiting. The first live event arrives at position 50005. `TryStart` loads the flow at 4200, processes event 50005, and writes a checkpoint at 50005. Those 15 scan events (positions 4210-20000) are **permanently sealed off** — the resume path's backward reader stops at `EventNumber <= _areaCheckpoint` (now 50005), so it will never walk back far enough to find them. SmartScanner's registry is permanently incomplete.
>
> This happens because the resume path is only initiated by `FlowHost.Resume()` at startup, which only processes flows listed in the projection's `routes` array. Flows not in `routes` are handled by the live `TryStart` path, which does not read the routes stream for gap events.
>
> Source: `src\Totem.Timeline\Runtime\FlowScope.cs` — `TryStart()` loads the flow but does not call `Resume()`; `ObserveQueue()` only calls `Resume()` when `_resumeWhenConnected` is `true` (set exclusively by `FlowHost.Resume()`)
>
> Source: `src\Totem.Timeline\Runtime\FlowHost.cs` — `Resume()` iterates only the routes from the projection state
>
> Source: `src\Totem.Timeline.EventStore\DbOperations\ReadFlowToResumeCommand.cs` — `ReadNextBatch()` stops at `e.Event.EventNumber <= _areaCheckpoint`, permanently excluding events below the checkpoint

> **What if no external events are available?**
>
> If your architecture has no separate event source (e.g., the stopped Service is the only process that writes to EventStoreDB), no new events can route to the flow while the Service is stopped. The flow's `latest` will never be set, and it will never appear in the resume routes. You have two options:
>
> 1. **Accept that gap events will be permanently lost.** Start the service immediately (proceed to Step 8). The flow will un-stop and process new events correctly from its checkpoint position, but every event between the checkpoint and the first live event will never be processed. This is a **permanent data loss** for the flow's state — those events' effects (state changes from `Given` handlers, commands from `When` handlers) are gone. Only consider this when:
>    - The gap is genuinely small (e.g., you used the "skip" strategy and the only gap event is the error event itself, which you intentionally want to skip)
>    - The flow can tolerate missing data (e.g., a "latest status" flow where only the most recent event matters, not historical ones)
>
> 2. **Use Method 1 instead.** If the missed events would corrupt the flow's state or cause downstream problems, use the full reset approach. Method 1 re-processes all events from the beginning, guaranteeing nothing is missed.

#### 8. Start the Totem Service

Once the flow appears in the resume `routes`, start the service:

1. `SubscribeCommand` reads the `resume` projection state from the `resume` stream
2. The previously-stopped flow appears in the `routes` array (because `isStopped` is `false` and `latest` is set)
3. `FlowHost.Resume()` creates a scope for the flow and calls `ResumeWhenConnected()`, setting `_resumeWhenConnected = true`
4. `FlowScope.ObserveQueue()` sees the flag and calls `Resume()` → triggers `ReadFlowToResumeCommand`
5. `ReadFlowCommand` reads the checkpoint stream → finds the clean checkpoint from Step 6 → returns `FlowInfo.Loaded`
6. `_areaCheckpoint` is set to `4200` (the position from the clean checkpoint)
7. `ReadFlowToResumeCommand` reads the routes stream **backwards**, collecting all events with `EventNumber > 4200` — these are the **gap events**
8. `FlowQueue.ResumeWith()` loads gap events into `_resumeQueue` (in chronological order)
9. `FlowQueue.Dequeue()` drains the resume queue **before** the live queue — gap events are processed first
10. Once all gap events are processed, the flow switches to the live queue and continues normally

**Result:** The flow resumes from exactly position `4200`, processes all gap events in order, then transitions to live processing. **Zero events are missed.**

> Source: `src\Totem.Timeline.EventStore\DbOperations\ReadFlowToResumeCommand.cs` — `ReadPoints()` collects routes stream events beyond `_areaCheckpoint`
>
> Source: `src\Totem.Timeline\Runtime\FlowQueue.cs` — `Dequeue()` calls `TryDequeueResumePoint()` before `TryDequeuePoint()`; live events at or before `_resumeCheckpoint` are deduplicated

### Fixing Multiple Stopped Flows

Repeat Steps 3-6 for each stopped flow. After writing all the `isDone` + clean checkpoints, wait for **all** flows to appear in the resume routes (Step 7) before starting the service (Step 8).

For multi-instance flows, use the full checkpoint stream name including the instance ID:

```
# Single-instance
SmartScanner-checkpoint

# Multi-instance (pipe-separated)
BoxInventory|client:pallet:box-checkpoint
```

> Source: `src\Totem.Timeline.EventStore\TimelineStreams.cs` — `GetCheckpointStream()` uses `$"{key.Type}|{key.Id}-checkpoint"` for multi-instance flows

---

## 5. Prevention

### Increase Connection Timeout

The default ESDB connection timeout is 10 seconds. For environments with high load or slow networks, this is often too short. Increase it in `appsettings.json`:

```json
{
  "totem.timeline.eventStore": {
    "connection": {
      "timeout": "00:15:00"
    }
  }
}
```

This value becomes `EventStoreClientSettings.DefaultDeadline` and applies to **all** gRPC operations — reads, writes, and subscription calls.

> Source: `src\Totem.Timeline.EventStore\Hosting\EventStoreServiceExtensions.cs` — `BuildClientSettings()` sets `s.DefaultDeadline = options.Connection.Timeout`

### Wrap External Calls in Try/Catch

Totem's recommended pattern for handling errors in Topics is to catch exceptions and emit an error event instead of letting the exception propagate (which stops the flow):

```csharp
public class ImportSteps : Topic
{
    async Task When(ImportStarted e, IProductFile file)
    {
        try
        {
            var diff = await file.DiffProducts(_products);
            Then(new ProductsDiffed(diff));
        }
        catch(Exception error)
        {
            Then(new ImportFailed(error.ToString()));
        }
    }
}
```

The `ErrorEvent` base class provides a standard pattern:

```csharp
// Inherit from ErrorEvent for consistent error events
public class ImportFailed : ErrorEvent
{
    public ImportFailed(string error) : base(error) { }
}
```

> Source: `src\Totem.Timeline\ErrorEvent.cs` — abstract base class carrying `string Error`
>
> Pattern demonstrated in: `How To\How Totem Works.md` — the `ImportSteps` Topic example

**Key insight:** `ErrorEvent` does **not** stop the flow. It's just a regular event that carries error information. The flow continues processing after emitting it. Queries can observe it to surface the error to users via the API/UI.

### Monitor for Stopped Flows

Look for these patterns in service logs:
- `"Flow {Key} stopped at position {Position}"` — a flow has just stopped
- `"Error while resuming {Key}"` — a previously-stopped flow was encountered during startup
- `"Failed to write stoppage of {Key}"` — the error checkpoint couldn't even be written (double failure)

### Design Idempotent Handlers

When a flow retries an event after being fixed, the `Given` and `When` handlers run again for that event. Ensure your handlers are safe to re-execute:
- Avoid side effects in `Given` methods (they should only update flow state)
- Make `When` methods idempotent where possible (check before acting)

---

## 6. Reference

### Checkpoint Stream Naming

| Flow Type | Example | Checkpoint Stream | Routes Stream |
|-----------|---------|-------------------|---------------|
| Single-instance Topic | `SmartScanner` | `SmartScanner-checkpoint` | `SmartScanner-routes` |
| Single-instance Query | `StackQuery` | `StackQuery-checkpoint` | `StackQuery-routes` |
| Multi-instance Topic | `BoxInventory` with ID `client:pallet:box` | `BoxInventory\|client:pallet:box-checkpoint` | `BoxInventory\|client:pallet:box-routes` |
| Multi-instance Query | `RollActivityQuery` with ID `user-42` | `RollActivityQuery\|user-42-checkpoint` | `RollActivityQuery\|user-42-routes` |

> Source: `src\Totem.Timeline.EventStore\TimelineStreams.cs` — `GetCheckpointStream()` and `GetRoutesStream()`

### Resume Projection State Format

The `resume` projection outputs a JSON state to the `resume` result stream:

```json
{
  "checkpoint": 50000,
  "routes": [
    "SmartScanner",
    "ShiftScheduleTopic",
    ["RollActivityQuery", "user-1", "user-2", "user-3"],
    ["BoxRollList", "clientA:pallet1:box1"]
  ],
  "schedule": [120, 350, 4001]
}
```

- **`checkpoint`** — the latest timeline position the projection has processed
- **`routes`** — flows with pending work. Strings are single-instance; arrays are multi-instance (first element is the type name, rest are instance IDs). **Stopped flows are excluded.**
- **`schedule`** — timeline positions of scheduled future events

### Key ESDB HTTP API Commands

| Operation | Command |
|-----------|---------|
| Read last checkpoint | `curl -s http://localhost:2113/streams/{flow}-checkpoint/head/backward/1 -H "Accept: application/json" -u admin:changeit` |
| Read timeline event | `curl -s http://localhost:2113/streams/timeline/{position} -H "Accept: application/json" -u admin:changeit` |
| Read resume state | `curl -s http://localhost:2113/projection/resume/state -H "Accept: application/json" -u admin:changeit` |
| Soft-delete stream | `curl -i -X DELETE http://localhost:2113/streams/{stream} -u admin:changeit` |
| Delete projection | `curl -i -X DELETE "http://localhost:2113/projection/resume?deleteCheckpointStream=true&deleteStateStream=true&deleteEmittedStreams=true" -u admin:changeit` |
| Disable projection | `curl -i -X POST http://localhost:2113/projection/resume/command/disable -u admin:changeit` |
| Reset projection | `curl -i -X POST http://localhost:2113/projection/resume/command/reset -u admin:changeit` |
| Enable projection | `curl -i -X POST http://localhost:2113/projection/resume/command/enable -u admin:changeit` |
| Start scavenge | `curl -i -d {} -X POST http://localhost:2113/admin/scavenge -u admin:changeit` |
| Check scavenge status | `curl -i -X GET http://localhost:2113/admin/scavenge/current -u admin:changeit` |

### Key Source Files

| File | Purpose |
|------|---------|
| `src\Totem.Timeline\Flow.cs` | Base class for all flows (Topics and Queries) |
| `src\Totem.Timeline\Runtime\FlowScope.cs` | Flow lifecycle: queue observation, error handling, `Stop()` method |
| `src\Totem.Timeline\Runtime\FlowContext.cs` | Flow metadata: checkpoint position, error position, `SetError()` |
| `src\Totem.Timeline.EventStore\resume-projection.js` | ESDB projection tracking resumable flows — the `isStopped` flag logic |
| `src\Totem.Timeline.EventStore\ResumeProjection.cs` | Ensures the resume projection exists in ESDB on startup |
| `src\Totem.Timeline.EventStore\TimelineDb.cs` | Database operations: `WriteCheckpoint()`, client notifications |
| `src\Totem.Timeline.EventStore\EventStoreContextExtensions.cs` | Serialization of checkpoint events with `CheckpointMetadata` |
| `src\Totem.Timeline.EventStore\CheckpointMetadata.cs` | Data class: `Position`, `ErrorPosition`, `ErrorMessage`, `IsDone` |
| `src\Totem.Timeline.EventStore\DbOperations\ReadFlowCommand.cs` | Reads checkpoint and returns `FlowInfo.Stopped` if error exists |
| `src\Totem.Timeline.EventStore\DbOperations\ReadFlowToResumeCommand.cs` | Loads flow state and pending events for resuming |
| `src\Totem.Timeline.EventStore\TimelineStreams.cs` | Stream name conventions: checkpoint, routes, schedule |
| `src\Totem.Timeline.EventStore\Hosting\EventStoreServiceExtensions.cs` | Connection configuration, `DefaultDeadline` setting |
| `src\Totem.Timeline\ErrorEvent.cs` | Base class for application-level error events |

### ESDB / KurrentDB Documentation Links

- [Scavenging operations](https://developers.eventstore.com/server/v23.10/operations/scavenge.html) — how to remove deleted events from `$all`
- [Deleting streams and events](https://developers.eventstore.com/server/v23.10/features/streams.html#deleting-streams-and-events) — soft delete vs hard delete behavior
- [Deleted events and projections](https://developers.eventstore.com/server/v23.10/features/streams.html#deleted-events-and-projections) — why projections see deleted events before scavenge
- [User-defined projections](https://developers.eventstore.com/server/v23.10/features/projections/custom.html) — projection management and configuration
- [Stream metadata](https://developers.eventstore.com/server/v23.10/features/streams.html#stream-metadata) — `$maxCount`, `$maxAge`, `$tb` settings
