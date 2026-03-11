using System.Collections.Generic;

namespace Quantum.Wasp.Models.Common;

public class WaspResult<T>
{
    public T Data { get; set; }
    public List<WtResult> Messages { get; set; } = new();
    public int? BatchNumber { get; set; }
    public bool HasError { get; set; }
    public bool HasHttpError { get; set; }
    public bool HasMessage { get; set; }
    public bool HasSuccessWithMoreDataRemaining { get; set; }
    public long TotalRecordsLongCount { get; set; }
}
