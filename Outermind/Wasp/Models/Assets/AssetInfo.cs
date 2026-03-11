using System;
using System.Collections.Generic;
using Quantum.Wasp.Models.Common;

namespace Quantum.Wasp.Models.Assets;

public class AssetInfo
{
    public int RowNumber { get; set; }
    public string AssetTag { get; set; }
    public string AssetDescription { get; set; }
    public int? AssetClassId { get; set; }
    public string AssetTypeNumber { get; set; }
    public string AssetTypeDescription { get; set; }
    public string DepartmentCode { get; set; }
    public string DepartmentName { get; set; }
    public string SiteName { get; set; }
    public string SiteDescription { get; set; }
    public string LocationCode { get; set; }
    public string GroupTag { get; set; }
    public bool IsGroup { get; set; }
    public bool TransactAsWhole { get; set; }
    public bool AuditAsWhole { get; set; }
    public string SupplierNumber { get; set; }
    public string SupplierContactEmail { get; set; }
    public string SupplierContactName { get; set; }
    public string SupplierEmail { get; set; }
    public string SupplierName { get; set; }
    public string SupplierWebsite { get; set; }
    public string AssetSerialNumber { get; set; }
    public string ConditionDescription { get; set; }
    public string ManufacturerName { get; set; }
    public string AssetModelName { get; set; }
    public string CategoryDescription { get; set; }
    public int? CheckoutLength { get; set; }
    public int? CheckoutLeadTime { get; set; }
    public bool AssetIsCheckedOut { get; set; }
    public bool AssetShouldDepreciate { get; set; }
    public decimal? AssetSalvageValue { get; set; }
    public decimal? AssetDepreciatedValue { get; set; }
    public decimal? BookValue { get; set; }
    public DateTime? AssetDepreciationBeginDate { get; set; }
    public decimal? AssetDefaultCost { get; set; }
    public string AssetAssignee { get; set; }
    public string AssetAssigneeNumber { get; set; }
    public int AssetRecordStatus { get; set; }
    public string NewDefaultAttachment { get; set; }
    public bool HasAttachment { get; set; }
    public decimal? AssetTransQuantity { get; set; }
    public string OwnerName { get; set; }
    public string OwnerNumber { get; set; }
    public string OwnerFirstName { get; set; }
    public string OwnerLastName { get; set; }
    public string WarrantyProvider { get; set; }
    public DateTime? WarrantyBeginDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public string AssetPdPoNumber { get; set; }
    public DateTime? AssetPdPurchaseDate { get; set; }
    public decimal? AssetPdCost { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? Altitude { get; set; }
    public DateTime? TimeUTC { get; set; }
    public decimal? Accuracy { get; set; }
    public List<DcfValueInfo> CustomFields { get; set; }
    public List<string> AttachmentsToAdd { get; set; }
    public List<string> AttachmentsToDelete { get; set; }
    public List<KeyValuePair<string, string>> AttachmentNames { get; set; }
    public DateTime? AssetDueDate { get; set; }
    public DateTime? AssetCreatedDate { get; set; }
    public DateTime? AssetLastUpdatedDate { get; set; }
}
