using System.Collections.Generic;
using Quantum.Wasp.Models.Common;

namespace Quantum.Wasp.Models.Locations;

public class LocationModelInfo
{
    public int RowNumber { get; set; }
    public string SiteName { get; set; }
    public string ZoneName { get; set; }
    public string LocationCode { get; set; }
    public string LocationDescription { get; set; }
    public string UsageTypeName { get; set; }
    public int? LocationSaleable { get; set; }
    public bool DefaultLocation { get; set; }
    public int? LocationRecordStatus { get; set; }
    public string LocationNotes { get; set; }
    public List<NoteInfo> AllLocationNotes { get; set; }
    public List<DcfValueInfo> CustomFields { get; set; }
    public decimal? LocationSequence { get; set; }
    public decimal? LocationWidth { get; set; }
    public decimal? LocationHeight { get; set; }
    public decimal? LocationDepth { get; set; }
}
