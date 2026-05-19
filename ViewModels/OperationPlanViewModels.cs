using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities;

namespace OmniBizAI.ViewModels;

public class OperationPlanListViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public int TaskCount { get; set; }
}

public class OperationPlanDetailViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int ProgressPercent { get; set; }
    public List<PlanTaskViewModel> Tasks { get; set; } = new();
}

public class PlanTaskViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? AssignedUserName { get; set; }
    public string? EquipmentName { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
}

public class OperationPlanCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên kế hoạch")]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string PlanType { get; set; } = "Daily";

    [Required]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);

    public string? Notes { get; set; }
}

public class PlanTaskCreateViewModel
{
    public Guid PlanId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên công việc")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public Guid? AssignedUserId { get; set; }
    public Guid? EquipmentId { get; set; }

    public List<SelectOption> Users { get; set; } = new();
    public List<SelectOption> Equipments { get; set; } = new();
}
