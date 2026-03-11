using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.PurchaseOrders;

namespace Quantum.Web.Wasp.Controllers;

public class PurchaseOrderController : WaspHttpClient
{
    public PurchaseOrderController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<AssetPurchaseOrderInfo>>> CreateAsync(IReadOnlyList<AssetPurchaseOrderInfo> orders) =>
        BatchAsync<AssetPurchaseOrderInfo, AssetPurchaseOrderInfo>(orders, batch =>
            PostAsync<List<AssetPurchaseOrderInfo>>("public-api/ac/purchaseorder/create", batch));

    public Task<WaspResult<List<AssetPurchaseOrderInfo>>> UpdateAsync(IReadOnlyList<AssetPurchaseOrderInfo> orders) =>
        BatchAsync<AssetPurchaseOrderInfo, AssetPurchaseOrderInfo>(orders, batch =>
            PostAsync<List<AssetPurchaseOrderInfo>>("public-api/ac/purchaseorder/update", batch));

    public Task<WaspResult<List<AssetPurchaseOrderInfo>>> SearchAsync(AdvancedSearchParameters search) =>
        PostAsync<List<AssetPurchaseOrderInfo>>("public-api/ac/purchaseorder/purchaseordersearch", search);

    public Task<WaspResult<List<AssetPurchaseOrderInfo>>> GetByNumberAsync(IReadOnlyList<string> poNumbers) =>
        PostAsync<List<AssetPurchaseOrderInfo>>("public-api/ac/purchaseorder/getordersbynumber", poNumbers);

    public Task<WaspResult<List<WtResult>>> DeleteByNumberAsync(IReadOnlyList<string> poNumbers) =>
        PostAsync<List<WtResult>>("public-api/ac/purchaseorder/deleteordersbynumber", poNumbers);

    public Task<WaspResult<List<WtResult>>> UpdateStatusByNumberAsync(object statusUpdate) =>
        PostAsync<List<WtResult>>("public-api/ac/purchaseorder/updateorderstatusbynumber", statusUpdate);

    public Task<WaspResult<List<WtResult>>> ReceiveAsync(object receiveData) =>
        PostAsync<List<WtResult>>("public-api/ac/purchaseorder/receive-public", receiveData);
}
