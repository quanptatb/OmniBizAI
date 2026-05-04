using System;
using System.Threading;
using System.Threading.Tasks;
using OmniBizAI.ViewModels.Kpi;

namespace OmniBizAI.Services.Kpi;

public interface IKpiService
{
    Task<ObjectiveDto> CreateObjectiveAsync(CreateObjectiveRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<KeyResultDto> CreateKeyResultAsync(CreateKeyResultRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<KpiDto> CreateKpiAsync(CreateKpiRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<CheckInDto> SubmitCheckInAsync(SubmitCheckInRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<CheckInDto> ApproveCheckInAsync(Guid checkInId, ReviewCheckInRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<CheckInDto> RejectCheckInAsync(Guid checkInId, ReviewCheckInRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<EmployeeScorecardDto> GetScorecardAsync(Guid employeeId, Guid periodId, Guid userId, CancellationToken cancellationToken = default);
}
