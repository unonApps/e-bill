using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCoveringIndexesForMyCallLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CallLogVerifications_SubmittedToSupervisor_CallRecordId_ApprovalStatus",
                schema: "ebill",
                table: "CallLogVerifications",
                columns: new[] { "SubmittedToSupervisor", "CallRecordId", "ApprovalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_CallLogPaymentAssignments_AssignedFrom_AssignmentStatus_CallRecordId",
                schema: "ebill",
                table: "CallLogPaymentAssignments",
                columns: new[] { "AssignedFrom", "AssignmentStatus", "CallRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_CallLogPaymentAssignments_AssignedTo_AssignmentStatus_CallRecordId",
                schema: "ebill",
                table: "CallLogPaymentAssignments",
                columns: new[] { "AssignedTo", "AssignmentStatus", "CallRecordId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CallLogVerifications_SubmittedToSupervisor_CallRecordId_ApprovalStatus",
                schema: "ebill",
                table: "CallLogVerifications");

            migrationBuilder.DropIndex(
                name: "IX_CallLogPaymentAssignments_AssignedFrom_AssignmentStatus_CallRecordId",
                schema: "ebill",
                table: "CallLogPaymentAssignments");

            migrationBuilder.DropIndex(
                name: "IX_CallLogPaymentAssignments_AssignedTo_AssignmentStatus_CallRecordId",
                schema: "ebill",
                table: "CallLogPaymentAssignments");
        }
    }
}
