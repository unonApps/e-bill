using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffBillingReportStoredProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Covering index for the Staff Billing Report grouped query
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CallRecords_StaffBillingReport' AND object_id = OBJECT_ID('ebill.CallRecords'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CallRecords_StaffBillingReport
    ON ebill.CallRecords (call_year, call_month, ext_resp_index)
    INCLUDE (verification_type, call_cost_kshs, call_cost_usd,
             recovery_status, recovery_amount,
             snapshot_org_id, snapshot_org_name,
             snapshot_office_id, snapshot_office_name);
END
");

            // Stored procedure: Staff Billing Report with KPIs + paginated rows
            migrationBuilder.Sql(@"
CREATE PROCEDURE ebill.sp_StaffBillingReport
    @Year INT,
    @Month INT = 0,
    @OrgId INT = NULL,
    @OfficeId INT = NULL,
    @SortBy VARCHAR(30) = 'FullName',
    @SortDir VARCHAR(4) = 'asc',
    @PageNumber INT = 1,
    @PageSize INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Materialize grouped staff data into temp table
    SELECT
        cr.ext_resp_index AS IndexNumber,
        COALESCE(MAX(cr.snapshot_org_name), MAX(o.Name), '-') AS OrganizationName,
        COALESCE(MAX(cr.snapshot_office_name), MAX(ofc.Name), '-') AS OfficeName,
        COALESCE(MAX(eu.FirstName + ' ' + eu.LastName), 'Unknown') AS FullName,
        -- Personal
        COUNT(CASE WHEN cr.verification_type = 'Personal' THEN 1 END) AS PersonalCallCount,
        ISNULL(SUM(CASE WHEN cr.verification_type = 'Personal' THEN cr.call_cost_kshs ELSE 0 END), 0) AS PersonalCallCostKES,
        ISNULL(SUM(CASE WHEN cr.verification_type = 'Personal' THEN cr.call_cost_usd ELSE 0 END), 0) AS PersonalCallCostUSD,
        -- Official
        COUNT(CASE WHEN cr.verification_type = 'Official' THEN 1 END) AS OfficialCallCount,
        ISNULL(SUM(CASE WHEN cr.verification_type = 'Official' THEN cr.call_cost_kshs ELSE 0 END), 0) AS OfficialCallCostKES,
        ISNULL(SUM(CASE WHEN cr.verification_type = 'Official' THEN cr.call_cost_usd ELSE 0 END), 0) AS OfficialCallCostUSD,
        -- Recovered
        COUNT(CASE WHEN cr.recovery_status = 'Processed' THEN 1 END) AS RecoveredCallCount,
        ISNULL(SUM(CASE WHEN cr.recovery_status = 'Processed' THEN cr.recovery_amount ELSE 0 END), 0) AS RecoveredCallCostKES,
        -- Totals
        COUNT(*) AS TotalCallCount,
        ISNULL(SUM(cr.call_cost_kshs), 0) AS TotalCostKES,
        ISNULL(SUM(cr.call_cost_usd), 0) AS TotalCostUSD
    INTO #StaffData
    FROM ebill.CallRecords cr
    LEFT JOIN ebill.EbillUsers eu ON cr.ext_resp_index = eu.IndexNumber
    LEFT JOIN ebill.Organizations o ON eu.OrganizationId = o.Id
    LEFT JOIN ebill.Offices ofc ON eu.OfficeId = ofc.Id
    WHERE cr.call_year = @Year
      AND cr.ext_resp_index IS NOT NULL
      AND (@Month = 0 OR cr.call_month = @Month)
      AND (@OrgId IS NULL OR COALESCE(cr.snapshot_org_id, eu.OrganizationId) = @OrgId)
      AND (@OfficeId IS NULL OR COALESCE(cr.snapshot_office_id, eu.OfficeId) = @OfficeId)
    GROUP BY cr.ext_resp_index;

    -- Result set 1: KPI totals
    SELECT
        COUNT(*) AS TotalStaffCount,
        ISNULL(SUM(TotalCallCount), 0) AS TotalCallCount,
        ISNULL(SUM(PersonalCallCostKES), 0) AS TotalPersonalCostKES,
        ISNULL(SUM(OfficialCallCostKES), 0) AS TotalOfficialCostKES,
        ISNULL(SUM(RecoveredCallCostKES), 0) AS TotalRecoveredCostKES
    FROM #StaffData;

    -- Result set 2: Staff rows (paginated or all for export)
    IF @PageSize > 0
    BEGIN
        SELECT *, COUNT(*) OVER() AS TotalCount
        FROM #StaffData
        ORDER BY
            CASE WHEN @SortBy = 'PersonalCallCostKES' AND @SortDir = 'asc' THEN PersonalCallCostKES END ASC,
            CASE WHEN @SortBy = 'PersonalCallCostKES' AND @SortDir = 'desc' THEN PersonalCallCostKES END DESC,
            CASE WHEN @SortBy = 'OfficialCallCostKES' AND @SortDir = 'asc' THEN OfficialCallCostKES END ASC,
            CASE WHEN @SortBy = 'OfficialCallCostKES' AND @SortDir = 'desc' THEN OfficialCallCostKES END DESC,
            CASE WHEN @SortBy = 'RecoveredCallCostKES' AND @SortDir = 'asc' THEN RecoveredCallCostKES END ASC,
            CASE WHEN @SortBy = 'RecoveredCallCostKES' AND @SortDir = 'desc' THEN RecoveredCallCostKES END DESC,
            CASE WHEN @SortBy = 'TotalCostKES' AND @SortDir = 'asc' THEN TotalCostKES END ASC,
            CASE WHEN @SortBy = 'TotalCostKES' AND @SortDir = 'desc' THEN TotalCostKES END DESC,
            CASE WHEN @SortBy = 'FullName' AND @SortDir = 'desc' THEN FullName END DESC,
            FullName ASC
        OFFSET (@PageNumber - 1) * @PageSize ROWS
        FETCH NEXT @PageSize ROWS ONLY;
    END
    ELSE
    BEGIN
        SELECT *, 0 AS TotalCount
        FROM #StaffData
        ORDER BY
            CASE WHEN @SortBy = 'PersonalCallCostKES' AND @SortDir = 'asc' THEN PersonalCallCostKES END ASC,
            CASE WHEN @SortBy = 'PersonalCallCostKES' AND @SortDir = 'desc' THEN PersonalCallCostKES END DESC,
            CASE WHEN @SortBy = 'OfficialCallCostKES' AND @SortDir = 'asc' THEN OfficialCallCostKES END ASC,
            CASE WHEN @SortBy = 'OfficialCallCostKES' AND @SortDir = 'desc' THEN OfficialCallCostKES END DESC,
            CASE WHEN @SortBy = 'RecoveredCallCostKES' AND @SortDir = 'asc' THEN RecoveredCallCostKES END ASC,
            CASE WHEN @SortBy = 'RecoveredCallCostKES' AND @SortDir = 'desc' THEN RecoveredCallCostKES END DESC,
            CASE WHEN @SortBy = 'TotalCostKES' AND @SortDir = 'asc' THEN TotalCostKES END ASC,
            CASE WHEN @SortBy = 'TotalCostKES' AND @SortDir = 'desc' THEN TotalCostKES END DESC,
            CASE WHEN @SortBy = 'FullName' AND @SortDir = 'desc' THEN FullName END DESC,
            FullName ASC;
    END

    DROP TABLE #StaffData;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ebill.sp_StaffBillingReport;");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CallRecords_StaffBillingReport' AND object_id = OBJECT_ID('ebill.CallRecords'))
    DROP INDEX IX_CallRecords_StaffBillingReport ON ebill.CallRecords;
");
        }
    }
}
