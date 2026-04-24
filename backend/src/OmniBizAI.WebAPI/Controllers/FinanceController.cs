using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Application.Common;
using OmniBizAI.Application.DTOs;
using OmniBizAI.Application.Interfaces;

namespace OmniBizAI.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class FinanceController : ControllerBase
{
    private readonly IFinanceService _financeService;

    public FinanceController(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    [HttpGet("budgets")]
    public async Task<ActionResult<ApiResponse<PagedResult<BudgetDto>>>> GetBudgets([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResult<BudgetDto>>.Ok(await _financeService.GetBudgetsAsync(request, cancellationToken)));
    }

    [HttpPost("budgets")]
    [Authorize(Roles = "Admin,Director,Accountant")]
    public async Task<ActionResult<ApiResponse<BudgetDto>>> CreateBudget(CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        var created = await _financeService.CreateBudgetAsync(request, cancellationToken);
        return Created($"/api/v1/budgets/{created.Id}", ApiResponse<BudgetDto>.Ok(created, "Budget created"));
    }

    [HttpPost("budget-categories")]
    [Authorize(Roles = "Admin,Director,Accountant")]
    public async Task<ActionResult<ApiResponse<BudgetCategoryDto>>> CreateCategory(CreateBudgetCategoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<BudgetCategoryDto>.Ok(await _financeService.CreateCategoryAsync(request, cancellationToken), "Category created"));
    }

    [HttpPost("vendors")]
    [Authorize(Roles = "Admin,Director,Accountant")]
    public async Task<ActionResult<ApiResponse<VendorDto>>> CreateVendor(CreateVendorRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<VendorDto>.Ok(await _financeService.CreateVendorAsync(request, cancellationToken), "Vendor created"));
    }

    [HttpPost("wallets")]
    [Authorize(Roles = "Admin,Director,Accountant")]
    public async Task<ActionResult<ApiResponse<WalletDto>>> CreateWallet(CreateWalletRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<WalletDto>.Ok(await _financeService.CreateWalletAsync(request, cancellationToken), "Wallet created"));
    }

    [HttpPost("payment-requests")]
    public async Task<ActionResult<ApiResponse<PaymentRequestDto>>> CreatePaymentRequest(CreatePaymentRequestRequest request, CancellationToken cancellationToken)
    {
        var created = await _financeService.CreatePaymentRequestAsync(request, cancellationToken);
        return Created($"/api/v1/payment-requests/{created.Id}", ApiResponse<PaymentRequestDto>.Ok(created, "Payment request created"));
    }

    [HttpPost("payment-requests/{id:guid}/submit")]
    public async Task<ActionResult<ApiResponse<PaymentRequestDto>>> SubmitPaymentRequest(Guid id, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PaymentRequestDto>.Ok(await _financeService.SubmitPaymentRequestAsync(id, cancellationToken), "Payment request submitted"));
    }

    [HttpPost("transactions")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> CreateTransaction(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var created = await _financeService.CreateTransactionAsync(request, cancellationToken);
        return Created($"/api/v1/transactions/{created.Id}", ApiResponse<TransactionDto>.Ok(created, "Transaction created"));
    }
}
