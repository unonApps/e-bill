-- Drop and recreate AuditLogs table with correct schema
DROP TABLE IF EXISTS [AuditLogs];
GO

CREATE TABLE [dbo].[AuditLogs] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [EntityType] nvarchar(100) NOT NULL,
    [EntityId] nvarchar(100) NULL,
    [Action] nvarchar(50) NOT NULL,
    [Description] nvarchar(500) NULL,
    [OldValues] nvarchar(2000) NULL,
    [NewValues] nvarchar(2000) NULL,
    [PerformedBy] nvarchar(100) NOT NULL,
    [PerformedDate] datetime2 NOT NULL CONSTRAINT [DF_AuditLogs_PerformedDate] DEFAULT (GETUTCDATE()),
    [IPAddress] nvarchar(50) NULL,
    [UserAgent] nvarchar(500) NULL,
    [Module] nvarchar(50) NULL,
    [IsSuccess] bit NOT NULL CONSTRAINT [DF_AuditLogs_IsSuccess] DEFAULT (1),
    [ErrorMessage] nvarchar(1000) NULL,
    [AdditionalData] nvarchar(4000) NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
);

CREATE INDEX [IX_AuditLogs_EntityType] ON [AuditLogs] ([EntityType]);
CREATE INDEX [IX_AuditLogs_EntityId] ON [AuditLogs] ([EntityId]);
CREATE INDEX [IX_AuditLogs_Action] ON [AuditLogs] ([Action]);
CREATE INDEX [IX_AuditLogs_PerformedBy] ON [AuditLogs] ([PerformedBy]);
CREATE INDEX [IX_AuditLogs_PerformedDate] ON [AuditLogs] ([PerformedDate]);
CREATE INDEX [IX_AuditLogs_Module] ON [AuditLogs] ([Module]);

PRINT 'AuditLogs table recreated successfully';
GO
