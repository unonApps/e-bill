-- Check existing organizations first
SELECT Name, Code FROM Organizations WHERE
    Name IN ('United Nations International Children''s Emergency Fund',
             'United Nations Information Centre Nairobi',
             'United Nations International Strategy for Disaster Reduction',
             'United Nations Entity for Gender Equality and the Empowerment of Women')
    OR Code IN ('UNICEF', 'UNICEN', 'UNISDR', 'UNWOMEN');

-- Seed additional Organizations (with duplicate checking by both Name and Code)
IF NOT EXISTS (SELECT 1 FROM [TABDB].[dbo].[Organizations]
               WHERE [Code] = 'UNICEF'
               OR [Name] = 'United Nations International Children''s Emergency Fund')
    INSERT INTO [TABDB].[dbo].[Organizations] ([Name], [Description], [CreatedDate], [Code])
    VALUES ('United Nations International Children''s Emergency Fund', 'UN agency providing humanitarian and developmental aid to children worldwide.', GETDATE(), 'UNICEF');

IF NOT EXISTS (SELECT 1 FROM [TABDB].[dbo].[Organizations]
               WHERE [Code] = 'UNICEN'
               OR [Name] = 'United Nations Information Centre Nairobi')
    INSERT INTO [TABDB].[dbo].[Organizations] ([Name], [Description], [CreatedDate], [Code])
    VALUES ('United Nations Information Centre Nairobi', 'UN Information Centre providing communication and outreach in Nairobi.', GETDATE(), 'UNICEN');

IF NOT EXISTS (SELECT 1 FROM [TABDB].[dbo].[Organizations]
               WHERE [Code] = 'UNISDR'
               OR [Name] = 'United Nations International Strategy for Disaster Reduction')
    INSERT INTO [TABDB].[dbo].[Organizations] ([Name], [Description], [CreatedDate], [Code])
    VALUES ('United Nations International Strategy for Disaster Reduction', 'UN body dedicated to coordinating disaster risk reduction worldwide.', GETDATE(), 'UNISDR');

IF NOT EXISTS (SELECT 1 FROM [TABDB].[dbo].[Organizations]
               WHERE [Code] = 'UNWOMEN'
               OR [Name] = 'United Nations Entity for Gender Equality and the Empowerment of Women')
    INSERT INTO [TABDB].[dbo].[Organizations] ([Name], [Description], [CreatedDate], [Code])
    VALUES ('United Nations Entity for Gender Equality and the Empowerment of Women', 'UN organization dedicated to gender equality and women''s empowerment.', GETDATE(), 'UNWOMEN');

-- Verify all organizations
SELECT Name, Code, Description FROM Organizations WHERE Code IN ('UNICEF', 'UNICEN', 'UNISDR', 'UNWOMEN');