# External uptime monitoring (D3)

Monitor **LIVE liveness**, not readiness.

| | |
|---|---|
| **URL** | `https://api.uletismenu.com/health` |
| **Expect** | HTTP `200`, body contains `"ok"` |
| **Interval** | Every **5 minutes** |
| **Alert** | Email `support@uletismenu.com` (and optionally SMS) |

Do **not** use `/health/ready` as the primary uptime check — it returns Unhealthy/`503` while Azure SQL is paused (normal for serverless).

## UptimeRobot (free)

1. Sign up / log in at [https://uptimerobot.com](https://uptimerobot.com).
2. **Add New Monitor**
   - Monitor Type: **HTTP(s)**
   - Friendly Name: `UletiSmenu LIVE API health`
   - URL: `https://api.uletismenu.com/health`
   - Monitoring Interval: **5 minutes**
3. **Alert Contacts** → add `support@uletismenu.com` (confirm the email).
4. Optional: Keyword monitor for `ok` or `"status":"ok"`.
5. Save.

After the first successful check, mark **D3** Done in `PRODUCTION_SMOKE.md`.

## Alternatives

- Better Stack, Pingdom, Azure Availability Tests — same URL and interval.
