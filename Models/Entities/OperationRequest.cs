using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class OperationRequest : TenantEntity
{
    [StringLength(50)]
    public string RequestNo { get; set; } = string.Empty;

    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? CustomerSiteId { get; set; }
    public CustomerSite? CustomerSite { get; set; }

    public Guid OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    public Guid RequestedByUserId { get; set; }
    public AppUser? RequestedByUser { get; set; }

    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

    public OperationStatus Status { get; set; } = OperationStatus.Draft;

    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? ApprovalDueAt { get; set; }
    public DateTimeOffset? ResolutionDueAt { get; set; }

    public DateOnly? DueDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? EstimatedCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ActualCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CostVariance { get; set; }

    [Column(TypeName = "decimal(9,2)")]
    public decimal? CostVariancePercent { get; set; }

    public DateTimeOffset? CostVarianceCalculatedAt { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    public ICollection<OperationRequestLine> Lines { get; set; } = new List<OperationRequestLine>();
    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
    public ICollection<AiInsight> AiInsights { get; set; } = new List<AiInsight>();
    public ICollection<OperationComment> Comments { get; set; } = new List<OperationComment>();
    // Liên kết 1-1 với KpiDefinition (Đề xuất tạo KPI)
    public KpiDefinition? KpiDefinition { get; set; }

    // Liên kết 1-1 với OkrObjective (Đề xuất tạo OKR)
    public OkrObjective? OkrObjective { get; set; }

    public ICollection<OperationSlaBreach> SlaBreaches { get; set; } = new List<OperationSlaBreach>();
    public ICollection<OperationProgressLog> ProgressLogs { get; set; } = new List<OperationProgressLog>();
    public ICollection<OperationRequestAssignment> Assignments { get; set; } = new List<OperationRequestAssignment>();
    public ICollection<GoodsIssue> GoodsIssues { get; set; } = new List<GoodsIssue>();
    public ICollection<PaymentRequest> PaymentRequests { get; set; } = new List<PaymentRequest>();
    public ICollection<OperationPlan> OperationPlans { get; set; } = new List<OperationPlan>();
}
