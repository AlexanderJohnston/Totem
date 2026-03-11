using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Contracts;
using ContractService = Quantum.Web.Wasp.Controllers.ContractController;

namespace Outermind.Controllers.Wasp;

[ApiController]
[Route("public-api/contracts")]
public class WaspContractsApiController : WaspApiControllerBase
{
    private readonly ContractService _contracts;

    public WaspContractsApiController(ContractService contracts, ILogger<WaspContractsApiController> logger)
        : base(logger)
    {
        _contracts = contracts;
    }

    [HttpPost("infosearch")]
    public Task<ActionResult<WaspResult<List<ContractInfo>>>> InfoSearch([FromBody] string searchText) =>
        ExecuteAsync(() => _contracts.InfoSearchAsync(searchText), nameof(InfoSearch));
}
