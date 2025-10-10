-- Add Sub-Offices under UNEP offices
-- First, get the UNEP organization ID
DECLARE @UNEPId INT;
SELECT @UNEPId = Id FROM Organizations WHERE Code = 'UNEP';

IF @UNEPId IS NULL
BEGIN
    PRINT 'Error: UNEP organization not found. Please ensure UNEP exists in the Organizations table.';
    RETURN;
END

PRINT 'Adding sub-offices for UNEP offices';

-- Variables for office IDs
DECLARE @CPIId INT, @DECId INT, @DEPDLId INT, @DEPIId INT, @DEWAId INT, @DRCRId INT, @EOId INT, @OEDId INT;

-- Get office IDs
SELECT @CPIId = Id FROM Offices WHERE Code = 'CPI' AND OrganizationId = @UNEPId;
SELECT @DECId = Id FROM Offices WHERE Code = 'DEC' AND OrganizationId = @UNEPId;
SELECT @DEPDLId = Id FROM Offices WHERE Code = 'DEPDL' AND OrganizationId = @UNEPId;
SELECT @DEPIId = Id FROM Offices WHERE Code = 'DEPI' AND OrganizationId = @UNEPId;
SELECT @DEWAId = Id FROM Offices WHERE Code = 'DEWA' AND OrganizationId = @UNEPId;
SELECT @DRCRId = Id FROM Offices WHERE Code = 'DRCR' AND OrganizationId = @UNEPId;
SELECT @EOId = Id FROM Offices WHERE Code = 'EO' AND OrganizationId = @UNEPId;
SELECT @OEDId = Id FROM Offices WHERE Code = 'OED' AND OrganizationId = @UNEPId;

-- LIB - Law & Implementation Branch (under CPI)
IF @CPIId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'LIB' AND OfficeId = @CPIId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Law & Implementation Branch', 'LIB', 'Manages legal frameworks and implementation for chemical and pollution policies', @CPIId, GETDATE());
    PRINT 'Added: Law & Implementation Branch (LIB) under CPI';
END

-- ODS - Ozone Depleting Substances Branch (under CPI)
IF @CPIId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'ODS' AND OfficeId = @CPIId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Ozone Depleting Substances Branch', 'ODS', 'Focuses on monitoring and reducing ozone depleting substances', @CPIId, GETDATE());
    PRINT 'Added: Ozone Depleting Substances Branch (ODS) under CPI';
END

-- MEA - Multilateral Environmental Agreements (under DEC)
IF @DECId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'MEA' AND OfficeId = @DECId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Multilateral Environmental Agreements', 'MEA', 'Manages multilateral environmental agreements and conventions', @DECId, GETDATE());
    PRINT 'Added: Multilateral Environmental Agreements (MEA) under DEC';
END

-- OD - Office of the Director (under DEC)
IF @DECId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'OD-DEC' AND OfficeId = @DECId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Office of the Director', 'OD-DEC', 'Director''s office for Environmental Conventions / Law Division', @DECId, GETDATE());
    PRINT 'Added: Office of the Director (OD-DEC) under DEC';
END

-- PLS - Policy & Law Section (under DEC)
IF @DECId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'PLS' AND OfficeId = @DECId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Policy & Law Section', 'PLS', 'Develops environmental policy and legal frameworks', @DECId, GETDATE());
    PRINT 'Added: Policy & Law Section (PLS) under DEC';
END

-- LEO - Law & Environment Office (under DEPDL)
IF @DEPDLId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'LEO' AND OfficeId = @DEPDLId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Law & Environment Office', 'LEO', 'Integrates legal considerations into environmental policy development', @DEPDLId, GETDATE());
    PRINT 'Added: Law & Environment Office (LEO) under DEPDL';
END

-- PADELIA - Programme for the Development & Environmental Law in Africa (under DEPDL)
IF @DEPDLId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'PADELIA' AND OfficeId = @DEPDLId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Programme for the Development & Environmental Law in Africa', 'PADELIA', 'Supports environmental law development in African countries', @DEPDLId, GETDATE());
    PRINT 'Added: Programme for the Development & Environmental Law in Africa (PADELIA) under DEPDL';
END

-- PARD - Policy Analysis & Research Division (under DEPDL)
IF @DEPDLId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'PARD' AND OfficeId = @DEPDLId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Policy Analysis & Research Division', 'PARD', 'Conducts policy analysis and research for environmental issues', @DEPDLId, GETDATE());
    PRINT 'Added: Policy Analysis & Research Division (PARD) under DEPDL';
END

-- PCIAA - Policy & Compliance / International Agreements (under DEPDL)
IF @DEPDLId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'PCIAA' AND OfficeId = @DEPDLId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Policy & Compliance / International Agreements', 'PCIAA', 'Ensures policy compliance with international environmental agreements', @DEPDLId, GETDATE());
    PRINT 'Added: Policy & Compliance / International Agreements (PCIAA) under DEPDL';
END

-- RMU - Resource Management Unit (under DEPDL)
IF @DEPDLId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'RMU' AND OfficeId = @DEPDLId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Resource Management Unit', 'RMU', 'Manages resources for environmental policy development', @DEPDLId, GETDATE());
    PRINT 'Added: Resource Management Unit (RMU) under DEPDL';
END

-- TC - Technical Cooperation (under DEPI)
IF @DEPIId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'TC' AND OfficeId = @DEPIId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Technical Cooperation', 'TC', 'Provides technical cooperation for environmental policy implementation', @DEPIId, GETDATE());
    PRINT 'Added: Technical Cooperation (TC) under DEPI';
END

-- CBPS - Capacity Building & Policy Support (under DEWA)
IF @DEWAId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'CBPS' AND OfficeId = @DEWAId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Capacity Building & Policy Support', 'CBPS', 'Builds capacity and provides policy support for environmental monitoring', @DEWAId, GETDATE());
    PRINT 'Added: Capacity Building & Policy Support (CBPS) under DEWA';
END

-- CCSI - Climate Change & Scientific Information (under DEWA)
IF @DEWAId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'CCSI' AND OfficeId = @DEWAId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Climate Change & Scientific Information', 'CCSI', 'Provides scientific information on climate change and environmental trends', @DEWAId, GETDATE());
    PRINT 'Added: Climate Change & Scientific Information (CCSI) under DEWA';
END

-- EIS - Environmental Information Systems (under DEWA)
IF @DEWAId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'EIS' AND OfficeId = @DEWAId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Environmental Information Systems', 'EIS', 'Manages environmental data and information systems', @DEWAId, GETDATE());
    PRINT 'Added: Environmental Information Systems (EIS) under DEWA';
END

-- EWIF - Early Warning & Information Facility (under DEWA)
IF @DEWAId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'EWIF' AND OfficeId = @DEWAId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Early Warning & Information Facility', 'EWIF', 'Provides early warning systems for environmental threats', @DEWAId, GETDATE());
    PRINT 'Added: Early Warning & Information Facility (EWIF) under DEWA';
END

-- GEO - Global Environment Outlook (under DEWA)
IF @DEWAId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'GEO' AND OfficeId = @DEWAId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Global Environment Outlook', 'GEO', 'Produces comprehensive global environmental assessments', @DEWAId, GETDATE());
    PRINT 'Added: Global Environment Outlook (GEO) under DEWA';
END

-- RC - Regional Cooperation (under DEWA)
IF @DEWAId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'RC' AND OfficeId = @DEWAId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Regional Cooperation', 'RC', 'Coordinates regional environmental monitoring and assessment', @DEWAId, GETDATE());
    PRINT 'Added: Regional Cooperation (RC) under DEWA';
END

-- ROA - Regional Office for Africa (under DRCR)
IF @DRCRId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'ROA' AND OfficeId = @DRCRId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Regional Office for Africa', 'ROA', 'UNEP''s regional office coordinating African environmental initiatives', @DRCRId, GETDATE());
    PRINT 'Added: Regional Office for Africa (ROA) under DRCR';
END

-- CSS - Corporate Support Services (under EO)
IF @EOId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'CSS' AND OfficeId = @EOId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Corporate Support Services', 'CSS', 'Provides corporate and administrative support services', @EOId, GETDATE());
    PRINT 'Added: Corporate Support Services (CSS) under EO';
END

-- ISS - Inspection & Security Section (under OED)
IF @OEDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'ISS' AND OfficeId = @OEDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Inspection & Security Section', 'ISS', 'Manages inspection and security operations for UNEP', @OEDId, GETDATE());
    PRINT 'Added: Inspection & Security Section (ISS) under OED';
END

-- OMB&SCU - Office Management, Budget & Support Coordination Unit (under OED)
IF @OEDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'OMB&SCU' AND OfficeId = @OEDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Office Management, Budget & Support Coordination Unit', 'OMB&SCU', 'Coordinates office management, budget, and support services', @OEDId, GETDATE());
    PRINT 'Added: Office Management, Budget & Support Coordination Unit (OMB&SCU) under OED';
END

-- STAFF ASS - Staff Association (general UNEP entry)
IF @OEDId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'STAFF ASS' AND OfficeId = @OEDId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Staff Association', 'STAFF ASS', 'UNEP/UNON staff association for employee representation', @OEDId, GETDATE());
    PRINT 'Added: Staff Association (STAFF ASS) under OED';
END

-- Add OD offices for other divisions that commonly have them
-- OD - Office of the Director (under DEPI)
IF @DEPIId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'OD-DEPI' AND OfficeId = @DEPIId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Office of the Director', 'OD-DEPI', 'Director''s office for Environmental Policy Implementation', @DEPIId, GETDATE());
    PRINT 'Added: Office of the Director (OD-DEPI) under DEPI';
END

-- OD - Office of the Director (under DEWA)
IF @DEWAId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'OD-DEWA' AND OfficeId = @DEWAId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Office of the Director', 'OD-DEWA', 'Director''s office for Early Warning and Assessment', @DEWAId, GETDATE());
    PRINT 'Added: Office of the Director (OD-DEWA) under DEWA';
END

-- OD - Office of the Director (under DRCR)
IF @DRCRId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'OD-DRCR' AND OfficeId = @DRCRId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Office of the Director', 'OD-DRCR', 'Director''s office for Regional Cooperation and Representation', @DRCRId, GETDATE());
    PRINT 'Added: Office of the Director (OD-DRCR) under DRCR';
END

PRINT 'UNEP sub-offices setup completed successfully!';