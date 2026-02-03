using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddClassOfServiceVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "ClassOfServices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "ClassOfServices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentClassOfServiceId",
                table: "ClassOfServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ClassOfServices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ClassOfServices_ParentClassOfServiceId",
                table: "ClassOfServices",
                column: "ParentClassOfServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassOfServices_ClassOfServices_ParentClassOfServiceId",
                table: "ClassOfServices",
                column: "ParentClassOfServiceId",
                principalTable: "ClassOfServices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassOfServices_ClassOfServices_ParentClassOfServiceId",
                table: "ClassOfServices");

            migrationBuilder.DropIndex(
                name: "IX_ClassOfServices_ParentClassOfServiceId",
                table: "ClassOfServices");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "ClassOfServices");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "ClassOfServices");

            migrationBuilder.DropColumn(
                name: "ParentClassOfServiceId",
                table: "ClassOfServices");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ClassOfServices");
        }
    }
}
