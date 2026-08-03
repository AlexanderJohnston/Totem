# Design Specification: Canonical Roll-Scoped Microfilm Resources and Targeted Audit

Last updated: 2026-08-03

Current authority: `docs/SCAN_AND_PROCESSING_BACKEND_POLICY.md`. The temporary client-scoped coexistence design was retired when the canonical row boundary was implemented. Historical rollout evidence remains in the execution and governance logs, but it is not a supported API contract.

Source inputs:
- `docs/product_backlog.md`
- `docs/vision.md`
- `docs/qa_plan.md`
- `docs/governance_traceability.md`
- `backend-integration-guide.md`
- `docs/execution_log.md`
- Code inspection of `Outermind.Web/Controllers/MicrofilmController.cs`, `Outermind.Web/Program.cs`, `Outermind/Microfilm/RollTableCommands.cs`, `Outermind/Microfilm/RollTableEvents.cs`, `Outermind/Microfilm/TableTypes.cs`, `Outermind/Microfilm/Topics/RollMicrofilmTableTopic.cs`, and current Microfilm queries

## Architecture Overview

### Product-aligned scope

- P1 `/api/session` tracking and P2 backend-owned cookie identity are **completed enabling infrastructure**. Preserve them as the server-owned actor source for P3. [Product → Design: P1-S1, P1-S2, P1-S3, P2-S1 through P2-S7]
- P3 moves Miller/Formatic table contracts away from client-wide buckets toward **roll-scoped resources** plus **targeted per-cell audit facts** for changed/editable cells first. [Product → Design: P3-S1 through P3-S5]
- This remains tracking, not authorization: no permission gates, QueryHub auth, command blocking, or role checks are added by this design. `identified`, `unmapped`, and `unidentified` remain tracking states. [Product → Design: PD-P3-007, PD-P3-009]

### Current-state summary from code

- The client and roll `/columns` endpoints are deleted. `MicrofilmController` exposes canonical roll rows/table reads and client profiles.
- Roll-scoped writes are schema-independent: arbitrary trimmed, non-empty field IDs accept only scalar/null values. Roll rows guarantee `boxName` and `rollName` as presentation cells.
- `RollMicrofilmRowsQuery`, `RollMicrofilmRowQuery`, and `RollMicrofilmTableQuery` expose durable canonical roll rows. Client-wide row projections and routing indexes are removed.
- Box and roll navigation already exists (`ClientBoxesQuery`, `BoxStatusQuery`, `RollStatusQuery`), so P3 can anchor new table resources on `rollId` without inventing a new top-level hierarchy.
- Any deployment data still stored only in the retired client-scoped model must be explicitly converted to a known roll before Scan or Process is enabled. Conversion cannot infer identity from `boxName`, `rollName`, or other cells. [Design → Product] [Design → Execution]

### Target architecture

```text
box
  -> roll
        -> rows
             -> sparse cells keyed by field ID
                  -> optional audit metadata for tracked cells

Browser / frontend
  -> canonical roll-scoped APIs
       -> MicrofilmController
            -> server-resolved InteractionSession actor stamp
            -> roll-scoped commands/events/projections
```

### Canonical addressing

- Primary hierarchy: `box -> roll -> row -> cell/column definition`. [Product → Design: P3-S1]
- Canonical public row address for P3: **`rollId + rowId`**.
- Current code does not guarantee global row uniqueness, so P3 should **not** collapse to `rowId`-only addressing now.
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
  -> invalidate roll-scoped buckets
Frontend
  -> requery roll row/table as needed
```

## Component Designs

### 1. Roll-scoped command/topic model

[Product → Design: P3-S1, P3-S4] [Design → Execution]

Use the roll-scoped table topic routed by `rollId` as the only row write model.

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
- Keep regular and custom origins in one underlying roll-row model with identical ownership and write rules. Sibling create/patch routes are only typed HTTP affordances. [Design → Product]

### 5. Query/read-model projections

[Product → Design: P3-S1, P3-S3, P3-S5] [Design → Execution]

Current projections:

```text
RollMicrofilmRowsQuery(rollId)
RollMicrofilmRowQuery(rollId:rowId)
RollMicrofilmTableQuery(rollId)     # convenience aggregate of durable roll rows
RollMicrofilmLookupQuery()          # rollId -> boxId, clientId
```

Read-model rules:
- `RollMicrofilmRowsQuery` returns roll-scoped sparse rows with `id`, `rollId`, `origin`, cell values, and cell audits.
- `RollMicrofilmRowQuery` and `RollMicrofilmTableQuery` expose the same durable row/audit model; neither projects profile columns.

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

Canonical query instances are keyed only by roll or by the composite roll-row ID:

```text
microfilm.rolls.{rollId}.rows
microfilm.rolls.{rollId}.row.{rowId}
microfilm.rolls.{rollId}.table
```

A roll row fact invalidates its roll row detail, roll rows, and roll table query instances. QueryHub remains notification infrastructure; HTTP retrieval and ETags remain the state-retrieval/cache boundary and are not business concurrency versions.

### 7. Retired client-scoped data boundary

[Product → Design: P3-S2] [Design → Execution]

The backend no longer exposes client-scoped row reads or writes and no longer maintains a `clientId + rowId` routing index. Environments with client-only row events must complete an explicit, reviewed conversion before Scan or Process can be enabled:

1. Produce a manifest that assigns each retained row to one durable `rollId`.
2. Reject missing or ambiguous assignments; never match `boxName`, `rollName`, or another cell.
3. Append roll-scoped row facts without fabricating actor or timestamp history for untouched cells.
4. Verify regular/custom provenance, row counts, values, and audits against the manifest.
5. Enable consumers only after they use roll-scoped reads, writes, and QueryHub query instances.

This repository intentionally contains no cell-matching migration bridge.
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

### Row uniqueness rule

- Public contract: `rollId + rowId`.
- `rowId` uniqueness is required only within its owning roll; callers must never address a row without its `rollId`.

## API Specifications

### Canonical roll-scoped APIs

[Product → Design: P3-S1 through P3-S5]

| Method | Route | Purpose | Notes |
| --- | --- | --- | --- |
| `GET` | `/api/microfilm/rolls/{rollId}/table` | Roll aggregate: roll ID and durable rows | Frontend joins rows to its selected profile; support `includeAudit=true` optionally. |
| `GET` | `/api/microfilm/rolls/{rollId}/rows` | List roll-scoped rows | Can omit full audit metadata by default. |
| `GET` | `/api/microfilm/rolls/{rollId}/rows/{rowId}` | Read one row | Composite addressing remains canonical. |
| `POST` | `/api/microfilm/rolls/{rollId}/rows` | Create a regular row in a roll | Body includes optional `rowId` and `cells`. |
| `POST` | `/api/microfilm/rolls/{rollId}/custom-rows` | Create a custom row in a roll | Body includes optional `rowId` and `cells`; ownership rules match regular rows. |
| `PATCH` | `/api/microfilm/rolls/{rollId}/rows/{rowId}/cells/{columnId}` | Targeted regular-row cell edit | One cell per command/fact. |
| `PATCH` | `/api/microfilm/rolls/{rollId}/custom-rows/{rowId}/cells/{columnId}` | Targeted custom-row cell edit | One cell per command/fact. |

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

## Trade-off Decisions

### Decision 1: `rollId + rowId` vs global `rowId`

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| `rollId + rowId` canonical addressing | Matches real hierarchy; safe without a global uniqueness guarantee; works with future divergent roll data. | Slightly longer URLs and lookup keys. | **Choose.** |
| Global `rowId` only | Simpler URLs. | Current code does not guarantee it and it loses roll ownership context. | Do not choose. |

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

### Decision 5: explicit conversion vs runtime compatibility

| Approach | Pros | Cons | Decision |
| --- | --- | --- | --- |
| Explicit pre-enable conversion manifest | Deterministic ownership and validation; avoids fake audit history. | Requires deployment-specific data work. | **Choose when old data remains.** |
| Runtime client route, routing index, or cell matching | Avoids an up-front conversion gate. | Preserves ambiguous authority and can retarget rows incorrectly. | Do not choose. |

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
- Full per-cell audit metadata should be optional on list views to avoid large payloads for multi-row tables.

### Reliability and migration safety
- Any deployment-specific conversion must be idempotent by migration/batch ID.
- Missing or ambiguous row-to-roll assignments must fail rather than guess from names or cells.
- Converted cells must not fake historical actor/timestamp data.

### Compatibility
- Preserve current session contract and cookie-backed actor source unchanged.
- Do not restore client-scoped row endpoints, projections, routing indexes, or fallback dispatch.

## Testing and QA Implications

[Product → Design: P3-S4, P3-S5] [Design → QA]

Required coverage:
- **Domain/topic tests**: regular/custom parity, arbitrary scalar/null fields, object/array and empty-ID rejection, `rowKind`, actor stamp persistence, and no client actor override.
- **Projection tests**: `RollMicrofilmRowsQuery`, `RollMicrofilmRowQuery`, `RollMicrofilmTableQuery`, composite roll-row isolation, and `notTrackedYet` vs tracked audit state.
- **Controller/contract tests**: canonical roll routes are present and client-scoped row routes are absent.
- **Conversion tests when applicable**: idempotent import, missing/ambiguous row-to-roll assignment detection, no cell matching, and no fabricated audit metadata.
- **QueryHub tests/evidence**: roll change invalidates only the affected roll-scoped query instances.

Primary QA risks to carry forward:

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Deployment data lacks persisted `rollId` | Wrong or incomplete conversion | Require an explicit row-to-roll manifest before enabling Scan/Process. [Design → QA] |
| Profile type reinterpretation hides a durable field | UI can misrepresent retained data | Join rows to the selected profile without deleting/retyping stored cells or audits. [Design → Product] |
| A client attempts a removed client-scoped route | Integration failure | Coordinate frontend migration to canonical roll routes before deployment. [Design → QA] |
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

Outermind.Web/
  Controllers/
    MicrofilmController.cs         # canonical roll-scoped row routes

tests/
  Quantum.Tests/
    Microfilm/
      RollTableTopicTests.cs
      RollTableProjectionTests.cs
      MicrofilmControllerRollRoutesTests.cs
```

Hygiene notes:
- Keep `App_Data/` and other local-only auth artifacts ignored; P3 does not change that baseline.
- Do not add new planning docs; keep this design in `docs/design.md` only.
- Any conversion manifest or operator-supplied mapping data should be environment/deployment data, not committed personal content. [Design → Execution]

## Change Log

| Date | Change |
| --- | --- |
| 2026-08-03 | Retired the temporary client-scoped coexistence contract. The only supported row model and HTTP surface is roll-scoped; deployment data without roll ownership requires explicit pre-enable conversion. |
| 2026-06-30 | Reframed `docs/design.md` around P3 roll-scoped resources and targeted audit migration. Preserved P1/P2 backend session and cookie identity as completed actor-source infrastructure; added roll hierarchy, row addressing, endpoint migration strategy, targeted cell-fact design, actor stamping, custom-row options, column-definition resources, read models, QueryHub invalidation semantics, backfill strategy, QA risks, and recommended project structure. |
