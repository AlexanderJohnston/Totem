using System.Collections.Generic;
using Quantum.Wasp.Models.Common;

namespace Quantum.Wasp.Models.Vendors;

public class VendorInfo
{
    public int RowNumber { get; set; }
    public string VendorNumber { get; set; }
    public string VendorName { get; set; }
    public string VendorDescription { get; set; }
    public string Website { get; set; }
    public string Email { get; set; }
    public string ContactName { get; set; }
    public string ContactEmail { get; set; }
    public int? VendorRecordStatus { get; set; }
    public List<DcfValueInfo> CustomFields { get; set; }
    public List<string> ApplicableFields { get; set; }
}
