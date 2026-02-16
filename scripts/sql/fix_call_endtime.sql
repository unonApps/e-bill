-- =============================================
-- Fix Script: Update call_endtime in CallRecords and CallLogStagings
-- Issue: Existing records have call_endtime = date + duration (wrong)
--        Should be call_endtime = call_date + call_time from source table
-- =============================================

-- Preview affected Safaricom records
SELECT TOP 20
    cr.Id AS CallRecordId,
    cr.ext_no,
    cr.call_date,
    cr.call_endtime AS [Current_EndTime],
    s.call_time AS [Safaricom_CallTime],
    DATEADD(SECOND, DATEDIFF(SECOND, 0, s.call_time), CAST(CAST(s.call_date AS DATE) AS DATETIME)) AS [Corrected_EndTime]
FROM ebill.CallRecords cr
INNER JOIN ebill.CallLogStagings cls ON cls.Id = cr.SourceStagingId
INNER JOIN ebill.Safaricom s ON CAST(s.Id AS NVARCHAR(50)) = cls.SourceRecordId
WHERE cls.SourceSystem = 'Safaricom'
  AND s.call_time IS NOT NULL;

-- STEP 1: Fix CallLogStagings for Safaricom
UPDATE cls
SET cls.CallEndTime = DATEADD(SECOND, DATEDIFF(SECOND, 0, s.call_time), CAST(CAST(s.call_date AS DATE) AS DATETIME))
FROM ebill.CallLogStagings cls
INNER JOIN ebill.Safaricom s ON CAST(s.Id AS NVARCHAR(50)) = cls.SourceRecordId
WHERE cls.SourceSystem = 'Safaricom'
  AND s.call_time IS NOT NULL;

PRINT 'CallLogStagings (Safaricom) updated: ' + CAST(@@ROWCOUNT AS VARCHAR);

-- STEP 2: Fix CallRecords for Safaricom
UPDATE cr
SET cr.call_endtime = DATEADD(SECOND, DATEDIFF(SECOND, 0, s.call_time), CAST(CAST(s.call_date AS DATE) AS DATETIME))
FROM ebill.CallRecords cr
INNER JOIN ebill.CallLogStagings cls ON cls.Id = cr.SourceStagingId
INNER JOIN ebill.Safaricom s ON CAST(s.Id AS NVARCHAR(50)) = cls.SourceRecordId
WHERE cls.SourceSystem = 'Safaricom'
  AND s.call_time IS NOT NULL;

PRINT 'CallRecords (Safaricom) updated: ' + CAST(@@ROWCOUNT AS VARCHAR);

-- STEP 3: Fix CallLogStagings for Airtel
UPDATE cls
SET cls.CallEndTime = DATEADD(SECOND, DATEDIFF(SECOND, 0, a.call_time), CAST(CAST(a.call_date AS DATE) AS DATETIME))
FROM ebill.CallLogStagings cls
INNER JOIN ebill.Airtel a ON CAST(a.Id AS NVARCHAR(50)) = cls.SourceRecordId
WHERE cls.SourceSystem = 'Airtel'
  AND a.call_time IS NOT NULL;

PRINT 'CallLogStagings (Airtel) updated: ' + CAST(@@ROWCOUNT AS VARCHAR);

-- STEP 4: Fix CallRecords for Airtel
UPDATE cr
SET cr.call_endtime = DATEADD(SECOND, DATEDIFF(SECOND, 0, a.call_time), CAST(CAST(a.call_date AS DATE) AS DATETIME))
FROM ebill.CallRecords cr
INNER JOIN ebill.CallLogStagings cls ON cls.Id = cr.SourceStagingId
INNER JOIN ebill.Airtel a ON CAST(a.Id AS NVARCHAR(50)) = cls.SourceRecordId
WHERE cls.SourceSystem = 'Airtel'
  AND a.call_time IS NOT NULL;

PRINT 'CallRecords (Airtel) updated: ' + CAST(@@ROWCOUNT AS VARCHAR);
