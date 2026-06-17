# Staging on Azure

Deploy UletiSmenu staging at **https://staging.uletismenu.com** with API at **https://api-staging.uletismenu.com**.

Payments stay **disabled** (`Stripe:Enabled=false`) until CorvusPay or Stripe is configured.

### Azure names (defaults)

| Resource | Name |
|----------|------|
| Resource group | `rg-uletismenu-staging` |
| API App Service | `api-staging-uletismenu` |
| Custom API host | `api-staging.uletismenu.com` |
| Custom web host | `staging.uletismenu.com` |
| SQL database | `UletiSmenuDb_Staging` |

---

## Architecture

```
Browser → https://staging.uletismenu.com      (Azure Static Web App — React)
       → https://api-staging.uletismenu.com  (Azure App Service — .NET 8 API)
       → Azure SQL Database (UletiSmenuDb_Staging)
       → /home/uploads on App Service (profile photos)
```

### Why Azure SQL?

For App Service hosting, **Azure SQL Database** is the right default:

| | Azure SQL | SQL on a VPS |
|---|-----------|--------------|
| Backups | Automatic | You manage |
| Firewall | Per-server rules + “Allow Azure services” | Manual |
| Scaling | Change tier in portal | Resize VM |
| Connection | Standard encrypted connection string | Same, but you maintain server |

Use a **Basic** tier database for staging (~few EUR/month). Production can use S0 or higher.

---

## 1. Prerequisites

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) — `az login`
- .NET 8 SDK (publish API locally)
- Node.js 20+ (build frontend)
- DNS access for `uletismenu.com` (CNAME records)

---

## 2. One-time: provision Azure resources

From the backend repo:

```powershell
cd D:\repos\UletiSmenu\uleti-smenu-backend-git\UletiSmenu
az login
.\scripts\provision-azure-staging.ps1 -NamePrefix uletismenu-stg
```

`NamePrefix` must be **globally unique** (used in SQL server and app names). If taken, try `uletismenu-stg2`.

This creates:

- Resource group `rg-uletismenu-staging` (West Europe by default)
- Azure SQL server + database `UletiSmenuDb_Staging`
- Linux App Service plan (B1) + API web app **`api-staging-uletismenu`**
- Static Web App for the React build
- App settings: `Staging` environment, connection string, CORS, upload path

Output is saved to `scripts/azure-staging-output.json` (gitignored — add to `.gitignore` if needed).

**Save the SQL admin password** printed during provisioning.

### Optional: allow your IP for SSMS

```powershell
az sql server firewall-rule create `
  --resource-group rg-uletismenu-staging `
  --server YOUR_SQL_SERVER_NAME `
  --name MyDevMachine `
  --start-ip-address YOUR_PUBLIC_IP `
  --end-ip-address YOUR_PUBLIC_IP
```

Migrations run automatically on API startup.

---

## 3. Custom domains & HTTPS

### API (`api-staging.uletismenu.com`)

1. Azure Portal → App Service → **Custom domains** → Add `api-staging.uletismenu.com`
2. DNS: **CNAME** `api-staging` → `api-staging-uletismenu.azurewebsites.net`
3. **TLS/SSL** → Create **App Service Managed Certificate** (free) → bind to the hostname

### Frontend (`staging.uletismenu.com`)

1. Azure Portal → Static Web App → **Custom domains** → Add `staging.uletismenu.com`
2. Follow the wizard (CNAME or TXT validation)
3. Managed certificate is issued automatically for SWA custom domains

---

## 4. Configure & deploy the API

If the app was created manually in the portal, fix settings first (connection string name must be `ConnectionStrings__UletiSmenu`, not `DefaultConnection`):

```powershell
cd D:\repos\UletiSmenu\uleti-smenu-backend-git\UletiSmenu
.\scripts\configure-azure-staging.ps1
.\scripts\deploy-azure-staging.ps1
```

Defaults: resource group `rg-uletismenu-staging`, API app **`api-staging-uletismenu`**  
(URL: `https://api-staging-uletismenu.azurewebsites.net` until custom domain is bound).

Smoke test: `https://api-staging.uletismenu.com/swagger` (disable Swagger in production later).

---

## 5. Build & deploy frontend

```powershell
cd D:\repos\UletiSmenu\uleti-smenu\uleti-smenu_react\uleti-smenu
.\scripts\build-staging.ps1
```

`VITE_API_BASE_URL` must be `https://api-staging.uletismenu.com` (set in `.env.staging`).

### Deploy `dist/` to Static Web App

**Option A — SWA CLI (quick manual deploy):**

```powershell
npm install -g @azure/static-web-apps-cli
swa deploy ./dist --env production --deployment-token YOUR_SWA_DEPLOYMENT_TOKEN
```

Token: Static Web App → **Manage deployment token** in Azure Portal.

**Option B — GitHub Actions:** connect the frontend repo to the SWA in Azure Portal for CI deploys on push.

---

## 6. App settings reference

Set in Azure Portal → App Service → **Configuration** (provision script sets most of these):

| Name | Value |
|------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Staging` |
| `ConnectionStrings__UletiSmenu` | Azure SQL connection string |
| `Cors__AllowedOrigins__0` | `https://staging.uletismenu.com` |
| `Cors__AllowedOrigins__1` | `http://localhost:5173` (optional, local dev against staging API) |
| `FileSettings__UploadPath` | `/home/uploads` |
| `Stripe__Enabled` | `false` |
| `SmtpSettings__Username` | (when ready) |
| `SmtpSettings__Password` | (secret) |

Non-secret defaults live in `appsettings.Staging.json`; **secrets override via app settings**.

---

## 7. Smoke test checklist

- [ ] `GET https://api-staging.uletismenu.com/swagger`
- [ ] Register employee + employer
- [ ] Employer trial; create job post
- [ ] Employee apply; employer accept
- [ ] Chat after accept
- [ ] Profile photo upload (`/home/uploads` writable on App Service)
- [ ] `/billing/upgrade` — plans visible; checkout disabled (expected)
- [ ] SignalR notifications — check browser console

---

## 8. Payments (current)

- **Stripe:** not for Serbia-only businesses — leave disabled.
- **Manual upgrades:** `support@uletismenu.com` until CorvusPay or foreign-entity Stripe.
- Trial and billing gates work without a payment provider.

---

## 9. Scripts

| Script | Repo | Purpose |
|--------|------|---------|
| `scripts/provision-azure-staging.ps1` | backend | One-time Azure resources |
| `scripts/configure-azure-staging.ps1` | backend | App settings + start web app |
| `scripts/provision-render-staging.ps1` | backend | Create Render API + static site |
| `scripts/deploy-azure-staging.ps1` | backend | Publish + zip deploy API |
| `scripts/publish-api.ps1` | backend | Release build only |
| `scripts/build-staging.ps1` | frontend | Staging `npm run build` |

---

## Next: production

When staging is stable, mirror with `appsettings.Production.json` and `PRODUCTION_CONFIG.md` (separate SQL database, production hostnames, Swagger off).
