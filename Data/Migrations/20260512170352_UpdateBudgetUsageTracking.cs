using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetMasterFinal.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBudgetUsageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SpentAmount",
                table: "Budgets",
                newName: "UsedAmount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UsedAmount",
                table: "Budgets",
                newName: "SpentAmount");
        }
    }
}
