using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Assets;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Transactions;

namespace Quantum.Web.Wasp.Controllers;

public class TransactionController : WaspHttpClient
{
    public TransactionController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<string>>> GetDisposeReasonsAsync() =>
        GetAsync<List<string>>("public-api/transactions/dispose/reasons");

    public Task<WaspResult<int>> DisposeAsync(AssetDisposeModel model) =>
        PostAsync<int>("public-api/transactions/dispose", model);

    public Task<WaspResult<List<int>>> MoveAsync(IReadOnlyList<AssetMoveModel> moves) =>
        BatchAsync<AssetMoveModel, int>(moves, batch =>
            PostAsync<List<int>>("public-api/transactions/public/asset/move", batch));

    public Task<WaspResult<List<int>>> CheckOutAsync(IReadOnlyList<AssetCheckOutModel> checkOuts) =>
        BatchAsync<AssetCheckOutModel, int>(checkOuts, batch =>
            PostAsync<List<int>>("public-api/transactions/public/asset/check-out", batch));

    public Task<WaspResult<List<int>>> CheckInAsync(IReadOnlyList<AssetCheckInModel> checkIns) =>
        BatchAsync<AssetCheckInModel, int>(checkIns, batch =>
            PostAsync<List<int>>("public-api/transactions/public/asset/check-in", batch));

    public Task<WaspResult<List<AssetTransactionModel>>> GetHistoryAsync(AssetTransactionSearch search) =>
        PostAsync<List<AssetTransactionModel>>("public-api/transactions/asset/history", search);

    public Task<WaspResult<List<WtResult>>> AuditCountAsync(object auditData) =>
        PostAsync<List<WtResult>>("public-api/transactions/public-api/audit-count", auditData);

    public Task<WaspResult<List<WtResult>>> AuditCountV2Async(object auditData) =>
        PostAsync<List<WtResult>>("public-api/transactions/audit-count-v2", auditData);

    public Task<WaspResult<List<WtResult>>> ReconcileAsync(object reconcileData) =>
        PostAsync<List<WtResult>>("public-api/transactions/public-api/reconcile", reconcileData);

    public Task<WaspResult<List<WtResult>>> ReconcileV2Async(object reconcileData) =>
        PostAsync<List<WtResult>>("public-api/transactions/reconcile-v2", reconcileData);

    public Task<WaspResult<object>> RfScanAsync(object scanData) =>
        PostAsync<object>("public-api/transactions/rfscan", scanData);

    public Task<Stream> StreamCsvAsync(GridStreamRequestModel request) =>
        PostStreamAsync("public-api/transactions/streamgridrequestcsv", request);

    public Task<Stream> StreamArchiveCsvAsync(GridStreamRequestModel request) =>
        PostStreamAsync("public-api/transactions/streamgridrequestarchivecsv", request);

    public Task<WaspResult<List<AssetTransactionModel>>> HistoryGridAsync(object request) =>
        PostAsync<List<AssetTransactionModel>>("public-api/transactions/grid-query/history-public-api", request);

    public Task<WaspResult<object>> HistoryNotesAsync(object request) =>
        PostAsync<object>("public-api/transactions/grid-query/history-notes-public-api", request);

    public Task<WaspResult<List<AssetTransactionModel>>> HistoryGridV2Async(AdvancedSearchParameters search) =>
        PostAsync<List<AssetTransactionModel>>("public-api/transactions/grid-query/transaction-history-v2", search);
}
