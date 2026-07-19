using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetMasterFinal.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BudgetId",
                table: "ActualTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActualTransactions_BudgetId",
                table: "ActualTransactions",
                column: "BudgetId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActualTransactions_Budgets_BudgetId",
                table: "ActualTransactions",
                column: "BudgetId",
                principalTable: "Budgets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActualTransactions_Budgets_BudgetId",
                table: "ActualTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ActualTransactions_BudgetId",
                table: "ActualTransactions");

            migrationBuilder.DropColumn(
                name: "BudgetId",
                table: "ActualTransactions");
        }
    }
}
