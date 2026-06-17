# Staging on Render

Deploy UletiSmenu staging to **Render** workspace **UletiSmenu**.

| Service | Name | URL |
|---------|------|-----|
| API (Docker / .NET 8) | `uletismenu-api-staging` | https://uletismenu-api-staging.onrender.com |
| Frontend (static / Vite) | `uletismenu-web-staging` | https://uletismenu-web-staging.onrender.com |

Database stays on **Azure SQL** (`UletiSmenuDb_Staging`) — Render does not host SQL Server.

---

## 1. Push config to GitHub

Both repos need `render.yaml` on `main`:

- Backend: `render.yaml` + `UletiSmenu/Dockerfile`
- Frontend: `render.yaml` (rootDir `uleti-smenu`)

---

## 2. Create services

**Option A — Blueprint (recommended)**

1. [Render Dashboard](https://dashboard.render.com) → workspace **UletiSmenu**
2. **New** → **Blueprint**
3. Connect `lukavi44/uleti-smenu-backend` → Blueprint path: `render.yaml`
4. Repeat for `lukavi44/uleti-smenu_react`

**Option B — Script**

```powershell
cd D:\repos\UletiSmenu\uleti-smenu-backend-git\UletiSmenu
$env:RENDER_API_KEY = "rnd_..."
.\scripts\provision-render-staging.ps1
```

---

## 3. API environment variables

In Render → **uletismenu-api-staging** → **Environment**:

| Key | Value |
|-----|--------|
| `ConnectionStrings__UletiSmenu` | Azure SQL connection string |
| `ASPNETCORE_ENVIRONMENT` | `Staging` (set by blueprint) |
| `Cors__AllowedOrigins__0` | `https://uletismenu-web-staging.onrender.com` |
| `Stripe__Enabled` | `false` |

---

## 4. Azure SQL firewall

**Render is not an Azure service** — `Allow Azure services` alone is not enough.

**Staging (quick):** allow external connections on the SQL server:

```powershell
az sql server firewall-rule create `
  --resource-group rg-uletismenu-staging `
  --server uletismenu-staging-sql `
  --name AllowExternalStaging `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 255.255.255.255
```

**Tighter (production):** Render Dashboard → API service → **Connect** → **Outbound** tab → add those CIDR ranges as firewall rules (Frankfurt region).

Error **40613** on startup often means firewall blocked or database waking from pause — the API retries migrations automatically.

---

## 5. Smoke test

- [ ] https://uletismenu-api-staging.onrender.com/health
- [ ] https://uletismenu-api-staging.onrender.com/swagger
- [ ] https://uletismenu-web-staging.onrender.com loads
- [ ] Register / login works (CORS + API URL)

**Note:** Free tier web services **spin down** after inactivity (~50s cold start).

---

## Custom domains later

Point `api-staging.uletismenu.com` / `staging.uletismenu.com` via CNAME when on a paid plan (free tier has no custom domains on web services).

See also: `STAGING_DEPLOY.md` (Azure path), `.cursor/RENDER_PLUGIN.md` (Cursor MCP).
