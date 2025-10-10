-- Seed ODG Organization
IF NOT EXISTS (SELECT 1 FROM [TABDB].[dbo].[Organizations]
               WHERE [Code] = 'ODG'
               OR [Name] = 'Office of the Director-General')
    INSERT INTO [TABDB].[dbo].[Organizations] ([Name], [Description], [CreatedDate], [Code])
    VALUES ('Office of the Director-General', 'Executive office providing leadership and strategic direction for UN operations.', GETDATE(), 'ODG');

-- Verify the insert
SELECT Name, Code, Description FROM Organizations WHERE Code = 'ODG';