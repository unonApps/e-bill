using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIctsFieldsToSimRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedNo",
                table: "SimRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CollectionNotifiedDate",
                table: "SimRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IctsRemark",
                table: "SimRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LineType",
                table: "SimRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LineUsage",
                table: "SimRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousLines",
                table: "SimRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceRequestNo",
                table: "SimRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimCollectedBy",
                table: "SimRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SimCollectedDate",
                table: "SimRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimIssuedBy",
                table: "SimRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimPuk",
                table: "SimRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimSerialNo",
                table: "SimRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SpNotifiedDate",
                table: "SimRequests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedNo",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "CollectionNotifiedDate",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "IctsRemark",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "LineType",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "LineUsage",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "PreviousLines",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "ServiceRequestNo",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "SimCollectedBy",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "SimCollectedDate",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "SimIssuedBy",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "SimPuk",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "SimSerialNo",
                table: "SimRequests");

            migrationBuilder.DropColumn(
                name: "SpNotifiedDate",
                table: "SimRequests");
        }
    }
}
