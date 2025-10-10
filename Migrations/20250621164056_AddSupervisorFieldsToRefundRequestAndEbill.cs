using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSupervisorFieldsToRefundRequestAndEbill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SubmittedToSupervisor",
                table: "RefundRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Supervisor",
                table: "RefundRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SupervisorApprovalDate",
                table: "RefundRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorEmail",
                table: "RefundRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorName",
                table: "RefundRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorNotes",
                table: "RefundRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorRemarks",
                table: "RefundRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SubmittedToSupervisor",
                table: "Ebills",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Supervisor",
                table: "Ebills",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SupervisorApprovalDate",
                table: "Ebills",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorEmail",
                table: "Ebills",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorName",
                table: "Ebills",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorNotes",
                table: "Ebills",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorRemarks",
                table: "Ebills",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmittedToSupervisor",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "Supervisor",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "SupervisorApprovalDate",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "SupervisorEmail",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "SupervisorName",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "SupervisorNotes",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "SupervisorRemarks",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "SubmittedToSupervisor",
                table: "Ebills");

            migrationBuilder.DropColumn(
                name: "Supervisor",
                table: "Ebills");

            migrationBuilder.DropColumn(
                name: "SupervisorApprovalDate",
                table: "Ebills");

            migrationBuilder.DropColumn(
                name: "SupervisorEmail",
                table: "Ebills");

            migrationBuilder.DropColumn(
                name: "SupervisorName",
                table: "Ebills");

            migrationBuilder.DropColumn(
                name: "SupervisorNotes",
                table: "Ebills");

            migrationBuilder.DropColumn(
                name: "SupervisorRemarks",
                table: "Ebills");
        }
    }
}
