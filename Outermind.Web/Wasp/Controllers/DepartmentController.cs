using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Departments;

namespace Quantum.Web.Wasp.Controllers;

public class DepartmentController : WaspHttpClient
{
    public DepartmentController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<DepartmentInfo>>> CreateAsync(IReadOnlyList<DepartmentInfo> departments) =>
        BatchAsync<DepartmentInfo, DepartmentInfo>(departments, batch =>
            PostAsync<List<DepartmentInfo>>("public-api/departments/create", batch));

    public Task<WaspResult<List<DepartmentInfo>>> UpdateAsync(IReadOnlyList<DepartmentInfo> departments) =>
        BatchAsync<DepartmentInfo, DepartmentInfo>(departments, batch =>
            PostAsync<List<DepartmentInfo>>("public-api/departments/update", batch));

    public Task<WaspResult<List<DepartmentInfo>>> InfoSearchAsync(string searchText) =>
        PostAsync<List<DepartmentInfo>>("public-api/departments/infosearch", CreateSearchPatternRequest(searchText));

    public Task<WaspResult<List<WtResult>>> DeleteByCodeAsync(IReadOnlyList<string> codes) =>
        BatchVoidAsync(codes, batch =>
            PostAsync<List<WtResult>>("public-api/departments/deletedepartmentsbycode", batch));
}
