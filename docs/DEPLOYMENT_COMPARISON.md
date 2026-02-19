# Deployment Comparison: IIS On-Premises vs Azure App Service

Side-by-side comparison for deploying TAB.Web (ASP.NET Core 8.0).

---

## Quick Summary

| Factor | IIS On-Premises (VM) | Azure App Service |
|--------|----------------------|-------------------|
| **Monthly cost** | VM license + hardware/hosting | ~$70-150/mo (B2/S1 tier) |
| **Setup effort** | High (OS, IIS, certs, firewall) | Low (portal or CLI) |
| **Ongoing maintenance** | You manage everything | Microsoft manages infra |
| **SSL certificates** | You buy/renew/bind manually | Free managed certs or custom |
| **Scaling** | Manual (add VMs, load balancer) | Built-in auto-scale |
| **Uptime SLA** | Depends on your infra | 99.95% SLA |
| **Data residency** | Full control -- your data center | Azure region (West Europe currently) |
| **Network latency** | Low if users are on-site | Depends on region |
| **Compliance** | Easier for strict data sovereignty | Requires Azure compliance review |
| **Hangfire jobs** | Works natively (Always Running pool) | Works with Always On (Standard+ tier) |
| **Deployment speed** | Slower (manual or scripted) | Fast (CI/CD, zip deploy, git push) |
| **Rollback** | Manual (swap folders, restore backup) | Deployment slots (instant swap) |
| **Monitoring** | Windows Event Viewer + custom | Application Insights built-in |

---

## Detailed Comparison

### 1. Cost

#### IIS On-Premises
- Windows Server license (if not already covered)
- VM hosting / hardware costs
- Network bandwidth
- Staff time for maintenance, patching, monitoring
- SSL certificate (~$10-200/yr depending on provider)
- No per-request or bandwidth charges

#### Azure App Service
- **Basic (B1)**: ~$55/mo -- good for dev/test
- **Standard (S1)**: ~$73/mo -- production minimum (includes Always On, deployment slots, auto-scale)
- **Premium (P1v3)**: ~$138/mo -- better performance, VNet integration
- Azure SQL: already in use, no change
- Azure Blob Storage: already in use, no change
- Free managed SSL for `*.azurewebsites.net`; custom domain certs also free via App Service Managed Certificates

**Verdict**: On-prem is cheaper if you already have infrastructure. App Service is cheaper if you factor in staff time.

---

### 2. Security

#### IIS On-Premises
- Full control over the OS, firewall, patches
- You manage Windows Updates, antivirus, TLS config
- Secrets stored in environment variables or DPAPI on the server
- Network isolation is your responsibility
- Must harden IIS manually (disable weak ciphers, etc.)
- Physical security of the data center is your responsibility

#### Azure App Service
- Microsoft patches the underlying OS automatically
- Built-in DDoS protection
- Secrets managed via Azure Key Vault or App Service Configuration (encrypted at rest)
- VNet integration available for private connectivity to Azure SQL
- Managed Identity eliminates the need for SQL passwords entirely
- Built-in authentication/authorization features (Easy Auth) as an additional layer
- SOC 2, ISO 27001, HIPAA compliant infrastructure

**Verdict**: App Service is more secure by default with less effort. On-prem gives more control but requires more expertise.

---

### 3. Deployment and CI/CD

#### IIS On-Premises
- Existing scripts: `deployment/DEPLOY-FULL.ps1` targets `10.104.104.78`
- Manual process: build locally, copy to server, restart app pool
- Rollback: `deployment/ROLLBACK.ps1` (manual)
- No deployment slots -- downtime during updates
- Azure Pipelines config exists but disabled (pending parallelism grant)

#### Azure App Service
- Multiple deployment methods: zip deploy, git push, GitHub Actions, Azure DevOps
- **Deployment slots** (Standard+ tier): deploy to staging, test, then swap to production with zero downtime
- Built-in CI/CD with GitHub Actions or Azure DevOps
- One-click rollback to any previous deployment
- Kudu console for debugging

**Verdict**: App Service is significantly easier to deploy and roll back.

---

### 4. Hangfire Background Jobs

#### IIS On-Premises
- Works well with `AlwaysRunning` start mode and idle timeout disabled
- App pool recycling at 3 AM is fine -- Hangfire recovers automatically
- Full control over job scheduling and resources

#### Azure App Service
- Requires **Always On** (Standard tier or above -- not available on Basic/Free)
- Without Always On, the app sleeps after 20 minutes of inactivity and Hangfire stops
- Works reliably on Standard (S1) or higher with Always On enabled
- Alternative: use Azure Functions for background jobs (more complex migration)

**Verdict**: Both work. On-prem is simpler. App Service requires Standard tier minimum ($73/mo).

---

### 5. SSL/TLS

#### IIS On-Premises
- You must obtain, install, and renew certificates manually
- Use IIS Crypto to harden TLS settings
- Certificate costs: $0 (Let's Encrypt) to $200/yr (commercial)

#### Azure App Service
- Free managed certificates for custom domains
- Automatic renewal
- TLS 1.2 enforced by default
- Custom certificates also supported

**Verdict**: App Service wins -- free, automatic SSL.

---

### 6. Monitoring and Diagnostics

#### IIS On-Premises
- Stdout logs at `.\logs\stdout`
- Windows Event Viewer
- Hangfire dashboard at `/hangfire`
- Must set up your own monitoring (Prometheus, Grafana, etc.)
- No built-in APM (Application Performance Monitoring)

#### Azure App Service
- **Application Insights**: built-in APM with request tracing, exceptions, dependencies, live metrics
- **Log Stream**: real-time log viewing in the portal
- **Kudu console**: SSH-like access for debugging
- **Health checks**: built-in endpoint monitoring
- **Alerts**: email/SMS/webhook on errors, high CPU, slow responses

**Verdict**: App Service is far superior for monitoring.

---

### 7. Scaling

#### IIS On-Premises
- Vertical scaling: add CPU/RAM to the VM (requires downtime)
- Horizontal scaling: add more VMs + configure load balancer (significant effort)
- No auto-scaling

#### Azure App Service
- Vertical scaling: change tier in the portal (minimal downtime)
- Horizontal scaling: auto-scale based on CPU, memory, or request count
- Scale out to multiple instances with built-in load balancer

**Verdict**: App Service wins for scaling flexibility.

---

### 8. Data Residency and Compliance

#### IIS On-Premises
- Data stays in your physical data center
- Full control over data location
- Easier to meet strict data sovereignty requirements
- No third-party access to the server

#### Azure App Service
- Data in Azure West Europe region (current setup)
- Microsoft compliance certifications (SOC 2, ISO 27001, GDPR)
- Data could theoretically be accessed by Microsoft under subpoena
- Some organizations require on-prem for regulatory reasons

**Verdict**: On-prem wins if data sovereignty is a strict requirement. App Service is fine for most organizations.

---

### 9. Network Architecture

#### IIS On-Premises
```
Users (LAN) --> Firewall --> IIS VM (10.104.104.78:443)
                                |
                                +--> Azure SQL (unonsqlsvr01.database.windows.net)
                                +--> Azure Blob Storage (unondataservices)
                                +--> Azure AD (login.microsoftonline.com)
```
- Users on the local network get low latency
- External users need VPN or public IP with firewall rules
- Hybrid: app on-prem but database and storage in Azure (current setup)

#### Azure App Service
```
Users (Internet) --> Azure Front Door/CDN --> App Service
                                                |
                                                +--> Azure SQL (same region, low latency)
                                                +--> Azure Blob Storage (same region)
                                                +--> Azure AD (same cloud)
```
- All Azure resources in the same region = low latency between services
- Users access via internet (global reach)
- VNet integration for private connectivity to Azure SQL

**Verdict**: App Service has better Azure-to-Azure performance. On-prem is better for local network users. Note: the current hybrid setup (app on-prem, DB in Azure) adds latency on every database call.

---

## Recommendation Matrix

| If your priority is... | Choose |
|-------------------------|--------|
| Lowest total cost of ownership | App Service (less staff time) |
| Fastest time to production | App Service |
| Data must stay in your building | IIS On-Premises |
| Best Azure SQL performance | App Service (same network) |
| Zero-downtime deployments | App Service (deployment slots) |
| Easiest monitoring | App Service (Application Insights) |
| Maximum control | IIS On-Premises |
| Compliance/regulatory requirement for on-prem | IIS On-Premises |
| Auto-scaling for variable load | App Service |
| Local network users (intranet app) | IIS On-Premises |

---

## Hybrid Consideration

Your current architecture is already hybrid: the app runs on-prem but uses Azure SQL, Azure Blob Storage, and Azure AD. This means:

- Every database query crosses the internet (added latency)
- You're already dependent on Azure for core services
- Moving the app to App Service would put everything in the same Azure region, reducing latency
- If Azure goes down, your on-prem app stops working anyway (no database)

This makes a strong case for Azure App Service unless there is a specific data sovereignty or compliance reason to keep the application server on-premises.

---

## Azure App Service -- Complete Settings Reference

Every setting the app needs, mapped to the Azure Portal.

### Application Settings (Environment Variables)

In Azure Portal: **App Service > Settings > Environment variables > App settings**.

| Name | Value | Purpose |
|------|-------|---------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Disables dev error pages, enables HSTS |
| `AzureAd__Instance` | `https://login.microsoftonline.com/` | Azure AD login endpoint |
| `AzureAd__TenantId` | `<your tenant ID>` | Azure AD tenant |
| `AzureAd__ClientId` | `<your client ID>` | App registration client ID |
| `AzureAd__ClientSecret` | `<your secret>` | OAuth client secret |
| `AzureAd__CallbackPath` | `/signin-oidc` | OAuth callback path |
| `AzureAd__SignedOutCallbackPath` | `/signout-callback-oidc` | Sign-out callback path |
| `AzureAd__RedirectUri` | `https://<app>.azurewebsites.net/signin-oidc` | Must match Azure AD app registration |
| `AzureBlobStorage__StorageConnection` | `DefaultEndpointsProtocol=https;AccountName=...` | Full blob connection string |
| `AzureBlobStorage__ContainerName` | `ebill-imports` | Blob container for file uploads |
| `AzureBlobStorage__StorageAccountName` | `unondataservices` | Storage account name |
| `AzureBlobStorage__StorageAccountKey` | `<your key>` | Storage access key |
| `DatabaseSchema` | `ebill` | Schema used by Hangfire and app tables |
| `EmailSettings__SmtpServer` | `smtp.gmail.com` | SMTP server for notifications |
| `EmailSettings__SmtpPort` | `587` | SMTP port (TLS) |
| `EmailSettings__FromEmail` | `<sender email>` | Sender email address |
| `EmailSettings__FromName` | `TAB System` | Sender display name |
| `EmailSettings__Username` | `<smtp username>` | SMTP authentication username |
| `EmailSettings__Password` | `<app password>` | Gmail app password |
| `EmailSettings__EnableSsl` | `true` | Enable TLS for SMTP |

Note: use double underscores (`__`) as section separators in environment variable names (e.g., `AzureAd__ClientSecret` maps to `AzureAd:ClientSecret` in appsettings).

### Connection Strings

In Azure Portal: **App Service > Settings > Environment variables > Connection strings**.

| Name | Value | Type |
|------|-------|------|
| `DefaultConnection` | `Data Source=unonsqlsvr01.database.windows.net;Initial Catalog=UNONDB01;User id=eBill_usr;Password=<password>;Encrypt=True;TrustServerCertificate=True;` | `SQLAzure` |

### General Settings

In Azure Portal: **App Service > Settings > Configuration > General settings**.

| Setting | Value | Why |
|---------|-------|-----|
| Stack | `.NET 8 (LTS)` | Target framework |
| Platform | `64-bit` | Required -- project targets `x64` for ExcelDataReader |
| Always On | **ON** | Required for Hangfire background jobs |
| HTTP version | `2.0` | Better performance |
| ARR affinity | `On` | Session stickiness for auth cookies |
| Min TLS version | `1.2` | Security baseline |
| HTTPS only | **ON** | Redirects all HTTP to HTTPS |

### Networking and Firewall

In Azure Portal: **App Service > Settings > Networking**.

#### Inbound Access Restrictions (who can reach the app)

**Option A: Intranet only (UN staff)**
```
Priority 100: Allow  -- UN Nairobi office IP range
Priority 200: Allow  -- VPN IP range
Default:      Deny   -- Everything else
```

**Option B: Public-facing (Azure AD handles auth)**
- Leave access restrictions open
- Consider Azure Front Door or WAF for DDoS protection

#### Outbound Connections (what the app connects to)

The app requires outbound access to these services (allowed by default):

| Service | Endpoint | Port |
|---------|----------|------|
| Azure SQL | `unonsqlsvr01.database.windows.net` | 1433 |
| Azure Blob Storage | `unondataservices.blob.core.windows.net` | 443 |
| Azure AD | `login.microsoftonline.com` | 443 |
| Gmail SMTP | `smtp.gmail.com` | 587 |

No changes needed unless VNet Integration is enabled.

### Azure SQL Firewall

In Azure Portal: **SQL Server (unonsqlsvr01) > Security > Networking**.

| Setting | Value | Why |
|---------|-------|-----|
| Allow Azure services and resources to access this server | **ON** | Lets App Service connect without IP whitelisting |
| Public network access | `Selected networks` | Restrict to Azure services only |

For tighter security: use VNet Integration + Private Endpoint (eliminates public SQL access entirely, requires Premium tier).

### Azure Blob Storage Firewall

In Azure Portal: **Storage Account (unondataservices) > Security + networking > Networking**.

| Setting | Value |
|---------|-------|
| Allow Azure services on the trusted services list | **ON** |
| Public network access | `Enabled from all networks` or `Selected virtual networks and IP addresses` |

### Azure AD App Registration

In Azure Portal: **Microsoft Entra ID > App registrations > your app > Authentication**.

| Setting | Value |
|---------|-------|
| Redirect URI (Web) | `https://<app>.azurewebsites.net/signin-oidc` |
| Front-channel logout URL | `https://<app>.azurewebsites.net/signout-callback-oidc` |
| ID tokens | Checked |
| Supported account types | Single tenant (your org only) |

### App Service Plan (Tier Selection)

| Tier | Price | Always On | Deployment Slots | VNet Integration | Recommendation |
|------|-------|-----------|------------------|------------------|----------------|
| B1 (Basic) | ~$55/mo | No | No | No | Dev/test only |
| **S1 (Standard)** | **~$73/mo** | **Yes** | **1 slot** | **No** | **Production minimum** |
| P1v3 (Premium) | ~$138/mo | Yes | 5 slots | Yes | If you need private SQL endpoints |

Standard S1 is the minimum because Hangfire requires Always On and deployment slots enable zero-downtime deployments.

### Settings Map (Visual)

```
Azure Portal
  |
  +-- App Service
  |     +-- Environment variables
  |     |     +-- App settings (19 settings above)
  |     |     +-- Connection strings (DefaultConnection = SQLAzure)
  |     +-- Configuration > General settings
  |     |     +-- .NET 8, 64-bit, Always On, HTTPS only
  |     +-- Networking
  |     |     +-- Inbound: Access restrictions (IP allowlist or open)
  |     |     +-- Outbound: Default allow all
  |     +-- TLS/SSL settings
  |     |     +-- Min TLS: 1.2, HTTPS only: ON
  |     |     +-- Custom domain + cert (optional)
  |     +-- Scale up: Standard S1 minimum
  |
  +-- SQL Server (unonsqlsvr01)
  |     +-- Networking: Allow Azure services = ON
  |
  +-- Storage Account (unondataservices)
  |     +-- Networking: Allow trusted Azure services = ON
  |
  +-- Microsoft Entra ID (App Registration)
        +-- Redirect URI: https://<app>.azurewebsites.net/signin-oidc
        +-- Logout URI: https://<app>.azurewebsites.net/signout-callback-oidc
```

---

## Migration Path (On-Prem to App Service)

If you decide to move later, the migration is straightforward:

1. Create App Service (Standard S1 tier minimum for Always On)
2. Set environment variables in App Service Configuration
3. Update Azure AD redirect URIs to the new `*.azurewebsites.net` domain
4. Deploy via `az webapp deploy` or GitHub Actions
5. Test with deployment slot before swapping to production
6. Update DNS if using a custom domain
7. Decommission the VM

The application code requires **zero changes** -- it already supports both environments.

---

## Related Docs
- [IIS Deployment Guide](./IIS_DEPLOYMENT_GUIDE.md) -- detailed on-prem setup
- [Azure Deployment Guide](./AZURE_DEPLOYMENT_GUIDE.md) -- App Service setup
