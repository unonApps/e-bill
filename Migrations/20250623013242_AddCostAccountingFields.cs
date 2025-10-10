using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCostAccountingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CostCenter",
                table: "RefundRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostObject",
                table: "RefundRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundCommitment",
                table: "RefundRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostCenter",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "CostObject",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "FundCommitment",
                table: "RefundRequests");
        }
    }
}
