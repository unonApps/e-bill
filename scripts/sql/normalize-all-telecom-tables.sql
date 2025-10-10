-- =====================================================
-- Normalize ALL Telecom Tables (PrivateWires, Safaricom, Airtel, PSTN)
-- Remove redundant columns and use EbillUsers relationship
-- =====================================================

-- ===========================================
-- STEP 1: Update all tables with EbillUserId
-- ===========================================

PRINT 'Step 1: Updating EbillUserId in all telecom tables...';

-- PrivateWires
UPDATE pw
SET pw.EbillUserId = eu.Id
FROM PrivateWires pw
INNER JOIN EbillUsers eu ON pw.IndexNumber = eu.IndexNumber
WHERE pw.EbillUserId IS NULL;

PRINT 'PrivateWires: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records updated';

-- Safaricom
UPDATE s
SET s.EbillUserId = eu.Id
FROM Safaricom s
INNER JOIN EbillUsers eu ON s.IndexNumber = eu.IndexNumber
WHERE s.EbillUserId IS NULL;

PRINT 'Safaricom: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records updated';

-- Airtel
UPDATE a
SET a.EbillUserId = eu.Id
FROM Airtel a
INNER JOIN EbillUsers eu ON a.IndexNumber = eu.IndexNumber
WHERE a.EbillUserId IS NULL;

PRINT 'Airtel: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records updated';

-- PSTN
UPDATE p
SET p.EbillUserId = eu.Id
FROM PSTN p
INNER JOIN EbillUsers eu ON p.IndexNumber = eu.IndexNumber
WHERE p.EbillUserId IS NULL;

PRINT 'PSTN: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records updated';

-- ===========================================
-- STEP 2: Update Organization/Office/SubOffice IDs
-- ===========================================

PRINT 'Step 2: Updating Organization/Office/SubOffice IDs...';

-- PrivateWires
UPDATE pw
SET
    pw.OrganizationId = eu.OrganizationId,
    pw.OfficeId = eu.OfficeId,
    pw.SubOfficeId = eu.SubOfficeId
FROM PrivateWires pw
INNER JOIN EbillUsers eu ON pw.EbillUserId = eu.Id
WHERE pw.OrganizationId IS NULL OR pw.OfficeId IS NULL;

PRINT 'PrivateWires: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records updated';

-- Safaricom
UPDATE s
SET
    s.OrganizationId = eu.OrganizationId,
    s.OfficeId = eu.OfficeId,
    s.SubOfficeId = eu.SubOfficeId
FROM Safaricom s
INNER JOIN EbillUsers eu ON s.EbillUserId = eu.Id
WHERE s.OrganizationId IS NULL OR s.OfficeId IS NULL;

PRINT 'Safaricom: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records updated';

-- Airtel
UPDATE a
SET
    a.OrganizationId = eu.OrganizationId,
    a.OfficeId = eu.OfficeId,
    a.SubOfficeId = eu.SubOfficeId
FROM Airtel a
INNER JOIN EbillUsers eu ON a.EbillUserId = eu.Id
WHERE a.OrganizationId IS NULL OR a.OfficeId IS NULL;

PRINT 'Airtel: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records updated';

-- PSTN
UPDATE p
SET
    p.OrganizationId = eu.OrganizationId,
    p.OfficeId = eu.OfficeId,
    p.SubOfficeId = eu.SubOfficeId
FROM PSTN p
INNER JOIN EbillUsers eu ON p.EbillUserId = eu.Id
WHERE p.OrganizationId IS NULL OR p.OfficeId IS NULL;

PRINT 'PSTN: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records updated';

-- ===========================================
-- STEP 3: Create Unified Call Records View
-- ===========================================

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
    eu.Location,

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
LEFT JOIN EbillUsers eu ON pw.EbillUserId = eu.Id
LEFT JOIN Organizations org ON COALESCE(eu.OrganizationId, pw.OrganizationId) = org.Id
LEFT JOIN Offices ofc ON COALESCE(eu.OfficeId, pw.OfficeId) = ofc.Id
LEFT JOIN SubOffices subof ON COALESCE(eu.SubOfficeId, pw.SubOfficeId) = subof.Id

UNION ALL

-- Safaricom
SELECT
    'Safaricom' AS CallType,
    s.Id AS RecordId,
    s.IndexNumber,
    s.PhoneNumber AS CallingNumber,
    s.DestinationNumber AS CalledNumber,
    s.Destination,
    s.CallDate,
    s.CallTime,
    s.Duration,
    NULL AS DurationExtended,
    s.TotalCharges AS Amount,
    s.BillingPeriod,
    s.CallMonth,
    s.CallYear,

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
LEFT JOIN EbillUsers eu ON s.EbillUserId = eu.Id
LEFT JOIN Organizations org ON COALESCE(eu.OrganizationId, s.OrganizationId) = org.Id
LEFT JOIN Offices ofc ON COALESCE(eu.OfficeId, s.OfficeId) = ofc.Id
LEFT JOIN SubOffices subof ON COALESCE(eu.SubOfficeId, s.SubOfficeId) = subof.Id

UNION ALL

-- Airtel
SELECT
    'Airtel' AS CallType,
    a.Id AS RecordId,
    a.IndexNumber,
    a.CallingNumber,
    a.CalledNumber,
    a.Destination,
    a.CallDate,
    a.CallTime,
    a.Duration,
    NULL AS DurationExtended,
    a.ChargeAmount AS Amount,
    a.BillingPeriod,
    a.CallMonth,
    a.CallYear,

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
LEFT JOIN EbillUsers eu ON a.EbillUserId = eu.Id
LEFT JOIN Organizations org ON COALESCE(eu.OrganizationId, a.OrganizationId) = org.Id
LEFT JOIN Offices ofc ON COALESCE(eu.OfficeId, a.OfficeId) = ofc.Id
LEFT JOIN SubOffices subof ON COALESCE(eu.SubOfficeId, a.SubOfficeId) = subof.Id

UNION ALL

-- PSTN
SELECT
    'PSTN' AS CallType,
    p.Id AS RecordId,
    p.IndexNumber,
    p.CallingNumber,
    p.CalledNumber,
    p.Destination,
    p.CallDate,
    p.CallTime,
    p.Duration,
    NULL AS DurationExtended,
    p.ChargeAmount AS Amount,
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
    eu.Location,

    p.EbillUserId,
    COALESCE(eu.OrganizationId, p.OrganizationId) AS OrganizationId,
    COALESCE(eu.OfficeId, p.OfficeId) AS OfficeId,
    COALESCE(eu.SubOfficeId, p.SubOfficeId) AS SubOfficeId,

    p.ProcessingStatus,
    p.ProcessedDate,
    p.ImportAuditId,
    p.StagingBatchId
FROM PSTN p
LEFT JOIN EbillUsers eu ON p.EbillUserId = eu.Id
LEFT JOIN Organizations org ON COALESCE(eu.OrganizationId, p.OrganizationId) = org.Id
LEFT JOIN Offices ofc ON COALESCE(eu.OfficeId, p.OfficeId) = ofc.Id
LEFT JOIN SubOffices subof ON COALESCE(eu.SubOfficeId, p.SubOfficeId) = subof.Id;
GO

-- ===========================================
-- STEP 4: Create Indexes for Performance
-- ===========================================

PRINT 'Step 4: Creating indexes for better performance...';

-- PrivateWires
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PrivateWires_IndexNumber' AND object_id = OBJECT_ID('PrivateWires'))
    CREATE INDEX IX_PrivateWires_IndexNumber ON PrivateWires(IndexNumber);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PrivateWires_EbillUserId' AND object_id = OBJECT_ID('PrivateWires'))
    CREATE INDEX IX_PrivateWires_EbillUserId ON PrivateWires(EbillUserId);

-- Safaricom
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Safaricom_IndexNumber' AND object_id = OBJECT_ID('Safaricom'))
    CREATE INDEX IX_Safaricom_IndexNumber ON Safaricom(IndexNumber);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Safaricom_EbillUserId' AND object_id = OBJECT_ID('Safaricom'))
    CREATE INDEX IX_Safaricom_EbillUserId ON Safaricom(EbillUserId);

-- Airtel
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Airtel_IndexNumber' AND object_id = OBJECT_ID('Airtel'))
    CREATE INDEX IX_Airtel_IndexNumber ON Airtel(IndexNumber);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Airtel_EbillUserId' AND object_id = OBJECT_ID('Airtel'))
    CREATE INDEX IX_Airtel_EbillUserId ON Airtel(EbillUserId);

-- PSTN
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PSTN_IndexNumber' AND object_id = OBJECT_ID('PSTN'))
    CREATE INDEX IX_PSTN_IndexNumber ON PSTN(IndexNumber);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PSTN_EbillUserId' AND object_id = OBJECT_ID('PSTN'))
    CREATE INDEX IX_PSTN_EbillUserId ON PSTN(EbillUserId);

PRINT 'Indexes created successfully';

-- ===========================================
-- STEP 5: Summary Report
-- ===========================================

PRINT 'Step 5: Generating summary report...';

SELECT
    'PrivateWires' AS TableName,
    COUNT(*) AS TotalRecords,
    COUNT(EbillUserId) AS LinkedToUser,
    COUNT(OrganizationId) AS HasOrganization,
    COUNT(OfficeId) AS HasOffice,
    COUNT(CASE WHEN EbillUserId IS NULL THEN 1 END) AS OrphanRecords
FROM PrivateWires
UNION ALL
SELECT
    'Safaricom',
    COUNT(*),
    COUNT(EbillUserId),
    COUNT(OrganizationId),
    COUNT(OfficeId),
    COUNT(CASE WHEN EbillUserId IS NULL THEN 1 END)
FROM Safaricom
UNION ALL
SELECT
    'Airtel',
    COUNT(*),
    COUNT(EbillUserId),
    COUNT(OrganizationId),
    COUNT(OfficeId),
    COUNT(CASE WHEN EbillUserId IS NULL THEN 1 END)
FROM Airtel
UNION ALL
SELECT
    'PSTN',
    COUNT(*),
    COUNT(EbillUserId),
    COUNT(OrganizationId),
    COUNT(OfficeId),
    COUNT(CASE WHEN EbillUserId IS NULL THEN 1 END)
FROM PSTN;

-- Test the unified view
SELECT TOP 10 * FROM vw_AllCallRecords WHERE Organization IS NOT NULL ORDER BY CallDate DESC;

PRINT 'Normalization complete!';

-- ===========================================
-- COLUMNS TO DEPRECATE (DO NOT RUN YET!)
-- ===========================================
/*
-- ONLY RUN AFTER THOROUGH TESTING AND BACKUP!

-- PrivateWires
ALTER TABLE PrivateWires DROP COLUMN CallerName_DEPRECATED;
ALTER TABLE PrivateWires DROP COLUMN SubOffice;
ALTER TABLE PrivateWires DROP COLUMN Level4Unit;
ALTER TABLE PrivateWires DROP COLUMN OrganizationalUnit;
ALTER TABLE PrivateWires DROP COLUMN Location;
ALTER TABLE PrivateWires DROP COLUMN OCACode;

-- Similar columns in other tables...
*/