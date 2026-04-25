using OmniBizAI.Application.Common;
using OmniBizAI.Application.Interfaces;
using OmniBizAI.Domain.Entities.Finance;
using OmniBizAI.Domain.Enums;
using OmniBizAI.Domain.Interfaces;

namespace OmniBizAI.Application.Services;

public sealed class PaymentRequestAccountingService : IPaymentRequestAccountingService
{
    private readonly IUnitOfWork _unitOfWork;

    public PaymentRequestAccountingService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task CreateApprovedTransactionAsync(Guid paymentRequestId, Guid? recordedBy, CancellationToken cancellationToken = default)
    {
        var paymentRequest = await _unitOfWork.Repository<PaymentRequest>().GetByIdAsync(paymentRequestId, cancellationToken)
            ?? throw new NotFoundException("Payment request not found.");

        var transactionExists = _unitOfWork.Repository<Transaction>().Query()
            .Any(x => x.PaymentRequestId == paymentRequest.Id && !x.IsDeleted && x.Status != "Reversed");
        if (transactionExists)
        {
            return;
        }

        var wallet = _unitOfWork.Repository<Wallet>().Query()
            .Where(x => x.CompanyId == paymentRequest.CompanyId && x.IsActive)
            .OrderByDescending(x => x.Balance)
            .FirstOrDefault();
        if (wallet is null)
        {
            throw new BusinessRuleException("No active wallet is available for approved payment transaction.");
        }

        var transaction = new Transaction
        {
            CompanyId = paymentRequest.CompanyId,
            TransactionNumber = $"TXN-{DateTime.UtcNow:yyyy}-{_unitOfWork.Repository<Transaction>().Query().Count() + 1:0000}",
            Type = TransactionType.Expense,
            Amount = paymentRequest.TotalAmount,
            WalletId = wallet.Id,
            DepartmentId = paymentRequest.DepartmentId,
            CategoryId = paymentRequest.CategoryId,
            BudgetId = paymentRequest.BudgetId,
            PaymentRequestId = paymentRequest.Id,
            VendorId = paymentRequest.VendorId,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Description = $"Auto transaction from {paymentRequest.RequestNumber}",
            RecordedBy = recordedBy
        };

        wallet.Balance -= paymentRequest.TotalAmount;
        if (paymentRequest.BudgetId.HasValue)
        {
            var budget = await _unitOfWork.Repository<Budget>().GetByIdAsync(paymentRequest.BudgetId.Value, cancellationToken);
            if (budget is not null)
            {
                budget.SpentAmount += paymentRequest.TotalAmount;
            }
        }

        await _unitOfWork.Repository<Transaction>().AddAsync(transaction, cancellationToken);
    }
}
