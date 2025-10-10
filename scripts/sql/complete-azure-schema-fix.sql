-- =====================================================================================
-- COMPLETE AZURE DATABASE SCHEMA FIX
-- This script adds ALL missing columns and constraints to Azure database
-- Run this AFTER add-missing-call-log-tables.sql
-- =====================================================================================

PRINT '========================================';
PRINT 'Complete Azure Schema Fix';
PRINT '========================================';
PRINT '';

-- =====================================================================================
-- CallRecords Table - Add ALL missing columns
-- =====================================================================================
PRINT 'Fixing CallRecords table...';

-- UserPhoneId (from migration 20251002173558)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallRecords]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[CallRecords]
    ADD [UserPhoneId] int NULL;
    PRINT '✓ Added UserPhoneId column';
END
ELSE
    PRINT '- UserPhoneId already exists';

-- verification_period (from migration 20251002123541)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallRecords]') AND name = 'verification_period')
BEGIN
    ALTER TABLE [dbo].[CallRecords]
    ADD [verification_period] datetime2 NULL;
    PRINT '✓ Added verification_period column';
END
ELSE
    PRINT '- verification_period already exists';

-- verification_type
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallRecords]') AND name = 'verification_type')
BEGIN
    ALTER TABLE [dbo].[CallRecords]
    ADD [verification_type] nvarchar(20) NULL;
    PRINT '✓ Added verification_type column';
END
ELSE
    PRINT '- verification_type already exists';

-- payment_assignment_id
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallRecords]') AND name = 'payment_assignment_id')
BEGIN
    ALTER TABLE [dbo].[CallRecords]
    ADD [payment_assignment_id] int NULL;
    PRINT '✓ Added payment_assignment_id column';
END
ELSE
    PRINT '- payment_assignment_id already exists';

-- assignment_status (from migration 20251006175733)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallRecords]') AND name = 'assignment_status')
BEGIN
    ALTER TABLE [dbo].[CallRecords]
    ADD [assignment_status] nvarchar(20) NOT NULL DEFAULT 'None';
    PRINT '✓ Added assignment_status column';
END
ELSE
    PRINT '- assignment_status already exists';

-- overage_justified
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallRecords]') AND name = 'overage_justified')
BEGIN
    ALTER TABLE [dbo].[CallRecords]
    ADD [overage_justified] bit NOT NULL DEFAULT 0;
    PRINT '✓ Added overage_justified column';
END
ELSE
    PRINT '- overage_justified already exists';

-- supervisor_approval_status
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallRecords]') AND name = 'supervisor_approval_status')
BEGIN
    ALTER TABLE [dbo].[CallRecords]
    ADD [supervisor_approval_status] nvarchar(20) NULL;
    PRINT '✓ Added supervisor_approval_status column';
END
ELSE
    PRINT '- supervisor_approval_status already exists';

-- supervisor_approved_by
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallRecords]') AND name = 'supervisor_approved_by')
BEGIN
    ALTER TABLE [dbo].[CallRecords]
    ADD [supervisor_approved_by] nvarchar(50) NULL;
    PRINT '✓ Added supervisor_approved_by column';
END
ELSE
    PRINT '- supervisor_approved_by already exists';

-- supervisor_approved_date
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallRecords]') AND name = 'supervisor_approved_date')
BEGIN
    ALTER TABLE [dbo].[CallRecords]
    ADD [supervisor_approved_date] datetime2 NULL;
    PRINT '✓ Added supervisor_approved_date column';
END
ELSE
    PRINT '- supervisor_approved_date already exists';

PRINT '';

-- =====================================================================================
-- CallRecords Foreign Keys and Indexes
-- =====================================================================================
PRINT 'Adding CallRecords foreign keys and indexes...';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CallRecords]') AND name = 'IX_CallRecords_UserPhoneId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CallRecords_UserPhoneId]
        ON [dbo].[CallRecords] ([UserPhoneId]);
    PRINT '✓ Created index IX_CallRecords_UserPhoneId';
END
ELSE
    PRINT '- Index IX_CallRecords_UserPhoneId already exists';

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_CallRecords_UserPhones_UserPhoneId]'))
BEGIN
    ALTER TABLE [dbo].[CallRecords]
    ADD CONSTRAINT [FK_CallRecords_UserPhones_UserPhoneId]
    FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id]);
    PRINT '✓ Created foreign key FK_CallRecords_UserPhones_UserPhoneId';
END
ELSE
    PRINT '- Foreign key FK_CallRecords_UserPhones_UserPhoneId already exists';

PRINT '';

-- =====================================================================================
-- ClassOfServices Table - Add missing columns
-- =====================================================================================
PRINT 'Fixing ClassOfServices table...';

-- AirtimeAllowanceAmount
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ClassOfServices]') AND name = 'AirtimeAllowanceAmount')
BEGIN
    ALTER TABLE [dbo].[ClassOfServices]
    ADD [AirtimeAllowanceAmount] decimal(18,4) NULL;
    PRINT '✓ Added AirtimeAllowanceAmount column';
END
ELSE
    PRINT '- AirtimeAllowanceAmount already exists';

-- DataAllowanceAmount
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ClassOfServices]') AND name = 'DataAllowanceAmount')
BEGIN
    ALTER TABLE [dbo].[ClassOfServices]
    ADD [DataAllowanceAmount] decimal(18,4) NULL;
    PRINT '✓ Added DataAllowanceAmount column';
END
ELSE
    PRINT '- DataAllowanceAmount already exists';

-- BillingPeriod
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ClassOfServices]') AND name = 'BillingPeriod')
BEGIN
    ALTER TABLE [dbo].[ClassOfServices]
    ADD [BillingPeriod] nvarchar(20) NOT NULL DEFAULT 'Monthly';
    PRINT '✓ Added BillingPeriod column';
END
ELSE
    PRINT '- BillingPeriod already exists';

-- HandsetAllowanceAmount (rename from MonthlyCallCostLimit if needed)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ClassOfServices]') AND name = 'MonthlyCallCostLimit')
BEGIN
    EXEC sp_rename 'ClassOfServices.MonthlyCallCostLimit', 'HandsetAllowanceAmount', 'COLUMN';
    PRINT '✓ Renamed MonthlyCallCostLimit to HandsetAllowanceAmount';
END
ELSE IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ClassOfServices]') AND name = 'HandsetAllowanceAmount')
BEGIN
    ALTER TABLE [dbo].[ClassOfServices]
    ADD [HandsetAllowanceAmount] decimal(18,4) NULL;
    PRINT '✓ Added HandsetAllowanceAmount column';
END
ELSE
    PRINT '- HandsetAllowanceAmount already exists';

PRINT '';

-- =====================================================================================
-- UserPhones Table - Add Status column
-- =====================================================================================
PRINT 'Fixing UserPhones table...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UserPhones]') AND name = 'Status')
BEGIN
    ALTER TABLE [dbo].[UserPhones]
    ADD [Status] int NOT NULL DEFAULT 0;
    PRINT '✓ Added Status column to UserPhones';
END
ELSE
    PRINT '- Status already exists in UserPhones';

PRINT '';

-- =====================================================================================
-- EbillUsers Table - Add authentication columns
-- =====================================================================================
PRINT 'Fixing EbillUsers table...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'ApplicationUserId')
BEGIN
    ALTER TABLE [dbo].[EbillUsers]
    ADD [ApplicationUserId] nvarchar(450) NULL;
    PRINT '✓ Added ApplicationUserId column';
END
ELSE
    PRINT '- ApplicationUserId already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'HasLoginAccount')
BEGIN
    ALTER TABLE [dbo].[EbillUsers]
    ADD [HasLoginAccount] bit NOT NULL DEFAULT 0;
    PRINT '✓ Added HasLoginAccount column';
END
ELSE
    PRINT '- HasLoginAccount already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'LoginEnabled')
BEGIN
    ALTER TABLE [dbo].[EbillUsers]
    ADD [LoginEnabled] bit NOT NULL DEFAULT 0;
    PRINT '✓ Added LoginEnabled column';
END
ELSE
    PRINT '- LoginEnabled already exists';

PRINT '';

-- =====================================================================================
-- EbillUsers - Fix AspNetUsers relationship
-- =====================================================================================
PRINT 'Fixing AspNetUsers foreign key...';

-- Drop old index if not unique
IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'IX_AspNetUsers_EbillUserId' AND is_unique = 0)
BEGIN
    DROP INDEX [IX_AspNetUsers_EbillUserId] ON [AspNetUsers];
    PRINT '✓ Dropped old non-unique index';
END

-- Create unique index if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'IX_AspNetUsers_EbillUserId' AND is_unique = 1)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_AspNetUsers_EbillUserId]
        ON [AspNetUsers] ([EbillUserId])
        WHERE [EbillUserId] IS NOT NULL;
    PRINT '✓ Created unique filtered index';
END
ELSE
    PRINT '- Unique index already exists';

-- Update foreign key
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetUsers_EbillUsers_EbillUserId]'))
BEGIN
    ALTER TABLE [AspNetUsers] DROP CONSTRAINT [FK_AspNetUsers_EbillUsers_EbillUserId];
    PRINT '✓ Dropped old foreign key';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetUsers_EbillUsers_EbillUserId]'))
BEGIN
    ALTER TABLE [AspNetUsers]
    ADD CONSTRAINT [FK_AspNetUsers_EbillUsers_EbillUserId]
    FOREIGN KEY ([EbillUserId])
    REFERENCES [EbillUsers] ([Id])
    ON DELETE SET NULL;
    PRINT '✓ Created new foreign key with SET NULL';
END

PRINT '';

-- =====================================================================================
-- CallLogVerifications - Apply pending changes from migration 20251003180350
-- =====================================================================================
PRINT 'Fixing CallLogVerifications table...';

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CallLogVerifications')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'OverageAmount')
    BEGIN
        ALTER TABLE [dbo].[CallLogVerifications]
        ADD [OverageAmount] decimal(18,4) NOT NULL DEFAULT 0;
        PRINT '✓ Added OverageAmount column';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'OverageJustified')
    BEGIN
        ALTER TABLE [dbo].[CallLogVerifications]
        ADD [OverageJustified] bit NOT NULL DEFAULT 0;
        PRINT '✓ Added OverageJustified column';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'PaymentAssignmentId')
    BEGIN
        ALTER TABLE [dbo].[CallLogVerifications]
        ADD [PaymentAssignmentId] int NULL;
        PRINT '✓ Added PaymentAssignmentId column';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'SupervisorApprovedBy')
    BEGIN
        ALTER TABLE [dbo].[CallLogVerifications]
        ADD [SupervisorApprovedBy] nvarchar(50) NULL;
        PRINT '✓ Added SupervisorApprovedBy column';
    END

    -- Rename columns
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'SupervisorAction')
    BEGIN
        EXEC sp_rename 'CallLogVerifications.SupervisorAction', 'SupervisorApprovalStatus', 'COLUMN';
        PRINT '✓ Renamed SupervisorAction to SupervisorApprovalStatus';
    END

    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'SupervisorActionDate')
    BEGIN
        EXEC sp_rename 'CallLogVerifications.SupervisorActionDate', 'SupervisorApprovedDate', 'COLUMN';
        PRINT '✓ Renamed SupervisorActionDate to SupervisorApprovedDate';
    END
END
ELSE
    PRINT '- CallLogVerifications table does not exist';

PRINT '';

-- =====================================================================================
-- PrivateWires Table - Add AmountKSH and UserPhoneId
-- =====================================================================================
PRINT 'Fixing PrivateWires table...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'AmountKSH')
BEGIN
    ALTER TABLE [dbo].[PrivateWires]
    ADD [AmountKSH] decimal(18,4) NULL;
    PRINT '✓ Added AmountKSH column to PrivateWires';
END
ELSE
    PRINT '- AmountKSH already exists in PrivateWires';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[PrivateWires]
    ADD [UserPhoneId] int NULL;
    PRINT '✓ Added UserPhoneId column to PrivateWires';
END
ELSE
    PRINT '- UserPhoneId already exists in PrivateWires';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'ProcessingStatus')
BEGIN
    ALTER TABLE [dbo].[PrivateWires]
    ADD [ProcessingStatus] int NOT NULL DEFAULT 0;
    PRINT '✓ Added ProcessingStatus column to PrivateWires';
END
ELSE
    PRINT '- ProcessingStatus already exists in PrivateWires';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'ProcessedDate')
BEGIN
    ALTER TABLE [dbo].[PrivateWires]
    ADD [ProcessedDate] datetime2 NULL;
    PRINT '✓ Added ProcessedDate column to PrivateWires';
END
ELSE
    PRINT '- ProcessedDate already exists in PrivateWires';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'StagingBatchId')
BEGIN
    ALTER TABLE [dbo].[PrivateWires]
    ADD [StagingBatchId] uniqueidentifier NULL;
    PRINT '✓ Added StagingBatchId column to PrivateWires';
END
ELSE
    PRINT '- StagingBatchId already exists in PrivateWires';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'BillingPeriod')
BEGIN
    ALTER TABLE [dbo].[PrivateWires]
    ADD [BillingPeriod] nvarchar(20) NULL;
    PRINT '✓ Added BillingPeriod column to PrivateWires';
END
ELSE
    PRINT '- BillingPeriod already exists in PrivateWires';

PRINT '';

-- =====================================================================================
-- Airtel, Safaricom, PSTN Tables - Add common missing columns
-- =====================================================================================
PRINT 'Fixing Airtel, Safaricom, and PSTN tables...';

-- Airtel
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Airtel')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Airtel]') AND name = 'AmountUSD')
    BEGIN
        ALTER TABLE [dbo].[Airtel]
        ADD [AmountUSD] decimal(18,4) NULL;
        PRINT '✓ Added AmountUSD column to Airtel';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Airtel]') AND name = 'UserPhoneId')
    BEGIN
        ALTER TABLE [dbo].[Airtel]
        ADD [UserPhoneId] int NULL;
        PRINT '✓ Added UserPhoneId column to Airtel';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Airtel]') AND name = 'ProcessingStatus')
    BEGIN
        ALTER TABLE [dbo].[Airtel]
        ADD [ProcessingStatus] int NOT NULL DEFAULT 0;
        PRINT '✓ Added ProcessingStatus column to Airtel';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Airtel]') AND name = 'StagingBatchId')
    BEGIN
        ALTER TABLE [dbo].[Airtel]
        ADD [StagingBatchId] uniqueidentifier NULL;
        PRINT '✓ Added StagingBatchId column to Airtel';
    END
END
ELSE
    PRINT '- Airtel table does not exist';

-- Safaricom
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Safaricom')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND name = 'AmountUSD')
    BEGIN
        ALTER TABLE [dbo].[Safaricom]
        ADD [AmountUSD] decimal(18,4) NULL;
        PRINT '✓ Added AmountUSD column to Safaricom';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND name = 'UserPhoneId')
    BEGIN
        ALTER TABLE [dbo].[Safaricom]
        ADD [UserPhoneId] int NULL;
        PRINT '✓ Added UserPhoneId column to Safaricom';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND name = 'ProcessingStatus')
    BEGIN
        ALTER TABLE [dbo].[Safaricom]
        ADD [ProcessingStatus] int NOT NULL DEFAULT 0;
        PRINT '✓ Added ProcessingStatus column to Safaricom';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND name = 'StagingBatchId')
    BEGIN
        ALTER TABLE [dbo].[Safaricom]
        ADD [StagingBatchId] uniqueidentifier NULL;
        PRINT '✓ Added StagingBatchId column to Safaricom';
    END
END
ELSE
    PRINT '- Safaricom table does not exist';

-- PSTN
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PSTNs')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND name = 'UserPhoneId')
    BEGIN
        ALTER TABLE [dbo].[PSTNs]
        ADD [UserPhoneId] int NULL;
        PRINT '✓ Added UserPhoneId column to PSTNs';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND name = 'ProcessingStatus')
    BEGIN
        ALTER TABLE [dbo].[PSTNs]
        ADD [ProcessingStatus] int NOT NULL DEFAULT 0;
        PRINT '✓ Added ProcessingStatus column to PSTNs';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND name = 'StagingBatchId')
    BEGIN
        ALTER TABLE [dbo].[PSTNs]
        ADD [StagingBatchId] uniqueidentifier NULL;
        PRINT '✓ Added StagingBatchId column to PSTNs';
    END
END
ELSE
    PRINT '- PSTNs table does not exist';

PRINT '';

-- =====================================================================================
-- Notifications Table
-- =====================================================================================
PRINT 'Fixing Notifications table...';

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

    PRINT '✓ Created Notifications table';
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

PRINT 'CallRecords verification columns:';
SELECT COUNT(*) as ColumnCount
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'CallRecords'
AND COLUMN_NAME IN ('UserPhoneId', 'verification_period', 'verification_type',
                     'payment_assignment_id', 'assignment_status', 'overage_justified',
                     'supervisor_approval_status', 'supervisor_approved_by', 'supervisor_approved_date');
PRINT '';

PRINT 'ClassOfServices allowance columns:';
SELECT COUNT(*) as ColumnCount
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ClassOfServices'
AND COLUMN_NAME IN ('AirtimeAllowanceAmount', 'DataAllowanceAmount', 'HandsetAllowanceAmount', 'BillingPeriod');
PRINT '';

PRINT 'EbillUsers authentication columns:';
SELECT COUNT(*) as ColumnCount
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'EbillUsers'
AND COLUMN_NAME IN ('ApplicationUserId', 'HasLoginAccount', 'LoginEnabled');
PRINT '';

PRINT '========================================';
PRINT 'Complete Schema Fix Finished!';
PRINT 'Next: Restart Azure App Service';
PRINT '========================================';
