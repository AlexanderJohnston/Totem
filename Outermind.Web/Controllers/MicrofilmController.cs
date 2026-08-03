using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

    [HttpGet("rolls/{rollId}/rows/{rowId}/operation-context")]
    [ProducesResponseType(typeof(OperationRowContext), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProcessingIssueEnvelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProcessingIssueEnvelope), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetOperationContext(string rollId, string rowId)
    {
      var roll = Id.From(rollId);
      var status = await _queryDb.ReadQuery<RollStatusQuery>(roll);

      if(status.Roll == null)
      {
        return NotFound(OperationContextApiErrors.RollNotFound(rollId, CorrelationId));
      }

      var rollLookup = await _queryDb.ReadQuery<RollMicrofilmLookupQuery>();
      var clientLookup = await _queryDb.ReadQuery<MicrofilmClientLookupQuery>();

      if(!rollLookup.TryGetRoll(roll, out var mapping)
        || !rollLookup.BoxesById.TryGetValue(mapping.BoxId.ToString(), out var box)
        || !clientLookup.TryGetClient(mapping.ClientId, out var client)
        || mapping.Roll.RollId != status.Roll.RollId
        || mapping.Roll.BoxId != status.Roll.BoxId
        || box.BoxId != status.Roll.BoxId
        || box.ClientId != mapping.ClientId
        || client.ClientId != mapping.ClientId)
      {
        return Conflict(OperationContextApiErrors.RollMappingInvalid(rollId, CorrelationId));
      }

      var rowQuery = await _queryDb.ReadQuery<RollMicrofilmRowQuery>(RollMicrofilmRowQuery.CreateId(roll, rowId));

      if(rowQuery.Row == null)
      {
        return NotFound(OperationContextApiErrors.RowNotFound(rollId, rowId, CorrelationId));
      }

      if(rowQuery.Row.RollId != status.Roll.RollId.ToString()
        || !MicrofilmTableRowOrigins.IsSupported(rowQuery.Row.Origin))
      {
        return Conflict(OperationContextApiErrors.RowContextInvalid(rollId, rowId, CorrelationId));
      }

      var operation = await _queryDb.ReadQuery<RollOperationQuery>(roll);

      if(operation.RollId != roll || operation.ResourceRevision < 1)
      {
        return Conflict(OperationContextApiErrors.RollMappingInvalid(rollId, CorrelationId));
      }

      return Ok(OperationRowContextFactory.Create(
        client,
        status.Roll,
        rowQuery.Row,
        operation.ScanState,
        operation.ActiveScanId,
        operation.ResourceVersion));
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

    [HttpPost("rolls/{rollId}/rows")]
    public Task<IActionResult> CreateRollRegularRow(string rollId, [FromBody] CreateRollMicrofilmRowRequest request) =>
      CreateRollRow(rollId, MicrofilmTableRowOrigins.Regular, request);

    [HttpPatch("rolls/{rollId}/rows/{rowId}/cells/{columnId}")]
    public Task<IActionResult> UpdateRollRegularRowCell(
      string rollId,
      string rowId,
      string columnId,
      [FromBody] UpdateMicrofilmTableCellRequest request) =>
      UpdateRollRowCell(rollId, rowId, MicrofilmTableRowOrigins.Regular, columnId, request);

    [HttpPost("rolls/{rollId}/custom-rows")]
    public Task<IActionResult> CreateRollCustomRow(string rollId, [FromBody] CreateRollMicrofilmRowRequest request) =>
      CreateRollRow(rollId, MicrofilmTableRowOrigins.Custom, request);

    [HttpPatch("rolls/{rollId}/custom-rows/{rowId}/cells/{columnId}")]
    public Task<IActionResult> UpdateRollCustomRowCell(
      string rollId,
      string rowId,
      string columnId,
      [FromBody] UpdateMicrofilmTableCellRequest request) =>
      UpdateRollRowCell(rollId, rowId, MicrofilmTableRowOrigins.Custom, columnId, request);

    async Task<IActionResult> CreateRollRow(string rollId, string rowKind, CreateRollMicrofilmRowRequest request)
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

    string CorrelationId => HttpContext?.TraceIdentifier;
  }
}
