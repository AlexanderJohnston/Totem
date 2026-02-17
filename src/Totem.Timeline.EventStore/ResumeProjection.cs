using System.IO;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;
using Grpc.Core;
using Totem.Runtime;
using Totem.Timeline.Area;

namespace Totem.Timeline.EventStore
{
  /// <summary>
  /// The projection installed to track the set of flows to resume
  /// </summary>
  public sealed class ResumeProjection : Notion, IResumeProjection
  {
    readonly AreaMap _area;
    readonly EventStoreProjectionManagementClient _manager;

    public ResumeProjection(AreaMap area, EventStoreProjectionManagementClient manager)
    {
      _area = area;
      _manager = manager;
    }

    public async Task Synchronize()
    {
      if(await StreamNotFound())
      {
        await CreateStream();
      }
    }

    async Task<bool> StreamNotFound()
    {
      try
      {
        await _manager.GetStatusAsync(TimelineStreams.Resume);

        return false;
      }
      catch(RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
      {
        return true;
      }
    }

    async Task CreateStream()
    {
      await _manager.CreateContinuousAsync(TimelineStreams.Resume, await ReadScript());

      Log.Debug("[timeline] Created projection {Name}", TimelineStreams.Resume);
    }

    async Task<string> ReadScript()
    {
      var type = GetType();

      using(var resource = type.Assembly.GetManifestResourceStream(type, "resume-projection.js"))
      using(var reader = new StreamReader(resource))
      {
        return await reader.ReadToEndAsync();
      }
    }
  }
}