using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class ResourceManagementController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public ResourceManagementController(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IActionResult> Index()
    {
        var tid = _tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);

        var vm = new ResourceManagementDashboardViewModel
        {
            Human = new HumanResourceOverviewViewModel
            {
                TotalEmployees = await _db.EmployeeProfiles.CountAsync(x => x.TenantId == tid && !x.IsDeleted),
                OnLeaveToday = await _db.LeaveRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Status == LeaveStatus.Approved && x.StartDate <= today && x.EndDate >= today),
                PendingLeaves = await _db.LeaveRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Status == LeaveStatus.Submitted),
                AvgPerformanceScore = await _db.EvaluationResults.Where(x => x.TenantId == tid && !x.IsDeleted).Select(x => (decimal?)x.FinalScore).AverageAsync() ?? 0
            },
            Equipment = new EquipmentResourceOverviewViewModel
            {
                TotalMaintenanceRequests = await _db.OperationRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Type == "Maintenance"),
                ActiveMaintenanceRequests = await _db.OperationRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Type == "Maintenance" && (x.Status == OperationStatus.Submitted || x.Status == OperationStatus.InProgress)),
                CompletedMaintenanceRequests = await _db.OperationRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Type == "Maintenance" && x.Status == OperationStatus.Completed),
                OverdueMaintenanceRequests = await _db.OperationRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Type == "Maintenance" && x.DueDate.HasValue && x.DueDate < today && x.Status != OperationStatus.Completed)
            },
            Inventory = new InventoryResourceOverviewViewModel
            {
                TotalProducts = await _db.ProductServices.CountAsync(x => x.TenantId == tid && !x.IsDeleted),
                ActiveStockAlerts = await _db.StockAlerts.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.IsActive),
                GoodsReceiptCount = await _db.GoodsReceipts.CountAsync(x => x.TenantId == tid && !x.IsDeleted),
                GoodsIssueCount = await _db.GoodsIssues.CountAsync(x => x.TenantId == tid && !x.IsDeleted)
            },
            Infrastructure = new InfrastructureResourceOverviewViewModel
            {
                TotalDepartments = await _db.OrganizationUnits.CountAsync(x => x.TenantId == tid && !x.IsDeleted),
                TotalPositions = await _db.Positions.CountAsync(x => x.TenantId == tid && !x.IsDeleted),
                TotalWorkCalendars = await _db.WorkCalendars.CountAsync(x => x.TenantId == tid && !x.IsDeleted),
                TotalCustomerSites = await _db.CustomerSites.CountAsync(x => x.TenantId == tid && !x.IsDeleted)
            }
        };

        return View(vm);
    }
}
