# UletiSmenu — Payment & Subscription (Approved)

**Status: APPROVED & IMPLEMENTED** (Stripe behind `IPaymentProvider`)

---

## Plans

| Plan | Type | Billing | Default price (EUR) | Behavior |
|------|------|---------|---------------------|----------|
| **Free Trial** | `Trial` | 90 days | €0 | Post during trial; limits from `Billing:Trial:MaxActivePosts` |
| **Basic** | `Basic` | Credit pack (one-time Stripe Checkout) | €29 / 10 credits | Pay per post via credits; limits from `Billing:Basic` |
| **Pro** | `Pro` | Monthly subscription | €49 / month | Stripe subscription; limits from `Billing:Pro` |

Limits are **configurable** in `appsettings.json` → `Billing` section (not hardcoded).

---

## Currency

**EUR** in Stripe for now. RSD/local provider can be evaluated later.

---

## Grace period & PastDue

- **7-day grace** after failed payment (`Billing:GracePeriodDays`)
- During grace: dashboard + existing data readable; posting allowed only if Stripe subscription is still `active`/`trialing` and internal gate permits
- **PastDue**: nothing deleted; banner *"Your subscription needs attention"*; block new posts after grace
- **Customer Portal** for payment method / cancel / invoices

---

## Stripe integration

| Feature | Choice |
|---------|--------|
| New subscriptions / credit packs | **Stripe Checkout** (hosted) |
| Manage billing | **Stripe Customer Portal** |
| Custom card UI | **No** |

### Webhooks (idempotent via `PaymentEvents` table)

- `checkout.session.completed`
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `invoice.payment_failed`
- `invoice.payment_succeeded`

Endpoint: `POST /api/v1/Billing/webhooks/stripe`

---

## Internal statuses (`BillingStatus` enum)

`Trialing` · `Active` · `PastDue` · `Canceled` · `Expired` · `Incomplete`

Stored on employer (separate from Stripe raw status). Mapped in `StripePaymentProvider.MapStripeSubscriptionStatus`.

### Employer billing columns

`StripeCustomerId`, `StripeSubscriptionId`, `StripePriceId`, `CurrentPeriodEndUtc`, `TrialEndsAtUtc`, `GracePeriodEndsAtUtc`, `PostCredits`, `BillingProvider`

---

## API endpoints

| Method | Route | Auth |
|--------|-------|------|
| GET | `/api/v1/Billing/plans` | Employer |
| GET | `/api/v1/Billing/me` | Employer |
| POST | `/api/v1/Billing/checkout` | Employer |
| POST | `/api/v1/Billing/portal` | Employer |
| POST | `/api/v1/Billing/webhooks/stripe` | Anonymous (signature verified) |

---

## Enabling Stripe locally

1. Create products/prices in [Stripe Dashboard](https://dashboard.stripe.com) (EUR).
2. Set in `appsettings.Development.json` or user secrets:

```json
"Stripe": {
  "Enabled": true,
  "SecretKey": "sk_test_...",
  "WebhookSecret": "whsec_...",
  "PriceIds": {
    "BasicCreditPack": "price_...",
    "ProMonthly": "price_..."
  }
}
```

3. Forward webhooks: `stripe listen --forward-to https://localhost:7029/api/v1/Billing/webhooks/stripe`

---

## Code layout

| Piece | Location |
|-------|----------|
| `IPaymentProvider` | `Core/Services/IPaymentProvider.cs` |
| `StripePaymentProvider` | `Infrastructure.Stripe/` |
| `DisabledPaymentProvider` | Used when `Stripe:Enabled` is false |
| `BillingService` | Gate + status |
| `BillingWebhookProcessor` | Webhook business logic |
| Frontend upgrade page | `/billing/upgrade` |
