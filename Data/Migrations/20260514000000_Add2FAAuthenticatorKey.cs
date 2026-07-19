using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetMasterFinal.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add2FAAuthenticatorKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthenticatorKey",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthenticatorKey",
                table: "AspNetUsers");
        }
    }
}
