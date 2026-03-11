using System;
namespace Quantum.Wasp.Models.Transactions;

public class AssetTransactionSearch
{
    public string AssetTag { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; } = 20;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
