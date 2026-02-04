using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSupervisorEmailToCallLogVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add SupervisorEmail column
            migrationBuilder.AddColumn<string>(
                name: "SupervisorEmail",
                table: "CallLogVerifications",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            // Create index for performance
            migrationBuilder.CreateIndex(
                name: "IX_CallLogVerifications_SupervisorEmail",
                table: "CallLogVerifications",
                column: "SupervisorEmail");

            // Populate SupervisorEmail from existing data
            // If SupervisorIndexNumber contains @, it's already an email - use it directly
            // Otherwise, look up the email from EbillUsers using the index number
            migrationBuilder.Sql(@"
                UPDATE clv
                SET clv.SupervisorEmail =
                    CASE
                        WHEN clv.SupervisorIndexNumber LIKE '%@%' THEN clv.SupervisorIndexNumber
                        ELSE eu.Email
                    END
                FROM [ebill].[CallLogVerifications] clv
                LEFT JOIN [ebill].[EbillUsers] eu ON eu.IndexNumber = clv.SupervisorIndexNumber
                WHERE clv.SupervisorEmail IS NULL
                  AND clv.SupervisorIndexNumber IS NOT NULL
                  AND clv.SubmittedToSupervisor = 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CallLogVerifications_SupervisorEmail",
                table: "CallLogVerifications");

            migrationBuilder.DropColumn(
                name: "SupervisorEmail",
                table: "CallLogVerifications");
        }
    }
}
