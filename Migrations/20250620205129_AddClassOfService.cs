using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddClassOfService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassOfServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Class = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Service = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EligibleStaff = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AirtimeAllowance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DataAllowance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HandsetAIRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ServiceStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassOfServices", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassOfServices");
        }
    }
}
