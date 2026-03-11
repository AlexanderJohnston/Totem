using System;
namespace Quantum.Wasp.Models.Transactions;

public class AssetTransactionModel
{
    public string AssetTag { get; set; }
    public string AssetDescription { get; set; }
    public string TransactionType { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string SiteName { get; set; }
    public string LocationCode { get; set; }
    public string AssigneeName { get; set; }
    public string AssigneeNumber { get; set; }
    public decimal? Quantity { get; set; }
    public string Note { get; set; }
    public string RecordSource { get; set; }
    public string TransactionUniqueIdentifier { get; set; }
}
