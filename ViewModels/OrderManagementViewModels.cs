using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.ViewModels;

public class SalesOrderIndexViewModel
{
    public List<SalesOrderSummaryViewModel> Orders { get; set; } = new();
    public int TotalOrders { get; set; }
    public int PendingApprovalCount { get; set; }
    public int InProductionCount { get; set; }
    public int CompletedCount { get; set; }
    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
}

public class SalesOrderSummaryViewModel
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public SalesOrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public string? WorkflowStatus { get; set; }
}

public class SalesOrderDetailViewModel
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public SalesOrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public string? WorkflowStatus { get; set; }

    public List<SalesOrderLineViewModel> Lines { get; set; } = new();
    public List<ProductionStepViewModel> ProductionSteps { get; set; } = new();
    public List<ProductTraceabilityViewModel> Traceabilities { get; set; } = new();
    public List<ApprovalStepItemViewModel> ApprovalHistory { get; set; } = new();
}

public class SalesOrderLineViewModel
{
    public Guid Id { get; set; }
    public Guid ProductServiceId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class ProductionStepViewModel
{
    public Guid Id { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public ProductionStepStatus Status { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public QcStatus QcStatus { get; set; }
    public string? QcNotes { get; set; }
    public Guid? QcUserId { get; set; }
    public string? QcUserName { get; set; }
    public DateTimeOffset? QcCheckedAt { get; set; }
}

public class ProductTraceabilityViewModel
{
    public Guid Id { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string OriginDetails { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class ApprovalStepItemViewModel
{
    public Guid Id { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public string? AssignedRole { get; set; }
    public string? DecisionNote { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class SalesOrderCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn khách hàng")]
    public Guid CustomerId { get; set; }

    public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly DeliveryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

    public string? Notes { get; set; }

    public List<SelectOption> Customers { get; set; } = new();
    public List<SelectOption> Products { get; set; } = new();

    // To submit with initial lines
    public List<SalesOrderLineInputViewModel> Lines { get; set; } = new();
}

public class SalesOrderLineInputViewModel
{
    [Required(ErrorMessage = "Chọn sản phẩm")]
    public Guid ProductServiceId { get; set; }

    [Range(0.01, 99999999, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public decimal Quantity { get; set; } = 1;

    [Range(0, 9999999999, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0")]
    public decimal UnitPrice { get; set; } = 0;
}

public class SalesOrderQcInputViewModel
{
    public Guid ProductionStepId { get; set; }
    public QcStatus QcStatus { get; set; }
    public string? QcNotes { get; set; }
}
