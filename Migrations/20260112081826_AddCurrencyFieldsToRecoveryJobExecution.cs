using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyFieldsToRecoveryJobExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmountRecoveredKSH",
                table: "RecoveryJobExecutions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmountRecoveredUSD",
                table: "RecoveryJobExecutions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalAmountRecoveredKSH",
                table: "RecoveryJobExecutions");

            migrationBuilder.DropColumn(
                name: "TotalAmountRecoveredUSD",
                table: "RecoveryJobExecutions");
        }
    }
}
