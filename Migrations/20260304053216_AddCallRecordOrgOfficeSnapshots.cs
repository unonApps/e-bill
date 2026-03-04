using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCallRecordOrgOfficeSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add columns with IF NOT EXISTS guards (safe for re-runs after partial failure)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ebill.CallRecords') AND name = 'snapshot_org_id')
    ALTER TABLE ebill.CallRecords ADD snapshot_org_id int NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ebill.CallRecords') AND name = 'snapshot_org_name')
    ALTER TABLE ebill.CallRecords ADD snapshot_org_name nvarchar(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ebill.CallRecords') AND name = 'snapshot_office_id')
    ALTER TABLE ebill.CallRecords ADD snapshot_office_id int NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ebill.CallRecords') AND name = 'snapshot_office_name')
    ALTER TABLE ebill.CallRecords ADD snapshot_office_name nvarchar(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ebill.CallRecords') AND name = 'snapshot_suboffice_id')
    ALTER TABLE ebill.CallRecords ADD snapshot_suboffice_id int NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ebill.CallRecords') AND name = 'snapshot_suboffice_name')
    ALTER TABLE ebill.CallRecords ADD snapshot_suboffice_name nvarchar(100) NULL;
");
            // Backfill is run separately post-migration due to large table size.
            // See: Migrations/backfill_snapshot_org_office.sql
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "snapshot_org_id", schema: "ebill", table: "CallRecords");
            migrationBuilder.DropColumn(name: "snapshot_org_name", schema: "ebill", table: "CallRecords");
            migrationBuilder.DropColumn(name: "snapshot_office_id", schema: "ebill", table: "CallRecords");
            migrationBuilder.DropColumn(name: "snapshot_office_name", schema: "ebill", table: "CallRecords");
            migrationBuilder.DropColumn(name: "snapshot_suboffice_id", schema: "ebill", table: "CallRecords");
            migrationBuilder.DropColumn(name: "snapshot_suboffice_name", schema: "ebill", table: "CallRecords");
        }
    }
}
