using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks client box and roll IDs plus roll table versions without retaining table rows.
  /// </summary>
  public class ClientRollIndexQuery : Query
  {
    public Id ClientId { get; set; }
    public HashSet<Id> BoxIds { get; set; } = new();
    public Dictionary<string, ClientRollIndexEntry> RollsById { get; set; } = new();

    static Id RouteFirst(ClientCreated e) => e.Client.ClientId;
    static Id RouteFirst(BoxCreated e) => e.Box.ClientId;
    static Id RouteFirst(WaspBoxRollsIdentified e) => e.ClientId;
    static Id RouteFirst(WaspRollIdentified e) => e.ClientId;
    static Id RouteFirst(RollCreated e) => e.ClientId;
    static Id RouteFirst(RollMicrofilmRowCreated e) => e.ClientId;
    static Id RouteFirst(RollMicrofilmRowCellChanged e) => e.ClientId;

    void Given(ClientCreated e)
    {
      ClientId = e.Client.ClientId;
    }

    void Given(BoxCreated e)
    {
      ClientId = e.Box.ClientId;
      BoxIds.Add(e.Box.BoxId);
    }

    void Given(WaspBoxRollsIdentified e)
    {
      ClientId = e.ClientId;
      BoxIds.Add(e.BoxId);

      foreach (var roll in e.Rolls)
      {
        UpsertRoll(RollIds.From(e.ClientId, e.BoxId, roll.RollName), e.BoxId);
      }
    }

    void Given(WaspRollIdentified e)
    {
      ClientId = e.ClientId;
      BoxIds.Add(e.BoxId);
      UpsertRoll(RollIds.From(e.ClientId, e.BoxId, e.RollName), e.BoxId);
    }

    void Given(RollCreated e)
    {
      ClientId = e.ClientId;
      UpsertRoll(e.Roll.RollId, e.Roll.BoxId);
    }

    void Given(RollMicrofilmRowCreated e)
    {
      ClientId = e.ClientId;
      AdvanceRoll(e.RollId);
    }

    void Given(RollMicrofilmRowCellChanged e)
    {
      ClientId = e.ClientId;
      AdvanceRoll(e.RollId);
    }

    void UpsertRoll(Id rollId, Id boxId)
    {
      var key = rollId.ToString();

      if (RollsById.TryGetValue(key, out var existing))
      {
        if (boxId.IsAssigned)
        {
          existing.BoxId = boxId;
          BoxIds.Add(boxId);
        }

        return;
      }

      RollsById[key] = new ClientRollIndexEntry(rollId, boxId);

      if (boxId.IsAssigned)
      {
        BoxIds.Add(boxId);
      }
    }

    void AdvanceRoll(Id rollId)
    {
      UpsertRoll(rollId, Id.Unassigned);
      RollsById[rollId.ToString()].TableVersion++;
    }
  }

  public class ClientRollIndexEntry
  {
    public Id RollId { get; set; }
    public Id BoxId { get; set; }
    public long TableVersion { get; set; }

    public ClientRollIndexEntry()
    {
    }

    public ClientRollIndexEntry(Id rollId, Id boxId)
    {
      RollId = rollId;
      BoxId = boxId;
    }
  }
}
