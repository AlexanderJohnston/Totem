using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Quantum.Web.ScanProcessing;
using Totem;
using Totem.Timeline.Client;
using Totem.Timeline.Mvc;
using static Totem.Timeline.FlowCall;

namespace Quantum.Web.Controllers
{
  /// <summary>
  /// Disposable, feature-flagged frontend demo surface. No action in this controller performs
  /// durable orchestration, physical path resolution, or filesystem access.
  /// </summary>
  [ApiController]
  [Route("api/scan-processing")]
  public sealed class ScanProcessingDemoController : ControllerBase
  {
    const string DemoActorHeader = "X-Scan-Processing-Demo-Actor";

    readonly ScanProcessingDemoStore _store;
    readonly ScanProcessingDemoOptions _options;
    readonly IRegisteredScanProcessingActorResolver _actors;
    readonly ScanProcessingDemoPhysicalWorkflow _physical;
    readonly ICommandServer _commands;
    readonly IQueryDb _queryDb;

    public ScanProcessingDemoController(
      ScanProcessingDemoStore store,
      IOptions<ScanProcessingDemoOptions> options,
      IRegisteredScanProcessingActorResolver actors,
      ScanProcessingDemoPhysicalWorkflow physical,
      ICommandServer commands,
      IQueryDb queryDb)
    {
      _store = store;
      _options = options.Value;
      _actors = actors;
      _physical = physical;
      _commands = commands;
      _queryDb = queryDb;
    }

    [HttpGet("rolls/{rollId}/rows/{rowId}/discovery")]
    public IActionResult Discover(string rollId, string rowId)
    {
      if(!TryBegin(out var actorId, out var error))
      {
        return error;
      }

      if(!ValidScope(rollId, rowId, out error))
      {
        return error;
      }

      return Ok(_store.Discover(actorId, rollId, rowId));
    }

    [HttpPost("rolls/{rollId}/rows/{rowId}/scan/start")]
    public async System.Threading.Tasks.Task<IActionResult> Start(
      string rollId,
      string rowId,
      [FromBody] StartScanDemoRequest request)
    {
      if(!TryBegin(out var actorId, out var error) || !ValidScope(rollId, rowId, out error))
      {
        return error;
      }

      if(_physical.Enabled && !await CanonicalRowExists(rollId, rowId))
      {
        return NotFound(Issue(
          "ROW_NOT_FOUND",
          "The physical demo requires a canonical row owned by the requested roll.",
          "rowId"));
      }

      var started = _store.Start(actorId, rollId, rowId, request);
      if(!started.Succeeded || !_physical.Enabled || !string.IsNullOrWhiteSpace(started.Value.Scan.InformationalFullPath))
      {
        return Map(started);
      }

      var folder = _physical.CreateScanFolder(started.Value.Scan.FolderName);
      if(!folder.Succeeded)
      {
        _store.FailStart(actorId, rollId, rowId, started.Value.Scan.ScanId);
        return Map(folder);
      }

      return Map(_store.AttachScanFolder(
        actorId,
        rollId,
        rowId,
        started.Value.Scan.ScanId,
        folder.Value));
    }

    [HttpPost("rolls/{rollId}/rows/{rowId}/scans/{scanId}/finish")]
    public async System.Threading.Tasks.Task<IActionResult> Finish(
      string rollId,
      string rowId,
      string scanId,
      [FromBody] FinishScanDemoRequest request)
    {
      if(!TryBegin(out var actorId, out var error) || !ValidScope(rollId, rowId, out error))
      {
        return error;
      }

      if(!_physical.Enabled)
      {
        return Map(_store.Finish(actorId, rollId, rowId, scanId, request));
      }

      var validation = _store.ValidateFinish(actorId, rollId, rowId, scanId, request);
      if(!validation.Succeeded)
      {
        return Map(validation);
      }

      if(_store.TryReplayPhysicalFinish(actorId, scanId, request, out var replay))
      {
        return StatusCode(202, replay);
      }

      if(!_store.TryGetActiveScanFolder(actorId, rollId, rowId, scanId, out var folderPath))
      {
        return Conflict(Issue(
          ScanProcessingDemoIssueCodes.ScanFolderUnavailable,
          "The active physical demo scan folder is unavailable for this actor and row.",
          "scanId"));
      }

      var inspection = _physical.InspectIdf(folderPath);
      if(!inspection.Succeeded)
      {
        return Map(inspection);
      }

      var roll = Id.From(rollId);
      var lookup = await _queryDb.ReadQuery<RollMicrofilmLookupQuery>();
      if(!lookup.TryGetRoll(roll, out var knownRoll))
      {
        return NotFound(Issue("ROLL_NOT_FOUND", "The canonical roll was not found.", "rollId"));
      }

      var rowQuery = await _queryDb.ReadQuery<RollMicrofilmRowQuery>(RollMicrofilmRowQuery.CreateId(roll, rowId));
      if(rowQuery.Row == null || Id.From(rowQuery.Row.RollId) != roll)
      {
        return NotFound(Issue("ROW_NOT_FOUND", "The canonical row was not found for this roll.", "rowId"));
      }

      var auditActor = new MicrofilmAuditActorStamp(
        "identified",
        actorId,
        null,
        "scan-processing-demo");

      return await _commands.Execute(
        new UpdateRollMicrofilmRowCell(
          roll,
          knownRoll.ClientId,
          rowId,
          rowQuery.Row.Origin,
          "imageCount",
          MicrofilmCellValue.FromNumber(inspection.Value.ImageCount),
          auditActor),
        When<RollMicrofilmRowCellChanged>.Then(_ =>
        {
          var finished = _store.Finish(actorId, rollId, rowId, scanId, request);
          if(!finished.Succeeded)
          {
            return Map(finished);
          }

          var response = new FinishScanProcessingDemoResponse(
              finished.Value.Context,
              finished.Value.Scan,
              inspection.Value.ImageCount,
              inspection.Value.IdfFileName);
          _store.RememberPhysicalFinish(actorId, scanId, request, response);
          return StatusCode(202, response);
        }),
        When<RollMicrofilmTableRollNotRecognized>.Then(_ => NotFound(Issue(
          "ROLL_NOT_FOUND",
          "The canonical roll was not recognized while recording imageCount.",
          "rollId"))),
        When<RollMicrofilmTableRowKindMismatch>.Then(_ => Conflict(Issue(
          ScanProcessingDemoIssueCodes.DurableImageCountFailed,
          "The row provenance changed while recording imageCount.",
          "rowId"))),
        When<MicrofilmTableRowNotRecognized>.Then(_ => NotFound(Issue(
          "ROW_NOT_FOUND",
          "The canonical row was not recognized while recording imageCount.",
          "rowId"))),
        When<MicrofilmTableCellValueRejected>.Then(_ => UnprocessableEntity(Issue(
          ScanProcessingDemoIssueCodes.DurableImageCountFailed,
          "The durable imageCount cell update was rejected.",
          "imageCount"))));
    }

    [HttpPost("rolls/{rollId}/rows/{rowId}/processing/qpf-settings/preview")]
    public IActionResult PreviewQpf(
      string rollId,
      string rowId,
      [FromBody] PreviewQpfSettingsDemoRequest request)
    {
      if(!TryBegin(out var actorId, out var error) || !ValidScope(rollId, rowId, out error))
      {
        return error;
      }

      return Map(_store.PreviewQpf(actorId, rollId, rowId, request));
    }

    [HttpPost("rolls/{rollId}/rows/{rowId}/processing/frames-paths/preview")]
    public IActionResult PreviewFrames(
      string rollId,
      string rowId,
      [FromBody] PreviewFramesPathsDemoRequest request)
    {
      if(!TryBegin(out var actorId, out var error) || !ValidScope(rollId, rowId, out error))
      {
        return error;
      }

      return Map(_store.PreviewFrames(actorId, rollId, rowId, request));
    }

    [HttpPost("plans/{planId}/apply")]
    public IActionResult Apply(string planId, [FromBody] ApplyScanProcessingDemoPlanRequest request)
    {
      if(!TryBegin(out var actorId, out var error))
      {
        return error;
      }

      Func<ScanProcessingDemoPlanExecution, ScanProcessingDemoResult<ScanProcessingDemoQpfApplyResult>> physicalApply =
        _physical.Enabled ? _physical.ApplyQpfSettings : null;
      return Map(_store.Apply(actorId, planId, request, physicalApply));
    }

    [HttpGet("jobs/{jobId}")]
    public IActionResult GetJob(string jobId)
    {
      if(!TryBegin(out var actorId, out var error))
      {
        return error;
      }

      return Map(_store.GetJob(actorId, jobId));
    }

    [HttpPost("jobs/{jobId}/cancel")]
    public IActionResult CancelJob(string jobId)
    {
      if(!TryBegin(out var actorId, out var error))
      {
        return error;
      }

      return Map(_store.CancelJob(actorId, jobId));
    }

    [HttpPost("demo/reset")]
    public IActionResult Reset()
    {
      if(!TryBegin(out _, out var error))
      {
        return error;
      }

      return Ok(_store.Reset());
    }

    bool TryBegin(out string actorId, out IActionResult error)
    {
      actorId = null;
      error = null;

      if(!_options.Enabled)
      {
        error = NotFound(Issue(
          ScanProcessingDemoIssueCodes.Disabled,
          "The disposable Scan/Processing demo surface is disabled.",
          "ScanProcessingDemo:Enabled"));
        return false;
      }

      Response.Headers["X-Scan-Processing-Demo"] = "true";
      Response.Headers["Cache-Control"] = "no-store";

      if(_actors.TryResolve(HttpContext.User, out var actor))
      {
        actorId = actor.UserId;
        return true;
      }

      if(!_options.AllowSyntheticActor)
      {
        error = Unauthorized(Issue(
          ScanProcessingDemoIssueCodes.RegisteredActorRequired,
          "A registered actor is required when synthetic demo actors are disabled.",
          "actor"));
        return false;
      }

      var requestedActor = Request.Headers[DemoActorHeader].ToString().Trim();
      actorId = string.IsNullOrWhiteSpace(requestedActor) ? "demo-frontend" : requestedActor;

      if(actorId.Length > 100)
      {
        error = BadRequest(Issue(
          ScanProcessingDemoIssueCodes.InvalidRequest,
          $"{DemoActorHeader} must be 100 characters or fewer.",
          DemoActorHeader));
        return false;
      }

      return true;
    }

    bool ValidScope(string rollId, string rowId, out IActionResult error)
    {
      error = null;
      if(string.IsNullOrWhiteSpace(rollId))
      {
        error = BadRequest(Issue(ScanProcessingDemoIssueCodes.InvalidRequest, "rollId is required.", "rollId"));
        return false;
      }

      if(string.IsNullOrWhiteSpace(rowId))
      {
        error = BadRequest(Issue(ScanProcessingDemoIssueCodes.InvalidRequest, "rowId is required.", "rowId"));
        return false;
      }

      return true;
    }

    async System.Threading.Tasks.Task<bool> CanonicalRowExists(string rollId, string rowId)
    {
      var roll = Id.From(rollId);
      var lookup = await _queryDb.ReadQuery<RollMicrofilmLookupQuery>();
      if(!lookup.TryGetRoll(roll, out _))
      {
        return false;
      }

      var row = await _queryDb.ReadQuery<RollMicrofilmRowQuery>(RollMicrofilmRowQuery.CreateId(roll, rowId));
      return row.Row != null && Id.From(row.Row.RollId) == roll;
    }

    IActionResult Map<T>(ScanProcessingDemoResult<T> result)
    {
      if(result.Succeeded)
      {
        return StatusCode(result.StatusCode, result.Value);
      }

      result.Issue.CorrelationId = HttpContext.TraceIdentifier;
      return StatusCode(result.StatusCode, new ProcessingIssueEnvelope(result.Issue));
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
  }
}
