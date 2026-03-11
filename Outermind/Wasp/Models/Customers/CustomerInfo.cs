using System.Collections.Generic;
using Quantum.Wasp.Models.Common;

namespace Quantum.Wasp.Models.Customers;

public class CustomerInfo
{
    public int RowNumber { get; set; }
    public int? CustomerId { get; set; }
    public string CustomerNumber { get; set; }
    public string CustomerName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Department { get; set; }
    public string SiteName { get; set; }
    public int? CustomerRecordStatus { get; set; }
    public List<DcfValueInfo> CustomFields { get; set; }
    public List<string> ApplicableFields { get; set; }
}
