using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks generic table columns for a microfilm client.
  /// </summary>
  public class MicrofilmTableColumnsQuery : Query
  {
    public List<MicrofilmTableColumn> Columns { get; set; } = new();

    static Id RouteFirst(ClientCreated e) => e.Client.ClientId;
    static Id Route(MicrofilmTableSeeded e) => e.ClientId;
    static Id Route(MicrofilmTableColumnsChanged e) => e.ClientId;

    void Given(ClientCreated e)
    {
    }

    void Given(MicrofilmTableSeeded e)
    {
      Columns = e.Columns.Select(column => column.Clone()).ToList();
    }

    void Given(MicrofilmTableColumnsChanged e)
    {
      Columns = e.Columns.Select(column => column.Clone()).ToList();
    }

  }
}
