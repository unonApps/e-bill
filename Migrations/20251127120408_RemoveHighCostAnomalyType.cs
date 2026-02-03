using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHighCostAnomalyType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete the HIGH_COST anomaly type record
            migrationBuilder.Sql("DELETE FROM AnomalyTypes WHERE Code = 'HIGH_COST'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-insert the HIGH_COST anomaly type if rolling back
            migrationBuilder.Sql(@"
                INSERT INTO AnomalyTypes (Code, Name, Description, Severity, AutoReject, IsActive)
                VALUES ('HIGH_COST', 'Unusually High Cost', 'Call cost exceeds threshold', 2, 0, 1)
            ");
        }
    }
}
