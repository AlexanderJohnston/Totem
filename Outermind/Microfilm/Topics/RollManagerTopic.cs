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

    static Id RouteFirst (BoxCreated e) => e.Box.BoxId;
    static Id Route(CreateRoll e) => e.BoxId;
    static Id Route(RollCreated e) => e.Roll.BoxId;
    static Id Route(WaspRollIdentified e) => e.BoxId;

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

    void When(WaspRollIdentified e)
    {
      if (_rolls.Any(r => string.Equals(r.RollName, e.RollName, StringComparison.OrdinalIgnoreCase)))
      {
        Then(new RollAlreadyExists(e.RollName, e.BoxId));
      }
      else
      {
        var roll = new KnownRoll(e.RollName, Id.FromGuid(), e.BoxId);
        Then(new RollCreated(roll));
      }
    }
  }
}
