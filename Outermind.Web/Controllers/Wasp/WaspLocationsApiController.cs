using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Locations;
using LocationService = Quantum.Web.Wasp.Controllers.LocationController;

namespace Outermind.Controllers.Wasp;

[ApiController]
[Route("public-api/locations")]
public class WaspLocationsApiController : WaspApiControllerBase
{
    private readonly LocationService _locations;

    public WaspLocationsApiController(LocationService locations, ILogger<WaspLocationsApiController> logger)
        : base(logger)
    {
        _locations = locations;
    }

    [HttpPost("infosearch")]
    public Task<ActionResult<WaspResult<List<LocationModelInfo>>>> InfoSearch([FromBody] string searchText) =>
        ExecuteAsync(() => _locations.InfoSearchAsync(searchText), nameof(InfoSearch));
}
