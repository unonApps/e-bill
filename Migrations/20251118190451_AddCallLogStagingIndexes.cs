using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCallLogStagingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add indexes for CallLogStagings to improve query performance
            // These indexes speed up COUNT queries and filtering operations
            // Note: IX_CallLogStagings_BatchId already exists from foreign key

            // Composite index for BatchId + VerificationStatus - used for counting records by status
            migrationBuilder.CreateIndex(
                name: "IX_CallLogStagings_BatchId_VerificationStatus",
                table: "CallLogStagings",
                columns: new[] { "BatchId", "VerificationStatus" });

            // Index for HasAnomalies - used for filtering anomaly records
            migrationBuilder.CreateIndex(
                name: "IX_CallLogStagings_BatchId_HasAnomalies",
                table: "CallLogStagings",
                columns: new[] { "BatchId", "HasAnomalies" });

            // Index for VerificationDate - used for filtering verified today
            migrationBuilder.CreateIndex(
                name: "IX_CallLogStagings_VerificationDate",
                table: "CallLogStagings",
                column: "VerificationDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CallLogStagings_BatchId_VerificationStatus",
                table: "CallLogStagings");

            migrationBuilder.DropIndex(
                name: "IX_CallLogStagings_BatchId_HasAnomalies",
                table: "CallLogStagings");

            migrationBuilder.DropIndex(
                name: "IX_CallLogStagings_VerificationDate",
                table: "CallLogStagings");
        }
    }
}
