using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKanbanColumnWipLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WipEnforced",
                table: "KanbanColumns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "WipLimit",
                table: "KanbanColumns",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_KanbanColumns_WipLimit",
                table: "KanbanColumns",
                sql: "[WipLimit] IS NULL OR [WipLimit] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_KanbanColumns_WipLimit",
                table: "KanbanColumns");

            migrationBuilder.DropColumn(
                name: "WipEnforced",
                table: "KanbanColumns");

            migrationBuilder.DropColumn(
                name: "WipLimit",
                table: "KanbanColumns");
        }
    }
}
