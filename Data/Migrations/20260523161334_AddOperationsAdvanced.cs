using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationsAdvanced : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SpareParts",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastOverdueNotificationAt",
                table: "PmSchedules",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PmSchedules",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PlanTasks",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OperationRequests",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MaintenanceIncidents",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DowntimeEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DowntimeEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DowntimeEvents_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DowntimeEvents_MaintenanceIncidents_MaintenanceIncidentId",
                        column: x => x.MaintenanceIncidentId,
                        principalTable: "MaintenanceIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DowntimeEvents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentAvailabilityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ShiftCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PlannedMinutes = table.Column<int>(type: "int", nullable: false),
                    DowntimeMinutes = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentAvailabilityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentAvailabilityLogs_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentAvailabilityLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperationSlaConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    OrganizationUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponseHours = table.Column<int>(type: "int", nullable: false),
                    ResolveHours = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationSlaConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationSlaConfigs_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationSlaConfigs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProductCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IdealCycleSeconds = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GoodCount = table.Column<int>(type: "int", nullable: false),
                    RejectCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionRuns_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRuns_PlanTasks_PlanTaskId",
                        column: x => x.PlanTaskId,
                        principalTable: "PlanTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRuns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeasuredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DefectType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DefectCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_QualityResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityResults_ProductionRuns_ProductionRunId",
                        column: x => x.ProductionRunId,
                        principalTable: "ProductionRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityResults_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DowntimeEvents_EquipmentId",
                table: "DowntimeEvents",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DowntimeEvents_MaintenanceIncidentId",
                table: "DowntimeEvents",
                column: "MaintenanceIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_DowntimeEvents_TenantId",
                table: "DowntimeEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAvailabilityLogs_EquipmentId",
                table: "EquipmentAvailabilityLogs",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAvailabilityLogs_TenantId",
                table: "EquipmentAvailabilityLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationSlaConfigs_OrganizationUnitId",
                table: "OperationSlaConfigs",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationSlaConfigs_TenantId",
                table: "OperationSlaConfigs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRuns_EquipmentId",
                table: "ProductionRuns",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRuns_PlanTaskId",
                table: "ProductionRuns",
                column: "PlanTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRuns_TenantId",
                table: "ProductionRuns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityResults_ProductionRunId",
                table: "QualityResults",
                column: "ProductionRunId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityResults_TenantId",
                table: "QualityResults",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DowntimeEvents");

            migrationBuilder.DropTable(
                name: "EquipmentAvailabilityLogs");

            migrationBuilder.DropTable(
                name: "OperationSlaConfigs");

            migrationBuilder.DropTable(
                name: "QualityResults");

            migrationBuilder.DropTable(
                name: "ProductionRuns");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SpareParts");

            migrationBuilder.DropColumn(
                name: "LastOverdueNotificationAt",
                table: "PmSchedules");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PmSchedules");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OperationRequests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MaintenanceIncidents");
        }
    }
}
