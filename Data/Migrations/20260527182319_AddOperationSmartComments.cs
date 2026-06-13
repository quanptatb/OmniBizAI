using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationSmartComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OperationComments_TenantId",
                table: "OperationComments");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentCommentId",
                table: "OperationComments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "OperationComments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Note");

            migrationBuilder.CreateIndex(
                name: "IX_OperationComments_ParentCommentId",
                table: "OperationComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationComments_TenantId_OperationRequestId_CreatedAt",
                table: "OperationComments",
                columns: new[] { "TenantId", "OperationRequestId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_OperationComments_OperationComments_ParentCommentId",
                table: "OperationComments",
                column: "ParentCommentId",
                principalTable: "OperationComments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OperationComments_OperationComments_ParentCommentId",
                table: "OperationComments");

            migrationBuilder.DropIndex(
                name: "IX_OperationComments_ParentCommentId",
                table: "OperationComments");

            migrationBuilder.DropIndex(
                name: "IX_OperationComments_TenantId_OperationRequestId_CreatedAt",
                table: "OperationComments");

            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                table: "OperationComments");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "OperationComments");

            migrationBuilder.CreateIndex(
                name: "IX_OperationComments_TenantId",
                table: "OperationComments",
                column: "TenantId");
        }
    }
}
