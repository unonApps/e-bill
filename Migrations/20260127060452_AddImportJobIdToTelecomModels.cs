using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddImportJobIdToTelecomModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EbillUsers_Email",
                table: "EbillUsers");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneType",
                table: "UserPhones",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<Guid>(
                name: "ImportJobId",
                table: "Safaricom",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImportJobId",
                table: "PSTNs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImportJobId",
                table: "PrivateWires",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "EbillUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<Guid>(
                name: "ImportJobId",
                table: "Airtel",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Safaricom_ImportJobId",
                table: "Safaricom",
                column: "ImportJobId");

            migrationBuilder.CreateIndex(
                name: "IX_PSTNs_ImportJobId",
                table: "PSTNs",
                column: "ImportJobId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateWires_ImportJobId",
                table: "PrivateWires",
                column: "ImportJobId");

            migrationBuilder.CreateIndex(
                name: "IX_EbillUsers_Email",
                table: "EbillUsers",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Airtel_ImportJobId",
                table: "Airtel",
                column: "ImportJobId");

            migrationBuilder.AddForeignKey(
                name: "FK_Airtel_ImportJobs_ImportJobId",
                table: "Airtel",
                column: "ImportJobId",
                principalTable: "ImportJobs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateWires_ImportJobs_ImportJobId",
                table: "PrivateWires",
                column: "ImportJobId",
                principalTable: "ImportJobs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PSTNs_ImportJobs_ImportJobId",
                table: "PSTNs",
                column: "ImportJobId",
                principalTable: "ImportJobs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Safaricom_ImportJobs_ImportJobId",
                table: "Safaricom",
                column: "ImportJobId",
                principalTable: "ImportJobs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Airtel_ImportJobs_ImportJobId",
                table: "Airtel");

            migrationBuilder.DropForeignKey(
                name: "FK_PrivateWires_ImportJobs_ImportJobId",
                table: "PrivateWires");

            migrationBuilder.DropForeignKey(
                name: "FK_PSTNs_ImportJobs_ImportJobId",
                table: "PSTNs");

            migrationBuilder.DropForeignKey(
                name: "FK_Safaricom_ImportJobs_ImportJobId",
                table: "Safaricom");

            migrationBuilder.DropIndex(
                name: "IX_Safaricom_ImportJobId",
                table: "Safaricom");

            migrationBuilder.DropIndex(
                name: "IX_PSTNs_ImportJobId",
                table: "PSTNs");

            migrationBuilder.DropIndex(
                name: "IX_PrivateWires_ImportJobId",
                table: "PrivateWires");

            migrationBuilder.DropIndex(
                name: "IX_EbillUsers_Email",
                table: "EbillUsers");

            migrationBuilder.DropIndex(
                name: "IX_Airtel_ImportJobId",
                table: "Airtel");

            migrationBuilder.DropColumn(
                name: "ImportJobId",
                table: "Safaricom");

            migrationBuilder.DropColumn(
                name: "ImportJobId",
                table: "PSTNs");

            migrationBuilder.DropColumn(
                name: "ImportJobId",
                table: "PrivateWires");

            migrationBuilder.DropColumn(
                name: "ImportJobId",
                table: "Airtel");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneType",
                table: "UserPhones",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "EbillUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EbillUsers_Email",
                table: "EbillUsers",
                column: "Email",
                unique: true);
        }
    }
}
