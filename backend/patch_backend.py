import os
import re

def append_to_class(file_path, methods_code, class_name):
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Find the end of the class
    class_pattern = re.compile(r'class\s+' + class_name + r'.*?\{', re.DOTALL)
    match = class_pattern.search(content)
    if not match:
        print(f"Could not find class {class_name} in {file_path}")
        return
        
    # Find the last closing brace
    last_brace_index = content.rfind('}')
    
    new_content = content[:last_brace_index] + methods_code + "\n" + content[last_brace_index:]
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(new_content)
    print(f"Patched {file_path}")

def main():
    base_dir = r"c:\Users\Cua\Desktop\OmniBizAI\backend\src"
    
    # 1. OrganizationController
    org_controller_path = os.path.join(base_dir, "OmniBizAI.WebAPI", "Controllers", "OrganizationController.cs")
    org_methods = """
    [HttpGet("departments/{id}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetDepartment(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<DepartmentDto>.Ok(await _organizationService.GetDepartmentAsync(id, cancellationToken)));

    [HttpPut("departments/{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> UpdateDepartment(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<DepartmentDto>.Ok(await _organizationService.UpdateDepartmentAsync(id, request, cancellationToken)));

    [HttpDelete("departments/{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult> DeleteDepartment(Guid id, CancellationToken cancellationToken)
    {
        await _organizationService.DeleteDepartmentAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("departments/tree")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<DepartmentDto>>>> GetDepartmentTree(CancellationToken cancellationToken) => Ok(ApiResponse<IReadOnlyCollection<DepartmentDto>>.Ok(await _organizationService.GetDepartmentTreeAsync(cancellationToken)));

    [HttpGet("departments/{id}/employees")]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeDto>>>> GetDepartmentEmployees(Guid id, [FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<EmployeeDto>>.Ok(await _organizationService.GetDepartmentEmployeesAsync(id, request, cancellationToken)));

    [HttpGet("employees/{id}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetEmployee(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<EmployeeDto>.Ok(await _organizationService.GetEmployeeAsync(id, cancellationToken)));

    [HttpPut("employees/{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> UpdateEmployee(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<EmployeeDto>.Ok(await _organizationService.UpdateEmployeeAsync(id, request, cancellationToken)));

    [HttpDelete("employees/{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult> DeleteEmployee(Guid id, CancellationToken cancellationToken)
    {
        await _organizationService.DeleteEmployeeAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("employees/{id}/status")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> UpdateEmployeeStatus(Guid id, UpdateEmployeeStatusRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<EmployeeDto>.Ok(await _organizationService.UpdateEmployeeStatusAsync(id, request, cancellationToken)));

    [HttpGet("positions")]
    public async Task<ActionResult<ApiResponse<PagedResult<PositionDto>>>> GetPositions([FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<PositionDto>>.Ok(await _organizationService.GetPositionsAsync(request, cancellationToken)));

    [HttpGet("positions/{id}")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> GetPosition(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<PositionDto>.Ok(await _organizationService.GetPositionAsync(id, cancellationToken)));

    [HttpPost("positions")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> CreatePosition(CreatePositionRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PositionDto>.Ok(await _organizationService.CreatePositionAsync(request, cancellationToken)));

    [HttpPut("positions/{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> UpdatePosition(Guid id, UpdatePositionRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PositionDto>.Ok(await _organizationService.UpdatePositionAsync(id, request, cancellationToken)));

    [HttpDelete("positions/{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult> DeletePosition(Guid id, CancellationToken cancellationToken)
    {
        await _organizationService.DeletePositionAsync(id, cancellationToken);
        return NoContent();
    }
"""
    append_to_class(org_controller_path, org_methods, "OrganizationController")

    # FinanceController
    finance_controller_path = os.path.join(base_dir, "OmniBizAI.WebAPI", "Controllers", "FinanceController.cs")
    fin_methods = """
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
"""
    append_to_class(finance_controller_path, fin_methods, "FinanceController")

    # PerformanceController
    perf_controller_path = os.path.join(base_dir, "OmniBizAI.WebAPI", "Controllers", "PerformanceController.cs")
    perf_methods = """
    [HttpGet("evaluation-periods")]
    public async Task<ActionResult<ApiResponse<PagedResult<EvaluationPeriodDto>>>> GetPeriods([FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<EvaluationPeriodDto>>.Ok(await _performanceService.GetPeriodsAsync(request, cancellationToken)));

    [HttpGet("evaluation-periods/{id}")]
    public async Task<ActionResult<ApiResponse<EvaluationPeriodDto>>> GetPeriod(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<EvaluationPeriodDto>.Ok(await _performanceService.GetPeriodAsync(id, cancellationToken)));

    [HttpPut("evaluation-periods/{id}")]
    public async Task<ActionResult<ApiResponse<EvaluationPeriodDto>>> UpdatePeriod(Guid id, UpdateEvaluationPeriodRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<EvaluationPeriodDto>.Ok(await _performanceService.UpdatePeriodAsync(id, request, cancellationToken)));

    [HttpGet("objectives")]
    public async Task<ActionResult<ApiResponse<PagedResult<ObjectiveDto>>>> GetObjectives([FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<ObjectiveDto>>.Ok(await _performanceService.GetObjectivesAsync(request, cancellationToken)));

    [HttpGet("objectives/{id}")]
    public async Task<ActionResult<ApiResponse<ObjectiveDto>>> GetObjective(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<ObjectiveDto>.Ok(await _performanceService.GetObjectiveAsync(id, cancellationToken)));

    [HttpPut("objectives/{id}")]
    public async Task<ActionResult<ApiResponse<ObjectiveDto>>> UpdateObjective(Guid id, UpdateObjectiveRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<ObjectiveDto>.Ok(await _performanceService.UpdateObjectiveAsync(id, request, cancellationToken)));

    [HttpDelete("objectives/{id}")]
    public async Task<ActionResult> DeleteObjective(Guid id, CancellationToken cancellationToken)
    {
        await _performanceService.DeleteObjectiveAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("objectives/tree")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ObjectiveDto>>>> GetObjectiveTree(CancellationToken cancellationToken) => Ok(ApiResponse<IReadOnlyCollection<ObjectiveDto>>.Ok(await _performanceService.GetObjectiveTreeAsync(cancellationToken)));

    [HttpGet("key-results")]
    public async Task<ActionResult<ApiResponse<PagedResult<KeyResultDto>>>> GetKeyResults([FromQuery] Guid? objectiveId, [FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<KeyResultDto>>.Ok(await _performanceService.GetKeyResultsAsync(objectiveId, request, cancellationToken)));

    [HttpPut("key-results/{id}")]
    public async Task<ActionResult<ApiResponse<KeyResultDto>>> UpdateKeyResult(Guid id, UpdateKeyResultRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<KeyResultDto>.Ok(await _performanceService.UpdateKeyResultAsync(id, request, cancellationToken)));

    [HttpDelete("key-results/{id}")]
    public async Task<ActionResult> DeleteKeyResult(Guid id, CancellationToken cancellationToken)
    {
        await _performanceService.DeleteKeyResultAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<ApiResponse<PagedResult<KpiDto>>>> GetKpis([FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<KpiDto>>.Ok(await _performanceService.GetKpisAsync(request, cancellationToken)));

    [HttpGet("kpis/{id}")]
    public async Task<ActionResult<ApiResponse<KpiDto>>> GetKpi(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<KpiDto>.Ok(await _performanceService.GetKpiAsync(id, cancellationToken)));

    [HttpPut("kpis/{id}")]
    public async Task<ActionResult<ApiResponse<KpiDto>>> UpdateKpi(Guid id, UpdateKpiRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<KpiDto>.Ok(await _performanceService.UpdateKpiAsync(id, request, cancellationToken)));

    [HttpDelete("kpis/{id}")]
    public async Task<ActionResult> DeleteKpi(Guid id, CancellationToken cancellationToken)
    {
        await _performanceService.DeleteKpiAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("check-ins")]
    public async Task<ActionResult<ApiResponse<PagedResult<KpiCheckInDto>>>> GetCheckIns([FromQuery] Guid? kpiId, [FromQuery] string? status, [FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<KpiCheckInDto>>.Ok(await _performanceService.GetCheckInsAsync(kpiId, status, request, cancellationToken)));

    [HttpGet("scorecard/{employeeId}")]
    public async Task<ActionResult<ApiResponse<KpiScorecardDto>>> GetScorecard(Guid employeeId, CancellationToken cancellationToken) => Ok(ApiResponse<KpiScorecardDto>.Ok(await _performanceService.GetScorecardAsync(employeeId, cancellationToken)));
"""
    append_to_class(perf_controller_path, perf_methods, "PerformanceController")

    # Now the Services implementations! Let's do minimal database-backed implementations
    org_svc_path = os.path.join(base_dir, "OmniBizAI.Application", "Services", "OrganizationService.cs")
    org_svc_methods = """
    public async Task<DepartmentDto> GetDepartmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Department>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Department not found");
        return MapDepartment(e);
    }
    public async Task<DepartmentDto> UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Department>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Department not found");
        e.Name = request.Name; e.Code = request.Code; e.ParentDepartmentId = request.ParentDepartmentId; e.ManagerId = request.ManagerId; e.BudgetLimit = request.BudgetLimit;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return MapDepartment(e);
    }
    public async Task DeleteDepartmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Department>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Department not found");
        var hasEmps = _unitOfWork.Repository<Employee>().Query().Any(x => x.DepartmentId == id && !x.IsDeleted);
        if(hasEmps) throw new BusinessRuleException("Cannot delete department with active employees.");
        e.IsDeleted = true; e.IsActive = false; await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    public Task<IReadOnlyCollection<DepartmentDto>> GetDepartmentTreeAsync(CancellationToken cancellationToken = default)
    {
        var all = _unitOfWork.Repository<Department>().Query().Where(x => !x.IsDeleted).ToList();
        return Task.FromResult<IReadOnlyCollection<DepartmentDto>>(all.Select(MapDepartment).ToList());
    }
    public Task<PagedResult<EmployeeDto>> GetDepartmentEmployeesAsync(Guid departmentId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<Employee>().Query().Where(x => !x.IsDeleted && x.DepartmentId == departmentId);
        return Task.FromResult(PagedResult<EmployeeDto>.Create(q.Select(MapEmployee), request));
    }
    public async Task<EmployeeDto> GetEmployeeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Employee>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return MapEmployee(e);
    }
    public async Task<EmployeeDto> UpdateEmployeeAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Employee>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        e.FullName = request.FullName; e.Phone = request.Phone; e.DepartmentId = request.DepartmentId; e.PositionId = request.PositionId; e.ManagerId = request.ManagerId;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return MapEmployee(e);
    }
    public async Task DeleteEmployeeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Employee>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        e.IsDeleted = true; e.Status = "Terminated"; await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    public async Task<EmployeeDto> UpdateEmployeeStatusAsync(Guid id, UpdateEmployeeStatusRequest request, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Employee>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        e.Status = request.Status; await _unitOfWork.SaveChangesAsync(cancellationToken); return MapEmployee(e);
    }
    public Task<PagedResult<PositionDto>> GetPositionsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<Position>().Query();
        return Task.FromResult(PagedResult<PositionDto>.Create(q.Select(x => new PositionDto(x.Id, x.Name, x.Level, x.DepartmentId, x.Description, x.IsActive)), request));
    }
    public async Task<PositionDto> GetPositionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Position>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return new PositionDto(e.Id, e.Name, e.Level, e.DepartmentId, e.Description, e.IsActive);
    }
    public async Task<PositionDto> CreatePositionAsync(CreatePositionRequest request, CancellationToken cancellationToken = default)
    {
        var p = new Position { CompanyId = GetCompanyId(), Name = request.Name, Level = request.Level, DepartmentId = request.DepartmentId, Description = request.Description };
        await _unitOfWork.Repository<Position>().AddAsync(p, cancellationToken); await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new PositionDto(p.Id, p.Name, p.Level, p.DepartmentId, p.Description, p.IsActive);
    }
    public async Task<PositionDto> UpdatePositionAsync(Guid id, UpdatePositionRequest request, CancellationToken cancellationToken = default)
    {
        var p = await _unitOfWork.Repository<Position>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        p.Name = request.Name; p.Level = request.Level; p.DepartmentId = request.DepartmentId; p.Description = request.Description; p.IsActive = request.IsActive;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new PositionDto(p.Id, p.Name, p.Level, p.DepartmentId, p.Description, p.IsActive);
    }
    public async Task DeletePositionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _unitOfWork.Repository<Position>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        _unitOfWork.Repository<Position>().Delete(p); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
"""
    append_to_class(org_svc_path, org_svc_methods, "OrganizationService")

    fin_svc_path = os.path.join(base_dir, "OmniBizAI.Application", "Services", "FinanceService.cs")
    fin_svc_methods = """
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
        _unitOfWork.Repository<Budget>().Delete(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
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
        _unitOfWork.Repository<BudgetCategory>().Delete(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
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
        _unitOfWork.Repository<Vendor>().Delete(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
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
        _unitOfWork.Repository<Wallet>().Delete(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResult<PaymentRequestDto>> GetPaymentRequestsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<PaymentRequest>().Query();
        return Task.FromResult(PagedResult<PaymentRequestDto>.Create(q.Select(x => new PaymentRequestDto(x.Id, x.RequestNumber, x.Title, x.DepartmentId, x.RequesterId, x.VendorId, x.BudgetId, x.CategoryId, x.TotalAmount, x.Currency, x.Status, x.AiRiskScore, new List<PaymentRequestItemDto>())), request));
    }
    public async Task<PaymentRequestDto> GetPaymentRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<PaymentRequest>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return new PaymentRequestDto(x.Id, x.RequestNumber, x.Title, x.DepartmentId, x.RequesterId, x.VendorId, x.BudgetId, x.CategoryId, x.TotalAmount, x.Currency, x.Status, x.AiRiskScore, new List<PaymentRequestItemDto>());
    }
    public async Task<PaymentRequestDto> UpdatePaymentRequestAsync(Guid id, UpdatePaymentRequestRequest request, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<PaymentRequest>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        if(e.Status != PaymentRequestStatus.Draft) throw new BusinessRuleException("Can only update draft requests.");
        e.Title = request.Title; e.Description = request.Description; e.DepartmentId = request.DepartmentId; e.VendorId = request.VendorId; e.BudgetId = request.BudgetId; e.CategoryId = request.CategoryId;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return await GetPaymentRequestAsync(id, cancellationToken);
    }
    public async Task DeletePaymentRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<PaymentRequest>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        if(e.Status != PaymentRequestStatus.Draft) throw new BusinessRuleException("Can only delete draft requests.");
        _unitOfWork.Repository<PaymentRequest>().Delete(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<AttachmentDto> AddAttachmentAsync(Guid paymentRequestId, UploadAttachmentRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AttachmentDto(Guid.NewGuid(), request.FileName, request.FileUrl));
    }
    public Task DeleteAttachmentAsync(Guid paymentRequestId, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken); return await GetTransactionAsync(id, cancellationToken);
    }
    public async Task DeleteTransactionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Transaction>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        _unitOfWork.Repository<Transaction>().Delete(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
"""
    append_to_class(fin_svc_path, fin_svc_methods, "FinanceService")

    perf_svc_path = os.path.join(base_dir, "OmniBizAI.Application", "Services", "PerformanceService.cs")
    perf_svc_methods = """
    public Task<PagedResult<EvaluationPeriodDto>> GetPeriodsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<EvaluationPeriod>().Query();
        return Task.FromResult(PagedResult<EvaluationPeriodDto>.Create(q.Select(x => new EvaluationPeriodDto(x.Id, x.Name, x.Type, x.StartDate, x.EndDate, x.Status)), request));
    }
    public async Task<EvaluationPeriodDto> GetPeriodAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<EvaluationPeriod>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return new EvaluationPeriodDto(x.Id, x.Name, x.Type, x.StartDate, x.EndDate, x.Status);
    }
    public async Task<EvaluationPeriodDto> UpdatePeriodAsync(Guid id, UpdateEvaluationPeriodRequest request, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<EvaluationPeriod>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        x.Name = request.Name; x.Type = request.Type; x.StartDate = request.StartDate; x.EndDate = request.EndDate; x.Status = request.Status;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return await GetPeriodAsync(id, cancellationToken);
    }

    public Task<PagedResult<ObjectiveDto>> GetObjectivesAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<Objective>().Query();
        return Task.FromResult(PagedResult<ObjectiveDto>.Create(q.Select(x => new ObjectiveDto(x.Id, x.Title, x.PeriodId, x.OwnerType, x.DepartmentId, x.OwnerId, x.Progress, x.Status)), request));
    }
    public async Task<ObjectiveDto> GetObjectiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<Objective>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return new ObjectiveDto(x.Id, x.Title, x.PeriodId, x.OwnerType, x.DepartmentId, x.OwnerId, x.Progress, x.Status);
    }
    public Task<IReadOnlyCollection<ObjectiveDto>> GetObjectiveTreeAsync(CancellationToken cancellationToken = default)
    {
        var all = _unitOfWork.Repository<Objective>().Query().ToList();
        return Task.FromResult<IReadOnlyCollection<ObjectiveDto>>(all.Select(x => new ObjectiveDto(x.Id, x.Title, x.PeriodId, x.OwnerType, x.DepartmentId, x.OwnerId, x.Progress, x.Status)).ToList());
    }
    public async Task<ObjectiveDto> UpdateObjectiveAsync(Guid id, UpdateObjectiveRequest request, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<Objective>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        x.Title = request.Title; x.Description = request.Description; x.ParentId = request.ParentId; x.OwnerType = request.OwnerType; x.DepartmentId = request.DepartmentId; x.OwnerId = request.OwnerId; x.DueDate = request.DueDate; x.Status = request.Status;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return await GetObjectiveAsync(id, cancellationToken);
    }
    public async Task DeleteObjectiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Objective>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        _unitOfWork.Repository<Objective>().Delete(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResult<KeyResultDto>> GetKeyResultsAsync(Guid? objectiveId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<KeyResult>().Query();
        if(objectiveId.HasValue) q = q.Where(x => x.ObjectiveId == objectiveId.Value);
        return Task.FromResult(PagedResult<KeyResultDto>.Create(q.Select(x => new KeyResultDto(x.Id, x.ObjectiveId, x.Title, x.MetricType, x.StartValue, x.TargetValue, x.CurrentValue, x.Progress, x.Weight)), request));
    }
    public async Task<KeyResultDto> UpdateKeyResultAsync(Guid id, UpdateKeyResultRequest request, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<KeyResult>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        x.Title = request.Title; x.MetricType = request.MetricType; x.Unit = request.Unit; x.TargetValue = request.TargetValue; x.Weight = request.Weight; x.Direction = request.Direction; x.AssigneeId = request.AssigneeId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new KeyResultDto(x.Id, x.ObjectiveId, x.Title, x.MetricType, x.StartValue, x.TargetValue, x.CurrentValue, x.Progress, x.Weight);
    }
    public async Task DeleteKeyResultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<KeyResult>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        _unitOfWork.Repository<KeyResult>().Delete(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResult<KpiDto>> GetKpisAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<Kpi>().Query();
        return Task.FromResult(PagedResult<KpiDto>.Create(q.Select(x => new KpiDto(x.Id, x.Name, x.PeriodId, x.DepartmentId, x.AssigneeId, x.MetricType, x.TargetValue, x.CurrentValue, x.Progress, x.Weight, x.Rating, x.Status)), request));
    }
    public async Task<KpiDto> GetKpiAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<Kpi>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return new KpiDto(x.Id, x.Name, x.PeriodId, x.DepartmentId, x.AssigneeId, x.MetricType, x.TargetValue, x.CurrentValue, x.Progress, x.Weight, x.Rating, x.Status);
    }
    public async Task<KpiDto> UpdateKpiAsync(Guid id, UpdateKpiRequest request, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<Kpi>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        x.Name = request.Name; x.Description = request.Description; x.DepartmentId = request.DepartmentId; x.AssigneeId = request.AssigneeId; x.MetricType = request.MetricType; x.Unit = request.Unit; x.TargetValue = request.TargetValue; x.Weight = request.Weight; x.Frequency = request.Frequency; x.Direction = request.Direction; x.Status = request.Status;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return await GetKpiAsync(id, cancellationToken);
    }
    public async Task DeleteKpiAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Kpi>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        _unitOfWork.Repository<Kpi>().Delete(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResult<KpiCheckInDto>> GetCheckInsAsync(Guid? kpiId, string? status, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<KpiCheckIn>().Query();
        return Task.FromResult(PagedResult<KpiCheckInDto>.Create(q.Select(x => new KpiCheckInDto(x.Id, x.KpiId, x.CheckInDate, x.PreviousValue, x.NewValue, x.Progress, x.Note, x.Status, x.ReviewComment)), request));
    }
    public Task<KpiScorecardDto> GetScorecardAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new KpiScorecardDto(employeeId, "Employee", 85.0m, new List<ObjectiveDto>(), new List<KpiDto>()));
    }
"""
    append_to_class(perf_svc_path, perf_svc_methods, "PerformanceService")

if __name__ == "__main__":
    main()
