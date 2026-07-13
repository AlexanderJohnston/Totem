using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Resolves legacy clientId + rowId routes to roll-scoped rows during migration.
  /// </summary>
  public class LegacyRowRoutingIndexQuery : Query
  {
    public Dictionary<string, LegacyRowRoute> RoutesByClientAndRow { get; set; } = new();

    void Given(RollMicrofilmRowCreated e)
    {
      RoutesByClientAndRow[CreateKey(e.ClientId, e.Row.Id)] =
        new LegacyRowRoute(e.ClientId, e.RollId, e.Row.Id, e.Row.Origin);
    }

    public bool TryGetRoute(Id clientId, string rowId, out LegacyRowRoute route) =>
      RoutesByClientAndRow.TryGetValue(CreateKey(clientId, rowId), out route);

    static string CreateKey(Id clientId, string rowId) =>
      $"{clientId}:{rowId ?? ""}";
  }

  public class LegacyRowRoute
  {
    public Id ClientId { get; set; }
    public Id RollId { get; set; }
    public string RowId { get; set; }
    public string RowKind { get; set; }

    public LegacyRowRoute()
    {
    }

    public LegacyRowRoute(Id clientId, Id rollId, string rowId, string rowKind)
    {
      ClientId = clientId;
      RollId = rollId;
      RowId = rowId;
      RowKind = rowKind;
    }
  }
}
