using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEbillUserAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_EbillUsers_EbillUserId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_EbillUserId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "EbillUsers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasLoginAccount",
                table: "EbillUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LoginEnabled",
                table: "EbillUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EbillUserId",
                table: "AspNetUsers",
                column: "EbillUserId",
                unique: true,
                filter: "[EbillUserId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_EbillUsers_EbillUserId",
                table: "AspNetUsers",
                column: "EbillUserId",
                principalTable: "EbillUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_EbillUsers_EbillUserId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_EbillUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "EbillUsers");

            migrationBuilder.DropColumn(
                name: "HasLoginAccount",
                table: "EbillUsers");

            migrationBuilder.DropColumn(
                name: "LoginEnabled",
                table: "EbillUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EbillUserId",
                table: "AspNetUsers",
                column: "EbillUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_EbillUsers_EbillUserId",
                table: "AspNetUsers",
                column: "EbillUserId",
                principalTable: "EbillUsers",
                principalColumn: "Id");
        }
    }
}
