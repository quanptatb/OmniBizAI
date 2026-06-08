using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentCostBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LaborCost",
                table: "MaintenanceIncidents",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PartsCost",
                table: "MaintenanceIncidents",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "MaintenanceIncidents",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaborCost",
                table: "MaintenanceIncidents");

            migrationBuilder.DropColumn(
                name: "PartsCost",
                table: "MaintenanceIncidents");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "MaintenanceIncidents");
        }
    }
}
