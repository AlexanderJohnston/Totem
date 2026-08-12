using System.Collections.Generic;
using Totem.Timeline;

namespace Outermind.Microfilm
{
  public sealed class DefineScanProcessingRole : Command
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Permissions { get; set; }

    public DefineScanProcessingRole(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      string name,
      string description,
      IEnumerable<string> permissions)
    {
      RequestId = requestId;
      ActingActor = actingActor;
      Name = name;
      Description = description;
      Permissions = permissions == null ? new List<string>() : new List<string>(permissions);
    }
  }

  public sealed class ReplaceScanProcessingRole : Command
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public string RoleId { get; set; }
    public int ExpectedVersion { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Permissions { get; set; }

    public ReplaceScanProcessingRole(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      string roleId,
      int expectedVersion,
      string name,
      string description,
      IEnumerable<string> permissions)
    {
      RequestId = requestId;
      ActingActor = actingActor;
      RoleId = roleId;
      ExpectedVersion = expectedVersion;
      Name = name;
      Description = description;
      Permissions = permissions == null ? new List<string>() : new List<string>(permissions);
    }
  }

  public sealed class AssignScanProcessingRole : Command
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public ScanProcessingActorIdentity TargetActor { get; set; }
    public string RoleId { get; set; }

    public AssignScanProcessingRole(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      ScanProcessingActorIdentity targetActor,
      string roleId)
    {
      RequestId = requestId;
      ActingActor = actingActor;
      TargetActor = targetActor;
      RoleId = roleId;
    }
  }

  public sealed class RevokeScanProcessingRole : Command
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public ScanProcessingActorIdentity TargetActor { get; set; }
    public string RoleId { get; set; }

    public RevokeScanProcessingRole(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      ScanProcessingActorIdentity targetActor,
      string roleId)
    {
      RequestId = requestId;
      ActingActor = actingActor;
      TargetActor = targetActor;
      RoleId = roleId;
    }
  }

  /// <summary>
  /// Appended only after Web has validated the runtime-configured demo secret and resolved an existing user.
  /// The secret is deliberately absent from this durable command.
  /// </summary>
  public sealed class GrantScanProcessingManager : Command
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity TargetActor { get; set; }

    public GrantScanProcessingManager(string requestId, ScanProcessingActorIdentity targetActor)
    {
      RequestId = requestId;
      TargetActor = targetActor;
    }
  }

  /// <summary>
  /// Internal durable request created by a future operation endpoint after resolving actor and scope.
  /// It deliberately contains no client-authored role or permission claims.
  /// </summary>
  public sealed class RequestScanProcessingAuthorization : Command
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity Actor { get; set; }
    public string Permission { get; set; }
    public ScanProcessingResolvedScope ResolvedScope { get; set; }

    public RequestScanProcessingAuthorization(
      string requestId,
      ScanProcessingActorIdentity actor,
      string permission,
      ScanProcessingResolvedScope resolvedScope)
    {
      RequestId = requestId;
      Actor = actor;
      Permission = permission;
      ResolvedScope = resolvedScope;
    }
  }
}
