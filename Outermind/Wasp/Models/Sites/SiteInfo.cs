using System.Collections.Generic;
using Quantum.Wasp.Models.Common;

namespace Quantum.Wasp.Models.Sites;

public class SiteInfo
{
    public int RowNumber { get; set; }
    public string SiteName { get; set; }
    public string SiteDescription { get; set; }
    public int? SiteRecordStatus { get; set; }
    public List<DcfValueInfo> CustomFields { get; set; }
}
