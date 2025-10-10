-- Create ImportAudits table for tracking all import activities
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ImportAudits' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[ImportAudits] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ImportType] nvarchar(50) NOT NULL,
        [FileName] nvarchar(200) NOT NULL,
        [FileSize] bigint NOT NULL,
        [TotalRecords] int NOT NULL,
        [SuccessCount] int NOT NULL,
        [SkippedCount] int NOT NULL,
        [ErrorCount] int NOT NULL,
        [UpdatedCount] int NOT NULL,
        [ImportDate] datetime2 NOT NULL,
        [ImportedBy] nvarchar(100) NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [ProcessingTime] time NOT NULL,
        [DetailedResults] nvarchar(max) NULL,
        [SummaryMessage] nvarchar(500) NULL,
        [ImportOptions] nvarchar(max) NULL,
        CONSTRAINT [PK_ImportAudits] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Create indexes for performance
    CREATE NONCLUSTERED INDEX [IX_ImportAudits_ImportDate] 
    ON [dbo].[ImportAudits] ([ImportDate] ASC);

    CREATE NONCLUSTERED INDEX [IX_ImportAudits_ImportType] 
    ON [dbo].[ImportAudits] ([ImportType] ASC);

    PRINT 'ImportAudits table created successfully.';
END
ELSE
BEGIN
    PRINT 'ImportAudits table already exists.';
END