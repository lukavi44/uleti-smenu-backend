# Production hardening sprint

Checklist for LIVE (`api.uletismenu.com` / `app.uletismenu.com`).

## 1. Durable uploads (Azure Blob)

Local disk on App Service is lost on restart/scale. LIVE must use Blob storage.

### Azure Portal steps

1. Create Storage account (e.g. `uletismenulive`) in the same region as App Service.
2. Create private container `uploads`.
3. Copy connection string (Access keys).
4. Apply via script:

```powershell
cd D:\repos\UletiSmenu\uleti-smenu-backend-git\UletiSmenu\scripts
.\configure-azure-live.ps1 `
  -BlobConnectionString "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
```

5. Smoke test: upload profile photo → restart App Service → photo still loads.

## 2. SMTP (SendGrid)

Required for password reset. Email confirmation stays off until mail is verified.

### SendGrid setup

1. Create Zoho Mail app password for `support@uletismenu.com`.
2. Confirm `noreply@uletismenu.com` can send (alias of the primary mailbox).
3. Apply:

```powershell
.\configure-azure-live.ps1 -SmtpPassword "your-zoho-app-password"
```

See [email-setup.md](./email-setup.md) for full SMTP and rotation steps.

4. Test: forgot-password from LIVE, or `POST /api/v1/debug/test-email` on TEST/Staging.

### App settings reference

| Setting | Value |
|---------|-------|
| `SmtpSettings__Host` | `smtppro.zoho.eu` |
| `SmtpSettings__Port` | `587` |
| `SmtpSettings__Username` | `support@uletismenu.com` |
| `SmtpSettings__Password` | Zoho app password |
| `SmtpSettings__FromEmail` | `noreply@uletismenu.com` |
| `SmtpSettings__EnableSsl` | `true` |

After mail works end-to-end, consider a soft email-confirm banner (do **not** block login yet — see Roadmap Phase 1).

## 3. Monitoring

Minimum for launch — see also [`PRODUCTION_SMOKE.md`](./PRODUCTION_SMOKE.md):

| Check | URL / tool |
|-------|------------|
| Liveness | `https://api.uletismenu.com/health` — UptimeRobot (or equivalent) |
| Readiness | `https://api.uletismenu.com/health/ready` — optional; **Unhealthy while Azure SQL is paused is expected** |
| Frontend | `https://app.uletismenu.com` — UptimeRobot |
| Azure | `UletiSmenu/scripts/configure-azure-monitoring.ps1` → App Insights + Http5xx alert → `support@uletismenu.com` |
| SMTP failures | App Insights log alert on `Failed to send email` / `SMTP network connectivity failed` |

Alert on: 2 consecutive uptime failures; Http5xx metric spike; SMTP log threshold (≥3 / 30 min).

```powershell
cd UletiSmenu\scripts
.\configure-azure-monitoring.ps1
.\verify-live-smoke.ps1
```

## 4. Database verification + Production SQL review

Run against LIVE database in Azure Data Studio (DB must be **awake** — not paused):

```
UletiSmenu/scripts/verify-live-database.sql
```

Confirm Phase 3 unique index and no `NumberOfApplicants` column. `AspNetUsers.DeletedAtUtc` shows **WARN/SKIP** on LIVE until the account-deletion migration is deployed (`AddUserDeletedAtUtc` on `develop` → LIVE).

### Production SQL review checklist

| # | Topic | Current posture / action |
|---|--------|--------------------------|
| S1 | Tier | **GP_S_Gen5_2** (General Purpose **Serverless**), max 2 vCores, ~32 GB cap — LIVE DB `UletiSmenuDb_Staging` on `uletismenu-staging-sql` (2026-08-11). |
| S2 | Auto-pause / cold start | **autoPauseDelay = 60 min**, minCapacity 0.5 — `/health/ready` may be Unhealthy while paused; first request 30–60s. **Pilot decision: accept pause.** Revisit before marketing. |
| S3 | Backup / PITR | **STR 7 days**, diff backups every 12h. Point-in-time restore within 7-day window. Long-term retention (LTR) not configured. |
| S4 | Firewall | `AllowAllWindowsAzureIps` (Azure services) + **`AllowExternalStaging` 0.0.0.0–255.255.255.255** — **restrict to admin IPs before marketing.** |
| S5 | Connection string | `ConnectionStrings__UletiSmenu` present on App Service (value not in git). |
| S6 | Migration history | Run `verify-live-database.sql` in Azure Data Studio when DB awake; `DeletedAtUtc` WARN until account-deletion migration on LIVE. |

Automated posture check (no secrets printed):

```powershell
cd UletiSmenu\scripts
.\verify-live-sql-review.ps1
```

### Open decisions before marketing

- Accept SQL auto-pause for **pilot** vs disable pause / upgrade tier before marketing traffic. **Pilot: accept pause (2026-08-11).**
- Tighten firewall — remove internet-wide rule before marketing.
- Confirm backup/PITR meets counsel retention expectations (see also [`ACCOUNT_DELETION_RETENTION.md`](./ACCOUNT_DELETION_RETENTION.md) — deleted PII may linger in backups).

### SQL review outcome template

| Field | Value |
|-------|-------|
| Date | 2026-08-11 |
| Engineer | Luka |
| Tier / pause policy | GP_S_Gen5_2 serverless; auto-pause 60 min — **accept for pilot** |
| Backup/PITR notes | STR 7 days; diff every 12h; no LTR |
| `verify-live-database.sql` | Pending manual run in Azure Data Studio (DB awake; `/health/ready` Healthy) |
| Decision for pilot | **Accept pause**; restrict firewall before marketing |

## 5. Security headers

Implemented in `SecurityHeadersMiddleware` (API). Verify:

```bash
curl -i -X GET https://api.uletismenu.com/health
# or (also 200 after HEAD support ships to LIVE):
curl -I https://api.uletismenu.com/health
```

Expect: `200` (GET includes `{"status":"ok"}`), plus `X-Content-Type-Options`, `X-Frame-Options`, `Strict-Transport-Security` (Production).

## 6. LIVE App Service settings audit

Run in Azure Portal → App Service → Configuration:

| Setting | Expected |
|---------|----------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Proxy__Provider` | `None` |
| `AdminSeed__Enabled` | `false` |
| `FileSettings__Provider` | `AzureBlob` |
| `Cors__AllowedOrigins__0` | `https://app.uletismenu.com` |
| `Stripe__Enabled` | `false` |

## 7. SQL auto-pause

Free/serverless SQL pauses when idle. First request after idle may take 30–60s. Liveness `/health` should stay OK; readiness `/health/ready` may be Unhealthy while paused.

Options:

- **Pilot phase:** accept cold start; monitor `/health/ready` timeouts; wake DB with a real API call before smoke runs.
- **Before marketing:** upgrade to Basic (or higher) tier **or** disable auto-pause — explicit product decision (see §4 open decisions).

## 8. Security smoke test (manual)

After deploy:

- [ ] `GET /api/v1/User/users` without token → 401/403
- [ ] Public job listing has no employer phone/email
- [ ] Rapid login attempts → 429 after threshold
- [ ] Upload >5 MB image → 400
- [ ] Swagger not available on LIVE (`/swagger` → 404)

## 9. CI gate

Backend GitHub Actions runs `dotnet test` before LIVE deploy (see `.github/workflows/`).

## 10. Rollback

- API: redeploy previous GitHub Actions artifact or git revert + push `main`.
- DB: Azure point-in-time restore (requires paid SQL tier) or pre-migration backup.
- Blob: container versioning optional; uploads are non-critical for rollback.
