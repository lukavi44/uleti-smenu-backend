# UletiSmenu environments

Two environments: **LIVE** (real users) and **TEST** (development and QA).

See also: [`ROADMAP.md`](./ROADMAP.md) (planning source of truth).

## Naming glossary

| Say / write | Meaning |
|-------------|---------|
| **LIVE** / **PROD** / **Production** | Production traffic |
| **TEST** | Pre-release / QA |
| **Development** | Local only |

| Legacy label in Azure / GitHub | Treat as |
|--------------------------------|----------|
| `rg-uletismenu-staging` | LIVE resource group (name not changed yet) |
| `api-staging-uletismenu` | LIVE App Service |
| `UletiSmenuDb_Staging` | LIVE SQL database |
| Workflow `main_api-staging-uletismenu.yml` | Deploys **LIVE** API from `main` |
| `ASPNETCORE_ENVIRONMENT=Staging` (Render) | **TEST** (ASP.NET Core env name only) |
| `appsettings.Staging.json` | Loaded when env is `Staging` = TEST |

Do **not** rename Azure resources, DNS, or workflow file names without a planned cutover — those renames break deploys until everything is updated together.

Prefer scripts/docs: `configure-azure-live.ps1` (not the deprecated `configure-azure-staging.ps1`).

## Matrix

| | LIVE | TEST |
|---|------|------|
| **Purpose** | Production traffic | Pre-release validation |
| **Frontend** | `https://app.uletismenu.com` | `https://test.app.uletismenu.com` |
| **API** | `https://api.uletismenu.com` | `https://api-test.uletismenu.com` |
| **API host** | Azure App Service B1 (`api-staging-uletismenu`) | Render free (`uletismenu-api-test`) |
| **Frontend host** | Cloudflare Pages (`main` branch) | Cloudflare Pages (`develop` branch) |
| **Database** | `UletiSmenuDb_Staging` on Azure SQL | `UletiSmenuDb_Test` on same SQL server |
| **ASPNETCORE_ENVIRONMENT** | `Production` | `Staging` (= TEST) |
| **Uploads** | Azure Blob (`FileSettings:Provider=AzureBlob`) | Local disk on Render (ephemeral) |
| **Swagger** | Disabled | Enabled |
| **AdminSeed** | Disabled | Enabled (TEST only) |
| **Deploy trigger** | Push to `main` (GitHub Actions) | Render auto-deploy + Pages `develop` |

## LIVE setup checklist

1. Azure App Service on B1 (already provisioned).
2. Run `UletiSmenu/scripts/configure-azure-live.ps1` with Blob + SMTP secrets.
3. Create Azure Storage account + `uploads` container (private).
4. Set `FileSettings__BlobConnectionString` in App Service settings.
5. Cloudflare Pages: `main` → `app.uletismenu.com`, build env `VITE_API_BASE_URL=https://api.uletismenu.com`.
6. DNS: `api` CNAME → Azure (grey cloud for managed cert), `app` CNAME → Pages.
7. Run `scripts/verify-live-database.sql` against LIVE DB.
8. Monitor `/health` and `/health/ready`.

## TEST setup checklist

1. Run `UletiSmenu/scripts/provision-test-database.ps1 -SqlServerName <your-server>`.
2. Render: New Blueprint → `render-test.yaml` (backend repo).
3. Set `ConnectionStrings__UletiSmenu` in Render TEST service to `UletiSmenuDb_Test`.
4. Cloudflare Pages: `develop` → `test.app.uletismenu.com`.
5. DNS: `api-test` CNAME → Render TEST API hostname; `test.app` CNAME → Pages.
6. Remember: Render Free blocks outbound SMTP (25/465/587) — verify email on LIVE or local.

## Branch strategy

```
main     → LIVE (Azure API + Cloudflare Pages production)
develop  → TEST  (Render API + Cloudflare Pages preview)
feature/* → local / PR into develop
```

## Cost notes

- LIVE: Azure App Service B1 + SQL.
- TEST: Render free + shared SQL server (extra DB only).
- Blob storage: required for LIVE upload durability.

## Legacy resources to retire (when ready)

- Render `uletismenu-api-staging` / `uletismenu-web-staging` — shut down if TEST (`*-test`) is the active path.
- Azure Static Web App from initial staging script — unused if Cloudflare Pages is LIVE frontend.
- Optional future: rename Azure `*staging*` resources to `*live*` / `*prod*` (manual, coordinated).
