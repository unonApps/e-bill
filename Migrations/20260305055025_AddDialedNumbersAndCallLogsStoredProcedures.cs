using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDialedNumbersAndCallLogsStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SP 1: Get Dialed Number Groups for Level 2 (replaces 5 separate EF queries)
            migrationBuilder.Sql(@"
CREATE PROCEDURE ebill.sp_GetDialedNumberGroups
    @UserIndexNumber NVARCHAR(50) = NULL,
    @IsAdmin BIT = 0,
    @Extension NVARCHAR(50),
    @Month INT,
    @Year INT,
    @Page INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    -- Step 1: Collect filtered call IDs for this extension/month/year
    CREATE TABLE #FilteredCalls (Id INT PRIMARY KEY);

    IF @IsAdmin = 1 AND @UserIndexNumber IS NULL
    BEGIN
        INSERT INTO #FilteredCalls (Id)
        SELECT cr.Id
        FROM ebill.CallRecords cr
        WHERE cr.ext_no = @Extension
          AND cr.call_month = @Month
          AND cr.call_year = @Year;
    END
    ELSE IF @UserIndexNumber IS NOT NULL
    BEGIN
        INSERT INTO #FilteredCalls (Id)
        SELECT cr.Id
        FROM ebill.CallRecords cr
        WHERE cr.ext_no = @Extension
          AND cr.call_month = @Month
          AND cr.call_year = @Year
          AND ((cr.ext_resp_index = @UserIndexNumber
                AND NOT EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a
                                WHERE a.CallRecordId = cr.Id
                                  AND a.AssignedFrom = @UserIndexNumber
                                  AND a.AssignmentStatus = 'Accepted'))
               OR EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a
                          WHERE a.CallRecordId = cr.Id
                            AND a.AssignedTo = @UserIndexNumber
                            AND a.AssignmentStatus IN ('Pending','Accepted')));
    END

    -- Step 2: GroupBy DialedNumber with aggregates (single pass)
    SELECT
        CASE WHEN ISNULL(cr.call_number, '') = '' THEN 'Subscription' ELSE cr.call_number END AS DialedNumber,
        MAX(cr.call_destination) AS Destination,
        COUNT(*) AS CallCount,
        ISNULL(SUM(cr.call_cost_usd), 0) AS TotalCostUSD,
        ISNULL(SUM(cr.call_cost_kshs), 0) AS TotalCostKSH,
        ISNULL(SUM(CAST(cr.call_duration AS DECIMAL(18,2))), 0) AS TotalDuration,
        CASE
            WHEN COUNT(DISTINCT ISNULL(cr.verification_type, 'Unverified')) > 1 THEN 'Mixed'
            ELSE ISNULL(MIN(cr.verification_type), 'Unverified')
        END AS AssignmentStatus,
        CASE
            WHEN MAX(CASE WHEN cr.call_type IS NOT NULL
                          AND (cr.call_type LIKE '%GPRS%' OR cr.call_type LIKE '%data%')
                     THEN 1 ELSE 0 END) = 1
            THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT)
        END AS IsDataSession
    INTO #DialedGroups
    FROM ebill.CallRecords cr
    INNER JOIN #FilteredCalls fc ON fc.Id = cr.Id
    GROUP BY CASE WHEN ISNULL(cr.call_number, '') = '' THEN 'Subscription' ELSE cr.call_number END;

    -- Result set 1: Total count for pagination
    DECLARE @TotalCount INT = (SELECT COUNT(*) FROM #DialedGroups);
    SELECT @TotalCount AS TotalCount;

    -- Step 3: Submission counts from CallLogVerifications
    SELECT
        CASE WHEN ISNULL(cr.call_number, '') = '' THEN 'Subscription' ELSE cr.call_number END AS DialedNumber,
        COUNT(*) AS SubmittedCount,
        SUM(CASE WHEN v.ApprovalStatus = 'Pending' THEN 1 ELSE 0 END) AS PendingCount,
        SUM(CASE WHEN v.ApprovalStatus = 'Approved' THEN 1 ELSE 0 END) AS ApprovedCount,
        SUM(CASE WHEN v.ApprovalStatus = 'Rejected' THEN 1 ELSE 0 END) AS RejectedCount,
        SUM(CASE WHEN v.ApprovalStatus = 'Reverted' THEN 1 ELSE 0 END) AS RevertedCount,
        SUM(CASE WHEN v.ApprovalStatus = 'PartiallyApproved' THEN 1 ELSE 0 END) AS PartiallyApprovedCount
    INTO #SubmissionCounts
    FROM ebill.CallLogVerifications v
    INNER JOIN ebill.CallRecords cr ON v.CallRecordId = cr.Id
    INNER JOIN #FilteredCalls fc ON fc.Id = cr.Id
    WHERE v.SubmittedToSupervisor = 1
    GROUP BY CASE WHEN ISNULL(cr.call_number, '') = '' THEN 'Subscription' ELSE cr.call_number END;

    -- Step 4: Incoming assignment counts (calls assigned TO current user, pending)
    SELECT
        CASE WHEN ISNULL(cr.call_number, '') = '' THEN 'Subscription' ELSE cr.call_number END AS DialedNumber,
        COUNT(*) AS AssignmentCount,
        CASE WHEN COUNT(DISTINCT a.AssignedFrom) = 1 THEN MIN(a.AssignedFrom) ELSE NULL END AS AssignedFromUser
    INTO #IncomingAssignments
    FROM ebill.CallLogPaymentAssignments a
    INNER JOIN ebill.CallRecords cr ON a.CallRecordId = cr.Id
    INNER JOIN #FilteredCalls fc ON fc.Id = cr.Id
    WHERE @UserIndexNumber IS NOT NULL
      AND a.AssignedTo = @UserIndexNumber
      AND a.AssignmentStatus = 'Pending'
    GROUP BY CASE WHEN ISNULL(cr.call_number, '') = '' THEN 'Subscription' ELSE cr.call_number END;

    -- Step 5: Outgoing pending assignment counts
    SELECT
        CASE WHEN ISNULL(cr.call_number, '') = '' THEN 'Subscription' ELSE cr.call_number END AS DialedNumber,
        COUNT(*) AS OutgoingCount,
        CASE WHEN COUNT(DISTINCT a.AssignedTo) = 1 THEN MIN(a.AssignedTo) ELSE NULL END AS AssignedToUser
    INTO #OutgoingAssignments
    FROM ebill.CallLogPaymentAssignments a
    INNER JOIN ebill.CallRecords cr ON a.CallRecordId = cr.Id
    INNER JOIN #FilteredCalls fc ON fc.Id = cr.Id
    WHERE @UserIndexNumber IS NOT NULL
      AND a.AssignedFrom = @UserIndexNumber
      AND a.AssignmentStatus = 'Pending'
    GROUP BY CASE WHEN ISNULL(cr.call_number, '') = '' THEN 'Subscription' ELSE cr.call_number END;

    -- Result set 2: Paginated dialed number groups with all joined data
    SELECT
        dg.DialedNumber,
        dg.Destination,
        dg.CallCount,
        dg.TotalCostUSD,
        dg.TotalCostKSH,
        dg.TotalDuration,
        dg.AssignmentStatus,
        dg.IsDataSession,
        ISNULL(sc.SubmittedCount, 0) AS SubmittedCount,
        ISNULL(sc.PendingCount, 0) AS PendingApprovalCount,
        ISNULL(sc.ApprovedCount, 0) AS ApprovedCount,
        ISNULL(sc.RejectedCount, 0) AS RejectedCount,
        ISNULL(sc.RevertedCount, 0) AS RevertedCount,
        ISNULL(sc.PartiallyApprovedCount, 0) AS PartiallyApprovedCount,
        ISNULL(ia.AssignmentCount, 0) AS IncomingAssignmentCount,
        ia.AssignedFromUser,
        ISNULL(oa.OutgoingCount, 0) AS OutgoingPendingCount,
        oa.AssignedToUser
    FROM #DialedGroups dg
    LEFT JOIN #SubmissionCounts sc ON sc.DialedNumber = dg.DialedNumber
    LEFT JOIN #IncomingAssignments ia ON ia.DialedNumber = dg.DialedNumber
    LEFT JOIN #OutgoingAssignments oa ON oa.DialedNumber = dg.DialedNumber
    ORDER BY dg.CallCount DESC
    OFFSET (@Page - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    DROP TABLE #FilteredCalls, #DialedGroups, #SubmissionCounts, #IncomingAssignments, #OutgoingAssignments;
END
");

            // SP 2: Get Call Logs for Level 3 (replaces 4 separate EF queries)
            migrationBuilder.Sql(@"
CREATE PROCEDURE ebill.sp_GetCallLogs
    @UserIndexNumber NVARCHAR(50) = NULL,
    @IsAdmin BIT = 0,
    @Extension NVARCHAR(50),
    @Month INT,
    @Year INT,
    @DialedNumber NVARCHAR(50) = NULL,
    @Page INT = 1,
    @PageSize INT = 10,
    @SortBy NVARCHAR(20) = 'CallDate',
    @SortDesc BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    -- Step 1: Collect filtered call IDs
    CREATE TABLE #FilteredCalls (Id INT PRIMARY KEY);

    -- Build base filter: extension + month + year + dialed number
    IF @IsAdmin = 1 AND @UserIndexNumber IS NULL
    BEGIN
        IF @DialedNumber IS NULL OR @DialedNumber = 'Subscription' OR @DialedNumber = 'Unknown' OR @DialedNumber = ''
        BEGIN
            INSERT INTO #FilteredCalls (Id)
            SELECT cr.Id FROM ebill.CallRecords cr
            WHERE cr.ext_no = @Extension AND cr.call_month = @Month AND cr.call_year = @Year
              AND (cr.call_number IS NULL OR cr.call_number = '');
        END
        ELSE
        BEGIN
            INSERT INTO #FilteredCalls (Id)
            SELECT cr.Id FROM ebill.CallRecords cr
            WHERE cr.ext_no = @Extension AND cr.call_month = @Month AND cr.call_year = @Year
              AND cr.call_number = @DialedNumber;
        END
    END
    ELSE IF @UserIndexNumber IS NOT NULL
    BEGIN
        IF @DialedNumber IS NULL OR @DialedNumber = 'Subscription' OR @DialedNumber = 'Unknown' OR @DialedNumber = ''
        BEGIN
            INSERT INTO #FilteredCalls (Id)
            SELECT cr.Id FROM ebill.CallRecords cr
            WHERE cr.ext_no = @Extension AND cr.call_month = @Month AND cr.call_year = @Year
              AND (cr.call_number IS NULL OR cr.call_number = '')
              AND ((cr.ext_resp_index = @UserIndexNumber
                    AND NOT EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a
                                    WHERE a.CallRecordId = cr.Id AND a.AssignedFrom = @UserIndexNumber AND a.AssignmentStatus = 'Accepted'))
                   OR EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a
                              WHERE a.CallRecordId = cr.Id AND a.AssignedTo = @UserIndexNumber
                                AND a.AssignmentStatus IN ('Pending','Accepted')));
        END
        ELSE
        BEGIN
            INSERT INTO #FilteredCalls (Id)
            SELECT cr.Id FROM ebill.CallRecords cr
            WHERE cr.ext_no = @Extension AND cr.call_month = @Month AND cr.call_year = @Year
              AND cr.call_number = @DialedNumber
              AND ((cr.ext_resp_index = @UserIndexNumber
                    AND NOT EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a
                                    WHERE a.CallRecordId = cr.Id AND a.AssignedFrom = @UserIndexNumber AND a.AssignmentStatus = 'Accepted'))
                   OR EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a
                              WHERE a.CallRecordId = cr.Id AND a.AssignedTo = @UserIndexNumber
                                AND a.AssignmentStatus IN ('Pending','Accepted')));
        END
    END

    -- Result set 1: Total count
    DECLARE @TotalCount INT = (SELECT COUNT(*) FROM #FilteredCalls);
    SELECT @TotalCount AS TotalCount;

    -- Step 2: Get verification data for filtered calls
    SELECT v.CallRecordId, v.ApprovalStatus
    INTO #Verifications
    FROM ebill.CallLogVerifications v
    INNER JOIN #FilteredCalls fc ON fc.Id = v.CallRecordId
    WHERE v.SubmittedToSupervisor = 1;

    -- Step 3: Get incoming assignments (assigned TO current user)
    SELECT a.CallRecordId, a.AssignedFrom
    INTO #IncomingAssignments
    FROM ebill.CallLogPaymentAssignments a
    INNER JOIN #FilteredCalls fc ON fc.Id = a.CallRecordId
    WHERE @UserIndexNumber IS NOT NULL
      AND a.AssignedTo = @UserIndexNumber
      AND a.AssignmentStatus IN ('Pending','Accepted');

    -- Step 4: Get outgoing assignments (assigned FROM current user)
    SELECT a.CallRecordId, a.AssignmentStatus, a.AssignedTo
    INTO #OutgoingAssignments
    FROM ebill.CallLogPaymentAssignments a
    INNER JOIN #FilteredCalls fc ON fc.Id = a.CallRecordId
    WHERE @UserIndexNumber IS NOT NULL
      AND a.AssignedFrom = @UserIndexNumber
      AND a.AssignmentStatus IN ('Pending','Accepted');

    -- Result set 2: Paginated call records with pre-joined verification/assignment data
    SELECT
        cr.Id,
        CASE WHEN ISNULL(cr.call_number, '') = '' THEN 'Subscription' ELSE cr.call_number END AS DialedNumber,
        cr.call_date AS CallDate,
        cr.call_endtime AS CallEndTime,
        cr.call_duration AS CallDuration,
        cr.call_cost_usd AS CallCostUSD,
        cr.call_cost_kshs AS CallCostKSH,
        ISNULL(cr.call_destination, '') AS Destination,
        ISNULL(cr.call_type, '') AS CallType,
        ISNULL(cr.verification_type, '') AS VerificationType,
        cr.call_ver_ind AS IsVerified,
        cr.supervisor_approval_status AS SupervisorApprovalStatus,
        CASE WHEN vf.CallRecordId IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsSubmittedToSupervisor,
        CASE
            WHEN ia.CallRecordId IS NOT NULL THEN 'assigned'
            WHEN oa.CallRecordId IS NOT NULL AND oa.AssignmentStatus = 'Pending' THEN 'assigned_out_pending'
            ELSE 'own'
        END AS AssignmentStatus,
        ia.AssignedFrom,
        oa.AssignedTo,
        CASE WHEN cr.supervisor_approval_status = 'Approved' THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsLocked
    FROM ebill.CallRecords cr
    INNER JOIN #FilteredCalls fc ON fc.Id = cr.Id
    LEFT JOIN #Verifications vf ON vf.CallRecordId = cr.Id
    LEFT JOIN #IncomingAssignments ia ON ia.CallRecordId = cr.Id
    LEFT JOIN #OutgoingAssignments oa ON oa.CallRecordId = cr.Id
    ORDER BY
        CASE WHEN @SortBy = 'CallDate'  AND @SortDesc = 0 THEN cr.call_date END ASC,
        CASE WHEN @SortBy = 'CallDate'  AND @SortDesc = 1 THEN cr.call_date END DESC,
        CASE WHEN @SortBy = 'Duration'  AND @SortDesc = 0 THEN cr.call_duration END ASC,
        CASE WHEN @SortBy = 'Duration'  AND @SortDesc = 1 THEN cr.call_duration END DESC,
        CASE WHEN @SortBy = 'CostKSH'   AND @SortDesc = 0 THEN cr.call_cost_kshs END ASC,
        CASE WHEN @SortBy = 'CostKSH'   AND @SortDesc = 1 THEN cr.call_cost_kshs END DESC,
        CASE WHEN @SortBy = 'CostUSD'   AND @SortDesc = 0 THEN cr.call_cost_usd END ASC,
        CASE WHEN @SortBy = 'CostUSD'   AND @SortDesc = 1 THEN cr.call_cost_usd END DESC,
        CASE WHEN @SortBy = 'Type'      AND @SortDesc = 0 THEN cr.call_type END ASC,
        CASE WHEN @SortBy = 'Type'      AND @SortDesc = 1 THEN cr.call_type END DESC,
        CASE WHEN @SortBy = 'Status'    AND @SortDesc = 0 THEN CAST(cr.call_ver_ind AS INT) END ASC,
        CASE WHEN @SortBy = 'Status'    AND @SortDesc = 0 THEN cr.verification_type END ASC,
        CASE WHEN @SortBy = 'Status'    AND @SortDesc = 1 THEN CAST(cr.call_ver_ind AS INT) END DESC,
        CASE WHEN @SortBy = 'Status'    AND @SortDesc = 1 THEN cr.verification_type END DESC
    OFFSET (@Page - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    DROP TABLE #FilteredCalls, #Verifications, #IncomingAssignments, #OutgoingAssignments;
END
");

            // SP 3: Get Phone Level Overages (replaces N+1 loop in CalculatePhoneLevelOveragesAsync)
            migrationBuilder.Sql(@"
CREATE PROCEDURE ebill.sp_GetPhoneLevelOverages
    @CallRecordIds NVARCHAR(MAX), -- comma-separated list of call record IDs
    @Month INT,
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Parse the comma-separated IDs into a table
    CREATE TABLE #CallIds (Id INT PRIMARY KEY);
    INSERT INTO #CallIds (Id)
    SELECT CAST(value AS INT) FROM STRING_SPLIT(@CallRecordIds, ',')
    WHERE RTRIM(LTRIM(value)) <> '';

    -- Single query: get all phone overages with allowance, usage, and existing justifications
    SELECT
        up.Id AS UserPhoneId,
        up.PhoneNumber,
        up.PhoneType,
        cos.[Class] AS ClassOfService,
        ISNULL(cos.AirtimeAllowanceAmount, 0) AS AllowanceLimit,
        ISNULL(usage.TotalUsage, 0) AS TotalUsage,
        CASE
            WHEN cos.AirtimeAllowanceAmount IS NOT NULL AND cos.AirtimeAllowanceAmount > 0
                 AND ISNULL(usage.TotalUsage, 0) > cos.AirtimeAllowanceAmount
            THEN ISNULL(usage.TotalUsage, 0) - cos.AirtimeAllowanceAmount
            ELSE 0
        END AS OverageAmount,
        CASE
            WHEN cos.AirtimeAllowanceAmount IS NOT NULL AND cos.AirtimeAllowanceAmount > 0
                 AND ISNULL(usage.TotalUsage, 0) > cos.AirtimeAllowanceAmount
            THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT)
        END AS HasOverage,
        callCounts.CallCount,
        CASE WHEN poj.Id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasExistingJustification,
        poj.JustificationText AS ExistingJustificationText,
        poj.SubmittedDate AS ExistingJustificationDate,
        poj.ApprovalStatus AS ExistingJustificationStatus,
        ISNULL(docCounts.DocumentCount, 0) AS ExistingDocumentCount
    FROM (
        SELECT DISTINCT cr.UserPhoneId
        FROM ebill.CallRecords cr
        INNER JOIN #CallIds ci ON ci.Id = cr.Id
        WHERE cr.UserPhoneId IS NOT NULL
    ) phones
    INNER JOIN ebill.UserPhones up ON up.Id = phones.UserPhoneId AND up.IsActive = 1
    INNER JOIN ebill.ClassOfServices cos ON up.ClassOfServiceId = cos.Id
    -- Monthly usage for this phone (Official calls only)
    LEFT JOIN (
        SELECT cr2.UserPhoneId, SUM(cr2.call_cost_usd) AS TotalUsage
        FROM ebill.CallRecords cr2
        WHERE cr2.call_month = @Month AND cr2.call_year = @Year
          AND cr2.verification_type = 'Official'
          AND cr2.UserPhoneId IS NOT NULL
        GROUP BY cr2.UserPhoneId
    ) usage ON usage.UserPhoneId = up.Id
    -- Count of calls in the submission set for this phone
    INNER JOIN (
        SELECT cr3.UserPhoneId, COUNT(*) AS CallCount
        FROM ebill.CallRecords cr3
        INNER JOIN #CallIds ci3 ON ci3.Id = cr3.Id
        WHERE cr3.UserPhoneId IS NOT NULL
        GROUP BY cr3.UserPhoneId
    ) callCounts ON callCounts.UserPhoneId = up.Id
    -- Existing justification
    LEFT JOIN ebill.PhoneOverageJustifications poj
        ON poj.UserPhoneId = up.Id AND poj.Month = @Month AND poj.Year = @Year
    -- Document count for existing justification
    LEFT JOIN (
        SELECT PhoneOverageJustificationId, COUNT(*) AS DocumentCount
        FROM ebill.PhoneOverageDocuments
        GROUP BY PhoneOverageJustificationId
    ) docCounts ON docCounts.PhoneOverageJustificationId = poj.Id
    WHERE cos.AirtimeAllowanceAmount IS NOT NULL AND cos.AirtimeAllowanceAmount > 0
    ORDER BY OverageAmount DESC;

    DROP TABLE #CallIds;
END
");

            // SP 4: Get Call IDs for Extension (replaces 2 EF queries in OnGetCallIdsForExtensionAsync)
            migrationBuilder.Sql(@"
CREATE PROCEDURE ebill.sp_GetCallIdsForExtension
    @UserIndexNumber NVARCHAR(50),
    @Extension NVARCHAR(50),
    @Month INT,
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Get eligible call IDs (Official or unverified, not Personal)
    -- Excluding calls assigned out and accepted, including incoming assignments
    -- Then exclude already submitted+pending/approved
    SELECT cr.Id
    FROM ebill.CallRecords cr
    WHERE cr.ext_no = @Extension
      AND cr.call_month = @Month
      AND cr.call_year = @Year
      AND ISNULL(cr.verification_type, '') <> 'Personal'
      AND ((cr.ext_resp_index = @UserIndexNumber
            AND NOT EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a
                            WHERE a.CallRecordId = cr.Id AND a.AssignedFrom = @UserIndexNumber AND a.AssignmentStatus = 'Accepted'))
           OR EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a
                      WHERE a.CallRecordId = cr.Id AND a.AssignedTo = @UserIndexNumber
                        AND a.AssignmentStatus IN ('Pending','Accepted')))
      AND NOT EXISTS (SELECT 1 FROM ebill.CallLogVerifications v
                      WHERE v.CallRecordId = cr.Id
                        AND v.SubmittedToSupervisor = 1
                        AND v.ApprovalStatus IN ('Pending','Approved'));

    -- Count of skipped (already submitted+pending/approved)
    SELECT COUNT(*) AS SkippedCount
    FROM ebill.CallRecords cr
    WHERE cr.ext_no = @Extension
      AND cr.call_month = @Month
      AND cr.call_year = @Year
      AND ISNULL(cr.verification_type, '') <> 'Personal'
      AND ((cr.ext_resp_index = @UserIndexNumber
            AND NOT EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a
                            WHERE a.CallRecordId = cr.Id AND a.AssignedFrom = @UserIndexNumber AND a.AssignmentStatus = 'Accepted'))
           OR EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a
                      WHERE a.CallRecordId = cr.Id AND a.AssignedTo = @UserIndexNumber
                        AND a.AssignmentStatus IN ('Pending','Accepted')))
      AND EXISTS (SELECT 1 FROM ebill.CallLogVerifications v
                  WHERE v.CallRecordId = cr.Id
                    AND v.SubmittedToSupervisor = 1
                    AND v.ApprovalStatus IN ('Pending','Approved'));
END
");

            // SP 5: Get Call IDs for Dialed Number (replaces 2 EF queries in OnGetCallIdsForDialedNumberAsync)
            migrationBuilder.Sql(@"
CREATE PROCEDURE ebill.sp_GetCallIdsForDialedNumber
    @UserIndexNumber NVARCHAR(50),
    @Extension NVARCHAR(50),
    @Month INT,
    @Year INT,
    @DialedNumber NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Determine dialed number filter
    DECLARE @IsSubscription BIT = CASE WHEN @DialedNumber IS NULL OR @DialedNumber = '' OR @DialedNumber = 'Subscription' THEN 1 ELSE 0 END;

    -- Get eligible call IDs
    SELECT cr.Id
    FROM ebill.CallRecords cr
    WHERE cr.ext_no = @Extension
      AND cr.call_month = @Month
      AND cr.call_year = @Year
      AND ISNULL(cr.verification_type, '') <> 'Personal'
      AND ((@IsSubscription = 1 AND (cr.call_number IS NULL OR cr.call_number = ''))
           OR (@IsSubscription = 0 AND cr.call_number = @DialedNumber))
      AND ((cr.ext_resp_index = @UserIndexNumber
            AND NOT EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a
                            WHERE a.CallRecordId = cr.Id AND a.AssignedFrom = @UserIndexNumber AND a.AssignmentStatus = 'Accepted'))
           OR EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a
                      WHERE a.CallRecordId = cr.Id AND a.AssignedTo = @UserIndexNumber
                        AND a.AssignmentStatus IN ('Pending','Accepted')))
      AND NOT EXISTS (SELECT 1 FROM ebill.CallLogVerifications v
                      WHERE v.CallRecordId = cr.Id
                        AND v.SubmittedToSupervisor = 1
                        AND v.ApprovalStatus IN ('Pending','Approved'));

    -- Count of skipped
    SELECT COUNT(*) AS SkippedCount
    FROM ebill.CallRecords cr
    WHERE cr.ext_no = @Extension
      AND cr.call_month = @Month
      AND cr.call_year = @Year
      AND ISNULL(cr.verification_type, '') <> 'Personal'
      AND ((@IsSubscription = 1 AND (cr.call_number IS NULL OR cr.call_number = ''))
           OR (@IsSubscription = 0 AND cr.call_number = @DialedNumber))
      AND ((cr.ext_resp_index = @UserIndexNumber
            AND NOT EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a
                            WHERE a.CallRecordId = cr.Id AND a.AssignedFrom = @UserIndexNumber AND a.AssignmentStatus = 'Accepted'))
           OR EXISTS (SELECT 1 FROM ebill.CallLogPaymentAssignments a
                      WHERE a.CallRecordId = cr.Id AND a.AssignedTo = @UserIndexNumber
                        AND a.AssignmentStatus IN ('Pending','Accepted')))
      AND EXISTS (SELECT 1 FROM ebill.CallLogVerifications v
                  WHERE v.CallRecordId = cr.Id
                    AND v.SubmittedToSupervisor = 1
                    AND v.ApprovalStatus IN ('Pending','Approved'));
END
");

            // SP 6: Bulk Recall Assignments (replaces inline raw SQL in BulkRecallAssignmentsRawAsync)
            migrationBuilder.Sql(@"
CREATE PROCEDURE ebill.sp_BulkRecallAssignments
    @IndexNumber NVARCHAR(50),
    @Extension NVARCHAR(50) = NULL,
    @Month INT = NULL,
    @Year INT = NULL,
    @DialedNumber NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    DECLARE @RecalledCount INT = 0;

    BEGIN TRANSACTION;

    -- Step 1: Update CallRecords - revert payment back to original owner
    UPDATE cr
    SET cr.call_pay_index = pa.AssignedFrom,
        cr.payment_assignment_id = NULL,
        cr.assignment_status = 'None'
    FROM ebill.CallRecords cr
    INNER JOIN ebill.CallLogPaymentAssignments pa ON cr.payment_assignment_id = pa.Id
    WHERE pa.AssignedFrom = @IndexNumber
      AND pa.AssignmentStatus = 'Pending'
      AND (@Extension IS NULL OR cr.ext_no = @Extension)
      AND (@Month IS NULL OR cr.call_month = @Month)
      AND (@Year IS NULL OR cr.call_year = @Year)
      AND (@DialedNumber IS NULL OR ISNULL(cr.call_number, '') = @DialedNumber);

    -- Step 2: Update assignments to Recalled
    UPDATE pa
    SET pa.AssignmentStatus = 'Recalled',
        pa.ModifiedDate = @Now
    FROM ebill.CallLogPaymentAssignments pa
    INNER JOIN ebill.CallRecords cr ON pa.CallRecordId = cr.Id
    WHERE pa.AssignedFrom = @IndexNumber
      AND pa.AssignmentStatus = 'Pending'
      AND (@Extension IS NULL OR cr.ext_no = @Extension)
      AND (@Month IS NULL OR cr.call_month = @Month)
      AND (@Year IS NULL OR cr.call_year = @Year)
      AND (@DialedNumber IS NULL OR ISNULL(cr.call_number, '') = @DialedNumber);

    SET @RecalledCount = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT @RecalledCount AS RecalledCount;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ebill.sp_GetDialedNumberGroups;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ebill.sp_GetCallLogs;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ebill.sp_GetPhoneLevelOverages;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ebill.sp_GetCallIdsForExtension;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ebill.sp_GetCallIdsForDialedNumber;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ebill.sp_BulkRecallAssignments;");
        }
    }
}
