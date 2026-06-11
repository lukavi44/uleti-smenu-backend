# Stripe test mode — local setup

Follow this once to enable billing on your machine. Keys stay in **user secrets** (not committed to git).

---

## Serbia is not supported by Stripe

**Stripe does not offer merchant accounts for businesses registered in Serbia.**  
If Serbia is missing from the country list at signup, that is normal — you cannot complete onboarding with a Serbian company or local bank account today.

### Practical options for UletiSmenu

| Path | Best for | Notes |
|------|----------|--------|
| **A. Keep building without live Stripe** | Now | Leave `Stripe:Enabled=false`. Billing gate, upgrade UI, and manual upgrades still work. |
| **B. Foreign entity + Stripe** | EU/US launch | Register in a [supported country](https://stripe.com/global) (e.g. Croatia, Slovenia, Estonia) or use [Stripe Atlas](https://stripe.com/atlas) (US company). Then follow this guide. |
| **C. Local provider (recommended for RS market)** | Production in Serbia | **CorvusPay** supports Serbian merchants, **RSD**, and **subscriptions** via API — fits our `IPaymentProvider` design. Implement as `CorvusPaymentProvider` later. |
| **D. Dev-only test keys** | Testing Stripe code once | Use a Stripe test account tied to a supported country (e.g. if you or a partner have an EU/US entity). **Test keys only** — not for production. |

We built billing behind `IPaymentProvider` so Stripe can stay the reference implementation while you add CorvusPay (or another RS provider) without rewriting the app.

**For step 2 right now:** if you have no foreign company, skip Stripe Dashboard setup and continue with **option A** until you choose B or C.

---

## 1. Stripe account & Dashboard

> Skip this section if you are in Serbia without a supported-country business entity (see above).

1. Sign in at [dashboard.stripe.com](https://dashboard.stripe.com) (create account if needed).
2. Turn on **Test mode** (toggle top-right).

---

## 2. Create products (EUR)

### Basic — credit pack (one-time payment)

1. **Product catalog** → **Add product**
2. Name: `UletiSmenu Basic Credits`
3. Price: **One time** · **29 EUR** (or your test amount)
4. Save and copy the **Price ID** (`price_...`) → this is `BasicCreditPack`

### Pro — monthly subscription

1. **Add product**
2. Name: `UletiSmenu Pro`
3. Price: **Recurring** · **Monthly** · **49 EUR**
4. Copy **Price ID** → `ProMonthly`

---

## 3. API keys

1. **Developers** → **API keys**
2. Copy **Secret key** (`sk_test_...`)

Do not commit this key.

---

## 4. Customer Portal (for “Manage billing”)

1. **Settings** → **Billing** → **Customer portal**
2. Enable portal and allow: update payment method, cancel subscription, view invoices
3. Save

---

## 5. Install Stripe CLI (webhooks locally)

**Windows (winget):**

```powershell
winget install Stripe.StripeCli
```

Restart the terminal, then log in:

```powershell
stripe login
```

---

## 6. Configure the API (user secrets)

From the API project folder:

```powershell
cd D:\repos\UletiSmenu\uleti-smenu-backend-git\UletiSmenu\API

dotnet user-secrets set "Stripe:Enabled" "true"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY"
dotnet user-secrets set "Stripe:PriceIds:BasicCreditPack" "price_YOUR_BASIC"
dotnet user-secrets set "Stripe:PriceIds:ProMonthly" "price_YOUR_PRO"
```

Webhook secret is set in step 7 (from `stripe listen` output).

Or edit `scripts/set-stripe-dev-secrets.ps1` and run it.

Verify:

```powershell
dotnet user-secrets list
```

---

## 7. Run the stack (3 terminals)

**Terminal 1 — API**

```powershell
cd D:\repos\UletiSmenu\uleti-smenu-backend-git\UletiSmenu\API
dotnet run --launch-profile https
```

**Terminal 2 — Stripe webhooks** (use HTTP to avoid dev-cert issues)

```powershell
stripe listen --forward-to http://localhost:5201/api/v1/Billing/webhooks/stripe
```

Copy the webhook signing secret (`whsec_...`) from the output, then:

```powershell
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project D:\repos\UletiSmenu\uleti-smenu-backend-git\UletiSmenu\API
```

Restart the API after setting the webhook secret.

**Terminal 3 — Frontend**

```powershell
cd D:\repos\UletiSmenu\uleti-smenu\uleti-smenu_react\uleti-smenu
npm run dev
```

---

## 8. Test the flow

1. Open `http://localhost:5173` and log in as an **employer**.
2. Go to **Plans & billing** (`/billing/upgrade`).
3. Confirm **Subscribe** / **Buy credits** buttons are enabled (`paymentsEnabled: true` in network tab on `GET /api/v1/Billing/me`).
4. Click **Pro Monthly** → Stripe Checkout opens.
5. Pay with test card: `4242 4242 4242 4242` · any future expiry · any CVC · any postal code.
6. After redirect, watch Terminal 2 for `checkout.session.completed` and similar events.
7. Refresh profile / billing — status should show **Active** (Pro) or credits added (Basic).
8. Try **Manage billing** → Stripe Customer Portal.
9. Try creating a job post (should work per plan limits).

### Test PastDue (optional)

In Stripe Dashboard → customer → subscription → simulate **failed payment** or use test clock. After grace period, new posts should block with billing warning.

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| Checkout button disabled | `Stripe:Enabled` + `SecretKey` + price IDs set; restart API |
| “Stripe price is not configured” | Match price IDs in user secrets to Dashboard prices |
| Webhook 400 / signature error | `WebhookSecret` must match current `stripe listen` session; restart API |
| Webhook never fires | CLI running; URL is `http://localhost:5201/...` not https |
| Portal fails | Enable Customer Portal in Stripe Dashboard; employer must have completed checkout once |
| Build locked DLLs | Stop debugger / other `dotnet run` before rebuilding |

---

## Checklist

- [ ] Test mode on in Stripe
- [ ] Two EUR prices created (one-time + monthly)
- [ ] User secrets set (Enabled, SecretKey, both PriceIds, WebhookSecret)
- [ ] API + `stripe listen` + frontend running
- [ ] Successful Pro checkout and job post
- [ ] Basic credit pack adds `PostCredits` after webhook
