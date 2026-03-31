using System.Collections.Generic;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks all known operators. Single instance.
  /// </summary>
  public class OperatorList : Query
  {
    public HashSet<KnownOperator> Operators { get; set; } = new();

    void Given(OperatorCreated e)
    {
      Operators.Add(e.Operator);
    }
  }
}
