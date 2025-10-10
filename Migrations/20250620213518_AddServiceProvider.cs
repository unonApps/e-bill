using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SPID = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ServiceProviderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SPMainCP = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SPMainCPEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SPOtherCPsEmail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SPStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProviders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_SPID",
                table: "ServiceProviders",
                column: "SPID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceProviders");
        }
    }
}
