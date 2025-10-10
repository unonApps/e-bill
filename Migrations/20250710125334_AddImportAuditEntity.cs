using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddImportAuditEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    TotalRecords = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedCount = table.Column<int>(type: "int", nullable: false),
                    ImportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessingTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    DetailedResults = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SummaryMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImportOptions = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportAudits_ImportDate",
                table: "ImportAudits",
                column: "ImportDate");

            migrationBuilder.CreateIndex(
                name: "IX_ImportAudits_ImportType",
                table: "ImportAudits",
                column: "ImportType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportAudits");
        }
    }
}
