using System;
using System.Collections.Generic;
namespace Quantum.Wasp.Models.Transactions;

public class AssetDisposeModel
{
    public string DisposeReason { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string RecordSource { get; set; }
    public string Note { get; set; }
    public List<AssetsToDispose> Assets { get; set; }
}

public class AssetsToDispose
{
    public string AssetTag { get; set; }
    public string SiteName { get; set; }
    public string LocationCode { get; set; }
    public string GroupTag { get; set; }
    public decimal? Quantity { get; set; }
}
