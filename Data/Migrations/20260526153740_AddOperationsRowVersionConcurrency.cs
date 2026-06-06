using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationsRowVersionConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WorkItems",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ShiftAssignments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PlanTasks",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OperationRequests",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OperationPlans",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MaintenanceIncidents",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Equipments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ApprovalTasks",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ShiftAssignments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PlanTasks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OperationRequests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OperationPlans");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MaintenanceIncidents");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ApprovalTasks");
        }
    }
}
