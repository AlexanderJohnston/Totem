using System;
using System.Collections.Generic;
using Quantum.Wasp.Models.Common;

namespace Quantum.Wasp.Models.Contracts;

public class ContractInfo
{
    public int RowNumber { get; set; }
    public string ContractNumber { get; set; }
    public string ContractDescription { get; set; }
    public string ContractType { get; set; }
    public string VendorNumber { get; set; }
    public string VendorName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Cost { get; set; }
    public string Status { get; set; }
    public List<NoteInfo> Notes { get; set; }
    public List<DcfValueInfo> CustomFields { get; set; }
    public List<string> ApplicableFields { get; set; }
}
