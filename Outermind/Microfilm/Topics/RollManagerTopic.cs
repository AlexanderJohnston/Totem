using System;
using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Manages roll creation per box. One instance per BoxId.
  /// </summary>
  public class RollManagerTopic : Topic
  {
    readonly HashSet<KnownRoll> _rolls = new();

    static Id RouteFirst(CreateRoll e) => e.BoxId;
    static Id Route(RollCreated e) => e.Roll.BoxId;

    void Given(RollCreated e)
    {
      _rolls.Add(e.Roll);
    }

    void When(CreateRoll command)
    {
      if (_rolls.Any(r => string.Equals(r.RollName, command.RollName, StringComparison.OrdinalIgnoreCase)))
      {
        Then(new RollAlreadyExists(command.RollName, command.BoxId));
      }
      else
      {
        var roll = new KnownRoll(command.RollName, Id.FromGuid(), command.BoxId);
        Then(new RollCreated(roll));
      }
    }
  }
}
