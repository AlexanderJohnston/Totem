# EventStoreDB 22.10 — HTTP / AtomPub interface

EventStoreDB provides an AtomPub-based HTTP interface for interacting with streams and events. AtomPub is a RESTful protocol that can reuse many existing components (reverse proxies, HTTP caches). Events are immutable, so responses can use long cache lifetimes. EventStoreDB supports content negotiation so clients can request JSON or XML representations.

Note: AtomPub support is disabled by default in v20+. New applications should prefer the gRPC protocol introduced in v20.

## Overview

Compatibility with AtomPub
- EventStoreDB v5 is compatible with Atom Protocol 1.0 and adds extensions (headers for control, custom rel links).
- Warning: AtomPub is disabled by default in v20+ and planned for deprecation; use gRPC for new development.

Existing implementations (examples — not officially supported)
- .NET (BCL): System.ServiceModel.SyndicationServices
- JVM: various RSS/Atom libraries
- PHP: simplepie.org
- Ruby: cardmagic/simple-rss
- Clojure: scsibug/feedparser-clj
- Python: feedparser
- node.js: node-feedparser

Content types
- Preferred method: set Accept header. As fallback, ?format=xml is supported.
- Accepted content types for POST:
  - application/xml
  - application/vnd.eventstore.events+xml
  - application/json
  - application/vnd.eventstore.events+json
  - text/xml
- Accepted content types for GET:
  - application/xml
  - application/atom+xml
  - application/json
  - application/vnd.eventstore.atom+json
  - text/xml
  - text/html
  - application/vnd.eventstore.streamdesc+json

---

## Appending events

You append to a stream with HTTP POST to /streams/{stream}. Non-existent streams are created implicitly.

EventStoreDB media types
- Custom media type for posting events: `application/vnd.eventstore.events+json` (or `...+xml`). Supports extra features such as batch posts.
- JSON schema for batch posts (eventId must be a UUID):

```json
[
  {
    "eventId": "string",
    "eventType": "string",
    "data": "object",
    "metadata": "object"
  }
]
```

Appending a single event to a new stream
- Example event (event.json):

```json
[
  {
    "eventId": "fbf4a1a1-b4a3-4dfe-a01f-ec52c34e16e4",
    "eventType": "event-type",
    "data": { "a": "1" }
  }
]
```

- POST to create stream and add event:

```bash
curl -i -d "@event.json" "http://127.0.0.1:2113/streams/newstream" \
  -H "Content-Type:application/vnd.eventstore.events+json" \
  -u "admin:changeit"
```

- If you omit ES-EventId, EventStoreDB may respond with a 307 Temporary Redirect to an idempotent incoming URI. E.g.:

POST without ES-EventId and using Content-Type `application/json` and `ES-EventType`:

```bash
curl -i -d "@event.json" "http://127.0.0.1:2113/streams/newstream" \
  -H "Content-Type:application/json" \
  -H "ES-EventType: SomeEvent"
```

Server returns 307 with a location like:

`/streams/newstream/incoming/{uuid}`

Then you POST to that incoming URI (which is idempotent):

```bash
curl -i -d "@event.json" "http://127.0.0.1:2113/streams/newstream/incoming/8a00e469-3a99-4517-a0b0-8dc662ffad9b" \
  -H "Content-Type: application/json" -H "ES-EventType: SomeEvent"
```

- EventType header is required when posting; omission results in an error.

Batch append operation
- You can append multiple events in a single array. The batch is transactional: all are appended together or the operation fails.

Example (multiple-events.json):

```json
[
  {
    "eventId": "fbf4b1a1-b4a3-4dfe-a01f-ec52c34e16e4",
    "eventType": "event-type",
    "data": { "a": "1" }
  },
  {
    "eventId": "0f9fad5b-d9cb-469f-a165-70867728951e",
    "eventType": "event-type",
    "data": { "b": "2" }
  }
]
```

Post the batch:

```bash
curl -i -d "@multiple-events.json" "http://127.0.0.1:2113/streams/newstream" \
  -H "Content-Type:application/vnd.eventstore.events+json"
```

Appending events (subsequent appends)
- Example append:

```json
[
  {
    "eventId": "fbf4a1a1-b4a3-4dfe-a01f-ec52c34e16e5",
    "eventType": "event-type",
    "data": { "b": "2" }
  }
]
```

```bash
curl -i -d "@event-append.json" "http://127.0.0.1:2113/streams/newstream" \
  -H "Content-Type:application/vnd.eventstore.events+json" \
  -H "ES-EventType: SomeEvent"
```

Data-only events
- Since v3.7.0 EventStoreDB supports `application/octet-stream` for data-only binary events. Must include `ES-EventType` and `ES-EventId` headers and cannot include metadata.

Example (base64 body `SGVsbG8gV29ybGQ=`):

```bash
curl -i -d "SGVsbG8gV29ybGQ=" "http://127.0.0.1:2113/streams/newstream" \
  -H "Content-Type:application/octet-stream" \
  -H "ES-EventType:rawDataType" \
  -H "ES-EventId:eeccf3ce-4f54-409d-8870-b35dd836cca6"
```

Expected version header (optimistic concurrency)
- Use `ES-ExpectedVersion` (or `ES-CurrentVersion` in examples) to express expected stream version for optimistic locking.
- If expected version mismatches current version, server returns HTTP 400.
- Special values:
  - `-2`: never conflict (always succeed; at-least-once semantics)
  - `-1`: stream must not exist (append creates it)
  - `0`: stream must exist and be empty

Example:

```bash
curl -i -d @event-version.json "http://127.0.0.1:2113/streams/newstream" \
  -H "Content-Type:application/vnd.eventstore.events+json" \
  -H "ES-CurrentVersion: 0"
```

Idempotence
- Appends are idempotent based on EventId. Clients should retry failed requests reusing the same UUID.
- If using expected version, idempotence is guaranteed. Using `-2` makes idempotence best-effort only (at-least-once).
- If you re-send the same event (same EventId), EventStoreDB will not duplicate it.

---

## Reading streams and events

Reading a stream
- Stream resource: `http(s)://{host}:{port}/streams/{stream}`.
- GET returns AtomFeed (or other negotiated format).

Example:

```bash
curl -i -H "Accept:application/vnd.eventstore.atom+json" "http://127.0.0.1:2113/streams/newstream" -u "admin:changeit"
```

Reading an event from a stream
- Entries in a feed have links to event resources. Follow the `alternate` link and set Accept to the desired mime type.

Accepted GET content types include:
- application/xml
- application/atom+xml
- application/json
- application/vnd.eventstore.atom+json
- text/xml
- text/html

Example reading event 0 as JSON:

```bash
curl -i http://127.0.0.1:2113/streams/newstream/0 -H "Accept: application/json" -u "admin:changeit"
```

Feed paging
- Atom feed includes first/last/previous/next links (RFC 5005).
- Two main traversal patterns:
  - GET last link and follow `previous` links (for live tailing follow `previous`).
  - GET first link and follow `next` links (final item has no `next`).
- Head link (latest) may be polled for new data.

Example of following forward paging:

```bash
curl -i http://127.0.0.1:2113/streams/newstream/1/forward/20 -H "Accept:application/vnd.eventstore.atom+json"
```

Tips:
- All links except the head link are fully cacheable: `Cache-Control: max-age=31536000, public`.
- Do not bookmark links other than the head. Follow links for forward compatibility.

Paging through events
- Create many events, then traverse via provided links (last/previous or first/next).

Example:

```bash
curl -i -d "@paging-events.json" "http://127.0.0.1:2113/streams/alphabet" -H "Content-Type:application/vnd.eventstore.events+json"
```

Then request the stream:

```bash
curl -i http://127.0.0.1:2113/streams/alphabet -H "Accept:application/vnd.eventstore.atom+json"
```

Reading all events ($all)
- `$all` is a special paged stream for all events on a node. It supports the same paging form but cannot be posted to.
- Access requires admin credentials.

Example:

```bash
curl -i http://127.0.0.1:2113/streams/%24all \
  -H "Accept:application/vnd.eventstore.atom+json" -u admin:changeit
```

Conditional GETs (ETags)
- Head link supports conditional GETs using ETag/If-None-Match.
- Server returns `304 Not Modified` if nothing changed.

Example ETag usage:

```http
ETag: "26;-2060438500"
```

Then:

```bash
curl -i http://127.0.0.1:2113/streams/alphabet \
  -H "Accept:application/vnd.eventstore.atom+json" \
  -H "If-None-Match:26;-2060438500"
```

Tip:
- ETags are created using stream version and media type. Do not mix media types when reusing ETags.

---

## Embedding data into streams (JSON)

By default feeds contain links to event data. You can embed event bodies/metadata into the feed to reduce requests.

Use `embed` query parameter:

- `embed=rich` — rich embed returns additional properties (eventType, streamId, position, etc.)
- `embed=body` — returns the JSON/XML body of events inline
- Variants:
  - `PrettyBody` — pretty-print JSON
  - `TryHarder` — tries harder to parse/format JSON from event bodies

Examples:

Rich embed:

```bash
curl -i -H "Accept:application/vnd.eventstore.atom+json" \
  "http://127.0.0.1:2113/streams/newstream?embed=rich"
```

Body embed:

```bash
curl -i -H "Accept:application/vnd.eventstore.atom+json" \
  "http://127.0.0.1:2113/streams/newstream?embed=body"
```

---

## Deleting a stream

Soft deleting
- Issue DELETE to the stream resource (default is soft delete):

```bash
curl -X DELETE "http://127.0.0.1:2113/streams/newstream"
```

- Soft-deleted streams return `404 StreamNotFound` for GETs. You can recreate a soft-deleted stream by appending new events; version numbers continue from where the stream left off.

Example GET after soft delete:

```bash
curl -X GET "http://127.0.0.1:2113/streams/newstream" \
  -H 'Accept: application/vnd.eventstore.events+json'
```

Recreate by appending:

```bash
curl -i -d "@event-append.json" "http://127.0.0.1:2113/streams/newstream" \
  -H "Content-Type:application/vnd.eventstore.events+json" \
  -H "ES-EventType: SomeEvent"
```

Hard deleting
- To permanently delete, use `ES-HardDelete:true` header:

```bash
curl -X DELETE http://127.0.0.1:2113/streams/newstream -H "ES-HardDelete:true"
```

- Hard delete is permanent and will return `410 StreamDeleted` on reads. The stream cannot be recreated after a hard delete.

Example GET after hard delete:

```bash
curl -X GET "http://127.0.0.1:2113/streams/newstream" \
  -H 'Accept: application/vnd.eventstore.events+json'
```

Attempting to append after hard delete will also return `410`.

---

## Description document

- The description document exposes supported methods for a stream (useful for clients supporting competing consumers, etc.).
- Clients can request the description document explicitly with Accept header `application/vnd.eventstore.streamdesc+json`:

```bash
curl -i http://localhost:2113/streams/newstream \
  -H "accept:application/vnd.eventstore.streamdesc+json"
```

- The document contains links like `streams`, `streamSubscription` and indicates available subscription methods. If no subscriptions exist, `streamSubscription` may be absent.

---

## Optimistic concurrency and idempotence

Idempotence
- All HTTP operations are idempotent if the client keeps the same EventId across retries (and if expected version is applied).
- Clients should retry on unknown failures (timeouts, broken connections) using the same UUID.
- Example:

```bash
curl -i -d @event.txt "http://127.0.0.1:2113/streams/newstream"
```

If retried with same EventId the event will only be stored once.

Tip:
- This enables a simple retry policy ("if you get an unknown condition, retry") to work reliably.

---

## Stream metadata

- Each stream has an associated metadata stream prefixed by `$$` (e.g., stream `foo` has metadata stream `$$foo`).
- Metadata contains ACLs, `$maxCount`, `$maxAge`, `TruncateBefore`, and can include application-specific metadata used by clients or projections.
- Metadata is stored as JSON and can be accessed over the HTTP API.

Reading stream metadata
- Follow the metadata link provided by the stream resource (do not hardcode metadata URLs).
- Example to get metadata for `$users` stream:

```bash
curl -i -H "Accept:application/vnd.eventstore.atom+json" \
  http://127.0.0.1:2113/streams/%24users --user admin:changeit
```

Then fetch the metadata stream resource:

```bash
curl -i -H "Accept:application/vnd.eventstore.atom+json" http://127.0.0.1:2113/streams/%24users/metadata --user admin:changeit
```

- If security is enabled and credentials are required, a missing credential will return `401 Unauthorized`.

Writing metadata
- POST a metadata event to the metadata resource.

Example metadata payload (metadata.json):

```json
[
  {
    "eventId": "7c314750-05e1-439f-b2eb-f5b0e019be72",
    "eventType": "$user-updated",
    "data": {
      "readRole": "$all",
      "metaReadRole": "$all"
    }
  }
]
```

POST metadata:

```bash
curl -i -d @metadata.json http://127.0.0.1:2113/streams/%24users/metadata \
  --user admin:changeit \
  -H "Content-Type: application/vnd.eventstore.events+json"
```

- You can include user-specified metadata fields (e.g., which adapter populates a stream, which projection created a stream, process correlation IDs).
- If the caller lacks permission to write stream metadata, a `401 Unauthorized` response is returned.

---