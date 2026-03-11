namespace Quantum.Wasp.Models.PurchaseOrders;

public class AssetPurchaseOrderLineInfo
{
    public int LineNumber { get; set; }
    public string AssetTag { get; set; }
    public string AssetTypeNumber { get; set; }
    public string AssetDescription { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? TotalCost { get; set; }
}
