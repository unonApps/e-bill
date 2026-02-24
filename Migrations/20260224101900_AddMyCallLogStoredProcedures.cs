using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMyCallLogStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SP 1: Get Extension Groups (paginated) with submission/assignment counts and CoS
            migrationBuilder.Sql(@"
CREATE PROCEDURE ebill.sp_GetMyCallLogExtensionGroups
    @UserIndexNumber NVARCHAR(50) = NULL,
    @IsAdmin BIT = 0,
    @FilterMonth INT = NULL,
    @FilterYear INT = NULL,
    @FilterStartDate DATETIME2 = NULL,
    @FilterEndDate DATETIME2 = NULL,
    @FilterMinCost DECIMAL(18,4) = NULL,
    @FilterStatus NVARCHAR(20) = NULL,
    @FilterAssignmentType NVARCHAR(20) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 25
AS
BEGIN
    SET NOCOUNT ON;

    -- Step 1: Collect filtered call IDs into a temp table (single scan of CallRecords)
    CREATE TABLE #FilteredCalls (Id INT PRIMARY KEY);

    IF @IsAdmin = 1 AND @UserIndexNumber IS NULL
    BEGIN
        INSERT INTO #FilteredCalls (Id)
        SELECT cr.Id
        FROM ebill.CallRecords cr
        WHERE (@FilterMonth IS NULL OR cr.call_month = @FilterMonth)
          AND (@FilterYear IS NULL OR cr.call_year = @FilterYear)
          AND (@FilterStartDate IS NULL OR cr.call_date >= @FilterStartDate)
          AND (@FilterEndDate IS NULL OR cr.call_date <= @FilterEndDate)
          AND (@FilterMinCost IS NULL OR cr.call_cost_usd >= @FilterMinCost)
          AND (@FilterStatus IS NULL
               OR (@FilterStatus = 'unverified' AND cr.call_ver_ind = 0)
               OR (@FilterStatus = 'verified' AND cr.call_ver_ind = 1)
               OR (@FilterStatus = 'approved' AND cr.supervisor_approval_status = 'Approved')
               OR (@FilterStatus = 'pending' AND cr.call_ver_ind = 1 AND cr.supervisor_approval_status = 'Pending')
               OR (@FilterStatus = 'overdue' AND cr.call_ver_ind = 0 AND cr.verification_period IS NOT NULL AND cr.verification_period < SYSUTCDATETIME()));
    END
    ELSE IF @UserIndexNumber IS NOT NULL
    BEGIN
        IF @FilterAssignmentType = 'own'
        BEGIN
            INSERT INTO #FilteredCalls (Id)
            SELECT cr.Id FROM ebill.CallRecords cr
            WHERE cr.ext_resp_index = @UserIndexNumber
              AND NOT EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a WHERE a.CallRecordId = cr.Id AND a.AssignedFrom = @UserIndexNumber AND a.AssignmentStatus = 'Accepted')
              AND (cr.payment_assignment_id IS NULL
                   OR NOT EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a WHERE a.CallRecordId = cr.Id AND a.AssignedTo = @UserIndexNumber AND a.AssignmentStatus IN ('Pending','Accepted')))
              AND (@FilterMonth IS NULL OR cr.call_month = @FilterMonth)
              AND (@FilterYear IS NULL OR cr.call_year = @FilterYear)
              AND (@FilterStartDate IS NULL OR cr.call_date >= @FilterStartDate)
              AND (@FilterEndDate IS NULL OR cr.call_date <= @FilterEndDate)
              AND (@FilterMinCost IS NULL OR cr.call_cost_usd >= @FilterMinCost)
              AND (@FilterStatus IS NULL
                   OR (@FilterStatus = 'unverified' AND cr.call_ver_ind = 0)
                   OR (@FilterStatus = 'verified' AND cr.call_ver_ind = 1)
                   OR (@FilterStatus = 'approved' AND cr.supervisor_approval_status = 'Approved')
                   OR (@FilterStatus = 'pending' AND cr.call_ver_ind = 1 AND cr.supervisor_approval_status = 'Pending')
                   OR (@FilterStatus = 'overdue' AND cr.call_ver_ind = 0 AND cr.verification_period IS NOT NULL AND cr.verification_period < SYSUTCDATETIME()));
        END
        ELSE IF @FilterAssignmentType = 'assigned'
        BEGIN
            INSERT INTO #FilteredCalls (Id)
            SELECT cr.Id FROM ebill.CallRecords cr
            WHERE EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a WHERE a.CallRecordId = cr.Id AND a.AssignedTo = @UserIndexNumber AND a.AssignmentStatus IN ('Pending','Accepted'))
              AND (@FilterMonth IS NULL OR cr.call_month = @FilterMonth)
              AND (@FilterYear IS NULL OR cr.call_year = @FilterYear)
              AND (@FilterStartDate IS NULL OR cr.call_date >= @FilterStartDate)
              AND (@FilterEndDate IS NULL OR cr.call_date <= @FilterEndDate)
              AND (@FilterMinCost IS NULL OR cr.call_cost_usd >= @FilterMinCost)
              AND (@FilterStatus IS NULL
                   OR (@FilterStatus = 'unverified' AND cr.call_ver_ind = 0)
                   OR (@FilterStatus = 'verified' AND cr.call_ver_ind = 1)
                   OR (@FilterStatus = 'approved' AND cr.supervisor_approval_status = 'Approved')
                   OR (@FilterStatus = 'pending' AND cr.call_ver_ind = 1 AND cr.supervisor_approval_status = 'Pending')
                   OR (@FilterStatus = 'overdue' AND cr.call_ver_ind = 0 AND cr.verification_period IS NOT NULL AND cr.verification_period < SYSUTCDATETIME()));
        END
        ELSE
        BEGIN
            -- Default: own (excluding accepted outgoing) + incoming assigned
            INSERT INTO #FilteredCalls (Id)
            SELECT cr.Id FROM ebill.CallRecords cr
            WHERE ((cr.ext_resp_index = @UserIndexNumber
                    AND NOT EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a WHERE a.CallRecordId = cr.Id AND a.AssignedFrom = @UserIndexNumber AND a.AssignmentStatus = 'Accepted'))
                   OR EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a WHERE a.CallRecordId = cr.Id AND a.AssignedTo = @UserIndexNumber AND a.AssignmentStatus IN ('Pending','Accepted')))
              AND (@FilterMonth IS NULL OR cr.call_month = @FilterMonth)
              AND (@FilterYear IS NULL OR cr.call_year = @FilterYear)
              AND (@FilterStartDate IS NULL OR cr.call_date >= @FilterStartDate)
              AND (@FilterEndDate IS NULL OR cr.call_date <= @FilterEndDate)
              AND (@FilterMinCost IS NULL OR cr.call_cost_usd >= @FilterMinCost)
              AND (@FilterStatus IS NULL
                   OR (@FilterStatus = 'unverified' AND cr.call_ver_ind = 0)
                   OR (@FilterStatus = 'verified' AND cr.call_ver_ind = 1)
                   OR (@FilterStatus = 'approved' AND cr.supervisor_approval_status = 'Approved')
                   OR (@FilterStatus = 'pending' AND cr.call_ver_ind = 1 AND cr.supervisor_approval_status = 'Pending')
                   OR (@FilterStatus = 'overdue' AND cr.call_ver_ind = 0 AND cr.verification_period IS NOT NULL AND cr.verification_period < SYSUTCDATETIME()));
        END
    END

    -- Step 2: GroupBy Extension + Month + Year (single pass over filtered calls)
    SELECT
        ISNULL(cr.ext_no, 'Unknown') AS Extension,
        cr.call_month AS [Month],
        cr.call_year AS [Year],
        COUNT(*) AS CallCount,
        ISNULL(SUM(cr.call_cost_usd), 0) AS TotalCostUSD,
        ISNULL(SUM(cr.call_cost_kshs), 0) AS TotalCostKSH,
        ISNULL(SUM(CASE WHEN cr.verification_type = 'Official' THEN cr.call_cost_usd ELSE 0 END), 0) AS OfficialUSD,
        ISNULL(SUM(CASE WHEN cr.verification_type = 'Official' THEN cr.call_cost_kshs ELSE 0 END), 0) AS OfficialKSH,
        ISNULL(SUM(CASE WHEN cr.verification_type = 'Personal' THEN cr.call_cost_usd ELSE 0 END), 0) AS PersonalUSD,
        ISNULL(SUM(CASE WHEN cr.verification_type = 'Personal' THEN cr.call_cost_kshs ELSE 0 END), 0) AS PersonalKSH,
        ISNULL(SUM(CASE WHEN cr.SourceSystem = 'PrivateWire' THEN ISNULL(cr.recovery_amount, 0) ELSE 0 END), 0) AS TotalRecoveredUSD,
        ISNULL(SUM(CASE WHEN cr.SourceSystem IN ('Safaricom','Airtel','PSTN') THEN ISNULL(cr.recovery_amount, 0) ELSE 0 END), 0) AS TotalRecoveredKSH,
        SUM(CASE WHEN cr.SourceSystem = 'PrivateWire' THEN 1 ELSE 0 END) AS PrivateWireCount,
        SUM(CASE WHEN cr.SourceSystem IN ('Safaricom','Airtel','PSTN') THEN 1 ELSE 0 END) AS KshSourceCount,
        COUNT(DISTINCT cr.call_number) AS DialedNumberCount
    INTO #ExtGroups
    FROM ebill.CallRecords cr
    INNER JOIN #FilteredCalls fc ON fc.Id = cr.Id
    GROUP BY ISNULL(cr.ext_no, 'Unknown'), cr.call_month, cr.call_year;

    -- Step 3: Submission counts from CallLogVerifications (single pass)
    SELECT
        ISNULL(cr.ext_no, 'Unknown') AS Extension,
        cr.call_month AS [Month],
        cr.call_year AS [Year],
        COUNT(*) AS SubmittedCount,
        SUM(CASE WHEN v.ApprovalStatus = 'Pending' THEN 1 ELSE 0 END) AS PendingCount,
        SUM(CASE WHEN v.ApprovalStatus = 'Approved' THEN 1 ELSE 0 END) AS ApprovedCount,
        SUM(CASE WHEN v.ApprovalStatus = 'PartiallyApproved' THEN 1 ELSE 0 END) AS PartiallyApprovedCount,
        SUM(CASE WHEN v.ApprovalStatus = 'Rejected' THEN 1 ELSE 0 END) AS RejectedCount,
        SUM(CASE WHEN v.ApprovalStatus = 'Reverted' THEN 1 ELSE 0 END) AS RevertedCount
    INTO #SubmissionCounts
    FROM ebill.CallLogVerifications v
    INNER JOIN ebill.CallRecords cr ON v.CallRecordId = cr.Id
    INNER JOIN #FilteredCalls fc ON fc.Id = cr.Id
    WHERE v.SubmittedToSupervisor = 1
    GROUP BY ISNULL(cr.ext_no, 'Unknown'), cr.call_month, cr.call_year;

    -- Step 4: Incoming assignment counts (calls assigned TO current user, pending)
    SELECT
        ISNULL(cr.ext_no, 'Unknown') AS Extension,
        cr.call_month AS [Month],
        cr.call_year AS [Year],
        COUNT(*) AS AssignmentCount,
        CASE WHEN COUNT(DISTINCT a.AssignedFrom) = 1 THEN MIN(a.AssignedFrom) ELSE NULL END AS AssignedFromUser
    INTO #IncomingAssignments
    FROM ebill.CallLogPaymentAssignments a
    INNER JOIN ebill.CallRecords cr ON a.CallRecordId = cr.Id
    INNER JOIN #FilteredCalls fc ON fc.Id = cr.Id
    WHERE @UserIndexNumber IS NOT NULL
      AND a.AssignedTo = @UserIndexNumber
      AND a.AssignmentStatus = 'Pending'
    GROUP BY ISNULL(cr.ext_no, 'Unknown'), cr.call_month, cr.call_year;

    -- Step 5: Outgoing pending assignment counts (calls user reassigned out, pending)
    SELECT
        ISNULL(cr.ext_no, 'Unknown') AS Extension,
        cr.call_month AS [Month],
        cr.call_year AS [Year],
        COUNT(*) AS OutgoingCount,
        CASE WHEN COUNT(DISTINCT a.AssignedTo) = 1 THEN MIN(a.AssignedTo) ELSE NULL END AS AssignedToUser
    INTO #OutgoingAssignments
    FROM ebill.CallLogPaymentAssignments a
    INNER JOIN ebill.CallRecords cr ON a.CallRecordId = cr.Id
    INNER JOIN #FilteredCalls fc ON fc.Id = cr.Id
    WHERE @UserIndexNumber IS NOT NULL
      AND a.AssignedFrom = @UserIndexNumber
      AND a.AssignmentStatus = 'Pending'
    GROUP BY ISNULL(cr.ext_no, 'Unknown'), cr.call_month, cr.call_year;

    -- Step 6: Class of Service lookup (one row per extension)
    SELECT
        up.PhoneNumber AS Extension,
        cos.[Class] AS ClassOfService,
        cos.Service AS CosService,
        cos.EligibleStaff AS CosEligibleStaff,
        cos.AirtimeAllowance AS CosAirtimeAllowance,
        cos.DataAllowance AS CosDataAllowance,
        cos.HandsetAllowance AS CosHandsetAllowance
    INTO #CosData
    FROM ebill.UserPhones up
    INNER JOIN ebill.ClassOfServices cos ON up.ClassOfServiceId = cos.Id
    WHERE up.ClassOfServiceId IS NOT NULL
      AND up.PhoneNumber IN (SELECT DISTINCT Extension FROM #ExtGroups);

    -- Result set 1: Total count for pagination
    DECLARE @TotalRecords INT = (SELECT COUNT(*) FROM #ExtGroups);
    SELECT @TotalRecords AS TotalRecords;

    -- Result set 2: Paginated extension groups with all joined data
    SELECT
        eg.Extension,
        eg.[Month],
        eg.[Year],
        eg.CallCount,
        eg.TotalCostUSD,
        eg.TotalCostKSH,
        eg.OfficialUSD,
        eg.OfficialKSH,
        eg.PersonalUSD,
        eg.PersonalKSH,
        eg.TotalRecoveredUSD,
        eg.TotalRecoveredKSH,
        eg.PrivateWireCount,
        eg.KshSourceCount,
        eg.DialedNumberCount,
        ISNULL(sc.SubmittedCount, 0) AS SubmittedCount,
        ISNULL(sc.PendingCount, 0) AS PendingApprovalCount,
        ISNULL(sc.ApprovedCount, 0) AS ApprovedCount,
        ISNULL(sc.PartiallyApprovedCount, 0) AS PartiallyApprovedCount,
        ISNULL(sc.RejectedCount, 0) AS RejectedCount,
        ISNULL(sc.RevertedCount, 0) AS RevertedCount,
        ISNULL(ia.AssignmentCount, 0) AS IncomingAssignmentCount,
        ia.AssignedFromUser,
        ISNULL(oa.OutgoingCount, 0) AS OutgoingPendingCount,
        oa.AssignedToUser,
        cd.ClassOfService,
        cd.CosService,
        cd.CosEligibleStaff,
        cd.CosAirtimeAllowance,
        cd.CosDataAllowance,
        cd.CosHandsetAllowance
    FROM #ExtGroups eg
    LEFT JOIN #SubmissionCounts sc ON sc.Extension = eg.Extension AND sc.[Month] = eg.[Month] AND sc.[Year] = eg.[Year]
    LEFT JOIN #IncomingAssignments ia ON ia.Extension = eg.Extension AND ia.[Month] = eg.[Month] AND ia.[Year] = eg.[Year]
    LEFT JOIN #OutgoingAssignments oa ON oa.Extension = eg.Extension AND oa.[Month] = eg.[Month] AND oa.[Year] = eg.[Year]
    LEFT JOIN #CosData cd ON cd.Extension = eg.Extension
    ORDER BY eg.[Year] DESC, eg.[Month] DESC, eg.Extension ASC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    DROP TABLE #FilteredCalls, #ExtGroups, #SubmissionCounts, #IncomingAssignments, #OutgoingAssignments, #CosData;
END
");

            // SP 2: Get Summary Stats (single aggregation query)
            migrationBuilder.Sql(@"
CREATE PROCEDURE ebill.sp_GetMyCallLogSummary
    @UserIndexNumber NVARCHAR(50) = NULL,
    @IsAdmin BIT = 0,
    @FilterMonth INT = NULL,
    @FilterYear INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @IsAdmin = 1 AND @UserIndexNumber IS NULL
    BEGIN
        SELECT
            COUNT(*) AS TotalCalls,
            SUM(CASE WHEN call_ver_ind = 1 THEN 1 ELSE 0 END) AS VerifiedCalls,
            SUM(CASE WHEN call_ver_ind = 0 THEN 1 ELSE 0 END) AS UnverifiedCalls,
            ISNULL(SUM(call_cost_usd), 0) AS TotalAmount,
            ISNULL(SUM(CASE WHEN call_ver_ind = 1 THEN call_cost_usd ELSE 0 END), 0) AS VerifiedAmount,
            SUM(CASE WHEN verification_type = 'Personal' THEN 1 ELSE 0 END) AS PersonalCalls,
            SUM(CASE WHEN verification_type = 'Official' THEN 1 ELSE 0 END) AS OfficialCalls,
            CASE WHEN COUNT(*) > 0
                THEN CAST(SUM(CASE WHEN call_ver_ind = 1 THEN 1 ELSE 0 END) AS DECIMAL(18,4)) / COUNT(*) * 100
                ELSE 0
            END AS CompliancePercentage
        FROM ebill.CallRecords
        WHERE (@FilterMonth IS NULL OR call_month = @FilterMonth)
          AND (@FilterYear IS NULL OR call_year = @FilterYear);
    END
    ELSE IF @UserIndexNumber IS NOT NULL
    BEGIN
        SELECT
            COUNT(*) AS TotalCalls,
            SUM(CASE WHEN cr.call_ver_ind = 1 THEN 1 ELSE 0 END) AS VerifiedCalls,
            SUM(CASE WHEN cr.call_ver_ind = 0 THEN 1 ELSE 0 END) AS UnverifiedCalls,
            ISNULL(SUM(cr.call_cost_usd), 0) AS TotalAmount,
            ISNULL(SUM(CASE WHEN cr.call_ver_ind = 1 THEN cr.call_cost_usd ELSE 0 END), 0) AS VerifiedAmount,
            SUM(CASE WHEN cr.verification_type = 'Personal' THEN 1 ELSE 0 END) AS PersonalCalls,
            SUM(CASE WHEN cr.verification_type = 'Official' THEN 1 ELSE 0 END) AS OfficialCalls,
            CASE WHEN COUNT(*) > 0
                THEN CAST(SUM(CASE WHEN cr.call_ver_ind = 1 THEN 1 ELSE 0 END) AS DECIMAL(18,4)) / COUNT(*) * 100
                ELSE 0
            END AS CompliancePercentage
        FROM ebill.CallRecords cr
        WHERE ((cr.ext_resp_index = @UserIndexNumber
                AND NOT EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a WHERE a.CallRecordId = cr.Id AND a.AssignedFrom = @UserIndexNumber AND a.AssignmentStatus = 'Accepted'))
               OR EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a WHERE a.CallRecordId = cr.Id AND a.AssignedTo = @UserIndexNumber AND a.AssignmentStatus IN ('Pending','Accepted')))
          AND (@FilterMonth IS NULL OR cr.call_month = @FilterMonth)
          AND (@FilterYear IS NULL OR cr.call_year = @FilterYear);
    END
    ELSE
    BEGIN
        SELECT 0 AS TotalCalls, 0 AS VerifiedCalls, 0 AS UnverifiedCalls,
               CAST(0 AS DECIMAL(18,4)) AS TotalAmount, CAST(0 AS DECIMAL(18,4)) AS VerifiedAmount,
               0 AS PersonalCalls, 0 AS OfficialCalls, CAST(0 AS DECIMAL(18,4)) AS CompliancePercentage;
    END
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ebill.sp_GetMyCallLogExtensionGroups;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ebill.sp_GetMyCallLogSummary;");
        }
    }
}
