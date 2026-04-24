using OmniBizAI.Application.Common;
using OmniBizAI.Application.DTOs;

namespace OmniBizAI.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
}

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default);
}

public interface IOrganizationService
{
    Task<PagedResult<DepartmentDto>> GetDepartmentsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<EmployeeDto>> GetEmployeesAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
}

public interface IFinanceService
{
    Task<PagedResult<BudgetDto>> GetBudgetsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<BudgetDto> CreateBudgetAsync(CreateBudgetRequest request, CancellationToken cancellationToken = default);
    Task<BudgetCategoryDto> CreateCategoryAsync(CreateBudgetCategoryRequest request, CancellationToken cancellationToken = default);
    Task<VendorDto> CreateVendorAsync(CreateVendorRequest request, CancellationToken cancellationToken = default);
    Task<WalletDto> CreateWalletAsync(CreateWalletRequest request, CancellationToken cancellationToken = default);
    Task<PaymentRequestDto> CreatePaymentRequestAsync(CreatePaymentRequestRequest request, CancellationToken cancellationToken = default);
    Task<PaymentRequestDto> SubmitPaymentRequestAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default);
}

public interface IPerformanceService
{
    Task<EvaluationPeriodDto> CreatePeriodAsync(CreateEvaluationPeriodRequest request, CancellationToken cancellationToken = default);
    Task<ObjectiveDto> CreateObjectiveAsync(CreateObjectiveRequest request, CancellationToken cancellationToken = default);
    Task<KeyResultDto> CreateKeyResultAsync(CreateKeyResultRequest request, CancellationToken cancellationToken = default);
    Task<KpiDto> CreateKpiAsync(CreateKpiRequest request, CancellationToken cancellationToken = default);
    Task<KpiCheckInDto> CreateCheckInAsync(CreateKpiCheckInRequest request, CancellationToken cancellationToken = default);
    Task<KpiCheckInDto> ApproveCheckInAsync(Guid id, ReviewCheckInRequest request, CancellationToken cancellationToken = default);
    Task<KpiCheckInDto> RejectCheckInAsync(Guid id, ReviewCheckInRequest request, CancellationToken cancellationToken = default);
}

public interface IWorkflowService
{
    Task<WorkflowTemplateDto> EnsureDefaultPaymentWorkflowAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Guid> StartPaymentRequestWorkflowAsync(Guid paymentRequestId, Guid companyId, Guid? initiatedBy, CancellationToken cancellationToken = default);
    Task<PagedResult<ApprovalQueueItemDto>> GetApprovalQueueAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task ApproveAsync(Guid instanceId, ApprovalActionRequest request, CancellationToken cancellationToken = default);
    Task RejectAsync(Guid instanceId, ApprovalActionRequest request, CancellationToken cancellationToken = default);
}

public interface IAiCopilotService
{
    Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken cancellationToken = default);
    Task<RiskAnalysisResponse> AnalyzeRiskAsync(RiskAnalysisRequest request, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
}

public interface ISeedDataService
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
