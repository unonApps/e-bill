-- Add Offices under OIOS (Office of Internal Oversight Services)
-- First, get the OIOS organization ID
DECLARE @OIOSId INT;
SELECT @OIOSId = Id FROM Organizations WHERE Code = 'OIOS';

IF @OIOSId IS NULL
BEGIN
    PRINT 'Error: OIOS organization not found. Please ensure OIOS exists in the Organizations table.';
    RETURN;
END

PRINT 'Adding offices for OIOS (Organization ID: ' + CAST(@OIOSId AS VARCHAR(10)) + ')';

-- IAD - Internal Audit Division
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'IAD' AND OrganizationId = @OIOSId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Internal Audit Division', 'IAD', 'Conducts internal audits to ensure compliance and efficiency across UN operations', @OIOSId, GETDATE());
    PRINT 'Added: Internal Audit Division (IAD)';
END

-- INV - Investigations Division
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'INV' AND OrganizationId = @OIOSId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Investigations Division', 'INV', 'Investigates allegations of misconduct, fraud, and violations of UN regulations', @OIOSId, GETDATE());
    PRINT 'Added: Investigations Division (INV)';
END

-- UNHCR - Oversight of UNHCR
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'UNHCR' AND OrganizationId = @OIOSId)
BEGIN
    INSERT INTO Offices (Name, Code, Description, OrganizationId, CreatedDate)
    VALUES ('Oversight of UNHCR', 'UNHCR', 'Provides oversight services specifically for UNHCR operations and programs', @OIOSId, GETDATE());
    PRINT 'Added: Oversight of UNHCR (UNHCR)';
END

PRINT 'OIOS offices setup completed successfully!';