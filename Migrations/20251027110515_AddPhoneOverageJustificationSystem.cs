using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneOverageJustificationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhoneOverageJustifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserPhoneId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    AllowanceLimit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalUsage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OverageAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    JustificationText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneOverageJustifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneOverageJustifications_UserPhones_UserPhoneId",
                        column: x => x.UserPhoneId,
                        principalTable: "UserPhones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhoneOverageDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhoneOverageJustificationId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneOverageDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneOverageDocuments_PhoneOverageJustifications_PhoneOverageJustificationId",
                        column: x => x.PhoneOverageJustificationId,
                        principalTable: "PhoneOverageJustifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneOverageDocuments_PhoneOverageJustificationId",
                table: "PhoneOverageDocuments",
                column: "PhoneOverageJustificationId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneOverageJustifications_ApprovalStatus",
                table: "PhoneOverageJustifications",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneOverageJustifications_Month_Year",
                table: "PhoneOverageJustifications",
                columns: new[] { "Month", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneOverageJustifications_UserPhoneId",
                table: "PhoneOverageJustifications",
                column: "UserPhoneId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneOverageJustifications_UserPhoneId_Month_Year",
                table: "PhoneOverageJustifications",
                columns: new[] { "UserPhoneId", "Month", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneOverageDocuments");

            migrationBuilder.DropTable(
                name: "PhoneOverageJustifications");
        }
    }
}
