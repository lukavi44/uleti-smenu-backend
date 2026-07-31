# External uptime monitoring (D3)

Monitor **LIVE liveness**, not readiness.

| | |
|---|---|
| **URL** | `https://api.uletismenu.com/health` |
| **Method** | **HEAD** (UptimeRobot free) or **GET** |
| **Expect** | HTTP `200`. GET body: `{"status":"ok"}`. HEAD: empty body, same status. |
| **Interval** | Every **5 minutes** |
| **Alert** | Email `support@uletismenu.com` (and optionally SMS) |

Do **not** use `/health/ready` as the primary uptime check — it returns Unhealthy/`503` while Azure SQL is paused (normal for serverless).

## UptimeRobot free plan

Keep **HTTP method = HEAD** (default / free). The API supports HEAD on `/health` so this works without a paid GET method.

1. Sign up / log in at [https://uptimerobot.com](https://uptimerobot.com).
2. Monitor settings:
   - Monitor Type: **HTTP(s)**
   - Friendly Name: `UletiSmenu LIVE API health`
   - URL: `https://api.uletismenu.com/health`
   - **HTTP method: HEAD**
   - Up HTTP status codes: **2xx, 3xx**
   - Auth: **None**
   - Interval: **5 minutes**
3. **Alert Contacts** → `support@uletismenu.com`
4. Save and confirm the next check is green.

If you later use a paid plan, **GET** (or Keyword monitor with keyword `ok`) also works and can assert the JSON body.

## Verify manually

```bash
# Expect 200 + {"status":"ok"}
curl -i https://api.uletismenu.com/health

# Expect 200, empty body (UptimeRobot free uses this)
curl -I https://api.uletismenu.com/health

# Expect 405
curl -i -X POST https://api.uletismenu.com/health
```

## Alternatives

- Better Stack, Pingdom, Azure Availability Tests — same URL; GET or HEAD both OK after this change.
- Render / Azure host probes use path `/health` (GET-style).
