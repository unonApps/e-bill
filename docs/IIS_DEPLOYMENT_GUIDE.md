# Secure IIS Deployment Guide (On-Premises / Local Data Center)

TAB.Web -- ASP.NET Core 8.0 on IIS in a Windows Server VM.

---

## 1. VM / OS Setup

| Item | Recommendation |
|------|----------------|
| OS | Windows Server 2022 (2019 minimum) |
| Patches | Enable Windows Update, auto-install security patches |
| Firewall | Allow only port 443 (HTTPS) and 3389 (RDP, restricted to admin IPs) |
| Antivirus | Windows Defender or equivalent |
| Service Account | Dedicated domain account for the app pool (not `LocalSystem`) |

---

## 2. IIS Prerequisites

Install these Windows features on the VM:

```powershell
Install-WindowsFeature -Name Web-Server, Web-WebSockets, Web-Dyn-Compression -IncludeManagementTools
```

Install the **.NET 8 Hosting Bundle** (includes ASP.NET Core Module v2):
- Download from https://dotnet.microsoft.com/download/dotnet/8.0
- Run `dotnet-hosting-8.x.x-win.exe`
- Restart IIS after installation: `iisreset`

The existing `deployment/setup-iis.ps1` script should handle this.

---

## 3. SSL/TLS Certificate

The app enforces HTTPS and Azure AD requires it for OAuth redirects.

| Option | Best For |
|--------|----------|
| Internal CA cert (ADCS) | Organizations with Active Directory Certificate Services |
| Commercial cert | DigiCert, Sectigo, etc. -- if accessed externally |
| Let's Encrypt (win-acme) | Free, requires port 80 for ACME challenge |

### Bind the certificate in IIS

```powershell
# After importing the .pfx into the server's certificate store:
New-WebBinding -Name "TABWeb" -Protocol "https" -Port 443 -SslFlags 1

netsh http add sslcert ipport=0.0.0.0:443 certhash=<THUMBPRINT> appid='{YOUR-APP-GUID}'
```

### TLS hardening

Disable TLS 1.0 and 1.1, enforce TLS 1.2+ only. Use [IIS Crypto](https://www.nartac.com/Products/IISCrypto) or configure via registry.

---

## 4. Secrets Management

The `appsettings.Production.json` and `appsettings.Development.json` files are excluded from git via `.gitignore` and have never been committed. Keep it that way.

### Secrets that must be protected

- `AzureAd:ClientSecret` -- Azure AD OAuth client secret
- `ConnectionStrings:DefaultConnection` -- SQL Server password
- `AzureBlobStorage:StorageAccountKey` -- Azure Blob Storage access key
- `AzureBlobStorage:StorageConnection` -- full connection string with key
- `EmailSettings:Password` -- SMTP credentials

### Option A: Environment Variables (simplest)

Set secrets directly in `web.config` on the server (this file is not in git):

```xml
<environmentVariables>
  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  <environmentVariable name="ConnectionStrings__DefaultConnection"
    value="Data Source=...;Password=SECURE;Encrypt=True;TrustServerCertificate=True;" />
  <environmentVariable name="AzureAd__ClientSecret" value="YOUR_SECRET" />
  <environmentVariable name="AzureBlobStorage__StorageAccountKey" value="YOUR_KEY" />
</environmentVariables>
```

Note: use double underscores (`__`) as section separators in environment variable names.

### Option B: Windows DPAPI (good)

Use `dotnet user-secrets` for development. For production, encrypt config sections using Windows Data Protection API.

### Option C: Azure Key Vault (best)

The app already uses Azure AD -- add `Azure.Extensions.AspNetCore.Configuration.Secrets` NuGet package and configure Key Vault as a configuration source.

---

## 5. IIS Application Pool Configuration

| Setting | Value |
|---------|-------|
| App Pool Name | `TABWebPool` |
| .NET CLR Version | `No Managed Code` (ASP.NET Core uses its own runtime) |
| Pipeline Mode | `Integrated` |
| Identity | Custom account (e.g., `DOMAIN\svc-tabweb`) |
| Start Mode | `AlwaysRunning` (prevents cold starts, keeps Hangfire alive) |
| Idle Timeout | `0` (disabled -- required for Hangfire background jobs) |
| Rapid-Fail Protection | 5 failures / 5 min (default) |
| Recycling | Daily at 3:00 AM (off-peak) |

The app uses **in-process hosting** (`hostingModel="inprocess"` in `web.config`) for best IIS performance.

---

## 6. Network and Database Security

### Azure SQL connection

The app connects to Azure SQL at `unonsqlsvr01.database.windows.net`.

- Whitelist the VM's public IP in Azure SQL firewall rules
- Consider a VPN tunnel or Azure ExpressRoute between the data center and Azure
- `Encrypt=True` is already set in the connection string -- keep it
- Consider switching to Managed Identity instead of SQL username/password

### Azure Blob Storage

- Restrict access via IP allowlist on the storage account
- Consider Azure Private Endpoints if using VPN/ExpressRoute

### Internal network

- Place the VM in a DMZ or segmented VLAN
- Use a reverse proxy or load balancer in front of IIS if needed

---

## 7. Deployment Process

Existing scripts are in `deployment/`. The target server is `10.104.104.78`.

### Manual deployment flow

```powershell
# 1. Build on the CI/build machine (not on the server)
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish

# 2. Stop the app pool on the target server
Invoke-Command -ComputerName 10.104.104.78 -ScriptBlock {
    Stop-WebAppPool -Name "TABWebPool"
}

# 3. Copy published files (never overwrite server-side appsettings)
robocopy .\publish \\10.104.104.78\c$\inetpub\wwwroot\TABWeb /MIR `
    /XF appsettings.Production.json appsettings.Development.json

# 4. Start the app pool
Invoke-Command -ComputerName 10.104.104.78 -ScriptBlock {
    Start-WebAppPool -Name "TABWebPool"
}
```

### Using existing scripts

```powershell
.\deployment\DEPLOY-FULL.ps1
```

### Key rules

- Never overwrite `appsettings.Production.json` on the server
- Never copy `appsettings.Development.json` to the server
- The `web.config` on the server contains environment-specific settings -- merge carefully

---

## 8. Security Hardening Checklist

### Already implemented

- [x] HTTPS enforcement (`UseHttpsRedirection()` + HSTS)
- [x] Cookie security (`HttpOnly`, `Secure=Always`, `SameSite=Lax`)
- [x] Authentication on all pages (`AuthorizeFolder("/")`)
- [x] Account lockout (5 attempts / 15-minute lockout)
- [x] Strong password policy (12+ chars, upper, lower, digit, special)
- [x] Forwarded headers for reverse proxy (`UseForwardedHeaders()`)
- [x] Data Protection keys persisted to `App_Data/`
- [x] Hangfire dashboard restricted to Admin role
- [x] Secrets files excluded from git

### To do

- [ ] Add security response headers (see below)
- [ ] Restrict `AllowedHosts` from `*` to your actual domain
- [ ] Set `DetailedErrors: false` in production config
- [ ] Configure structured logging with log rotation
- [ ] Set up database backup schedule
- [ ] Configure Windows Firewall rules
- [ ] Set up health check endpoint for monitoring
- [ ] Rotate Azure AD client secret and SQL credentials after deployment

### Recommended security headers

Add to `Program.cs` in the HTTP pipeline:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';";
    await next();
});
```

---

## 9. `web.config` Reference

The existing `web.config` at the project root is configured for IIS in-process hosting:

```xml
<aspNetCore processPath=".\TAB.Web.exe"
            stdoutLogEnabled="true"
            stdoutLogFile=".\logs\stdout"
            hostingModel="inprocess"
            requestTimeout="00:20:00">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
    <environmentVariable name="ASPNETCORE_HTTPS_PORT" value="443" />
  </environmentVariables>
</aspNetCore>
```

Key IIS limits already configured:
- Max request body: 100 MB (`maxAllowedContentLength`)
- Max query string: 32,768 bytes (for large Azure AD tokens)
- Execution timeout: 1,200 seconds
- Stdout logging enabled at `.\logs\stdout`

---

## 10. Monitoring and Maintenance

| Area | Approach |
|------|----------|
| Application logs | Stdout logs at `.\logs\stdout` -- set up log rotation |
| Background jobs | Hangfire dashboard at `/hangfire` (Admin only) |
| Windows events | ASP.NET Core Module logs to Windows Event Viewer |
| Health checks | Add `app.MapHealthChecks("/health")` for uptime monitoring |
| Database backups | Azure SQL has automatic backups -- verify retention policy |
| Data Protection keys | Back up `App_Data/DataProtection-Keys/` directory |
| SSL renewal | Set calendar reminders before cert expiry |

---

## 11. Azure AD Configuration

After deployment, update the Azure AD app registration:

1. Go to Azure Portal > App registrations > TAB Web
2. Under **Authentication**, add the new redirect URI:
   ```
   https://YOUR-SERVER-DOMAIN/signin-oidc
   ```
3. Add the sign-out URI:
   ```
   https://YOUR-SERVER-DOMAIN/signout-callback-oidc
   ```
4. Update `appsettings.Production.json` on the server with the matching `RedirectUri`

---

## 12. Quick Start Summary

```
1. Provision Windows Server 2022 VM
2. Install IIS + .NET 8 Hosting Bundle
3. Obtain and bind an SSL certificate
4. Create app pool (No Managed Code, custom identity, AlwaysRunning, idle timeout 0)
5. Deploy application files (exclude secrets files)
6. Configure appsettings.Production.json on the server with real credentials
7. Configure Windows Firewall (port 443 only)
8. Whitelist VM IP in Azure SQL firewall
9. Update Azure AD redirect URIs to the new server's domain
10. Test end-to-end: login, page access, Hangfire jobs, file uploads
```
