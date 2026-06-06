using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationRequestAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationRequestAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationRequestAssignments", x => x.Id);
                    table.CheckConstraint("CK_OperationRequestAssignments_Target", "([AssignedUserId] IS NOT NULL AND [OrganizationUnitId] IS NULL) OR ([AssignedUserId] IS NULL AND [OrganizationUnitId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_OperationRequestAssignments_AppUsers_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationRequestAssignments_OperationRequests_OperationRequestId",
                        column: x => x.OperationRequestId,
                        principalTable: "OperationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationRequestAssignments_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationRequestAssignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationRequestAssignments_AssignedUserId",
                table: "OperationRequestAssignments",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationRequestAssignments_OperationRequestId",
                table: "OperationRequestAssignments",
                column: "OperationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationRequestAssignments_OrganizationUnitId",
                table: "OperationRequestAssignments",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationRequestAssignments_TenantId_OperationRequestId_Role_IsActive",
                table: "OperationRequestAssignments",
                columns: new[] { "TenantId", "OperationRequestId", "Role", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationRequestAssignments");
        }
    }
}
