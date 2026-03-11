namespace Quantum.Wasp.Models.Transactions;

public class AssetMoveModel
{
    public string AssetTag { get; set; }
    public string FromSiteName { get; set; }
    public string FromLocationCode { get; set; }
    public string FromGroupTag { get; set; }
    public string ToSiteName { get; set; }
    public string ToLocationCode { get; set; }
    public string ToGroupTag { get; set; }
    public decimal? Quantity { get; set; }
    public string RecordSource { get; set; }
    public string Note { get; set; }
    public string Condition { get; set; }
}
