using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class AiRiskAssessment
{
    public Guid Id { get; set; }

    public string EntityType { get; set; } = null!;

    public Guid EntityId { get; set; }

    public decimal RiskScore { get; set; }

    public string RiskLevel { get; set; } = null!;

    public string RiskFactorsJson { get; set; } = null!;

    public string RecommendationsJson { get; set; } = null!;

    public string? Model { get; set; }

    public DateTime AssessedAt { get; set; }

    public string AssessedBy { get; set; } = null!;
}
