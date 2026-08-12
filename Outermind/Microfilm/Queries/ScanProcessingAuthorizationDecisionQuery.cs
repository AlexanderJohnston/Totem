using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Durable decision lookup by operation request ID for lost-response reconciliation.
  /// </summary>
  public sealed class ScanProcessingAuthorizationDecisionQuery : Query
  {
    public string RequestId { get; set; }
    public bool? Authorized { get; set; }
    public ScanProcessingActorIdentity Actor { get; set; }
    public string Permission { get; set; }
    public ScanProcessingResolvedScope ResolvedScope { get; set; }
    public List<string> EffectiveRoleIds { get; set; } = new();
    public string Code { get; set; }
    public string Message { get; set; }
    public long AuthorizationRevision { get; set; }

    static Id RouteFirst(ScanProcessingOperationAuthorized e) => Id.From(e.RequestId);
    static Id RouteFirst(ScanProcessingOperationAuthorizationRejected e) => Id.From(e.RequestId);

    void Given(ScanProcessingOperationAuthorized e)
    {
      RequestId = e.RequestId;
      Authorized = true;
      Actor = e.Actor?.Clone();
      Permission = e.Permission;
      ResolvedScope = e.ResolvedScope?.Clone();
      EffectiveRoleIds = new List<string>(e.EffectiveRoleIds ?? new List<string>());
      Code = null;
      Message = null;
      AuthorizationRevision = e.AuthorizationRevision;
    }

    void Given(ScanProcessingOperationAuthorizationRejected e)
    {
      RequestId = e.RequestId;
      Authorized = false;
      Actor = e.Actor?.Clone();
      Permission = e.Permission;
      ResolvedScope = e.ResolvedScope?.Clone();
      EffectiveRoleIds = new List<string>();
      Code = e.Code;
      Message = e.Message;
      AuthorizationRevision = e.AuthorizationRevision;
    }
  }
}
