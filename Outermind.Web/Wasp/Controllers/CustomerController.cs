using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Assets;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Customers;

namespace Quantum.Web.Wasp.Controllers;

public class CustomerController : WaspHttpClient
{
    public CustomerController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<CustomerInfo>>> CreateNewAsync(IReadOnlyList<CustomerInfo> customers) =>
        BatchAsync<CustomerInfo, CustomerInfo>(customers, batch =>
            PostAsync<List<CustomerInfo>>("public-api/customers/createNew", batch));

    public Task<WaspResult<List<CustomerInfo>>> UpdateExistingAsync(IReadOnlyList<CustomerInfo> customers) =>
        BatchAsync<CustomerInfo, CustomerInfo>(customers, batch =>
            PostAsync<List<CustomerInfo>>("public-api/customers/updateExisting", batch));

    public Task<WaspResult<List<CustomerInfo>>> SaveAsync(IReadOnlyList<CustomerInfo> customers) =>
        BatchAsync<CustomerInfo, CustomerInfo>(customers, batch =>
            PostAsync<List<CustomerInfo>>("public-api/customers/save", batch));

    public Task<WaspResult<List<WtResult>>> DeleteAsync(IReadOnlyList<string> customerNumbers) =>
        BatchVoidAsync(customerNumbers, batch =>
            PostAsync<List<WtResult>>("public-api/customers/delete", batch));

    public Task<WaspResult<List<AssetCheckOutStatus>>> GetCheckoutStatusAsync(string customerNumber) =>
        GetAsync<List<AssetCheckOutStatus>>($"public-api/customers/checkout-status/{Uri.EscapeDataString(customerNumber)}");

    public Task<WaspResult<List<CustomerInfo>>> AdvancedSearchAsync(AdvancedSearchParameters search) =>
        PostAsync<List<CustomerInfo>>("public-api/customers/advancedinfosearch", search);

    public Task<WaspResult<List<CustomerInfo>>> GetByNumberAsync(IReadOnlyList<string> customerNumbers) =>
        PostAsync<List<CustomerInfo>>("public-api/customers/GetCustomersByNumber", customerNumbers);
}
