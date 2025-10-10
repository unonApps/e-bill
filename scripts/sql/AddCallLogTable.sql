-- Create CallLogs table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CallLogs' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[CallLogs] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [AccountNo] nvarchar(50) NOT NULL,
        [SubAccountNo] nvarchar(50) NOT NULL,
        [SubAccountName] nvarchar(200) NOT NULL,
        [MSISDN] nvarchar(20) NOT NULL,
        [TaxInvoiceSummaryNo] nvarchar(100) NOT NULL DEFAULT '',
        [InvoiceNo] nvarchar(100) NOT NULL DEFAULT '',
        [InvoiceDate] datetime2 NOT NULL,
        [NetAccessFee] decimal(18,2) NOT NULL,
        [NetUsageLessTax] decimal(18,2) NOT NULL,
        [LessTaxes] decimal(18,2) NOT NULL,
        [VAT16] decimal(18,2) NULL,
        [Excise15] decimal(18,2) NULL,
        [GrossTotal] decimal(18,2) NOT NULL,
        [EbillUserId] int NULL,
        [ImportedBy] nvarchar(max) NULL,
        [ImportedDate] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_CallLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Create foreign key to EbillUsers
    ALTER TABLE [dbo].[CallLogs]
    ADD CONSTRAINT [FK_CallLogs_EbillUsers_EbillUserId] 
    FOREIGN KEY ([EbillUserId]) 
    REFERENCES [dbo].[EbillUsers] ([Id])
    ON DELETE SET NULL;

    -- Create index on MSISDN for performance
    CREATE NONCLUSTERED INDEX [IX_CallLogs_MSISDN] 
    ON [dbo].[CallLogs] ([MSISDN] ASC);

    -- Create index on EbillUserId for performance
    CREATE NONCLUSTERED INDEX [IX_CallLogs_EbillUserId] 
    ON [dbo].[CallLogs] ([EbillUserId] ASC);

    PRINT 'CallLogs table created successfully.';
END
ELSE
BEGIN
    PRINT 'CallLogs table already exists.';
END