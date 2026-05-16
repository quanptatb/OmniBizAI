using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Tenant & Config
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantSetting> TenantSettings => Set<TenantSetting>();
    public DbSet<TenantModule> TenantModules => Set<TenantModule>();
    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();
    public DbSet<SystemParameter> SystemParameters => Set<SystemParameter>();
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();

    // Auth / RBAC
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserTenant> UserTenants => Set<UserTenant>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<RoleDefinition> RoleDefinitions => Set<RoleDefinition>();
    public DbSet<PermissionDefinition> PermissionDefinitions => Set<PermissionDefinition>();
    public DbSet<PermissionAssignment> PermissionAssignments => Set<PermissionAssignment>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    // Organization
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
    public DbSet<EmployeeDepartmentAssignment> EmployeeDepartmentAssignments => Set<EmployeeDepartmentAssignment>();
    public DbSet<EmployeeContract> EmployeeContracts => Set<EmployeeContract>();
    public DbSet<WorkCalendar> WorkCalendars => Set<WorkCalendar>();

    // CRM & Catalog
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
    public DbSet<CustomerSite> CustomerSites => Set<CustomerSite>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductService> ProductServices => Set<ProductService>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();

    // Operations & Work
    public DbSet<OperationRequest> OperationRequests => Set<OperationRequest>();
    public DbSet<OperationRequestLine> OperationRequestLines => Set<OperationRequestLine>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<WorkItemAssignment> WorkItemAssignments => Set<WorkItemAssignment>();
    public DbSet<WorkItemChecklist> WorkItemChecklists => Set<WorkItemChecklist>();
    public DbSet<WorkItemComment> WorkItemComments => Set<WorkItemComment>();
    public DbSet<KanbanColumn> KanbanColumns => Set<KanbanColumn>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<EntityTag> EntityTags => Set<EntityTag>();

    // Workflow & Approval
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowHistory> WorkflowHistories => Set<WorkflowHistory>();
    public DbSet<ApprovalTask> ApprovalTasks => Set<ApprovalTask>();

    // Finance
    public DbSet<ProcurementRequest> ProcurementRequests => Set<ProcurementRequest>();
    public DbSet<ProcurementRequestLine> ProcurementRequestLines => Set<ProcurementRequestLine>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PaymentRequest> PaymentRequests => Set<PaymentRequest>();
    public DbSet<PaymentRequestLine> PaymentRequestLines => Set<PaymentRequestLine>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Expense> Expenses => Set<Expense>();

    // KPI & OKR (merged from Manage-KPI-or-OKR-System)
    public DbSet<KpiDefinition> KpiDefinitions => Set<KpiDefinition>();
    public DbSet<KpiTarget> KpiTargets => Set<KpiTarget>();
    public DbSet<KpiResult> KpiResults => Set<KpiResult>();
    public DbSet<KpiCheckIn> KpiCheckIns => Set<KpiCheckIn>();
    public DbSet<KpiCheckInDetail> KpiCheckInDetails => Set<KpiCheckInDetail>();
    public DbSet<KpiCheckInHistoryLog> KpiCheckInHistoryLogs => Set<KpiCheckInHistoryLog>();
    public DbSet<KpiGoalComment> KpiGoalComments => Set<KpiGoalComment>();
    public DbSet<KpiFailReason> KpiFailReasons => Set<KpiFailReason>();
    public DbSet<KpiDepartmentAssignment> KpiDepartmentAssignments => Set<KpiDepartmentAssignment>();
    public DbSet<KpiEmployeeAssignment> KpiEmployeeAssignments => Set<KpiEmployeeAssignment>();
    public DbSet<KpiAdjustmentHistory> KpiAdjustmentHistories => Set<KpiAdjustmentHistory>();
    public DbSet<KpiResultComparison> KpiResultComparisons => Set<KpiResultComparison>();

    // OKR & Strategy
    public DbSet<MissionVision> MissionVisions => Set<MissionVision>();
    public DbSet<OkrObjective> OkrObjectives => Set<OkrObjective>();
    public DbSet<OkrKeyResult> OkrKeyResults => Set<OkrKeyResult>();
    public DbSet<OkrMissionMapping> OkrMissionMappings => Set<OkrMissionMapping>();
    public DbSet<OkrDepartmentAllocation> OkrDepartmentAllocations => Set<OkrDepartmentAllocation>();
    public DbSet<OkrEmployeeAllocation> OkrEmployeeAllocations => Set<OkrEmployeeAllocation>();

    // Evaluation & HR
    public DbSet<EvaluationPeriod> EvaluationPeriods => Set<EvaluationPeriod>();
    public DbSet<EvaluationResult> EvaluationResults => Set<EvaluationResult>();
    public DbSet<GradingRank> GradingRanks => Set<GradingRank>();
    public DbSet<BonusRule> BonusRules => Set<BonusRule>();
    public DbSet<RealtimeExpectedBonus> RealtimeExpectedBonuses => Set<RealtimeExpectedBonus>();
    public DbSet<OneOnOneMeeting> OneOnOneMeetings => Set<OneOnOneMeeting>();

    // AI / Audit / Import / Notification
    public DbSet<AiPromptTemplate> AiPromptTemplates => Set<AiPromptTemplate>();
    public DbSet<AiProviderConfiguration> AiProviderConfigurations => Set<AiProviderConfiguration>();
    public DbSet<AiInsight> AiInsights => Set<AiInsight>();
    public DbSet<AiGenerationHistory> AiGenerationHistories => Set<AiGenerationHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<ImportStagingRow> ImportStagingRows => Set<ImportStagingRow>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    // Reports
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<DashboardWidget> DashboardWidgets => Set<DashboardWidget>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply configurations from assembly
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global: prevent multiple cascade paths (SQL Server error 1785)
        foreach (var relationship in builder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Global query filter: soft delete & tenant isolation are applied per-entity in configurations
    }
}
