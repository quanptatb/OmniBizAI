using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class SalesOrder : TenantEntity
{
    [Required, StringLength(50)]
    public string OrderNo { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly DeliveryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; } = 0;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public Guid? WorkflowInstanceId { get; set; }
    public WorkflowInstance? WorkflowInstance { get; set; }

    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
    public ICollection<ProductionStep> ProductionSteps { get; set; } = new List<ProductionStep>();
    public ICollection<ProductTraceability> Traceabilities { get; set; } = new List<ProductTraceability>();
}
