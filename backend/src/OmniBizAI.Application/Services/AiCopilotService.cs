using OmniBizAI.Application.DTOs;
using OmniBizAI.Application.Interfaces;
using OmniBizAI.Domain.Entities.AI;
using OmniBizAI.Domain.Entities.Finance;
using OmniBizAI.Domain.Enums;
using OmniBizAI.Domain.Interfaces;

namespace OmniBizAI.Application.Services;

public sealed class AiCopilotService : IAiCopilotService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AiCopilotService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        var session = request.SessionId.HasValue
            ? await _unitOfWork.Repository<AiChatSession>().GetByIdAsync(request.SessionId.Value, cancellationToken)
            : null;

        if (session is null)
        {
            session = new AiChatSession
            {
                UserId = _currentUserService.UserId ?? Guid.Empty,
                Title = request.Message.Length > 80 ? request.Message[..80] : request.Message,
                ContextType = request.ContextType ?? "General"
            };
            await _unitOfWork.Repository<AiChatSession>().AddAsync(session, cancellationToken);
        }

        var answer = BuildDeterministicAnswer(request.Message);
        session.MessageCount += 2;
        session.LastMessageAt = DateTime.UtcNow;
        session.Messages.Add(new AiMessage { Role = "user", Content = request.Message });
        session.Messages.Add(new AiMessage
        {
            Role = "assistant",
            Content = answer.Content,
            CitationsJson = answer.CitationsJson,
            Model = "local-rules-fallback"
        });

        await _unitOfWork.Repository<AiGenerationHistory>().AddAsync(new AiGenerationHistory
        {
            UserId = _currentUserService.UserId ?? Guid.Empty,
            CompanyId = _unitOfWork.Repository<PaymentRequest>().Query().Select(x => x.CompanyId).FirstOrDefault(),
            Module = "Chat",
            PromptType = "QA",
            InputSummary = request.Message,
            OutputContent = answer.Content,
            Model = "local-rules-fallback",
            ExpiresAt = DateTime.UtcNow.AddDays(90)
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AiChatResponse(session.Id, answer.Content, answer.Citations);
    }

    public async Task<RiskAnalysisResponse> AnalyzeRiskAsync(RiskAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.EntityType, "PaymentRequest", StringComparison.OrdinalIgnoreCase))
        {
            return new RiskAnalysisResponse(10, "Low", ["Entity type has no custom risk rule."], ["Review manually before approval."]);
        }

        var paymentRequest = await _unitOfWork.Repository<PaymentRequest>().GetByIdAsync(request.EntityId, cancellationToken);
        if (paymentRequest is null)
        {
            return new RiskAnalysisResponse(0, "Low", ["Payment request not found."], []);
        }

        var factors = new List<string>();
        var recommendations = new List<string>();
        decimal score = 0;

        if (paymentRequest.BudgetId.HasValue)
        {
            var budget = await _unitOfWork.Repository<Budget>().GetByIdAsync(paymentRequest.BudgetId.Value, cancellationToken);
            if (budget is not null)
            {
                var projectedUtilization = budget.AllocatedAmount <= 0 ? 100 : (budget.SpentAmount + paymentRequest.TotalAmount) / budget.AllocatedAmount * 100;
                if (projectedUtilization > 100)
                {
                    score += 45;
                    factors.Add($"Projected budget utilization is {projectedUtilization:0.##}% after this request.");
                    recommendations.Add("Require Director approval and consider budget adjustment.");
                }
                else if (projectedUtilization >= budget.WarningThreshold)
                {
                    score += 25;
                    factors.Add($"Projected budget utilization reaches warning threshold: {projectedUtilization:0.##}%.");
                    recommendations.Add("Review remaining budget before approving.");
                }
            }
        }

        if (paymentRequest.VendorId.HasValue)
        {
            var recentDuplicate = _unitOfWork.Repository<PaymentRequest>().Query().Any(x =>
                x.Id != paymentRequest.Id &&
                x.VendorId == paymentRequest.VendorId &&
                x.CreatedAt >= DateTime.UtcNow.AddDays(-7) &&
                x.Status != PaymentRequestStatus.Cancelled);
            if (recentDuplicate)
            {
                score += 20;
                factors.Add("Same vendor has another payment request in the last 7 days.");
                recommendations.Add("Check duplicate invoice or split payment before approval.");
            }
        }

        var historical = _unitOfWork.Repository<PaymentRequest>().Query()
            .Where(x => x.DepartmentId == paymentRequest.DepartmentId && x.Id != paymentRequest.Id)
            .Select(x => x.TotalAmount)
            .ToList();
        if (historical.Count > 0 && paymentRequest.TotalAmount > historical.Average() * 2)
        {
            score += 25;
            factors.Add("Amount is more than twice the department historical average.");
            recommendations.Add("Ask requester for additional evidence.");
        }

        if (factors.Count == 0)
        {
            factors.Add("No major budget, duplicate vendor, or unusual amount signal detected.");
            recommendations.Add("Proceed with normal approval workflow.");
        }

        score = Math.Clamp(score, 0, 100);
        var level = score switch
        {
            >= 85 => "Critical",
            >= 70 => "High",
            >= 40 => "Medium",
            _ => "Low"
        };

        await _unitOfWork.Repository<AiRiskAssessment>().AddAsync(new AiRiskAssessment
        {
            EntityType = "PaymentRequest",
            EntityId = paymentRequest.Id,
            RiskScore = score,
            RiskLevel = level,
            RiskFactorsJson = System.Text.Json.JsonSerializer.Serialize(factors),
            RecommendationsJson = System.Text.Json.JsonSerializer.Serialize(recommendations),
            Model = "local-rules-fallback"
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RiskAnalysisResponse(score, level, factors, recommendations);
    }

    private (string Content, IReadOnlyCollection<object> Citations, string CitationsJson) BuildDeterministicAnswer(string message)
    {
        var normalized = message.ToLowerInvariant();
        if (normalized.Contains("vượt ngân sách") || normalized.Contains("vuot ngan sach") || normalized.Contains("budget"))
        {
            var overBudget = _unitOfWork.Repository<Budget>().Query()
                .Where(x => x.AllocatedAmount > 0 && x.SpentAmount > x.AllocatedAmount)
                .OrderByDescending(x => x.SpentAmount / x.AllocatedAmount)
                .Take(5)
                .ToList();
            if (overBudget.Count == 0)
            {
                return ("Hiện chưa có phòng ban hoặc danh mục nào vượt ngân sách trong dữ liệu bạn có quyền xem.", [], "[]");
            }

            var lines = overBudget.Select(x => $"- {x.Name}: đã chi {x.SpentAmount:n0}/{x.AllocatedAmount:n0} {x.UtilizationPercent:0.##}%");
            var content = "Các ngân sách đang vượt mức:\n" + string.Join('\n', lines);
            var citations = overBudget.Select(x => (object)new { type = "budget", id = x.Id, label = x.Name }).ToList();
            return (content, citations, System.Text.Json.JsonSerializer.Serialize(citations));
        }

        var expense = _unitOfWork.Repository<Transaction>().Query().Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);
        var income = _unitOfWork.Repository<Transaction>().Query().Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
        var generic = $"Tổng quan nhanh: thu {income:n0} VND, chi {expense:n0} VND. Tôi chỉ trả lời dựa trên dữ liệu nội bộ đã có trong hệ thống.";
        return (generic, [], "[]");
    }
}
