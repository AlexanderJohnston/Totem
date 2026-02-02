# Plan: Copilot agent skill — In-memory Totem timeline simulator

## Goal
Create a Copilot “agent skill/custom agent” prompt file that lets a user *simulate* Totem’s runtime rules (timeline/events/topics/workflows/queries/reports) entirely in-memory. The user can “pretend” to call HTTP-style endpoints:
- **GET**: read a query (optionally by `id`/route)
- **POST**: issue a command, which the simulator routes to topic/workflow handlers and appends resulting events

The simulator maintains a **single JSON state** containing:
- timeline positions (append-only)
- topic instance state (by topic type + route id)
- query instance state (by query type + route id)
- (optional) workflow queue / report rows

## Current repo state (what I found)
- The primary spec for runtime rules is `examples\\Totem Documentation\\readme.md`.
- Totem’s key runtime rules described there:
  - Timeline processes events in order; `When(...)` handlers fully complete before moving to next event.
  - **Topics** maintain state via `Given(Event)` and make decisions via `When(Command)`, emitting new events via `Then(...)` (added *after* `When` completes, with “cause” set to current position).
  - If a Topic has both `Given` and `When` for the same event, `Given` runs first.
  - **Workflows** observe events via `When(Event)` and enqueue follow-up commands via `ThenEnqueue(...)`.
  - **Reports** write row-shaped projections via `When(Event)` and track checkpoints.
  - HTTP boundary concepts: web host reads queries with GET and issues commands with POST/PUT/DELETE.
  - Query caching/concurrency: responses include a version (e.g., ETag) based on last observed timeline position.
- In code, the HTTP server uses generic MVC controllers:
  - `src\\Totem.Http.Server\\Mvc\\Controllers\\HttpCommandController.cs`
  - `src\\Totem.Http.Server\\Mvc\\Controllers\\HttpReportQueryController.cs`
  - `src\\Totem.Http.Server\\Mvc\\Controllers\\HttpReportListQueryController.cs`
  These show the request/response patterns (ETag, 304/404 behavior for report queries).
- No existing Copilot “agent/skill” scaffolding was found in-repo (no `.copilot/`, no obvious `agents/` or `skills/` folders).

## Decision: target file location/format
You chose: **a standalone prompt/skill markdown file under** `examples\\Totem Documentation`.

Planned file: `examples\\Totem Documentation\\totem-timeline-agent.md`

Notes:
- This keeps the simulator guidance close to the Totem documentation.
- No additional repo-wide Copilot configuration is required.

## Proposed agent behavior (spec in the agent prompt)
### 1) Input language (how the user “calls endpoints”)
Define a small, strict command grammar the user can type, e.g.:
- `GET QueryType id=... [headers...]`
- `POST CommandType id=... body={...} [headers...]`

Alternative: accept literal HTTP-ish lines:
- `GET /queries/{QueryType}/{id}`
- `POST /commands/{CommandType}` with JSON body

The agent prompt will tell the agent to:
- parse the request
- update state deterministically
- respond with an HTTP-like response (status + JSON + optional ETag)

### 2) State schema (single JSON blob)
Define and always maintain a top-level state object (rendered in a code block in responses), supporting **multiple routed instances by `Id`**:
- `state.timeline`: array of positions `{position, time, type, data, causePosition}`
- `state.topics[topicType][routeId]`: `{checkpointPosition, state}`
- `state.queries[queryType][routeId]`: `{checkpointPosition, state}`
- `state.workflows.queue`: queued commands (optional)
- `state.reports[reportType][routeId]`: `{checkpointPosition, row}` (optional)

### 3) Runtime rules to simulate (minimum viable)
Implement (in prompt/instructions) a deterministic algorithm:
1. **Append event(s)** to the timeline (new position numbers).
2. For each new position, run observers in the correct order:
   - Queries: `Given(Event)` only
   - Topics: `Given(Event)` then `When(Command)` logic when commands are executed
   - Workflows: `When(Event)` -> `ThenEnqueue(Command)`
   - Reports: `When(Event)` -> update row
3. **Executing a command**:
   - determine route id (from `id=` or from a `Route(...)` rule)
   - load topic instance state
   - run topic `When(Command)`; collect emitted `Then(...)` events
   - append those events (with cause = the command’s “position” or a synthetic position)
   - drain workflow queue if enabled (loop with a max-iterations safety cap)

### 4) What domain/types to support
Because the readme is conceptual and doesn’t fully enumerate query definitions, the agent will ship with **a built-in demo area** derived from the readme snippets:
- Events: `ImportStarted`, `ImportFinished`, `ImportAlreadyStarted`, `ImportRequested` (if needed)
- Command: `StartImport`
- Topic: `ImportProcess` (tracks `_importing`, emits started/already-started)
- Query: `ImportStatus` (tracks `Importing`, `Reason` via Given handlers)
- Workflow: `ImportFlow` (optional)

Also allow a “generic mode” where unknown types are accepted but treated as no-op unless the user defines rules.

### 5) ETag/version behavior (optional but aligned with docs)
- `GET` returns `ETag: W/\"{lastObservedPosition}\"` (or a simple integer string).
- If request includes `If-None-Match` matching current version, return `304 Not Modified`.

## Deliverables
1. A single Copilot agent prompt/skill file (location depends on your answer) containing:
   - purpose and constraints
   - the request grammar
   - the state schema
   - the runtime simulation rules
   - built-in demo type definitions and their Given/When behaviors
   - example sessions (GET/POST with expected state transitions)
2. (Optional) A short README snippet pointing to the skill file and how to use it.

## Workplan
- [ ] Decide target file format/location for the Copilot agent/skill prompt
- [ ] Extract and summarize required Totem runtime rules from `examples\\Totem Documentation\\readme.md`
- [ ] Define the simulation state JSON schema and versioning/ETag rules
- [ ] Define the “endpoint” command grammar (GET/POST)
- [ ] Implement demo domain (Import* example) rules in the prompt
- [ ] Write the agent/skill file content (prompt + usage examples)
- [ ] Quick self-test by running 2-3 scripted example interactions (manual walkthrough)
- [ ] Update this plan with any adjustments based on your chosen target format
