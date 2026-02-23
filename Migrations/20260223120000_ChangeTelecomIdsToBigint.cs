using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTelecomIdsToBigint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safaricom: alter Id column from int to bigint (identity)
            migrationBuilder.Sql(@"
                ALTER TABLE [ebill].[Safaricom] DROP CONSTRAINT IF EXISTS [PK_Safaricom];
                ALTER TABLE [ebill].[Safaricom] ALTER COLUMN [Id] BIGINT NOT NULL;
                ALTER TABLE [ebill].[Safaricom] ADD CONSTRAINT [PK_Safaricom] PRIMARY KEY ([Id]);
            ");

            // Airtel: alter Id column from int to bigint (identity)
            migrationBuilder.Sql(@"
                ALTER TABLE [ebill].[Airtel] DROP CONSTRAINT IF EXISTS [PK_Airtel];
                ALTER TABLE [ebill].[Airtel] ALTER COLUMN [Id] BIGINT NOT NULL;
                ALTER TABLE [ebill].[Airtel] ADD CONSTRAINT [PK_Airtel] PRIMARY KEY ([Id]);
            ");

            // PSTN: alter Id column from int to bigint (identity)
            migrationBuilder.Sql(@"
                ALTER TABLE [ebill].[PSTNs] DROP CONSTRAINT IF EXISTS [PK_PSTNs];
                ALTER TABLE [ebill].[PSTNs] ALTER COLUMN [Id] BIGINT NOT NULL;
                ALTER TABLE [ebill].[PSTNs] ADD CONSTRAINT [PK_PSTNs] PRIMARY KEY ([Id]);
            ");

            // PrivateWires: alter Id column from int to bigint (identity)
            migrationBuilder.Sql(@"
                ALTER TABLE [ebill].[PrivateWires] DROP CONSTRAINT IF EXISTS [PK_PrivateWires];
                ALTER TABLE [ebill].[PrivateWires] ALTER COLUMN [Id] BIGINT NOT NULL;
                ALTER TABLE [ebill].[PrivateWires] ADD CONSTRAINT [PK_PrivateWires] PRIMARY KEY ([Id]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert Safaricom Id back to int
            migrationBuilder.Sql(@"
                ALTER TABLE [ebill].[Safaricom] DROP CONSTRAINT IF EXISTS [PK_Safaricom];
                ALTER TABLE [ebill].[Safaricom] ALTER COLUMN [Id] INT NOT NULL;
                ALTER TABLE [ebill].[Safaricom] ADD CONSTRAINT [PK_Safaricom] PRIMARY KEY ([Id]);
            ");

            // Revert Airtel Id back to int
            migrationBuilder.Sql(@"
                ALTER TABLE [ebill].[Airtel] DROP CONSTRAINT IF EXISTS [PK_Airtel];
                ALTER TABLE [ebill].[Airtel] ALTER COLUMN [Id] INT NOT NULL;
                ALTER TABLE [ebill].[Airtel] ADD CONSTRAINT [PK_Airtel] PRIMARY KEY ([Id]);
            ");

            // Revert PSTN Id back to int
            migrationBuilder.Sql(@"
                ALTER TABLE [ebill].[PSTNs] DROP CONSTRAINT IF EXISTS [PK_PSTNs];
                ALTER TABLE [ebill].[PSTNs] ALTER COLUMN [Id] INT NOT NULL;
                ALTER TABLE [ebill].[PSTNs] ADD CONSTRAINT [PK_PSTNs] PRIMARY KEY ([Id]);
            ");

            // Revert PrivateWires Id back to int
            migrationBuilder.Sql(@"
                ALTER TABLE [ebill].[PrivateWires] DROP CONSTRAINT IF EXISTS [PK_PrivateWires];
                ALTER TABLE [ebill].[PrivateWires] ALTER COLUMN [Id] INT NOT NULL;
                ALTER TABLE [ebill].[PrivateWires] ADD CONSTRAINT [PK_PrivateWires] PRIMARY KEY ([Id]);
            ");
        }
    }
}
