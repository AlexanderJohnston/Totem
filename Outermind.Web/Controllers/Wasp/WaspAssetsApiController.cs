using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Quantum.Wasp.Models.Assets;
using Quantum.Wasp.Models.Common;
using AssetService = Quantum.Web.Wasp.Controllers.AssetController;

namespace Outermind.Controllers.Wasp;

[ApiController]
[Route("public-api/assets")]
public class WaspAssetsApiController : WaspApiControllerBase
{
    private readonly AssetService _assets;

    public WaspAssetsApiController(AssetService assets, ILogger<WaspAssetsApiController> logger)
        : base(logger)
    {
        _assets = assets;
    }

    [HttpPost("assetinfosearch")]
    public Task<ActionResult<WaspResult<List<AssetInfo>>>> InfoSearch([FromBody] string searchText) =>
        ExecuteAsync(() => _assets.InfoSearchAsync(searchText), nameof(InfoSearch));
}
