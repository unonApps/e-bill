-- Add UN Organizations to the Organizations table
-- Check if organizations exist before inserting to avoid duplicates

-- OIOS - Office of Internal Oversight Services
IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'OIOS' OR Name = 'Office of Internal Oversight Services')
BEGIN
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('Office of Internal Oversight Services', 'OIOS', 'The Office of Internal Oversight Services is an independent office in the United Nations Secretariat whose mandate is to provide internal audit, investigation, inspection and evaluation services.', GETDATE());
END
GO

-- UN-Habitat - United Nations Human Settlements Programme
IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'UN-HAB' OR Name = 'United Nations Human Settlements Programme')
BEGIN
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('United Nations Human Settlements Programme', 'UN-HAB', 'UN-Habitat is the United Nations programme for human settlements and sustainable urban development.', GETDATE());
END
GO

-- UNEP - United Nations Environment Programme
IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'UNEP' OR Name = 'United Nations Environment Programme')
BEGIN
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('United Nations Environment Programme', 'UNEP', 'The United Nations Environment Programme is responsible for coordinating the UN''s environmental activities and assisting developing countries in implementing environmentally sound policies.', GETDATE());
END
GO

-- UNON - United Nations Office at Nairobi
IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'UNON' OR Name = 'United Nations Office at Nairobi')
BEGIN
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('United Nations Office at Nairobi', 'UNON', 'The United Nations Office at Nairobi is one of four major United Nations office sites where numerous different UN agencies have a joint presence.', GETDATE());
END
GO

-- UNIC - United Nations Information Centre
IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'UNIC' OR Name = 'United Nations Information Centre')
BEGIN
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('United Nations Information Centre', 'UNIC', 'The United Nations Information Centre in Nairobi serves as a hub for UN communications and public information activities in the region.', GETDATE());
END
GO

PRINT 'UN Organizations added successfully';