# Script to apply Entity Framework migrations to Azure SQL Database

Write-Host "Applying EF Migrations to Azure SQL Database..." -ForegroundColor Green

# Azure SQL connection string
$connectionString = "Server=tcp:ebiling.database.windows.net,1433;Initial Catalog=tabdb;User ID=ebiling;Password=KamitiF5%254;Encrypt=True;TrustServerCertificate=False;"

Write-Host "Step 1: Checking current migrations..." -ForegroundColor Yellow
dotnet ef migrations list

Write-Host "`nStep 2: Applying migrations to Azure SQL..." -ForegroundColor Yellow
dotnet ef database update --connection $connectionString

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Migrations applied successfully!" -ForegroundColor Green

    Write-Host "`nStep 3: Verifying admin user creation..." -ForegroundColor Yellow

    # Create a simple C# script to check admin user
    $checkScript = @"
using System;
using System.Data.SqlClient;

var connectionString = @"$connectionString";
using var connection = new SqlConnection(connectionString);
connection.Open();

// Check if admin user exists
var command = new SqlCommand("SELECT COUNT(*) FROM AspNetUsers WHERE Email = 'admin@example.com'", connection);
var count = (int)command.ExecuteScalar();

if (count > 0) {
    Console.WriteLine("✅ Admin user exists in database");
} else {
    Console.WriteLine("⚠️ Admin user NOT found - will be created on first app startup");
}

// Check total users
command = new SqlCommand("SELECT COUNT(*) FROM AspNetUsers", connection);
var totalUsers = (int)command.ExecuteScalar();
Console.WriteLine($"Total users in database: {totalUsers}");

// Check roles
command = new SqlCommand("SELECT COUNT(*) FROM AspNetRoles", connection);
var totalRoles = (int)command.ExecuteScalar();
Console.WriteLine($"Total roles in database: {totalRoles}");
"@

    # Save and run the check script
    $checkScript | Out-File -FilePath "check-admin.csx" -Encoding UTF8
    dotnet script check-admin.csx
    Remove-Item "check-admin.csx"

} else {
    Write-Host "`n❌ Migration failed!" -ForegroundColor Red
    Write-Host "Common issues:" -ForegroundColor Yellow
    Write-Host "1. Check if password needs encoding (% -> %25)" -ForegroundColor White
    Write-Host "2. Verify firewall rules allow your IP" -ForegroundColor White
    Write-Host "3. Ensure user has db_owner permissions" -ForegroundColor White
}

Write-Host "`nLogin Credentials:" -ForegroundColor Cyan
Write-Host "Username: admin@example.com" -ForegroundColor White
Write-Host "Password: Admin123!" -ForegroundColor White
Write-Host "`nURL: https://tabweb20250926123812.azurewebsites.net/Account/Login" -ForegroundColor Cyan