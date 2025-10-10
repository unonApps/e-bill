using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRefundRequestModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalDetails",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "ApprovedAmount",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "DeviceModel",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "IMEINumber",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "ProcessingNotes",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "RefundRequests");

            migrationBuilder.RenameColumn(
                name: "RefundReason",
                table: "RefundRequests",
                newName: "PurchaseReceiptPath");

            migrationBuilder.RenameColumn(
                name: "RefundAmount",
                table: "RefundRequests",
                newName: "DevicePurchaseAmount");

            migrationBuilder.RenameColumn(
                name: "ProcessedDate",
                table: "RefundRequests",
                newName: "PreviousDeviceReimbursedDate");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "RefundRequests",
                newName: "MobileService");

            migrationBuilder.RenameColumn(
                name: "DeviceType",
                table: "RefundRequests",
                newName: "ClassOfService");

            migrationBuilder.AddColumn<decimal>(
                name: "DeviceAllowance",
                table: "RefundRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DevicePurchaseCurrency",
                table: "RefundRequests",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IndexNo",
                table: "RefundRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MobileNumberAssignedTo",
                table: "RefundRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Office",
                table: "RefundRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OfficeExtension",
                table: "RefundRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Organization",
                table: "RefundRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryMobileNumber",
                table: "RefundRequests",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "RefundRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UmojaBankName",
                table: "RefundRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceAllowance",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "DevicePurchaseCurrency",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "IndexNo",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "MobileNumberAssignedTo",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "Office",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "OfficeExtension",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "Organization",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PrimaryMobileNumber",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "UmojaBankName",
                table: "RefundRequests");

            migrationBuilder.RenameColumn(
                name: "PurchaseReceiptPath",
                table: "RefundRequests",
                newName: "RefundReason");

            migrationBuilder.RenameColumn(
                name: "PreviousDeviceReimbursedDate",
                table: "RefundRequests",
                newName: "ProcessedDate");

            migrationBuilder.RenameColumn(
                name: "MobileService",
                table: "RefundRequests",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "DevicePurchaseAmount",
                table: "RefundRequests",
                newName: "RefundAmount");

            migrationBuilder.RenameColumn(
                name: "ClassOfService",
                table: "RefundRequests",
                newName: "DeviceType");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalDetails",
                table: "RefundRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedAmount",
                table: "RefundRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "RefundRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeviceModel",
                table: "RefundRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "RefundRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IMEINumber",
                table: "RefundRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "RefundRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcessingNotes",
                table: "RefundRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseDate",
                table: "RefundRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "RefundRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
