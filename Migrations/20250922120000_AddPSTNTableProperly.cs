using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPSTNTableProperly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only add SubOfficeId if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns
                              WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]')
                              AND name = 'SubOfficeId')
                BEGIN
                    ALTER TABLE [AspNetUsers] ADD [SubOfficeId] int NULL;
                END
            ");

            // Create SubOffices table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SubOffices]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [SubOffices] (
                        [Id] int NOT NULL IDENTITY,
                        [Name] nvarchar(100) NOT NULL,
                        [Description] nvarchar(500) NULL,
                        [ContactPerson] nvarchar(100) NULL,
                        [PhoneNumber] nvarchar(20) NULL,
                        [Email] nvarchar(100) NULL,
                        [Address] nvarchar(200) NULL,
                        [OfficeId] int NOT NULL,
                        [IsActive] bit NOT NULL DEFAULT 1,
                        [CreatedDate] datetime2 NOT NULL,
                        [ModifiedDate] datetime2 NULL,
                        CONSTRAINT [PK_SubOffices] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SubOffices_Offices_OfficeId] FOREIGN KEY ([OfficeId]) REFERENCES [Offices] ([Id]) ON DELETE CASCADE
                    );
                END
            ");

            // Create indexes if they don't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SubOffices_OfficeId' AND object_id = OBJECT_ID('[dbo].[SubOffices]'))
                BEGIN
                    CREATE INDEX [IX_SubOffices_OfficeId] ON [SubOffices] ([OfficeId]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AspNetUsers_SubOfficeId' AND object_id = OBJECT_ID('[dbo].[AspNetUsers]'))
                BEGIN
                    CREATE INDEX [IX_AspNetUsers_SubOfficeId] ON [AspNetUsers] ([SubOfficeId]);
                END
            ");

            // Add foreign key if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AspNetUsers_SubOffices_SubOfficeId')
                BEGIN
                    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_SubOffices_SubOfficeId]
                    FOREIGN KEY ([SubOfficeId]) REFERENCES [SubOffices] ([Id]);
                END
            ");

            // Create PSTNs table
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [PSTNs] (
                        [Id] int NOT NULL IDENTITY,
                        [Ext] nvarchar(50) NULL,
                        [Dialed] nvarchar(100) NULL,
                        [Time] time NULL,
                        [Dest] nvarchar(200) NULL,
                        [Dl] nvarchar(50) NULL,
                        [Durx] decimal(18,2) NULL,
                        [Org] nvarchar(100) NULL,
                        [Office] nvarchar(100) NULL,
                        [SubOffice] nvarchar(100) NULL,
                        [Org_Unit] nvarchar(100) NULL,
                        [Name] nvarchar(200) NULL,
                        [Date] datetime2 NULL,
                        [Dur] decimal(18,2) NULL,
                        [Kshs] decimal(18,2) NULL,
                        [Inde_] nvarchar(50) NULL,
                        [Location] nvarchar(200) NULL,
                        [Oca] nvarchar(50) NULL,
                        [Car] nvarchar(50) NULL,
                        [CreatedDate] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NULL,
                        [ModifiedDate] datetime2 NULL,
                        [ModifiedBy] nvarchar(max) NULL,
                        [ImportAuditId] int NULL,
                        CONSTRAINT [PK_PSTNs] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_PSTNs_ImportAudits_ImportAuditId] FOREIGN KEY ([ImportAuditId]) REFERENCES [ImportAudits] ([Id])
                    );
                END
            ");

            // Create index for PSTNs
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PSTNs_ImportAuditId' AND object_id = OBJECT_ID('[dbo].[PSTNs]'))
                BEGIN
                    CREATE INDEX [IX_PSTNs_ImportAuditId] ON [PSTNs] ([ImportAuditId]);
                END
            ");

            // Handle SimRequests index properly
            migrationBuilder.Sql(@"
                -- Drop unique index if exists and create non-unique
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SimRequests_IndexNo' AND object_id = OBJECT_ID('[dbo].[SimRequests]') AND is_unique = 1)
                BEGIN
                    DROP INDEX [IX_SimRequests_IndexNo] ON [SimRequests];
                END

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SimRequests_IndexNo' AND object_id = OBJECT_ID('[dbo].[SimRequests]'))
                BEGIN
                    CREATE INDEX [IX_SimRequests_IndexNo] ON [SimRequests] ([IndexNo]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND type in (N'U'))
                BEGIN
                    DROP TABLE [PSTNs];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AspNetUsers_SubOffices_SubOfficeId')
                BEGIN
                    ALTER TABLE [AspNetUsers] DROP CONSTRAINT [FK_AspNetUsers_SubOffices_SubOfficeId];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SubOffices]') AND type in (N'U'))
                BEGIN
                    DROP TABLE [SubOffices];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns
                          WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]')
                          AND name = 'SubOfficeId')
                BEGIN
                    ALTER TABLE [AspNetUsers] DROP COLUMN [SubOfficeId];
                END
            ");
        }
    }
}