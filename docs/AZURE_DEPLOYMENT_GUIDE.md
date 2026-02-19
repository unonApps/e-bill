# Azure App Service Deployment Guide

## Prerequisites Completed ✅
1. **NuGet Packages Updated** - All EF Core packages updated to version 8.0.6
2. **Connection String Configured** - Production connection string set for Azure SQL Database
3. **Project Cleaned and Restored** - Dependencies resolved

## Azure Resources
- **App Service**: TABWeb20250926123812
- **Resource Group**: TABWeb20250926123812ResourceGroup
- **SQL Server**: ebiling.database.windows.net
- **Database**: tabdb
- **Subscription**: 774b35e5-d726-454b-ae7b-f0e688e72bd9

## Connection String Details
```
Server=tcp:ebiling.database.windows.net,1433;
Initial Catalog=tabdb;
User ID=ebiling;
Password=<SET_IN_AZURE_PORTAL>;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

## Deployment Steps

### 1. Build and Test Locally
```bash
dotnet clean
dotnet restore
dotnet build --configuration Release
dotnet ef database update
dotnet run --environment Production
```

### 2. Publish to Azure App Service

#### Option A: Using Visual Studio
1. Right-click on project → Publish
2. Select "Azure" → "Azure App Service (Windows)"
3. Select existing App Service: TABWeb20250926123812
4. Click "Publish"

#### Option B: Using Azure CLI
```bash
# Login to Azure
az login

# Build and publish
dotnet publish -c Release -o ./publish

# Deploy to App Service
az webapp deployment source config-zip \
  --resource-group TABWeb20250926123812ResourceGroup \
  --name TABWeb20250926123812 \
  --src ./publish.zip
```

#### Option C: Using PowerShell Script
Create `deploy-to-azure.ps1`:
```powershell
# Build the application
dotnet clean
dotnet restore
dotnet build --configuration Release
dotnet publish -c Release -o ./publish

# Zip the publish folder
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force

# Deploy using Azure CLI
az webapp deployment source config-zip `
  --resource-group TABWeb20250926123812ResourceGroup `
  --name TABWeb20250926123812 `
  --src ./publish.zip

Write-Host "Deployment completed!"
```

### 3. Configure App Service Settings

In Azure Portal, navigate to your App Service and configure:

#### Application Settings
```json
{
  "ASPNETCORE_ENVIRONMENT": "Production",
  "ASPNETCORE_URLS": "http://+:80;https://+:443"
}
```

#### Connection Strings
Add under "Connection strings" section:
- Name: `DefaultConnection`
- Value: `Server=tcp:ebiling.database.windows.net,1433;Initial Catalog=tabdb;Persist Security Info=False;User ID=ebiling;Password=<SET_IN_AZURE_PORTAL>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`
- Type: `SQLAzure`

### 4. Database Migration

#### Option A: Run Migrations from Local Machine
```bash
# Update connection string in appsettings.json temporarily
dotnet ef database update --connection "Server=tcp:ebiling.database.windows.net,1433;Initial Catalog=tabdb;User ID=ebiling;Password=<SET_IN_AZURE_PORTAL>;Encrypt=True;TrustServerCertificate=False;"
```

#### Option B: Using SQL Scripts
1. Generate migration script:
```bash
dotnet ef migrations script -o migration.sql
```
2. Execute on Azure SQL Database using Azure Portal Query Editor or SSMS

### 5. Verify Deployment

1. **Check App Service Health**
   - Navigate to: https://tabweb20250926123812.azurewebsites.net
   - Check Application Insights for errors

2. **Test Database Connection**
   - Try logging in with a test user
   - Check if data loads correctly

3. **Monitor Logs**
   - Enable Application Logging in App Service
   - Use Log Stream to view real-time logs

## Troubleshooting

### Common Issues and Solutions

#### 1. Package Version Conflicts
**Error**: "Package restore failed"
**Solution**: Ensure all EF Core packages are version 8.0.6

#### 2. Connection String Issues
**Error**: "Cannot connect to SQL Database"
**Solution**:
- Verify firewall rules allow App Service IP
- Check password doesn't contain special characters that need escaping
- Ensure "Allow Azure services" is enabled on SQL Server

#### 3. Migration Failures
**Error**: "Database migration failed"
**Solution**:
- Run migrations locally first
- Check user has db_owner permissions
- Verify all migration files are included in deployment

#### 4. Application Startup Issues
**Error**: "HTTP Error 500.30"
**Solution**:
- Check ASPNETCORE_ENVIRONMENT is set to Production
- Verify all required services are registered in Program.cs
- Check Event Logs in Kudu console

### Azure Service Connector
The Azure Service Connector has been successfully created:
- Resource ID: `/subscriptions/774b35e5-d726-454b-ae7b-f0e688e72bd9/resourcegroups/TABWeb20250926123812ResourceGroup/providers/Microsoft.Web/sites/TABWeb20250926123812/providers/Microsoft.ServiceLinker/linkers/ConnectionStrings_501AC3732D`

This connector manages the connection between your App Service and SQL Database.

## Security Considerations

1. **Connection String Security**
   - Store connection strings in Azure Key Vault for production
   - Use Managed Identity instead of SQL authentication when possible

2. **SSL/TLS**
   - Ensure "HTTPS Only" is enabled in App Service
   - Configure custom domain with SSL certificate

3. **IP Restrictions**
   - Configure IP restrictions if needed
   - Use Private Endpoints for enhanced security

## Performance Optimization

1. **App Service Plan**
   - Consider scaling up to Standard or Premium tier for production
   - Enable Always On to prevent cold starts

2. **Database**
   - Monitor DTU usage and scale as needed
   - Implement caching for frequently accessed data

3. **Application Insights**
   - Enable for monitoring and diagnostics
   - Set up alerts for critical metrics

## Backup and Recovery

1. **Database Backup**
   - Configure automated backups in Azure SQL Database
   - Test restore procedures regularly

2. **App Service Backup**
   - Configure backup schedule in App Service
   - Store backups in separate storage account

## Next Steps

1. ✅ Deploy application using preferred method
2. ✅ Verify database migrations are applied
3. ✅ Test all critical functionality
4. ✅ Configure monitoring and alerts
5. ✅ Document any environment-specific configurations

## Support Contacts

- Azure Portal: https://portal.azure.com
- App Service URL: https://tabweb20250926123812.azurewebsites.net
- Kudu Console: https://tabweb20250926123812.scm.azurewebsites.net

## Important Notes

⚠️ **Password Security**: The password contains special characters. If you encounter issues, consider:
- URL encoding the password in connection strings
- Using Azure Key Vault to store sensitive credentials
- Implementing Managed Identity for passwordless authentication

⚠️ **Firewall Rules**: Ensure your Azure SQL Database allows connections from:
- Azure App Service (Enable "Allow Azure services")
- Your development machine IP (for migrations)

✅ **Package Versions**: All Entity Framework Core packages are now at version 8.0.6, matching Azure's requirements.