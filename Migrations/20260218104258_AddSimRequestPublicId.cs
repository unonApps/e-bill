using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSimRequestPublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                schema: "ebill",
                table: "SimRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.CreateIndex(
                name: "IX_SimRequests_PublicId",
                schema: "ebill",
                table: "SimRequests",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SimRequests_PublicId",
                schema: "ebill",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "PublicId",
                schema: "ebill",
                table: "SimRequests");
        }
    }
}
