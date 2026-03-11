using System.Collections.Generic;
using Quantum.Wasp.Models.Common;

namespace Quantum.Wasp.Models.Manufacturers;

public class ManufacturerInfo
{
    public int RowNumber { get; set; }
    public string ManufacturerName { get; set; }
    public string ManufacturerDescription { get; set; }
    public string Website { get; set; }
    public string Email { get; set; }
    public string ContactName { get; set; }
    public int? ManufacturerRecordStatus { get; set; }
    public List<DcfValueInfo> CustomFields { get; set; }
    public List<string> ApplicableFields { get; set; }
}
