using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Outermind;
using Outermind.Queries;
using Outermind.SmartScanning;
using Quantum.Queries.Clients;
using Quantum.Web;
using Totem;
using Totem.Timeline.Client;
using Totem.Timeline.Mvc;

namespace Outermind.Controllers
{
  //[ApiController]
  //[Route("[controller]")]
  public class ScanController : Controller
  {
    public ICommandServer _commands;
    public IQueryServer _queries;
    public IQueryDb _queryDb;

    public ScanController(ICommandServer commands, IQueryServer queries, IQueryDb queryDb)
    {
      _commands = commands;
      _queries = queries;
      _queryDb = queryDb;
    }

    //[HttpGet(Name = "TestScan")]
    //public async Task<IActionResult> Test()
    //{
    //  return Ok("All good");
    //}

    //[HttpPut("action")]
    //public async Task<IActionResult> Start([FromBody] ScanTest command)
    //{
    //  var cmd = new StartScan(command.Roll, command.Worker, DateTime.UtcNow);
    //  return await _commands.Execute(cmd, When<ScanStarted>.ThenOk, When<ScanAlreadyInProgress>.ThenConflict);
    //}

    ////Finish Scan

    //[HttpPost("action")]
    //public async Task<IActionResult> Finish([FromBody] FinishScan command)
    //{
    //  return await _commands.Execute(command, When<ScanFinished>.ThenOk, When<ScanNotFound>.ThenBadRequest);
    //}

    //// Delete Scan
    //[HttpDelete("action")]
    //public async Task<IActionResult> Delete([FromBody] DeleteScan command)
    //{
    //  return await _commands.Execute(command, When<ScanDeleted>.ThenOk, When<NothingToDelete>.ThenBadRequest);
    //}

    //// Move Scan
    //[HttpPost("action")]
    //public async Task<IActionResult> Move([FromBody] MoveScan command)
    //{
    //  return await _commands.Execute(command, When<ScanMoved>.ThenOk, When<CannotMoveScan>.ThenConflict);
    //}

    //// Operator Comment
    //[HttpPost("action")]
    //public async Task<IActionResult> Comment([FromBody] OperatorComment command)
    //{
    //  return await _commands.Execute(command, When<CommentAdded>.ThenOk, When<CommentRefused>.ThenBadRequest);
    //}

    //[HttpPost("action")]
    //public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettings command)
    //{
    //  return await _commands.Execute(command, When<SettingsUpdated>.ThenOk, When<SettingsRefused>.ThenConflict);
    //}

    [HttpPost("/api/registry")]
    public Task<IActionResult> UpdateRegistry(
      [FromBody] List<SmartScanRecord> scans,
      [FromServices] ICommandServer commands) =>
      commands.Execute(
        new UpdateScanRegistry(scans),
        When<RegisterScans>.ThenOk);

    [HttpGet("/api/clients")]
    public Task<IActionResult> GetClients([FromServices] IQueryServer queries) =>
      queries.Get<ClientList>();

    [HttpGet("/api/clients/{id}")]
    public Task<IActionResult> GetClientPallets(string id, [FromServices] IQueryServer queries) =>
      queries.Get<ClientPalletList>(id);

    [HttpGet("/api/pallets/{id}")]
    public Task<IActionResult> GetPalletBoxes(string id, [FromServices] IQueryServer queries) =>
      queries.Get<PalletBoxList>(id);

    [HttpGet("/api/boxes/{id}")]
    public Task<IActionResult> GetBoxRolls(string id, [FromServices] IQueryServer queries) =>
      queries.Get<BoxRollList>(id);

    [HttpGet("/api/TimeOnTask/{id}")]
    public Task<IActionResult> UpdateTimeOnTask(string id, [FromServices] IQueryServer queries)
    {
      return queries.Get(typeof(TimeOnTaskQuery), id);
    }

    [HttpGet("/api/TimeOnTask/users/{userId}")]
    public Task<IActionResult> GetKnownTemporalUsers(string userId, [FromServices] IQueryServer queries) =>
      queries.Get<KnownTemporalUsersQuery>(userId);

    [HttpPost("/api/TimeOnTask")]
    public async Task<IActionResult> BatchTimeOnTask([FromBody] List<string> temporalUsers)
    {
      var distinctKeys = temporalUsers?.Distinct().ToList() ?? new List<string>();

      var tasks = distinctKeys.Select(async key =>
      {
        var query = await _queryDb.ReadQuery<TimeOnTaskQuery>(Id.From(key));
        return (key, query.OffTaskWindows);
      });

      var results = await Task.WhenAll(tasks);

      var response = results.ToDictionary(r => r.key, r => r.OffTaskWindows);

      return Ok(response);
    }
  }
}
