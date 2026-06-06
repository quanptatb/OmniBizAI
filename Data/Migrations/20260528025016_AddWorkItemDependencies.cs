using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemDependencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkItemDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlockerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlockedId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItemDependencies", x => x.Id);
                    table.CheckConstraint("CK_WorkItemDependencies_NoSelfDependency", "[BlockerId] <> [BlockedId]");
                    table.ForeignKey(
                        name: "FK_WorkItemDependencies_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkItemDependencies_WorkItems_BlockedId",
                        column: x => x.BlockedId,
                        principalTable: "WorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkItemDependencies_WorkItems_BlockerId",
                        column: x => x.BlockerId,
                        principalTable: "WorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemDependencies_BlockedId",
                table: "WorkItemDependencies",
                column: "BlockedId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemDependencies_BlockerId",
                table: "WorkItemDependencies",
                column: "BlockerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemDependencies_TenantId_BlockedId_Type",
                table: "WorkItemDependencies",
                columns: new[] { "TenantId", "BlockedId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemDependencies_TenantId_BlockerId_BlockedId_Type",
                table: "WorkItemDependencies",
                columns: new[] { "TenantId", "BlockerId", "BlockedId", "Type" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemDependencies_TenantId_BlockerId_Type",
                table: "WorkItemDependencies",
                columns: new[] { "TenantId", "BlockerId", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkItemDependencies");
        }
    }
}
