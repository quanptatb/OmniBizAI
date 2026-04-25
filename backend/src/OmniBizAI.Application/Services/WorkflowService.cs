using OmniBizAI.Application.Common;
using OmniBizAI.Application.DTOs;
using OmniBizAI.Application.Interfaces;
using OmniBizAI.Domain.Entities.Finance;
using OmniBizAI.Domain.Entities.Identity;
using OmniBizAI.Domain.Entities.Organization;
using OmniBizAI.Domain.Entities.Workflow;
using OmniBizAI.Domain.Enums;
using OmniBizAI.Domain.Interfaces;

namespace OmniBizAI.Application.Services;

public sealed class WorkflowService : IWorkflowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;
    private readonly IPaymentRequestAccountingService _paymentRequestAccountingService;

    public WorkflowService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        INotificationService notificationService,
        IPaymentRequestAccountingService paymentRequestAccountingService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _paymentRequestAccountingService = paymentRequestAccountingService;
    }

    public async Task<WorkflowTemplateDto> EnsureDefaultPaymentWorkflowAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var existing = _unitOfWork.Repository<WorkflowTemplate>().Query().FirstOrDefault(x => x.CompanyId == companyId && x.EntityType == "PaymentRequest" && x.IsDefault);
        if (existing is not null)
        {
            return MapTemplate(existing);
        }

        var managerRoleId = _unitOfWork.Repository<Role>().Query().First(x => x.Name == "Manager").Id;
        var directorRoleId = _unitOfWork.Repository<Role>().Query().First(x => x.Name == "Director").Id;
        var template = new WorkflowTemplate
        {
            CompanyId = companyId,
            Name = "Duyet de nghi chi mac dinh",
            EntityType = "PaymentRequest",
            IsDefault = true,
            IsActive = true,
            Steps =
            {
                new WorkflowStep { StepOrder = 1, Name = "Quan ly phong ban duyet", ApproverType = "Role", ApproverRoleId = managerRoleId, TimeoutHours = 48 },
                new WorkflowStep { StepOrder = 2, Name = "Giam doc duyet", ApproverType = "Role", ApproverRoleId = directorRoleId, TimeoutHours = 72 }
            }
        };

        await _unitOfWork.Repository<WorkflowTemplate>().AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapTemplate(template);
    }

    public async Task<Guid> StartPaymentRequestWorkflowAsync(Guid paymentRequestId, Guid companyId, Guid? initiatedBy, CancellationToken cancellationToken = default)
    {
        var template = _unitOfWork.Repository<WorkflowTemplate>().Query().First(x => x.CompanyId == companyId && x.EntityType == "PaymentRequest" && x.IsDefault);
        var steps = _unitOfWork.Repository<WorkflowStep>().Query().Where(x => x.TemplateId == template.Id).OrderBy(x => x.StepOrder).ToList();
        if (steps.Count == 0)
        {
            steps = template.Steps.OrderBy(x => x.StepOrder).ToList();
        }

        var exists = _unitOfWork.Repository<WorkflowInstance>().Query().Any(x => x.EntityType == "PaymentRequest" && x.EntityId == paymentRequestId && x.Status != WorkflowStatus.Cancelled);
        if (exists)
        {
            return _unitOfWork.Repository<WorkflowInstance>().Query().First(x => x.EntityType == "PaymentRequest" && x.EntityId == paymentRequestId).Id;
        }

        var instance = new WorkflowInstance
        {
            TemplateId = template.Id,
            EntityType = "PaymentRequest",
            EntityId = paymentRequestId,
            TotalSteps = steps.Count,
            Status = WorkflowStatus.InProgress,
            InitiatedBy = initiatedBy,
            Steps = steps.Select(step => new WorkflowInstanceStep
            {
                StepOrder = step.StepOrder,
                StepName = step.Name,
                Status = step.StepOrder == 1 ? "InProgress" : "Pending",
                StartedAt = step.StepOrder == 1 ? DateTime.UtcNow : null,
                DeadlineAt = DateTime.UtcNow.AddHours(step.TimeoutHours)
            }).ToList()
        };

        await _unitOfWork.Repository<WorkflowInstance>().AddAsync(instance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return instance.Id;
    }

    public Task<PagedResult<ApprovalQueueItemDto>> GetApprovalQueueAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var items = _unitOfWork.Repository<WorkflowInstance>().Query()
            .Where(x => x.Status == WorkflowStatus.InProgress || x.Status == WorkflowStatus.Pending)
            .OrderByDescending(x => x.InitiatedAt)
            .Select(x => new ApprovalQueueItemDto(x.Id, x.EntityType, x.EntityId, x.CurrentStepOrder, x.Status.ToString(), x.InitiatedAt));

        return Task.FromResult(PagedResult<ApprovalQueueItemDto>.Create(items, request));
    }

    public Task ApproveAsync(Guid instanceId, ApprovalActionRequest request, CancellationToken cancellationToken = default)
    {
        return ExecuteActionAsync(instanceId, ApprovalActionType.Approve, request.Comment, cancellationToken);
    }

    public Task RejectAsync(Guid instanceId, ApprovalActionRequest request, CancellationToken cancellationToken = default)
    {
        return ExecuteActionAsync(instanceId, ApprovalActionType.Reject, request.Comment, cancellationToken);
    }

    private async Task ExecuteActionAsync(Guid instanceId, ApprovalActionType action, string? comment, CancellationToken cancellationToken)
    {
        var instance = await _unitOfWork.Repository<WorkflowInstance>().GetByIdAsync(instanceId, cancellationToken)
            ?? throw new NotFoundException("Workflow instance not found.");
        var steps = _unitOfWork.Repository<WorkflowInstanceStep>().Query().Where(x => x.InstanceId == instanceId).OrderBy(x => x.StepOrder).ToList();
        var currentStep = steps.FirstOrDefault(x => x.StepOrder == instance.CurrentStepOrder)
            ?? throw new BusinessRuleException("Current workflow step is missing.");

        currentStep.Actions.Add(new ApprovalAction
        {
            InstanceId = instance.Id,
            InstanceStepId = currentStep.Id,
            UserId = _currentUserService.UserId ?? Guid.Empty,
            Action = action,
            Comment = comment
        });

        if (action == ApprovalActionType.Reject)
        {
            currentStep.Status = "Rejected";
            currentStep.CompletedAt = DateTime.UtcNow;
            instance.Status = WorkflowStatus.Rejected;
            instance.CompletedAt = DateTime.UtcNow;
            await UpdatePaymentRequestStatusAsync(instance, PaymentRequestStatus.Rejected, comment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        currentStep.Status = "Approved";
        currentStep.CompletedAt = DateTime.UtcNow;

        if (instance.CurrentStepOrder >= instance.TotalSteps)
        {
            instance.Status = WorkflowStatus.Approved;
            instance.CompletedAt = DateTime.UtcNow;
            await UpdatePaymentRequestStatusAsync(instance, PaymentRequestStatus.Approved, null, cancellationToken);
        }
        else
        {
            instance.CurrentStepOrder += 1;
            var nextStep = steps.First(x => x.StepOrder == instance.CurrentStepOrder);
            nextStep.Status = "InProgress";
            nextStep.StartedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdatePaymentRequestStatusAsync(WorkflowInstance instance, PaymentRequestStatus status, string? rejectionReason, CancellationToken cancellationToken)
    {
        if (instance.EntityType != "PaymentRequest")
        {
            return;
        }

        var paymentRequest = await _unitOfWork.Repository<PaymentRequest>().GetByIdAsync(instance.EntityId, cancellationToken);
        if (paymentRequest is null)
        {
            return;
        }

        paymentRequest.Status = status;
        if (status == PaymentRequestStatus.Approved)
        {
            paymentRequest.ApprovedAt = DateTime.UtcNow;
            await _paymentRequestAccountingService.CreateApprovedTransactionAsync(paymentRequest.Id, _currentUserService.UserId, cancellationToken);
            await NotifyRequesterAsync(
                paymentRequest,
                "Đề nghị thanh toán đã được duyệt",
                $"{paymentRequest.RequestNumber} đã được phê duyệt.",
                cancellationToken);
        }
        else if (status == PaymentRequestStatus.Rejected)
        {
            paymentRequest.RejectedAt = DateTime.UtcNow;
            paymentRequest.RejectionReason = rejectionReason;
            await NotifyRequesterAsync(
                paymentRequest,
                "Đề nghị thanh toán bị từ chối",
                $"{paymentRequest.RequestNumber} bị từ chối: {rejectionReason ?? "Không có ghi chú"}.",
                cancellationToken);
        }
    }

    private async Task NotifyRequesterAsync(PaymentRequest paymentRequest, string title, string message, CancellationToken cancellationToken)
    {
        var requester = await _unitOfWork.Repository<Employee>().GetByIdAsync(paymentRequest.RequesterId, cancellationToken);
        if (requester?.UserId is null)
        {
            return;
        }

        await _notificationService.NotifyUserAsync(
            requester.UserId.Value,
            new CreateNotificationRequest(title, message, "ApprovalResult", "High", "PaymentRequest", paymentRequest.Id, "/finance/payment-requests"),
            cancellationToken);
    }

    private static WorkflowTemplateDto MapTemplate(WorkflowTemplate template)
    {
        return new WorkflowTemplateDto(template.Id, template.Name, template.EntityType, template.IsActive, template.IsDefault, template.Steps.Count);
    }
}
