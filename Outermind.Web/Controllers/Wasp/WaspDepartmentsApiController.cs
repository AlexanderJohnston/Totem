using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Departments;
using DepartmentService = Quantum.Web.Wasp.Controllers.DepartmentController;

namespace Outermind.Controllers.Wasp;

[ApiController]
[Route("public-api/departments")]
public class WaspDepartmentsApiController : WaspApiControllerBase
{
    private readonly DepartmentService _departments;

    public WaspDepartmentsApiController(DepartmentService departments, ILogger<WaspDepartmentsApiController> logger)
        : base(logger)
    {
        _departments = departments;
    }

    [HttpPost("infosearch")]
    public Task<ActionResult<WaspResult<List<DepartmentInfo>>>> InfoSearch([FromBody] string searchText) =>
        ExecuteAsync(() => _departments.InfoSearchAsync(searchText), nameof(InfoSearch));
}
