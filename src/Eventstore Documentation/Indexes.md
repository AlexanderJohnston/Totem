# EventStoreDB 22.10 — Indexes

EventStoreDB stores indexes separately from the main data files and uses them to access records by stream name.

## Indexing

### Overview

- EventStoreDB creates index entries while processing commit events and keeps them in memory (memtables) until the MaxMemTableSize is reached.
- When persisted, memtables are written to disk in the index folder as PTable files, accompanied by `.bloomfilter` files and an `indexmap` file.
- The `indexmap` describes file order and level, contains the data checkpoint for the last written file, the indexmap version, and a checksum.
- Indexes are sorted lists based on hashes of stream names. To speed lookups, EventStoreDB stores midpoints that map stream hashes to physical offsets.
- Files are merged automatically: when more than two files exist at the same level they are merged into a single file at the next level.
- Each index entry is 24 bytes. Rough file sizing: ~24 MB per 1M entries. Bloom filter files are ~1% of index size.

Index file sizing by level (assuming default MaxMemTableSize = 1M):
- Level 1: 1M entries — ~24 MB
- Level 2: 2M entries — ~48 MB
- Level 3: 4M entries — ~96 MB
- Level 4: 8M entries — ~192 MB
- Level 5: 16M entries — ~384 MB
- Level 6: 32M entries — ~768 MB
- Level 7: 64M entries — ~1536 MB
- Level 8: 128M entries — ~3072 MB
- Level n: 2^(n-1) * 1M entries — 2^(n-1) * 24 MB

Level 0 is the in-memory memtable level (generally only one level 0 table).

### Indexing in depth

The following details are primarily useful for developers working on the indexing subsystem, crash recovery, or tuning.

#### Index map files

- `indexmap` files are line-delimited text files. Line delimiters depend on the OS; if editing to fix corruption, do so on the same OS where the cluster runs.
- Structure:
  - `hash` — MD5 hash of the rest of the file
  - `version` — indexmap file version
  - `checkpoint` — maximum prepare/commit position of persisted PTables
  - `maxAutoMergeLevel` — value of MaxAutoMergeIndexLevel or `int32.MaxValue` if unset
  - `ptable,level,index` — list of all ptables with level and order
- Writing is done via a safe replace: write to a temporary file, delete original, rename temporary to final. EventStoreDB retries up to 5 times. If a crash occurs during this two-phase process, EventStoreDB recovers by rebuilding indexes from the checkpoint.

#### Writing and merging of index files

- Persisting memtables, merging PTables, and updating `indexmap` happen on background thread(s), performed serially on a thread-pool thread. Additional operations are queued.
- If merging produces conditions that trigger further merges at the next level, those merges are queued immediately.
- For safety, source PTables are only deleted after the new merged PTable is persisted and the `indexmap` updated.
- On crash, EventStoreDB deletes any files not listed in the indexmap and reindexes from the stored prepare/commit checkpoint.

#### Manual merging

- If you set MaxAutoMergeIndexLevel, files above that level will not be merged automatically.
- Trigger manual merges via the `/admin/mergeindexes` endpoint or via `es-cli` (available with commercial support).
- Manual merge merges all tables at or above the configured maximum merge level into a single table. If only one table exists at that level or above, no merge is performed.

#### Stream existence filter

- Located under:
  - `/index/stream-existence/streamExistenceFilter.chk` (checkpoint)
  - `/index/stream-existence/streamExistenceFilter.dat` (persisted Bloom filter)
- The `.dat` file is a persisted Bloom filter; the `.chk` file stores the log position the filter has processed up to.
- Backup procedure: back up the checkpoint (`.chk`) before the `.dat` file.
- Purpose: quickly determine whether a stream might exist or definitely does not exist to avoid index lookups when creating/appending to streams that did not previously exist.
- Building or resizing the filter requires a full read-through of the index (initial build or rebuild on resize).

## Configuration options affecting indexing

The options that affect indexing are summarized below. Each option can usually be set via command line, YAML, or environment variable.

- Index — controls where indexes are stored
- MaxMemTableSize — how many entries to keep in memory before flushing to disk
- IndexCacheDepth — minimum number of midpoints calculated for an index file
- SkipIndexVerify — skip index hash verification at startup
- MaxAutoMergeIndexLevel — max index level to auto-merge before requiring manual merge
- StreamExistenceFilterSize — size in bytes for the stream existence filter
- IndexCacheSize — maximum number of entries in each index LRU cache
- UseIndexBloomFilters — feature flag to enable/disable index Bloom filters

Read the option details below to choose appropriate values for your workload.

### Index location
- Command line: `--index`
- YAML: `Index`
- Environment variable: `EVENTSTORE_INDEX`
- Default: data files location

Recommendation: place index files on a separate drive to reduce IO contention between data, index, and log files.

### Memtable size (MaxMemTableSize)
- Command line: `--max-mem-table-size`
- YAML: `MaxMemTableSize`
- Environment variable: `EVENTSTORE_MAX_MEM_TABLE_SIZE`
- Default: `1000000`

Impacts:
- Disk IO for writing index files, index seek time, and startup time.
- Larger `MaxMemTableSize` reduces the frequency of index file writes and merges (fewer files) but increases startup time (rebuilding in-memory index takes longer).
- Larger memtables reduce cross-file seeks for streams that span multiple index files.

### Index cache depth (IndexCacheDepth)
- Command line: `--index-cache-depth`
- YAML: `IndexCacheDepth`
- Environment variable: `EVENTSTORE_INDEX_CACHE_DEPTH`
- Default: `16`

Impacts:
- Controls how many midpoints are calculated per index file.
- Increasing reduces the amount of per-file scanning after finding a midpoint (more dense midpoints), at the cost of slightly larger files and more memory for midpoints.
- Default (`16`) supports index files up to ~1.5 GB to be fully searchable using midpoints. After that, a seek distance limit is used (maximum effective scanned entries ≈ distance/24 bytes).

### Skip index verification (SkipIndexVerify)
- Command line: `--skip-index-verify`
- YAML: `SkipIndexVerify`
- Environment variable: `EVENTSTORE_SKIP_INDEX_VERIFY`
- Default: `false`

Behavior:
- When true, EventStoreDB skips recalculating index hashes and reads midpoints from the file footer to reduce startup time.
- Risk: reduced verification increases the chance that corruption is not detected until the corrupted entries are read; corrupted indexes will require rebuilds from chunk files.
- For ZFS on Linux you can safely disable this because the filesystem provides checksums.

### Auto-merge index level (MaxAutoMergeIndexLevel)
- Command line: `--max-auto-merge-index-level`
- YAML: `MaxAutoMergeIndexLevel`
- Environment variable: `EVENTSTORE_MAX_AUTO_MERGE_INDEX_LEVEL`
- Default: `2147483647` (int32.MaxValue)

Behavior:
- Controls the maximum index level that EventStoreDB will merge automatically.
- Large merges (higher levels) use large amounts of disk IO. Set this value to avoid very large automatic merges and perform large merges manually during controlled maintenance windows.
- Example: merging two level 7 files implies ~3072 MB reads and ~3072 MB writes; merging level 8 files roughly doubles that.

### Stream existence filter size (StreamExistenceFilterSize)
- Command line: `--stream-existence-filter-size`
- YAML: `StreamExistenceFilterSize`
- Environment variable: `EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE`
- Default: `256000000` (bytes)

Behavior:
- Amount of memory and disk space to use for the stream existence filter.
- Should be roughly the maximum number of streams you expect (e.g., 500 million streams → ~500000000).
- The value should fit entirely in memory to avoid performance degradation. Use `0` to disable the filter.
- Upgrading to a version that supports the filter requires building it, which reads through the whole index. Resizing triggers a full rebuild.

### Index cache size (IndexCacheSize)
- Command line: `--index-cache-size`
- YAML: `IndexCacheSize`
- Environment variable: `EVENTSTORE_INDEX_CACHE_SIZE`
- Default: `0`

Behavior:
- Maximum number of entries in each index LRU cache. Default `0` (disabled) because of memory overhead and negative effects on workloads with many cache misses.
- Useful for read-heavy workloads of long-lived streams.
- LRU cache is only created for index files that have Bloom filters.

### Use index Bloom filters (UseIndexBloomFilters)
- Command line: `--use-index-bloom-filters`
- YAML: `UseIndexBloomFilters`
- Environment variable: `EVENTSTORE_USE_INDEX_BLOOM_FILTERS`
- Default: `true`

Behavior:
- Feature flag to disable index Bloom filters (provided as a safety escape; intended to be removed in a future release).
- When enabled, EventStoreDB creates a `.bloomfilter` file for each new PTable. Bloom filters describe which streams are present in the PTable and speed up reads by avoiding searches in PTables that don't contain the stream.
- Immediately after upgrade, existing PTables will not have Bloom filters; new PTables will get them naturally as files are produced, or rebuild the index for immediate generation.

## Tuning indexes

- Defaults are suitable for most clusters. Tuning is primarily for clusters with very large event counts or constrained environments.
- The most common tuning is setting `MaxAutoMergeIndexLevel` to avoid very large merges occurring on all nodes at once (large merges consume high IOPS).
- Because changing `MaxAutoMergeIndexLevel` requires an index rebuild, start with a conservative (higher) value and decrease it until you reach a balance between manual-merge operational cost and automatic-merge IOPS cost.
- Example: on a small cloud node with ~3000 256B IOPS (~0.73 Gb/s), an index merge at level 7+ may consume most or all IOPS and make nodes appear to stall. Setting `MaxAutoMergeIndexLevel` to 6 or below avoids these large automatic merges and allows manual control of merges on individual nodes to keep read/write latency more consistent.
