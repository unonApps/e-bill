-- =====================================================
-- Normalize PrivateWires Table
-- Remove redundant columns and use EbillUsers relationship
-- =====================================================

-- Step 1: Analyze current data relationships
-- Check how many records have matching EbillUsers
SELECT
    COUNT(*) AS TotalRecords,
    COUNT(DISTINCT pw.IndexNumber) AS UniqueIndexNumbers,
    COUNT(eu.Id) AS MatchingEbillUsers,
    COUNT(*) - COUNT(eu.Id) AS MissingEbillUsers
FROM PrivateWires pw
LEFT JOIN EbillUsers eu ON pw.IndexNumber = eu.IndexNumber;

-- Step 2: Show sample of unmatched records
SELECT TOP 10
    pw.Id,
    pw.IndexNumber,
    pw.CallerName_DEPRECATED,
    pw.SubOffice,
    pw.OrganizationalUnit,
    pw.Location
FROM PrivateWires pw
LEFT JOIN EbillUsers eu ON pw.IndexNumber = eu.IndexNumber
WHERE eu.Id IS NULL;

-- Step 3: Update EbillUserId for all matching records
UPDATE pw
SET pw.EbillUserId = eu.Id
FROM PrivateWires pw
INNER JOIN EbillUsers eu ON pw.IndexNumber = eu.IndexNumber
WHERE pw.EbillUserId IS NULL;

-- Verify the update
SELECT
    COUNT(*) AS UpdatedRecords
FROM PrivateWires
WHERE EbillUserId IS NOT NULL;

-- Step 4: For records with EbillUserId, populate Organization/Office/SubOffice IDs
UPDATE pw
SET
    pw.OrganizationId = eu.OrganizationId,
    pw.OfficeId = eu.OfficeId,
    pw.SubOfficeId = eu.SubOfficeId
FROM PrivateWires pw
INNER JOIN EbillUsers eu ON pw.EbillUserId = eu.Id
WHERE pw.OrganizationId IS NULL OR pw.OfficeId IS NULL;

-- Step 5: Create a view that joins the data properly
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_PrivateWires_Normalized')
    DROP VIEW vw_PrivateWires_Normalized;
GO

CREATE VIEW vw_PrivateWires_Normalized AS
SELECT
    pw.Id,
    pw.Extension,
    pw.DestinationLine,
    pw.DurationExtended,
    pw.DialedNumber,
    pw.CallTime,
    pw.Destination,
    pw.AmountUSD,
    pw.CallDate,
    pw.Duration,
    pw.IndexNumber,
    pw.BillingPeriod,
    pw.CallMonth,
    pw.CallYear,
    pw.ProcessingStatus,
    pw.ProcessedDate,

    -- Get user info from EbillUsers
    eu.FirstName,
    eu.LastName,
    eu.FirstName + ' ' + eu.LastName AS CallerName,
    eu.Email,
    eu.OfficialMobileNumber,

    -- Get organization info through relationships
    COALESCE(org.Name, pw.OrganizationalUnit) AS Organization,
    COALESCE(ofc.Name, pw.SubOffice) AS Office,
    COALESCE(subof.Name, pw.Level4Unit) AS SubOffice,
    COALESCE(eu.Location, pw.Location) AS Location,

    -- Keep IDs for filtering
    pw.EbillUserId,
    COALESCE(eu.OrganizationId, pw.OrganizationId) AS OrganizationId,
    COALESCE(eu.OfficeId, pw.OfficeId) AS OfficeId,
    COALESCE(eu.SubOfficeId, pw.SubOfficeId) AS SubOfficeId,

    -- Audit fields
    pw.CreatedDate,
    pw.CreatedBy,
    pw.ModifiedDate,
    pw.ModifiedBy,
    pw.ImportAuditId,
    pw.StagingBatchId

FROM PrivateWires pw
LEFT JOIN EbillUsers eu ON pw.EbillUserId = eu.Id OR pw.IndexNumber = eu.IndexNumber
LEFT JOIN Organizations org ON COALESCE(eu.OrganizationId, pw.OrganizationId) = org.Id
LEFT JOIN Offices ofc ON COALESCE(eu.OfficeId, pw.OfficeId) = ofc.Id
LEFT JOIN SubOffices subof ON COALESCE(eu.SubOfficeId, pw.SubOfficeId) = subof.Id;
GO

-- Step 6: Test the view
SELECT TOP 10 * FROM vw_PrivateWires_Normalized;

-- Step 7: Create indexes for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PrivateWires_IndexNumber')
    CREATE INDEX IX_PrivateWires_IndexNumber ON PrivateWires(IndexNumber);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PrivateWires_EbillUserId')
    CREATE INDEX IX_PrivateWires_EbillUserId ON PrivateWires(EbillUserId);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PrivateWires_BillingPeriod')
    CREATE INDEX IX_PrivateWires_BillingPeriod ON PrivateWires(BillingPeriod);

-- Step 8: After verification, these columns can be deprecated (DO NOT RUN until verified!)
/*
-- ONLY RUN AFTER THOROUGH TESTING!
ALTER TABLE PrivateWires DROP COLUMN CallerName_DEPRECATED;
ALTER TABLE PrivateWires DROP COLUMN SubOffice;
ALTER TABLE PrivateWires DROP COLUMN Level4Unit;
ALTER TABLE PrivateWires DROP COLUMN OrganizationalUnit;
ALTER TABLE PrivateWires DROP COLUMN Location;
ALTER TABLE PrivateWires DROP COLUMN OCACode;
*/

-- Step 9: Summary Report
SELECT
    'PrivateWires Normalization Summary' AS Report,
    COUNT(*) AS TotalRecords,
    COUNT(EbillUserId) AS RecordsWithEbillUser,
    COUNT(OrganizationId) AS RecordsWithOrganization,
    COUNT(OfficeId) AS RecordsWithOffice,
    COUNT(SubOfficeId) AS RecordsWithSubOffice,
    COUNT(CASE WHEN EbillUserId IS NULL THEN 1 END) AS OrphanRecords
FROM PrivateWires;