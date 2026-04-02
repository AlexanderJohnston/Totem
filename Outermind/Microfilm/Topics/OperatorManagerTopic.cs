using System;
using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Manages operator creation and assignment. Single instance.
  /// </summary>
  public class OperatorManagerTopic : Topic
  {
    readonly HashSet<KnownOperator> _operators = new();

    void Given(OperatorCreated e)
    {
      _operators.Add(e.Operator);
    }

    void When(CreateOperator command)
    {
      var operatorId = Id.FromGuid();
      var candidate = new KnownOperator(command.OperatorName, operatorId);

      if (_operators.Any(o => string.Equals(o.OperatorName, command.OperatorName, StringComparison.OrdinalIgnoreCase)))
      {
        var existing = _operators.First(o => string.Equals(o.OperatorName, command.OperatorName, StringComparison.OrdinalIgnoreCase));
        Then(new OperatorAlreadyExists(command.OperatorName, existing.OperatorId));
      }
      else
      {
        Then(new OperatorCreated(candidate));
      }
    }

    void When(AssignOperator command)
    {
      var match = _operators.FirstOrDefault(o => o.OperatorId == command.OperatorId);

      if (match == null)
      {
        Then(new OperatorNotRecognized(command.OperatorId.ToString()));
      }
      else
      {
        var rolls = new List<Id>(command.RollIds);
        Then(new OperatorAssigned(match, rolls, command.BoxId));
      }
    }
  }
}
