using System.Collections.Generic;

namespace Outermind.Service
{
  public class MicrofilmTableSeedOptions
  {
    public List<MicrofilmTableSeedDefinition> Clients { get; set; } = new();
  }

  public class MicrofilmTableSeedDefinition
  {
    public string ClientId { get; set; }
    public string SeedId { get; set; }
    public List<MicrofilmTableSeedColumn> Columns { get; set; } = new();
    public List<MicrofilmTableSeedRow> RegularRows { get; set; } = new();
  }

  public class MicrofilmTableSeedColumn
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public double? Width { get; set; }
    public List<string> DropdownOptions { get; set; } = new();
  }

  public class MicrofilmTableSeedRow
  {
    public string Id { get; set; }
    public Dictionary<string, string> Cells { get; set; } = new();
  }
}
