using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Customers;
using CustomerService = Quantum.Web.Wasp.Controllers.CustomerController;

namespace Outermind.Controllers.Wasp;

[ApiController]
[Route("public-api/customers")]
public class WaspCustomersApiController : WaspApiControllerBase
{
    private readonly CustomerService _customers;

    public WaspCustomersApiController(CustomerService customers, ILogger<WaspCustomersApiController> logger)
        : base(logger)
    {
        _customers = customers;
    }

    [HttpPost("advancedinfosearch")]
    public Task<ActionResult<WaspResult<List<CustomerInfo>>>> AdvancedSearch([FromBody] AdvancedSearchParameters search) =>
        ExecuteAsync(() => _customers.AdvancedSearchAsync(search), nameof(AdvancedSearch));

    [HttpPost("GetCustomersByNumber")]
    public Task<ActionResult<WaspResult<List<WaspResult<CustomerInfo>>>>> GetByNumber([FromBody] IReadOnlyList<string> customerNumbers) =>
        ExecuteAsync(() => _customers.GetByNumberAsync(customerNumbers), nameof(GetByNumber));
}
