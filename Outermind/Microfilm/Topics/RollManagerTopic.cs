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
    Id _boxId;
    readonly HashSet<KnownRoll> _rolls = new();

    static Id RouteFirst (BoxCreated e) => e.Box.BoxId;
    static Id Route(CreateRoll e) => e.BoxId;
    static Id Route(RollCreated e) => e.Roll.BoxId;
    static Id Route(WaspBoxRollsIdentified e) => e.BoxId;
    static Id Route(WaspRollIdentified e) => e.BoxId;

    void Given(BoxCreated e) => _boxId = e.Box.BoxId;

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
        var roll = CreateRoll(command.ClientId, command.BoxId, command.RollName);
        Then(new RollCreated(roll));
      }
    }

    void When(WaspBoxRollsIdentified e)
    {
      var knownRollNames = new HashSet<string>(_rolls.Select(r => r.RollName), StringComparer.OrdinalIgnoreCase);

      foreach (var roll in e.Rolls)
      {
        if (!knownRollNames.Add(roll.RollName))
        {
          continue;
        }

        Then(new RollCreated(CreateRoll(e.ClientId, e.BoxId, roll.RollName)));
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
        var roll = CreateRoll(e.ClientId, e.BoxId, e.RollName);
        Then(new RollCreated(roll));
      }
    }

    static KnownRoll CreateRoll(Id clientId, Id boxId, string rollName) =>
      new(rollName, RollIds.From(clientId, boxId, rollName), boxId);
  }
}
