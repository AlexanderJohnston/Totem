using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Quantum.Web.ScanProcessing;
using Totem;
using Totem.Timeline.Client;
using Totem.Timeline.Mvc;

namespace Quantum.Web.Controllers
{
  /// <summary>
  /// Developer-admin logical configuration contracts. These demo routes require a registered
  /// actor but do not resolve storage, accept paths, or provide operation authority.
  /// </summary>
  [ApiController]
  [Route("api/scan-processing/storage-bindings/clients/{clientId}")]
  public sealed class ScanProcessingStorageBindingsController : ControllerBase
  {
    readonly ICommandServer _commands;
    readonly IQueryDb _queryDb;
    readonly IRegisteredScanProcessingActorResolver _actors;

    public ScanProcessingStorageBindingsController(
      ICommandServer commands,
      IQueryDb queryDb,
      IRegisteredScanProcessingActorResolver actors)
    {
      _commands = commands;
      _queryDb = queryDb;
      _actors = actors;
    }

    [HttpGet]
    public async Task<IActionResult> Get(string clientId)
    {
      if(!TryResolveActor(out _))
      {
        return RegisteredActorRequired();
      }

      var client = await ResolveClient(clientId);

      if(client == null)
      {
        return NotFound(Issue(
          ScanProcessingStorageBindingIssueCodes.ClientNotFound,
          "The Version 1 client/workspace was not found.",
          "clientId"));
      }

      var query = await _queryDb.ReadQuery<ScanProcessingStorageBindingQuery>(client.ClientId);
      return Ok(StorageResponse(query.State));
    }

    [HttpPost("bindings")]
    public async Task<IActionResult> Create(string clientId, [FromBody] CreateScanProcessingStorageBindingRequest request)
    {
      if(!TryResolveActor(out var actor))
      {
        return RegisteredActorRequired();
      }

      if(request == null)
      {
        return BadRequest(Issue(
          ScanProcessingStorageBindingIssueCodes.InvalidRequest,
          "Storage binding request body is required.",
          "request"));
      }

      var client = await ResolveClient(clientId);

      if(client == null)
      {
        return NotFound(Issue(
          ScanProcessingStorageBindingIssueCodes.ClientNotFound,
          "The Version 1 client/workspace was not found.",
          "clientId"));
      }

      var bindingId = $"binding-{Guid.NewGuid():N}";

      return await _commands.Execute(
        new CreateScanProcessingStorageBinding(
          NewRequestId(),
          actor,
          client.ClientId,
          bindingId,
          request.Label,
          request.Capabilities),
        When<ScanProcessingStorageBindingCreated>.Then(e => Created(
          $"/api/scan-processing/storage-bindings/clients/{e.ClientId}/bindings/{e.Binding.BindingId}",
          new ScanProcessingStorageBindingCreatedResponse(e.StorageRevision, e.Binding))),
        When<ScanProcessingStorageBindingRequestRejected>.Then(MapRejected));
    }

    [HttpPost("bindings/{bindingId}/activate")]
    public async Task<IActionResult> Activate(
      string clientId,
      string bindingId,
      [FromBody] ActivateScanProcessingStorageBindingRequest request)
    {
      if(!TryResolveActor(out var actor))
      {
        return RegisteredActorRequired();
      }

      if(request == null)
      {
        return BadRequest(Issue(
          ScanProcessingStorageBindingIssueCodes.InvalidRequest,
          "Storage binding activation request body is required.",
          "request"));
      }

      var client = await ResolveClient(clientId);

      if(client == null)
      {
        return NotFound(Issue(
          ScanProcessingStorageBindingIssueCodes.ClientNotFound,
          "The Version 1 client/workspace was not found.",
          "clientId"));
      }

      return await _commands.Execute(
        new ActivateScanProcessingStorageBinding(
          NewRequestId(),
          actor,
          client.ClientId,
          bindingId,
          request.ConfigurationGeneration,
          request.ExpectedStorageRevision),
        When<ScanProcessingStorageBindingActivated>.Then(e => Ok(ActivationResponse(e))),
        When<ScanProcessingStorageBindingActivationUnchanged>.Then(e => Ok(ActivationResponse(e))),
        When<ScanProcessingStorageBindingRequestRejected>.Then(MapRejected));
    }

    async Task<KnownClient> ResolveClient(string clientId)
    {
      var id = Id.From(clientId);
      var lookup = await _queryDb.ReadQuery<MicrofilmClientLookupQuery>();
      return lookup.TryGetClient(id, out var client) ? client : null;
    }

    bool TryResolveActor(out ScanProcessingActorIdentity actor) =>
      _actors.TryResolve(HttpContext.User, out actor);

    IActionResult RegisteredActorRequired() =>
      Unauthorized(Issue(
        ScanProcessingStorageBindingIssueCodes.AuthenticatedActorRequired,
        "A registered authenticated actor is required.",
        "actor"));

    IActionResult MapRejected(ScanProcessingStorageBindingRequestRejected rejected)
    {
      var envelope = Issue(rejected.Code, rejected.Message, rejected.Field);

      return rejected.Code switch
      {
        ScanProcessingStorageBindingIssueCodes.AuthenticatedActorRequired => Unauthorized(envelope),
        ScanProcessingStorageBindingIssueCodes.ClientNotFound => NotFound(envelope),
        ScanProcessingStorageBindingIssueCodes.BindingNotFound => NotFound(envelope),
        ScanProcessingStorageBindingIssueCodes.BindingAlreadyExists => Conflict(envelope),
        ScanProcessingStorageBindingIssueCodes.StorageRevisionStale => Conflict(envelope),
        ScanProcessingStorageBindingIssueCodes.ConfigurationGenerationStale => Conflict(envelope),
        _ => BadRequest(envelope)
      };
    }

    ProcessingIssueEnvelope Issue(string code, string message, string field) =>
      new(new ProcessingIssue
      {
        Code = code,
        Severity = ProcessingIssueSeverity.Error,
        Message = message,
        Field = field,
        CorrelationId = HttpContext?.TraceIdentifier
      });

    static ScanProcessingStorageBindingsResponse StorageResponse(ScanProcessingStorageBindingState state) =>
      new(
        state.ClientId,
        state.WorkspaceId,
        state.StorageRevision,
        state.ActiveBinding?.Clone(),
        state.BindingsById.Values
          .OrderBy(binding => binding.Label, StringComparer.OrdinalIgnoreCase)
          .ThenBy(binding => binding.BindingId, StringComparer.Ordinal)
          .Select(binding => binding.Clone())
          .ToList());

    static ScanProcessingStorageBindingActivatedResponse ActivationResponse(
      ScanProcessingStorageBindingActivated e) =>
      new(
        e.ClientId,
        e.WorkspaceId,
        e.StorageRevision,
        new ScanProcessingActiveStorageBinding(
          e.BindingId,
          e.ConfigurationGeneration,
          e.StorageRevision));

    static ScanProcessingStorageBindingActivatedResponse ActivationResponse(
      ScanProcessingStorageBindingActivationUnchanged e) =>
      new(
        e.ClientId,
        e.WorkspaceId,
        e.StorageRevision,
        new ScanProcessingActiveStorageBinding(
          e.BindingId,
          e.ConfigurationGeneration,
          e.StorageRevision));

    static string NewRequestId() => Guid.NewGuid().ToString("N");
  }

  public sealed class CreateScanProcessingStorageBindingRequest
  {
    public string Label { get; set; }
    public List<ScanProcessingStorageCapability> Capabilities { get; set; } = new();
  }

  public sealed class ActivateScanProcessingStorageBindingRequest
  {
    public long ConfigurationGeneration { get; set; }
    public long ExpectedStorageRevision { get; set; }
  }

  public sealed record ScanProcessingStorageBindingsResponse(
    string ClientId,
    string WorkspaceId,
    long StorageRevision,
    ScanProcessingActiveStorageBinding ActiveBinding,
    IReadOnlyList<ScanProcessingStorageBindingDefinition> Bindings);

  public sealed record ScanProcessingStorageBindingCreatedResponse(
    long StorageRevision,
    ScanProcessingStorageBindingDefinition Binding);

  public sealed record ScanProcessingStorageBindingActivatedResponse(
    string ClientId,
    string WorkspaceId,
    long StorageRevision,
    ScanProcessingActiveStorageBinding ActiveBinding);

  /// <summary>
  /// Output-only shape for a deliberately scoped, Operations-resolved informational path.
  /// FullPath is never accepted by any package 4 request or stored in binding state.
  /// </summary>
  public sealed record ScanProcessingInformationalPathResponse(string Label, string FullPath);

  public sealed record ScanProcessingResourceReferenceResponse(
    string Id,
    string Kind,
    string DisplayName,
    string Version,
    IReadOnlyList<string> Capabilities,
    DateTimeOffset? ExpiresAt,
    ScanProcessingInformationalPathResponse InformationalPath);
}
