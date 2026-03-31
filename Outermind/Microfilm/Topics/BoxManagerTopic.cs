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

    static Id RouteFirst(CreateBox e) => e.ClientId;
    static Id Route(BoxCreated e) => e.Box.ClientId;

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
  }
}
