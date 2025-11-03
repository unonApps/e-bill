using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBatchDeadlines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalDeadline",
                table: "StagingBatches");

            migrationBuilder.DropColumn(
                name: "VerificationDeadline",
                table: "StagingBatches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDeadline",
                table: "StagingBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationDeadline",
                table: "StagingBatches",
                type: "datetime2",
                nullable: true);
        }
    }
}
