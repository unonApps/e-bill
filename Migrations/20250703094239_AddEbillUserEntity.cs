using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEbillUserEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SimRequestId1",
                table: "SimRequestHistories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EbillUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IndexNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OfficialMobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssuedDeviceID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClassOfService = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Organization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Office = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SupervisorIndexNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SupervisorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SupervisorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EbillUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimRequestHistories_SimRequestId1",
                table: "SimRequestHistories",
                column: "SimRequestId1");

            migrationBuilder.CreateIndex(
                name: "IX_EbillUsers_Email",
                table: "EbillUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EbillUsers_IndexNumber",
                table: "EbillUsers",
                column: "IndexNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SimRequestHistories_SimRequests_SimRequestId1",
                table: "SimRequestHistories",
                column: "SimRequestId1",
                principalTable: "SimRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SimRequestHistories_SimRequests_SimRequestId1",
                table: "SimRequestHistories");

            migrationBuilder.DropTable(
                name: "EbillUsers");

            migrationBuilder.DropIndex(
                name: "IX_SimRequestHistories_SimRequestId1",
                table: "SimRequestHistories");

            migrationBuilder.DropColumn(
                name: "SimRequestId1",
                table: "SimRequestHistories");
        }
    }
}
