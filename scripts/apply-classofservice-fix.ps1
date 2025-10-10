# PowerShell script to add ClassOfServiceId column to UserPhones table
# Run this script when SQL Server is accessible

$connectionString = "Server=MICHUKI\SQLEXPRESS;Database=TABDB;Integrated Security=True;TrustServerCertificate=True;"

$sqlScript = @"
-- Add ClassOfServiceId column to UserPhones table
-- This script ONLY adds the ClassOfServiceId column if it doesn't exist

-- Check if the column already exists
IF NOT EXISTS (
    SELECT *
    FROM sys.columns
    WHERE object_id = OBJECT_ID('UserPhones')
    AND name = 'ClassOfServiceId'
)
BEGIN
    PRINT 'Adding ClassOfServiceId column to UserPhones table...'
    ALTER TABLE UserPhones ADD ClassOfServiceId int NULL;
    PRINT 'ClassOfServiceId column added successfully.'
END
ELSE
BEGIN
    PRINT 'ClassOfServiceId column already exists in UserPhones table.'
END

-- Add foreign key constraint if it doesn't exist
IF NOT EXISTS (
    SELECT *
    FROM sys.foreign_keys
    WHERE name = 'FK_UserPhones_ClassOfServices_ClassOfServiceId'
)
BEGIN
    PRINT 'Adding foreign key constraint FK_UserPhones_ClassOfServices_ClassOfServiceId...'
    ALTER TABLE UserPhones
    ADD CONSTRAINT FK_UserPhones_ClassOfServices_ClassOfServiceId
    FOREIGN KEY (ClassOfServiceId) REFERENCES ClassOfServices(Id);
    PRINT 'Foreign key constraint added successfully.'
END
ELSE
BEGIN
    PRINT 'Foreign key constraint already exists.'
END

-- Create index on ClassOfServiceId if it doesn't exist
IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name = 'IX_UserPhones_ClassOfServiceId'
    AND object_id = OBJECT_ID('UserPhones')
)
BEGIN
    PRINT 'Creating index IX_UserPhones_ClassOfServiceId...'
    CREATE INDEX IX_UserPhones_ClassOfServiceId ON UserPhones(ClassOfServiceId);
    PRINT 'Index created successfully.'
END
ELSE
BEGIN
    PRINT 'Index IX_UserPhones_ClassOfServiceId already exists.'
END

PRINT 'ClassOfService to UserPhone relationship setup complete!';
"@

Write-Host "Applying ClassOfServiceId column to UserPhones table..." -ForegroundColor Cyan

try {
    # Open connection
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()

    # Create command
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript

    # Capture output messages
    $handler = [System.Data.SqlClient.SqlInfoMessageEventHandler] {
        param($sender, $event)
        Write-Host $event.Message -ForegroundColor Yellow
    }
    $connection.add_InfoMessage($handler)
    $connection.FireInfoMessageEventOnUserErrors = $true

    # Execute
    $result = $command.ExecuteNonQuery()

    Write-Host "`nSuccessfully applied ClassOfServiceId column to UserPhones table!" -ForegroundColor Green
    Write-Host "The application should now work without the 'Invalid column name' error." -ForegroundColor Green
    Write-Host ""
    Write-Host "IMPORTANT: After running this script, please restart the application." -ForegroundColor Yellow
}
catch {
    Write-Host "`nError: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "If SQL Server is not running, please:" -ForegroundColor Yellow
    Write-Host "1. Start SQL Server" -ForegroundColor Yellow
    Write-Host "2. Run this script again: .\apply-classofservice-fix.ps1" -ForegroundColor Yellow
}
finally {
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}