using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetMasterFinal.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixBillingDiscountDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinimumOrderAmount",
                table: "BillingDiscounts",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinimumOrderAmount",
                table: "BillingDiscounts");
        }
    }
}
