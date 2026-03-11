using System;
using System.Collections.Generic;
using Quantum.Wasp.Models.Common;

namespace Quantum.Wasp.Models.PurchaseOrders;

public class AssetPurchaseOrderInfo
{
    public int RowNumber { get; set; }
    public int? OrderId { get; set; }
    public string PurchaseOrderNumber { get; set; }
    public string DescriptionText { get; set; }
    public string ReferenceNumber { get; set; }
    public DateTime? DateStarted { get; set; }
    public DateTime? DateDue { get; set; }
    public string VendorNumber { get; set; }
    public string OrderStatus { get; set; }
    public string OrderStatusReasonCode { get; set; }
    public string ShipMethod { get; set; }
    public string PayMethod { get; set; }
    public decimal? ShippingCost { get; set; }
    public decimal? TaxCost { get; set; }
    public decimal? TotalCost { get; set; }
    public AddressInfo ShipToContact { get; set; }
    public AddressInfo VendorContact { get; set; }
    public List<AssetPurchaseOrderLineInfo> PurchaseOrderLines { get; set; }
    public List<NoteInfo> PurchaseOrderNotes { get; set; }
    public decimal? Quantity { get; set; }
    public int? LocationId { get; set; }
    public string SiteName { get; set; }
    public int? ContainerId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public int? SupplierId { get; set; }
    public string SupplierName { get; set; }
    public decimal? PurchaseCost { get; set; }
    public decimal? Accrual { get; set; }
    public decimal? Outstanding { get; set; }
}
