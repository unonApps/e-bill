using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRecoveryConfigurationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNotificationEmail",
                table: "RecoveryConfiguration",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "RecoveryConfiguration",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultRevertDays",
                table: "RecoveryConfiguration",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableEmailNotifications",
                table: "RecoveryConfiguration",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "JobIntervalMinutes",
                table: "RecoveryConfiguration",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNotificationEmail",
                table: "RecoveryConfiguration");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "RecoveryConfiguration");

            migrationBuilder.DropColumn(
                name: "DefaultRevertDays",
                table: "RecoveryConfiguration");

            migrationBuilder.DropColumn(
                name: "EnableEmailNotifications",
                table: "RecoveryConfiguration");

            migrationBuilder.DropColumn(
                name: "JobIntervalMinutes",
                table: "RecoveryConfiguration");
        }
    }
}
