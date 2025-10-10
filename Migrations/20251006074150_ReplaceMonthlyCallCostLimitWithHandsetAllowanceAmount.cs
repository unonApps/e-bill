using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceMonthlyCallCostLimitWithHandsetAllowanceAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MonthlyCallCostLimit",
                table: "ClassOfServices",
                newName: "HandsetAllowanceAmount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HandsetAllowanceAmount",
                table: "ClassOfServices",
                newName: "MonthlyCallCostLimit");
        }
    }
}
