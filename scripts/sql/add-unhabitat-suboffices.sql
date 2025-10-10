-- Add Sub-Offices under UN-Habitat offices
-- First, get the UN-Habitat organization ID
DECLARE @UNHABId INT;
SELECT @UNHABId = Id FROM Organizations WHERE Code = 'UN-HAB';

IF @UNHABId IS NULL
BEGIN
    PRINT 'Error: UN-Habitat organization not found. Please ensure UN-HAB exists in the Organizations table.';
    RETURN;
END

PRINT 'Adding sub-offices for UN-Habitat offices';

-- Variables for office IDs
DECLARE @GDId INT, @ODEDId INT, @OEDId INT, @PSDId INT, @RTCDId INT, @UDBId INT, @UDGBId INT, @USId INT;

-- Get office IDs
SELECT @GDId = Id FROM Offices WHERE Code = 'GD' AND OrganizationId = @UNHABId;
SELECT @ODEDId = Id FROM Offices WHERE Code = 'ODED' AND OrganizationId = @UNHABId;
SELECT @OEDId = Id FROM Offices WHERE Code = 'OED' AND OrganizationId = @UNHABId;
SELECT @PSDId = Id FROM Offices WHERE Code = 'PSD' AND OrganizationId = @UNHABId;
SELECT @RTCDId = Id FROM Offices WHERE Code = 'RTCD' AND OrganizationId = @UNHABId;
SELECT @UDBId = Id FROM Offices WHERE Code = 'UDB' AND OrganizationId = @UNHABId;
SELECT @UDGBId = Id FROM Offices WHERE Code = 'UDGB' AND OrganizationId = @UNHABId;
SELECT @USId = Id FROM Offices WHERE Code = 'US' AND OrganizationId = @UNHABId;

-- INFS - Information & Finance Section (General - not tied to specific office)
IF @PSDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'INFS' AND OfficeId = @PSDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Information & Finance Section', 'INFS', 'Manages information systems and financial operations', @PSDId, GETDATE());
    PRINT 'Added: Information & Finance Section (INFS) under PSD';
END

-- FOS - Finance & Oversight Section
IF @PSDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'FOS' AND OfficeId = @PSDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Finance & Oversight Section', 'FOS', 'Provides financial management and oversight services', @PSDId, GETDATE());
    PRINT 'Added: Finance & Oversight Section (FOS) under PSD';
END

-- SB - Sub-Branch (under GD)
IF @GDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'SB' AND OfficeId = @GDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Sub-Branch', 'SB', 'Subsidiary branch supporting global division operations', @GDId, GETDATE());
    PRINT 'Added: Sub-Branch (SB) under GD';
END

-- TCBB - Technical Cooperation Branch
IF @RTCDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'TCBB' AND OfficeId = @RTCDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Technical Cooperation Branch', 'TCBB', 'Manages technical cooperation programmes and partnerships', @RTCDId, GETDATE());
    PRINT 'Added: Technical Cooperation Branch (TCBB) under RTCD';
END

-- UDB sub-offices under UDB itself
IF @UDBId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'UDB' AND OfficeId = @UDBId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Urban Development Branch Sub-Unit', 'UDB', 'Specialized unit within Urban Development Branch', @UDBId, GETDATE());
    PRINT 'Added: Urban Development Branch Sub-Unit (UDB) under UDB';
END

-- UDGB sub-offices under UDGB itself
IF @UDGBId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'UDGB' AND OfficeId = @UDGBId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Urban Development & Governance Branch Sub-Unit', 'UDGB', 'Specialized unit within Urban Development & Governance Branch', @UDGBId, GETDATE());
    PRINT 'Added: Urban Development & Governance Branch Sub-Unit (UDGB) under UDGB';
END

-- USI - Urban Services Infrastructure
IF @USId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'USI' AND OfficeId = @USId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Urban Services Infrastructure', 'USI', 'Manages urban infrastructure and services development', @USId, GETDATE());
    PRINT 'Added: Urban Services Infrastructure (USI) under US';
END

-- GC - Governing Council (under ODED)
IF @ODEDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'GC' AND OfficeId = @ODEDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Governing Council', 'GC', 'Supports the Governing Council of UN-Habitat', @ODEDId, GETDATE());
    PRINT 'Added: Governing Council (GC) under ODED';
END

-- FO - Finance Office (under PSD)
IF @PSDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'FO-PSD' AND OfficeId = @PSDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Finance Office', 'FO-PSD', 'Finance office supporting programme operations', @PSDId, GETDATE());
    PRINT 'Added: Finance Office (FO-PSD) under PSD';
END

-- FO - Finance Office (under OED)
IF @OEDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'FO-OED' AND OfficeId = @OEDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Finance Office', 'FO-OED', 'Executive finance office', @OEDId, GETDATE());
    PRINT 'Added: Finance Office (FO-OED) under OED';
END

-- FO - Finance Office (under US)
IF @USId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'FO-US' AND OfficeId = @USId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Finance Office', 'FO-US', 'Finance office for urban services', @USId, GETDATE());
    PRINT 'Added: Finance Office (FO-US) under US';
END

-- IU - Internal Unit (under PSD)
IF @PSDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'IU' AND OfficeId = @PSDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Internal Unit', 'IU', 'Internal coordination and support unit', @PSDId, GETDATE());
    PRINT 'Added: Internal Unit (IU) under PSD';
END

-- MSU - Management Support Unit
IF @PSDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'MSU' AND OfficeId = @PSDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Management Support Unit', 'MSU', 'Provides management and administrative support services', @PSDId, GETDATE());
    PRINT 'Added: Management Support Unit (MSU) under PSD';
END

-- PCO - Programme Coordination Office
IF @PSDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'PCO' AND OfficeId = @PSDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Programme Coordination Office', 'PCO', 'Coordinates UN-Habitat programme activities', @PSDId, GETDATE());
    PRINT 'Added: Programme Coordination Office (PCO) under PSD';
END

-- RMEA - Regional Monitoring & Evaluation/Assessment
IF @RTCDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'RMEA' AND OfficeId = @RTCDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Regional Monitoring & Evaluation/Assessment', 'RMEA', 'Conducts regional monitoring, evaluation and assessment', @RTCDId, GETDATE());
    PRINT 'Added: Regional Monitoring & Evaluation/Assessment (RMEA) under RTCD';
END

-- ROAAS - Regional Office for Africa & Arab States (under RTCD)
IF @RTCDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'ROAAS' AND OfficeId = @RTCDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Regional Office for Africa & Arab States', 'ROAAS', 'Regional office covering Africa and Arab States regions', @RTCDId, GETDATE());
    PRINT 'Added: Regional Office for Africa & Arab States (ROAAS) under RTCD';
END

-- TABS - Technical Advisory Branch/Section (under RTCD)
IF @RTCDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'TABS' AND OfficeId = @RTCDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Technical Advisory Branch/Section', 'TABS', 'Provides technical advisory services for regional programmes', @RTCDId, GETDATE());
    PRINT 'Added: Technical Advisory Branch/Section (TABS) under RTCD';
END

-- BPLL - Budget, Planning & Liaison (under US)
IF @USId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'BPLL' AND OfficeId = @USId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Budget, Planning & Liaison', 'BPLL', 'Manages budget planning and liaison for urban services', @USId, GETDATE());
    PRINT 'Added: Budget, Planning & Liaison (BPLL) under US';
END

-- GEN - General Services (under US)
IF @USId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'GEN' AND OfficeId = @USId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('General Services', 'GEN', 'Provides general administrative and support services', @USId, GETDATE());
    PRINT 'Added: General Services (GEN) under US';
END

-- GUO - Governance Unit/Office (under US)
IF @USId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'GUO' AND OfficeId = @USId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Governance Unit/Office', 'GUO', 'Manages governance aspects of urban services', @USId, GETDATE());
    PRINT 'Added: Governance Unit/Office (GUO) under US';
END

-- PAR - Partnerships (under US)
IF @USId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'PAR' AND OfficeId = @USId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Partnerships', 'PAR', 'Manages partnerships for urban services development', @USId, GETDATE());
    PRINT 'Added: Partnerships (PAR) under US';
END

-- UEF - Urban Economy & Finance (under US)
IF @USId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'UEF' AND OfficeId = @USId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Urban Economy & Finance', 'UEF', 'Focuses on urban economic development and financial solutions', @USId, GETDATE());
    PRINT 'Added: Urban Economy & Finance (UEF) under US';
END

PRINT 'UN-Habitat sub-offices setup completed successfully!';