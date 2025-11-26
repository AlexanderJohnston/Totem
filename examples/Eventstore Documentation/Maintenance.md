# EventStoreDB 22.10 — Maintenance

EventStoreDB requires regular maintenance for three main operational concerns:

- Scavenging — reclaim disk space and remove deleted/expired events.
- Backup and restore — disaster recovery and data portability.
- Certificate updates — rolling renewal of TLS certificates.

You may also need to look after diagnostics and indexes as part of routine operations.

---

## Scavenging

Events that are deleted or expired (by stream metadata) stop appearing in stream reads but remain physically present in the database and are visible from the `$all` stream. To remove these events and reclaim disk space, run a scavenge on each node.

A scavenge creates a copy of affected chunk(s) without the removed events, deletes the old chunk, and updates indexes. Scavenging is destructive — once a scavenge has removed events they cannot be recovered except from backups.

Warning
- Scavenging is destructive. After a scavenge you cannot recover removed events except from a backup.

### Starting a scavenge

Start a scavenge via the HTTP API (admin or ops credentials):

```bash
curl -i -d {} -X POST http://localhost:2113/admin/scavenge -u "admin:changeit"
```

You can also start scavenges from the Admin UI. Because each node has an independent copy of the DB, run the scavenge on each node (concurrently or in series depending on your load strategy).

### Getting the current scavenge ID

Get the ID of the currently running scavenge (if any):

```bash
curl -i -X GET http://127.0.0.1:2113/admin/scavenge/current -u "admin:changeit"
```

### Stopping a scavenge

Stop a running scavenge by ID:

```bash
curl -i -X DELETE http://localhost:2113/admin/scavenge/{scavengeId} -u "admin:changeit"
```

Or stop the currently running scavenge:

```bash
curl -i -X DELETE http://localhost:2113/admin/scavenge/current -u "admin:changeit"
```

A `200` response is returned after the scavenge has stopped.

Tip
- A scavenge can be stopped at any time. The next scavenge resumes from where the previous one stopped.

### Viewing progress

Scavenge progress is logged in the server logs. During the execution phase, EventStoreDB emits events into scavenge-specific streams; each scavenge creates a stream documenting its events. See the `$scavenges` stream documentation to observe progress and status.

---

## Scavenging best practices

### Backups
- Do not perform file-copy backups while a scavenge is running. Stop the scavenge and resume it after the backup.
- Disk snapshot backups can be taken while a scavenge is running.

### How often to scavenge
Frequency depends on:
- How often you delete streams.
- How you configure `$maxAge`, `$maxCount` or `TruncateBefore` metadata.
- Importance of freeing disk space.
- GDPR or compliance needs.

Use scavenge logs and the `$scavenges` stream to see how much data is being removed and schedule accordingly (cron, Windows Task Scheduler, etc.).

### Spreading the load
Scavenging can be IO-heavy. To reduce impact:
- Run scavenge on one node at a time.
- Run scavenge on follower nodes to avoid leader load; resign leader and scavenge it separately.
- Stop scavenge during peak hours.
- Use throttle and threshold options to limit resource use.

---

## Scavenging algorithm

Scavenging uses the concept of a scavenge point (a log record) that defines:
- the log position to scavenge up to,
- a unique scavenge number,
- EffectiveNow (time used for maxAge decisions),
- and the threshold used to decide which chunks to execute.

A scavenge run is tied to a single scavenge point. This lets you create a scavenge point on one node and later scavenge to the same point on other nodes, producing consistent results.

Phases of the scavenging algorithm:

- Beginning
  - If a previous scavenge stopped, it resumes. Otherwise it checks for an existing scavenge point; if none exists it writes a new scavenge point (replicated to other nodes) and completes the active chunk so it can be scavenged.

- Accumulation phase
  - Reads chunks added since the previous scavenge (up to the scavenge point), gathers tombstones and metadata, and stores information in the scavenge DB. Each chunk is accumulated only once.

  Tip: the first scavenge typically takes longer because it accumulates all chunks.

- Calculation phase
  - Calculates which events can be discarded and assigns weight to chunks based on removable records.

- Execution phase
  - Removes events from chunks and indexes for chunks whose weight meets the threshold. Small chunks may be merged.

- Cleaning phase
  - Removes unneeded data from the scavenge database.

---

## Starting a scavenge — request options

When starting a scavenge via HTTP POST you can pass several query parameters.

- Threads
  Number of threads to run scavenge. Default: `1`.

  Example:

  ```bash
  curl -i -X POST "http://127.0.0.1:2113/admin/scavenge?threads=2" -u "admin:changeit"
  ```

- Threshold
  Only execute chunks whose weight meets the minimum. Possible values:
  - `-1`: scavenge all chunks (even if no events to remove)
  - `0`: default; scavenge any chunk that has events to remove
  - `>0`: minimum weight required to scavenge a chunk

  Example:

  ```bash
  curl -i -X POST "http://127.0.0.1:2113/admin/scavenge?threshold=2000" -u "admin:changeit"
  ```

  Tip
  - A positive threshold means some deleted/expired events may remain (important for GDPR considerations).

- Throttle percent
  Controls scavenge speed/resource usage. `100` runs full speed (default). Lower values make the operation take proportionally longer.

  - Range: `1`–`100`.
  - For multi-threaded scavenges the throttle percent must be `100`.

  Example:

  ```bash
  curl -i -X POST "http://127.0.0.1:2113/admin/scavenge?throttlePercent=50" -u "admin:changeit"
  ```

- Sync Only
  `syncOnly=true` prevents creating a new scavenge point and only runs the scavenge if an existing scavenge point exists that hasn’t yet been reached. Useful to ensure another node scavenges to the same point.

  Example:

  ```bash
  curl -i -X POST "http://127.0.0.1:2113/admin/scavenge?syncOnly=true" -u "admin:changeit"
  ```

- Start From Chunk
  Deprecated — ignored and will be removed.

---

## Scavenging database options

Options that change scavenging behavior on the server node:

- Disable chunk merging
  - CLI: `--disable-scavenge-merging`
  - YAML: `DisableScavengeMerging`
  - ENV: `EVENTSTORE_DISABLE_SCAVENGE_MERGING`
  - Default: `false` (small scavenged chunks are merged into ~256 MB files).

- Scavenge history retention
  - CLI: `--scavenge-history-max-age`
  - YAML: `ScavengeHistoryMaxAge`
  - ENV: `EVENTSTORE_SCAVENGE_HISTORY_MAX_AGE`
  - Default: `30` (days). Controls how long scavenge history streams are retained.

- Always keep scavenged (Deprecated)
  - Ensures newer chunk from a scavenge is always kept (deprecated).

- Scavenge backend page size
  - CLI: `--scavenge-backend-page-size`
  - YAML: `ScavengeBackendPageSize`
  - ENV: `EVENTSTORE_SCAVENGE_BACKEND_PAGE_SIZE`
  - Default: `16` KiB.

- Scavenge backend cache size
  - CLI: `--scavenge-backend-cache-size`
  - YAML: `ScavengeBackendCacheSize`
  - ENV: `EVENTSTORE_SCAVENGE_BACKEND_CACHE_SIZE`
  - Default: `64` MiB.

- Scavenge hash users cache capacity
  - CLI: `--scavenge-hash-users-cache-capacity`
  - YAML: `ScavengeHashUsersCacheCapacity`
  - ENV: `EVENTSTORE_SCAVENGE_HASH_USERS_CACHE_CAPACITY`
  - Default: `100000`. Number of stream hashes cached when checking for collisions; increase if you see many cache misses during accumulation.

---

## Backup and restore

Backups should follow the correct ordering and best practices to avoid data corruption and ensure recoverability.

### Types of backups
- Disk snapshots — easiest if infrastructure supports them.
- Regular file copy:
  - Simple full backup — small DB or low append rate.
  - Differential backup — large DB or high append rate.

### Backup and restore best practices
- Back up one node but ensure it is up-to-date and connected. Optionally back up a quorum of nodes for additional safety.
- Don’t file-copy backup a node while scavenge is running.
- Read-only replica nodes are good backup sources.
- Do not mix backup files from different nodes.
- Restore must be performed while the node is stopped.
- Restore can run on any node; you may use the same backup to restore multiple nodes.
- After restoring the node that was the backup source, perform a full backup again.

### Database files information
Two primary directories to include in backups (locations may vary by config):

- `db/` — data chunks and database checkpoint files:
  - chunk files: `chunk-X.Y` (`X` chunk number, `Y` version)
  - checkpoint files: `*.chk` (e.g., `chaser.chk`, `epoch.chk`, `proposal.chk`, `truncate.chk`, `writer.chk`)
- `index/` — indexes and index metadata:
  - `indexmap`
  - index files named by UUID (e.g., `5a1a8395-...`)
  - `index` checkpoint files: `**/*.chk` under the index dir

### Disk snapshots
- If `db/` and `index/` are on the same volume, a single snapshot suffices.
- If on different volumes, snapshot `index/` first, then `db/`.

---

### Simple full backup & restore

Backup steps (example assumes `data/` contains DB and `data/index` contains index):

```bash
# Copy index checkpoint files
rsync -aIR data/./index/**/*.chk backup

# Copy rest of index files (excluding checkpoints)
rsync -aI --exclude '*.chk' data/index backup

# Copy database checkpoint files
rsync -aI data/*.chk backup

# Copy chunk files
rsync -a data/*.0* backup
```

Restore steps:
1. Ensure EventStoreDB is stopped.
2. Copy all backup files back to the node.
3. Create a copy of `chaser.chk` named `truncate.chk` (overwrites restored `truncate.chk`).

---

### Differential backup & restore

Procedure to minimize backup storage (index first, then chunks):

Backup index:
1. If index dir is empty (apart from directories), skip to chunk backup.
2. Copy `index/indexmap` to backup; if not present retry until it exists.
3. List `index/<GUID>` and `index/<GUID>.bloomfilter` files into `indexFiles`.
4. Copy `indexFiles` to backup, skipping files already present.
5. Compare source and backup `indexmap`. If different, repeat from step 2.
6. Remove from backup any index files not listed in the current `indexFiles`.
7. Copy `index/stream-existence/streamExistenceFilter.chk` (if present).
8. Copy `index/stream-existence/streamExistenceFilter.dat` (if present).
9. Copy `index/scavenging/scavenging.db` (if present).

Backup log/chunks:
1. Rename the last chunk in backup to `*.old` (e.g., `chunk-000123.000000` → `chunk-000123.000000.old`).
2. Copy `chaser.chk`, `epoch.chk`, `writer.chk`, `proposal.chk` to backup.
3. List `chunkFiles` (all `chunk-X.Y`) in source.
4. Copy `chunkFiles` to backup, skipping already-copied files.
5. Remove any chunks in backup not present in `chunkFiles` (including the `.old` file).

Restore:
1. Ensure EventStoreDB is stopped.
2. Copy all files to the desired location.
3. Copy `chaser.chk` to `truncate.chk`.

---

### Other options for data recovery

- Additional node (Hot Backup)
  - Increase cluster from 3 → 5 so more copies exist. This slows writes (quorum increases). Alternatively, use a read-only replica to minimize write impact.

- Alternative storage
  - Use a durable subscription to copy events to another store (KV store, column store) for alternate recovery options.

- Backup cluster (primary/secondary)
  - Run a second EventStoreDB cluster and asynchronously replicate via a durable subscription. Keep failover manual to avoid split-brain.

---

## Certificate update upon expiry (rolling update)

To renew TLS certificates, perform a rolling update across the cluster.

### Step 1 — Generate new certificates
- Use `es-gencert-cli` or your existing method to generate a new CA and node certificates.
- Note: since v23.10.0 you can do a rolling update with a different Common Name (CN) provided `CertificateReservedNodeCommonName` is handled during reload.

### Step 2 — Replace the certificates
- Replace certificates on disk or update symlinks to point to the new certs.

Linux example (symlink pattern):
```
ca -> /etc/eventstore/certs/ca
node.crt -> /etc/eventstore/certs/node.crt
node.key -> /etc/eventstore/certs/node.key
```

Update symlinks to point at new files:
```
ca -> /etc/eventstore/newCerts/ca
node.crt -> /etc/eventstore/newCerts/node.crt
node.key -> /etc/eventstore/newCerts/node.key
```

### Step 3 — Reload the configuration (no restart required)

Reload via the admin API:

```bash
curl -k -X POST --basic "https://{nodeAddress}:{HttpPort}/admin/reloadconfig" -u {username}:{password}
```

Example:

```bash
curl -k -X POST --basic https://127.0.0.1:2113/admin/reloadconfig -u admin:changeit
```

On Linux you can also send SIGHUP to the process:

```bash
pidof eventstored   # find PID
kill -s SIGHUP 38956
```

Example relevant log lines on successful reload:

```
[108277,30,14:46:07.453,INF] Reloading the node's configuration since a request has been received on /admin/reloadconfig.
[108277,29,14:46:07.457,INF] Loading the node's certificate(s) from file: "/home/ubuntu/links/node.crt"
[108277,29,14:46:07.488,INF] Loading the node's certificate. Subject: "CN=eventstoredb-node", Previous thumbprint: "...", New thumbprint: "..."
[108277,29,14:46:07.493,INF] Certificate chain verification successful.
[108277,29,14:46:07.494,INF] All certificates successfully loaded.
[108277,29,14:46:07.494,INF] The node's configuration was successfully reloaded
```

Tip
- If certificate loading fails (e.g., file not found), the reload will not take effect and error will be logged (FileNotFoundException).

### Step 4 — Update the other nodes
- Repeat the replacement and reload on each node.

### Step 5 — Monitor the cluster
- Node-to-node connections reset every 10 minutes; update certificates on all nodes within this window to avoid communication errors.
- During rolling update you may see certificate-chain errors in logs until all nodes are updated — these are expected during the window and should be resolved once all nodes reload.

Example error (during rollout):

```
[108277,29,14:59:47.489,ERR] eventstoredb-node : A certificate chain could not be built to a trusted root authority.
[108277,29,14:59:47.489,ERR] Client certificate validation error: "The certificate (CN=eventstoredb-node) provided by the client failed validation ... RemoteCertificateChainErrors (PartialChain)"
```

Warning
- It can take up to 10 minutes for certificate changes to take effect. Update all nodes within this window to avoid connectivity problems.

---

This file summarizes maintenance operations for EventStoreDB 22.10, including scavenging, backup/restore, and certificate renewal procedures, with commands, tips, and warnings preserved for operational safety.
