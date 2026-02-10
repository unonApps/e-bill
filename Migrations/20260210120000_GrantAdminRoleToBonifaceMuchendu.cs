using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class GrantAdminRoleToBonifaceMuchendu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One-time assignment: grant Admin role to boniface.muchendu@un.org
            // Uses INSERT with NOT EXISTS to prevent duplicates safely.
            migrationBuilder.Sql(@"
                INSERT INTO AspNetUserRoles (UserId, RoleId)
                SELECT u.Id, r.Id
                FROM AspNetUsers u
                CROSS JOIN AspNetRoles r
                WHERE u.NormalizedEmail = 'BONIFACE.MUCHENDU@UN.ORG'
                  AND r.NormalizedName = 'ADMIN'
                  AND NOT EXISTS (
                      SELECT 1 FROM AspNetUserRoles ur
                      WHERE ur.UserId = u.Id AND ur.RoleId = r.Id
                  )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: remove Admin role from boniface.muchendu@un.org
            migrationBuilder.Sql(@"
                DELETE ur
                FROM AspNetUserRoles ur
                INNER JOIN AspNetUsers u ON ur.UserId = u.Id
                INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                WHERE u.NormalizedEmail = 'BONIFACE.MUCHENDU@UN.ORG'
                  AND r.NormalizedName = 'ADMIN'
            ");
        }
    }
}
