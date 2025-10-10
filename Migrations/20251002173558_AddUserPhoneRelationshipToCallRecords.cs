using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPhoneRelationshipToCallRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserPhoneId",
                table: "CallRecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_UserPhoneId",
                table: "CallRecords",
                column: "UserPhoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_CallRecords_UserPhones_UserPhoneId",
                table: "CallRecords",
                column: "UserPhoneId",
                principalTable: "UserPhones",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallRecords_UserPhones_UserPhoneId",
                table: "CallRecords");

            migrationBuilder.DropIndex(
                name: "IX_CallRecords_UserPhoneId",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "UserPhoneId",
                table: "CallRecords");
        }
    }
}
