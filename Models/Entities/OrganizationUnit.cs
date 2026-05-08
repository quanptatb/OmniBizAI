using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class OrganizationUnit : TenantEntity
{
    public Guid? ParentId { get; set; }
    public OrganizationUnit? Parent { get; set; }

    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public int Level { get; set; }

    public Guid? ManagerUserId { get; set; }
    public AppUser? ManagerUser { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<OrganizationUnit> Children { get; set; } = new List<OrganizationUnit>();
    public ICollection<Position> Positions { get; set; } = new List<Position>();
    public ICollection<EmployeeDepartmentAssignment> EmployeeAssignments { get; set; } = new List<EmployeeDepartmentAssignment>();
}
