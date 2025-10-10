-- Create AuditLogs table for tracking system actions
USE TABDB;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' AND xtype='U')
BEGIN
    CREATE TABLE AuditLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EntityType NVARCHAR(100) NOT NULL,
        EntityId NVARCHAR(100) NULL,
        [Action] NVARCHAR(50) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        OldValues NVARCHAR(2000) NULL,
        NewValues NVARCHAR(2000) NULL,
        PerformedBy NVARCHAR(100) NOT NULL,
        PerformedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IPAddress NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        Module NVARCHAR(50) NULL,
        IsSuccess BIT NOT NULL DEFAULT 1,
        ErrorMessage NVARCHAR(1000) NULL,
        AdditionalData NVARCHAR(4000) NULL
    );

    -- Create indexes for common queries
    CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs(EntityType, EntityId);
    CREATE INDEX IX_AuditLogs_PerformedBy ON AuditLogs(PerformedBy);
    CREATE INDEX IX_AuditLogs_PerformedDate ON AuditLogs(PerformedDate DESC);
    CREATE INDEX IX_AuditLogs_Module ON AuditLogs(Module);

    PRINT 'AuditLogs table created successfully';
END
ELSE
BEGIN
    PRINT 'AuditLogs table already exists';
END