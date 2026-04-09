using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Resolves imported WASP client batches from job number to internal client ID.
  /// </summary>
  public class WaspClientImportTopic : Topic
  {
    Id _clientId;

    static Id RouteFirst(ClientCreated e) => Id.From(e.Client.JobNumber);
    static Id RouteFirst(WaspClientAssetsImported e) => Id.From(e.JobNumber);
    static Id Route(ClientReassigned e) => Id.From(e.Client.JobNumber);
    static Id Route(WaspClientAssetsImported e) => Id.From(e.JobNumber);

    void Given(ClientCreated e)
    {
      _clientId = e.Client.ClientId;
    }

    void Given(ClientReassigned e)
    {
      _clientId = e.Client.ClientId;
    }

    void When(WaspClientAssetsImported e)
    {
      if (_clientId.IsAssigned)
      {
        Then(new WaspClientAssetsAccepted(_clientId, e.JobNumber, e.Boxes, e.Rolls));
        return;
      }

      var ignoredAssets = e.Boxes
        .Select(box => new IgnoredWaspLegacyAsset(
          box.AssetId,
          $"Job number '{e.JobNumber}' is not registered to a known client."))
        .Concat(e.Rolls.Select(roll => new IgnoredWaspLegacyAsset(
          roll.AssetId,
          $"Job number '{e.JobNumber}' is not registered to a known client.")))
        .ToList();

      if (ignoredAssets.Count > 0)
      {
        Then(new WaspLegacyAssetsIgnored(ignoredAssets));
      }

      Then(new WaspImportClientHandled(e.JobNumber));
    }
  }
}
