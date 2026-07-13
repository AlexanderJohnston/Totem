using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks roll-to-client ownership for roll-scoped API validation and compatibility routing.
  /// </summary>
  public class RollMicrofilmLookupQuery : Query
  {
    public Dictionary<string, KnownBox> BoxesById { get; set; } = new();
    public Dictionary<string, RollMicrofilmLookup> RollsById { get; set; } = new();

    void Given(BoxCreated e)
    {
      BoxesById[e.Box.BoxId.ToString()] = e.Box;
    }

    void Given(RollCreated e)
    {
      if(BoxesById.TryGetValue(e.Roll.BoxId.ToString(), out var box))
      {
        RollsById[e.Roll.RollId.ToString()] = new RollMicrofilmLookup(e.Roll, box.ClientId, box.BoxId);
      }
    }

    public bool TryGetRoll(Id rollId, out RollMicrofilmLookup roll) =>
      RollsById.TryGetValue(rollId.ToString(), out roll);
  }

  public class RollMicrofilmLookup
  {
    public KnownRoll Roll { get; set; }
    public Id ClientId { get; set; }
    public Id BoxId { get; set; }

    public RollMicrofilmLookup()
    {
    }

    public RollMicrofilmLookup(KnownRoll roll, Id clientId, Id boxId)
    {
      Roll = roll;
      ClientId = clientId;
      BoxId = boxId;
    }
  }
}
