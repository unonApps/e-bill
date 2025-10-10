using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneStatusToUserPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SimRequestHistories_SimRequests_SimRequestId1",
                table: "SimRequestHistories");

            migrationBuilder.DropIndex(
                name: "IX_SimRequestHistories_SimRequestId1",
                table: "SimRequestHistories");

            migrationBuilder.DropColumn(
                name: "SimRequestId1",
                table: "SimRequestHistories");

            migrationBuilder.RenameColumn(
                name: "SupervisorActionDate",
                table: "CallLogVerifications",
                newName: "SupervisorApprovedDate");

            migrationBuilder.RenameColumn(
                name: "SupervisorAction",
                table: "CallLogVerifications",
                newName: "SupervisorApprovalStatus");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "UserPhones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "OverageAmount",
                table: "CallLogVerifications",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "OverageJustified",
                table: "CallLogVerifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PaymentAssignmentId",
                table: "CallLogVerifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorApprovedBy",
                table: "CallLogVerifications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CallLogVerifications_PaymentAssignmentId",
                table: "CallLogVerifications",
                column: "PaymentAssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogVerifications_CallLogPaymentAssignments_PaymentAssignmentId",
                table: "CallLogVerifications",
                column: "PaymentAssignmentId",
                principalTable: "CallLogPaymentAssignments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallLogVerifications_CallLogPaymentAssignments_PaymentAssignmentId",
                table: "CallLogVerifications");

            migrationBuilder.DropIndex(
                name: "IX_CallLogVerifications_PaymentAssignmentId",
                table: "CallLogVerifications");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UserPhones");

            migrationBuilder.DropColumn(
                name: "OverageAmount",
                table: "CallLogVerifications");

            migrationBuilder.DropColumn(
                name: "OverageJustified",
                table: "CallLogVerifications");

            migrationBuilder.DropColumn(
                name: "PaymentAssignmentId",
                table: "CallLogVerifications");

            migrationBuilder.DropColumn(
                name: "SupervisorApprovedBy",
                table: "CallLogVerifications");

            migrationBuilder.RenameColumn(
                name: "SupervisorApprovedDate",
                table: "CallLogVerifications",
                newName: "SupervisorActionDate");

            migrationBuilder.RenameColumn(
                name: "SupervisorApprovalStatus",
                table: "CallLogVerifications",
                newName: "SupervisorAction");

            migrationBuilder.AddColumn<int>(
                name: "SimRequestId1",
                table: "SimRequestHistories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimRequestHistories_SimRequestId1",
                table: "SimRequestHistories",
                column: "SimRequestId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SimRequestHistories_SimRequests_SimRequestId1",
                table: "SimRequestHistories",
                column: "SimRequestId1",
                principalTable: "SimRequests",
                principalColumn: "Id");
        }
    }
}
