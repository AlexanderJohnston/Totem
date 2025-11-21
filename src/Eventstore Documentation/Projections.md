# EventStoreDB 22.10 — Projections

Projections is an EventStoreDB subsystem that lets you append new events or link existing events to streams in a reactive manner.

Projections are well suited to solve "temporal correlation queries" — queries that look for patterns across events with timing constraints. Examples include monitoring sensor data, complex business rules, and near real-time event processing. Note: projections require event bodies to be in JSON.

## Introduction

Projections support continuous queries: a projection can produce a stream of results for all matching events in the past and can continue running to produce results as new events arrive. The output of a projection is itself a stream and can be consumed like any other stream.

Types of projections:
- Built-in (system) projections
- User-defined JavaScript projections (created via API or Admin UI)

Performance impact:
- Projections emit events or link events as a reaction to processed events, resulting in write amplification and additional IO.
- System projections ($by_category, $by_event_type, $by_correlation_id, $streams, $stream_by_category) emit link events and can significantly increase the number of writes.
- Custom projections can create additional amplification because their emitted/link events are further processed by system projections.
- Projections run only on the cluster leader, which concentrates CPU and IO load on that node.

Limitations:
- Streams owned by a projection (streams where projections emit events) cannot be appended to by external applications. Projections detect externally appended events and will break if they do.
- Projections rely on exclusive ownership of their emitted streams to track progress and validate correctness.

## System projections

EventStoreDB ships with five built-in system projections:
- By Category ($by_category)
- By Event Type ($by_event_type)
- By Correlation ID ($by_correlation_id)
- Stream by Category ($stream_by_category)
- Streams ($streams)

When starting from a fresh DB these projections exist but are disabled (Stopped). Enable a projection by issuing a command to switch its status from Stopped to Running, for example:

```bash
curl -i -X POST "http://{event-store-ip}:{ext-http-port}/projection/{projection-name}/command/enable" \
  -H "accept:application/json" -H "Content-Length:0" -u admin:changeit
```

### By Category ($by_category)

$by_category links events from streams into category streams with a `$ce-` prefix. It splits a stream id using a configurable separator and split position.

- Two parameters:
  - position: `first` or `last` — specifies whether to split on the first or last occurrence of the separator
  - separator: any character

Examples:
- With parameters `first` and `-`, stream id `account-9E763770-0A8D-456D-AF23-410ADBC88249` becomes `$ce-account`.
- With parameters `last` and `-`, stream id `shopping-cart-1` becomes `$ce-shopping-cart`.

Warning: changing category projection settings can break consumers that expect a stable category format.

Use case: subscribe to all events within a category.

### By Event Type ($by_event_type)

$by_event_type links events into streams named `$et-{event-type}`. For example, an event with EventType `PaymentProcessed` will produce a link in `$et-PaymentProcessed`. This projection is not configurable.

### By Correlation ID ($by_correlation_id)

$by_correlation_id links events into streams named `$bc-{correlation id}`. It accepts a JSON parameter specifying the correlation property:

```json
{
  "correlationIdProperty": "$myCorrelationId"
}
```

### Stream by Category ($stream_by_category)

$stream_by_category links events into `$category-{category}` streams by splitting stream IDs, similarly configurable with `first`/`last` and a separator. Example: `account-1` → `$category-account`. Use case: subscribe to all instances of a stream category.

### Streams projection ($streams)

$streams links existing events from streams into `$streams`. This projection is not configurable.

## User-defined projections

Note: This section contains a work-in-progress overview.

User-defined projections are written in JavaScript. Example projection that counts `myEventType` events in `account-1`, transforms state, and outputs it:

```javascript
options({
    resultStreamName: "my_demo_projection_result",
    $includeLinks:    false,
    reorderEvents:    false,
    processingLag:    0
})

fromStream('account-1')
    .when({
        $init: function () {
            return {
                count: 0
            }
        },
        myEventType: function (state, event) {
            state.count += 1;
        }
    })
    .transformBy(function (state) {
        return {Total: state.count}
    })
    .outputState()
```

### User defined projections API

Options (selection):
- resultStreamName — overrides default result stream name (`$projections-{projection-name}-result`).
- $includeLinks — include/exclude links (default: `false`).
- processingLag — used with `reorderEvents` to control buffer processing (default: 500 ms; only for fromStreams()).
- reorderEvents — buffer and reorder events by prepare position (default: `false`; only for fromStreams()).

Selectors:
- fromAll() — select events from `$all`. Provides partitionBy, when, foreachStream, outputState.
- fromCategory({category}) — select from `$ce-{category}`. Provides partitionBy, when, foreachStream, outputState.
- fromStream({streamId}) — select from a specific stream. Provides partitionBy, when, outputState.
- fromStreams(streams[]) — select from a list of streams. Provides partitionBy, when, outputState.

Filters and transformations:
- when(handlers) — filter specific event types and provide handlers. Provides $defines_state_transform, transformBy, filterBy, outputTo, outputState.
- foreachStream() — partition state per stream.
- outputState() — writes projection state to `$projections-{projection-name}-result`.
- partitionBy(function(event)) — partition state based on handler return.
- transformBy(function(state)) — transform final state.
- filterBy(function(state)) — filter out states.

Handlers:
Each handler receives current projection state and an event object with properties:
- isJson, data, body, bodyRaw, sequenceNumber, metadataRaw, linkMetadataRaw, partition, eventType, streamId

Common handlers:
- {event-type} — handler for a specific event type.
- $init — initialize state.
- $initShared — init for partitioned projections.
- $any — pattern match for any event type.
- $deleted — called when a stream is deleted (only with foreachStream).

Functions available in handlers:
- emit(streamId, eventType, eventBody, metadata) — append an event to stream.
- linkTo(streamId, event, metadata) — write a link to an event in designated stream.

## Configuring projections

You can change projection settings to reduce pressure or improve performance. A projection must be stopped before modifying its configuration.

Common configuration options are grouped below.

### Emit options

These control how projections append/link events.

- EmitEnabled (emit): boolean — whether projection can emit/link events. If a projection uses emit()/linkTo() but EmitEnabled is false, you will see an error: `'emit' is not allowed by the projection/configuration/mode`. Emit is disabled by default and should be enabled at projection creation if needed.

- TrackEmittedStreams (trackemittedstreams): boolean — when enabled and EmitEnabled is true, EventStoreDB records each stream name emitted to in `$projections-{projection_name}-emittedstreams` to enable deleting a projection and all streams it created. This produces extra write amplification (one event per emitted stream name) and is disabled by default.

  Tip: check older projections (created between v3.8.0 and v4.0.2) for this setting, as it was enabled by default in that period.

- MaxAllowedWritesInFlight — maximum concurrent writes a projection can perform. Default: unbounded. Lowering it throttles projection writes and can reduce commit timeouts but slows projection throughput.

- MaxWriteBatchLength — maximum number of events a projection can write in a single batch. Default: 500 events.

### Checkpoint options

Checkpoints record how far a projection has processed. Writing checkpoints has overhead; adjust carefully.

- CheckpointAfterMs — minimum time between checkpoints. Default: 0 (no minimum).

- CheckpointHandledThreshold — number of handled events before writing a checkpoint. Default: 4000 handled events.

- CheckpointUnhandledBytesThreshold — number of bytes of unhandled events processed before writing a checkpoint (to avoid re-reading large unrelated sequences). Default: 10 MiB.

### Processing options

- PendingEventsThreshold — number of pending events allowed before pausing the projection's reads. When paused, the projection drains the queue; it resumes when pending drops below half the threshold. Default: 5000 events.

## Debugging

JavaScript projections are easier to debug using a browser (Chrome, Firefox, Edge, Safari).

### Logging from within a projection

Projections provide a `log()` method that writes messages to the configured EventStoreDB logger (default: NLog to file and stdout). Printing event body structure can help with debugging:

```javascript
fromStream('$stats-127.0.0.1:2113')
    .when({
        $any: function (s, e) {
            log(JSON.stringify(e));
        }
    })
```

### Creating a sample projection for debugging

Filename: `stats-counter.json`

Contents:

```javascript
fromStream('$stats-127.0.0.1:2113')
    .when({
        $init: function () {
            return {
                count: 0
            }
        },
        $any:  function (s, e) {
            s.count += 1;
        }
    })
```

Create the projection by posting the definition to the API:

```bash
curl -i -d@stats-counter.json \
  "http://localhost:2113/projections/continuous?name=stats-counter%26type=js%26enabled=true%26emit=true%26trackemittedstreams=true" \
  -u admin:changeit
```

### Using the debugger

Open the projection in the Admin UI and click the Debug button to access the debugging interface. Use Run Step to step through queued events and Update to change projection definitions in the debugger. Step into the handler(state, eventEnvelope) method when debugging.

## Projections settings (server-side)

Warning: server-side projections impact node performance (write amplification and IO).

### Projection runtime

An Interpreted runtime (introduced in v21.6.0) replaces the legacy V8 runtime. Use `ProjectionRuntime` to choose between `Interpreted` and `Legacy`. Default: `Interpreted`.

- CLI: `--projection-runtime`
- YAML: `ProjectionRuntime`
- Env: `EVENTSTORE_PROJECTION_RUNTIME`

### Run projections

Control which projections the server runs at startup: `None`, `System`, or `All`.

- `None`: projections subsystem disabled; Admin UI Projections menu disabled.
- `System`: system projections will be enabled but only start if `StartStandardProjections` is `true`. If `StartStandardProjections` is `false`, system projections are enabled but not started and must be started manually.
- `All`: enable both system and custom projections.

- CLI: `--run-projections`
- YAML: `RunProjections`
- Env: `EVENTSTORE_RUN_PROJECTIONS`

Default: `None` (all projections disabled).

### Projection threads

Projection threads run JavaScript runtimes (one V8/interpreted engine per thread). Increase `ProjectionThreads` if projections perform heavy CPU work. Too many threads increase context switching and memory use.

- CLI: `--projection-threads`
- YAML: `ProjectionThreads`
- Env: `EVENTSTORE_PROJECTION_THREADS`

Default: 3

Primary reasons for projection lag:
- sustained high write load outpacing processors,
- CPU-heavy projection logic,
- high write amplification with slow disks.

Tune `ProjectionThreads` according to CPU usage and projection lag.

### Fault out-of-order projections

Occasionally projections may receive an event number that skips expected sequence numbers (e.g., 7 after processing 4 because events 5 and 6 were deleted and scavenged). By default the projection engine ignores ordering failures. To force projections to fail on out-of-order events, enable `FaultOutOfOrderProjections`.

- CLI: `--fault-out-of-order-projections`
- YAML: `FaultOutOfOrderProjections`
- Env: `EVENTSTORE_FAULT_OUT_OF_ORDER_PROJECTIONS`

Default: `false`.

---

This document converts the Projections reference into a single readable Markdown page, preserving configuration keys, code examples, warnings, and tuning guidance.
