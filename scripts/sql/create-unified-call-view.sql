-- =====================================================
-- Create Unified Call Records View
-- Combines all telecom tables with proper column mappings
-- =====================================================

IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_AllCallRecords')
    DROP VIEW vw_AllCallRecords;
GO

CREATE VIEW vw_AllCallRecords AS
-- PrivateWires
SELECT
    'PrivateWire' AS CallType,
    pw.Id AS RecordId,
    pw.IndexNumber,
    pw.Extension AS CallingNumber,
    pw.DestinationLine AS CalledNumber,
    pw.Destination,
    pw.CallDate,
    pw.CallTime,
    pw.Duration,
    pw.DurationExtended,
    pw.AmountUSD AS Amount,
    'USD' AS Currency,
    pw.BillingPeriod,
    pw.CallMonth,
    pw.CallYear,

    -- User Information
    eu.FirstName,
    eu.LastName,
    eu.FirstName + ' ' + eu.LastName AS CallerName,
    eu.Email,
    eu.OfficialMobileNumber,

    -- Organization Structure (from relationships)
    org.Name AS Organization,
    ofc.Name AS Office,
    subof.Name AS SubOffice,
    COALESCE(eu.Location, pw.Location) AS Location,

    -- IDs for filtering
    pw.EbillUserId,
    COALESCE(eu.OrganizationId, pw.OrganizationId) AS OrganizationId,
    COALESCE(eu.OfficeId, pw.OfficeId) AS OfficeId,
    COALESCE(eu.SubOfficeId, pw.SubOfficeId) AS SubOfficeId,

    -- Processing Status
    pw.ProcessingStatus,
    pw.ProcessedDate,
    pw.ImportAuditId,
    pw.StagingBatchId
FROM PrivateWires pw
LEFT JOIN EbillUsers eu ON pw.IndexNumber = eu.IndexNumber OR pw.EbillUserId = eu.Id
LEFT JOIN Organizations org ON COALESCE(eu.OrganizationId, pw.OrganizationId) = org.Id
LEFT JOIN Offices ofc ON COALESCE(eu.OfficeId, pw.OfficeId) = ofc.Id
LEFT JOIN SubOffices subof ON COALESCE(eu.SubOfficeId, pw.SubOfficeId) = subof.Id

UNION ALL

-- Safaricom
SELECT
    'Safaricom' AS CallType,
    s.Id AS RecordId,
    s.IndexNumber,
    s.ext AS CallingNumber,
    s.dialed AS CalledNumber,
    s.dest AS Destination,
    s.call_date AS CallDate,
    s.call_time AS CallTime,
    s.dur AS Duration,
    s.durx AS DurationExtended,
    s.cost AS Amount,
    'KSH' AS Currency,
    s.BillingPeriod,
    s.call_month AS CallMonth,
    s.call_year AS CallYear,

    eu.FirstName,
    eu.LastName,
    eu.FirstName + ' ' + eu.LastName AS CallerName,
    eu.Email,
    eu.OfficialMobileNumber,

    org.Name AS Organization,
    ofc.Name AS Office,
    subof.Name AS SubOffice,
    eu.Location,

    s.EbillUserId,
    COALESCE(eu.OrganizationId, s.OrganizationId) AS OrganizationId,
    COALESCE(eu.OfficeId, s.OfficeId) AS OfficeId,
    COALESCE(eu.SubOfficeId, s.SubOfficeId) AS SubOfficeId,

    s.ProcessingStatus,
    s.ProcessedDate,
    s.ImportAuditId,
    s.StagingBatchId
FROM Safaricom s
LEFT JOIN EbillUsers eu ON s.IndexNumber = eu.IndexNumber OR s.EbillUserId = eu.Id
LEFT JOIN Organizations org ON COALESCE(eu.OrganizationId, s.OrganizationId) = org.Id
LEFT JOIN Offices ofc ON COALESCE(eu.OfficeId, s.OfficeId) = ofc.Id
LEFT JOIN SubOffices subof ON COALESCE(eu.SubOfficeId, s.SubOfficeId) = subof.Id

UNION ALL

-- Airtel
SELECT
    'Airtel' AS CallType,
    a.Id AS RecordId,
    a.IndexNumber,
    a.ext AS CallingNumber,
    a.dialed AS CalledNumber,
    a.dest AS Destination,
    a.call_date AS CallDate,
    a.call_time AS CallTime,
    a.dur AS Duration,
    a.durx AS DurationExtended,
    a.cost AS Amount,
    'KSH' AS Currency,
    a.BillingPeriod,
    a.call_month AS CallMonth,
    a.call_year AS CallYear,

    eu.FirstName,
    eu.LastName,
    eu.FirstName + ' ' + eu.LastName AS CallerName,
    eu.Email,
    eu.OfficialMobileNumber,

    org.Name AS Organization,
    ofc.Name AS Office,
    subof.Name AS SubOffice,
    eu.Location,

    a.EbillUserId,
    COALESCE(eu.OrganizationId, a.OrganizationId) AS OrganizationId,
    COALESCE(eu.OfficeId, a.OfficeId) AS OfficeId,
    COALESCE(eu.SubOfficeId, a.SubOfficeId) AS SubOfficeId,

    a.ProcessingStatus,
    a.ProcessedDate,
    a.ImportAuditId,
    a.StagingBatchId
FROM Airtel a
LEFT JOIN EbillUsers eu ON a.IndexNumber = eu.IndexNumber OR a.EbillUserId = eu.Id
LEFT JOIN Organizations org ON COALESCE(eu.OrganizationId, a.OrganizationId) = org.Id
LEFT JOIN Offices ofc ON COALESCE(eu.OfficeId, a.OfficeId) = ofc.Id
LEFT JOIN SubOffices subof ON COALESCE(eu.SubOfficeId, a.SubOfficeId) = subof.Id

UNION ALL

-- PSTNs
SELECT
    'PSTN' AS CallType,
    p.Id AS RecordId,
    p.IndexNumber,
    p.Extension AS CallingNumber,
    p.DialedNumber AS CalledNumber,
    p.Destination,
    p.CallDate,
    p.CallTime,
    p.Duration,
    p.DurationExtended,
    p.AmountKSH AS Amount,
    'KSH' AS Currency,
    p.BillingPeriod,
    p.CallMonth,
    p.CallYear,

    eu.FirstName,
    eu.LastName,
    eu.FirstName + ' ' + eu.LastName AS CallerName,
    eu.Email,
    eu.OfficialMobileNumber,

    org.Name AS Organization,
    ofc.Name AS Office,
    subof.Name AS SubOffice,
    COALESCE(eu.Location, p.Location) AS Location,

    p.EbillUserId,
    COALESCE(eu.OrganizationId, p.OrganizationId) AS OrganizationId,
    COALESCE(eu.OfficeId, p.OfficeId) AS OfficeId,
    COALESCE(eu.SubOfficeId, p.SubOfficeId) AS SubOfficeId,

    p.ProcessingStatus,
    p.ProcessedDate,
    p.ImportAuditId,
    p.StagingBatchId
FROM PSTNs p
LEFT JOIN EbillUsers eu ON p.IndexNumber = eu.IndexNumber OR p.EbillUserId = eu.Id
LEFT JOIN Organizations org ON COALESCE(eu.OrganizationId, p.OrganizationId) = org.Id
LEFT JOIN Offices ofc ON COALESCE(eu.OfficeId, p.OfficeId) = ofc.Id
LEFT JOIN SubOffices subof ON COALESCE(eu.SubOfficeId, p.SubOfficeId) = subof.Id;
GO

-- Test the unified view
SELECT TOP 10
    CallType,
    IndexNumber,
    CallerName,
    Organization,
    Office,
    CallDate,
    Duration,
    Amount,
    Currency
FROM vw_AllCallRecords
ORDER BY CallDate DESC;

PRINT 'Unified call records view created successfully!';