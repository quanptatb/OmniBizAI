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

    [HttpGet("budgets/{id}")]
    public async Task<ActionResult<ApiResponse<BudgetDto>>> GetBudget(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<BudgetDto>.Ok(await _financeService.GetBudgetAsync(id, cancellationToken)));

    [HttpPut("budgets/{id}")]
    public async Task<ActionResult<ApiResponse<BudgetDto>>> UpdateBudget(Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<BudgetDto>.Ok(await _financeService.UpdateBudgetAsync(id, request, cancellationToken)));

    [HttpDelete("budgets/{id}")]
    public async Task<ActionResult> DeleteBudget(Guid id, CancellationToken cancellationToken)
    {
        await _financeService.DeleteBudgetAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("budget-categories")]
    public async Task<ActionResult<ApiResponse<PagedResult<BudgetCategoryDto>>>> GetCategories([FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<BudgetCategoryDto>>.Ok(await _financeService.GetCategoriesAsync(request, cancellationToken)));

    [HttpGet("budget-categories/{id}")]
    public async Task<ActionResult<ApiResponse<BudgetCategoryDto>>> GetCategory(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<BudgetCategoryDto>.Ok(await _financeService.GetCategoryAsync(id, cancellationToken)));

    [HttpPut("budget-categories/{id}")]
    public async Task<ActionResult<ApiResponse<BudgetCategoryDto>>> UpdateCategory(Guid id, UpdateBudgetCategoryRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<BudgetCategoryDto>.Ok(await _financeService.UpdateCategoryAsync(id, request, cancellationToken)));

    [HttpDelete("budget-categories/{id}")]
    public async Task<ActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        await _financeService.DeleteCategoryAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("vendors")]
    public async Task<ActionResult<ApiResponse<PagedResult<VendorDto>>>> GetVendors([FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<VendorDto>>.Ok(await _financeService.GetVendorsAsync(request, cancellationToken)));

    [HttpGet("vendors/{id}")]
    public async Task<ActionResult<ApiResponse<VendorDto>>> GetVendor(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<VendorDto>.Ok(await _financeService.GetVendorAsync(id, cancellationToken)));

    [HttpPut("vendors/{id}")]
    public async Task<ActionResult<ApiResponse<VendorDto>>> UpdateVendor(Guid id, UpdateVendorRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<VendorDto>.Ok(await _financeService.UpdateVendorAsync(id, request, cancellationToken)));

    [HttpDelete("vendors/{id}")]
    public async Task<ActionResult> DeleteVendor(Guid id, CancellationToken cancellationToken)
    {
        await _financeService.DeleteVendorAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("wallets")]
    public async Task<ActionResult<ApiResponse<PagedResult<WalletDto>>>> GetWallets([FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<WalletDto>>.Ok(await _financeService.GetWalletsAsync(request, cancellationToken)));

    [HttpGet("wallets/{id}")]
    public async Task<ActionResult<ApiResponse<WalletDto>>> GetWallet(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<WalletDto>.Ok(await _financeService.GetWalletAsync(id, cancellationToken)));

    [HttpPut("wallets/{id}")]
    public async Task<ActionResult<ApiResponse<WalletDto>>> UpdateWallet(Guid id, UpdateWalletRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<WalletDto>.Ok(await _financeService.UpdateWalletAsync(id, request, cancellationToken)));

    [HttpDelete("wallets/{id}")]
    public async Task<ActionResult> DeleteWallet(Guid id, CancellationToken cancellationToken)
    {
        await _financeService.DeleteWalletAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("payment-requests")]
    public async Task<ActionResult<ApiResponse<PagedResult<PaymentRequestDto>>>> GetPaymentRequests([FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<PaymentRequestDto>>.Ok(await _financeService.GetPaymentRequestsAsync(request, cancellationToken)));

    [HttpGet("payment-requests/{id}")]
    public async Task<ActionResult<ApiResponse<PaymentRequestDto>>> GetPaymentRequest(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<PaymentRequestDto>.Ok(await _financeService.GetPaymentRequestAsync(id, cancellationToken)));

    [HttpPut("payment-requests/{id}")]
    public async Task<ActionResult<ApiResponse<PaymentRequestDto>>> UpdatePaymentRequest(Guid id, UpdatePaymentRequestRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PaymentRequestDto>.Ok(await _financeService.UpdatePaymentRequestAsync(id, request, cancellationToken)));

    [HttpDelete("payment-requests/{id}")]
    public async Task<ActionResult> DeletePaymentRequest(Guid id, CancellationToken cancellationToken)
    {
        await _financeService.DeletePaymentRequestAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("payment-requests/{id}/attachments")]
    public async Task<ActionResult<ApiResponse<AttachmentDto>>> AddAttachment(Guid id, UploadAttachmentRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<AttachmentDto>.Ok(await _financeService.AddAttachmentAsync(id, request, cancellationToken)));

    [HttpDelete("payment-requests/{id}/attachments/{fileId}")]
    public async Task<ActionResult> DeleteAttachment(Guid id, Guid fileId, CancellationToken cancellationToken)
    {
        await _financeService.DeleteAttachmentAsync(id, fileId, cancellationToken);
        return NoContent();
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<ApiResponse<PagedResult<TransactionDto>>>> GetTransactions([FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<TransactionDto>>.Ok(await _financeService.GetTransactionsAsync(request, cancellationToken)));

    [HttpGet("transactions/{id}")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> GetTransaction(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<TransactionDto>.Ok(await _financeService.GetTransactionAsync(id, cancellationToken)));

    [HttpPut("transactions/{id}")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> UpdateTransaction(Guid id, CreateTransactionRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<TransactionDto>.Ok(await _financeService.UpdateTransactionAsync(id, request, cancellationToken)));

    [HttpDelete("transactions/{id}")]
    public async Task<ActionResult> DeleteTransaction(Guid id, CancellationToken cancellationToken)
    {
        await _financeService.DeleteTransactionAsync(id, cancellationToken);
        return NoContent();
    }

}
