using System.Collections.Generic;

namespace Quantum.Wasp.Models.Common;

public class AdvancedSearchParameters
{
    public int PageSize { get; set; } = 100;
    public int PageNumber { get; set; } = 1;
    public long? TotalCountFromPriorFetch { get; set; }
    public int AdditionalSkipCount { get; set; }
    public List<SortDescriptor> Sort { get; set; }
    public TopLevelFilterType Filter { get; set; }
    public string ClientUtcOffset { get; set; }
    public int? FilterBehavior { get; set; }
    public bool IgnoreAttachments { get; set; }
    public bool IgnoreGeoLocation { get; set; }
    public string WorkingSiteIdCsvList { get; set; }
}
