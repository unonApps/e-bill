using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRefundRequestWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BudgetOfficerApprovalDate",
                table: "RefundRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BudgetOfficerEmail",
                table: "RefundRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BudgetOfficerName",
                table: "RefundRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BudgetOfficerNotes",
                table: "RefundRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BudgetOfficerRemarks",
                table: "RefundRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationDate",
                table: "RefundRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "RefundRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledBy",
                table: "RefundRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletionDate",
                table: "RefundRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionNotes",
                table: "RefundRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentApprovalDate",
                table: "RefundRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentApprovalNotes",
                table: "RefundRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentApprovalRemarks",
                table: "RefundRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentApproverEmail",
                table: "RefundRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentApproverName",
                table: "RefundRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "RefundRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StaffClaimsApprovalDate",
                table: "RefundRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffClaimsNotes",
                table: "RefundRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffClaimsOfficerEmail",
                table: "RefundRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffClaimsOfficerName",
                table: "RefundRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffClaimsRemarks",
                table: "RefundRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetOfficerApprovalDate",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "BudgetOfficerEmail",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "BudgetOfficerName",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "BudgetOfficerNotes",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "BudgetOfficerRemarks",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "CancellationDate",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "CompletionDate",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "CompletionNotes",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PaymentApprovalDate",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PaymentApprovalNotes",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PaymentApprovalRemarks",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PaymentApproverEmail",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PaymentApproverName",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "StaffClaimsApprovalDate",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "StaffClaimsNotes",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "StaffClaimsOfficerEmail",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "StaffClaimsOfficerName",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "StaffClaimsRemarks",
                table: "RefundRequests");
        }
    }
}
