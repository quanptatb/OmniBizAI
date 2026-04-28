using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OmniBizAI.ViewModels;

public sealed class PaymentRequestCreateViewModel
{
    [Required]
    [StringLength(300, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public Guid? VendorId { get; set; }

    public Guid? BudgetId { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? PaymentDueDate { get; set; }

    [Required]
    public string Priority { get; set; } = "Normal";

    public List<PaymentRequestItemRequest> Items { get; set; } = [new()];

    public List<SelectListItem> Departments { get; set; } = [];
    public List<SelectListItem> Categories { get; set; } = [];
    public List<SelectListItem> Vendors { get; set; } = [];
    public List<SelectListItem> Budgets { get; set; } = [];
    public List<SelectListItem> Priorities { get; set; } = [];
}
