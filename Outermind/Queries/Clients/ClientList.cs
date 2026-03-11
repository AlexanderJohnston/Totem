using Outermind;
using System;
using System.Collections.Generic;
using System.Text;
using Totem;
using Totem.Timeline;

namespace Quantum.Queries.Clients
{
  public class ClientList : Query
  {
    public HashSet<string> Clients { get; set; } = new();

    void Given(NewRollDiscovered e)
    {
      Clients.Add(e.Client);
    }
  }
}
