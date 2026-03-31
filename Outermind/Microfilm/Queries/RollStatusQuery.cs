using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks a roll and its assigned operator. Multi-instance, routed by RollId.
  /// </summary>
  public class RollStatusQuery : Query
  {
    public KnownRoll Roll { get; set; }
    public KnownOperator AssignedOperator { get; set; }

    static Id RouteFirst(RollCreated e) => e.Roll.RollId;
    static Many<Id> Route(OperatorAssigned e) => e.Rolls.Select(r => r).ToMany();

    void Given(RollCreated e)
    {
      Roll = e.Roll;
    }

    void Given(OperatorAssigned e)
    {
      AssignedOperator = e.Operator;
    }
  }
}
