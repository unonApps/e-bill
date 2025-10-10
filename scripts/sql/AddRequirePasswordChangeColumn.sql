-- Set the correct options
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
GO

-- Add RequirePasswordChange column to AspNetUsers table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'RequirePasswordChange')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD RequirePasswordChange BIT NOT NULL DEFAULT 0;
    PRINT 'Column RequirePasswordChange added to AspNetUsers table.';
END
ELSE
BEGIN
    PRINT 'Column RequirePasswordChange already exists in AspNetUsers table.';
END
GO 