using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks an operator's assigned rolls and boxes. Multi-instance, routed by OperatorId.
  /// </summary>
  public class OperatorTaskList : Query
  {
    public KnownOperator Operator { get; set; }
    public HashSet<Id> AssignedRolls { get; set; } = new();
    public HashSet<Id> AssignedBoxes { get; set; } = new();

    static Id RouteFirst(OperatorCreated e) => e.Operator.OperatorId;
    static Id Route(OperatorAssigned e) => e.Operator.OperatorId;

    void Given(OperatorCreated e)
    {
      Operator = e.Operator;
    }

    void Given(OperatorAssigned e)
    {
      foreach (var rollId in e.Rolls)
      {
        AssignedRolls.Add(rollId);
      }
    }
  }
}
