-- =====================================================
-- Normalize ALL Telecom Tables (PrivateWires, Safaricom, Airtel, PSTNs)
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

-- PSTNs (note the plural)
UPDATE p
SET p.EbillUserId = eu.Id
FROM PSTNs p
INNER JOIN EbillUsers eu ON p.IndexNumber = eu.IndexNumber
WHERE p.EbillUserId IS NULL;

PRINT 'PSTNs: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records updated';

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

-- PSTNs
UPDATE p
SET
    p.OrganizationId = eu.OrganizationId,
    p.OfficeId = eu.OfficeId,
    p.SubOfficeId = eu.SubOfficeId
FROM PSTNs p
INNER JOIN EbillUsers eu ON p.EbillUserId = eu.Id
WHERE p.OrganizationId IS NULL OR p.OfficeId IS NULL;

PRINT 'PSTNs: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records updated';

-- ===========================================
-- STEP 3: Create Indexes for Performance
-- ===========================================

PRINT 'Step 3: Creating indexes for better performance...';

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

-- PSTNs
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PSTNs_IndexNumber' AND object_id = OBJECT_ID('PSTNs'))
    CREATE INDEX IX_PSTNs_IndexNumber ON PSTNs(IndexNumber);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PSTNs_EbillUserId' AND object_id = OBJECT_ID('PSTNs'))
    CREATE INDEX IX_PSTNs_EbillUserId ON PSTNs(EbillUserId);

PRINT 'Indexes created successfully';

-- ===========================================
-- STEP 4: Summary Report
-- ===========================================

PRINT 'Step 4: Generating summary report...';

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
    'PSTNs',
    COUNT(*),
    COUNT(EbillUserId),
    COUNT(OrganizationId),
    COUNT(OfficeId),
    COUNT(CASE WHEN EbillUserId IS NULL THEN 1 END)
FROM PSTNs;

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