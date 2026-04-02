using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outermind.Microfilm
{
  /// <summary>
  /// Provides access to WASP asset data for import processing
  /// </summary>
  public interface IWaspAssetService
  {
    Task<List<string>> GetAssetIdsAsync();
  }
}
