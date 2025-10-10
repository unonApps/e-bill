using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimsUnitProcessingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimsActionDate",
                table: "RefundRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundUsdAmount",
                table: "RefundRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UmojaPaymentDocumentId",
                table: "RefundRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaimsActionDate",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "RefundUsdAmount",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "UmojaPaymentDocumentId",
                table: "RefundRequests");
        }
    }
}
