using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class EmployeeProfile : TenantEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    [StringLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public DateOnly? StartDate { get; set; }

    public ICollection<EmployeeContract> Contracts { get; set; } = new List<EmployeeContract>();
    public ICollection<EmployeeDepartmentAssignment> DepartmentAssignments { get; set; } = new List<EmployeeDepartmentAssignment>();
}
