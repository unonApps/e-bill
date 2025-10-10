-- Add Offices under UN-Habitat (United Nations Human Settlements Programme)
-- First, get the UN-Habitat organization ID
DECLARE @UNHABId INT;
SELECT @UNHABId = Id FROM Organizations WHERE Code = 'UN-HAB';

IF @UNHABId IS NULL
BEGIN
    PRINT 'Error: UN-Habitat organization not found. Please ensure UN-HAB exists in the Organizations table.';
    RETURN;
END

PRINT 'Adding offices for UN-Habitat (Organization ID: ' + CAST(@UNHABId AS VARCHAR(10)) + ')';

-- DED - Office of the Deputy Executive Director
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'DED' AND OrganizationId = @UNHABId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Office of the Deputy Executive Director', 'DED', 'Supports the Deputy Executive Director in managing UN-Habitat operations', @UNHABId, GETDATE());
    PRINT 'Added: Office of the Deputy Executive Director (DED)';
END

-- GD - Global Division
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'GD' AND OrganizationId = @UNHABId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Global Division / Governance Division', 'GD', 'Manages global initiatives and governance frameworks for sustainable urban development', @UNHABId, GETDATE());
    PRINT 'Added: Global Division / Governance Division (GD)';
END

-- ODED - Office of the Deputy Executive Director
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'ODED' AND OrganizationId = @UNHABId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Office of the Deputy Executive Director', 'ODED', 'Executive support office for the Deputy Executive Director', @UNHABId, GETDATE());
    PRINT 'Added: Office of the Deputy Executive Director (ODED)';
END

-- OED - Office of the Executive Director
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'OED' AND OrganizationId = @UNHABId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Office of the Executive Director', 'OED', 'Executive office providing leadership and strategic direction for UN-Habitat', @UNHABId, GETDATE());
    PRINT 'Added: Office of the Executive Director (OED)';
END

-- PSD - Programme Support Division
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'PSD' AND OrganizationId = @UNHABId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Programme Support Division', 'PSD', 'Provides administrative and operational support for UN-Habitat programmes', @UNHABId, GETDATE());
    PRINT 'Added: Programme Support Division (PSD)';
END

-- RDD - Research & Development Division (legacy)
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'RDD' AND OrganizationId = @UNHABId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Research & Development Division (legacy)', 'RDD', 'Legacy division for urban research and development initiatives', @UNHABId, GETDATE());
    PRINT 'Added: Research & Development Division (legacy) (RDD)';
END

-- RTCD - Regional & Technical Cooperation Division (legacy)
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'RTCD' AND OrganizationId = @UNHABId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Regional & Technical Cooperation Division (legacy)', 'RTCD', 'Legacy division for regional partnerships and technical cooperation', @UNHABId, GETDATE());
    PRINT 'Added: Regional & Technical Cooperation Division (legacy) (RTCD)';
END

-- UDB - Urban Development Branch
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'UDB' AND OrganizationId = @UNHABId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Urban Development Branch', 'UDB', 'Focuses on sustainable urban development policies and practices', @UNHABId, GETDATE());
    PRINT 'Added: Urban Development Branch (UDB)';
END

-- UDGB - Urban Development & Governance Branch
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'UDGB' AND OrganizationId = @UNHABId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Urban Development & Governance Branch', 'UDGB', 'Integrates urban development with governance frameworks for cities', @UNHABId, GETDATE());
    PRINT 'Added: Urban Development & Governance Branch (UDGB)';
END

-- US - Urban Services Division
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'US' AND OrganizationId = @UNHABId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Urban Services Division', 'US', 'Manages urban services including water, sanitation, and infrastructure', @UNHABId, GETDATE());
    PRINT 'Added: Urban Services Division (US)';
END

PRINT 'UN-Habitat offices setup completed successfully!';