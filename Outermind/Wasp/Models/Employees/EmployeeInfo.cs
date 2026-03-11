using System.Collections.Generic;
using Quantum.Wasp.Models.Common;

namespace Quantum.Wasp.Models.Employees;

public class EmployeeInfo
{
    public int RowNumber { get; set; }
    public int? EmployeeId { get; set; }
    public string EmployeeNumber { get; set; }
    public string EmployeeName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Department { get; set; }
    public string SiteName { get; set; }
    public string Title { get; set; }
    public int? EmployeeRecordStatus { get; set; }
    public List<DcfValueInfo> CustomFields { get; set; }
    public List<string> ApplicableFields { get; set; }
}
