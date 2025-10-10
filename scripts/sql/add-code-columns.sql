-- Add Code column to Organizations table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Organizations]') AND name = 'Code')
BEGIN
    ALTER TABLE Organizations
    ADD Code NVARCHAR(10) NULL;
END
GO

-- Add Code column to Offices table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Offices]') AND name = 'Code')
BEGIN
    ALTER TABLE Offices
    ADD Code NVARCHAR(10) NULL;
END
GO

-- Add Code column to SubOffices table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SubOffices]') AND name = 'Code')
BEGIN
    ALTER TABLE SubOffices
    ADD Code NVARCHAR(10) NULL;
END
GO

PRINT 'Code columns added successfully to Organizations, Offices, and SubOffices tables';