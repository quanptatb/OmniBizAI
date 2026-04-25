using OmniBizAI.Domain.Enums;

namespace OmniBizAI.Application.DTOs;

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record AuthUserDto(Guid Id, string Email, string FullName, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions, Guid? DepartmentId);
public sealed record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, AuthUserDto User);

public sealed record DepartmentDto(Guid Id, string Name, string Code, Guid? ParentDepartmentId, Guid? ManagerId, decimal BudgetLimit, bool IsActive);
public sealed record CreateDepartmentRequest(string Name, string Code, Guid? ParentDepartmentId, Guid? ManagerId, decimal BudgetLimit);
public sealed record EmployeeDto(Guid Id, Guid? UserId, string EmployeeCode, string FullName, string Email, Guid? DepartmentId, Guid? PositionId, Guid? ManagerId, string Status);
public sealed record CreateEmployeeRequest(string FullName, string Email, string? Phone, Guid? DepartmentId, Guid? PositionId, Guid? ManagerId, DateOnly JoinDate);
public sealed record UpdateDepartmentRequest(string Name, string Code, Guid? ParentDepartmentId, Guid? ManagerId, decimal BudgetLimit);
public sealed record UpdateEmployeeRequest(string FullName, string? Phone, Guid? DepartmentId, Guid? PositionId, Guid? ManagerId);
public sealed record UpdateEmployeeStatusRequest(string Status);
public sealed record PositionDto(Guid Id, string Name, int Level, Guid? DepartmentId, string? Description, bool IsActive);
public sealed record CreatePositionRequest(string Name, int Level, Guid? DepartmentId, string? Description);
public sealed record UpdatePositionRequest(string Name, int Level, Guid? DepartmentId, string? Description, bool IsActive);

public sealed record BudgetDto(Guid Id, string Name, Guid DepartmentId, Guid CategoryId, Guid FiscalPeriodId, decimal AllocatedAmount, decimal SpentAmount, decimal CommittedAmount, decimal RemainingAmount, decimal UtilizationPercent, string WarningLevel, BudgetStatus Status);
public sealed record CreateBudgetRequest(string Name, Guid DepartmentId, Guid CategoryId, Guid FiscalPeriodId, decimal AllocatedAmount, string? Notes);
public sealed record BudgetCategoryDto(Guid Id, string Name, string Code, TransactionType Type, Guid? ParentId, bool IsActive);
public sealed record CreateBudgetCategoryRequest(string Name, string Code, TransactionType Type, Guid? ParentId, string? Color);
public sealed record VendorDto(Guid Id, string Name, string? TaxCode, string? Email, string? Phone, decimal? Rating, string Status);
public sealed record CreateVendorRequest(string Name, string? TaxCode, string? Email, string? Phone, string? Address, string? BankAccount);
public sealed record WalletDto(Guid Id, string Name, string Type, decimal Balance, string Currency, bool IsActive);
public sealed record CreateWalletRequest(string Name, string Type, decimal OpeningBalance, string Currency, string? BankName, string? AccountNumber);
public sealed record PaymentRequestItemDto(Guid? Id, string Description, decimal Quantity, string? Unit, decimal UnitPrice, decimal TotalPrice);
public sealed record PaymentRequestDto(Guid Id, string RequestNumber, string Title, Guid DepartmentId, Guid RequesterId, Guid? VendorId, Guid? BudgetId, Guid CategoryId, decimal TotalAmount, string Currency, PaymentRequestStatus Status, decimal? AiRiskScore, IReadOnlyCollection<PaymentRequestItemDto> Items);
public sealed record CreatePaymentRequestRequest(string Title, string? Description, Guid DepartmentId, Guid RequesterId, Guid? VendorId, Guid? BudgetId, Guid CategoryId, string Currency, string? PaymentMethod, DateOnly? PaymentDueDate, string Priority, IReadOnlyCollection<PaymentRequestItemDto> Items);
public sealed record TransactionDto(Guid Id, string TransactionNumber, TransactionType Type, decimal Amount, Guid WalletId, Guid DepartmentId, Guid CategoryId, Guid? BudgetId, DateOnly TransactionDate, string Status);
public sealed record CreateTransactionRequest(TransactionType Type, decimal Amount, Guid WalletId, Guid DepartmentId, Guid CategoryId, Guid? BudgetId, Guid? PaymentRequestId, Guid? VendorId, DateOnly TransactionDate, string? ReferenceNumber, string? Description);
public sealed record UpdateBudgetRequest(string Name, decimal AllocatedAmount, string? Notes);
public sealed record UpdateBudgetCategoryRequest(string Name, string Code, TransactionType Type, Guid? ParentId, string? Color, bool IsActive);
public sealed record UpdateVendorRequest(string Name, string? TaxCode, string? Email, string? Phone, string? Address, string? BankAccount, string Status);
public sealed record UpdateWalletRequest(string Name, string Type, bool IsActive);
public sealed record UpdatePaymentRequestRequest(string Title, string? Description, Guid DepartmentId, Guid? VendorId, Guid? BudgetId, Guid CategoryId, string Currency, string? PaymentMethod, DateOnly? PaymentDueDate, string Priority, IReadOnlyCollection<PaymentRequestItemDto> Items);
public sealed record UploadAttachmentRequest(string FileName, string FileUrl);
public sealed record AttachmentDto(Guid Id, string FileName, string FileUrl);

public sealed record EvaluationPeriodDto(Guid Id, string Name, string Type, DateOnly StartDate, DateOnly EndDate, string Status);
public sealed record CreateEvaluationPeriodRequest(string Name, string Type, DateOnly StartDate, DateOnly EndDate);
public sealed record ObjectiveDto(Guid Id, string Title, Guid PeriodId, OwnerType OwnerType, Guid? DepartmentId, Guid? OwnerId, decimal Progress, string Status);
public sealed record CreateObjectiveRequest(string Title, string? Description, Guid PeriodId, Guid? ParentId, OwnerType OwnerType, Guid? DepartmentId, Guid? OwnerId, DateOnly? DueDate);
public sealed record KeyResultDto(Guid Id, Guid ObjectiveId, string Title, MetricType MetricType, decimal StartValue, decimal TargetValue, decimal CurrentValue, decimal Progress, decimal Weight);
public sealed record CreateKeyResultRequest(Guid ObjectiveId, string Title, MetricType MetricType, string? Unit, decimal StartValue, decimal TargetValue, decimal CurrentValue, decimal Weight, ProgressDirection Direction, Guid? AssigneeId);
public sealed record KpiDto(Guid Id, string Name, Guid PeriodId, Guid? DepartmentId, Guid? AssigneeId, MetricType MetricType, decimal TargetValue, decimal CurrentValue, decimal Progress, decimal Weight, string? Rating, string Status);
public sealed record CreateKpiRequest(string Name, string? Description, Guid PeriodId, Guid? DepartmentId, Guid? AssigneeId, MetricType MetricType, string? Unit, decimal StartValue, decimal TargetValue, decimal CurrentValue, decimal Weight, string Frequency, ProgressDirection Direction);
public sealed record KpiCheckInDto(Guid Id, Guid KpiId, DateOnly CheckInDate, decimal? PreviousValue, decimal NewValue, decimal? Progress, string Note, CheckInStatus Status, string? ReviewComment);
public sealed record CreateKpiCheckInRequest(Guid KpiId, DateOnly CheckInDate, decimal NewValue, string Note);
public sealed record ReviewCheckInRequest(string? Comment);
public sealed record UpdateEvaluationPeriodRequest(string Name, string Type, DateOnly StartDate, DateOnly EndDate, string Status);
public sealed record UpdateObjectiveRequest(string Title, string? Description, Guid? ParentId, OwnerType OwnerType, Guid? DepartmentId, Guid? OwnerId, DateOnly? DueDate, string Status);
public sealed record UpdateKeyResultRequest(string Title, MetricType MetricType, string? Unit, decimal TargetValue, decimal Weight, ProgressDirection Direction, Guid? AssigneeId);
public sealed record UpdateKpiRequest(string Name, string? Description, Guid? DepartmentId, Guid? AssigneeId, MetricType MetricType, string? Unit, decimal TargetValue, decimal Weight, string Frequency, ProgressDirection Direction, string Status);
public sealed record KpiScorecardDto(Guid EmployeeId, string EmployeeName, decimal OverallScore, IReadOnlyCollection<ObjectiveDto> Objectives, IReadOnlyCollection<KpiDto> Kpis);

public sealed record WorkflowTemplateDto(Guid Id, string Name, string EntityType, bool IsActive, bool IsDefault, int StepCount);
public sealed record ApprovalQueueItemDto(Guid InstanceId, string EntityType, Guid EntityId, int CurrentStepOrder, string Status, DateTime InitiatedAt);
public sealed record ApprovalActionRequest(string? Comment);

public sealed record NotificationDto(Guid Id, string Title, string Message, string Type, string Priority, string? EntityType, Guid? EntityId, string? ActionUrl, bool IsRead, DateTime CreatedAt);
public sealed record CreateNotificationRequest(string Title, string Message, string Type, string Priority, string? EntityType, Guid? EntityId, string? ActionUrl);

public sealed record AiChatRequest(Guid? SessionId, string Message, string? ContextType);
public sealed record AiChatResponse(Guid SessionId, string Content, IReadOnlyCollection<object> Citations);
public sealed record RiskAnalysisRequest(string EntityType, Guid EntityId);
public sealed record RiskAnalysisResponse(decimal RiskScore, string RiskLevel, IReadOnlyCollection<string> RiskFactors, IReadOnlyCollection<string> Recommendations);

public sealed record DashboardOverviewDto(decimal TotalIncome, decimal TotalExpense, decimal RemainingBudget, decimal AverageKpiProgress, int PendingApprovals, IReadOnlyCollection<string> RiskAlerts);
