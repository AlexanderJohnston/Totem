using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Outermind.Microfilm;
using Quantum.Web.ScanProcessing;

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

    public ScanProcessingDemoController(
      ScanProcessingDemoStore store,
      IOptions<ScanProcessingDemoOptions> options,
      IRegisteredScanProcessingActorResolver actors)
    {
      _store = store;
      _options = options.Value;
      _actors = actors;
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
    public IActionResult Start(string rollId, string rowId, [FromBody] StartScanDemoRequest request)
    {
      if(!TryBegin(out var actorId, out var error) || !ValidScope(rollId, rowId, out error))
      {
        return error;
      }

      return Map(_store.Start(actorId, rollId, rowId, request));
    }

    [HttpPost("rolls/{rollId}/rows/{rowId}/scans/{scanId}/finish")]
    public IActionResult Finish(
      string rollId,
      string rowId,
      string scanId,
      [FromBody] FinishScanDemoRequest request)
    {
      if(!TryBegin(out var actorId, out var error) || !ValidScope(rollId, rowId, out error))
      {
        return error;
      }

      return Map(_store.Finish(actorId, rollId, rowId, scanId, request));
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

      return Map(_store.Apply(actorId, planId, request));
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
