using System;
using System.Collections.Generic;

namespace OmniBizAI.Models
{
    public enum RiskLevel { Low, Medium, High }
    public enum PrStatus { Pending, Approved, Rejected, NeedRevision }

    public class PurchaseRequest
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public RiskLevel AiRisk { get; set; }
        public PrStatus Status { get; set; }
        public List<PrItem> Items { get; set; } = [];
        public List<WorkflowStep> Workflow { get; set; } = [];
        public string? RejectReason { get; set; }
        public string? RevisionNote { get; set; }
    }

    public class PrItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total => Quantity * UnitPrice;
    }

    public class WorkflowStep
    {
        public string StepName { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public bool IsCompleted { get; set; }
    }
}