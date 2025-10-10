-- =====================================================================================
-- COMPREHENSIVE AZURE DATABASE SCHEMA FIX
-- This script adds ALL missing columns from recent migrations to Azure database
-- Run this in Azure Portal Query Editor
-- =====================================================================================

PRINT '========================================';
PRINT 'Starting Azure Database Schema Sync';
PRINT '========================================';
PRINT '';

-- =====================================================================================
-- Migration: 20251002123541_AddVerificationPeriodToCallRecords
-- =====================================================================================
PRINT 'Checking migration: AddVerificationPeriodToCallRecords';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallRecords]') AND name = 'verification_period')
BEGIN
    ALTER TABLE [dbo].[CallRecords]
    ADD [verification_period] datetime2 NULL;
    PRINT '✓ Added verification_period column to CallRecords';
END
ELSE
    PRINT '- verification_period column already exists in CallRecords';
PRINT '';

-- =====================================================================================
-- Migration: 20251003180350_AddPhoneStatusToUserPhone
-- =====================================================================================
PRINT 'Checking migration: AddPhoneStatusToUserPhone';

-- Add Status to UserPhones
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UserPhones]') AND name = 'Status')
BEGIN
    ALTER TABLE [dbo].[UserPhones]
    ADD [Status] int NOT NULL DEFAULT 0;
    PRINT '✓ Added Status column to UserPhones';
END
ELSE
    PRINT '- Status column already exists in UserPhones';

-- Add columns to CallLogVerifications
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CallLogVerifications')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'OverageAmount')
    BEGIN
        ALTER TABLE [dbo].[CallLogVerifications]
        ADD [OverageAmount] decimal(18,4) NOT NULL DEFAULT 0;
        PRINT '✓ Added OverageAmount column to CallLogVerifications';
    END
    ELSE
        PRINT '- OverageAmount column already exists in CallLogVerifications';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'OverageJustified')
    BEGIN
        ALTER TABLE [dbo].[CallLogVerifications]
        ADD [OverageJustified] bit NOT NULL DEFAULT 0;
        PRINT '✓ Added OverageJustified column to CallLogVerifications';
    END
    ELSE
        PRINT '- OverageJustified column already exists in CallLogVerifications';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'PaymentAssignmentId')
    BEGIN
        ALTER TABLE [dbo].[CallLogVerifications]
        ADD [PaymentAssignmentId] int NULL;
        PRINT '✓ Added PaymentAssignmentId column to CallLogVerifications';
    END
    ELSE
        PRINT '- PaymentAssignmentId column already exists in CallLogVerifications';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'SupervisorApprovedBy')
    BEGIN
        ALTER TABLE [dbo].[CallLogVerifications]
        ADD [SupervisorApprovedBy] nvarchar(50) NULL;
        PRINT '✓ Added SupervisorApprovedBy column to CallLogVerifications';
    END
    ELSE
        PRINT '- SupervisorApprovedBy column already exists in CallLogVerifications';

    -- Rename columns in CallLogVerifications
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'SupervisorAction')
    BEGIN
        EXEC sp_rename 'CallLogVerifications.SupervisorAction', 'SupervisorApprovalStatus', 'COLUMN';
        PRINT '✓ Renamed SupervisorAction to SupervisorApprovalStatus in CallLogVerifications';
    END
    ELSE IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'SupervisorApprovalStatus')
    BEGIN
        ALTER TABLE [dbo].[CallLogVerifications]
        ADD [SupervisorApprovalStatus] nvarchar(20) NULL;
        PRINT '✓ Added SupervisorApprovalStatus column to CallLogVerifications';
    END
    ELSE
        PRINT '- SupervisorApprovalStatus column already exists in CallLogVerifications';

    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'SupervisorActionDate')
    BEGIN
        EXEC sp_rename 'CallLogVerifications.SupervisorActionDate', 'SupervisorApprovedDate', 'COLUMN';
        PRINT '✓ Renamed SupervisorActionDate to SupervisorApprovedDate in CallLogVerifications';
    END
    ELSE IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'SupervisorApprovedDate')
    BEGIN
        ALTER TABLE [dbo].[CallLogVerifications]
        ADD [SupervisorApprovedDate] datetime2 NULL;
        PRINT '✓ Added SupervisorApprovedDate column to CallLogVerifications';
    END
    ELSE
        PRINT '- SupervisorApprovedDate column already exists in CallLogVerifications';
END
ELSE
    PRINT '- CallLogVerifications table does not exist (will be created by migration)';
PRINT '';

-- =====================================================================================
-- Migration: 20251003192422_AddEbillUserAuthentication
-- =====================================================================================
PRINT 'Checking migration: AddEbillUserAuthentication';

-- Add missing columns to EbillUsers table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'ApplicationUserId')
BEGIN
    ALTER TABLE [dbo].[EbillUsers]
    ADD [ApplicationUserId] nvarchar(450) NULL;
    PRINT 'Added ApplicationUserId column to EbillUsers';
END
ELSE
    PRINT 'ApplicationUserId column already exists in EbillUsers';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'HasLoginAccount')
BEGIN
    ALTER TABLE [dbo].[EbillUsers]
    ADD [HasLoginAccount] bit NOT NULL DEFAULT 0;
    PRINT 'Added HasLoginAccount column to EbillUsers';
END
ELSE
    PRINT 'HasLoginAccount column already exists in EbillUsers';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'LoginEnabled')
BEGIN
    ALTER TABLE [dbo].[EbillUsers]
    ADD [LoginEnabled] bit NOT NULL DEFAULT 0;
    PRINT 'Added LoginEnabled column to EbillUsers';
END
ELSE
    PRINT 'LoginEnabled column already exists in EbillUsers';

-- Fix foreign key and index on AspNetUsers table
-- First, drop the old index if it exists (non-unique version)
IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'IX_AspNetUsers_EbillUserId')
BEGIN
    -- Check if it's the old non-unique index
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'IX_AspNetUsers_EbillUserId' AND is_unique = 1)
    BEGIN
        DROP INDEX [IX_AspNetUsers_EbillUserId] ON [AspNetUsers];
        PRINT 'Dropped old non-unique index IX_AspNetUsers_EbillUserId';
    END
    ELSE
        PRINT 'Index IX_AspNetUsers_EbillUserId is already unique';
END

-- Recreate as unique index with filter
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'IX_AspNetUsers_EbillUserId' AND is_unique = 1)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_AspNetUsers_EbillUserId]
    ON [AspNetUsers] ([EbillUserId])
    WHERE [EbillUserId] IS NOT NULL;
    PRINT 'Created unique filtered index IX_AspNetUsers_EbillUserId';
END
ELSE
    PRINT 'Unique index IX_AspNetUsers_EbillUserId already exists';

-- Drop and recreate the foreign key with correct cascade behavior
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetUsers_EbillUsers_EbillUserId]'))
BEGIN
    ALTER TABLE [AspNetUsers] DROP CONSTRAINT [FK_AspNetUsers_EbillUsers_EbillUserId];
    PRINT 'Dropped old foreign key FK_AspNetUsers_EbillUsers_EbillUserId';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetUsers_EbillUsers_EbillUserId]'))
BEGIN
    ALTER TABLE [AspNetUsers]
    ADD CONSTRAINT [FK_AspNetUsers_EbillUsers_EbillUserId]
    FOREIGN KEY ([EbillUserId])
    REFERENCES [EbillUsers] ([Id])
    ON DELETE SET NULL;
    PRINT 'Created foreign key FK_AspNetUsers_EbillUsers_EbillUserId with SET NULL';
END
ELSE
    PRINT 'Foreign key FK_AspNetUsers_EbillUsers_EbillUserId already exists';

PRINT '';

-- =====================================================================================
-- Migration: 20251006074150_ReplaceMonthlyCallCostLimitWithHandsetAllowanceAmount
-- =====================================================================================
PRINT 'Checking migration: ReplaceMonthlyCallCostLimitWithHandsetAllowanceAmount';

-- Rename column in ClassOfServices table
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ClassOfServices]') AND name = 'MonthlyCallCostLimit')
BEGIN
    EXEC sp_rename 'ClassOfServices.MonthlyCallCostLimit', 'HandsetAllowanceAmount', 'COLUMN';
    PRINT '✓ Renamed MonthlyCallCostLimit to HandsetAllowanceAmount in ClassOfServices';
END
ELSE IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ClassOfServices]') AND name = 'HandsetAllowanceAmount')
BEGIN
    -- If neither exists, add the new column
    ALTER TABLE [dbo].[ClassOfServices]
    ADD [HandsetAllowanceAmount] decimal(18,2) NULL;
    PRINT '✓ Added HandsetAllowanceAmount column to ClassOfServices';
END
ELSE
    PRINT '- HandsetAllowanceAmount column already exists in ClassOfServices';
PRINT '';

-- =====================================================================================
-- Migration: 20251006175733_AddAssignmentStatusToCallRecord
-- =====================================================================================
PRINT 'Checking migration: AddAssignmentStatusToCallRecord';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallRecords]') AND name = 'assignment_status')
BEGIN
    ALTER TABLE [dbo].[CallRecords]
    ADD [assignment_status] nvarchar(20) NOT NULL DEFAULT 'None';
    PRINT '✓ Added assignment_status column to CallRecords (with default value None)';
END
ELSE
    PRINT '- assignment_status column already exists in CallRecords';
PRINT '';

-- =====================================================================================
-- Migration: 20251008095859_AddNotificationsTable
-- =====================================================================================
PRINT 'Checking migration: AddNotificationsTable';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ReadDate] datetime2 NULL,
        [Link] nvarchar(500) NULL,
        [RelatedEntityType] nvarchar(100) NULL,
        [RelatedEntityId] nvarchar(100) NULL,
        [Icon] nvarchar(50) NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_AspNetUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_Notifications_UserId]
        ON [dbo].[Notifications] ([UserId]);

    PRINT '✓ Created Notifications table with indexes and foreign keys';
END
ELSE
    PRINT '- Notifications table already exists';
PRINT '';

-- =====================================================================================
-- FINAL VERIFICATION
-- =====================================================================================
PRINT '========================================';
PRINT 'Final Verification';
PRINT '========================================';
PRINT '';

-- Check EbillUsers columns
PRINT 'EbillUsers Authentication Columns:';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'EbillUsers'
AND COLUMN_NAME IN ('ApplicationUserId', 'HasLoginAccount', 'LoginEnabled')
ORDER BY COLUMN_NAME;
PRINT '';

-- Check CallRecords columns
PRINT 'CallRecords New Columns:';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'CallRecords'
AND COLUMN_NAME IN ('verification_period', 'assignment_status')
ORDER BY COLUMN_NAME;
PRINT '';

-- Check UserPhones columns
PRINT 'UserPhones Status Column:';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'UserPhones'
AND COLUMN_NAME = 'Status';
PRINT '';

-- Check ClassOfServices column
PRINT 'ClassOfServices HandsetAllowanceAmount Column:';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ClassOfServices'
AND COLUMN_NAME = 'HandsetAllowanceAmount';
PRINT '';

-- Check Notifications table
PRINT 'Notifications Table:';
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
    PRINT '✓ Notifications table exists'
ELSE
    PRINT '✗ Notifications table does NOT exist';
PRINT '';

PRINT '========================================';
PRINT 'Schema Sync Complete!';
PRINT '========================================';
