# PRD: Roll Inventory Tracker for Digitization Warehouse

> **Target repo:** `C:\users\ajohnston\Desktop\Refactor\Totem` (Totem.sln)

## Problem Statement

We monitor a digitization warehouse where scanning machines process film for multiple clients. Each client has projects containing pallets of boxes, each holding rolls of film. As scanning activity is detected, we receive file paths like:

```
\\sbsr-film\Film\NARA202416724\1-Originals3\Pallet 10\04-ReadyforQP\Box 01\Roll_1
```

**Goal:** Build a running inventory of all unique rolls per box for each client, accumulated over time. The system must:

1. **Deduplicate rolls** — the same roll path detected multiple times should only appear once. Box 01 in Pallet 10 and Box 01 in Pallet 11 are **different physical boxes**.
2. **Discover new rolls** — when a never-before-seen roll appears for a pallet+box, it is added to the list.
3. **Group by pallet+box** — the final output is one list per pallet+box containing all unique rolls ever seen.

## Path Anatomy

```
\\{Server}\{Share}\{Client}\{Project}\{Pallet}\{Stage}\{Box}\{Roll}
\\sbsr-film\Film\NARA202416724\1-Originals3\Pallet 10\04-ReadyforQP\Box 01\Roll_1
  seg[0]   [1]     [2]           [3]         [4]        [5]          [6]    [7]
```

## Identity & Routing Key

- **Box identity key:** `{Client}:{Pallet}:{Box}` (e.g., `NARA202416724:Pallet 10:Box 01`)
  - Pallet IS part of the identity — Box 01 in Pallet 10 is a different physical box than Box 01 in Pallet 11
  - This is the Totem **route key** for both the Topic and Query
- **Roll uniqueness key:** the Roll name within a pallet+box route (e.g., `Roll_1`)

## Example: Expected Behavior

Given 8 detections:

| Detection | Pallet | Box | Roll | Outcome |
|-----------|--------|------|------|---------|
| 1 | 10 | Box 01 | Roll_1 | New → creates `Pallet 10:Box 01` instance, adds Roll_1 |
| 2 | 10 | Box 01 | Roll_2 | New → adds Roll_2 |
| 3 | 10 | Box 01 | Roll_3 | New → adds Roll_3 |
| 4 | 11 | Box 10 | Roll_1 | New → creates `Pallet 11:Box 10` instance, adds Roll_1 |
| 5 | 11 | Box 10 | Roll_2 | New → adds Roll_2 |
| 6 | 10 | Box 02 | Roll_1 | New → creates `Pallet 10:Box 02` instance, adds Roll_1 |
| 7 | 10 | Box 01 | Roll_1 | **Duplicate** → Roll_1 already known for Pallet 10:Box 01 |
| 8 | 10 | Box 01 | Roll_4 | New → adds Roll_4 to Pallet 10:Box 01 |

**Final inventory (3 box instances):**

- `NARA202416724:Pallet 10:Box 01` → [Roll_1, Roll_2, Roll_3, Roll_4] (4 unique, 1 deduped)
- `NARA202416724:Pallet 11:Box 10` → [Roll_1, Roll_2] (2 unique)
- `NARA202416724:Pallet 10:Box 02` → [Roll_1] (1 unique)

---

## Proposed Totem Architecture

### Existing Pipeline (in this repo)

The refactored repo has a shorter pipeline than the original. The classification topics
(FolderClassifier, ShareClassifier, ProjectClassifier, ProjectManager, SupervisorForNARA)
do **not** exist here. The pipeline ends at `ScanDetected`:

```
UpdateScanRegistry (Command)
        ↓
  SmartScanner (Topic, singleton)       ← Filters/cleans scan records
        ↓
  ScanDetected (Event)                  ← Carries SmartScanRecord with FolderPath
        ↓
  ShiftScheduleTopic, RollActivityQuery, etc.
```

`ScanForNARA` is defined in `_Events.cs` but never emitted. Our new `ClientClassifier`
will fill that gap.

### New Components (this feature)

```
ScanDetected (from SmartScanner)
        ↓
  ClientClassifier (Topic, singleton)   ← Extracts client from path seg[2], routes to client-specific events
        ↓
  ScanForNARA (Event, already defined)  ← Emitted if client matches NARA
        ↓
  NaraPathParser (Topic, singleton)     ← Parses NARA path structure into RollPathDetected
        ↓
  RollPathDetected (Event)              ← Carries Client, Pallet, Box, Roll, FullPath, etc.
        ↓                    ↓
  BoxInventory (Topic)   BoxRollList (Query)
  routed by {Client}:{Pallet}:{Box}  routed by {Client}:{Pallet}:{Box}
        ↓
  NewRollDiscovered / DuplicateRollIgnored (Events)
        ↓              ↓              ↓
  BoxRollList    ClientPalletList  PalletBoxList
  (Query)        (Query)           (Query)
  {Client}:      {Client}          {Client}:{Pallet}
  {Pallet}:{Box}
```

### Frontend Drill-Down Flow

```
1. GET /api/inventory/pallets/{client}      → ClientPalletList  → ["Pallet 10", "Pallet 11"]
2. GET /api/inventory/boxes/{client:pallet}  → PalletBoxList     → ["Box 01", "Box 02"]
3. GET /api/inventory/rolls/{client:pallet:box} → BoxRollList    → [Roll_1, Roll_2, ...]
```

---

## Detailed Design

### 1. New Events

```csharp
using Totem;
using Totem.Timeline;

namespace Outermind
{
  /// <summary>
  /// Emitted by RollPathParser when a scan path is parsed into structured components.
  /// </summary>
  public class RollPathDetected : Event
  {
    public string FullPath;
    public string Client;
    public string Project;
    public string Pallet;
    public string Stage;
    public string Box;
    public string Roll;

    public RollPathDetected(string fullPath, string client, string project,
      string pallet, string stage, string box, string roll)
    {
      FullPath = fullPath;
      Client = client;
      Project = project;
      Pallet = pallet;
      Stage = stage;
      Box = box;
      Roll = roll;
    }
  }

  /// <summary>
  /// Emitted by BoxInventory when a truly new roll is discovered for a pallet+box.
  /// </summary>
  public class NewRollDiscovered : Event
  {
    public string Client;
    public string Pallet;
    public string Box;
    public string Roll;
    public string FullPath;

    public NewRollDiscovered(string client, string pallet, string box, string roll, string fullPath)
    {
      Client = client;
      Pallet = pallet;
      Box = box;
      Roll = roll;
      FullPath = fullPath;
    }
  }

  /// <summary>
  /// Emitted by BoxInventory when a roll was already known (deduplicated).
  /// </summary>
  public class DuplicateRollIgnored : Event
  {
    public string Client;
    public string Pallet;
    public string Box;
    public string Roll;
    public string DetectedPath;

    public DuplicateRollIgnored(string client, string pallet, string box, string roll, string detectedPath)
    {
      Client = client;
      Pallet = pallet;
      Box = box;
      Roll = roll;
      DetectedPath = detectedPath;
    }
  }
}
```

### 2. ClientClassifier Topic (singleton)

Sits after `ScanDetected` and classifies scans by client. Extracts the client name from
the path (segment[2]) and emits client-specific events. This is the extension point for
adding new clients in the future — each gets their own event and parser.

```csharp
using System;
using Totem;
using Totem.Timeline;

namespace Outermind.Topics
{
  /// <summary>
  /// Classifies ScanDetected events by client and emits client-specific events.
  /// New clients are added here as additional cases.
  /// </summary>
  public class ClientClassifier : Topic
  {
    void When(ScanDetected e)
    {
      var path = e.Scan?.FolderPath;
      if (string.IsNullOrWhiteSpace(path))
        return;

      var segments = path.Split(
        new[] { '\\', '/' },
        StringSplitOptions.RemoveEmptyEntries);

      // Need at least 3 segments to extract client: server, share, client
      if (segments.Length < 3)
        return;

      var client = segments[2];

      if (client.StartsWith("NARA", StringComparison.OrdinalIgnoreCase))
      {
        var owner = e.UserId;
        var changeType = e.Scan?.ChangeTag ?? string.Empty;
        Then(new ScanForNARA(path, owner, changeType));
      }
    }
  }
}
```

### 3. NaraPathParser Topic (singleton)

NARA-specific path parser. Subscribes to `ScanForNARA` and parses the NARA path structure
into `RollPathDetected`. Other clients would get their own parser topics.

```csharp
using System;
using Totem.Timeline;

namespace Outermind.Topics
{
  /// <summary>
  /// Parses ScanForNARA paths into structured RollPathDetected events.
  /// Expects: \\server\share\{Client}\{Project}\{Pallet}\{Stage}\{Box}\{Roll}
  /// </summary>
  public class NaraPathParser : Topic
  {
    void When(ScanForNARA e)
    {
      if (string.IsNullOrWhiteSpace(e.FolderPath))
        return;

      var segments = e.FolderPath.Split(
        new[] { '\\', '/' },
        StringSplitOptions.RemoveEmptyEntries);

      // Need at least 8 segments: server, share, client, project, pallet, stage, box, roll
      if (segments.Length < 8)
        return;

      var client  = segments[2];  // e.g., "NARA202416724"
      var project = segments[3];  // e.g., "1-Originals3"
      var pallet  = segments[4];  // e.g., "Pallet 10"
      var stage   = segments[5];  // e.g., "04-ReadyforQP"
      var box     = segments[6];  // e.g., "Box 01"
      var roll    = segments[7];  // e.g., "Roll_1"

      Then(new RollPathDetected(e.FolderPath, client, project, pallet, stage, box, roll));
    }
  }
}
```

### 4. BoxInventory Topic (routed by Client:Pallet:Box)

The decision-maker: determines if a roll is new or a duplicate for a given pallet+box.

```csharp
using System;
using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Topics
{
  /// <summary>
  /// Tracks unique rolls per pallet+box. Routed by "{Client}:{Pallet}:{Box}".
  /// Deduplicates rolls detected multiple times at the same location.
  /// </summary>
  public class BoxInventory : Topic
  {
    readonly HashSet<string> _knownRolls = new(StringComparer.OrdinalIgnoreCase);

    // Route key: "{Client}:{Pallet}:{Box}" — creates new instance on first sight
    static Id RouteFirst(RollPathDetected e) =>
      Id.From($"{e.Client}:{e.Pallet}:{e.Box}");

    void Given(NewRollDiscovered e) =>
      _knownRolls.Add(e.Roll);

    void When(RollPathDetected e)
    {
      if (_knownRolls.Contains(e.Roll))
      {
        Then(new DuplicateRollIgnored(e.Client, e.Pallet, e.Box, e.Roll, e.FullPath));
      }
      else
      {
        Then(new NewRollDiscovered(e.Client, e.Pallet, e.Box, e.Roll, e.FullPath));
      }
    }
  }
}
```

### 5. BoxRollList Query (routed by Client:Pallet:Box)

The read model: maintains the current list of unique rolls per pallet+box for API/UI consumption.

```csharp
using System;
using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Queries
{
  /// <summary>
  /// Read model: the definitive list of unique rolls for a pallet+box.
  /// Routed by "{Client}:{Pallet}:{Box}".
  /// </summary>
  public class BoxRollList : Query
  {
    public string Client { get; set; }
    public string Pallet { get; set; }
    public string Box { get; set; }
    public List<RollEntry> Rolls { get; set; } = new();

    static Id RouteFirst(NewRollDiscovered e) =>
      Id.From($"{e.Client}:{e.Pallet}:{e.Box}");

    static Id Route(DuplicateRollIgnored e) =>
      Id.From($"{e.Client}:{e.Pallet}:{e.Box}");

    void Given(NewRollDiscovered e)
    {
      Client = e.Client;
      Pallet = e.Pallet;
      Box = e.Box;
      Rolls.Add(new RollEntry
      {
        Roll = e.Roll,
        FullPath = e.FullPath,
        FirstSeenUtc = Clock.Now
      });
    }

    // Optionally track duplicate count
    void Given(DuplicateRollIgnored e)
    {
      // Could increment a counter per roll if desired
    }
  }

  public class RollEntry
  {
    public string Roll { get; set; }
    public string FullPath { get; set; }
    public DateTime FirstSeenUtc { get; set; }
  }
}
```

### 6. ClientPalletList Query (routed by Client)

Lightweight navigation query: lists all known pallets for a client.

```csharp
using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Queries
{
  /// <summary>
  /// Navigation query: lists all pallets discovered for a client.
  /// Routed by "{Client}". Stores only pallet names.
  /// </summary>
  public class ClientPalletList : Query
  {
    public HashSet<string> Pallets { get; set; } = new();

    static Id RouteFirst(NewRollDiscovered e) => Id.From(e.Client);

    void Given(NewRollDiscovered e)
    {
      Pallets.Add(e.Pallet);
    }
  }
}
```

### 7. PalletBoxList Query (routed by Client:Pallet)

Lightweight navigation query: lists all known boxes within a pallet.

```csharp
using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Queries
{
  /// <summary>
  /// Navigation query: lists all boxes discovered for a client+pallet.
  /// Routed by "{Client}:{Pallet}". Stores only box names.
  /// </summary>
  public class PalletBoxList : Query
  {
    public HashSet<string> Boxes { get; set; } = new();

    static Id RouteFirst(NewRollDiscovered e) =>
      Id.From($"{e.Client}:{e.Pallet}");

    void Given(NewRollDiscovered e)
    {
      Boxes.Add(e.Box);
    }
  }
}
```

### 8. Web API Endpoints

Added to the existing `Outermind.Web\Controllers\` alongside `ScanController.cs`.

```csharp
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Outermind.Queries;
using Totem;
using Totem.Timeline.Mvc;

namespace Outermind.Controllers
{
  public class InventoryController : Controller
  {
    // Step 1: List pallets for a client
    // GET /api/inventory/pallets/NARA202416724
    [HttpGet("/api/inventory/pallets/{client}")]
    public Task<IActionResult> GetPallets(
      string client,
      [FromServices] IQueryServer queries) =>
      queries.Get<ClientPalletList>(Id.From(client));

    // Step 2: List boxes in a pallet
    // GET /api/inventory/boxes/NARA202416724:Pallet 10
    [HttpGet("/api/inventory/boxes/{id}")]
    public Task<IActionResult> GetBoxes(
      string id,
      [FromServices] IQueryServer queries) =>
      queries.Get<PalletBoxList>(Id.From(id));

    // Step 3: List rolls in a box
    // GET /api/inventory/rolls/NARA202416724:Pallet 10:Box 01
    [HttpGet("/api/inventory/rolls/{id}")]
    public Task<IActionResult> GetBoxRolls(
      string id,
      [FromServices] IQueryServer queries) =>
      queries.Get<BoxRollList>(Id.From(id));
  }
}
```

---

## Data Flow Walkthrough

Using detection #8 (`Pallet 10\Box 01\Roll_4`) as an example:

```
1. ScanDetected (e.Scan.FolderPath = "\\sbsr-film\Film\NARA202416724\...\Box 01\Roll_4")
   ↓
2. ClientClassifier.When(ScanDetected)
   → seg[2] = "NARA202416724", starts with "NARA"
   → Then(ScanForNARA(folderPath, owner, changeType))
   ↓
3. NaraPathParser.When(ScanForNARA)
   → Parses e.FolderPath segments
   → Then(RollPathDetected(Client="NARA202416724", Pallet="Pallet 10", Box="Box 01", Roll="Roll_4", ...))
   ↓
4. BoxInventory["NARA202416724:Pallet 10:Box 01"].When(RollPathDetected)
   → "Roll_4" NOT in _knownRolls
   → Then(NewRollDiscovered(Client, "Pallet 10", "Box 01", "Roll_4", fullPath))
   ↓
5. BoxInventory["NARA202416724:Pallet 10:Box 01"].Given(NewRollDiscovered)
   → _knownRolls.Add("Roll_4")
   ↓
6. BoxRollList["NARA202416724:Pallet 10:Box 01"].Given(NewRollDiscovered)
   → Rolls.Add(RollEntry { Roll = "Roll_4", FullPath = "...\Pallet 10\...\Box 01\Roll_4" })
```

For detection #7 (`Pallet 10\Box 01\Roll_1` — duplicate):

```
4. BoxInventory["NARA202416724:Pallet 10:Box 01"].When(RollPathDetected)
   → "Roll_1" IS in _knownRolls
   → Then(DuplicateRollIgnored(...))   ← No new roll added
```

For a non-NARA scan (e.g. path contains "ACME" at seg[2]):

```
2. ClientClassifier.When(ScanDetected)
   → seg[2] = "ACME", does NOT start with "NARA"
   → No event emitted (silently ignored until an ACME parser is added)
```

---

## File Locations (Refactored Repo)

| File | Path |
|------|------|
| Events | `Outermind\_Events.cs` (ScanForNARA already exists; add RollPathDetected, NewRollDiscovered, DuplicateRollIgnored) |
| ClientClassifier | `Outermind\Topics\ClientClassifier.cs` |
| NaraPathParser | `Outermind\Topics\NaraPathParser.cs` |
| BoxInventory | `Outermind\Topics\BoxInventory.cs` |
| BoxRollList | `Outermind\Queries\BoxRollList.cs` |
| ClientPalletList | `Outermind\Queries\ClientPalletList.cs` |
| PalletBoxList | `Outermind\Queries\PalletBoxList.cs` |
| InventoryController | `Outermind.Web\Controllers\InventoryController.cs` |

## Tasks

1. **Define events** — Add `RollPathDetected`, `NewRollDiscovered`, `DuplicateRollIgnored` to `Outermind\_Events.cs` (`ScanForNARA` already exists)
2. **Create ClientClassifier topic** — Singleton topic that subscribes to `ScanDetected`, extracts client from `e.Scan.FolderPath` seg[2], emits `ScanForNARA` for NARA clients
3. **Create NaraPathParser topic** — Singleton topic that subscribes to `ScanForNARA`, parses NARA-specific path structure into `RollPathDetected`
4. **Create BoxInventory topic** — Routed topic that deduplicates rolls per pallet+box
5. **Create BoxRollList query** — Routed query that maintains the roll list per pallet+box
6. **Create ClientPalletList query** — Routed query by `{Client}` that accumulates pallet names from `NewRollDiscovered`
7. **Create PalletBoxList query** — Routed query by `{Client}:{Pallet}` that accumulates box names from `NewRollDiscovered`
8. **Add API endpoints** — Add `InventoryController` to `Outermind.Web\Controllers\` with drill-down routes
9. **Write tests** — Topic tests for ClientClassifier, NaraPathParser, BoxInventory; query tests for all three queries
