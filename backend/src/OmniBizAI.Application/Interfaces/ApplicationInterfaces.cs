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
    Task<DepartmentDto> GetDepartmentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentDto> UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task DeleteDepartmentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DepartmentDto>> GetDepartmentTreeAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<EmployeeDto>> GetDepartmentEmployeesAsync(Guid departmentId, PagedRequest request, CancellationToken cancellationToken = default);
    
    Task<PagedResult<EmployeeDto>> GetEmployeesAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> GetEmployeeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> UpdateEmployeeAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task DeleteEmployeeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmployeeDto> UpdateEmployeeStatusAsync(Guid id, UpdateEmployeeStatusRequest request, CancellationToken cancellationToken = default);
    
    Task<PagedResult<PositionDto>> GetPositionsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<PositionDto> GetPositionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PositionDto> CreatePositionAsync(CreatePositionRequest request, CancellationToken cancellationToken = default);
    Task<PositionDto> UpdatePositionAsync(Guid id, UpdatePositionRequest request, CancellationToken cancellationToken = default);
    Task DeletePositionAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IFinanceService
{
    Task<PagedResult<BudgetDto>> GetBudgetsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<BudgetDto> GetBudgetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BudgetDto> CreateBudgetAsync(CreateBudgetRequest request, CancellationToken cancellationToken = default);
    Task<BudgetDto> UpdateBudgetAsync(Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken = default);
    Task DeleteBudgetAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<PagedResult<BudgetCategoryDto>> GetCategoriesAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<BudgetCategoryDto> GetCategoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BudgetCategoryDto> CreateCategoryAsync(CreateBudgetCategoryRequest request, CancellationToken cancellationToken = default);
    Task<BudgetCategoryDto> UpdateCategoryAsync(Guid id, UpdateBudgetCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<PagedResult<VendorDto>> GetVendorsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<VendorDto> GetVendorAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VendorDto> CreateVendorAsync(CreateVendorRequest request, CancellationToken cancellationToken = default);
    Task<VendorDto> UpdateVendorAsync(Guid id, UpdateVendorRequest request, CancellationToken cancellationToken = default);
    Task DeleteVendorAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<PagedResult<WalletDto>> GetWalletsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<WalletDto> GetWalletAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WalletDto> CreateWalletAsync(CreateWalletRequest request, CancellationToken cancellationToken = default);
    Task<WalletDto> UpdateWalletAsync(Guid id, UpdateWalletRequest request, CancellationToken cancellationToken = default);
    Task DeleteWalletAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<PagedResult<PaymentRequestDto>> GetPaymentRequestsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<PaymentRequestDto> GetPaymentRequestAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentRequestDto> CreatePaymentRequestAsync(CreatePaymentRequestRequest request, CancellationToken cancellationToken = default);
    Task<PaymentRequestDto> UpdatePaymentRequestAsync(Guid id, UpdatePaymentRequestRequest request, CancellationToken cancellationToken = default);
    Task DeletePaymentRequestAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentRequestDto> SubmitPaymentRequestAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AttachmentDto> AddAttachmentAsync(Guid paymentRequestId, UploadAttachmentRequest request, CancellationToken cancellationToken = default);
    Task DeleteAttachmentAsync(Guid paymentRequestId, Guid attachmentId, CancellationToken cancellationToken = default);
    
    Task<PagedResult<TransactionDto>> GetTransactionsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<TransactionDto> GetTransactionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default);
    Task<TransactionDto> UpdateTransactionAsync(Guid id, CreateTransactionRequest request, CancellationToken cancellationToken = default);
    Task DeleteTransactionAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IPerformanceService
{
    Task<PagedResult<EvaluationPeriodDto>> GetPeriodsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<EvaluationPeriodDto> GetPeriodAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EvaluationPeriodDto> CreatePeriodAsync(CreateEvaluationPeriodRequest request, CancellationToken cancellationToken = default);
    Task<EvaluationPeriodDto> UpdatePeriodAsync(Guid id, UpdateEvaluationPeriodRequest request, CancellationToken cancellationToken = default);
    
    Task<PagedResult<ObjectiveDto>> GetObjectivesAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<ObjectiveDto> GetObjectiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ObjectiveDto>> GetObjectiveTreeAsync(CancellationToken cancellationToken = default);
    Task<ObjectiveDto> CreateObjectiveAsync(CreateObjectiveRequest request, CancellationToken cancellationToken = default);
    Task<ObjectiveDto> UpdateObjectiveAsync(Guid id, UpdateObjectiveRequest request, CancellationToken cancellationToken = default);
    Task DeleteObjectiveAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<PagedResult<KeyResultDto>> GetKeyResultsAsync(Guid? objectiveId, PagedRequest request, CancellationToken cancellationToken = default);
    Task<KeyResultDto> CreateKeyResultAsync(CreateKeyResultRequest request, CancellationToken cancellationToken = default);
    Task<KeyResultDto> UpdateKeyResultAsync(Guid id, UpdateKeyResultRequest request, CancellationToken cancellationToken = default);
    Task DeleteKeyResultAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<PagedResult<KpiDto>> GetKpisAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<KpiDto> GetKpiAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KpiDto> CreateKpiAsync(CreateKpiRequest request, CancellationToken cancellationToken = default);
    Task<KpiDto> UpdateKpiAsync(Guid id, UpdateKpiRequest request, CancellationToken cancellationToken = default);
    Task DeleteKpiAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<PagedResult<KpiCheckInDto>> GetCheckInsAsync(Guid? kpiId, string? status, PagedRequest request, CancellationToken cancellationToken = default);
    Task<KpiCheckInDto> CreateCheckInAsync(CreateKpiCheckInRequest request, CancellationToken cancellationToken = default);
    Task<KpiCheckInDto> ApproveCheckInAsync(Guid id, ReviewCheckInRequest request, CancellationToken cancellationToken = default);
    Task<KpiCheckInDto> RejectCheckInAsync(Guid id, ReviewCheckInRequest request, CancellationToken cancellationToken = default);
    
    Task<KpiScorecardDto> GetScorecardAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

public interface IWorkflowService
{
    Task<WorkflowTemplateDto> EnsureDefaultPaymentWorkflowAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Guid> StartPaymentRequestWorkflowAsync(Guid paymentRequestId, Guid companyId, Guid? initiatedBy, CancellationToken cancellationToken = default);
    Task<PagedResult<ApprovalQueueItemDto>> GetApprovalQueueAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task ApproveAsync(Guid instanceId, ApprovalActionRequest request, CancellationToken cancellationToken = default);
    Task RejectAsync(Guid instanceId, ApprovalActionRequest request, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);
    Task NotifyUserAsync(Guid userId, CreateNotificationRequest request, CancellationToken cancellationToken = default);
    Task NotifyRolesAsync(IReadOnlyCollection<string> roleNames, CreateNotificationRequest request, CancellationToken cancellationToken = default);
}

public interface IPaymentRequestAccountingService
{
    Task CreateApprovedTransactionAsync(Guid paymentRequestId, Guid? recordedBy, CancellationToken cancellationToken = default);
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
