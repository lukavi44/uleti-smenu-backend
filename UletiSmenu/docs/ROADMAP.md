# UletiSmenu roadmap

**Status:** active · **Last updated:** 2026-07-30 (B1–B6 LIVE email smoke verified)  
**Owner:** product / engineering (keep this file current)

This is the **single source of truth** for planning. Before proposing or implementing work:

1. Read this file.
2. Confirm the work matches the current phase (or Soft Launch checklist).
3. If it does not align, **stop and ask** before proceeding.

When a milestone finishes, update the status tables here in the same PR/commit.

Related environment reference: [`ENVIRONMENTS.md`](./ENVIRONMENTS.md) (workspace) · backend `docs/ENVIRONMENTS.md` / email: `docs/email-setup.md`.

---

## Project vision

The goal is **not** to endlessly add features.

The goal is to launch a **production-ready hospitality hiring platform in Serbia** as soon as possible, with high quality.

Evaluate every proposal against that goal. **Avoid feature creep.**

---

## Branch & environment policy

| Git branch | Deploy target | Public name |
|------------|---------------|-------------|
| `develop` | TEST first | TEST |
| `main` | LIVE after PR | LIVE / Production / PROD |

**Workflow:** implement and verify on **`develop` → TEST**, then PR to **`main` → LIVE**.  
Do not ship brand-new work straight to `main` unless explicitly requested.

**Naming:** Prefer LIVE / PROD / Production and TEST in docs and conversation.  
Many Azure resource names still contain `staging` — those are **LIVE** (legacy names). Do not rename Azure resources without a planned migration.  
Render TEST uses `ASPNETCORE_ENVIRONMENT=Staging` (ASP.NET Core environment name only — it means **TEST**, not a third env).

---

## Phase overview

| Phase | Name | Priority | Status |
|-------|------|----------|--------|
| 1 | Production stabilization | Highest | **In progress** |
| 2 | Administration | Highest development | Not started |
| 3 | SEO & analytics | Next | Not started |
| 4 | Payments (Stripe) | After SEO | Partial code exists; not production |
| 5 | Auth improvements (OAuth) | Only after 1–4 | Not started |

---

## Phase 1 — Production stabilization (highest priority)

### Email system

| Item | Status | Notes |
|------|--------|--------|
| Zoho SMTP integration (MailKit) | Done | Auth `support@`, From `noreply@`, Reply-To `support@` |
| Email templates + workflows | Done | Confirm, welcome, reset, favourite, contact |
| Contact API + `/kontakt` frontend | Done | |
| Azure LIVE SMTP App Settings | Done | Host/user/from present; rotate password if ever printed via `az` |
| Verify all email flows on LIVE | Done | B1–B6 verified LIVE 2026-07-30 (`PRODUCTION_SMOKE.md`) |
| SMTP monitoring / ops clarity | **In progress** | Run `configure-azure-monitoring.ps1` (D1–D2 pending) |
| Documented smoke tests | Done | `docs/PRODUCTION_SMOKE.md`, `scripts/verify-live-smoke.ps1` |
| Confirm production readiness | **In progress** | Email flows done; Azure alerts + uptime pending |
| Notification preferences (email + in-app) | Done on TEST | Ship to LIVE with `develop` → `main` PR |

**Do not** block login on unconfirmed email until SMTP/monitoring are solid (prefer soft banner later if needed).

### Production infrastructure

| Item | Status |
|------|--------|
| `/health` + `/health/ready` | `/health` OK LIVE; `/health/ready` Unhealthy when SQL **Paused** (documented) |
| Azure monitoring | **In progress** — run `configure-azure-monitoring.ps1` |
| Azure alerts | **Pending** — Http5xx + action group → `support@`; Portal log alert for SMTP |
| Logging review | Serilog console → App Service / Insights once linked |
| Uptime monitoring | Documented (UptimeRobot on `/health`) |
| Documented smoke tests | Done (`PRODUCTION_SMOKE.md`) |

### Production deployment

| Item | Status |
|------|--------|
| DB migration state LIVE | Verify |
| Production configuration | Verify (`configure-azure-live.ps1`) |
| Production secrets (no secrets in git) | Ongoing discipline |
| Render Free SMTP limitation | Known — not a LIVE blocker |

---

## Phase 2 — Administration

Highest **development** priority after Phase 1 is stable.

Design and implement a complete **Admin Panel** that can scale:

- dashboard  
- users, employers, candidates  
- jobs, applications  
- moderation, reports, analytics  
- contact messages  
- reviews  
- audit logs  
- feature flags  
- system settings  

Some admin surfaces may already exist in prototype form — Phase 2 means **production-grade** admin, not one-off screens.

---

## Phase 3 — SEO & analytics

Production-grade SEO and measurement:

- metadata, sitemap, robots.txt  
- structured data  
- Open Graph, Twitter Cards  
- canonical URLs  
- Google Search Console  
- Google Analytics 4  
- Microsoft Clarity  
- conversion tracking  
- performance monitoring  

---

## Phase 4 — Payments

Integrate **Stripe** at production quality:

- wallet top-ups  
- subscriptions  
- invoices  
- payment history  
- webhook processing  
- failed payments and retries  

Partial scaffolding may exist; do not treat it as done until production-ready.

---

## Phase 5 — Authentication improvements

**Only after Phases 1–4.**

- Google OAuth  
- Facebook OAuth (optional if still valuable)  

Do **not** start OAuth during stabilization.

---

## Features intentionally NOT planned

Do **not** propose these unless explicitly requested:

- CV upload  
- Resume builder  
- Document storage  
- Unnecessary social features  

Candidate profile replaces the traditional CV.

---

## Soft launch checklist

Before public launch, complete:

- [ ] Lawyer-approved legal texts  
- [x] SMTP verified (all core flows) — B1–B6 LIVE 2026-07-30  
- [ ] Production monitoring  
- [ ] Azure alerts  
- [x] Production smoke tests (email + infra A1–A4)  
- [ ] Account deletion  
- [ ] Cookie banner (if legally required)  
- [ ] Azure Blob storage validation  
- [ ] Production SQL review  
- [ ] Pilot employers  
- [x] Support email operational (`support@uletismenu.com`) — contact + Zoho LIVE verified  
- [ ] Bug fixing from pilot  
- [ ] Marketing launch  

---

## Environment naming cleanup (policy)

| Use in docs / talk | Meaning |
|--------------------|---------|
| LIVE / PROD / Production | Real users — Azure App Service + Cloudflare Pages `main` |
| TEST | QA / pre-release — Render + Pages `develop` |
| Development | Local machine |

| Legacy name (do not casually rename in Azure) | Reality |
|-----------------------------------------------|---------|
| `rg-uletismenu-staging` | LIVE resource group |
| `api-staging-uletismenu` | LIVE API App Service |
| `UletiSmenuDb_Staging` | LIVE database |
| GHA workflow `main_api-staging-uletismenu.yml` | Deploys LIVE API |
| `ASPNETCORE_ENVIRONMENT=Staging` on Render | **TEST** (ASP.NET env name) |
| `appsettings.Staging.json` | Config for TEST when env=`Staging` |

**Safe to change in git:** docs, comments, script display names, prefer `configure-azure-live.ps1`.  
**Manual later (breaking if rushed):** Azure resource renames, DNS, workflow file renames, SQL DB rename.

Deprecated / prefer live script: `configure-azure-staging.ps1` → use `configure-azure-live.ps1`.  
Historical doc: `STAGING_DEPLOY.md` describes old Azure staging paths — prefer `ENVIRONMENTS.md` + `configure-azure-live.ps1` for LIVE.

---

## How to update this roadmap

1. Same change set that completes work: edit status rows / checklists.  
2. Bump **Last updated**.  
3. Prefer short factual notes over long essays.  
4. New phases only by explicit product decision.
