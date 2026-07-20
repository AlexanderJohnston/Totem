# Design Specification: Roll-Scoped Microfilm Resources and Targeted Audit Migration

Last updated: 2026-06-30

Source inputs:
- `docs/product_backlog.md`
- `docs/vision.md`
- `docs/qa_plan.md`
- `docs/governance_traceability.md`
- `backend-integration-guide.md`
- `docs/execution_log.md`
- Code inspection of `Outermind.Web/Controllers/MicrofilmController.cs`, `Outermind.Web/Program.cs`, `Outermind/Microfilm/TableCommands.cs`, `Outermind/Microfilm/TableEvents.cs`, `Outermind/Microfilm/TableTypes.cs`, `Outermind/Microfilm/Topics/MicrofilmTableTopic.cs`, and current Microfilm queries

## Architecture Overview

### Product-aligned scope

- P1 `/api/session` tracking and P2 backend-owned cookie identity are **completed enabling infrastructure**. Preserve them as the server-owned actor source for P3. [Product → Design: P1-S1, P1-S2, P1-S3, P2-S1 through P2-S7]
- P3 moves Miller/Formatic table contracts away from client-wide buckets toward **roll-scoped resources** plus **targeted per-cell audit facts** for changed/editable cells first. [Product → Design: P3-S1 through P3-S5]
- This remains tracking, not authorization: no permission gates, QueryHub auth, command blocking, or role checks are added by this design. `identified`, `unmapped`, and `unidentified` remain tracking states. [Product → Design: PD-P3-007, PD-P3-009]

### Current-state summary from code

- The client and roll `/columns` endpoints are deleted. `MicrofilmController` exposes canonical roll rows/table reads, legacy client-wide row compatibility routes, and client profiles.
- Canonical roll and legacy client-wide writes are schema-independent: arbitrary trimmed, non-empty field IDs accept only scalar/null values. Roll rows guarantee `boxName` and `rollName`; legacy rows remain sparse.
- `MicrofilmRegularRowsQuery` and `MicrofilmCustomRowsQuery` retain client-wide compatibility projections; `RollMicrofilmRowsQuery`, `RollMicrofilmRowQuery`, and `RollMicrofilmTableQuery` expose durable canonical roll rows.
- Box and roll navigation already exists (`ClientBoxesQuery`, `BoxStatusQuery`, `RollStatusQuery`), so P3 can anchor new table resources on `rollId` without inventing a new top-level hierarchy.
- Existing legacy client-wide rows do **not** persist `rollId`. Automatic migration therefore requires an explicit row-to-roll assignment step; it cannot be inferred from the legacy table topic alone. [Design → Product] [Design → Execution]

### Target architecture

```text
box
  -> roll
        -> rows
             -> sparse cells keyed by field ID
                  -> optional audit metadata for tracked cells

Browser / frontend
  -> canonical roll-scoped APIs
  -> legacy client-wide compatibility APIs (temporary)
       -> MicrofilmController + compatibility adapter
            -> server-resolved InteractionSession actor stamp
            -> roll-scoped commands/events/projections
            -> legacy aggregate projections + QueryHub invalidation fan-out
```

### Canonical addressing

- Primary hierarchy: `box -> roll -> row -> cell/column definition`. [Product → Design: P3-S1]
- Canonical public row address for P3: **`rollId + rowId`**.
- Current code does not guarantee global row uniqueness, so P3 should **not** collapse to `rowId`-only addressing now.
- During coexistence, maintain a temporary lookup `{ clientId, rowId } -> { rollId, rowKind }` so legacy PATCH/GET routes can still resolve a row without forcing a flag-day cutover. [Product → Design: PD-P3-002, PD-P3-003]
- Future-proofing check: if global row uniqueness is later guaranteed, a `rowId` alias can be added, but the composite route should remain valid for backward compatibility.

### Sequence: canonical tracked cell edit

```text
Frontend
  -> PATCH /api/microfilm/rolls/{rollId}/rows/{rowId}/cells/{columnId}
MicrofilmController
  -> resolve InteractionSession from existing cookie/session infrastructure
  -> create UpdateRollRowCell(..., actorStamp)
Command/Topic
  -> validate roll, row, column, value
  -> emit RollRowCellChanged(rollId, rowId, columnId, value, actorStamp)
Queries
  -> update roll row/rows projections
  -> update optional audit projection
  -> invalidate roll-scoped and legacy client-scoped buckets
Frontend
  -> requery roll row/table as needed
```

### Sequence: legacy compatibility PATCH during migration

```text
Frontend
  -> PATCH /api/microfilm/rows/{clientId}/{rowId}
Compatibility adapter
  -> lookup { clientId, rowId } => { rollId, rowKind }
  -> dispatch the same roll-scoped UpdateRollRowCell command
Queries
  -> refresh legacy client-wide projection and roll-scoped projection
```

## Component Designs

### 1. Roll-scoped command/topic model

[Product → Design: P3-S1, P3-S4] [Design → Execution]

Introduce a new roll-scoped table topic or equivalent routed by `rollId`. Do **not** mutate the existing client-wide topic into the only source during coexistence; keep a clean migration seam.

Recommended command/event shapes:

```csharp
public sealed class UpdateRollRowCell : Command
{
  public string RollId { get; init; }
  public string RowId { get; init; }
  public string ColumnId { get; init; }
  public MicrofilmCellValue Value { get; init; }
  public AuditActorStamp Actor { get; init; }
}

public sealed class RollRowCellChanged : Event
{
  public string RollId { get; init; }
  public string RowId { get; init; }
  public string RowKind { get; init; }   // regular | custom
  public string ColumnId { get; init; }
  public MicrofilmCellValue Value { get; init; }
  public AuditActorStamp Actor { get; init; }
}
```

Notes:
- Per-cell edits should emit **one fact per changed cell**, not a whole-row snapshot event. [Product → Design: P3-S4]
- Row creation may still be a row-snapshot event (`CreateRollRow` / `RollRowCreated`) because creation commonly initializes many cells at once. P3 audit MVP is about targeted edit facts first, not N per-cell create facts.
- `changedAt` should come from Totem event metadata (`Event.When`) unless Execution finds a framework constraint that requires duplicating the timestamp into the event payload. [Design → Execution]

### 2. Server-resolved actor stamping

[Product → Design: P1-S3, P3-S4] [Design → QA]

Use the existing web-owned `InteractionSession` resolution path as the only actor source:
- Controller/application layer resolves the session from `HttpContext.User`.
- New roll-scoped write commands carry a **server-generated** actor stamp.
- The domain/topic must not read `HttpContext` directly.
- Client actor fields remain ignored/unsupported.

Recommended durable audit actor shape:

```csharp
public sealed class AuditActorStamp
{
  public string Status { get; init; }          // identified | unmapped | unidentified
  public string DisplayLabel { get; init; }    // non-empty
  public string ProcessUserId { get; init; }   // nullable
  public string TrackingSource { get; init; }  // backend-cookie | windows-integrated-auth | none
}
```

Design boundary:
- Persist `status`, `displayLabel`, `processUserId`, and `trackingSource`.
- Do **not** persist `windowsAccount` or `userPrincipalName` in row/cell audit facts by default; keep those as session diagnostics, not durable row-history payload. [Design → QA]
- `unmapped` and `unidentified` are valid audit actor states; they are recorded, not blocked. [Product → Design: PD-P3-007]

### 3. Optimistic fields and profile presentation

[Product → Design: P3-S3] [Design → Execution]

Rows store sparse `Dictionary<string, MicrofilmCellValue>` cells keyed by arbitrary trimmed, non-empty field IDs. Object/array values reject; text, finite numbers, booleans, and null are durable values.

Profiles independently own presentation definitions: membership, order, width, mappings, types, and dropdown options. They are not a row schema or write allow-list. Omitted profile fields remain omitted, and profile removal, re-addition, or type changes cannot mutate stored cells or audits.

The deleted `/api/microfilm/columns/{clientId}` and `/api/microfilm/rolls/{rollId}/columns` routes are not compatibility surfaces. Frontends join durable row reads to the selected profile for display.

### 4. Custom rows: recommended shape and remaining decision

[Product → Design: P3-S3] [Design → Product]

Two viable approaches:

| Approach | Pros | Cons | Design stance |
| --- | --- | --- | --- |
| Unified roll-row collection with `rowKind = regular | custom` | One audit model, one lookup model, one PATCH path, simpler invalidation. | UI may still want separate create/list affordances. | **Recommended domain/read-model shape.** |
| Separate sibling resources under roll (`/rows` and `/custom-rows`) | Easier mental mapping from current endpoints. | Duplicates projection and invalidation logic. | Acceptable as an API alias only if UI needs it. |

Recommended decision:
- Use one underlying roll-row model with `rowKind`.
- Keep separate legacy client-wide `/rows/{clientId}` and `/custom-rows/{clientId}` compatibility projections while frontend migration is in progress.
- Final product signoff is still needed on whether the new public roll API exposes one collection or sibling aliases. [Design → Product]

### 5. Query/read-model projections

[Product → Design: P3-S1, P3-S3, P3-S5] [Design → Execution]

Current projections:

```text
RollMicrofilmRowsQuery(rollId)
RollMicrofilmRowQuery(rollId:rowId)
RollMicrofilmTableQuery(rollId)     # convenience aggregate of durable roll rows
RollMicrofilmLookupQuery()          # rollId -> boxId, clientId
LegacyRowRoutingIndexQuery()        # clientId + rowId -> rollId, rowKind
MicrofilmRegularRowsQuery(clientId)
MicrofilmCustomRowsQuery(clientId)
```

Read-model rules:
- `RollMicrofilmRowsQuery` returns roll-scoped sparse rows with `id`, `rollId`, `origin`, cell values, and cell audits.
- `RollMicrofilmRowQuery` and `RollMicrofilmTableQuery` expose the same durable row/audit model; neither projects profile columns.
- `MicrofilmRegularRowsQuery` and `MicrofilmCustomRowsQuery` are temporary client-wide compatibility projections built from the roll-scoped model.
- Avoid recomputing legacy client-wide responses by scanning all rolls on every request; maintain aggregate projections keyed by `clientId`.

Recommended audit read shape:

```json
{
  "rollId": "ROLL-1",
  "rowId": "ROW-1",
  "rowKind": "regular",
  "cells": {
    "status": {
      "value": "Complete",
      "auditState": "tracked",
      "lastChangedAt": "2026-06-30T18:35:00Z",
      "lastChangedBy": {
        "status": "identified",
        "displayLabel": "Alex Johnston",
        "processUserId": "AJOHNSTON",
        "trackingSource": "backend-cookie"
      }
    },
    "notes": {
      "value": null,
      "auditState": "notTrackedYet"
    }
  }
}
```

Important distinction:
- `auditState = notTrackedYet` means no targeted audit fact exists yet.
- `lastChangedBy.status = unmapped | unidentified` means a tracked change exists, but the actor was not mapped/identified at edit time. [Product → Design: P3-S5]

### 6. QueryHub invalidation and bucket semantics

[Product → Design: PD-P3-008, P3-S5] [Design → Execution] [Design → QA]

Canonical roll queries are roll-scoped while legacy row projections remain client-wide by `clientId`.

Conceptual buckets:

```text
microfilm.rolls.{rollId}.rows
microfilm.rolls.{rollId}.row.{rowId}
microfilm.rolls.{rollId}.table
microfilm.clients.{clientId}.rows.legacy
microfilm.clients.{clientId}.custom-rows.legacy
```

Invalidation rules during coexistence:
- `RollRowCellChanged` invalidates:
  - roll row detail bucket
  - roll rows bucket
  - roll table bucket
  - matching legacy rows/custom-rows client bucket based on `rowKind`
- Migration/backfill invalidates both new roll buckets and legacy client buckets once per migrated batch.

If actual QueryHub bucket naming differs, Execution should map these semantics to the framework's real query-type/route-ID mechanism rather than invent a second invalidation system. [Design → Execution]

### 7. Migration and coexistence strategy

[Product → Design: P3-S2] [Design → Execution]

#### Phase A - add canonical roll-scoped APIs
- Add roll-scoped reads first.
- Keep current client-wide APIs in place.
- Keep only the legacy row routes needed during frontend migration; column endpoints are already removed.

#### Phase B - backfill roll-scoped projections
- Read current client-wide regular rows and custom rows.
- Use roll inventory (`ClientBoxesQuery` + `BoxStatusQuery`/`RollStatusQuery`) plus a migration manifest/index to assign each legacy row to a `rollId`.
- Seed roll-scoped rows without adding presentation-derived cells.
- Build `LegacyRowRoutingIndexQuery` so old `clientId + rowId` routes still resolve.
- Mark migrated cells as `auditState = notTrackedYet`; do **not** fabricate actor or timestamp history for untouched legacy cells. [Product → Design: PD-P3-006]

#### Phase C - adapt legacy writes
- Legacy PATCH routes resolve `rollId` from the routing index and dispatch roll-scoped commands.
- Legacy GET routes read aggregate compatibility projections sourced from the roll model.
- Legacy create routes are the hardest case because current request bodies do not identify a roll.

Required boundary for legacy create:
- Support one of these explicit rules; do **not** guess a roll when multiple rolls exist.
  1. Add optional `rollId` to legacy create requests during coexistence.
  2. Infer only when the client has exactly one eligible roll.
  3. Deprecate ambiguous legacy creates and require the canonical roll-scoped create endpoint.
- Recommended stance: allow `rollId` as an additive legacy-body hint and fail ambiguous creates deterministically when omitted. [Design → Product]

#### Phase D - sunset
- Remove legacy endpoints/buckets only after frontend consumers move to roll-scoped APIs and QueryHub subscriptions no longer depend on the legacy client-wide buckets.

## Data Models

### Roll-scoped read models

```csharp
public sealed class RollColumnDefinition
{
  public string ColumnId { get; init; }
  public string Name { get; init; }
  public string Type { get; init; }
  public double? Width { get; init; }
  public List<string> DropdownOptions { get; init; }
}

public sealed class RollCellReadModel
{
  public MicrofilmCellValue Value { get; init; }
  public string AuditState { get; init; }      // tracked | notTrackedYet
  public DateTimeOffset? LastChangedAt { get; init; }
  public AuditActorStamp LastChangedBy { get; init; }
}

public sealed class RollRowReadModel
{
  public string RollId { get; init; }
  public string RowId { get; init; }
  public string RowKind { get; init; }         // regular | custom
  public Dictionary<string, RollCellReadModel> Cells { get; init; }
}
```

### Compatibility lookup/index models

```csharp
public sealed class LegacyRowRoute
{
  public string ClientId { get; init; }
  public string RowId { get; init; }
  public string RollId { get; init; }
  public string RowKind { get; init; }
}
```

### Row uniqueness rule

- Public contract: `rollId + rowId`.
- Migration rule: keep `rowId` unique **within a client** while legacy routes remain live, even if the canonical contract is roll-scoped.
- After legacy sunset, Product may revisit whether `rowId` only needs roll-local uniqueness. [Design → Product]

## API Specifications

### Canonical roll-scoped APIs

[Product → Design: P3-S1 through P3-S5]

| Method | Route | Purpose | Notes |
| --- | --- | --- | --- |
| `GET` | `/api/microfilm/rolls/{rollId}/table` | Roll aggregate: roll ID and durable rows | Frontend joins rows to its selected profile; support `includeAudit=true` optionally. |
| `GET` | `/api/microfilm/rolls/{rollId}/rows` | List roll-scoped rows | Can omit full audit metadata by default. |
| `GET` | `/api/microfilm/rolls/{rollId}/rows/{rowId}` | Read one row | Composite addressing remains canonical. |
| `POST` | `/api/microfilm/rolls/{rollId}/rows` | Create a row in a roll | Recommended body includes `rowKind`, optional `rowId`, and `cells`. |
| `PATCH` | `/api/microfilm/rolls/{rollId}/rows/{rowId}/cells/{columnId}` | Targeted cell edit | One cell per command/fact. |

Recommended PATCH request:

```http
PATCH /api/microfilm/rolls/ROLL-1/rows/ROW-1/cells/status
Content-Type: application/json

{ "value": "Complete" }
```

Recommended `GET /api/microfilm/rolls/{rollId}/table?includeAudit=true` response envelope:

```json
{
  "rollId": "ROLL-1",
  "rows": [
    {
      "id": "ROW-1",
      "rollId": "ROLL-1",
      "origin": "regular",
      "cells": {
        "status": "Complete"
      },
      "cellAudits": {
        "status": {
          "state": "tracked",
          "lastChangedAt": "2026-06-30T18:35:00Z",
          "lastChangedBy": {
            "status": "identified",
            "displayLabel": "Alex Johnston",
            "processUserId": "AJOHNSTON",
            "trackingSource": "backend-cookie"
          }
        }
      }
    }
  ]
}
```

The response intentionally contains no profile or column-definition payload. The frontend applies its selected profile solely as presentation metadata over these durable sparse cells.

### Legacy compatibility APIs during migration

[Product → Design: P3-S2, P3-S3, P3-S5]

Existing routes remain temporarily supported:
- `GET /api/microfilm/rows/{clientId}`
- `GET /api/microfilm/custom-rows/{clientId}`
- `PATCH /api/microfilm/rows/{clientId}/{rowId}`
- `PATCH /api/microfilm/custom-rows/{clientId}/{rowId}`
- `POST /api/microfilm/rows/{clientId}` and `POST /api/microfilm/custom-rows/{clientId}` only while a deterministic target-roll rule exists

Compatibility rules:
- Reads are projection adapters over the roll-scoped model.
- PATCH writes resolve `rollId` through the legacy routing index and dispatch roll-scoped commands.
- Legacy create must not guess among multiple rolls.
- Legacy responses may keep current envelopes, but should start returning `rollId` on rows as additive metadata when possible to help frontend migration. [Design → Execution]

## Trade-off Decisions

### Decision 1: `rollId + rowId` vs global `rowId`

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| `rollId + rowId` canonical addressing | Matches real hierarchy; safe without a global uniqueness guarantee; works with future divergent roll data. | Slightly longer URLs and lookup keys. | **Choose.** |
| Global `rowId` only | Simpler URLs. | Current code does not guarantee it; legacy coexistence becomes riskier. | Defer unless backend later proves uniqueness. |

### Decision 2: unified row model vs sibling custom-row model

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| Unified roll-row model with `rowKind` | One command/audit/query path. | Requires UI distinction at a higher layer. | **Choose internally.** |
| Separate roll rows + custom rows everywhere | Mirrors current API. | Duplicates logic and invalidation. | Allow only as API alias if needed. |

### Decision 3: profile-owned presentation vs row schema

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| Profiles own presentation definitions | Preserves independent layouts, mappings, ordering, types, and dropdown options without constraining durable data. | Frontend must join durable rows to a selected profile. | **Choose.** |
| Row schema or shared column catalog controls writes | Centralized validation. | Rejects valid evolving fields and mutates interpretation of durable data. | Do not choose. |

### Decision 4: targeted cell facts vs whole-row update events

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| One fact per edited cell | Precise audit, smaller invalidation surface, fits P3 scope. | More event types/projection work. | **Choose for edits.** |
| Whole-row updated events | Reuses current pattern mentally. | Over-broad for audit, noisy payloads, harder to distinguish what changed. | Do not choose for edit history. |

### Decision 5: eager structural migration vs lazy migration on first touch

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| Eager structural backfill + lazy audit history | Deterministic coexistence, supports legacy lookups immediately, avoids fake audit history. | Requires a migration manifest/index. | **Choose.** |
| Lazy row migration only when touched | Lower up-front work. | Legacy reads/writes stay ambiguous; routing/index gaps persist longer. | Do not choose for MVP coexistence. |

### Decision 6: full session identity in audit vs minimal actor stamp

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| Minimal durable actor stamp (`status`, `displayLabel`, `processUserId`, `trackingSource`) | Enough for audit UX; lower PII retention. | Less raw diagnostic detail in history. | **Choose.** |
| Persist full session fields including Windows account/UPN | Easier diagnostics. | Higher PII retention and broader schema coupling. | Defer unless Product explicitly asks. |

## Non-functional Requirements

[Design → QA]

### Security and privacy
- Reuse server-resolved cookie/session identity only; never trust client actor fields.
- Do not convert tracking identity into authorization in this phase.
- Durable audit facts should avoid storing Windows account or UPN by default.
- Existing CSRF protections for cookie-backed unsafe requests remain relevant to the new roll-scoped write endpoints. [Design → Execution]

### Performance and scalability
- Roll-scoped queries should be keyed by `rollId`; avoid client-wide scans for normal reads.
- Compatibility projections should be maintained incrementally, not rebuilt across all rolls per request.
- Full per-cell audit metadata should be optional on list views to avoid large payloads for multi-row tables.

### Reliability and migration safety
- Migration must be idempotent by migration/batch ID.
- Ambiguous legacy creates must fail deterministically rather than guessing a roll.
- Backfilled cells must not fake historical actor/timestamp data.
- QueryHub invalidation must fan out to both new and legacy buckets until sunset.

### Compatibility
- No flag-day removal of current client-wide endpoints/buckets.
- Preserve current session contract and cookie-backed actor source unchanged.
- During coexistence, keep client-wide row lookup compatibility through an explicit routing index.

## Testing and QA Implications

[Product → Design: P3-S4, P3-S5] [Design → QA]

Required coverage when P3 execution begins:
- **Domain/topic tests**: roll/legacy row create and update with arbitrary scalar/null fields, object/array and empty-ID rejection, `rowKind`, actor stamp persistence, and no client actor override.
- **Projection tests**: `RollMicrofilmRowsQuery`, `RollMicrofilmRowQuery`, `RollMicrofilmTableQuery`, legacy row compatibility projections, routing index projection, `notTrackedYet` vs tracked audit state.
- **Controller/integration tests**: canonical roll routes, legacy compatibility PATCH routing, optional legacy create ambiguity handling, `GET /api/session` actor reuse, no `[Authorize]`/permission-gating regressions.
- **Migration tests**: idempotent backfill, missing/ambiguous row-to-roll assignment detection, no fabricated audit metadata on imported cells.
- **QueryHub tests/evidence**: roll change invalidates roll-scoped buckets and matching legacy client-wide buckets during coexistence.

Primary QA risks to carry forward:

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Legacy rows lack persisted `rollId` today | Wrong or incomplete migration | Require explicit row-to-roll manifest/index before switching writes. [Design → QA] |
| Profile type reinterpretation hides a durable field | UI can misrepresent retained data | Join rows to the selected profile without deleting/retyping stored cells or audits. [Design → Product] |
| Legacy create routes cannot choose a roll | Broken coexistence writes | Add optional `rollId` hint or fail ambiguous creates deterministically. [Design → QA] |
| QueryHub invalidation only updates new buckets | Stale legacy screens | Test fan-out to both roll and client compatibility buckets. [Design → QA] |
| P3 accidentally adds authorization behavior | Scope creep / broken workflows | Static scan and route smoke tests for no `[Authorize]`, no QueryHub auth, no unmapped/unidentified blocking. [Design → QA] |

## Project Structure & Hygiene

[Design → Product] [Design → Execution]

Repository scaffolding already exists. Recommended P3 additions only:

```text
Outermind/
  Microfilm/
    RollTableCommands.cs
    RollTableEvents.cs
    RollTableTypes.cs
    Topics/
      RollTableTopic.cs
    Queries/
      RollMicrofilmRowsQuery.cs
      RollMicrofilmRowQuery.cs
      RollMicrofilmTableQuery.cs
      RollMicrofilmLookupQuery.cs
      LegacyRowRoutingIndexQuery.cs
      MicrofilmRegularRowsQuery.cs
      MicrofilmCustomRowsQuery.cs

Outermind.Web/
  Controllers/
    MicrofilmController.cs         # add roll-scoped routes + compatibility adapters

tests/
  Quantum.Tests/
    Microfilm/
      RollTableTopicTests.cs
      RollTableProjectionTests.cs
      LegacyCompatibilityProjectionTests.cs
      MicrofilmControllerRollRoutesTests.cs
```

Hygiene notes:
- Keep `App_Data/` and other local-only auth artifacts ignored; P3 does not change that baseline.
- Do not add new planning docs; keep this design in `docs/design.md` only.
- Any migration manifest or operator-supplied mapping data should be environment/deployment data, not committed personal content. [Design → Execution]

## Change Log

| Date | Change |
| --- | --- |
| 2026-06-30 | Reframed `docs/design.md` around P3 roll-scoped resources and targeted audit migration. Preserved P1/P2 backend session and cookie identity as completed actor-source infrastructure; added roll hierarchy, row addressing, endpoint migration strategy, targeted cell-fact design, actor stamping, custom-row options, column-definition resources, read models, QueryHub invalidation semantics, backfill strategy, QA risks, and recommended project structure. |
