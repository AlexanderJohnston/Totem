using System;
namespace Quantum.Wasp.Models.Assets;

public class AssetCheckOutStatus
{
    public string AssetTag { get; set; }
    public string AssetDescription { get; set; }
    public string CheckedOutTo { get; set; }
    public string CheckedOutToNumber { get; set; }
    public DateTime? CheckOutDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string SiteName { get; set; }
    public string LocationCode { get; set; }
}
