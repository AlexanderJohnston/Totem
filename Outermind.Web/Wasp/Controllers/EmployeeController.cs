using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Assets;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Employees;

namespace Quantum.Web.Wasp.Controllers;

public class EmployeeController : WaspHttpClient
{
    public EmployeeController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<EmployeeInfo>>> CreateNewAsync(IReadOnlyList<EmployeeInfo> employees) =>
        BatchAsync<EmployeeInfo, EmployeeInfo>(employees, batch =>
            PostAsync<List<EmployeeInfo>>("public-api/employees/createNew", batch));

    public Task<WaspResult<List<EmployeeInfo>>> UpdateExistingAsync(IReadOnlyList<EmployeeInfo> employees) =>
        BatchAsync<EmployeeInfo, EmployeeInfo>(employees, batch =>
            PostAsync<List<EmployeeInfo>>("public-api/employees/updateExisting", batch));

    public Task<WaspResult<List<EmployeeInfo>>> SaveAsync(IReadOnlyList<EmployeeInfo> employees) =>
        BatchAsync<EmployeeInfo, EmployeeInfo>(employees, batch =>
            PostAsync<List<EmployeeInfo>>("public-api/employees/save", batch));

    public Task<WaspResult<List<WtResult>>> DeleteAsync(IReadOnlyList<string> employeeNumbers) =>
        BatchVoidAsync(employeeNumbers, batch =>
            PostAsync<List<WtResult>>("public-api/employees/delete", batch));

    public Task<WaspResult<EmployeeInfo>> SearchExactAsync(string searchText) =>
        PostAsync<EmployeeInfo>("public-api/employees/search/exact", searchText);

    public Task<WaspResult<List<EmployeeInfo>>> AdvancedSearchAsync(AdvancedSearchParameters search) =>
        PostAsync<List<EmployeeInfo>>("public-api/employees/advancedinfosearch", search);

    public Task<WaspResult<List<EmployeeInfo>>> GetByNumberAsync(IReadOnlyList<string> employeeNumbers) =>
        PostAsync<List<EmployeeInfo>>("public-api/employees/getemployeesbynumber", employeeNumbers);

    public Task<WaspResult<List<EmployeeInfo>>> GetByNumberV2Async(IReadOnlyList<string> employeeNumbers) =>
        PostAsync<List<EmployeeInfo>>("public-api/employees/getemployeesbynumber-v2", employeeNumbers);

    public Task<WaspResult<List<EmployeeInfo>>> InfoSearchAsync(string searchText) =>
        PostAsync<List<EmployeeInfo>>("public-api/employees/infosearch", CreateSearchPatternRequest(searchText));

    public Task<WaspResult<List<AssetCheckOutStatus>>> GetCheckoutStatusAsync(string employeeNumber) =>
        GetAsync<List<AssetCheckOutStatus>>($"public-api/employees/checkout-status/{Uri.EscapeDataString(employeeNumber)}");

    public Task<WaspResult<EmployeeInfo>> GetByRfScanAsync(RfScanModel model) =>
        PostAsync<EmployeeInfo>("public-api/employees/GetEmployeeByRfScanAsync", model);
}
