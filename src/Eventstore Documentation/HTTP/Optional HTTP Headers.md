# EventStoreDB 22.10 — Optional HTTP headers

EventStoreDB supports a set of optional HTTP headers to control behavior for appends, reads, deletes and other operations. These headers were previously prefixed with `X-ES-` and were changed to `ES-` in compliance with RFC‑6648.

Supported headers
- `ES-ExpectedVersion` — expected version of the stream (optimistic concurrency)
- `ES-ResolveLinkTos` — whether to resolve linkTos when returning events
- `ES-RequiresMaster` — require operation to run on the cluster leader
- `ES-TrustedAuth` — allow a trusted intermediary to handle authentication
- `ES-LongPoll` — instruct the server to long-poll when reading from the head
- `ES-HardDelete` — perform a hard delete instead of the default soft delete
- `ES-EventType` — declare the event type for a POSTed body
- `ES-EventId` — declare the event id (UUID) for a POSTed body

Below are details and examples for the most commonly used headers.

EventId (`ES-EventId`)
- Purpose: Specify the EventId for an append when you are not using the custom media types (`application/vnd.eventstore.events+json` or `...+xml`).
- When using the custom media type, the event JSON includes `eventId` and you do not need the header.
- Event IDs are used for idempotence: retries that reuse the same EventId will not create duplicates.

Example (using the custom media type — not required to set `ES-EventId`):

```bash
curl -i -d "@event.json" "http://127.0.0.1:2113/streams/newstream" \
  -H "Content-Type:application/vnd.eventstore.events+json" \
  -u "admin:changeit"
```

Example (posting a raw body and setting ES-EventId & ES-EventType):

```bash
curl -i -d "@event.json" "http://127.0.0.1:2113/streams/newstream" \
  -H "Content-Type:application/json" \
  -H "ES-EventType: SomeEvent" \
  -H "ES-EventId: eeccf3ce-4f54-409d-8870-b35dd836cca6" \
  -u "admin:changeit"
```

If you omit `ES-EventId` when posting a simple body (not using the event media type), EventStoreDB may return a `307 Temporary Redirect` and provide a generated, idempotent URI in the `Location` header where you can POST the event. If you can generate a UUID client-side, prefer doing so to avoid the redirect.

EventType (`ES-EventType`)
- Purpose: Specify the event type when posting without the custom media type. The custom media types embed the event type inside the payload and do not require the header.
- Required when posting a raw body that represents an event.

Example:

```bash
curl -i -d "@event.json" "http://127.0.0.1:2113/streams/newstream" \
  -H "Content-Type:application/json" \
  -H "ES-EventType: SomeEvent" \
  -u "admin:changeit"
```

Expected Version (`ES-ExpectedVersion`)
- Purpose: Optimistic concurrency control for appends. Set this header to the expected current version of the stream.
- Common special values:
  - `-2` — never conflict; attempt to always succeed (best-effort idempotence / at-least-once semantics)
  - `-1` — stream must not exist (append will create it)
  - `0` — stream must exist but be empty
  - `-4` — expect the stream or a metadata stream to exist when appending
- If the expected version does not match the stream's current version, EventStoreDB returns `HTTP 400 Wrong expected EventNumber` and includes the current version in the `ES-CurrentVersion` response header.

Example (append without expectation — create/append):

```bash
curl -i -d "@event.json" "http://127.0.0.1:2113/streams/newstream" \
  -H "Content-Type:application/vnd.eventstore.events+json" \
  -u "admin:changeit"
```

Example (append with expected version `0`):

```bash
curl -i -d @event-version.json "http://127.0.0.1:2113/streams/newstream" \
  -H "Content-Type:application/vnd.eventstore.events+json" \
  -H "ES-ExpectedVersion: 0" \
  -u "admin:changeit"
```

Note: If you re-post the same event with the same `EventId`, idempotence will cause a repeated attempt to return the same result rather than failing the expected version check.

HardDelete (`ES-HardDelete`)
- Purpose: Control deletion behavior. By default, DELETE performs a soft delete (stream can be recreated). Set `ES-HardDelete:true` to perform a permanent (hard) delete.
- Hard delete behavior: appends a tombstone; subsequent reads return `410 StreamDeleted` and the stream cannot be recreated.

Example (hard delete):

```bash
curl -X DELETE "http://127.0.0.1:2113/streams/newstream" \
  -H "ES-HardDelete:true" \
  -u "admin:changeit"
```

LongPoll (`ES-LongPoll`)
- Purpose: When reading the head of a stream and there is no new data, instruct the server to wait (long-poll) for a limited time for new events to arrive before returning an empty response. This reduces polling latency for Atom/HTTP clients.
- Value: number of seconds to wait (integer). Range: positive integers (server-side limits may apply).

Example (long poll for up to 10 seconds):

1. Get the head link:

```bash
curl -i -H "Accept:application/vnd.eventstore.atom+json" \
  "http://127.0.0.1:2113/streams/newstream" -u "admin:changeit"
```

2. Request the previous (tail) URL with `ES-LongPoll`:

```bash
curl -i "http://127.0.0.1:2113/streams/newstream/2/forward/20" \
  -H "Accept: application/json" \
  -H "ES-LongPoll: 10" \
  -u "admin:changeit"
```

- If no event is appended within the long-poll window, the request returns after the specified timeout with an empty feed.
- If an event is appended while the request is waiting, the request returns immediately with the new data.

Requires Master (`ES-RequiresMaster` / `ES-RequireMaster`)
- Purpose: In clustered deployments, instruct the node to only serve the request if it is the current leader. If the node is not the leader it will respond with `307 Temporary Redirect` pointing to the leader.
- Use this header for operations that must run on the leader (strong consistency / writes that should not be proxied to followers).

Example (request requiring master):

```bash
curl -i "http://127.0.0.1:32004/streams/newstream" \
  -H "ES-RequireMaster: True" \
  -u "admin:changeit"
```

If run against a follower:

```bash
curl -i "http://127.0.0.1:31004/streams/newstream" \
  -H "ES-RequireMaster: True" \
  -u "admin:changeit"
```

the follower will typically return a `307` redirect to the leader.

Resolve LinkTo (`ES-ResolveLinkTos`)
- Purpose: Control whether link events (created by `linkTo`) are resolved to their target events when reading. By default EventStoreDB resolves linkTos and returns the pointed-to event. Use `ES-ResolveLinkTos: false` to return the link event itself (the link metadata) rather than resolving it.
- Useful when you need to inspect link events or when you want the feed to reference the linkTo event instead of the original.

Examples:

Resolve linkTos (default behavior):

```bash
curl -i "http://127.0.0.1:2113/streams/shoppingCart-.../0" \
  -H "Accept:application/vnd.eventstore.atom+json" \
  -H "ES-ResolveLinkTos: true" \
  -u "admin:changeit"
```

Do not resolve linkTos (get the link event, not the target):

```bash
curl -i "http://127.0.0.1:2113/streams/shoppingCart-.../0" \
  -H "Accept:application/vnd.eventstore.atom+json" \
  -H "ES-ResolveLinkTos: false" \
  -u "admin:changeit"
```

Tip: When `ES-ResolveLinkTos` is `true`, content links in the feed will point at the original (target) event; when `false`, they point to the linkTo event itself.

Other headers
- `ES-TrustedAuth` — used in architectures with a trusted proxy that performs authentication on behalf of clients. Use with care and only when the network/proxy is trusted.
- `ES-RequiresMaster` vs `ES-RequireMaster` — header spelling may appear in examples; check your server version and client SDK for the exact header name expected (many examples use `ES-RequireMaster`).

General tips
- Prefer the custom event media types (`application/vnd.eventstore.events+json` / `...+xml`) for appends — they embed `eventId`, `eventType`, and support batch posts.
- Use stable client-side UUID generation for `EventId` to avoid redirects and to enable robust idempotence.
- When relying on `ES-LongPoll`, ensure your HTTP client appropriately handles long requests and timeouts.
- Use `ES-RequiresMaster` for operations that must run on the leader to avoid accidental routing to a follower.
- For link-heavy systems (projections that `linkTo`), explicitly set `ES-ResolveLinkTos` when you need to control whether you receive link events or their resolved targets.

This document summarizes the optional HTTP headers supported by EventStoreDB 22.10 and provides examples of common usage patterns.