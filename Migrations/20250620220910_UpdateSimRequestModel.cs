using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSimRequestModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SimRequests_ClassOfServices_ClassOfServiceId",
                table: "SimRequests");

            migrationBuilder.DropIndex(
                name: "IX_SimRequests_ClassOfServiceId",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "ClassOfServiceId",
                table: "SimRequests");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "SimRequests",
                newName: "IndexNo");

            migrationBuilder.RenameColumn(
                name: "Justification",
                table: "SimRequests",
                newName: "SupervisorNotes");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "SimRequests",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "SimRequests",
                newName: "OfficialEmail");

            migrationBuilder.RenameColumn(
                name: "Department",
                table: "SimRequests",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "AdditionalNotes",
                table: "SimRequests",
                newName: "Remarks");

            migrationBuilder.AddColumn<string>(
                name: "FunctionalTitle",
                table: "SimRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "SimRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Office",
                table: "SimRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OfficeExtension",
                table: "SimRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Organization",
                table: "SimRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreviouslyAssignedLines",
                table: "SimRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SubmittedToSupervisor",
                table: "SimRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Supervisor",
                table: "SimRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SupervisorApprovalDate",
                table: "SimRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimRequests_IndexNo",
                table: "SimRequests",
                column: "IndexNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SimRequests_IndexNo",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "FunctionalTitle",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "Office",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "OfficeExtension",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "Organization",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "PreviouslyAssignedLines",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "SubmittedToSupervisor",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "Supervisor",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "SupervisorApprovalDate",
                table: "SimRequests");

            migrationBuilder.RenameColumn(
                name: "SupervisorNotes",
                table: "SimRequests",
                newName: "Justification");

            migrationBuilder.RenameColumn(
                name: "Remarks",
                table: "SimRequests",
                newName: "AdditionalNotes");

            migrationBuilder.RenameColumn(
                name: "OfficialEmail",
                table: "SimRequests",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "SimRequests",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "IndexNo",
                table: "SimRequests",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "SimRequests",
                newName: "Department");

            migrationBuilder.AddColumn<int>(
                name: "ClassOfServiceId",
                table: "SimRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SimRequests_ClassOfServiceId",
                table: "SimRequests",
                column: "ClassOfServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_SimRequests_ClassOfServices_ClassOfServiceId",
                table: "SimRequests",
                column: "ClassOfServiceId",
                principalTable: "ClassOfServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
