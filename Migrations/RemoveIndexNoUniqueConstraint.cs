using Microsoft.EntityFrameworkCore.Migrations;

namespace TAB.Web.Migrations
{
    /// <summary>
    /// Removes the unique constraint on IndexNo in SimRequests table
    /// to allow multiple SIM requests for the same staff member
    /// </summary>
    public partial class RemoveIndexNoUniqueConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the unique index
            migrationBuilder.DropIndex(
                name: "IX_SimRequests_IndexNo",
                table: "SimRequests");

            // Create a non-unique index for query performance
            migrationBuilder.CreateIndex(
                name: "IX_SimRequests_IndexNo",
                table: "SimRequests",
                column: "IndexNo",
                unique: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the non-unique index
            migrationBuilder.DropIndex(
                name: "IX_SimRequests_IndexNo",
                table: "SimRequests");

            // Recreate the unique index
            migrationBuilder.CreateIndex(
                name: "IX_SimRequests_IndexNo",
                table: "SimRequests",
                column: "IndexNo",
                unique: true);
        }
    }
}