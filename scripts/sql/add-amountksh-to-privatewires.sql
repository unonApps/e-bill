-- Add AmountKSH column to PrivateWires table
-- This script adds the AmountKSH column for KES to USD conversion

-- Add AmountKSH column to PrivateWires table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'AmountKSH')
BEGIN
    ALTER TABLE [dbo].[PrivateWires] ADD [AmountKSH] DECIMAL(18,4) NULL;
    PRINT 'AmountKSH column added to PrivateWires table';
END
ELSE
BEGIN
    PRINT 'AmountKSH column already exists in PrivateWires table';
END
GO

-- Optional: Calculate AmountKSH from AmountUSD using exchange rates
-- This updates existing records where AmountUSD exists
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'AmountKSH')
BEGIN
    UPDATE pw
    SET pw.AmountKSH = pw.AmountUSD * er.Rate
    FROM [dbo].[PrivateWires] pw
    INNER JOIN [dbo].[ExchangeRates] er
        ON pw.CallMonth = er.Month
        AND pw.CallYear = er.Year
    WHERE pw.AmountUSD IS NOT NULL
        AND pw.AmountKSH IS NULL;

    DECLARE @updatedCount INT = @@ROWCOUNT;
    PRINT CONCAT('Updated ', @updatedCount, ' PrivateWire records with calculated AmountKSH values');
END
GO

PRINT 'Migration completed successfully';
GO
