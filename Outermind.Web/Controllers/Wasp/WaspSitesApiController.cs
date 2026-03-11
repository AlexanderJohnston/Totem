using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Sites;
using SiteService = Quantum.Web.Wasp.Controllers.SiteController;

namespace Outermind.Controllers.Wasp;

[ApiController]
[Route("public-api/sites")]
public class WaspSitesApiController : WaspApiControllerBase
{
    private readonly SiteService _sites;

    public WaspSitesApiController(SiteService sites, ILogger<WaspSitesApiController> logger)
        : base(logger)
    {
        _sites = sites;
    }

    [HttpPost("infosearch")]
    public Task<ActionResult<WaspResult<List<SiteInfo>>>> InfoSearch([FromBody] string searchText) =>
        ExecuteAsync(() => _sites.InfoSearchAsync(searchText), nameof(InfoSearch));
}
