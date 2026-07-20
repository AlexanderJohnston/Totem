using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Totem;
using Totem.Timeline;
using Totem.Timeline.Client;
using Totem.Timeline.Mvc;
using Quantum.Web.IdentityTracking;

namespace Outermind.Controllers
{
  [Route("api/microfilm")]
  [ApiController]
  public class MicrofilmController : ControllerBase
  {
    readonly ICommandServer _commands;
    readonly IQueryServer _queries;
    readonly IQueryDb _queryDb;
    readonly IInteractionIdentityResolver _identity;

    public MicrofilmController(
      ICommandServer commands,
      IQueryServer queries,
      IQueryDb queryDb,
      IInteractionIdentityResolver identity)
    {
      _commands = commands;
      _queries = queries;
      _queryDb = queryDb;
      _identity = identity;
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

    [HttpGet("clients/{clientId}/profile-selection")]
    public async Task<IActionResult> GetClientProfileSelection(string clientId)
    {
      var client = Id.From(clientId);

      if(!await ClientExists(client))
      {
        return NotFound(MicrofilmTableApiErrors.UnknownClient(client));
      }

      var selection = await _queryDb.ReadQuery<MicrofilmClientProfileSelectionQuery>(client);
      return Ok(new { clientId = selection.ClientId, profileId = selection.ProfileId });
    }

    [HttpPut("clients/{clientId}/profile-selection")]
    public async Task<IActionResult> SetClientProfileSelection(string clientId, [FromBody] SetMicrofilmClientProfileSelectionRequest request)
    {
      if(request == null)
      {
        return BadRequest(MicrofilmTableApiErrors.InvalidRequest("Profile selection request body is required."));
      }

      var client = Id.From(clientId);

      if(!await ClientExists(client))
      {
        return NotFound(MicrofilmTableApiErrors.UnknownClient(client));
      }

      if(request.ProfileId != null && string.IsNullOrWhiteSpace(request.ProfileId))
      {
        return BadRequest(MicrofilmTableApiErrors.InvalidRequest("Profile ID must not be whitespace."));
      }

      var profileId = request.ProfileId?.Trim();

      if(profileId != null)
      {
        var profiles = await _queryDb.ReadQuery<MicrofilmClientProfilesQuery>();

        if(!profiles.Profiles.Exists(profile => profile.Id == profileId))
        {
          return NotFound(MicrofilmTableApiErrors.UnknownProfile(profileId));
        }
      }

      return await _commands.Execute(
        new SetMicrofilmClientProfileSelection(client, profileId),
        When<MicrofilmClientProfileSelectionChanged>.Then(e => Ok(new { clientId = e.ClientId, profileId = e.ProfileId })),
        When<MicrofilmTableClientNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownClient(e.ClientId))));
    }

    [HttpGet("rows/{clientId}")]
    public Task<IActionResult> GetRows(string clientId) =>
      GetTableQuery<MicrofilmRegularRowsQuery>(clientId);

    [HttpGet("rolls/{rollId}/rows")]
    public async Task<IActionResult> GetRollRows(string rollId)
    {
      var roll = Id.From(rollId);

      if(!await RollExists(roll))
      {
        return NotFound(MicrofilmTableApiErrors.UnknownRoll(roll));
      }

      return await _queries.Get<RollMicrofilmRowsQuery>(roll);
    }

    [HttpGet("rolls/{rollId}/rows/{rowId}")]
    public async Task<IActionResult> GetRollRow(string rollId, string rowId)
    {
      var roll = Id.From(rollId);

      if(!await RollExists(roll))
      {
        return NotFound(MicrofilmTableApiErrors.UnknownRoll(roll));
      }

      var query = await _queryDb.ReadQuery<RollMicrofilmRowQuery>(RollMicrofilmRowQuery.CreateId(roll, rowId));

      return query.Row == null
        ? NotFound(MicrofilmTableApiErrors.UnknownRow(Id.Unassigned, roll, rowId))
        : Ok(query);
    }

    [HttpGet("rolls/{rollId}/table")]
    public async Task<IActionResult> GetRollTable(string rollId)
    {
      var roll = Id.From(rollId);

      if(!await RollExists(roll))
      {
        return NotFound(MicrofilmTableApiErrors.UnknownRoll(roll));
      }

      var table = await _queryDb.ReadQuery<RollMicrofilmTableQuery>(roll);
      return Ok(table);
    }

    [HttpPost("rows/{clientId}")]
    public Task<IActionResult> CreateRegularRow(string clientId, [FromBody] CreateMicrofilmRegularRowRequest request)
    {
      if(!string.IsNullOrWhiteSpace(request?.RollId))
      {
        return CreateRollRow(request.RollId, MicrofilmTableRowOrigins.Regular, new CreateRollMicrofilmRowRequest
        {
          RowId = request.RowId,
          Cells = request.Cells
        }, Id.From(clientId));
      }

      return _commands.Execute(
        new CreateMicrofilmRegularRow(Id.From(clientId), request?.RowId, request?.Cells),
        When<MicrofilmRegularRowCreated>.Then(e => Created($"/api/microfilm/rows/{e.ClientId}/{e.Row.Id}", new { row = e.Row })),
        When<MicrofilmTableClientNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownClient(e.ClientId))),
        When<MicrofilmTableRowConflict>.Then(e => Conflict(MicrofilmTableApiErrors.RowConflict(e))),
        When<MicrofilmTableColumnNotRecognized>.Then(e => BadRequest(MicrofilmTableApiErrors.UnknownColumn(e.ClientId, e.ColumnId))),
        When<MicrofilmTableCellValueRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidCell(e))));
    }

    [HttpPost("rolls/{rollId}/rows")]
    public Task<IActionResult> CreateRollRegularRow(string rollId, [FromBody] CreateRollMicrofilmRowRequest request) =>
      CreateRollRow(rollId, MicrofilmTableRowOrigins.Regular, request);

    [HttpPatch("rows/{clientId}/{rowId}")]
    public Task<IActionResult> UpdateRegularRowCell(string clientId, string rowId, [FromBody] UpdateMicrofilmTableCellRequest request)
    {
      if(request == null)
      {
        return Task.FromResult<IActionResult>(BadRequest(MicrofilmTableApiErrors.InvalidRequest("Cell update request body is required.")));
      }

      return PatchLegacyOrClientRegularRow(clientId, rowId, request);
    }

    async Task<IActionResult> PatchLegacyOrClientRegularRow(string clientId, string rowId, UpdateMicrofilmTableCellRequest request)
    {
      var client = Id.From(clientId);

      var route = await GetLegacyRoute(client, rowId, MicrofilmTableRowOrigins.Regular);

      if(route != null)
      {
        return await ExecuteLegacyRollCellUpdate(route, request);
      }

      return await _commands.Execute(
        new UpdateMicrofilmRegularRowCell(Id.From(clientId), rowId, request.ColumnId, request.Value),
        When<MicrofilmRegularRowCellUpdated>.Then(e => Ok(new { row = e.Row })),
        When<MicrofilmTableClientNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownClient(e.ClientId))),
        When<MicrofilmTableRowNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownRow(e.ClientId, e.RowId))),
        When<MicrofilmTableColumnNotRecognized>.Then(e => BadRequest(MicrofilmTableApiErrors.UnknownColumn(e.ClientId, e.ColumnId))),
        When<MicrofilmTableCellValueRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidCell(e))));
    }

    [HttpPatch("rolls/{rollId}/rows/{rowId}/cells/{columnId}")]
    public Task<IActionResult> UpdateRollRegularRowCell(
      string rollId,
      string rowId,
      string columnId,
      [FromBody] UpdateMicrofilmTableCellRequest request) =>
      UpdateRollRowCell(rollId, rowId, MicrofilmTableRowOrigins.Regular, columnId, request);

    [HttpGet("custom-rows/{clientId}")]
    public Task<IActionResult> GetCustomRows(string clientId) =>
      GetTableQuery<MicrofilmCustomRowsQuery>(clientId);

    [HttpPost("custom-rows/{clientId}")]
    public Task<IActionResult> CreateCustomRow(string clientId, [FromBody] CreateMicrofilmCustomRowRequest request)
    {
      if(!string.IsNullOrWhiteSpace(request?.RollId))
      {
        return CreateRollRow(request.RollId, MicrofilmTableRowOrigins.Custom, new CreateRollMicrofilmRowRequest
        {
          Cells = request.Cells
        }, Id.From(clientId));
      }

      return _commands.Execute(
        new CreateMicrofilmCustomRow(Id.From(clientId), request?.Cells),
        When<MicrofilmCustomRowCreated>.Then(e => Created($"/api/microfilm/custom-rows/{e.ClientId}/{e.Row.Id}", new { row = e.Row })),
        When<MicrofilmTableClientNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownClient(e.ClientId))),
        When<MicrofilmTableColumnNotRecognized>.Then(e => BadRequest(MicrofilmTableApiErrors.UnknownColumn(e.ClientId, e.ColumnId))),
        When<MicrofilmTableCellValueRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidCell(e))));
    }

    [HttpPost("rolls/{rollId}/custom-rows")]
    public Task<IActionResult> CreateRollCustomRow(string rollId, [FromBody] CreateRollMicrofilmRowRequest request) =>
      CreateRollRow(rollId, MicrofilmTableRowOrigins.Custom, request);

    [HttpPatch("custom-rows/{clientId}/{rowId}")]
    public Task<IActionResult> UpdateCustomRowCell(string clientId, string rowId, [FromBody] UpdateMicrofilmTableCellRequest request)
    {
      if(request == null)
      {
        return Task.FromResult<IActionResult>(BadRequest(MicrofilmTableApiErrors.InvalidRequest("Cell update request body is required.")));
      }

      return PatchLegacyOrClientCustomRow(clientId, rowId, request);
    }

    async Task<IActionResult> PatchLegacyOrClientCustomRow(string clientId, string rowId, UpdateMicrofilmTableCellRequest request)
    {
      var client = Id.From(clientId);

      var route = await GetLegacyRoute(client, rowId, MicrofilmTableRowOrigins.Custom);

      if(route != null)
      {
        return await ExecuteLegacyRollCellUpdate(route, request);
      }

      return await _commands.Execute(
        new UpdateMicrofilmCustomRowCell(Id.From(clientId), rowId, request.ColumnId, request.Value),
        When<MicrofilmCustomRowCellUpdated>.Then(e => Ok(new { row = e.Row })),
        When<MicrofilmTableClientNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownClient(e.ClientId))),
        When<MicrofilmTableRowNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownRow(e.ClientId, e.RowId))),
        When<MicrofilmTableColumnNotRecognized>.Then(e => BadRequest(MicrofilmTableApiErrors.UnknownColumn(e.ClientId, e.ColumnId))),
        When<MicrofilmTableCellValueRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidCell(e))));
    }

    [HttpPatch("rolls/{rollId}/custom-rows/{rowId}/cells/{columnId}")]
    public Task<IActionResult> UpdateRollCustomRowCell(
      string rollId,
      string rowId,
      string columnId,
      [FromBody] UpdateMicrofilmTableCellRequest request) =>
      UpdateRollRowCell(rollId, rowId, MicrofilmTableRowOrigins.Custom, columnId, request);

    async Task<IActionResult> CreateRollRow(string rollId, string rowKind, CreateRollMicrofilmRowRequest request, Id? expectedClientId = null)
    {
      if(request == null)
      {
        return BadRequest(MicrofilmTableApiErrors.InvalidRequest("Row request body is required."));
      }

      var roll = Id.From(rollId);
      var lookup = await GetRollLookup(roll);

      if(lookup == null)
      {
        return NotFound(MicrofilmTableApiErrors.UnknownRoll(roll));
      }

      if(expectedClientId.HasValue && lookup.ClientId != expectedClientId.Value)
      {
        return BadRequest(MicrofilmTableApiErrors.InvalidRequest("Roll does not belong to the client route."));
      }

      return await _commands.Execute(
        new CreateRollMicrofilmRow(roll, lookup.ClientId, request.RowId, rowKind, request.Cells, ResolveAuditActor()),
        When<RollMicrofilmRowCreated>.Then(e => Created($"/api/microfilm/rolls/{e.RollId}/rows/{e.Row.Id}", new { row = e.Row })),
        When<RollMicrofilmTableRollNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownRoll(e.RollId))),
        When<RollMicrofilmTableRowKindRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidRowKind(e))),
        When<MicrofilmTableRowConflict>.Then(e => Conflict(MicrofilmTableApiErrors.RowConflict(e))),
        When<MicrofilmTableColumnNotRecognized>.Then(e => BadRequest(MicrofilmTableApiErrors.UnknownColumn(e.ClientId, roll, e.ColumnId))),
        When<MicrofilmTableCellValueRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidCell(e))));
    }

    async Task<IActionResult> UpdateRollRowCell(
      string rollId,
      string rowId,
      string rowKind,
      string columnId,
      UpdateMicrofilmTableCellRequest request)
    {
      if(request == null)
      {
        return BadRequest(MicrofilmTableApiErrors.InvalidRequest("Cell update request body is required."));
      }

      var roll = Id.From(rollId);
      var lookup = await GetRollLookup(roll);

      if(lookup == null)
      {
        return NotFound(MicrofilmTableApiErrors.UnknownRoll(roll));
      }

      return await _commands.Execute(
        new UpdateRollMicrofilmRowCell(roll, lookup.ClientId, rowId, rowKind, columnId, request.Value, ResolveAuditActor()),
        When<RollMicrofilmRowCellChanged>.Then(e => Ok(new
        {
          rollId = e.RollId,
          rowId = e.RowId,
          rowKind = e.RowKind,
          columnId = e.ColumnId,
          value = e.Value,
          audit = MicrofilmCellAudit.Tracked(e.When, e.Actor)
        })),
        When<RollMicrofilmTableRollNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownRoll(e.RollId))),
        When<RollMicrofilmTableRowKindRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidRowKind(e))),
        When<RollMicrofilmTableRowKindMismatch>.Then(e => BadRequest(MicrofilmTableApiErrors.RowKindMismatch(e))),
        When<MicrofilmTableRowNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownRow(e.ClientId, roll, e.RowId))),
        When<MicrofilmTableColumnNotRecognized>.Then(e => BadRequest(MicrofilmTableApiErrors.UnknownColumn(e.ClientId, roll, e.ColumnId))),
        When<MicrofilmTableCellValueRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidCell(e))));
    }

    async Task<LegacyRowRoute> GetLegacyRoute(Id clientId, string rowId, string rowKind)
    {
      var index = await _queryDb.ReadQuery<LegacyRowRoutingIndexQuery>();

      if(index.TryGetRoute(clientId, rowId, out var route) && route.RowKind == rowKind)
      {
        return route;
      }

      return null;
    }

    async Task<IActionResult> ExecuteLegacyRollCellUpdate(LegacyRowRoute route, UpdateMicrofilmTableCellRequest request)
    {
      return await _commands.Execute(
        new UpdateRollMicrofilmRowCell(route.RollId, route.ClientId, route.RowId, route.RowKind, request.ColumnId, request.Value, ResolveAuditActor()),
        When<RollMicrofilmRowCellChanged>.ThenAsync(CreateLegacyRollCellUpdateResponse),
        When<RollMicrofilmTableRollNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownRoll(e.RollId))),
        When<RollMicrofilmTableRowKindRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidRowKind(e))),
        When<RollMicrofilmTableRowKindMismatch>.Then(e => BadRequest(MicrofilmTableApiErrors.RowKindMismatch(e))),
        When<MicrofilmTableRowNotRecognized>.Then(e => NotFound(MicrofilmTableApiErrors.UnknownRow(e.ClientId, route.RollId, e.RowId))),
        When<MicrofilmTableColumnNotRecognized>.Then(e => BadRequest(MicrofilmTableApiErrors.UnknownColumn(e.ClientId, route.RollId, e.ColumnId))),
        When<MicrofilmTableCellValueRejected>.Then(e => BadRequest(MicrofilmTableApiErrors.InvalidCell(e))));
    }

    async Task<IActionResult> CreateLegacyRollCellUpdateResponse(RollMicrofilmRowCellChanged e)
    {
      var query = await _queryDb.ReadQuery<RollMicrofilmRowQuery>(RollMicrofilmRowQuery.CreateId(e.RollId, e.RowId));
      var row = query.Row?.Clone(e.RowKind) ?? new MicrofilmTableRow(e.RowId, e.RollId.ToString(), e.RowKind, new Dictionary<string, MicrofilmCellValue>());

      row.Cells[e.ColumnId] = e.Value?.Clone() ?? MicrofilmCellValue.Null();
      row.CellAudits[e.ColumnId] = MicrofilmCellAudit.Tracked(e.When, e.Actor);

      return Ok(new { row });
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

    async Task<bool> RollExists(Id rollId) =>
      await GetRollLookup(rollId) != null;

    async Task<RollMicrofilmLookup> GetRollLookup(Id rollId)
    {
      var lookup = await _queryDb.ReadQuery<RollMicrofilmLookupQuery>();

      return lookup.TryGetRoll(rollId, out var roll) ? roll : null;
    }

    MicrofilmAuditActorStamp ResolveAuditActor()
    {
      var session = _identity.Resolve(HttpContext.User);

      return new MicrofilmAuditActorStamp(
        session.Status,
        session.DisplayLabel,
        session.ProcessUserId,
        session.TrackingSource);
    }
  }
}
