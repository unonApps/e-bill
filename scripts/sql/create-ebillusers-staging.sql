-- Create EbillUsers_InsertErrors table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers_InsertErrors]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EbillUsers_InsertErrors] (
        [ErrorId] int IDENTITY(1,1) NOT NULL,
        [StagingIndexNumber] nvarchar(100) NULL,
        [FirstName] nvarchar(200) NULL,
        [LastName] nvarchar(200) NULL,
        [Email] nvarchar(300) NULL,
        [OfficialMobileNumber] nvarchar(100) NULL,
        [Org] nvarchar(200) NULL,
        [Office] nvarchar(200) NULL,
        [SubOffice] nvarchar(200) NULL,
        [ClassOfService] nvarchar(100) NULL,
        [Location] nvarchar(200) NULL,
        [ErrorReason] nvarchar(400) NULL,
        [LoggedAt] datetime NULL CONSTRAINT [DF_EbillUsers_InsertErrors_LoggedAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_EbillUsers_InsertErrors] PRIMARY KEY CLUSTERED ([ErrorId] ASC)
    );

    CREATE INDEX [IX_EbillUsers_InsertErrors_LoggedAt] ON [EbillUsers_InsertErrors] ([LoggedAt]);

    PRINT 'EbillUsers_InsertErrors table created successfully';
END
ELSE
BEGIN
    PRINT 'EbillUsers_InsertErrors table already exists';
END
GO

-- Create EbillUsers_Staging table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers_Staging]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EbillUsers_Staging] (
        [OfficialMobileNumber] nvarchar(20) NULL,
        [FirstName] nvarchar(100) NULL,
        [LastName] nvarchar(100) NULL,
        [IndexNumber] nvarchar(50) NULL,
        [Location] nvarchar(200) NULL,
        [Org] nvarchar(50) NULL,
        [Office] nvarchar(50) NULL,
        [SubOffice] nvarchar(50) NULL,
        [ClassOfService] nvarchar(100) NULL,
        [Email] nvarchar(256) NULL
    );

    CREATE INDEX [IX_EbillUsers_Staging_IndexNumber] ON [EbillUsers_Staging] ([IndexNumber]);
    CREATE INDEX [IX_EbillUsers_Staging_Email] ON [EbillUsers_Staging] ([Email]);

    PRINT 'EbillUsers_Staging table created successfully';
END
ELSE
BEGIN
    PRINT 'EbillUsers_Staging table already exists';
END
GO
