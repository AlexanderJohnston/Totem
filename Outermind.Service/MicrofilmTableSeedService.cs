using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Outermind.Microfilm;
using Totem;
using Totem.Timeline.Client;

namespace Outermind.Service
{
  public class MicrofilmTableSeedService : IHostedService
  {
    readonly IClientDb _clientDb;
    readonly IConfiguration _configuration;
    readonly ILogger<MicrofilmTableSeedService> _logger;

    public MicrofilmTableSeedService(
      IClientDb clientDb,
      IConfiguration configuration,
      ILogger<MicrofilmTableSeedService> logger)
    {
      _clientDb = clientDb;
      _configuration = configuration;
      _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
      var options = new MicrofilmTableSeedOptions();
      _configuration.GetSection("Microfilm:TableSeeds").Bind(options);

      foreach(var seed in options.Clients ?? new List<MicrofilmTableSeedDefinition>())
      {
        cancellationToken.ThrowIfCancellationRequested();

        if(string.IsNullOrWhiteSpace(seed.ClientId))
        {
          _logger.LogWarning("Skipping microfilm table seed with no client ID.");
          continue;
        }

        var columns = ToColumns(seed.Columns);
        var rows = ToRows(seed.RegularRows, columns);
        var clientId = Id.From(seed.ClientId);
        var seedId = string.IsNullOrWhiteSpace(seed.SeedId) ? seed.ClientId : seed.SeedId;

        await _clientDb.WriteEvent(new SeedMicrofilmTable(clientId, seedId, columns, rows));

        _logger.LogInformation(
          "Requested microfilm table seed {SeedId} for client {ClientId}.",
          seedId,
          seed.ClientId);
      }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
      Task.CompletedTask;

    static List<MicrofilmTableColumn> ToColumns(List<MicrofilmTableSeedColumn> seedColumns) =>
      (seedColumns ?? new List<MicrofilmTableSeedColumn>())
        .Select(column => new MicrofilmTableColumn(
          column.Id,
          column.Name,
          column.Type,
          column.Width,
          column.DropdownOptions))
        .ToList();

    static List<MicrofilmTableRow> ToRows(List<MicrofilmTableSeedRow> seedRows, List<MicrofilmTableColumn> columns) =>
      (seedRows ?? new List<MicrofilmTableSeedRow>())
        .Select(row => new MicrofilmTableRow(
          row.Id,
          MicrofilmTableRowOrigins.Regular,
          ToCells(row.Cells, columns)))
        .ToList();

    static Dictionary<string, MicrofilmCellValue> ToCells(Dictionary<string, string> seedCells, List<MicrofilmTableColumn> columns)
    {
      var cells = new Dictionary<string, MicrofilmCellValue>();

      foreach(var cell in seedCells ?? new Dictionary<string, string>())
      {
        var column = columns.FirstOrDefault(candidate => candidate.Id == cell.Key);
        cells[cell.Key] = column == null
          ? MicrofilmCellValue.FromText(cell.Value)
          : MicrofilmTableRules.FromSeedText(column, cell.Value);
      }

      return cells;
    }
  }
}
