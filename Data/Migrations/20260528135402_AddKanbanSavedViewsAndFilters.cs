using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKanbanSavedViewsAndFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KanbanSavedViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SearchTerm = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SprintId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    DueTo = table.Column<DateOnly>(type: "date", nullable: true),
                    HasAttachment = table.Column<bool>(type: "bit", nullable: false),
                    QuickFilter = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanSavedViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KanbanSavedViews_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KanbanSavedViews_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KanbanSavedViews_TenantId_UserId_Name",
                table: "KanbanSavedViews",
                columns: new[] { "TenantId", "UserId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanSavedViews_UserId",
                table: "KanbanSavedViews",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KanbanSavedViews");
        }
    }
}
