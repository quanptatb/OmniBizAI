using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanTaskOeeMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActualDurationMinutes",
                table: "PlanTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualEndTime",
                table: "PlanTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStartTime",
                table: "PlanTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OeeAvailabilityPercent",
                table: "PlanTasks",
                type: "decimal(9,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OeePercent",
                table: "PlanTasks",
                type: "decimal(9,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OeePerformancePercent",
                table: "PlanTasks",
                type: "decimal(9,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OeeQualityPercent",
                table: "PlanTasks",
                type: "decimal(9,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlannedDurationMinutes",
                table: "PlanTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitsGood",
                table: "PlanTasks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitsProduced",
                table: "PlanTasks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanTasks_TenantId_EquipmentId_Status_ActualEndTime",
                table: "PlanTasks",
                columns: new[] { "TenantId", "EquipmentId", "Status", "ActualEndTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlanTasks_TenantId_EquipmentId_Status_ActualEndTime",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "ActualDurationMinutes",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "ActualEndTime",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "ActualStartTime",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "OeeAvailabilityPercent",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "OeePercent",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "OeePerformancePercent",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "OeeQualityPercent",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "PlannedDurationMinutes",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "UnitsGood",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "UnitsProduced",
                table: "PlanTasks");
        }
    }
}
