# EventStoreDB 22.10 — Persistent Subscriptions (HTTP)

This document describes the HTTP API for creating, updating, deleting and consuming persistent subscriptions (competing-consumer groups) on a stream. Persistent subscriptions provide server‑managed, at‑least‑once delivery with consumer groups so multiple clients can share processing of a stream.

Tip: The Admin UI includes a Competing Consumers / Persistent Subscriptions section where you can create, update, delete and view subscriptions and their statuses. Note that persistent subscriptions to `$all` are not supported over HTTP; use a gRPC client for `$all` subscriptions.

---

## Creating a persistent subscription

Create a persistent subscription group for a stream. Creating the same subscription group twice returns an error. Admin permissions are required.

- URI: `/subscriptions/{stream}/{subscription_name}`
- Method: PUT
- Supported content type: `application/json`

Warning: Persistent subscriptions to `$all` are not supported over the HTTP API. Use a gRPC client for `$all`.

### Query parameters
- stream — the stream the persistent subscription is on (part of the path).
- subscription_name — the name of the subscription group (path).

### Body (JSON)
Fields available when creating a subscription:

- resolveLinktos: boolean — whether to resolve link events
- startFrom: integer — start position in the stream
- extraStatistics: boolean — measure client timing histograms
- checkPointAfterMilliseconds: integer — attempt to checkpoint after this many ms
- liveBufferSize: integer — in-memory live buffer size before paging
- readBatchSize: integer — read batch size when paging
- bufferSize: integer — number of messages to buffer when in paging mode
- maxCheckPointCount: integer — max messages not checkpointed before forcing a checkpoint
- maxRetryCount: integer — number of times a message should be retried before it is considered bad
- maxSubscriberCount: integer — maximum number of allowed TCP subscribers
- messageTimeoutMilliseconds: integer — timeout for a client before message is retried
- minCheckPointCount: integer — minimum messages to write a checkpoint for
- namedConsumerStrategy: string — one of `RoundRobin`, `DispatchToSingle`, `Pinned`, `PinnedByCorrelation` (implementation may accept `RoundRobin`/`DispatchToSingle`/`Pinned`)

Example body (minimal):

```json
{
  "resolveLinktos": false,
  "startFrom": 0,
  "extraStatistics": false,
  "checkPointAfterMilliseconds": 1000,
  "liveBufferSize": 500,
  "readBatchSize": 20,
  "bufferSize": 500,
  "maxCheckPointCount": 500,
  "maxRetryCount": 10,
  "maxSubscriberCount": 10,
  "messageTimeoutMilliseconds": 10000,
  "minCheckPointCount": 10,
  "namedConsumerStrategy": "RoundRobin"
}
```

---

## Updating a persistent subscription

You can update settings of an existing subscription while it is running. Updating drops current subscribers and resets the subscription internally. Admin permissions are required.

- URI: `/subscriptions/{stream}/{subscription_name}`
- Method: POST
- Supported content type: `application/json`

Warning: Persistent subscriptions to `$all` are not supported over the HTTP API. Use gRPC to update `$all` subscriptions.

Query parameters and body fields are the same as for creation.

---

## Deleting a persistent subscription

- URI: `/subscriptions/{stream}/{subscription_name}`
- Method: DELETE
- Supported content type: `application/json`

Warning: Deleting persistent subscriptions to `$all` is not supported over HTTP; use gRPC for `$all`.

Query parameters:
- stream — the stream to delete the persistent subscription on (path)
- subscription_name — the subscription group name (path)

---

## Reading a stream via a persistent subscription

Reading from a persistent subscription returns one or more events in a competing-consumer feed format. By default, a GET returns a single event per request and does not embed full event properties unless requested.

- URIs:
  - `/subscriptions/{stream}/{subscription_name}`
  - `/subscriptions/{stream}/{subscription_name}?embed={embed}`
  - `/subscriptions/{stream}/{subscription_name}/{count}?embed={embed}`
- Method: GET
- Supported content types:
  - `application/vnd.eventstore.competingatom+xml`
  - `application/vnd.eventstore.competingatom+json`

Query parameters:
- stream — the stream the subscription is for (path)
- subscription_name — subscription group (path)
- count — how many events to return (optional, path segment)
- embed — one of: `None`, `Content`, `Rich`, `Body`, `PrettyBody`, `TryHarder` (affects how much of the event is embedded; see "Reading streams" documentation for details)

Example (single event feed response — simplified):

```json
{
  "title": "All Events Persistent Subscription",
  "id": "http://localhost:2113/subscriptions/newstream/competing_consumers_group1",
  "updated": "2015-12-02T09:17:48.556545Z",
  "author": { "name": "EventStore" },
  "headOfStream": false,
  "links": [
    { "uri": "http://localhost:2113/subscriptions/newstream/competing_consumers_group1/ack%3Fids=c322e299-cb73-4b47-97c5-5054f920746f", "relation": "ackAll" },
    { "uri": "http://localhost:2113/subscriptions/newstream/competing_consumers_group1/nack%3Fids=c322e299-cb73-4b47-97c5-5054f920746f", "relation": "nackAll" },
    { "uri": "http://localhost:2113/subscriptions/newstream/competing_consumers_group1/1%3Fembed=None", "relation": "previous" },
    { "uri": "http://localhost:2113/subscriptions/newstream/competing_consumers_group1", "relation": "self" }
  ],
  "entries": [
    {
      "title": "1@newstream",
      "id": "http://localhost:2113/streams/newstream/1",
      "updated": "2015-12-02T09:17:48.556545Z",
      "author": { "name": "EventStore" },
      "summary": "SomeEvent",
      "links": [
        { "uri": "http://localhost:2113/streams/newstream/1", "relation": "edit" },
        { "uri": "http://localhost:2113/streams/newstream/1", "relation": "alternate" },
        { "uri": "http://localhost:2113/subscriptions/newstream/competing_consumers_group1/ack/c322e299-cb73-4b47-97c5-5054f920746f", "relation": "ack" },
        { "uri": "http://localhost:2113/subscriptions/newstream/competing_consumers_group1/nack/c322e299-cb73-4b47-97c5-5054f920746f", "relation": "nack" }
      ]
    }
  ]
}
```

---

## Acknowledgements (ack / nack)

Clients must acknowledge (ACK) or not-acknowledge (NACK) messages. If a message is not acknowledged within the configured timeout it will be retried. Use the rel links provided in the feed (do not bookmark ack/nack URIs).

### Ack multiple messages
- URI: `/subscriptions/{stream}/{subscription_name}/ack?ids={messageids}`
- Method: POST
- Supported content type: `application/json`
- Query parameters:
  - stream, subscription_name (path)
  - messageids — comma-separated message IDs to ACK

### Ack a single message
- URI: `/subscriptions/{stream}/{subscription_name}/ack/{messageid}`
- Method: POST
- Supported content type: `application/json`

### Nack multiple messages
- URI: `/subscriptions/{stream}/{subscription_name}/nack?ids={messageids}&action={action}`
- Method: POST
- Supported content type: `application/json`
- Action parameter values:
  - `Park` — do not retry; park it until replayed
  - `Retry` — retry the message
  - `Skip` — discard the message
  - `Stop` — stop the subscription

### Nack a single message
- URI: `/subscriptions/{stream}/{subscription_name}/nack/{messageid}?action={action}`
- Method: POST
- Supported content type: `application/json`

---

## Replaying parked messages

Replay parked messages for a subscription (pushes parked messages to subscribers before new events):

- URI: `/subscriptions/{stream}/{subscription_name}/replayParked`
- Method: POST
- Supported content type: `application/json`

Example to replay a limited number of parked events via Admin HTTP (alternate endpoint also exists on streams for parked messages):

```bash
curl -i -X POST -d {} "https://localhost:2113/subscriptions/{stream}/{groupname}/replayParked?stopAt={numberOfEvents}" -u "admin:changeit"
```

---

## Getting subscription information

### List all subscriptions
- URI: `/subscriptions`
- Method: GET
- Response: JSON array of subscriptions

Example response (simplified):

```json
[
  {
    "links": [{ "href": "http://localhost:2113/subscriptions/newstream/competing_consumers_group1/info", "rel": "detail" }],
    "eventStreamId": "newstream",
    "groupName": "competing_consumers_group1",
    "parkedMessageUri": "http://localhost:2113/streams/$persistentsubscription-newstream::competing_consumers_group1-parked",
    "getMessagesUri": "http://localhost:2113/subscriptions/newstream/competing_consumers_group1/1",
    "status": "Live",
    "averageItemsPerSecond": 0.0,
    "totalItemsProcessed": 0,
    "lastProcessedEventNumber": -1,
    "lastKnownEventNumber": 5,
    "connectionCount": 0,
    "totalInFlightMessages": 0
  },
  {
    "links": [{ "href": "http://localhost:2113/subscriptions/another_newstream/competing_consumers_group1/info", "rel": "detail" }],
    "eventStreamId": "another_newstream",
    "groupName": "competing_consumers_group1",
    "parkedMessageUri": "http://localhost:2113/streams/$persistentsubscription-another_newstream::competing_consumers_group1-parked",
    "getMessagesUri": "http://localhost:2113/subscriptions/another_newstream/competing_consumers_group1/1",
    "status": "Live",
    "averageItemsPerSecond": 0.0,
    "totalItemsProcessed": 0,
    "lastProcessedEventNumber": -1,
    "lastKnownEventNumber": -1,
    "connectionCount": 0,
    "totalInFlightMessages": 0
  }
]
```

### Get subscriptions for a stream
- URI: `/subscriptions/{stream}`
- Method: GET
- Supported content type: `application/json`
- Response: JSON array of subscription info objects (similar to list above)

Example response structure shown in the previous section.

### Get a specific subscription
- URI: `/subscriptions/{stream}/{subscription_name}/info`
- Method: GET
- Supported content type: `application/json`

Example response (simplified):

```json
{
  "links": [
    { "href": "http://localhost:2113/subscriptions/newstream/competing_consumers_group1/info", "rel": "detail" },
    { "href": "http://localhost:2113/subscriptions/newstream/competing_consumers_group1/replayParked", "rel": "replayParked" }
  ],
  "config": {
    "resolveLinktos": false,
    "startFrom": 0,
    "messageTimeoutMilliseconds": 10000,
    "extraStatistics": false,
    "maxRetryCount": 10,
    "liveBufferSize": 500,
    "bufferSize": 500,
    "readBatchSize": 20,
    "preferRoundRobin": true,
    "checkPointAfterMilliseconds": 1000,
    "minCheckPointCount": 10,
    "maxCheckPointCount": 500,
    "maxSubscriberCount": 10,
    "namedConsumerStrategy": "RoundRobin"
  },
  "eventStreamId": "newstream",
  "groupName": "competing_consumers_group1",
  "status": "Live",
  "averageItemsPerSecond": 0.0,
  "parkedMessageUri": "http://localhost:2113/streams/$persistentsubscription-newstream::competing_consumers_group1-parked",
  "getMessagesUri": "http://localhost:2113/subscriptions/newstream/competing_consumers_group1/1",
  "totalItemsProcessed": 0,
  "countSinceLastMeasurement": 0,
  "lastProcessedEventNumber": -1,
  "lastKnownEventNumber": 5,
  "readBufferCount": 6,
  "liveBufferCount": 5,
  "retryBufferCount": 0,
  "totalInFlightMessages": 0,
  "connections": []
}
```

---

## Notes, tips and warnings

- Use the rel links in subscription feeds for ack/nack operations; do not hardcode or bookmark URIs as link structure may change.
- Persistent subscriptions execute on the Leader node; they put additional CPU / IO load on the leader. Plan capacity accordingly.
- Persistent subscriptions are server-managed and maintain their own checkpoints; subscribers can be restarted/replaced without losing the subscription’s place.
- Ordering is not guaranteed with competing consumers. If you need strict ordering guarantees, use a client-managed catch-up subscription and manage checkpoints on the client.
- Parked messages are persisted to streams named like:
  - `$persistentsubscription-{stream}::{groupName}-parked`
- Replay parked messages to re-deliver them before new events.
- Admin permissions are required for creation, updating and deletion of persistent subscriptions via HTTP.
- To operate on `$all` subscriptions (create/update/delete), use the appropriate gRPC client API — HTTP does not support persistent subs to `$all`.

---

This file converts the Persistent Subscriptions (HTTP) reference into a clean Markdown page for EventStoreDB 22.10, preserving endpoints, parameters, examples, responses, tips and warnings.