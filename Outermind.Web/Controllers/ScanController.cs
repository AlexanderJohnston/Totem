using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Outermind;
using Outermind.Queries;
using Outermind.SmartScanning;
using Quantum.Web;
using Totem;
using Totem.Timeline.Mvc;

namespace Outermind.Controllers
{
  //[ApiController]
  //[Route("[controller]")]
  public class ScanController : Controller
  {
    public ICommandServer _commands;
    public IQueryServer _queries;

    public ScanController(ICommandServer commands, IQueryServer queries)
    {
      _commands = commands;
      _queries = queries;
    }

    [HttpGet(Name = "TestScan")]
    public async Task<IActionResult> Test()
    {
      return Ok("All good");
    }

    [HttpPut("action")]
    public async Task<IActionResult> Start([FromBody] ScanTest command)
    {
      var cmd = new StartScan(command.Roll, command.Worker, DateTime.UtcNow);
      return await _commands.Execute(cmd, When<ScanStarted>.ThenOk, When<ScanAlreadyInProgress>.ThenConflict);
    }

    //Finish Scan

    [HttpPost("action")]
    public async Task<IActionResult> Finish([FromBody] FinishScan command)
    {
      return await _commands.Execute(command, When<ScanFinished>.ThenOk, When<ScanNotFound>.ThenBadRequest);
    }

    // Delete Scan
    [HttpDelete("action")]
    public async Task<IActionResult> Delete([FromBody] DeleteScan command)
    {
      return await _commands.Execute(command, When<ScanDeleted>.ThenOk, When<NothingToDelete>.ThenBadRequest);
    }

    // Move Scan
    [HttpPost("action")]
    public async Task<IActionResult> Move([FromBody] MoveScan command)
    {
      return await _commands.Execute(command, When<ScanMoved>.ThenOk, When<CannotMoveScan>.ThenConflict);
    }

    // Operator Comment
    [HttpPost("action")]
    public async Task<IActionResult> Comment([FromBody] OperatorComment command)
    {
      return await _commands.Execute(command, When<CommentAdded>.ThenOk, When<CommentRefused>.ThenBadRequest);
    }

    [HttpPost("action")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettings command)
    {
      return await _commands.Execute(command, When<SettingsUpdated>.ThenOk, When<SettingsRefused>.ThenConflict);
    }

    //[HttpPost("/api/registry")]
    //public async Task<IActionResult> UpdateRegistry([FromBody] List<SmartScanRecord> scans)
    //{
    //  var command = new UpdateScanRegistry(scans);
    //  return await _commands.Execute(command, When<ScanListUpdated>.ThenOk);
    //}

    [HttpPost("/api/registry")]
    public Task<IActionResult> UpdateRegistry(
      [FromBody] List<SmartScanRecord> scans,
      [FromServices] ICommandServer commands) =>
      commands.Execute(
        new UpdateScanRegistry(scans),
        When<RegisterScans>.ThenOk);

    [HttpGet("/api/TimeOnTask/{id}")]
    public Task<IActionResult> UpdateTimeOnTask(string id, [FromServices] IQueryServer queries)
    {
      return queries.Get(typeof(TimeOnTaskQuery), id);
    }
  }
}
