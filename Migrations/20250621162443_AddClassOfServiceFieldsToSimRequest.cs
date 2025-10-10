using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddClassOfServiceFieldsToSimRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HandsetAllowance",
                table: "SimRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileService",
                table: "SimRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileServiceAllowance",
                table: "SimRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorEmail",
                table: "SimRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorName",
                table: "SimRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorRemarks",
                table: "SimRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HandsetAllowance",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "MobileService",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "MobileServiceAllowance",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "SupervisorEmail",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "SupervisorName",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "SupervisorRemarks",
                table: "SimRequests");
        }
    }
}
