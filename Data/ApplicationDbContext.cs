// PR #23 — transaction boundary
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Models.Entities.Approvals;
using OmniBizAI.Models.Entities.Audit;
using OmniBizAI.Models.Entities.Bom;
using OmniBizAI.Models.Entities.Configuration;
using OmniBizAI.Models.Entities.Customers;
using OmniBizAI.Models.Entities.Menus;
using OmniBizAI.Models.Entities.Procurement;
using OmniBizAI.Models.Entities.Quantities;

namespace OmniBizAI.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    // Menus
    public DbSet<MenuPlan> MenuPlans => Set<MenuPlan>();
    public DbSet<MenuPlanItem> MenuPlanItems => Set<MenuPlanItem>();
    public DbSet<MenuPlanRevision> MenuPlanRevisions => Set<MenuPlanRevision>();

    // Approvals
    public DbSet<InternalApproval> InternalApprovals => Set<InternalApproval>();
    public DbSet<CustomerApprovalToken> CustomerApprovalTokens => Set<CustomerApprovalToken>();
    public DbSet<ApprovalTimeline> ApprovalTimelines => Set<ApprovalTimeline>();

    // Quantities
    public DbSet<DailyMealOrder> DailyMealOrders => Set<DailyMealOrder>();
    public DbSet<QuantitySubmission> QuantitySubmissions => Set<QuantitySubmission>();

    // Procurement
    public DbSet<ProcurementPlan> ProcurementPlans => Set<ProcurementPlan>();
    public DbSet<ProcurementPlanMenu> ProcurementPlanMenus => Set<ProcurementPlanMenu>();
    public DbSet<ProcurementPlanLine> ProcurementPlanLines => Set<ProcurementPlanLine>();

    // BOM
    public DbSet<DishBomItem> DishBomItems => Set<DishBomItem>();

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // Configuration
    public DbSet<ApprovalWorkflowConfig> ApprovalWorkflowConfigs => Set<ApprovalWorkflowConfig>();
    public DbSet<StateTransitionConfig> StateTransitionConfigs => Set<StateTransitionConfig>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    // Customers
    public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
    public DbSet<ServiceContract> ServiceContracts => Set<ServiceContract>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureMenuPlan(builder);
        ConfigureMenuPlanItem(builder);
        ConfigureMenuPlanRevision(builder);
        ConfigureInternalApproval(builder);
        ConfigureCustomerApprovalToken(builder);
        ConfigureApprovalTimeline(builder);
        ConfigureDailyMealOrder(builder);
        ConfigureQuantitySubmission(builder);
        ConfigureProcurementPlan(builder);
        ConfigureProcurementPlanMenu(builder);
        ConfigureProcurementPlanLine(builder);
        ConfigureDishBomItem(builder);
        ConfigureAuditLog(builder);
        ConfigureNotification(builder);
        ConfigureApprovalWorkflowConfig(builder);
        ConfigureStateTransitionConfig(builder);
        ConfigureUserRoleAssignment(builder);
        ConfigureCustomerContact(builder);
        ConfigureServiceContract(builder);
    }

    private static void ConfigureMenuPlan(ModelBuilder builder)
    {
        builder.Entity<MenuPlan>(e =>
        {
            e.ToTable("menu_plans");
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.CustomerCompanyId, x.ServiceDate, x.MealShiftId });
            e.HasIndex(x => new { x.TenantId, x.Status });
        });
    }

    private static void ConfigureMenuPlanItem(ModelBuilder builder)
    {
        builder.Entity<MenuPlanItem>(e =>
        {
            e.ToTable("menu_plan_items");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MenuPlanId, x.MealSlotDefinitionId }).IsUnique();
            e.HasOne(x => x.MenuPlan)
                .WithMany(mp => mp.Items)
                .HasForeignKey(x => x.MenuPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureMenuPlanRevision(ModelBuilder builder)
    {
        builder.Entity<MenuPlanRevision>(e =>
        {
            e.ToTable("menu_plan_revisions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MenuPlanId, x.RevisionNo }).IsUnique();
            e.HasOne(x => x.MenuPlan)
                .WithMany(mp => mp.Revisions)
                .HasForeignKey(x => x.MenuPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureInternalApproval(ModelBuilder builder)
    {
        builder.Entity<InternalApproval>(e =>
        {
            e.ToTable("internal_approvals");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MenuPlanId, x.SequenceNo }).IsUnique();
            e.HasIndex(x => new { x.Status, x.RequiredRoleDefinitionId });
            e.HasOne(x => x.MenuPlan)
                .WithMany(mp => mp.InternalApprovals)
                .HasForeignKey(x => x.MenuPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCustomerApprovalToken(ModelBuilder builder)
    {
        builder.Entity<CustomerApprovalToken>(e =>
        {
            e.ToTable("customer_approval_tokens");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.ExpiresAt);
            e.HasOne(x => x.MenuPlan)
                .WithMany(mp => mp.CustomerApprovalTokens)
                .HasForeignKey(x => x.MenuPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureApprovalTimeline(ModelBuilder builder)
    {
        builder.Entity<ApprovalTimeline>(e =>
        {
            e.ToTable("approval_timelines");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
        });
    }

    private static void ConfigureDailyMealOrder(ModelBuilder builder)
    {
        builder.Entity<DailyMealOrder>(e =>
        {
            e.ToTable("daily_meal_orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasIndex(x => x.MenuPlanId).IsUnique();
            e.HasIndex(x => new { x.CustomerCompanyId, x.DeliveryLocationId, x.ServiceDate, x.MealShiftId, x.MealTypeId });
            e.HasOne(x => x.MenuPlan)
                .WithOne(mp => mp.DailyMealOrder)
                .HasForeignKey<DailyMealOrder>(x => x.MenuPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureQuantitySubmission(ModelBuilder builder)
    {
        builder.Entity<QuantitySubmission>(e =>
        {
            e.ToTable("quantity_submissions");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.DailyMealOrder)
                .WithMany(dmo => dmo.Submissions)
                .HasForeignKey(x => x.DailyMealOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProcurementPlan(ModelBuilder builder)
    {
        builder.Entity<ProcurementPlan>(e =>
        {
            e.ToTable("procurement_plans");
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.ServiceDate, x.MealShiftId, x.Status });
        });
    }

    private static void ConfigureProcurementPlanMenu(ModelBuilder builder)
    {
        builder.Entity<ProcurementPlanMenu>(e =>
        {
            e.ToTable("procurement_plan_menus");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProcurementPlanId, x.MenuPlanId }).IsUnique();
            e.HasOne(x => x.ProcurementPlan)
                .WithMany(pp => pp.ProcurementPlanMenus)
                .HasForeignKey(x => x.ProcurementPlanId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.MenuPlan)
                .WithMany()
                .HasForeignKey(x => x.MenuPlanId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureProcurementPlanLine(ModelBuilder builder)
    {
        builder.Entity<ProcurementPlanLine>(e =>
        {
            e.ToTable("procurement_plan_lines");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProcurementPlanId, x.IngredientId, x.Unit }).IsUnique();
            e.HasOne(x => x.ProcurementPlan)
                .WithMany(pp => pp.Lines)
                .HasForeignKey(x => x.ProcurementPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDishBomItem(ModelBuilder builder)
    {
        builder.Entity<DishBomItem>(e =>
        {
            e.ToTable("dish_bom_items");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.DishId, x.IngredientId }).IsUnique();
        });
    }

    private static void ConfigureAuditLog(ModelBuilder builder)
    {
        builder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAt });
            e.HasIndex(x => new { x.TenantId, x.CreatedAt });
        });
    }

    private static void ConfigureNotification(ModelBuilder builder)
    {
        builder.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        });
    }

    private static void ConfigureApprovalWorkflowConfig(ModelBuilder builder)
    {
        builder.Entity<ApprovalWorkflowConfig>(e =>
        {
            e.ToTable("approval_workflow_configs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.WorkflowType, x.StepNo }).IsUnique();
        });
    }

    private static void ConfigureStateTransitionConfig(ModelBuilder builder)
    {
        builder.Entity<StateTransitionConfig>(e =>
        {
            e.ToTable("state_transition_configs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.StateMachine, x.FromStateCode, x.ActionCode }).IsUnique();
        });
    }

    private static void ConfigureUserRoleAssignment(ModelBuilder builder)
    {
        builder.Entity<UserRoleAssignment>(e =>
        {
            e.ToTable("user_role_assignments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.UserId, x.RoleDefinitionId });
        });
    }

    private static void ConfigureCustomerContact(ModelBuilder builder)
    {
        builder.Entity<CustomerContact>(e =>
        {
            e.ToTable("customer_contacts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CustomerCompanyId, x.Email });
        });
    }

    private static void ConfigureServiceContract(ModelBuilder builder)
    {
        builder.Entity<ServiceContract>(e =>
        {
            e.ToTable("service_contracts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CustomerCompanyId, x.Code }).IsUnique();
        });
    }
}
