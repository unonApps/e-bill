-- Add Offices under UNEP (United Nations Environment Programme)
-- First, get the UNEP organization ID
DECLARE @UNEPId INT;
SELECT @UNEPId = Id FROM Organizations WHERE Code = 'UNEP';

IF @UNEPId IS NULL
BEGIN
    PRINT 'Error: UNEP organization not found. Please ensure UNEP exists in the Organizations table.';
    RETURN;
END

PRINT 'Adding offices for UNEP (Organization ID: ' + CAST(@UNEPId AS VARCHAR(10)) + ')';

-- BIO - Biodiversity Division
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'BIO' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Biodiversity Division', 'BIO', 'Manages biodiversity conservation and ecosystem protection initiatives', @UNEPId, GETDATE());
    PRINT 'Added: Biodiversity Division (BIO)';
END

-- CPI - Chemicals & Pollution Implementation Division
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'CPI' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Chemicals & Pollution Implementation Division', 'CPI', 'Implements policies and programs for chemical safety and pollution control', @UNEPId, GETDATE());
    PRINT 'Added: Chemicals & Pollution Implementation Division (CPI)';
END

-- DEC - Division of Environmental Conventions / Law
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'DEC' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Division of Environmental Conventions / Law', 'DEC', 'Manages environmental conventions and legal frameworks', @UNEPId, GETDATE());
    PRINT 'Added: Division of Environmental Conventions / Law (DEC)';
END

-- DEPDL - Division of Environmental Policy Development & Law (legacy)
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'DEPDL' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Division of Environmental Policy Development & Law (legacy)', 'DEPDL', 'Legacy division for environmental policy development and legal affairs', @UNEPId, GETDATE());
    PRINT 'Added: Division of Environmental Policy Development & Law (legacy) (DEPDL)';
END

-- DEPI - Division of Environmental Policy Implementation
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'DEPI' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Division of Environmental Policy Implementation', 'DEPI', 'Implements environmental policies and strategies at regional and global levels', @UNEPId, GETDATE());
    PRINT 'Added: Division of Environmental Policy Implementation (DEPI)';
END

-- DEWA - Division of Early Warning and Assessment
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'DEWA' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Division of Early Warning and Assessment', 'DEWA', 'Provides early warning systems and environmental assessments', @UNEPId, GETDATE());
    PRINT 'Added: Division of Early Warning and Assessment (DEWA)';
END

-- DRCR - Division of Regional Cooperation and Representation
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'DRCR' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Division of Regional Cooperation and Representation', 'DRCR', 'Coordinates regional cooperation and represents UNEP at regional levels', @UNEPId, GETDATE());
    PRINT 'Added: Division of Regional Cooperation and Representation (DRCR)';
END

-- EO - Evaluation Office / Executive Office
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'EO' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Evaluation Office / Executive Office', 'EO', 'Conducts evaluations and provides executive support services', @UNEPId, GETDATE());
    PRINT 'Added: Evaluation Office / Executive Office (EO)';
END

-- EOU - Evaluation and Oversight Unit
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'EOU' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Evaluation and Oversight Unit', 'EOU', 'Provides evaluation and oversight for UNEP programs and projects', @UNEPId, GETDATE());
    PRINT 'Added: Evaluation and Oversight Unit (EOU)';
END

-- GEF - Global Environment Facility
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'GEF' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Global Environment Facility', 'GEF', 'UNEP unit implementing GEF-funded environmental projects', @UNEPId, GETDATE());
    PRINT 'Added: Global Environment Facility (GEF)';
END

-- ODED - Office of the Deputy Executive Director
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'ODED' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Office of the Deputy Executive Director', 'ODED', 'Supports the Deputy Executive Director in management and strategic planning', @UNEPId, GETDATE());
    PRINT 'Added: Office of the Deputy Executive Director (ODED)';
END

-- OED - Office of the Executive Director
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'OED' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Office of the Executive Director', 'OED', 'Executive office providing leadership and strategic direction for UNEP', @UNEPId, GETDATE());
    PRINT 'Added: Office of the Executive Director (OED)';
END

-- OZONE - Ozone Secretariat
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'OZONE' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Ozone Secretariat', 'OZONE', 'Secretariat for the Vienna Convention and Montreal Protocol on ozone protection', @UNEPId, GETDATE());
    PRINT 'Added: Ozone Secretariat (OZONE)';
END

-- PCMU - Programme Coordination & Management Unit
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'PCMU' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Programme Coordination & Management Unit', 'PCMU', 'Coordinates and manages UNEP programmes and initiatives', @UNEPId, GETDATE());
    PRINT 'Added: Programme Coordination & Management Unit (PCMU)';
END

-- SGB - Senior Governance Branch
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'SGB' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Senior Governance Branch', 'SGB', 'Manages governance structures and senior management processes', @UNEPId, GETDATE());
    PRINT 'Added: Senior Governance Branch (SGB)';
END

-- UNICEN - UN Information Centre
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'UNICEN' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('UN Information Centre', 'UNICEN', 'Information and communications centre for UNEP activities', @UNEPId, GETDATE());
    PRINT 'Added: UN Information Centre (UNICEN)';
END

PRINT 'UNEP offices setup completed successfully!';