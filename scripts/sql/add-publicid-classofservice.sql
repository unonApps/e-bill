-- Add PublicId column to ClassOfServices table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ClassOfServices]') AND name = 'PublicId')
BEGIN
    ALTER TABLE [ClassOfServices] ADD [PublicId] uniqueidentifier NOT NULL DEFAULT NEWID();
END
GO

-- Create index on PublicId column for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClassOfServices_PublicId' AND object_id = OBJECT_ID('[dbo].[ClassOfServices]'))
BEGIN
    CREATE UNIQUE INDEX [IX_ClassOfServices_PublicId] ON [ClassOfServices] ([PublicId]);
END
GO