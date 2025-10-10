-- Add Sub-Offices under UNON offices
-- First, get the UNON organization ID
DECLARE @UNONId INT;
SELECT @UNONId = Id FROM Organizations WHERE Code = 'UNON';

IF @UNONId IS NULL
BEGIN
    PRINT 'Error: UNON organization not found. Please ensure UNON exists in the Organizations table.';
    RETURN;
END

PRINT 'Adding sub-offices for UNON offices';

-- Variables for office IDs
DECLARE @ADMId INT, @BFMSId INT, @CSSId INT, @HRMSId INT, @ICTSId INT, @KSCOId INT, @SSId INT;

-- Get office IDs
SELECT @ADMId = Id FROM Offices WHERE Code = 'ADM' AND OrganizationId = @UNONId;
SELECT @BFMSId = Id FROM Offices WHERE Code = 'BFMS' AND OrganizationId = @UNONId;
SELECT @CSSId = Id FROM Offices WHERE Code = 'CSS' AND OrganizationId = @UNONId;
SELECT @HRMSId = Id FROM Offices WHERE Code = 'HRMS' AND OrganizationId = @UNONId;
SELECT @ICTSId = Id FROM Offices WHERE Code = 'ICTS' AND OrganizationId = @UNONId;
SELECT @KSCOId = Id FROM Offices WHERE Code = 'KSCO' AND OrganizationId = @UNONId;
SELECT @SSId = Id FROM Offices WHERE Code = 'SS' AND OrganizationId = @UNONId;

-- AUDITORS - Auditors (under ADM)
IF @ADMId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'AUDITORS' AND OfficeId = @ADMId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Auditors', 'AUDITORS', 'Internal and external audit functions for UNON operations', @ADMId, GETDATE());
    PRINT 'Added: Auditors (AUDITORS) under ADM';
END

-- ACS - Accounts Section (under BFMS)
IF @BFMSId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'ACS' AND OfficeId = @BFMSId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Accounts Section', 'ACS', 'Manages accounting operations and financial records', @BFMSId, GETDATE());
    PRINT 'Added: Accounts Section (ACS) under BFMS';
END

-- FMBS - Funds Management & Budget Section (under BFMS)
IF @BFMSId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'FMBS' AND OfficeId = @BFMSId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Funds Management & Budget Section', 'FMBS', 'Manages funds allocation and budget planning', @BFMSId, GETDATE());
    PRINT 'Added: Funds Management & Budget Section (FMBS) under BFMS';
END

-- OC - Other Costs / Oversight Committee (under BFMS)
IF @BFMSId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'OC' AND OfficeId = @BFMSId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Other Costs / Oversight Committee', 'OC', 'Manages miscellaneous costs and provides financial oversight', @BFMSId, GETDATE());
    PRINT 'Added: Other Costs / Oversight Committee (OC) under BFMS';
END

-- SSU - Staff Support Unit (under BFMS)
IF @BFMSId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'SSU' AND OfficeId = @BFMSId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Staff Support Unit', 'SSU', 'Provides financial support services to staff members', @BFMSId, GETDATE());
    PRINT 'Added: Staff Support Unit (SSU) under BFMS';
END

-- TREAS - Treasury (under BFMS)
IF @BFMSId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'TREAS' AND OfficeId = @BFMSId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Treasury', 'TREAS', 'Manages cash flow, banking, and treasury operations', @BFMSId, GETDATE());
    PRINT 'Added: Treasury (TREAS) under BFMS';
END

-- DOC - Documentation (under CSS)
IF @CSSId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'DOC' AND OfficeId = @CSSId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Documentation', 'DOC', 'Manages document processing and distribution services', @CSSId, GETDATE());
    PRINT 'Added: Documentation (DOC) under CSS';
END

-- MEETINGS - Meetings & Conference Services (under CSS)
IF @CSSId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'MEETINGS' AND OfficeId = @CSSId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Meetings & Conference Services', 'MEETINGS', 'Provides conference and meeting support services', @CSSId, GETDATE());
    PRINT 'Added: Meetings & Conference Services (MEETINGS) under CSS';
END

-- MPSS - Mail, Pouch & Supply Services (under CSS)
IF @CSSId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'MPSS' AND OfficeId = @CSSId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Mail, Pouch & Supply Services', 'MPSS', 'Manages mail distribution and supply chain services', @CSSId, GETDATE());
    PRINT 'Added: Mail, Pouch & Supply Services (MPSS) under CSS';
END

-- JMS - Joint Medical Services (under HRMS)
IF @HRMSId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'JMS' AND OfficeId = @HRMSId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Joint Medical Services', 'JMS', 'Provides medical and health services to UN staff', @HRMSId, GETDATE());
    PRINT 'Added: Joint Medical Services (JMS) under HRMS';
END

-- TU - Telecommunications Unit (under ICTS)
IF @ICTSId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'TU' AND OfficeId = @ICTSId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Telecommunications Unit', 'TU', 'Manages telecommunications infrastructure and services', @ICTSId, GETDATE());
    PRINT 'Added: Telecommunications Unit (TU) under ICTS';
END

-- SS sub-office - Security Section (under KSCO)
IF @KSCOId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'SS-KSCO' AND OfficeId = @KSCOId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Security Section', 'SS-KSCO', 'Security operations under Common Services Office', @KSCOId, GETDATE());
    PRINT 'Added: Security Section (SS-KSCO) under KSCO';
END

-- BGMU - Building Management Unit (under SS)
IF @SSId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'BGMU' AND OfficeId = @SSId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Building Management Unit', 'BGMU', 'Manages UN compound buildings and facilities', @SSId, GETDATE());
    PRINT 'Added: Building Management Unit (BGMU) under SS';
END

-- REG - Registry (under SS)
IF @SSId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'REG' AND OfficeId = @SSId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Registry', 'REG', 'Manages document registry and records management', @SSId, GETDATE());
    PRINT 'Added: Registry (REG) under SS';
END

-- RPA - Regional Procurement/Property Administration (under SS)
IF @SSId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'RPA' AND OfficeId = @SSId)
BEGIN
    INSERT INTO SubOffices (Name, Code, Description, OfficeId, CreatedDate)
    VALUES ('Regional Procurement/Property Administration', 'RPA', 'Manages procurement and property administration for the region', @SSId, GETDATE());
    PRINT 'Added: Regional Procurement/Property Administration (RPA) under SS';
END

PRINT 'UNON sub-offices setup completed successfully!';