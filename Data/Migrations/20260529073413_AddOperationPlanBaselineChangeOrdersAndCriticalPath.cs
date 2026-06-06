using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationPlanBaselineChangeOrdersAndCriticalPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EarlyFinish",
                table: "PlanTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EarlyStart",
                table: "PlanTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCriticalPath",
                table: "PlanTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LateFinish",
                table: "PlanTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LateStart",
                table: "PlanTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlackMinutes",
                table: "PlanTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProjectedEndDate",
                table: "OperationPlans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlanChangeOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NewStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldEndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NewEndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldAssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NewAssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OldEquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NewEquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanChangeOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanChangeOrders_AppUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanChangeOrders_AppUsers_NewAssignedUserId",
                        column: x => x.NewAssignedUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanChangeOrders_AppUsers_OldAssignedUserId",
                        column: x => x.OldAssignedUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanChangeOrders_Equipments_NewEquipmentId",
                        column: x => x.NewEquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanChangeOrders_Equipments_OldEquipmentId",
                        column: x => x.OldEquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanChangeOrders_OperationPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "OperationPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanChangeOrders_PlanTasks_PlanTaskId",
                        column: x => x.PlanTaskId,
                        principalTable: "PlanTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanChangeOrders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanTaskBaselines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BaselineStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaselineEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaselineAssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BaselineEquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SnapshottedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SnapshottedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanTaskBaselines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanTaskBaselines_AppUsers_BaselineAssignedUserId",
                        column: x => x.BaselineAssignedUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanTaskBaselines_AppUsers_SnapshottedByUserId",
                        column: x => x.SnapshottedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanTaskBaselines_Equipments_BaselineEquipmentId",
                        column: x => x.BaselineEquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanTaskBaselines_OperationPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "OperationPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanTaskBaselines_PlanTasks_PlanTaskId",
                        column: x => x.PlanTaskId,
                        principalTable: "PlanTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanTaskBaselines_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanTaskDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PredecessorTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuccessorTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanTaskDependencies", x => x.Id);
                    table.CheckConstraint("CK_PlanTaskDependencies_NoSelfDependency", "[PredecessorTaskId] <> [SuccessorTaskId]");
                    table.ForeignKey(
                        name: "FK_PlanTaskDependencies_OperationPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "OperationPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanTaskDependencies_PlanTasks_PredecessorTaskId",
                        column: x => x.PredecessorTaskId,
                        principalTable: "PlanTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanTaskDependencies_PlanTasks_SuccessorTaskId",
                        column: x => x.SuccessorTaskId,
                        principalTable: "PlanTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanTaskDependencies_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanTasks_TenantId_PlanId_EarlyStart",
                table: "PlanTasks",
                columns: new[] { "TenantId", "PlanId", "EarlyStart" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanTasks_TenantId_PlanId_IsCriticalPath",
                table: "PlanTasks",
                columns: new[] { "TenantId", "PlanId", "IsCriticalPath" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationPlans_TenantId_Status_ProjectedEndDate",
                table: "OperationPlans",
                columns: new[] { "TenantId", "Status", "ProjectedEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeOrders_ApprovedByUserId",
                table: "PlanChangeOrders",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeOrders_NewAssignedUserId",
                table: "PlanChangeOrders",
                column: "NewAssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeOrders_NewEquipmentId",
                table: "PlanChangeOrders",
                column: "NewEquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeOrders_OldAssignedUserId",
                table: "PlanChangeOrders",
                column: "OldAssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeOrders_OldEquipmentId",
                table: "PlanChangeOrders",
                column: "OldEquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeOrders_PlanId",
                table: "PlanChangeOrders",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeOrders_PlanTaskId",
                table: "PlanChangeOrders",
                column: "PlanTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeOrders_TenantId_PlanId_CreatedAt",
                table: "PlanChangeOrders",
                columns: new[] { "TenantId", "PlanId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeOrders_TenantId_PlanTaskId_CreatedAt",
                table: "PlanChangeOrders",
                columns: new[] { "TenantId", "PlanTaskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanTaskBaselines_BaselineAssignedUserId",
                table: "PlanTaskBaselines",
                column: "BaselineAssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTaskBaselines_BaselineEquipmentId",
                table: "PlanTaskBaselines",
                column: "BaselineEquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTaskBaselines_PlanId",
                table: "PlanTaskBaselines",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTaskBaselines_PlanTaskId",
                table: "PlanTaskBaselines",
                column: "PlanTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTaskBaselines_SnapshottedByUserId",
                table: "PlanTaskBaselines",
                column: "SnapshottedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTaskBaselines_TenantId_PlanId",
                table: "PlanTaskBaselines",
                columns: new[] { "TenantId", "PlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanTaskBaselines_TenantId_PlanTaskId",
                table: "PlanTaskBaselines",
                columns: new[] { "TenantId", "PlanTaskId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTaskDependencies_PlanId",
                table: "PlanTaskDependencies",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTaskDependencies_PredecessorTaskId",
                table: "PlanTaskDependencies",
                column: "PredecessorTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTaskDependencies_SuccessorTaskId",
                table: "PlanTaskDependencies",
                column: "SuccessorTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTaskDependencies_TenantId_PlanId",
                table: "PlanTaskDependencies",
                columns: new[] { "TenantId", "PlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanTaskDependencies_TenantId_PredecessorTaskId_SuccessorTaskId_Type",
                table: "PlanTaskDependencies",
                columns: new[] { "TenantId", "PredecessorTaskId", "SuccessorTaskId", "Type" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanChangeOrders");

            migrationBuilder.DropTable(
                name: "PlanTaskBaselines");

            migrationBuilder.DropTable(
                name: "PlanTaskDependencies");

            migrationBuilder.DropIndex(
                name: "IX_PlanTasks_TenantId_PlanId_EarlyStart",
                table: "PlanTasks");

            migrationBuilder.DropIndex(
                name: "IX_PlanTasks_TenantId_PlanId_IsCriticalPath",
                table: "PlanTasks");

            migrationBuilder.DropIndex(
                name: "IX_OperationPlans_TenantId_Status_ProjectedEndDate",
                table: "OperationPlans");

            migrationBuilder.DropColumn(
                name: "EarlyFinish",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "EarlyStart",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "IsCriticalPath",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "LateFinish",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "LateStart",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "SlackMinutes",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "ProjectedEndDate",
                table: "OperationPlans");
        }
    }
}
