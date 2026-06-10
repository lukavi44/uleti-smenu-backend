# UletiSmenu — Production Configuration Guide

This explains **what goes where** and **why**, so you can deploy without guessing.

---

## Configuration layers (ASP.NET Core)

ASP.NET Core loads settings in this order (later wins):

1. `appsettings.json` — shared defaults, **safe to commit**
2. `appsettings.{Environment}.json` — e.g. `Development`, `Production`
3. **Environment variables** — production secrets (highest priority)
4. **User Secrets** — local dev only (`dotnet user-secrets`), never committed

```
┌─────────────────────────────────────────────────────────┐
│  appsettings.json          → structure, non-secret defaults │
│  appsettings.Development.json → your machine (optional)   │
│  appsettings.Production.json  → production non-secrets    │
│  env vars / Key Vault      → passwords, API keys, URLs     │
└─────────────────────────────────────────────────────────┘
```

---

## What belongs in `appsettings.json` (committed)

**Safe defaults and structure only:**

- Logging levels
- Feature section names (`SmtpSettings`, `FileSettings`, `Cors`, `Backend`)
- Empty or placeholder values for secrets: `""` or `"REPLACE_IN_ENV"`
- **Never** real DB passwords, SMTP passwords, Stripe keys, or JWT secrets

Example:

```json
{
  "ConnectionStrings": {
    "UletiSmenu": "Server=localhost;Database=UletiSmenuDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  },
  "FileSettings": {
    "UploadPath": "uploads"
  },
  "Backend": {
    "BaseUrl": "https://localhost:7029",
    "FrontendBaseUrl": "http://localhost:5173"
  }
}
```

---

## What belongs in `appsettings.Development.json` (committed or local)

Overrides for **your dev machine**:

- Local SQL Server instance name (if different from default)
- `UploadPath`: `D:\uploads` or `./uploads`
- Swagger-friendly URLs

Optional: keep machine-specific connection strings in **User Secrets** instead:

```powershell
cd API
dotnet user-secrets set "ConnectionStrings:UletiSmenu" "Server=CNS16;Database=..."
dotnet user-secrets set "SmtpSettings:Password" "your-app-password"
```

User Secrets are stored outside the repo (`%APPDATA%\Microsoft\UserSecrets\`).

---

## What belongs in `appsettings.Production.json` (committed)

Production **non-secret** settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Cors": {
    "AllowedOrigins": [ "https://app.uletismenu.com" ]
  },
  "FileSettings": {
    "UploadPath": "/var/uleti/uploads"
  },
  "Backend": {
    "BaseUrl": "https://api.uletismenu.com",
    "FrontendBaseUrl": "https://app.uletismenu.com"
  },
  "SmtpSettings": {
    "Host": "smtp.sendgrid.net",
    "Port": 587,
    "FromEmail": "no-reply@uletismenu.com",
    "EnableSsl": true
  }
}
```

Still **no passwords** in this file.

---

## Environment variables / secrets (never commit)

Set on the server or in Azure App Service / hosting panel.

| Variable | Maps to | Example |
|----------|---------|---------|
| `ConnectionStrings__UletiSmenu` | SQL connection | `Server=...;User Id=...;Password=...` |
| `SmtpSettings__Username` | SMTP user | `apikey` |
| `SmtpSettings__Password` | SMTP password | `***` |
| `Stripe__Enabled` | Turn on live checkout | `true` |
| `Stripe__SecretKey` | Stripe secret | `sk_live_...` |
| `Stripe__WebhookSecret` | Webhook signing | `whsec_...` |
| `Stripe__PriceIds__BasicCreditPack` | Basic one-time price | `price_...` |
| `Stripe__PriceIds__ProMonthly` | Pro subscription price | `price_...` |
| `Billing__GracePeriodDays` | PastDue grace (days) | `7` |
| `Billing__Trial__MaxActivePosts` | Trial post limit | `5` |
| `Billing__Basic__MaxActivePosts` | Basic post limit | `3` |
| `Billing__Pro__MaxActivePosts` | Pro post limit | `50` |
| `ASPNETCORE_ENVIRONMENT` | Environment | `Production` |

Note: `__` (double underscore) = nested JSON path in .NET config.

**Never commit:** `.env` with secrets, `appsettings.Production.local.json`, User Secrets, Stripe keys, SMTP passwords, production connection strings.

---

## CORS — dev vs production

**Development:** frontend `http://localhost:5173` → API `https://localhost:7029`

```json
"Cors": { "AllowedOrigins": [ "http://localhost:5173" ] }
```

**Production:** only your real frontend origin(s):

```json
"Cors": { "AllowedOrigins": [ "https://app.uletismenu.com" ] }
```

Rules:

- Origins must **exactly** match (scheme + host + port).
- `AllowCredentials()` is required for cookies/bearer + SignalR — so you cannot use `*` for origins.
- Add staging URL separately when you have one: `https://staging.uletismenu.com`.

`Program.cs` reads `Cors:AllowedOrigins` from config (see implementation).

---

## Frontend API URL

Vite uses env vars prefixed with `VITE_`.

**`.env` (local, gitignored):**

```env
VITE_API_BASE_URL=https://localhost:7029
```

**`.env.production` (build-time on CI or hosting):**

```env
VITE_API_BASE_URL=https://api.uletismenu.com
```

Built into the static JS at `npm run build` — change URL = rebuild frontend.

Copy from `.env.example` when setting up a new machine.

---

## SMTP (production email)

Used for: registration confirmation, future billing emails.

| Setting | Dev | Production |
|---------|-----|------------|
| Host | `smtp.gmail.com` or Mailtrap | SendGrid, Amazon SES, Postmark |
| Port | 587 | 587 |
| Username/Password | User Secrets | Environment variables |
| FromEmail | `no-reply@uletismenu.local` | `no-reply@yourdomain.com` |

Use a domain you control for `FromEmail` (requires SPF/DKIM on DNS).

Registration already **succeeds even if email fails** — check logs in production.

---

## File uploads (replace `D:\uploads`)

**Problem:** `D:\uploads` only exists on your Windows PC.

**Solution:** configurable `FileSettings:UploadPath`:

| Environment | Path |
|-------------|------|
| Dev | `D:\uploads` or `./uploads` under API folder |
| Production Linux | `/var/uleti/uploads` |
| Production Windows Server | `C:\inetpub\uleti\uploads` |

The API serves files at `/uploads/...` via static files middleware using the same path.

Ensure the app process has **read/write** permission on that folder.

---

## Database connection strings

**Development:** LocalDB / SQL Express / `Server=CNS16;Integrated Security=True`

**Production:**

```
Server=your-server.database.windows.net;Database=UletiSmenuDb;User Id=...;Password=...;Encrypt=True;
```

Store only in environment variables or Azure Connection Strings panel.

Migrations run on startup (`EnsureDatabaseMigratedAsync`) — fine for small deployments; for large prod consider running `dotnet ef database update` in CI/CD instead.

---

## Billing & Stripe configuration

**Committed defaults** (`appsettings.json`):

```json
"Billing": {
  "GracePeriodDays": 7,
  "Currency": "EUR",
  "Trial": { "MaxActivePosts": 5, "CreditsPerPost": 0 },
  "Basic": { "MaxActivePosts": 3, "CreditsPerPost": 1 },
  "Pro": { "MaxActivePosts": 50, "CreditsPerPost": 0 }
},
"Stripe": {
  "Enabled": false,
  "SecretKey": "",
  "WebhookSecret": "",
  "PriceIds": {
    "BasicCreditPack": "",
    "ProMonthly": ""
  }
}
```

**Production:** set `Stripe:Enabled=true` and all secrets via environment variables. Register webhook URL in Stripe Dashboard:

`https://api.yourdomain.com/api/v1/Billing/webhooks/stripe`

Events: `checkout.session.completed`, `customer.subscription.*`, `invoice.payment_failed`, `invoice.payment_succeeded`.

**Frontend:** upgrade page calls `POST /api/v1/Billing/checkout` and redirects to Stripe; success/cancel returns to `/billing/upgrade?checkout=success|canceled`.

See `docs/PAYMENT_PROPOSAL.md` for full billing behavior.

---

## Checklist before going live

- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Connection string in env vars, not in git
- [ ] CORS lists only production frontend URL
- [ ] `VITE_API_BASE_URL` points to production API
- [ ] Upload folder exists and is writable
- [ ] SMTP configured and test email sent
- [ ] HTTPS on API and frontend
- [ ] Stripe enabled + keys + price IDs in env vars
- [ ] Stripe webhook endpoint registered and tested
- [ ] `Billing:*` limits reviewed for launch
- [ ] Legal pages reviewed by a lawyer (see draft disclaimer on site)

---

## Quick reference: files in repo

| File | Commit? | Contains secrets? |
|------|---------|-------------------|
| `appsettings.json` | Yes | No (placeholders only) |
| `appsettings.Development.json` | Optional | Avoid secrets |
| `appsettings.Production.json` | Yes | No |
| `appsettings.*.local.json` | **No** (gitignore) | Yes |
| `.env` / `.env.local` | **No** | Yes |
| `.env.example` | Yes | No |
| `dotnet user-secrets` | **No** | Yes |
