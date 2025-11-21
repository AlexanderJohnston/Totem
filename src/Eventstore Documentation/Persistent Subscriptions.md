# EventStoreDB 22.10 — Persistent subscriptions

Persistent subscriptions are a server-managed subscription type that lets multiple consumers (a consumer group) compete to process events from a single stream (or from $all). They provide at-least-once delivery semantics and save subscription state on the server so consumers can be load-balanced and restarted without losing their place.

Persistent subscriptions are useful when you want competing consumers / worker pools to process events in parallel while the server tracks progress.

---

## Key characteristics

- Server-managed: the subscription state (last processed position/checkpoint) is kept on the server.
- Competing consumers: many clients in the same consumer group share work for the same subscription.
- Runs on the cluster leader: the persistent subscription processing runs only on the Leader node.
- At-least-once delivery: clients must acknowledge processed events; otherwise the server will retry delivery.
- Use-case: background workers, job processing, scaled consumers across multiple machines.

Tip: The Admin UI includes a Persistent Subscriptions section for creating, updating, deleting and viewing subscriptions. Persistent subscriptions to `$all` must be created through a gRPC client.

---

## Concepts

- Consumer group: a named group of clients that share work for a subscription. Each group has its own server-side checkpoint and settings.
- Acknowledgement: clients must acknowledge (or explicitly not-ack) each message. If the server does not receive an acknowledgement before the message timeout it will retry delivery.
- Parked messages: messages that exceed the maximum retry count are parked (moved to a parked stream) for later inspection or replay.
- Checkpointing: the server periodically writes checkpoints to record progress. On restart or leader changes, the subscription resumes from the last checkpoint.
- Ordering: parallel processing across consumers can produce out-of-order delivery; ordering is not guaranteed.

Warning: processing events in parallel within a consumer group will most likely produce out-of-order handling within some window. Do not assume strict ordering unless you control concurrency and ordering in your application.

---

## Acknowledging messages

Clients must acknowledge messages after successfully processing them. If a message is not acknowledged before the server timeout, the server will retry delivery to the same or another consumer.

If a message has been retried more times than the subscription's `maxRetryCount`, it will be parked (see Parked messages).

Common acknowledgement actions (client API dependent):
- Acknowledge / Ack — message successfully processed.
- NotAcknowledge / Nack — message failed; options may include retry, park, or skip.

---

## Parked messages

Messages that exceed the max retry count are parked to the subscription's parked stream:

`$persistentsubscription-{groupname}::{streamname}-parked`

- You can view parked message counts in the persistent subscription statistics or the Admin UI.
- To retry parked messages you can Replay them; replaying pushes parked messages to subscribers before new messages.
- To replay a subset, use the `stopAt` parameter on the replay endpoint (HTTP example below).
- To discard parked messages, delete the parked stream like any normal stream.

Replay parked messages (HTTP example):

```bash
curl -i -X POST -d {} \
  "https://localhost:2113/subscriptions/{stream}/{groupname}/replayParked?stopAt={numberOfEvents}" \
  -u "admin:changeit"
```

Replace `{stream}`, `{groupname}`, and `{numberOfEvents}` as needed.

---

## Checkpointing

- The server writes checkpoints that capture how far the subscription has progressed.
- On leader change or subscription restart, processing resumes from the last checkpoint (some events may be redelivered).
- If a subscription has a filter, it will checkpoint when enough events have been handled or skipped per the filter configuration.

Note: persistent subscriptions will not write a new checkpoint if one is already in the process of being written. Even with a configuration that requests frequent checkpoints (e.g., max checkpoint count = 1) the server may not write after every event.

---

## Consumer strategies

When creating a persistent subscription you can choose a consumer strategy which determines how the server dispatches events to consumers in the group.

### RoundRobin (default)
- Distributes events evenly across all connected consumers.
- If a client's buffer reaches `bufferSize`, that client will be skipped until it has capacity again.
- Use when you want simple even load balancing across workers.

### DispatchToSingle
- Sends events to a single client until that client's buffer reaches `bufferSize`, then switches to the next client (round-robin across clients).
- Useful as a fall-back or for a primary consumer with overflow to others.

### Pinned
- Intended for use with indexing or category-style projections (e.g., `$by_category`).
- EventStoreDB hashes the source stream id into one of 1024 buckets and assigns buckets to clients.
- When a client disconnects its buckets are reassigned to other clients; when it reconnects it receives some existing buckets.
- Aims to reduce concurrency and ordering issues by trying to deliver events from the same stream (or bucket) to the same client.
- Warning: behaves differently if `ResolveLinkTos` is enabled; enable `ResolveLinkTos` when using this strategy with indexing projections like `$by_category`.

### PinnedByCorrelation
- Similar to Pinned, but hashing is done using the event's `correlationId` rather than the source stream id.
- Useful when you need related events grouped by correlation id to be sent to the same consumer.

---

## Considerations and limitations

- Persistent subscriptions run only on the Leader node:
  - This concentrates CPU and I/O load on the leader; plan capacity accordingly.
  - Subscriptions will reload from the last checkpoint when the Leader changes.

- Ordering is not guaranteed:
  - Because of retries, parallel processing, and consumer distribution, event ordering cannot be assumed.
  - If you need strict ordering guarantees, use a client-managed Catch-up subscription and handle checkpointing on the client side.

- Design for idempotency:
  - Because of at-least-once delivery, handlers should be idempotent or tolerate duplicate deliveries.

- Multi-group behavior:
  - You can create multiple consumer groups for the same stream; each group is independent and receives its own copy of events.

---

## Admin UI and management

- The Admin UI provides a Persistent Subscriptions screen where you can:
  - Create, update, and delete subscriptions (for streams; `$all` subscriptions must be created with gRPC).
  - View subscription statistics (connected consumers, parked messages, processed / in-flight counts).
  - Replay parked messages and perform other management tasks.

---

## Quick checklist for using persistent subscriptions

- Choose the right stream: direct stream or projected stream (system/user projections can create aggregate streams).
- Pick a consumer strategy suitable for ordering vs load distribution.
- Ensure consumers ack / nack messages correctly.
- Tune `maxRetryCount`, timeouts, buffer sizes, and checkpointing cadence for your workload.
- Monitor parked messages and replay/clear as part of operational runbooks.
- Plan capacity for the Leader node (persistent subscriptions execute on the leader).

---

This document summarizes EventStoreDB 22.10 persistent subscriptions: concepts, acknowledgement and parked message handling, checkpointing, consumer strategies, and operational considerations. Refer to your client SDK docs for language-specific APIs for creating and managing persistent subscriptions.