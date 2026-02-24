using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddResetTestDataStoredProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE PROCEDURE ebill.sp_ResetTestData
AS
BEGIN
    SET NOCOUNT ON;

    -- Target tables to truncate (order doesn't matter since we drop FKs first)
    DECLARE @TargetTables TABLE (FullName NVARCHAR(256));
    INSERT INTO @TargetTables VALUES
        ('ebill.PhoneOverageDocuments'),
        ('ebill.PhoneOverageJustifications'),
        ('ebill.CallLogDocuments'),
        ('ebill.CallLogPaymentAssignments'),
        ('ebill.CallLogVerifications'),
        ('ebill.RecoveryLogs'),
        ('ebill.DeadlineTracking'),
        ('ebill.RecoveryJobExecutions'),
        ('ebill.CallRecords'),
        ('ebill.CallLogStagings'),
        ('ebill.StagingBatches'),
        ('ebill.Safaricom'),
        ('ebill.Airtel'),
        ('ebill.PSTNs'),
        ('ebill.PrivateWires'),
        ('ebill.CallLogs'),
        ('ebill.CallLogReconciliations');

    -- Step 1: Capture record counts before deletion
    DECLARE @Results TABLE (TableName NVARCHAR(128), RecordCount INT);
    DECLARE @sql NVARCHAR(MAX), @tbl NVARCHAR(256), @c INT;

    DECLARE cnt_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT FullName FROM @TargetTables;
    OPEN cnt_cursor;
    FETCH NEXT FROM cnt_cursor INTO @tbl;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            SET @sql = N'SELECT @c = COUNT(*) FROM ' + @tbl;
            EXEC sp_executesql @sql, N'@c INT OUTPUT', @c OUTPUT;
            INSERT INTO @Results VALUES (PARSENAME(@tbl, 1), @c);
        END TRY
        BEGIN CATCH
            INSERT INTO @Results VALUES (PARSENAME(@tbl, 1), 0);
        END CATCH
        FETCH NEXT FROM cnt_cursor INTO @tbl;
    END
    CLOSE cnt_cursor;
    DEALLOCATE cnt_cursor;

    -- Step 2: Save FK constraint definitions that reference any target table
    -- TRUNCATE requires zero incoming FK constraints, so we must drop and recreate them
    CREATE TABLE #FKsToRecreate (
        Id INT IDENTITY(1,1),
        ConstraintName NVARCHAR(256),
        ParentSchema NVARCHAR(128),
        ParentTable NVARCHAR(128),
        ReferencedSchema NVARCHAR(128),
        ReferencedTable NVARCHAR(128),
        DeleteAction NVARCHAR(60),
        UpdateAction NVARCHAR(60),
        ColumnList NVARCHAR(MAX),
        ReferencedColumnList NVARCHAR(MAX)
    );

    INSERT INTO #FKsToRecreate
    SELECT
        fk.name,
        SCHEMA_NAME(pt.schema_id), pt.name,
        SCHEMA_NAME(rt.schema_id), rt.name,
        fk.delete_referential_action_desc,
        fk.update_referential_action_desc,
        STRING_AGG(QUOTENAME(pc.name), ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id),
        STRING_AGG(QUOTENAME(rc.name), ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id)
    FROM sys.foreign_keys fk
    INNER JOIN sys.tables pt ON fk.parent_object_id = pt.object_id
    INNER JOIN sys.tables rt ON fk.referenced_object_id = rt.object_id
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns pc ON fkc.parent_object_id = pc.object_id AND fkc.parent_column_id = pc.column_id
    INNER JOIN sys.columns rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
    WHERE (SCHEMA_NAME(rt.schema_id) + '.' + rt.name) IN (SELECT FullName FROM @TargetTables)
    GROUP BY fk.name, SCHEMA_NAME(pt.schema_id), pt.name,
             SCHEMA_NAME(rt.schema_id), rt.name,
             fk.delete_referential_action_desc, fk.update_referential_action_desc;

    -- Step 3: Drop all FK constraints that reference target tables
    SET @sql = N'';
    SELECT @sql += 'ALTER TABLE ' + QUOTENAME(ParentSchema) + '.' + QUOTENAME(ParentTable)
        + ' DROP CONSTRAINT ' + QUOTENAME(ConstraintName) + ';' + CHAR(13)
    FROM #FKsToRecreate;

    IF @sql != N'' EXEC sp_executesql @sql;

    -- Step 4: TRUNCATE all target tables (instant, minimal logging, resets identity seeds)
    DECLARE trunc_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT FullName FROM @TargetTables;
    OPEN trunc_cursor;
    FETCH NEXT FROM trunc_cursor INTO @tbl;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            SET @sql = N'TRUNCATE TABLE ' + @tbl;
            EXEC sp_executesql @sql;
        END TRY
        BEGIN CATCH
            -- Table might not exist, skip silently
        END CATCH
        FETCH NEXT FROM trunc_cursor INTO @tbl;
    END
    CLOSE trunc_cursor;
    DEALLOCATE trunc_cursor;

    -- Step 5: Recreate all FK constraints
    SET @sql = N'';
    SELECT @sql += 'ALTER TABLE ' + QUOTENAME(ParentSchema) + '.' + QUOTENAME(ParentTable)
        + ' ADD CONSTRAINT ' + QUOTENAME(ConstraintName)
        + ' FOREIGN KEY (' + ColumnList + ') REFERENCES '
        + QUOTENAME(ReferencedSchema) + '.' + QUOTENAME(ReferencedTable)
        + ' (' + ReferencedColumnList + ')'
        + CASE WHEN DeleteAction != 'NO_ACTION' THEN ' ON DELETE ' + REPLACE(DeleteAction, '_', ' ') ELSE '' END
        + CASE WHEN UpdateAction != 'NO_ACTION' THEN ' ON UPDATE ' + REPLACE(UpdateAction, '_', ' ') ELSE '' END
        + ';' + CHAR(13)
    FROM #FKsToRecreate;

    IF @sql != N'' EXEC sp_executesql @sql;

    DROP TABLE #FKsToRecreate;

    -- Return deletion summary
    SELECT TableName, RecordCount FROM @Results WHERE RecordCount > 0 ORDER BY RecordCount DESC;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ebill.sp_ResetTestData;");
        }
    }
}
