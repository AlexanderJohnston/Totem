using System.Collections.Generic;
using Quantum.Wasp.Models.Common;

namespace Quantum.Wasp.Models.Assets;

public class GridStreamRequestModel
{
    public AdvancedSearchParameters GridRequest { get; set; }
    public List<string> FieldTitles { get; set; }
}
