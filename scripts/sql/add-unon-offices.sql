-- Add Offices under UNON (United Nations Office at Nairobi)
-- First, get the UNON organization ID
DECLARE @UNONId INT;
SELECT @UNONId = Id FROM Organizations WHERE Code = 'UNON';

IF @UNONId IS NULL
BEGIN
    PRINT 'Error: UNON organization not found. Please ensure UNON exists in the Organizations table.';
    RETURN;
END

PRINT 'Adding offices for UNON (Organization ID: ' + CAST(@UNONId AS VARCHAR(10)) + ')';

-- ADM - Administrative Division
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'ADM' AND OrganizationId = @UNONId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Administrative Division', 'ADM', 'Manages administrative functions and operations for UNON', @UNONId, GETDATE());
    PRINT 'Added: Administrative Division (ADM)';
END

-- BFMS - Budget & Financial Management Service
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'BFMS' AND OrganizationId = @UNONId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Budget & Financial Management Service', 'BFMS', 'Handles budget planning, financial management, and fiscal operations', @UNONId, GETDATE());
    PRINT 'Added: Budget & Financial Management Service (BFMS)';
END

-- CSS - Central Support Service
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'CSS' AND OrganizationId = @UNONId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Central Support Service', 'CSS', 'Provides central support services and coordination', @UNONId, GETDATE());
    PRINT 'Added: Central Support Service (CSS)';
END

-- HRMS - Human Resources Management Service
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'HRMS' AND OrganizationId = @UNONId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Human Resources Management Service', 'HRMS', 'Manages human resources, recruitment, and staff development', @UNONId, GETDATE());
    PRINT 'Added: Human Resources Management Service (HRMS)';
END

-- ICTS - Information & Communication Technology Service
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'ICTS' AND OrganizationId = @UNONId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Information & Communication Technology Service', 'ICTS', 'Provides ICT infrastructure, support, and digital solutions', @UNONId, GETDATE());
    PRINT 'Added: Information & Communication Technology Service (ICTS)';
END

-- KSCO - Common Services Office
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'KSCO' AND OrganizationId = @UNONId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Common Services Office', 'KSCO', 'Coordinates common services and staff cooperative activities', @UNONId, GETDATE());
    PRINT 'Added: Common Services Office (KSCO)';
END

-- ODG - Office of the Director-General
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'ODG' AND OrganizationId = @UNONId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Office of the Director-General', 'ODG', 'Executive office providing strategic leadership and coordination', @UNONId, GETDATE());
    PRINT 'Added: Office of the Director-General (ODG)';
END

-- OSLA - Office of Staff Legal Assistance
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'OSLA' AND OrganizationId = @UNONId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Office of Staff Legal Assistance', 'OSLA', 'Provides legal assistance and advisory services to staff', @UNONId, GETDATE());
    PRINT 'Added: Office of Staff Legal Assistance (OSLA)';
END

-- SS - Security & Safety Service
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'SS' AND OrganizationId = @UNONId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Security & Safety Service', 'SS', 'Ensures security, safety, and emergency response for UNON premises', @UNONId, GETDATE());
    PRINT 'Added: Security & Safety Service (SS)';
END

-- UNIC - United Nations Information Centre, Nairobi
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'UNIC' AND OrganizationId = @UNONId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('United Nations Information Centre, Nairobi', 'UNIC', 'Manages public information and communications for UN activities in the region', @UNONId, GETDATE());
    PRINT 'Added: United Nations Information Centre, Nairobi (UNIC)';
END

PRINT 'UNON offices setup completed successfully!';