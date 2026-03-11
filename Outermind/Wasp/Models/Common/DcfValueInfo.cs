using System;

namespace Quantum.Wasp.Models.Common;

public class DcfValueInfo
{
    public int ImportRowNumber { get; set; }
    public string DcfLabel { get; set; }
    public int DCFDataType { get; set; }
    public string DcfTextValue { get; set; }
    public decimal? DcfNumberValue { get; set; }
    public DateTime? DcfDateValue { get; set; }
    public int DcfValueRecordStatus { get; set; }
}
