# EventStoreDB 22.10 — Projections (examples & HTTP API)

This page shows practical examples for user-defined projections: adding sample data, creating projections, querying state, emitting projection state to streams, configuring projection properties, partitioned projections (per-stream state), and the HTTP projection management API.

---

## Inspect all projections

List all known projections (returning JSON) and optionally filter for name/status:

```bash
curl -i http://localhost:2113/projections/any \
  -H "accept:application/json" -u "admin:changeit" \
  | grep -E 'effectiveName|status'
```

Sample output snippet:

```
"effectiveName": "$streams",
"status": "Running",
"statusUrl": "http://localhost:2113/projection/$streams",
"effectiveName": "$stream_by_category",
"status": "Running",
"statusUrl": "http://localhost:2113/projection/$stream_by_category",
"effectiveName": "$by_category",
"status": "Running",
"statusUrl": "http://localhost:2113/projection/$by_category",
"effectiveName": "$by_event_type",
"status": "Running",
"statusUrl": "http://localhost:2113/projection/$by_event_type",
"effectiveName": "$by_correlation_id",
"status": "Running",
"statusUrl": "http://localhost:2113/projection/$by_correlation_id",
```

---

## Add sample data

Download or prepare these sample files and append each to its own stream:

- shoppingCart-b989fe21-9469-4017-8d71-9820b8dd1164.json
- shoppingCart-b989fe21-9469-4017-8d71-9820b8dd1165.json
- shoppingCart-b989fe21-9469-4017-8d71-9820b8dd1166.json
- shoppingCart-b989fe21-9469-4017-8d71-9820b8dd1167.json

Append each file to its stream:

```bash
curl -i -d "@shoppingCart-b989fe21-9469-4017-8d71-9820b8dd1164.json" \
  -u "admin:changeit" \
  "http://127.0.0.1:2113/streams/shoppingCart-b989fe21-9469-4017-8d71-9820b8dd1164" \
  -H "Content-Type:application/vnd.eventstore.events+json"

curl -i -d "@shoppingCart-b989fe21-9469-4017-8d71-9820b8dd1165.json" \
  -u "admin:changeit" \
  "http://127.0.0.1:2113/streams/shoppingCart-b989fe21-9469-4017-8d71-9820b8dd1165" \
  -H "Content-Type:application/vnd.eventstore.events+json"

curl -i -d "@shoppingCart-b989fe21-9469-4017-8d71-9820b8dd1166.json" \
  -u "admin:changeit" \
  "http://127.0.0.1:2113/streams/shoppingCart-b989fe21-9469-4017-8d71-9820b8dd1166" \
  -H "Content-Type:application/vnd.eventstore.events+json"

curl -i -d "@shoppingCart-b989fe21-9469-4017-8d71-9820b8dd1167.json" \
  -u "admin:changeit" \
  "http://127.0.0.1:2113/streams/shoppingCart-b989fe21-9469-4017-8d71-9820b8dd1167" \
  -H "Content-Type:application/vnd.eventstore.events+json"
```

---

## Creating your first projection (count Xbox One S items)

The projection below counts occurrences of "Xbox One S" in ItemAdded events. It uses `fromAll()` and a `$init` to initialize state.

Projection JavaScript (xbox-one-s-counter.js):

```javascript
fromAll()
.when({
    $init: function(){
        return { count: 0 }
    },
    ItemAdded: function(s,e){
        if (e.body.Description.indexOf("Xbox One S") >= 0) {
            s.count += 1;
        }
    }
})
```

Create and start as a continuous projection via HTTP:

```bash
curl -i --data-binary "@xbox-one-s-counter.js" \
  "http://localhost:2113/projections/continuous?name=xbox-one-s-counter%26type=js%26enabled=true%26emit=true%26trackemittedstreams=true" \
  -u admin:changeit
```

You can also create the projection using the Admin UI → Projections → New Projection.

---

## Querying projection state

For projections with a single global state, query the state:

```bash
curl -i http://localhost:2113/projection/xbox-one-s-counter/state -u "admin:changeit"
```

Expected response (example):

```json
{
  "count": 3
}
```

---

## Emitting projection state to a stream (outputState)

To have the projection emit its state as events (so subscribers can react), use `.outputState()`.

Updated projection (xbox-one-s-counter-outputState.js):

```javascript
fromAll()
.when({
    $init: function(){
        return { count: 0 }
    },
    ItemAdded: function(s,e){
        if (e.body.Description.indexOf("Xbox One S") >= 0) {
            s.count += 1;
        }
    }
})
.outputState()
```

Update the projection query (emit=yes) via HTTP PUT:

```bash
curl -i -X PUT --data-binary @"xbox-one-s-counter-outputState.js" \
  "http://localhost:2113/projection/xbox-one-s-counter/query?emit=yes" \
  -u admin:changeit
```

Reset the projection so it reprocesses from the beginning and starts emitting:

```bash
curl -i -X POST "http://localhost:2113/projection/xbox-one-s-counter/command/reset" \
  -H "accept:application/json" -H "Content-Length:0" -u admin:changeit
```

Sample confirmation:

```json
{
  "msgTypeId": 293,
  "name": "xbox-one-s-counter"
}
```

Read emitted events from the result stream (default name `$projections-{projection-name}-result`), embed bodies and grep for data:

```bash
curl -i "http://localhost:2113/streams/$projections-xbox-one-s-counter-result?embed=body" \
  -H "accept:application/json" -u admin:changeit | grep data
```

---

## Configure projection properties (options)

You can change projection settings using `options({...})`. Example: change the default result stream name.

Projection with options (update-projection-options.js):

```javascript
options({
  resultStreamName: "xboxes"
})

fromAll()
.when({
    $init: function(){
        return { count: 0 }
    },
    ItemAdded: function(s,e){
        if (e.body.Description.indexOf("Xbox One S") >= 0) {
            s.count += 1;
        }
    }
})
.outputState()
```

Update via HTTP PUT:

```bash
curl -i -X PUT -d "@update-projection-options.js" \
  "http://localhost:2113/projection/xbox-one-s-counter/query?emit=yes" -u admin:changeit
```

Then read the new result stream:

```bash
curl -i "http://localhost:2113/streams/xboxes?embed=body" \
  -H "accept:application/json" -u admin:changeit | grep data
```

---

## Per-category / per-stream partitioned projection (shopping cart item counts)

Enable the built-in category projection to group streams by category:

```bash
curl -i -d {} http://localhost:2113/projection/$by_category/command/enable -u admin:changeit
```

By default the category projection splits stream ids on the first `-`, producing `$ce-{category}` streams. Examples:

- `shoppingCart-54` → category `shoppingCart`
- `shoppingCart-v1-54` → category `shoppingCart`
- `shoppingCart` → no category

Create a projection that counts items per shopping cart using `fromCategory('shoppingCart')` and `foreachStream()`:

Projection (shopping-cart-counter.js):

```javascript
fromCategory('shoppingCart')
.foreachStream()
.when({
    $init: function(){
        return { count: 0 }
    },
    ItemAdded: function(s,e){
        s.count += 1;
    }
})
```

Create as a continuous projection (emit to result stream if desired):

```bash
curl -i --data-binary "@shopping-cart-counter.js" \
  "http://localhost:2113/projections/continuous?name=shopping-cart-item-counter%26type=js%26enabled=true%26emit=true%26trackemittedstreams=true" \
  -u admin:changeit
```

---

## Querying projection state by partition

Partitioned projections maintain a state per partition (e.g., per shopping cart stream). Query a partition by providing the partition key in the request:

```bash
curl -i "http://localhost:2113/projection/shopping-cart-item-counter/state?partition=shoppingCart-b989fe21-9469-4017-8d71-9820b8dd1164" \
  -u "admin:changeit"
```

Example response (partition state):

```json
{
  "count": 2
}
```

---

## Projections HTTP API summary

List projections:

- GET `/projections/any` — all known projections
- GET `/projections/all-non-transient` — all known non-ad-hoc projections

Manage continuous projections:

- GET `/projections/continuous` — list continuous projections
- POST `/projections/continuous?name={name}&type={type}&enabled={enabled}&emit={emit}&trackemittedstreams={trackemittedstreams}` — create continuous projection

Manage transient (ad-hoc) projections:

- GET `/projections/transient` — list transient projections
- POST `/projections/transient?name={name}&type={type}&enabled={enabled}` — create transient projection

Manage one-time projections:

- GET `/projections/onetime` — list one-time projections
- POST `/projections/onetime?name={name}&type={type}&enabled={enabled}&checkpoints={checkpoints}&emit={emit}&trackemittedstreams={trackemittedstreams}` — create one-time projection

Manage a projection (detailed):

- GET `/projection/{name}` — get info
- GET `/projection/{name}/query?config={config}` — get definition (and config if requested)
- PUT `/projection/{name}/query?type={type}&emit={emit}` — update projection query
- DELETE `/projection/{name}?deleteStateStream={deleteStateStream}&deleteCheckpointStream={deleteCheckpointStream}&deleteEmittedStreams={deleteEmittedStreams}` — delete projection (optionally delete streams it created)
- GET `/projection/{name}/statistics` — get projection statistics
- GET `/projection/{name}/state?partition={partition}` — query projection state (optionally partition)
- GET `/projection/{name}/result?partition={partition}` — query projected result
- POST `/projection/{name}/command/enable?enableRunAs={enableRunAs}` — enable projection
- POST `/projection/{name}/command/disable?enableRunAs={enableRunAs}` — disable projection
- POST `/projection/{name}/command/reset?enableRunAs={enableRunAs}` — reset projection (re-emits events; emitted streams are soft deleted)
- POST `/projection/{name}/command/abort?enableRunAs={enableRunAs}` — abort projection

Common query parameters and flags:

- name — projection name
- type — `js` (JavaScript) (native runtime not commonly used)
- enabled — `true`/`false`
- emit — allow projection to write/link events (`true`/`false`)
- trackemittedstreams — record streams emitted by the projection (`true`/`false`)
- partition — partition key when querying per-partition state
- enableRunAs — allow projection to run as requesting user

---

This file documents practical projection examples (creating, emitting, configuring, partitioning) and the HTTP projection management API for EventStoreDB 22.10.