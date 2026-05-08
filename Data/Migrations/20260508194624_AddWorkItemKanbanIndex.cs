using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemKanbanIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkItems_TenantId",
                table: "WorkItems");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_TenantId_Status_DueDate",
                table: "WorkItems",
                columns: new[] { "TenantId", "Status", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkItems_TenantId_Status_DueDate",
                table: "WorkItems");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_TenantId",
                table: "WorkItems",
                column: "TenantId");
        }
    }
}
