using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Totem;
using Totem.Timeline.Mvc;

namespace Outermind.Controllers
{
  [Route("api/microfilm")]
  [ApiController]
  public class MicrofilmController : ControllerBase
  {
    readonly ICommandServer _commands;
    readonly IQueryServer _queries;

    public MicrofilmController(ICommandServer commands, IQueryServer queries)
    {
      _commands = commands;
      _queries = queries;
    }

    //
    // Commands
    //

    [HttpPost("servers")]
    public Task<IActionResult> NewServer([FromBody] NewServer command) =>
      _commands.Execute(command,
        When<ServerCreated>.ThenOk,
        When<ServerAlreadyExists>.ThenConflict);

    [HttpPost("clients")]
    public Task<IActionResult> NewClient([FromBody] NewClient command) =>
      _commands.Execute(command,
        When<ClientCreated>.ThenOk,
        When<ClientAlreadyExists>.ThenConflict,
        When<ServerNotRecognized>.ThenBadRequest);

    [HttpPut("clients/reassign")]
    public Task<IActionResult> ChangeClientAssignment([FromBody] ChangeClientAssignment command) =>
      _commands.Execute(command,
        When<ClientReassigned>.ThenOk,
        When<ClientNotRecognized>.ThenBadRequest,
        When<ServerNotRecognized>.ThenBadRequest,
        When<ClientAlreadyAssignedToServer>.ThenConflict);

    [HttpPost("operators")]
    public Task<IActionResult> CreateOperator([FromBody] CreateOperator command) =>
      _commands.Execute(command,
        When<OperatorCreated>.ThenOk,
        When<OperatorAlreadyExists>.ThenConflict);

    [HttpPost("boxes")]
    public Task<IActionResult> CreateBox([FromBody] CreateBox command) =>
      _commands.Execute(command,
        When<BoxCreated>.ThenOk,
        When<BoxAlreadyExists>.ThenConflict);

    [HttpPost("rolls")]
    public Task<IActionResult> CreateRoll([FromBody] CreateRoll command) =>
      _commands.Execute(command,
        When<RollCreated>.ThenOk,
        When<RollAlreadyExists>.ThenConflict);

    [HttpPost("operators/assign")]
    public Task<IActionResult> AssignOperator([FromBody] AssignOperator command) =>
      _commands.Execute(command,
        When<OperatorAssigned>.ThenOk,
        When<OperatorNotRecognized>.ThenBadRequest);

    [HttpPost("wasp/import")]
    public Task<IActionResult> SetWaspImportEnabled([FromBody] SetWaspImportEnabled command) =>
      _commands.Execute(command,
        When<WaspImportEnabledSet>.ThenOk,
        When<WaspImportAlreadyInRequestedState>.ThenConflict);

    [HttpPost("wasp/import/force")]
    public Task<IActionResult> ForceWaspImport([FromBody] ForceWaspImport command) =>
      _commands.Execute(command,
        When<ManualWaspImportEvent>.ThenOk);

    //
    // Queries
    //

    [HttpGet("servers")]
    public Task<IActionResult> GetServers() =>
      _queries.Get<ServerQuery>();

    [HttpGet("clients/{serverId}")]
    public Task<IActionResult> GetRegisteredClients(string serverId) =>
      _queries.Get<RegisteredClientsQuery>(serverId);

    [HttpGet("operators")]
    public Task<IActionResult> GetOperators() =>
      _queries.Get<OperatorList>();

    [HttpGet("operators/{operatorId}")]
    public Task<IActionResult> GetOperatorTasks(string operatorId) =>
      _queries.Get<OperatorTaskList>(operatorId);

    [HttpGet("boxes/{boxId}")]
    public Task<IActionResult> GetBoxStatus(string boxId) =>
      _queries.Get<BoxStatusQuery>(boxId);

    [HttpGet("boxes/by-client/{clientId}")]
    public Task<IActionResult> GetClientBoxes(string clientId) =>
      _queries.Get<ClientBoxesQuery>(clientId);

    [HttpGet("rolls/{rollId}")]
    public Task<IActionResult> GetRollStatus(string rollId) =>
      _queries.Get<RollStatusQuery>(rollId);

    [HttpGet("wasp/import/status")]
    public Task<IActionResult> GetWaspImportStatus() =>
      _queries.Get<WaspImportStatusQuery>();
  }
}
