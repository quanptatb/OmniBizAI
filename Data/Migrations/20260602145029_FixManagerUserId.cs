using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixManagerUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        // ManagerUserId1 was never actually in the SQL schema, so do not drop it.

        // The index and FK for ManagerUserId already exist in the database.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationUnits_AppUsers_ManagerUserId",
                table: "OrganizationUnits");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationUnits_ManagerUserId",
                table: "OrganizationUnits");

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerUserId1",
                table: "OrganizationUnits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnits_ManagerUserId1",
                table: "OrganizationUnits",
                column: "ManagerUserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationUnits_AppUsers_ManagerUserId1",
                table: "OrganizationUnits",
                column: "ManagerUserId1",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
