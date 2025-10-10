-- Add Exchange Rates table and USD amount columns
-- This script adds only the new features without conflicting with existing schema

BEGIN TRANSACTION;

-- 1. Create ExchangeRates table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExchangeRates')
BEGIN
    CREATE TABLE [dbo].[ExchangeRates] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Month] INT NOT NULL,
        [Year] INT NOT NULL,
        [Rate] DECIMAL(18,4) NOT NULL,
        [CreatedBy] NVARCHAR(256) NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [UpdatedDate] DATETIME2 NULL,
        CONSTRAINT [PK_ExchangeRates] PRIMARY KEY ([Id]),
        CONSTRAINT [IX_ExchangeRates_Month_Year] UNIQUE ([Month], [Year])
    );
    PRINT 'ExchangeRates table created successfully';
END
ELSE
BEGIN
    PRINT 'ExchangeRates table already exists';
END

-- 2. Add AmountUSD column to Airtel table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Airtel]') AND name = 'AmountUSD')
BEGIN
    ALTER TABLE [dbo].[Airtel] ADD [AmountUSD] DECIMAL(18,4) NULL;
    PRINT 'AmountUSD column added to Airtel table';
END
ELSE
BEGIN
    PRINT 'AmountUSD column already exists in Airtel table';
END

-- 3. Add AmountUSD column to Safaricom table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND name = 'AmountUSD')
BEGIN
    ALTER TABLE [dbo].[Safaricom] ADD [AmountUSD] DECIMAL(18,4) NULL;
    PRINT 'AmountUSD column added to Safaricom table';
END
ELSE
BEGIN
    PRINT 'AmountUSD column already exists in Safaricom table';
END

-- 4. Add AmountUSD column to PSTNs table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND name = 'AmountUSD')
BEGIN
    ALTER TABLE [dbo].[PSTNs] ADD [AmountUSD] DECIMAL(18,4) NULL;
    PRINT 'AmountUSD column added to PSTNs table';
END
ELSE
BEGIN
    PRINT 'AmountUSD column already exists in PSTNs table';
END

-- 5. Add DateFormatPreferences column to ImportAudits if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ImportAudits]') AND name = 'DateFormatPreferences')
BEGIN
    ALTER TABLE [dbo].[ImportAudits] ADD [DateFormatPreferences] NVARCHAR(500) NULL;
    PRINT 'DateFormatPreferences column added to ImportAudits table';
END
ELSE
BEGIN
    PRINT 'DateFormatPreferences column already exists in ImportAudits table';
END

-- 6. Insert a sample exchange rate for current month (optional)
IF NOT EXISTS (SELECT * FROM [dbo].[ExchangeRates])
BEGIN
    DECLARE @currentMonth INT = MONTH(GETDATE());
    DECLARE @currentYear INT = YEAR(GETDATE());

    INSERT INTO [dbo].[ExchangeRates] ([Month], [Year], [Rate], [CreatedBy], [CreatedDate])
    VALUES (@currentMonth, @currentYear, 150.0000, 'System', GETDATE());

    PRINT 'Sample exchange rate inserted for current month';
END

COMMIT TRANSACTION;

PRINT '';
PRINT '=== Exchange Rates and USD Columns Setup Complete ===';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Navigate to /Admin/ExchangeRates to manage exchange rates';
PRINT '2. Add exchange rates for billing periods you want to track';
PRINT '3. When importing call logs, USD amounts will be calculated automatically';
