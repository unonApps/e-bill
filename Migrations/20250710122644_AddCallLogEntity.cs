using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCallLogEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CallLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubAccountNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubAccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MSISDN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TaxInvoiceSummaryNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NetAccessFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetUsageLessTax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LessTaxes = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VAT16 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Excise15 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GrossTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EbillUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImportedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallLogs_EbillUsers_EbillUserId",
                        column: x => x.EbillUserId,
                        principalTable: "EbillUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_EbillUserId",
                table: "CallLogs",
                column: "EbillUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_MSISDN",
                table: "CallLogs",
                column: "MSISDN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CallLogs");
        }
    }
}
