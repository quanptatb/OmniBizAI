using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public interface IPaymentRequestService
{
    Task<IReadOnlyList<PaymentRequestListItemDto>> GetListAsync(PaymentRequestFilter filter, string userId, CancellationToken cancellationToken = default);
    Task<PaymentRequestDetailDto> GetDetailAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<PaymentRequestDetailDto> CreateDraftAsync(CreatePaymentRequestRequest request, string userId, CancellationToken cancellationToken = default);
    Task<PaymentRequestDetailDto> UpdateDraftAsync(Guid id, UpdatePaymentRequestRequest request, string userId, CancellationToken cancellationToken = default);
    Task<PaymentRequestLookupsDto> GetLookupsAsync(CancellationToken cancellationToken = default);
}
