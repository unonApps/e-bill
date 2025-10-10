using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentStatusToCallRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "assignment_status",
                table: "CallRecords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            // Set all existing records to "None" status (belongs to original phone owner)
            migrationBuilder.Sql(
                @"UPDATE CallRecords
                  SET assignment_status = 'None'
                  WHERE assignment_status IS NULL OR assignment_status = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "assignment_status",
                table: "CallRecords");
        }
    }
}
