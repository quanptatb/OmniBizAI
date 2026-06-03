using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentLifecycleAndPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PmSchedules_TenantId",
                table: "PmSchedules");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceIncidents_TenantId",
                table: "MaintenanceIncidents");

            migrationBuilder.AddColumn<string>(
                name: "ConditionSensorType",
                table: "PmSchedules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ConditionThreshold",
                table: "PmSchedules",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IntervalCycles",
                table: "PmSchedules",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "IntervalHours",
                table: "PmSchedules",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LastCyclesAtPm",
                table: "PmSchedules",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LastRunHoursAtPm",
                table: "PmSchedules",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggerType",
                table: "PmSchedules",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "FailureModeId",
                table: "MaintenanceIncidents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiveWhysJson",
                table: "MaintenanceIncidents",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnomalyDetected",
                table: "MaintenanceIncidents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "CycleCount",
                table: "Equipments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<double>(
                name: "RunHours",
                table: "Equipments",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "FailureModes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TypicalPreventionMeasure = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailureModes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FailureModes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedTechnicianId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ScheduledEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
                    ActualHours = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ActualCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    WorkDone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PmScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrders_AppUsers_AssignedTechnicianId",
                        column: x => x.AssignedTechnicianId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_AppUsers_CompletedByUserId",
                        column: x => x.CompletedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_AppUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_MaintenanceIncidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "MaintenanceIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_PmSchedules_PmScheduleId",
                        column: x => x.PmScheduleId,
                        principalTable: "PmSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SparePartRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedWorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IssuedGoodsIssueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsAutoReorder = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SparePartRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SparePartRequisitions_AppUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SparePartRequisitions_AppUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SparePartRequisitions_GoodsIssues_IssuedGoodsIssueId",
                        column: x => x.IssuedGoodsIssueId,
                        principalTable: "GoodsIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SparePartRequisitions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SparePartRequisitions_WorkOrders_LinkedWorkOrderId",
                        column: x => x.LinkedWorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderChecklistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderChecklistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderChecklistItems_AppUsers_CompletedByUserId",
                        column: x => x.CompletedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderChecklistItems_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderChecklistItems_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderSparePartUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SparePartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityUsed = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderSparePartUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderSparePartUsages_SpareParts_SparePartId",
                        column: x => x.SparePartId,
                        principalTable: "SpareParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderSparePartUsages_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderSparePartUsages_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SparePartRequisitionLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SparePartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SparePartRequisitionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SparePartRequisitionLines_SparePartRequisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "SparePartRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SparePartRequisitionLines_SpareParts_SparePartId",
                        column: x => x.SparePartId,
                        principalTable: "SpareParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SparePartRequisitionLines_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PmSchedules_TenantId_EquipmentId_NextDueDate",
                table: "PmSchedules",
                columns: new[] { "TenantId", "EquipmentId", "NextDueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceIncidents_FailureModeId",
                table: "MaintenanceIncidents",
                column: "FailureModeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceIncidents_TenantId_EquipmentId_Status",
                table: "MaintenanceIncidents",
                columns: new[] { "TenantId", "EquipmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceIncidents_TenantId_Status",
                table: "MaintenanceIncidents",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FailureModes_TenantId_Category",
                table: "FailureModes",
                columns: new[] { "TenantId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_FailureModes_TenantId_Code",
                table: "FailureModes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SparePartRequisitionLines_RequisitionId",
                table: "SparePartRequisitionLines",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SparePartRequisitionLines_SparePartId",
                table: "SparePartRequisitionLines",
                column: "SparePartId");

            migrationBuilder.CreateIndex(
                name: "IX_SparePartRequisitionLines_TenantId_RequisitionId",
                table: "SparePartRequisitionLines",
                columns: new[] { "TenantId", "RequisitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_SparePartRequisitions_ApprovedByUserId",
                table: "SparePartRequisitions",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SparePartRequisitions_IssuedGoodsIssueId",
                table: "SparePartRequisitions",
                column: "IssuedGoodsIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_SparePartRequisitions_LinkedWorkOrderId",
                table: "SparePartRequisitions",
                column: "LinkedWorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SparePartRequisitions_RequestedByUserId",
                table: "SparePartRequisitions",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SparePartRequisitions_TenantId_Code",
                table: "SparePartRequisitions",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SparePartRequisitions_TenantId_Status",
                table: "SparePartRequisitions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderChecklistItems_CompletedByUserId",
                table: "WorkOrderChecklistItems",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderChecklistItems_TenantId_WorkOrderId_SortOrder",
                table: "WorkOrderChecklistItems",
                columns: new[] { "TenantId", "WorkOrderId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderChecklistItems_WorkOrderId",
                table: "WorkOrderChecklistItems",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_AssignedTechnicianId",
                table: "WorkOrders",
                column: "AssignedTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_CompletedByUserId",
                table: "WorkOrders",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_EquipmentId",
                table: "WorkOrders",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_IncidentId",
                table: "WorkOrders",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_PmScheduleId",
                table: "WorkOrders",
                column: "PmScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_RequestedByUserId",
                table: "WorkOrders",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_Code",
                table: "WorkOrders",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_EquipmentId",
                table: "WorkOrders",
                columns: new[] { "TenantId", "EquipmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_Status_ScheduledStart",
                table: "WorkOrders",
                columns: new[] { "TenantId", "Status", "ScheduledStart" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderSparePartUsages_SparePartId",
                table: "WorkOrderSparePartUsages",
                column: "SparePartId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderSparePartUsages_TenantId_SparePartId",
                table: "WorkOrderSparePartUsages",
                columns: new[] { "TenantId", "SparePartId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderSparePartUsages_TenantId_WorkOrderId",
                table: "WorkOrderSparePartUsages",
                columns: new[] { "TenantId", "WorkOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderSparePartUsages_WorkOrderId",
                table: "WorkOrderSparePartUsages",
                column: "WorkOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceIncidents_FailureModes_FailureModeId",
                table: "MaintenanceIncidents",
                column: "FailureModeId",
                principalTable: "FailureModes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceIncidents_FailureModes_FailureModeId",
                table: "MaintenanceIncidents");

            migrationBuilder.DropTable(
                name: "FailureModes");

            migrationBuilder.DropTable(
                name: "SparePartRequisitionLines");

            migrationBuilder.DropTable(
                name: "WorkOrderChecklistItems");

            migrationBuilder.DropTable(
                name: "WorkOrderSparePartUsages");

            migrationBuilder.DropTable(
                name: "SparePartRequisitions");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_PmSchedules_TenantId_EquipmentId_NextDueDate",
                table: "PmSchedules");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceIncidents_FailureModeId",
                table: "MaintenanceIncidents");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceIncidents_TenantId_EquipmentId_Status",
                table: "MaintenanceIncidents");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceIncidents_TenantId_Status",
                table: "MaintenanceIncidents");

            migrationBuilder.DropColumn(
                name: "ConditionSensorType",
                table: "PmSchedules");

            migrationBuilder.DropColumn(
                name: "ConditionThreshold",
                table: "PmSchedules");

            migrationBuilder.DropColumn(
                name: "IntervalCycles",
                table: "PmSchedules");

            migrationBuilder.DropColumn(
                name: "IntervalHours",
                table: "PmSchedules");

            migrationBuilder.DropColumn(
                name: "LastCyclesAtPm",
                table: "PmSchedules");

            migrationBuilder.DropColumn(
                name: "LastRunHoursAtPm",
                table: "PmSchedules");

            migrationBuilder.DropColumn(
                name: "TriggerType",
                table: "PmSchedules");

            migrationBuilder.DropColumn(
                name: "FailureModeId",
                table: "MaintenanceIncidents");

            migrationBuilder.DropColumn(
                name: "FiveWhysJson",
                table: "MaintenanceIncidents");

            migrationBuilder.DropColumn(
                name: "IsAnomalyDetected",
                table: "MaintenanceIncidents");

            migrationBuilder.DropColumn(
                name: "CycleCount",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "RunHours",
                table: "Equipments");

            migrationBuilder.CreateIndex(
                name: "IX_PmSchedules_TenantId",
                table: "PmSchedules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceIncidents_TenantId",
                table: "MaintenanceIncidents",
                column: "TenantId");
        }
    }
}
