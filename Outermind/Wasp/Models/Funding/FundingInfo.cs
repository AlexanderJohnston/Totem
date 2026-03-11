using System;
using System.Collections.Generic;
using Quantum.Wasp.Models.Common;

namespace Quantum.Wasp.Models.Funding;

public class FundingInfo
{
    public int RowNumber { get; set; }
    public string FundingName { get; set; }
    public string FundingDescription { get; set; }
    public string FundingType { get; set; }
    public decimal? FundingAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? FundingRecordStatus { get; set; }
    public List<DcfValueInfo> CustomFields { get; set; }
    public List<string> ApplicableFields { get; set; }
}
