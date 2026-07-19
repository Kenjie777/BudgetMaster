using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetMasterFinal.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoStageApprovalToBudgetRequestFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "BudgetRequests",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinancialApprovalDate",
                table: "BudgetRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinancialRejectionDate",
                table: "BudgetRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinancialValidationNotes",
                table: "BudgetRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinanciallyApprovedByUserId",
                table: "BudgetRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinanciallyRejectedByUserId",
                table: "BudgetRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OperationalApprovalDate",
                table: "BudgetRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationalApprovalNotes",
                table: "BudgetRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationallyApprovedByUserId",
                table: "BudgetRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetRequests_FinanciallyApprovedByUserId",
                table: "BudgetRequests",
                column: "FinanciallyApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetRequests_FinanciallyRejectedByUserId",
                table: "BudgetRequests",
                column: "FinanciallyRejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetRequests_OperationallyApprovedByUserId",
                table: "BudgetRequests",
                column: "OperationallyApprovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetRequests_Users_FinanciallyApprovedByUserId",
                table: "BudgetRequests",
                column: "FinanciallyApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetRequests_Users_FinanciallyRejectedByUserId",
                table: "BudgetRequests",
                column: "FinanciallyRejectedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetRequests_Users_OperationallyApprovedByUserId",
                table: "BudgetRequests",
                column: "OperationallyApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetRequests_Users_FinanciallyApprovedByUserId",
                table: "BudgetRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_BudgetRequests_Users_FinanciallyRejectedByUserId",
                table: "BudgetRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_BudgetRequests_Users_OperationallyApprovedByUserId",
                table: "BudgetRequests");

            migrationBuilder.DropIndex(
                name: "IX_BudgetRequests_FinanciallyApprovedByUserId",
                table: "BudgetRequests");

            migrationBuilder.DropIndex(
                name: "IX_BudgetRequests_FinanciallyRejectedByUserId",
                table: "BudgetRequests");

            migrationBuilder.DropIndex(
                name: "IX_BudgetRequests_OperationallyApprovedByUserId",
                table: "BudgetRequests");

            migrationBuilder.DropColumn(
                name: "FinancialApprovalDate",
                table: "BudgetRequests");

            migrationBuilder.DropColumn(
                name: "FinancialRejectionDate",
                table: "BudgetRequests");

            migrationBuilder.DropColumn(
                name: "FinancialValidationNotes",
                table: "BudgetRequests");

            migrationBuilder.DropColumn(
                name: "FinanciallyApprovedByUserId",
                table: "BudgetRequests");

            migrationBuilder.DropColumn(
                name: "FinanciallyRejectedByUserId",
                table: "BudgetRequests");

            migrationBuilder.DropColumn(
                name: "OperationalApprovalDate",
                table: "BudgetRequests");

            migrationBuilder.DropColumn(
                name: "OperationalApprovalNotes",
                table: "BudgetRequests");

            migrationBuilder.DropColumn(
                name: "OperationallyApprovedByUserId",
                table: "BudgetRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "BudgetRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);
        }
    }
}
