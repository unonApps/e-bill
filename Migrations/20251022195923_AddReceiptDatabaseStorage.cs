using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptDatabaseStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PurchaseReceiptContentType",
                table: "RefundRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PurchaseReceiptData",
                table: "RefundRequests",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseReceiptFileName",
                table: "RefundRequests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseReceiptUploadDate",
                table: "RefundRequests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseReceiptContentType",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PurchaseReceiptData",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PurchaseReceiptFileName",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PurchaseReceiptUploadDate",
                table: "RefundRequests");
        }
    }
}
