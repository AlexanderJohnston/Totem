using System;
using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Manages box creation per client. One instance per ClientId.
  /// </summary>
  public class BoxManagerTopic : Topic
  {
    readonly HashSet<KnownBox> _boxes = new();

    static Id RouteFirst(ClientCreated e) => e.Client.ClientId;
    static Id RouteFirst(WaspClientAssetsAccepted e) => e.ClientId;
    static Id Route(CreateBox e) => e.ClientId;
    static Id Route(BoxCreated e) => e.Box.ClientId;
    static Id Route(WaspBoxIdentified e) => e.ClientId;
    static Id Route(WaspClientAssetsAccepted e) => e.ClientId;

    void Given(ClientCreated e)
    {
    }

    void Given(BoxCreated e)
    {
      _boxes.Add(e.Box);
    }

    void When(CreateBox command)
    {
      if (_boxes.Any(b => string.Equals(b.BoxName, command.BoxName, StringComparison.OrdinalIgnoreCase)))
      {
        Then(new BoxAlreadyExists(command.BoxName, command.ClientId));
      }
      else
      {
        var box = new KnownBox(command.BoxName, Id.FromGuid(), command.ClientId);
        Then(new BoxCreated(box));
      }
    }

    void When(WaspBoxIdentified e)
    {
      if (_boxes.Any(b => string.Equals(b.BoxName, e.BoxName, StringComparison.OrdinalIgnoreCase)))
      {
        Then(new BoxAlreadyExists(e.BoxName, e.ClientId));
      }
      else
      {
        var box = new KnownBox(e.BoxName, Id.FromGuid(), e.ClientId);
        Then(new BoxCreated(box));
      }
    }

    void When(WaspClientAssetsAccepted e)
    {
      var boxIdsByName = _boxes.ToDictionary(b => b.BoxName, b => b.BoxId, StringComparer.OrdinalIgnoreCase);

      foreach (var box in e.Boxes)
      {
        if (boxIdsByName.ContainsKey(box.BoxName))
        {
          continue;
        }

        var createdBox = new KnownBox(box.BoxName, Id.FromGuid(), e.ClientId);
        boxIdsByName[createdBox.BoxName] = createdBox.BoxId;
        Then(new BoxCreated(createdBox));
      }

      foreach (var roll in e.Rolls)
      {
        if (!boxIdsByName.TryGetValue(roll.BoxName, out var boxId))
        {
          Then(new WaspClientImportFailed(
            e.JobNumber,
            $"Box '{roll.BoxName}' is not recognized for job number '{e.JobNumber}'."));
          Then(new WaspImportClientHandled(e.JobNumber));
          return;
        }

        Then(new WaspRollIdentified(roll.AssetId, e.JobNumber, roll.RollName, boxId, e.ClientId));
      }

      Then(new WaspImportClientHandled(e.JobNumber));
    }
  }
}
