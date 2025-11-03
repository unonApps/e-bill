using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalPeriodAndRevertTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxRevertsAllowed",
                table: "RecoveryConfiguration",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "approval_period",
                table: "CallRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_revert_date",
                table: "CallRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "revert_count",
                table: "CallRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "revert_reason",
                table: "CallRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxRevertsAllowed",
                table: "RecoveryConfiguration");

            migrationBuilder.DropColumn(
                name: "approval_period",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "last_revert_date",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "revert_count",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "revert_reason",
                table: "CallRecords");
        }
    }
}
