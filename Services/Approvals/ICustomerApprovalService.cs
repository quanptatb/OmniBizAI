// PR #23 — transaction boundary
namespace OmniBizAI.Services.Approvals;

using OmniBizAI.Services.Dtos;

/// <summary>
/// Customer approval via email token link.
/// </summary>
public interface ICustomerApprovalService
{
    Task SendAsync(Guid menuPlanId, Guid customerContactId, string actorUserId,
        CancellationToken ct = default);

    Task<CustomerApprovalReviewViewModel> GetReviewAsync(string token,
        CancellationToken ct = default);

    Task SubmitAsync(CustomerApprovalSubmitRequest request,
        CancellationToken ct = default);
}
