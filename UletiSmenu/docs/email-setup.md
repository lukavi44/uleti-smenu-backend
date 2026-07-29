# Email setup (Zoho Mail)

Production and TEST email for UletiSmenu uses **Zoho Mail (EU)** via SMTP.

## Addresses

| Role | Address |
|------|---------|
| Primary mailbox / SMTP auth | `support@uletismenu.com` |
| General contact | `info@uletismenu.com` |
| Transactional From | `noreply@uletismenu.com` |
| Privacy | `privacy@uletismenu.com` |
| Legal | `legal@uletismenu.com` |
| Contact form recipient | `support@uletismenu.com` (fixed in backend) |

Do **not** use `hello@uletismenu.com` in the product.

## SMTP settings

| Setting | Value |
|---------|--------|
| Host | `smtppro.zoho.eu` |
| Port | `587` |
| Security | STARTTLS (`EnableSsl=true`) |
| Username | `support@uletismenu.com` |
| Password | Zoho **app password** (env only — never commit) |
| FromEmail | `noreply@uletismenu.com` |
| FromName | `UletiSmenu` |
| ReplyToEmail | `support@uletismenu.com` |
| ContactInbox | `support@uletismenu.com` |
| DebugApiKey | Random secret for TEST `test-email` endpoint |

Config section: `SmtpSettings`  
Env prefix: `SmtpSettings__`

There is **no silent fallback** when `FromEmail` is blank — sending is skipped / Production refuses to start.

## Zoho: app password

1. Sign in to Zoho Mail as `support@uletismenu.com`.
2. Open **Security** → **App Passwords** (or Account → Security).
3. Generate an app password named e.g. `UletiSmenu API`.
4. Store it only in Azure App Settings / Render secrets / `dotnet user-secrets`.
5. When rotating: create a new app password → update env → restart app → revoke the old password.

## Zoho: send as `noreply@` alias (required)

SMTP authenticates as `support@uletismenu.com` but **From** is `noreply@uletismenu.com`.

Zoho must allow that alias as a send identity:

1. Confirm `noreply@uletismenu.com` exists as an **alias** (or mailbox) on the same Zoho org / `support@` account.
2. In Zoho Mail: enable **Send Mail As** / alias sending for `noreply@uletismenu.com` under the `support@` account (Settings → Mail → Send Mail As, or Admin Console → Users → Aliases).
3. Send a test email and inspect headers: `From:` must be `noreply@uletismenu.com`, `Reply-To:` `support@uletismenu.com`.
4. If Zoho rejects the send (or rewrites From to `support@`), the alias is **not** authorized yet — fix Zoho before go-live. The API will **not** fall back to another From address.

## Environments

### Local Development

`ASPNETCORE_ENVIRONMENT=Development`

1. Copy `API/appsettings.Development.json.example` → `appsettings.Development.json` (gitignored).
2. Prefer user-secrets:

```powershell
cd UletiSmenu\API
dotnet user-secrets set "SmtpSettings:Host" "smtppro.zoho.eu"
dotnet user-secrets set "SmtpSettings:Username" "support@uletismenu.com"
dotnet user-secrets set "SmtpSettings:Password" "<zoho-app-password>"
dotnet user-secrets set "SmtpSettings:FromEmail" "noreply@uletismenu.com"
dotnet user-secrets set "SmtpSettings:ReplyToEmail" "support@uletismenu.com"
```

3. Test:

```http
POST https://localhost:7029/api/v1/debug/test-email
Content-Type: application/json

{ "email": "you@example.com" }
```

No debug API key required in Development.

### Render TEST

`render-test.yaml` sets **`ASPNETCORE_ENVIRONMENT=Staging`** explicitly. That is how TEST is identified (not Production).

In Render → `uletismenu-api-test` → Environment, set:

```
SmtpSettings__Password=<zoho-app-password>
SmtpSettings__DebugApiKey=<long-random-secret>
```

(Other SMTP keys are already in the blueprint.)

Test:

```http
POST https://api-test.uletismenu.com/api/v1/debug/test-email
Content-Type: application/json
X-Email-Debug-Key: <SmtpSettings__DebugApiKey>

{ "email": "you@example.com" }
```

Without a valid `X-Email-Debug-Key` on Staging, the endpoint returns **401**.  
On Production the same route returns **404**.

### Azure Production (LIVE)

`ASPNETCORE_ENVIRONMENT=Production`

If any required `SmtpSettings` field is missing at startup, the API logs a **critical** error and **refuses to start**.

Set in App Service Application settings:

```
SmtpSettings__Host=smtppro.zoho.eu
SmtpSettings__Port=587
SmtpSettings__EnableSsl=true
SmtpSettings__Username=support@uletismenu.com
SmtpSettings__Password=<zoho-app-password>
SmtpSettings__FromEmail=noreply@uletismenu.com
SmtpSettings__FromName=UletiSmenu
SmtpSettings__ReplyToEmail=support@uletismenu.com
SmtpSettings__ContactInbox=support@uletismenu.com
```

Do **not** set `DebugApiKey` for Production (endpoint is disabled regardless).

Optional script (does not print the password):

```powershell
cd UletiSmenu\scripts
.\configure-azure-live.ps1 -SmtpPassword "<zoho-app-password>"
```

Restart the App Service after changing settings.

## What sends email

| Flow | Recipient | Failure behavior |
|------|-----------|------------------|
| Email confirmation | New user | Logged; registration still succeeds |
| Welcome employer / candidate | New user | Logged; registration still succeeds |
| Password reset | User | Returns success message; send failure logged |
| Favourite restaurant new post | Followers | Logged; job post still created |
| Contact form | Fixed `ContactInbox` | 503 if SMTP fails |
| Debug test-email | Caller-supplied | Dev/Staging only |

## Credential rotation

1. Generate a new Zoho app password.
2. Update `SmtpSettings__Password` (Azure / Render / user-secrets).
3. Restart the app.
4. Send a test email (Dev or Staging debug endpoint).
5. Revoke the previous Zoho app password.

Never commit passwords. Never log passwords (application logs do not include `SmtpSettings__Password`).

## Troubleshooting Zoho SMTP

| Symptom | Likely cause | Fix |
|---------|----------------|-----|
| Auth failed / 535 | Wrong app password or using account login password | Create a fresh Zoho **app password** |
| Cannot send as noreply@ | Alias not allowed for Send Mail As | Enable Send Mail As / alias for `noreply@` on `support@` |
| Connection timeout | Firewall / wrong host | Use `smtppro.zoho.eu`, port `587`, STARTTLS |
| Production app won’t start | Missing SMTP env vars | Set all required `SmtpSettings__*` then restart |
| test-email 404 on LIVE | Expected | Endpoint disabled in Production |
| test-email 401 on TEST | Missing debug key | Set `SmtpSettings__DebugApiKey` and send `X-Email-Debug-Key` |
| Contact form 503 | SMTP incomplete or Zoho rejected | Check App Service logs (no secrets) and Zoho audit |

## Security notes

- Contact form recipient is **server-side only** (`SmtpSettings:ContactInbox`). Clients cannot choose the To address.
- Name / email / subject are sanitized to strip CR/LF and control characters (header-injection protection).
- Message body length is capped (4000 characters).
- Contact endpoint is rate-limited (`RateLimitPolicies.Contact`: 5/hour/IP).
