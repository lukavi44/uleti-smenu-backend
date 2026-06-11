# Staging deployment guide (no Docker)

Deploy UletiSmenu to a **staging** environment for testing before production.  
Payments stay **disabled** (`Stripe:Enabled=false`) until you add CorvusPay or Stripe later.

---

## Architecture (staging)

```
Browser → https://staging.uletismenu.com     (React static files)
       → https://api-staging.uletismenu.com (ASP.NET Core API)
       → SQL Server (Azure SQL or your server)
       → Upload folder on disk (profile photos, etc.)
```

Replace hostnames with your real staging URLs.

---

## 1. Prerequisites

- .NET 8 SDK on your machine (for publishing)
- Node.js 20+ (for frontend build)
- **SQL Server** database (Azure SQL Database or SQL Server on a VPS)
- Hosting for API (pick one):
  - **Azure App Service** (Windows or Linux) — recommended if new to deploy
  - **IIS on Windows Server** — if you already have a VPS
- Hosting for frontend (pick one):
  - **Azure Static Web Apps**
  - **Netlify / Cloudflare Pages**
  - **Same server as API** — IIS/nginx serving `dist/` folder

---

## 2. Database

1. Create database `UletiSmenuDb_Staging` on your SQL Server.
2. Note connection string (store in hosting secrets, not git):

```
Server=tcp:YOUR_SERVER.database.windows.net,1433;Database=UletiSmenuDb_Staging;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=False;
```

3. Migrations run automatically on API startup (`EnsureDatabaseMigratedAsync`).

---

## 3. Publish the API

From repo root:

```powershell
cd D:\repos\UletiSmenu\uleti-smenu-backend-git\UletiSmenu
.\scripts\publish-api.ps1
```

Output: `.\publish\api\` — zip this folder for upload or use `az webapp deploy`.

### Environment variables on the host

Set these in Azure **Configuration → Application settings** or IIS environment:

| Name | Example |
|------|---------|
| `ASPNETCORE_ENVIRONMENT` | `Staging` |
| `ConnectionStrings__UletiSmenu` | (full connection string) |
| `Cors__AllowedOrigins__0` | `https://staging.uletismenu.com` |
| `FileSettings__UploadPath` | `D:\inetpub\uploads` or `/home/uploads` |
| `SmtpSettings__Username` | (when ready) |
| `SmtpSettings__Password` | (secret) |
| `Stripe__Enabled` | `false` |

`appsettings.Staging.json` provides non-secret defaults; **secrets override via env vars**.

### Azure App Service (quick path)

1. Create **Web App** (.NET 8, Windows or Linux).
2. **Configuration** → add settings above.
3. Deploy zip:

```powershell
az webapp deploy --resource-group YOUR_RG --name YOUR_API_APP --src-path .\publish\api.zip --type zip
```

4. Enable **HTTPS only**. Note URL → set as `VITE_API_BASE_URL` for frontend build.

### IIS (Windows VPS)

1. Install [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Create site pointing to `publish\api` folder.
3. Create app pool: **No Managed Code**.
4. Create upload folder; grant `IIS_IUSRS` modify rights.
5. Bind HTTPS certificate.

---

## 4. Build & deploy frontend

```powershell
cd D:\repos\UletiSmenu\uleti-smenu\uleti-smenu_react\uleti-smenu

# Create .env.staging locally (copy from .env.staging.example)
# VITE_API_BASE_URL=https://api-staging.uletismenu.com

npm ci
npm run build
```

Upload contents of `dist/` to static hosting.

**Important:** `VITE_API_BASE_URL` is baked in at build time — rebuild when API URL changes.

---

## 5. CORS check

API `Cors:AllowedOrigins` must **exactly** match frontend origin:

- `https://staging.uletismenu.com` (no trailing slash)
- Include `http://localhost:5173` only if you still test locally against staging API

---

## 6. Smoke test checklist

- [ ] `GET https://api-staging.../swagger` (disable in prod later)
- [ ] Register employee + employer
- [ ] Employer gets 90-day trial; can create job post
- [ ] Employee can apply; employer can accept
- [ ] Chat opens after accept
- [ ] Profile photo upload works (upload path writable)
- [ ] `/billing/upgrade` shows plans; checkout **disabled** (expected)
- [ ] SignalR notifications (if used) — check browser console

---

## 7. Payments (current status)

- **Stripe:** not available for Serbia-only businesses — leave disabled.
- **Manual upgrades:** `support@uletismenu.com` until CorvusPay or foreign-entity Stripe.
- Billing gate and trial still work without a payment provider.

---

## 8. Scripts in this repo

| Script | Purpose |
|--------|---------|
| `scripts/publish-api.ps1` | Release publish to `publish/api` |
| `scripts/set-stripe-dev-secrets.ps1` | Optional; only if Stripe test account exists |

Workspace root (not in git): `D:\repos\UletiSmenu\run-dev.ps1` — starts API + frontend locally.

---

## Next: production

When staging is stable, copy approach to production using `appsettings.Production.json` and `PRODUCTION_CONFIG.md`.
