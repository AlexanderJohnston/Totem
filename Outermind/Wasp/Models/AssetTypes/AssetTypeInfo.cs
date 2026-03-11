using System.Collections.Generic;
using Quantum.Wasp.Models.Common;

namespace Quantum.Wasp.Models.AssetTypes;

public class AssetTypeInfo
{
    public int RowNumber { get; set; }
    public string AssetTypeNumber { get; set; }
    public string AssetTypeDescription { get; set; }
    public int? AssetClass { get; set; }
    public string DepreciationClassName { get; set; }
    public string ManufacturerName { get; set; }
    public string CategoryDescription { get; set; }
    public string SupplierNumber { get; set; }
    public string AssetTypeModelNumber { get; set; }
    public int? AssetTypeCheckOutDuration { get; set; }
    public int? AssetTypeLeadTime { get; set; }
    public bool AssetTypeAutoFillData { get; set; }
    public decimal? AssetDefaultCost { get; set; }
    public int? AssetTypeRecordStatus { get; set; }
    public List<DcfValueInfo> CustomFields { get; set; }
}
