using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Transactions;
using TransactionService = Quantum.Web.Wasp.Controllers.TransactionController;

namespace Outermind.Controllers.Wasp;

[ApiController]
[Route("public-api/transactions")]
public class WaspTransactionsApiController : WaspApiControllerBase
{
    private readonly TransactionService _transactions;

    public WaspTransactionsApiController(TransactionService transactions, ILogger<WaspTransactionsApiController> logger)
        : base(logger)
    {
        _transactions = transactions;
    }

    [HttpPost("grid-query/transaction-history-v2")]
    public Task<ActionResult<WaspResult<List<AssetTransactionModel>>>> HistoryGridV2([FromBody] AdvancedSearchParameters search) =>
        ExecuteAsync(() => _transactions.HistoryGridV2Async(search), nameof(HistoryGridV2));
}
