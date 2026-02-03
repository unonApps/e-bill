using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoCreatedFieldsToEbillUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutoCreated",
                table: "EbillUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "AutoCreatedFromImportJobId",
                table: "EbillUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EbillUsers_IsAutoCreated",
                table: "EbillUsers",
                column: "IsAutoCreated");

            // Update existing auto-created users based on Location field
            migrationBuilder.Sql(@"
                UPDATE EbillUsers
                SET IsAutoCreated = 1
                WHERE Location LIKE '%Auto-created from PSTN/PW import%'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EbillUsers_IsAutoCreated",
                table: "EbillUsers");

            migrationBuilder.DropColumn(
                name: "AutoCreatedFromImportJobId",
                table: "EbillUsers");

            migrationBuilder.DropColumn(
                name: "IsAutoCreated",
                table: "EbillUsers");
        }
    }
}
