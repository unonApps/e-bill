-- Script to rename PSTN table columns to more meaningful names
-- Run this in SQL Server Management Studio

USE [TABDB];
GO

-- Check if the table exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND type in (N'U'))
BEGIN
    PRINT 'Renaming PSTN columns to improve maintainability...';

    -- Rename columns to more meaningful names
    -- Only rename if the old column exists and new column doesn't exist

    -- Ext → Extension
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Ext' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Extension' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Ext', 'Extension', 'COLUMN';
        PRINT 'Renamed: Ext → Extension';
    END

    -- Dialed → DialedNumber
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Dialed' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'DialedNumber' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Dialed', 'DialedNumber', 'COLUMN';
        PRINT 'Renamed: Dialed → DialedNumber';
    END

    -- Time → CallTime
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Time' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'CallTime' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Time', 'CallTime', 'COLUMN';
        PRINT 'Renamed: Time → CallTime';
    END

    -- Dest → Destination
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Dest' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Destination' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Dest', 'Destination', 'COLUMN';
        PRINT 'Renamed: Dest → Destination';
    END

    -- Dl → DestinationLine
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Dl' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'DestinationLine' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Dl', 'DestinationLine', 'COLUMN';
        PRINT 'Renamed: Dl → DestinationLine';
    END

    -- Durx → DurationExtended
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Durx' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'DurationExtended' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Durx', 'DurationExtended', 'COLUMN';
        PRINT 'Renamed: Durx → DurationExtended';
    END

    -- Org → Organization
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Org' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Organization' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Org', 'Organization', 'COLUMN';
        PRINT 'Renamed: Org → Organization';
    END

    -- Org_Unit → OrganizationalUnit
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Org_Unit' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'OrganizationalUnit' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Org_Unit', 'OrganizationalUnit', 'COLUMN';
        PRINT 'Renamed: Org_Unit → OrganizationalUnit';
    END

    -- Name → CallerName
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Name' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'CallerName' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Name', 'CallerName', 'COLUMN';
        PRINT 'Renamed: Name → CallerName';
    END

    -- Date → CallDate
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Date' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'CallDate' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Date', 'CallDate', 'COLUMN';
        PRINT 'Renamed: Date → CallDate';
    END

    -- Dur → Duration
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Dur' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Duration' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Dur', 'Duration', 'COLUMN';
        PRINT 'Renamed: Dur → Duration';
    END

    -- Kshs → AmountKSH
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Kshs' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AmountKSH' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Kshs', 'AmountKSH', 'COLUMN';
        PRINT 'Renamed: Kshs → AmountKSH';
    END

    -- Inde_ → IndexNumber
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Inde_' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'IndexNumber' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Inde_', 'IndexNumber', 'COLUMN';
        PRINT 'Renamed: Inde_ → IndexNumber';
    END

    -- Oca → OCACode
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Oca' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'OCACode' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Oca', 'OCACode', 'COLUMN';
        PRINT 'Renamed: Oca → OCACode';
    END

    -- Car → Carrier
    IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'Car' AND Object_ID = Object_ID(N'PSTNs'))
        AND NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Carrier' AND Object_ID = Object_ID(N'PSTNs'))
    BEGIN
        EXEC sp_rename 'PSTNs.Car', 'Carrier', 'COLUMN';
        PRINT 'Renamed: Car → Carrier';
    END

    PRINT 'Column renaming completed successfully!';
    PRINT '';
    PRINT 'New column structure:';

    -- Display the new column structure
    SELECT
        COLUMN_NAME as 'Column Name',
        DATA_TYPE as 'Data Type',
        CHARACTER_MAXIMUM_LENGTH as 'Max Length',
        IS_NULLABLE as 'Nullable'
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'PSTNs'
    ORDER BY ORDINAL_POSITION;
END
ELSE
BEGIN
    PRINT 'PSTNs table does not exist. Please create the table first.';
END
GO