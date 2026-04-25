using System.Text.Json;
using OmniBizAI.Application.Common;
using OmniBizAI.Application.DTOs;
using OmniBizAI.Application.Interfaces;
using OmniBizAI.Domain.Common;
using OmniBizAI.Domain.Entities.Finance;
using OmniBizAI.Domain.Entities.Organization;
using OmniBizAI.Domain.Enums;
using OmniBizAI.Domain.Interfaces;

namespace OmniBizAI.Application.Services;

public sealed class FinanceService : IFinanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiCopilotService _aiCopilotService;
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;

    public FinanceService(
        IUnitOfWork unitOfWork,
        IAiCopilotService aiCopilotService,
        IWorkflowService workflowService,
        ICurrentUserService currentUserService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _aiCopilotService = aiCopilotService;
        _workflowService = workflowService;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
    }

    public Task<PagedResult<BudgetDto>> GetBudgetsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<Budget>().Query().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => x.Name.Contains(request.Search));
        }

        return Task.FromResult(PagedResult<BudgetDto>.Create(query.OrderByDescending(x => x.CreatedAt).Select(MapBudget), request));
    }

    public async Task<BudgetDto> CreateBudgetAsync(CreateBudgetRequest request, CancellationToken cancellationToken = default)
    {
        if (request.AllocatedAmount <= 0)
        {
            throw new BusinessRuleException("Allocated amount must be greater than zero.");
        }

        EnsureExists<Department>(request.DepartmentId, "Department not found.");
        EnsureExists<BudgetCategory>(request.CategoryId, "Budget category not found.");
        EnsureExists<FiscalPeriod>(request.FiscalPeriodId, "Fiscal period not found.");

        var budget = new Budget
        {
            CompanyId = GetCompanyId(),
            Name = request.Name.Trim(),
            DepartmentId = request.DepartmentId,
            CategoryId = request.CategoryId,
            FiscalPeriodId = request.FiscalPeriodId,
            AllocatedAmount = request.AllocatedAmount,
            Notes = request.Notes
        };

        await _unitOfWork.Repository<Budget>().AddAsync(budget, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapBudget(budget);
    }

    public async Task<BudgetCategoryDto> CreateCategoryAsync(CreateBudgetCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            throw new BusinessRuleException("Category name and code are required.");
        }

        var category = new BudgetCategory
        {
            CompanyId = GetCompanyId(),
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpperInvariant(),
            Type = request.Type,
            ParentId = request.ParentId,
            Color = request.Color
        };

        await _unitOfWork.Repository<BudgetCategory>().AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapCategory(category);
    }

    public async Task<VendorDto> CreateVendorAsync(CreateVendorRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BusinessRuleException("Vendor name is required.");
        }

        if (!string.IsNullOrWhiteSpace(request.TaxCode) && _unitOfWork.Repository<Vendor>().Query().Any(x => x.TaxCode == request.TaxCode && !x.IsDeleted))
        {
            throw new BusinessRuleException("Vendor tax code already exists.");
        }

        var vendor = new Vendor
        {
            CompanyId = GetCompanyId(),
            Name = request.Name.Trim(),
            TaxCode = request.TaxCode,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            BankAccount = request.BankAccount
        };

        await _unitOfWork.Repository<Vendor>().AddAsync(vendor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapVendor(vendor);
    }

    public async Task<WalletDto> CreateWalletAsync(CreateWalletRequest request, CancellationToken cancellationToken = default)
    {
        var wallet = new Wallet
        {
            CompanyId = GetCompanyId(),
            Name = request.Name.Trim(),
            Type = request.Type,
            Balance = request.OpeningBalance,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "VND" : request.Currency,
            BankName = request.BankName,
            AccountNumber = request.AccountNumber
        };

        await _unitOfWork.Repository<Wallet>().AddAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapWallet(wallet);
    }

    public async Task<PaymentRequestDto> CreatePaymentRequestAsync(CreatePaymentRequestRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new BusinessRuleException("Payment request must contain at least one line item.");
        }

        var requester = await _unitOfWork.Repository<Employee>().GetByIdAsync(request.RequesterId, cancellationToken)
            ?? _unitOfWork.Repository<Employee>().Query().FirstOrDefault(x => x.UserId == request.RequesterId)
            ?? throw new NotFoundException("Requester employee not found.");
        var departmentId = request.DepartmentId == Guid.Empty
            ? requester.DepartmentId ?? throw new BusinessRuleException("Requester has no department.")
            : request.DepartmentId;
        var categoryId = request.CategoryId == Guid.Empty
            ? _unitOfWork.Repository<BudgetCategory>().Query().Where(x => x.Type == TransactionType.Expense && x.IsActive).Select(x => x.Id).FirstOrDefault()
            : request.CategoryId;
        if (categoryId == Guid.Empty)
        {
            throw new BusinessRuleException("No active expense category found.");
        }
        var budgetId = request.BudgetId
            ?? _unitOfWork.Repository<Budget>().Query().Where(x => x.DepartmentId == departmentId && x.CategoryId == categoryId && x.Status == BudgetStatus.Active).Select(x => (Guid?)x.Id).FirstOrDefault();

        var items = request.Items.Select((item, index) =>
        {
            if (item.Quantity <= 0 || item.UnitPrice < 0)
            {
                throw new BusinessRuleException("Payment request item quantity and unit price are invalid.");
            }

            return new PaymentRequestItem
            {
                Description = item.Description.Trim(),
                Quantity = item.Quantity,
                Unit = item.Unit,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice,
                SortOrder = index
            };
        }).ToList();

        var number = $"PR-{DateTime.UtcNow:yyyy}-{_unitOfWork.Repository<PaymentRequest>().Query().Count() + 1:0000}";
        var paymentRequest = new PaymentRequest
        {
            CompanyId = GetCompanyId(),
            RequestNumber = number,
            Title = request.Title.Trim(),
            Description = request.Description,
            DepartmentId = departmentId,
            RequesterId = requester.Id,
            VendorId = request.VendorId,
            BudgetId = budgetId,
            CategoryId = categoryId,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "VND" : request.Currency,
            PaymentMethod = request.PaymentMethod,
            PaymentDueDate = request.PaymentDueDate,
            Priority = request.Priority,
            TotalAmount = items.Sum(x => x.TotalPrice),
            Items = items
        };

        await _unitOfWork.Repository<PaymentRequest>().AddAsync(paymentRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapPaymentRequest(paymentRequest);
    }

    public async Task<PaymentRequestDto> SubmitPaymentRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var paymentRequest = await _unitOfWork.Repository<PaymentRequest>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Payment request not found.");
        if (paymentRequest.Status != PaymentRequestStatus.Draft)
        {
            throw new BusinessRuleException("Only draft payment requests can be submitted.");
        }

        var risk = await _aiCopilotService.AnalyzeRiskAsync(new RiskAnalysisRequest("PaymentRequest", id), cancellationToken);
        paymentRequest.AiRiskScore = risk.RiskScore;
        paymentRequest.AiRiskFlagsJson = JsonSerializer.Serialize(risk.RiskFactors);
        paymentRequest.Status = PaymentRequestStatus.PendingApproval;
        paymentRequest.SubmittedAt = DateTime.UtcNow;

        await _workflowService.EnsureDefaultPaymentWorkflowAsync(paymentRequest.CompanyId, cancellationToken);
        await _workflowService.StartPaymentRequestWorkflowAsync(id, paymentRequest.CompanyId, _currentUserService.UserId, cancellationToken);
        await _notificationService.NotifyRolesAsync(
            ["Manager", "Director"],
            new CreateNotificationRequest(
                "Có đề nghị thanh toán cần duyệt",
                $"{paymentRequest.RequestNumber} - {paymentRequest.Title} đang chờ phê duyệt.",
                "ApprovalRequest",
                "High",
                "PaymentRequest",
                paymentRequest.Id,
                "/workflow/approvals"),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapPaymentRequest(paymentRequest);
    }

    public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new BusinessRuleException("Transaction amount must be greater than zero.");
        }

        var wallet = await _unitOfWork.Repository<Wallet>().GetByIdAsync(request.WalletId, cancellationToken)
            ?? throw new NotFoundException("Wallet not found.");
        var budget = request.BudgetId.HasValue ? await _unitOfWork.Repository<Budget>().GetByIdAsync(request.BudgetId.Value, cancellationToken) : null;

        var transaction = new Transaction
        {
            CompanyId = GetCompanyId(),
            TransactionNumber = $"TXN-{DateTime.UtcNow:yyyy}-{_unitOfWork.Repository<Transaction>().Query().Count() + 1:0000}",
            Type = request.Type,
            Amount = request.Amount,
            WalletId = request.WalletId,
            DepartmentId = request.DepartmentId,
            CategoryId = request.CategoryId,
            BudgetId = request.BudgetId,
            PaymentRequestId = request.PaymentRequestId,
            VendorId = request.VendorId,
            TransactionDate = request.TransactionDate,
            ReferenceNumber = request.ReferenceNumber,
            Description = request.Description,
            RecordedBy = _currentUserService.UserId
        };

        if (request.Type == TransactionType.Expense)
        {
            wallet.Balance -= request.Amount;
            if (budget is not null)
            {
                budget.SpentAmount += request.Amount;
            }
        }
        else
        {
            wallet.Balance += request.Amount;
        }

        await _unitOfWork.Repository<Transaction>().AddAsync(transaction, cancellationToken);
        if (request.PaymentRequestId.HasValue)
        {
            var paymentRequest = await _unitOfWork.Repository<PaymentRequest>().GetByIdAsync(request.PaymentRequestId.Value, cancellationToken);
            if (paymentRequest is not null && request.Type == TransactionType.Expense)
            {
                paymentRequest.Status = PaymentRequestStatus.Paid;
                paymentRequest.PaidAt = DateTime.UtcNow;
            }
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapTransaction(transaction);
    }

    private Guid GetCompanyId()
    {
        return _unitOfWork.Repository<Company>().Query().Select(x => x.Id).FirstOrDefault() is { } id && id != Guid.Empty
            ? id
            : throw new BusinessRuleException("Company seed data is missing.");
    }

    private void EnsureExists<TEntity>(Guid id, string message) where TEntity : BaseEntity
    {
        if (!_unitOfWork.Repository<TEntity>().Query().Any(x => x.Id == id))
        {
            throw new NotFoundException(message);
        }
    }

    private PaymentRequestDto MapPaymentRequest(PaymentRequest request)
    {
        var items = _unitOfWork.Repository<PaymentRequestItem>().Query()
            .Where(x => x.PaymentRequestId == request.Id)
            .OrderBy(x => x.SortOrder)
            .Select(x => new PaymentRequestItemDto(x.Id, x.Description, x.Quantity, x.Unit, x.UnitPrice, x.TotalPrice))
            .ToList();

        if (items.Count == 0 && request.Items.Count > 0)
        {
            items = request.Items.Select(x => new PaymentRequestItemDto(x.Id, x.Description, x.Quantity, x.Unit, x.UnitPrice, x.TotalPrice)).ToList();
        }

        return new PaymentRequestDto(request.Id, request.RequestNumber, request.Title, request.DepartmentId, request.RequesterId, request.VendorId, request.BudgetId, request.CategoryId, request.TotalAmount, request.Currency, request.Status, request.AiRiskScore, items);
    }

    private static BudgetDto MapBudget(Budget budget)
    {
        return new BudgetDto(budget.Id, budget.Name, budget.DepartmentId, budget.CategoryId, budget.FiscalPeriodId, budget.AllocatedAmount, budget.SpentAmount, budget.CommittedAmount, budget.RemainingAmount, budget.UtilizationPercent, budget.WarningLevel, budget.Status);
    }

    private static BudgetCategoryDto MapCategory(BudgetCategory category)
    {
        return new BudgetCategoryDto(category.Id, category.Name, category.Code, category.Type, category.ParentId, category.IsActive);
    }

    private static VendorDto MapVendor(Vendor vendor)
    {
        return new VendorDto(vendor.Id, vendor.Name, vendor.TaxCode, vendor.Email, vendor.Phone, vendor.Rating, vendor.Status);
    }

    private static WalletDto MapWallet(Wallet wallet)
    {
        return new WalletDto(wallet.Id, wallet.Name, wallet.Type, wallet.Balance, wallet.Currency, wallet.IsActive);
    }

    private static TransactionDto MapTransaction(Transaction transaction)
    {
        return new TransactionDto(transaction.Id, transaction.TransactionNumber, transaction.Type, transaction.Amount, transaction.WalletId, transaction.DepartmentId, transaction.CategoryId, transaction.BudgetId, transaction.TransactionDate, transaction.Status);
    }

    public async Task<BudgetDto> GetBudgetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Budget>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return new BudgetDto(e.Id, e.Name, e.DepartmentId, e.CategoryId, e.FiscalPeriodId, e.AllocatedAmount, e.SpentAmount, e.CommittedAmount, e.RemainingAmount, e.UtilizationPercent, e.WarningLevel, e.Status);
    }
    public async Task<BudgetDto> UpdateBudgetAsync(Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Budget>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        e.Name = request.Name; e.AllocatedAmount = request.AllocatedAmount; e.Notes = request.Notes;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return await GetBudgetAsync(id, cancellationToken);
    }
    public async Task DeleteBudgetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Budget>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        _unitOfWork.Repository<Budget>().Remove(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResult<BudgetCategoryDto>> GetCategoriesAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<BudgetCategory>().Query();
        return Task.FromResult(PagedResult<BudgetCategoryDto>.Create(q.Select(x => new BudgetCategoryDto(x.Id, x.Name, x.Code, x.Type, x.ParentId, x.IsActive)), request));
    }
    public async Task<BudgetCategoryDto> GetCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<BudgetCategory>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return new BudgetCategoryDto(x.Id, x.Name, x.Code, x.Type, x.ParentId, x.IsActive);
    }
    public async Task<BudgetCategoryDto> UpdateCategoryAsync(Guid id, UpdateBudgetCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<BudgetCategory>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        e.Name = request.Name; e.Code = request.Code; e.Type = request.Type; e.ParentId = request.ParentId; e.Color = request.Color; e.IsActive = request.IsActive;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return await GetCategoryAsync(id, cancellationToken);
    }
    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<BudgetCategory>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        _unitOfWork.Repository<BudgetCategory>().Remove(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResult<VendorDto>> GetVendorsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<Vendor>().Query();
        return Task.FromResult(PagedResult<VendorDto>.Create(q.Select(x => new VendorDto(x.Id, x.Name, x.TaxCode, x.Email, x.Phone, x.Rating, x.Status)), request));
    }
    public async Task<VendorDto> GetVendorAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<Vendor>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return new VendorDto(x.Id, x.Name, x.TaxCode, x.Email, x.Phone, x.Rating, x.Status);
    }
    public async Task<VendorDto> UpdateVendorAsync(Guid id, UpdateVendorRequest request, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Vendor>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        e.Name = request.Name; e.TaxCode = request.TaxCode; e.Email = request.Email; e.Phone = request.Phone; e.Address = request.Address; e.BankAccount = request.BankAccount; e.Status = request.Status;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return await GetVendorAsync(id, cancellationToken);
    }
    public async Task DeleteVendorAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Vendor>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        _unitOfWork.Repository<Vendor>().Remove(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResult<WalletDto>> GetWalletsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<Wallet>().Query();
        return Task.FromResult(PagedResult<WalletDto>.Create(q.Select(x => new WalletDto(x.Id, x.Name, x.Type, x.Balance, x.Currency, x.IsActive)), request));
    }
    public async Task<WalletDto> GetWalletAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<Wallet>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return new WalletDto(x.Id, x.Name, x.Type, x.Balance, x.Currency, x.IsActive);
    }
    public async Task<WalletDto> UpdateWalletAsync(Guid id, UpdateWalletRequest request, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Wallet>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        e.Name = request.Name; e.Type = request.Type; e.IsActive = request.IsActive;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return await GetWalletAsync(id, cancellationToken);
    }
    public async Task DeleteWalletAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Wallet>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        _unitOfWork.Repository<Wallet>().Remove(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResult<PaymentRequestDto>> GetPaymentRequestsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<PaymentRequest>().Query();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            q = q.Where(x => x.RequestNumber.Contains(request.Search) || x.Title.Contains(request.Search));
        }
        return Task.FromResult(PagedResult<PaymentRequestDto>.Create(q.OrderByDescending(x => x.CreatedAt).ToList().Select(MapPaymentRequest), request));
    }
    public async Task<PaymentRequestDto> GetPaymentRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<PaymentRequest>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return MapPaymentRequest(x);
    }
    public async Task<PaymentRequestDto> UpdatePaymentRequestAsync(Guid id, UpdatePaymentRequestRequest request, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<PaymentRequest>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        if(e.Status != PaymentRequestStatus.Draft) throw new BusinessRuleException("Can only update draft requests.");
        if (request.Items.Count == 0) throw new BusinessRuleException("Payment request must contain at least one line item.");
        e.Title = request.Title; e.Description = request.Description; e.DepartmentId = request.DepartmentId; e.VendorId = request.VendorId; e.BudgetId = request.BudgetId; e.CategoryId = request.CategoryId; e.Currency = request.Currency; e.PaymentMethod = request.PaymentMethod; e.PaymentDueDate = request.PaymentDueDate; e.Priority = request.Priority;
        var existingItems = _unitOfWork.Repository<PaymentRequestItem>().Query().Where(x => x.PaymentRequestId == id).ToList();
        foreach (var item in existingItems)
        {
            _unitOfWork.Repository<PaymentRequestItem>().Remove(item);
        }
        var newItems = request.Items.Select((item, index) => new PaymentRequestItem
        {
            PaymentRequestId = id,
            Description = item.Description,
            Quantity = item.Quantity,
            Unit = item.Unit,
            UnitPrice = item.UnitPrice,
            TotalPrice = item.Quantity * item.UnitPrice,
            SortOrder = index
        }).ToList();
        foreach (var item in newItems)
        {
            await _unitOfWork.Repository<PaymentRequestItem>().AddAsync(item, cancellationToken);
        }
        e.TotalAmount = newItems.Sum(x => x.TotalPrice);
        await _unitOfWork.SaveChangesAsync(cancellationToken); return await GetPaymentRequestAsync(id, cancellationToken);
    }
    public async Task DeletePaymentRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<PaymentRequest>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        if(e.Status != PaymentRequestStatus.Draft) throw new BusinessRuleException("Can only delete draft requests.");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<AttachmentDto> AddAttachmentAsync(Guid paymentRequestId, UploadAttachmentRequest request, CancellationToken cancellationToken = default)
    {
        var paymentRequest = await _unitOfWork.Repository<PaymentRequest>().GetByIdAsync(paymentRequestId, cancellationToken)
            ?? throw new NotFoundException("Payment request not found.");
        var attachment = new PaymentRequestAttachment
        {
            PaymentRequestId = paymentRequest.Id,
            FileName = request.FileName,
            FilePath = request.FileUrl,
            FileSize = 0,
            ContentType = "application/octet-stream",
            UploadedBy = _currentUserService.UserId
        };
        await _unitOfWork.Repository<PaymentRequestAttachment>().AddAsync(attachment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new AttachmentDto(attachment.Id, attachment.FileName, attachment.FilePath);
    }
    public async Task DeleteAttachmentAsync(Guid paymentRequestId, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await _unitOfWork.Repository<PaymentRequestAttachment>().GetByIdAsync(attachmentId, cancellationToken)
            ?? throw new NotFoundException("Attachment not found.");
        if (attachment.PaymentRequestId != paymentRequestId)
        {
            throw new BusinessRuleException("Attachment does not belong to this payment request.");
        }
        _unitOfWork.Repository<PaymentRequestAttachment>().Remove(attachment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResult<TransactionDto>> GetTransactionsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<Transaction>().Query();
        return Task.FromResult(PagedResult<TransactionDto>.Create(q.Select(x => new TransactionDto(x.Id, x.TransactionNumber, x.Type, x.Amount, x.WalletId, x.DepartmentId, x.CategoryId, x.BudgetId, x.TransactionDate, x.Status)), request));
    }
    public async Task<TransactionDto> GetTransactionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<Transaction>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return new TransactionDto(x.Id, x.TransactionNumber, x.Type, x.Amount, x.WalletId, x.DepartmentId, x.CategoryId, x.BudgetId, x.TransactionDate, x.Status);
    }
    public async Task<TransactionDto> UpdateTransactionAsync(Guid id, CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<Transaction>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        await ReverseTransactionImpactAsync(x, cancellationToken);
        x.Type = request.Type; x.Amount = request.Amount; x.WalletId = request.WalletId; x.DepartmentId = request.DepartmentId; x.CategoryId = request.CategoryId; x.BudgetId = request.BudgetId; x.PaymentRequestId = request.PaymentRequestId; x.VendorId = request.VendorId; x.TransactionDate = request.TransactionDate; x.ReferenceNumber = request.ReferenceNumber; x.Description = request.Description;
        await ApplyTransactionImpactAsync(x, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken); return await GetTransactionAsync(id, cancellationToken);
    }
    public async Task DeleteTransactionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Transaction>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        await ReverseTransactionImpactAsync(e, cancellationToken);
        e.Status = "Reversed"; e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyTransactionImpactAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        var wallet = await _unitOfWork.Repository<Wallet>().GetByIdAsync(transaction.WalletId, cancellationToken)
            ?? throw new NotFoundException("Wallet not found.");
        if (transaction.Type == TransactionType.Expense)
        {
            wallet.Balance -= transaction.Amount;
            if (transaction.BudgetId.HasValue)
            {
                var budget = await _unitOfWork.Repository<Budget>().GetByIdAsync(transaction.BudgetId.Value, cancellationToken);
                if (budget is not null) budget.SpentAmount += transaction.Amount;
            }
        }
        else
        {
            wallet.Balance += transaction.Amount;
        }
    }

    private async Task ReverseTransactionImpactAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        if (transaction.Status == "Reversed")
        {
            return;
        }

        var wallet = await _unitOfWork.Repository<Wallet>().GetByIdAsync(transaction.WalletId, cancellationToken);
        if (wallet is not null)
        {
            if (transaction.Type == TransactionType.Expense)
            {
                wallet.Balance += transaction.Amount;
            }
            else
            {
                wallet.Balance -= transaction.Amount;
            }
        }

        if (transaction.Type == TransactionType.Expense && transaction.BudgetId.HasValue)
        {
            var budget = await _unitOfWork.Repository<Budget>().GetByIdAsync(transaction.BudgetId.Value, cancellationToken);
            if (budget is not null)
            {
                budget.SpentAmount = Math.Max(0, budget.SpentAmount - transaction.Amount);
            }
        }
    }

}
