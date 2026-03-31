# Timeline Subscription Architecture: Research & Refactor Proposal

**Branch:** `work-dev-wasp-oldResume`
**Repo:** `https://github.com/AlexanderJohnston/Totem`
**Upstream:** `https://github.com/bwatts/Totem`

---

## Table of Contents

1. [Overview](#overview)
2. [Current Architecture](#current-architecture)
   - [Startup & Resume Flow](#startup--resume-flow)
   - [The Resume Projection](#the-resume-projection)
   - [The `-routes` Streams](#the--routes-streams)
   - [The TimelineSubscription](#the-timelinesubscription)
   - [FlowHost & Fan-out](#flowhost--fan-out)
   - [FlowScope & FlowQueue](#flowscope--flowqueue)
   - [Checkpoints](#checkpoints)
3. [Identified Problems](#identified-problems)
   - [P1 — Single Global Checkpoint Couples All Flows](#p1--single-global-checkpoint-couples-all-flows)
   - [P2 — The Resume Projection Does Too Much](#p2--the-resume-projection-does-too-much)
   - [P3 — Full Backwards Read for New Flows](#p3--full-backwards-read-for-new-flows)
   - [P4 — ResumeAlgorithm Bug (Math.Max vs Math.Min)](#p4--resumealgorithm-bug-mathmax-vs-mathmin)
   - [P5 — Resume Projection Reset Invalidates All Flows](#p5--resume-projection-reset-invalidates-all-flows)
4. [Proposed Refactor: Per-Flow Catch-Up Subscriptions](#proposed-refactor-per-flow-catch-up-subscriptions)
   - [Core Idea](#core-idea)
   - [What Changes](#what-changes)
   - [What Can Be Removed](#what-can-be-removed)
   - [Rough Sketch](#rough-sketch)
5. [Open Questions for the Team](#open-questions-for-the-team)
6. [File Reference Map](#file-reference-map)

---

## Overview

The Totem timeline uses EventStore as its backing store. On startup, all flows (topics and queries) are resumed from a centralized EventStore projection called `resume`. This document maps out how that mechanism works today, identifies structural problems with the current approach, and proposes a refactor toward per-flow catch-up subscriptions that each manage their own checkpoint position independently.

---

## Current Architecture

### Startup & Resume Flow

```
TimelineHost.Open()
  ├── _db.Connect(this)
  └── ResumeSubscription()
        ├── _db.Subscribe(this)                        → SubscribeCommand.Execute()
        │     ├── reads "resume" stream (1 event, backwards)
        │     │     ├── checkpoint   → global timeline position
        │     │     ├── routes[]     → FlowKeys with pending work
        │     │     └── schedule[]   → positions of scheduled events
        │     └── returns ResumeInfo
        ├── _flows.Resume(resumeInfo.Routes)
        │     └── for each FlowKey in Routes:
        │           ├── ReadFlowToResumeCommand.Execute()
        │           │     ├── ReadFlowCommand  → reads {key}-checkpoint (current state)
        │           │     ├── ReadLastRoute()  → reads end of {key}-routes (1 event)
        │           │     └── ReadBatch(...)   → reads {key}-routes backwards in batches
        │           └── FlowScope.ResumeWith(points)  → seeds FlowQueue
        ├── _schedule.Resume(resumeInfo.Schedule)
        └── Track(resumeInfo.Subscription)             → TimelineSubscription (live catch-up)
```

### The Resume Projection

`resume-projection.js` is a **server-side EventStore projection** that observes all streams and maintains a result state, written to a `resume` stream. It performs three duties simultaneously:

1. **Links area events into per-flow `-routes` streams** — via `linkTo(type + "-routes", event)` for single-instance flows and `linkTo(\`${type}|${id}-routes\`, event)` for multi-instance flows.

2. **Maintains a routes registry** — tracks which flow instances have pending work (i.e., `latest > checkpoint && !isStopped`). This is the `routes[]` array in the resume state.

3. **Maintains a schedule** — tracks positions of events that have a `whenOccurs` metadata field and removes them when their cause is resolved.

The projection also stores the **global timeline checkpoint** — the last processed position of the `timeline` stream — which is used to decide where `TimelineSubscription` restarts.

### The `-routes` Streams

Each flow type/instance gets a dedicated link stream in EventStore:

| Flow Type | Stream Name |
|---|---|
| Single-instance | `{FlowTypeName}-routes` |
| Multi-instance | `{FlowTypeName}\|{id}-routes` |

These streams contain **link events** pointing back into the `timeline` stream. They are written exclusively by the `resume` projection and read exclusively during the resume phase by `ReadFlowToResumeCommand`.

**`TimelineStreams.GetRoutesStream()`** resolves the stream name from a `FlowKey`:
```csharp
internal static string GetRoutesStream(this FlowKey key) =>
  key.GetStream("routes");
// → "{prefix}-routes"
```

### The TimelineSubscription

`TimelineSubscription` is a **single, shared catch-up subscription** to the `timeline` stream. There is exactly one per process lifetime.

```csharp
// Restarts from the global resume checkpoint
var fromStream = _checkpoint.IsSome
  ? FromStream.After(new StreamPosition((ulong)_checkpoint.ToInt64OrNull().Value))
  : FromStream.Start;

_subscription = await _context.Client.SubscribeToStreamAsync(
  TimelineStreams.Timeline,
  fromStream,
  eventAppeared: async (subscription, e, ct) =>
    await _observer.OnNext(_context.ReadAreaPoint(e)),
  subscriptionDropped: ...);
```

All live events flow through `TimelineHost.OnNext()` → `FlowHost.OnNext()` → fanned out to individual `FlowScope` instances.

**There are no per-flow subscriptions. There are no persistent subscriptions.**

### FlowHost & Fan-out

`FlowHost` maintains a `Dictionary<FlowKey, IFlowScope>` and an `HashSet<FlowKey>` of ignored flows. On each `OnNext`:

1. Iterates `point.Routes` (the set of `FlowKey`s interested in this event, computed at write time from `AreaEventMetadata.RouteIds`)
2. If the route is in `_ignored` and the event cannot be first — skip
3. If a scope doesn't exist yet — create one and connect it
4. Call `flow.Enqueue(point)` on the matching scope

Scopes are removed from the dictionary when their `LifetimeTask` completes (done, ignored, or faulted).

### FlowScope & FlowQueue

Each `FlowScope<T>` runs a **dedicated background `Task.Run` loop** (`ObserveQueue`). It never has its own EventStore subscription — it is purely driven by items placed into its `FlowQueue`.

`FlowQueue` manages two internal lanes:

| Lane | Field | Source | Purpose |
|---|---|---|---|
| Resume | `_resumeQueue` | `ReadFlowToResumeCommand` (backwards `-routes` read) | Historical events the flow missed |
| Live | `_queue` | `FlowHost.Enqueue()` from shared timeline subscription | Real-time events |

The `IsAfterResumeCheckpoint` gate in `TryDequeuePoint` discards any live event whose position is at or before the resume checkpoint, preventing double-processing as the two lanes merge.

```csharp
bool IsAfterResumeCheckpoint(TimelinePoint point) =>
  _resumeCheckpoint.IsNone || point.Position > _resumeCheckpoint;
```

Once `_resumeQueue` drains, it is set to `null` permanently and the live lane takes over.

### Checkpoints

Per-flow checkpoints are written to `{key}-checkpoint` streams by `FlowScope.WriteCheckpoint()` after each successfully processed event. The checkpoint stream always holds **exactly one event** — the current state of the flow.

```csharp
protected async Task WriteCheckpoint()
{
  await Db.WriteCheckpoint(Flow, Point);
  Flow.Context.SetNotNew();
}
```

These checkpoints serve **two purposes** today:
- **State reconstruction** — `ReadFlowCommand` reads the checkpoint to reload a flow's state
- **Resume stopping point** — `ReadFlowToResumeCommand` compares `e.Event.EventNumber` against `flow.Context.CheckpointPosition` to know when to stop reading backwards

**Critically, they do NOT determine where the shared `TimelineSubscription` restarts.** That is determined solely by the global checkpoint stored in the `resume` projection output.

---

## Identified Problems

### P1 — Single Global Checkpoint Couples All Flows

**Severity: High**

Because the single `TimelineSubscription` restarts from the minimum checkpoint across all flows (as stored in the `resume` projection), every flow in the system is coupled to the slowest flow.

**Example:**
- `OrderQuery` is fully caught up at timeline position `55,000`
- `ReportTopic` is behind at position `100`
- On restart: `TimelineSubscription` starts at `100`, replaying `54,900` events
- `OrderQuery`'s `FlowScope` discards every one of those 54,900 events via `PointIsAfterCheckpoint`

```csharp
bool PointIsAfterCheckpoint =>
  Flow == null || Point.Position > Flow.Context.CheckpointPosition;
```

This wasted replay cost grows proportionally to:
- The number of flows
- The spread between the fastest and slowest checkpoints
- The total volume of events on the timeline

### P2 — The Resume Projection Does Too Much

**Severity: High**

The `resume` projection has three distinct responsibilities bundled into one server-side component:

| Responsibility | Used By | Could Instead Be |
|---|---|---|
| Global timeline checkpoint | `TimelineSubscription` restart position | Per-flow checkpoint in `{key}-checkpoint` |
| Routes registry (which flows have pending work) | `FlowHost.Resume()` to know what to wake up | Implicit — each flow subscribes and just processes what comes |
| Schedule tracking | `ScheduleHost.Resume()` | Separate lightweight projection |

When this projection is reset (e.g., during a rebuild or schema change), **all three concerns are invalidated simultaneously**, forcing a full replay for every flow in the system.

### P3 — Full Backwards Read for New Flows

**Severity: Medium**

When a flow has never written a checkpoint (`StartAndResume()` path), `_areaCheckpoint` is `null`. The nullable lifted comparison in `ReadNextBatch` never triggers the stop condition:

```csharp
// _areaCheckpoint is long? — when null, this is always false
if((long)e.Event.EventNumber.ToUInt64() <= _areaCheckpoint)
  return false;
```

This causes `ReadFlowToResumeCommand` to read the **entire** `-routes` stream from the end to position 0 for any brand-new flow that appears in the resume projection's routes list. For a long-running system this could be a very large read.

### P4 — ResumeAlgorithm Bug (`Math.Max` vs `Math.Min`)

**Severity: Low / Bug**

`ResumeAlgorithm.GetNextBatchSize` uses `Math.Max` where `Math.Min` is clearly intended:

```csharp
// Current (broken):
public int GetNextBatchSize(int batchIndex) =>
  _sizes[Math.Max(batchIndex, _sizes.Length - 1)];
//         ^^^^^^^^
// Math.Max(0, 3) = 3, Math.Max(1, 3) = 3 — always uses the last (largest) size

// Intended:
public int GetNextBatchSize(int batchIndex) =>
  _sizes[Math.Min(batchIndex, _sizes.Length - 1)];
//         ^^^^^^^^
// Math.Min(0, 3) = 0, Math.Min(1, 3) = 1 — ramps up through sizes and caps at last
```

The effect is that the progressive batching strategy (`[ResumeAlgorithmAttribute(10, 50, 100, 200)]`) never actually progresses — every batch immediately uses the maximum size regardless of `batchIndex`. The `[ResumeAlgorithm]` attribute on flow types has no meaningful effect.

### P5 — Resume Projection Reset Invalidates All Flows

**Severity: Medium**

Because the `resume` projection is a single server-side component tracking all flows, resetting it (e.g., to fix a projection bug, rebuild the routes registry, or handle a schema change) forces:

1. All `-routes` streams to be rebuilt from scratch
2. All `FlowScope` instances to be re-resumed from position 0 of their `-routes` stream
3. All flow state to be re-read and re-validated

There is no way to surgically reset a single flow type's routes without resetting the whole projection.

---

## Proposed Refactor: Per-Flow Catch-Up Subscriptions

### Core Idea

Each `FlowScope` should own its own **catch-up subscription** starting from its `{key}-checkpoint` position, rather than being driven by a single shared subscription. The `{key}-checkpoint` streams already exist and already contain the correct restart position — they are just not being used for that purpose.

```
Before:
  One TimelineSubscription ("timeline" from global checkpoint)
    → FlowHost fans out to all FlowScopes
    → Scopes discard events <= their own checkpoint

After:
  Each FlowScope subscribes to "timeline" from its own checkpoint
    → No fan-out needed for historical events
    → No wasted replay
    → Slow flows don't affect fast flows
```

### What Changes

| Component | Current Behavior | Proposed Behavior |
|---|---|---|
| `TimelineSubscription` | One shared catch-up from global checkpoint | Retained for live fan-out only (from `StreamPosition.End`) OR replaced entirely |
| `FlowScope.Open()` | Passively receives events from `FlowHost` | Opens its own `SubscribeToStreamAsync("timeline", FromStream.After(checkpoint))` |
| `FlowHost.Resume()` | Reads `ResumeInfo.Routes`, calls `ReadFlowToResume` per key | Discovers flows to resume by querying `{type}-checkpoint` streams directly |
| `ReadFlowToResumeCommand` | Reads `-routes` backwards to build catch-up batch | Can be removed entirely |
| `FlowQueue` dual-lane logic | Merges `_resumeQueue` + `_queue` with deduplication gate | Simplifies to a single lane |
| `ResumeInfo.Routes` | List of `FlowKey`s with pending work from projection | Can be removed from `ResumeInfo` |
| `resume` projection | Manages global checkpoint + routes + schedule | Reduced to schedule tracking only (or removed if schedule is also refactored) |
| `-routes` streams | Written by projection, read on resume | Can be removed entirely |

### What Can Be Removed

If per-flow subscriptions are adopted:

- `ReadFlowToResumeCommand.cs` — no longer needed
- `ReadResumeScheduleCommand.cs` — schedule handled separately
- `FlowQueue._resumeQueue` lane and `ResumeWith()` — no longer needed
- `ResumeInfo.Routes` field — no longer needed
- `resume-projection.js` `updateRoutes()` / `linkTo()` logic — no longer needed
- `TimelineStreams.GetRoutesStream()` — no longer needed
- `{key}-routes` streams in EventStore — no longer written or read
- `ResumeAlgorithm` / `ResumeAlgorithmAttribute` — batch tuning only existed for `-routes` backwards reads

### Rough Sketch

```csharp
// FlowScope<T> — new Open() behavior
protected override async Task Open()
{
    var checkpoint = await LoadCheckpoint(); // reads {key}-checkpoint, returns null if not found

    var fromStream = checkpoint.HasValue
        ? FromStream.After(new StreamPosition((ulong)checkpoint.Value))
        : FromStream.Start;

    _subscription = await _context.Client.SubscribeToStreamAsync(
        TimelineStreams.Timeline,
        fromStream,
        eventAppeared: async (sub, e, ct) =>
        {
            var point = _context.ReadAreaPoint(e);
            if(point.Routes.Contains(Key))
                Enqueue(point);
        },
        subscriptionDropped: (sub, reason, error) => /* handle */);
}
```

Each flow would filter events from the shared `timeline` stream by checking whether its `FlowKey` is in `point.Routes`, which is already computed and stored in event metadata at write time by `AreaEventMetadata`.

---

## Open Questions for the Team

1. **Fan-out vs. per-subscription filtering** — Each per-flow subscription would receive all `timeline` events and filter client-side. At high event volume with many flows, this multiplies the read load on EventStore proportionally. Is a hybrid approach viable — shared subscription for live events, per-flow catch-up only for the resume phase?

2. **Schedule handling** — The `ScheduleHost` currently depends on `ResumeInfo.Schedule` which comes from the `resume` projection. If the projection is removed, how does `ScheduleHost` discover pending scheduled events on restart? Does it get its own lightweight projection, or does it scan the `schedule` stream directly?

3. **Flow discovery** — Without the `resume` projection's routes registry, how does `FlowHost` know which `FlowKey`s exist and need to be resumed? Options:
   - Scan all `{type}-checkpoint` streams by convention
   - Maintain a separate lightweight "known flows" registry stream
   - Let flows self-discover by subscribing from `FromStream.Start` and creating themselves on first event

4. **Subscription count scaling** — If there are hundreds of multi-instance flows (e.g., one `OrderTopic` per customer), is one EventStore subscription per instance acceptable? EventStore persistent subscriptions with competing consumers may be more appropriate at that scale.

5. **Backward compatibility** — Existing deployments have populated `-routes` and `resume` streams. Does the refactor require a migration step, or can the new model bootstrap cleanly from existing `-checkpoint` streams?

6. **`ResumeAlgorithm` bug (P4)** — This is a quick fix regardless of the larger refactor. Should it be patched on `work-dev-wasp-oldResume` now?

---

## File Reference Map

| File | Role |
|---|---|
| `src/Totem.Timeline/Runtime/ResumeInfo.cs` | DTO carrying checkpoint, routes, schedule, and subscription back to `TimelineHost` |
| `src/Totem.Timeline/Runtime/TimelineHost.cs` | Entry point; connects DB, resumes flows/schedule, tracks live subscription |
| `src/Totem.Timeline/Runtime/FlowHost.cs` | Manages all active `FlowScope` instances; fans out `TimelinePoint`s |
| `src/Totem.Timeline/Runtime/FlowScope.cs` | Per-flow async processing loop; owns `FlowQueue` and checkpoint writes |
| `src/Totem.Timeline/Runtime/FlowQueue.cs` | Dual-lane queue merging resume and live event streams |
| `src/Totem.Timeline/Runtime/ResumeAlgorithm.cs` | Batch size progression for backwards `-routes` reads (**contains Math.Max bug**) |
| `src/Totem.Timeline/ResumeAlgorithmAttribute.cs` | Per-flow-type attribute to configure `ResumeAlgorithm` |
| `src/Totem.Timeline.EventStore/TimelineSubscription.cs` | Single shared catch-up subscription to `timeline` stream |
| `src/Totem.Timeline.EventStore/TimelineStreams.cs` | Stream name constants and helpers (`GetRoutesStream`, `GetCheckpointStream`) |
| `src/Totem.Timeline.EventStore/DbOperations/SubscribeCommand.cs` | Reads `resume` stream; builds `ResumeInfo` |
| `src/Totem.Timeline.EventStore/DbOperations/ReadFlowToResumeCommand.cs` | Reads `{key}-routes` backwards to build `FlowResumeInfo`; primary consumer of `-routes` streams |
| `src/Totem.Timeline.EventStore/DbOperations/ReadFlowCommand.cs` | Reads `{key}-checkpoint` to reload flow state |
| `src/Totem.Timeline.EventStore/resume-projection.js` | Server-side EventStore projection; writes `resume` stream and all `-routes` link streams |
