using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class OkrObjective : TenantEntity
{
    [StringLength(255)]
    public string ObjectiveName { get; set; } = string.Empty;

    public OkrLevel Level { get; set; } = OkrLevel.Company;

    [StringLength(50)]
    public string? Cycle { get; set; }

    public OkrStatus Status { get; set; } = OkrStatus.Draft;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<OkrKeyResult> KeyResults { get; set; } = new List<OkrKeyResult>();
    public ICollection<OkrMissionMapping> MissionMappings { get; set; } = new List<OkrMissionMapping>();
    public ICollection<OkrDepartmentAllocation> DepartmentAllocations { get; set; } = new List<OkrDepartmentAllocation>();
    public ICollection<OkrEmployeeAllocation> EmployeeAllocations { get; set; } = new List<OkrEmployeeAllocation>();

    [NotMapped]
    public decimal TotalProgress
    {
        get
        {
            if (KeyResults == null || !KeyResults.Any()) return 0;
            decimal total = KeyResults.Sum(kr => kr.Progress);
            return Math.Round(total / KeyResults.Count, 2);
        }
    }
}
