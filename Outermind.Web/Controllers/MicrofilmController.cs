using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Totem;
using Totem.Timeline;
using Totem.Timeline.Client;
using Totem.Timeline.Mvc;

namespace Outermind.Controllers
{
  [Route("api/microfilm")]
  [ApiController]
  public class MicrofilmController : ControllerBase
  {
    readonly ICommandServer _commands;
    readonly IQueryServer _queries;
    readonly IQueryDb _queryDb;

    public MicrofilmController(ICommandServer commands, IQueryServer queries, IQueryDb queryDb)
    {
      _commands = commands;
      _queries = queries;
      _queryDb = queryDb;
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
    public async Task<IActionResult> GetRegisteredClients(string serverId)
    {
      var id = Id.From(serverId);

      if(!await ServerExists(id))
      {
        return NotFound(MicrofilmTableApiErrors.UnknownServer(id));
      }

      return await _queries.Get<RegisteredClientsQuery>(id);
    }

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

    [HttpGet("client-profiles")]
    public Task<IActionResult> GetClientProfiles() =>
      _queries.Get<MicrofilmClientProfilesQuery>();

    [HttpPost("client-profiles")]
    public Task<IActionResult> CreateClientProfile([FromBody] SaveMicrofilmClientProfileRequest request)
    {
      if(request == null)
      {
        return Task.FromResult<IActionResult>(BadRequest(MicrofilmTableApiErrors.InvalidRequest("Profile request body is required.")));
      }

      return _commands.Execute(
        new CreateMicrofilmClientProfile(request.Name, request.Description, request.Columns),
        When<MicrofilmClientProfileCreated>.Then(e => Created($"/api/microfilm/client-profiles/{e.Profile.Id}", new { profile = e.Profile })),
        When<MicrofilmClientProfileNameRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidProfileName(e))),
        When<MicrofilmClientProfileNameDuplicated>.Then(e => Conflict(MicrofilmTableApiErrors.DuplicateProfileName(e))),
        When<MicrofilmClientProfileColumnsRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidProfileColumns(e))));
    }

    [HttpPut("client-profiles/{profileId}")]
    public Task<IActionResult> ReplaceClientProfile(string profileId, [FromBody] SaveMicrofilmClientProfileRequest request)
    {
      if(request == null)
      {
        return Task.FromResult<IActionResult>(BadRequest(MicrofilmTableApiErrors.InvalidRequest("Profile request body is required.")));
      }

      return _commands.Execute(
        new ReplaceMicrofilmClientProfile(profileId, request.Name, request.Description, request.Columns),
        When<MicrofilmClientProfileReplaced>.Then(e => Ok(new { profile = e.Profile })),
        When<MicrofilmClientProfileNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownProfile(e.ProfileId))),
        When<MicrofilmClientProfileNameRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidProfileName(e))),
        When<MicrofilmClientProfileNameDuplicated>.Then(e => Conflict(MicrofilmTableApiErrors.DuplicateProfileName(e))),
        When<MicrofilmClientProfileColumnsRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidProfileColumns(e))));
    }

    [HttpDelete("client-profiles/{profileId}")]
    public Task<IActionResult> DeleteClientProfile(string profileId) =>
      _commands.Execute(
        new DeleteMicrofilmClientProfile(profileId),
        When<MicrofilmClientProfileDeleted>.Then(e => NoContent()),
        When<MicrofilmClientProfileNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownProfile(e.ProfileId))));

    [HttpGet("columns/{clientId}")]
    public Task<IActionResult> GetColumns(string clientId) =>
      GetTableQuery<MicrofilmTableColumnsQuery>(clientId);

    [HttpPut("columns/{clientId}")]
    public Task<IActionResult> ReplaceColumns(string clientId, [FromBody] ReplaceMicrofilmTableColumnsRequest request)
    {
      if(request == null)
      {
        return Task.FromResult<IActionResult>(BadRequest(MicrofilmTableApiErrors.InvalidRequest("Column request body is required.")));
      }

      return _commands.Execute(
        new ReplaceMicrofilmTableColumns(Id.From(clientId), request.Columns),
        When<MicrofilmTableColumnsChanged>.Then(e => Ok(new { columns = e.Columns })),
        When<MicrofilmTableClientNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownClient(e.ClientId))),
        When<MicrofilmTableColumnSchemaRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidColumns(e))));
    }

    [HttpGet("rows/{clientId}")]
    public Task<IActionResult> GetRows(string clientId) =>
      GetTableQuery<MicrofilmRegularRowsQuery>(clientId);

    [HttpPost("rows/{clientId}")]
    public Task<IActionResult> CreateRegularRow(string clientId, [FromBody] CreateMicrofilmRegularRowRequest request) =>
      _commands.Execute(
        new CreateMicrofilmRegularRow(Id.From(clientId), request?.RowId, request?.Cells),
        When<MicrofilmRegularRowCreated>.Then(e => Created($"/api/microfilm/rows/{e.ClientId}/{e.Row.Id}", new { row = e.Row })),
        When<MicrofilmTableClientNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownClient(e.ClientId))),
        When<MicrofilmTableRowConflict>.Then(e => Conflict(MicrofilmTableApiErrors.RowConflict(e))),
        When<MicrofilmTableColumnNotRecognized>.Then(e => BadRequest(MicrofilmTableApiErrors.UnknownColumn(e.ClientId, e.ColumnId))),
        When<MicrofilmTableCellValueRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidCell(e))));

    [HttpPatch("rows/{clientId}/{rowId}")]
    public Task<IActionResult> UpdateRegularRowCell(string clientId, string rowId, [FromBody] UpdateMicrofilmTableCellRequest request)
    {
      if(request == null)
      {
        return Task.FromResult<IActionResult>(BadRequest(MicrofilmTableApiErrors.InvalidRequest("Cell update request body is required.")));
      }

      return _commands.Execute(
        new UpdateMicrofilmRegularRowCell(Id.From(clientId), rowId, request.ColumnId, request.Value),
        When<MicrofilmRegularRowCellUpdated>.Then(e => Ok(new { row = e.Row })),
        When<MicrofilmTableClientNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownClient(e.ClientId))),
        When<MicrofilmTableRowNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownRow(e.ClientId, e.RowId))),
        When<MicrofilmTableColumnNotRecognized>.Then(e => BadRequest(MicrofilmTableApiErrors.UnknownColumn(e.ClientId, e.ColumnId))),
        When<MicrofilmTableCellValueRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidCell(e))));
    }

    [HttpGet("custom-rows/{clientId}")]
    public Task<IActionResult> GetCustomRows(string clientId) =>
      GetTableQuery<MicrofilmCustomRowsQuery>(clientId);

    [HttpPost("custom-rows/{clientId}")]
    public Task<IActionResult> CreateCustomRow(string clientId, [FromBody] CreateMicrofilmCustomRowRequest request)
    {
      return _commands.Execute(
        new CreateMicrofilmCustomRow(Id.From(clientId), request?.Cells),
        When<MicrofilmCustomRowCreated>.Then(e => Created($"/api/microfilm/custom-rows/{e.ClientId}/{e.Row.Id}", new { row = e.Row })),
        When<MicrofilmTableClientNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownClient(e.ClientId))),
        When<MicrofilmTableColumnNotRecognized>.Then(e => BadRequest(MicrofilmTableApiErrors.UnknownColumn(e.ClientId, e.ColumnId))),
        When<MicrofilmTableCellValueRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidCell(e))));
    }

    [HttpPatch("custom-rows/{clientId}/{rowId}")]
    public Task<IActionResult> UpdateCustomRowCell(string clientId, string rowId, [FromBody] UpdateMicrofilmTableCellRequest request)
    {
      if(request == null)
      {
        return Task.FromResult<IActionResult>(BadRequest(MicrofilmTableApiErrors.InvalidRequest("Cell update request body is required.")));
      }

      return _commands.Execute(
        new UpdateMicrofilmCustomRowCell(Id.From(clientId), rowId, request.ColumnId, request.Value),
        When<MicrofilmCustomRowCellUpdated>.Then(e => Ok(new { row = e.Row })),
        When<MicrofilmTableClientNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownClient(e.ClientId))),
        When<MicrofilmTableRowNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownRow(e.ClientId, e.RowId))),
        When<MicrofilmTableColumnNotRecognized>.Then(e => BadRequest(MicrofilmTableApiErrors.UnknownColumn(e.ClientId, e.ColumnId))),
        When<MicrofilmTableCellValueRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidCell(e))));
    }

    async Task<IActionResult> GetTableQuery<TQuery>(string clientId) where TQuery : Query
    {
      var id = Id.From(clientId);

      if(!await ClientExists(id))
      {
        return NotFound(MicrofilmTableApiErrors.UnknownClient(id));
      }

      return await _queries.Get<TQuery>(id);
    }

    async Task<bool> ServerExists(Id serverId)
    {
      var lookup = await _queryDb.ReadQuery<MicrofilmClientLookupQuery>();

      return lookup.HasServer(serverId);
    }

    async Task<bool> ClientExists(Id clientId)
    {
      var lookup = await _queryDb.ReadQuery<MicrofilmClientLookupQuery>();

      return lookup.HasClient(clientId);
    }
  }
}
