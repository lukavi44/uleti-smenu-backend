# LIVE production smoke tests

Run after deploys to `main` / Azure LIVE (`https://api.uletismenu.com`, `https://app.uletismenu.com`).  
Planning context: [`ROADMAP.md`](./ROADMAP.md) Phase 1.

Automated checks:

```powershell
cd UletiSmenu\scripts
.\verify-live-smoke.ps1
```

## A. Infrastructure

| # | Check | Expected |
|---|--------|----------|
| A1 | `GET https://api.uletismenu.com/health` | `200` + `{"status":"ok"}` |
| A2 | `GET https://api.uletismenu.com/health/ready` | Prefer `Healthy`. **`Unhealthy`/`503` is expected when Azure SQL is paused** (serverless). First real request wakes DB; re-check after ~1–2 min. |
| A3 | `GET https://api.uletismenu.com/swagger` | Disabled / not available in Production |
| A4 | `POST https://api.uletismenu.com/api/v1/debug/test-email` | **`404`** (never available on LIVE) |

## B. Email flows (manual — Zoho)

Use a real inbox you control. Inspect headers: **From** `noreply@uletismenu.com`, **Reply-To** `support@uletismenu.com`.

| # | Flow | How | Pass? |
|---|------|-----|-------|
| B1 | Password reset | LIVE `/forgot-password` | Done |
| B2 | Registration confirm | Register new employer or candidate | Done |
| B3 | Welcome employer | Same as B2 (employer) | Done |
| B4 | Welcome candidate | Register candidate | Done |
| B5 | Favourite job alert | Candidate favourites restaurant → employer posts job | Done |
| B6 | Contact form | `/kontakt` → message arrives at `support@` | Done |

Verified on LIVE **2026-07-30**. B5 note: one bounce to a non-existent `@uletismenu.com` mailbox is expected (invalid recipient), not an SMTP failure.

## C. Config (no secrets in tickets)

Confirm App Settings **names** exist (Portal or `az` with secret values redacted):

- `SmtpSettings__Host`, `Port`, `EnableSsl`, `Username`, `Password`
- `SmtpSettings__FromEmail`, `ReplyToEmail`, `FromName`, `ContactInbox`
- `ASPNETCORE_ENVIRONMENT=Production`
- Do **not** set `SmtpSettings__DebugApiKey` on LIVE

Optional: Application Insights linked (`APPLICATIONINSIGHTS_CONNECTION_STRING`).

## D. Monitoring

| # | Check | Notes |
|---|--------|-------|
| D1 | Azure alerts provisioned | Done — `uletismenu-live-http5xx` + action group `ag-uletismenu-live` (2026-07-30) |
| D2 | Action group emails `support@uletismenu.com` | Done — confirmed 2026-07-30 |
| D3 | External uptime (UptimeRobot or equivalent) | Done — `api.uletismenu.com/health` (+ optional `app.uletismenu.com`) every 5 min → `support@` (2026-07-30) |
| D4 | Log phrases for SMTP | Done — App Insights `appi-uletismenu-live` linked; scheduled query `uletismenu-live-smtp-failures` → `ag-uletismenu-live` |

## E. Known LIVE caveats

1. **SQL auto-pause** — `/health/ready` Unhealthy while paused is normal; liveness `/health` should still be OK.
2. **Render Free SMTP** — irrelevant to LIVE; do not use TEST to prove Zoho delivery.
3. **Rotate SMTP app password** if it was ever printed by `az webapp config appsettings list` (values are visible to subscription admins).

## F. Azure Blob / durable uploads

App Service local disk is ephemeral. LIVE must use Blob (`FileSettings__Provider=AzureBlob`).

Automated / Portal name check (no secret values printed):

```powershell
cd UletiSmenu\scripts
.\verify-live-blob-smoke.ps1 -CheckAppSettings
```

| # | Check | Expected | Pass? |
|---|--------|----------|-------|
| F1 | App Settings | `FileSettings__Provider=AzureBlob`; `FileSettings__BlobConnectionString` + `BlobContainerName` present (redact values in tickets) | Pass (2026-08-11, `verify-live-blob-smoke.ps1 -CheckAppSettings`) |
| F2 | Upload | Log in on LIVE → upload profile photo → image loads via `/uploads/...` (or Blob-backed URL) | Pass (2026-08-11) |
| F3 | Survive restart | Restart LIVE App Service → same photo URL still loads | Pass (2026-08-11, manual restart `api-staging-uletismenu`) |
| F4 | Optional replace/delete | Replace photo works; after account deletion, old blob removed best-effort (check logs if missing) | Skipped (pilot) |

### F outcome template

| Field | Value |
|-------|-------|
| Date | 2026-08-11 |
| Engineer | Luka |
| F1–F4 | F1 Pass · F2 Pass · F3 Pass · F4 Skipped |
| Notes | F1 via scripted App Settings check; F2 profile photo upload on LIVE; F3 photo survived App Service restart |

## G. Production SQL review (pointer)

Run `UletiSmenu/scripts/verify-live-database.sql` when the LIVE DB is awake.  
Checklist (tier, auto-pause, backup/PITR, firewall, open decisions): [`PRODUCTION_HARDENING.md`](./PRODUCTION_HARDENING.md) §4 / §7.

## Sign-off

| Role | Date | Notes |
|------|------|-------|
| Engineer | 2026-07-30 | A1–A4, B1–B6, D1–D4 monitoring verified |
| Engineer | 2026-08-11 | Section F Blob smoke F1–F3 Pass (F4 skipped) |
| Product | | |
