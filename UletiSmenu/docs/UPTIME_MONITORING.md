# External uptime monitoring (D3)

Monitor **LIVE liveness**, not readiness.

| | |
|---|---|
| **URL** | `https://api.uletismenu.com/health` |
| **Method** | **GET** (not HEAD) |
| **Expect** | HTTP `200`, body contains `"ok"` (JSON: `{"status":"ok"}`) |
| **Interval** | Every **5 minutes** |
| **Alert** | Email `support@uletismenu.com` (and optionally SMS) |

Do **not** use `/health/ready` as the primary uptime check — it returns Unhealthy/`503` while Azure SQL is paused (normal for serverless).

## Why GET (not HEAD)

The API registers liveness as **GET only** (`MapGet("/health", …)` in `Program.cs`).  
`HEAD /health` correctly returns **405 Method Not Allowed** with `Allow: GET`.

UptimeRobot’s plain **HTTP(s)** monitor often probes with **HEAD**, which will look like downtime even when the app is healthy. Configure the monitor to use **GET** (Keyword monitor, or HTTP Method = GET).

## UptimeRobot (free) — recommended setup

1. Sign up / log in at [https://uptimerobot.com](https://uptimerobot.com).
2. **Add New Monitor** (or edit the existing API monitor):
   - **Monitor Type:** **HTTP(s) - Keyword** (preferred — sends **GET** and checks the body)
   - Friendly Name: `UletiSmenu LIVE API health`
   - URL: `https://api.uletismenu.com/health`
   - **Keyword Type:** Exists
   - **Keyword Value:** `ok` (or `"status":"ok"`)
   - Monitoring Interval: **5 minutes**
3. If you keep Monitor Type **HTTP(s)** (non-keyword): open **Advanced Settings** / request options and set **HTTP Method** to **GET** (not HEAD / default).
4. **Alert Contacts** → add `support@uletismenu.com` (confirm the email).
5. Save and wait for the next successful check (green).

Do **not** point the primary monitor at `/health/ready`.

## Verify manually

```bash
# Expect 200 + {"status":"ok"}
curl -i https://api.uletismenu.com/health

# Expect 405 Allow: GET (by design — do not use HEAD for monitoring)
curl -I https://api.uletismenu.com/health
curl -i -X POST https://api.uletismenu.com/health
```

## Alternatives

- Better Stack, Pingdom, Azure Availability Tests — same URL, **GET**, 5-minute interval.
- Render / Azure host probes already use GET-style checks on `/health` (path only).
