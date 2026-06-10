# UletiSmenu — Payment & Subscription Proposal

**Status: DRAFT — awaiting your approval before Stripe implementation**

This document proposes how paid plans work after the 90-day free trial.  
Nothing in this file is implemented in payment code until you approve the decisions below.

---

## 1. Business rules (proposed)

| Rule | Proposal |
|------|----------|
| Trial length | **90 days** from employer registration (already implemented) |
| During trial | Full access: post shifts, applicants, chat, branches |
| After trial expires | **Cannot create new job posts** |
| Existing data | Active/archived posts, applications, chat, profile remain **readable** |
| Employees | Always free — no subscription |
| Grace period | **7 days** after expiry with banner only (optional — see question below) |

**Question for you:** Do you want a 7-day grace period where posting still works but upgrade is nagged, or **hard block** on day 91 (recommended for simplicity)?

---

## 2. Available plans (proposed)

Two paid tiers for **restaurants (employers)** only:

| Plan | Billing | Price (proposal) | Includes |
|------|---------|------------------|----------|
| **Starter** | Monthly | **4,990 RSD / month** | Unlimited active job posts, all branches, chat, reviews |
| **Starter** | Yearly | **49,900 RSD / year** (~2 months free) | Same as monthly |
| **Pro** (optional v2) | Monthly | **9,990 RSD / month** | Starter + priority support + featured listing (future) |

**Trial plan** (internal, not sold): `3-Month Free Trial`, 90 days, 0 RSD — already seeded.

**Question for you:**
- Approve **Starter only** for v1, or launch with **Starter + Pro**?
- Confirm **RSD pricing** or use EUR for Stripe?

---

## 3. Monthly vs yearly

**Recommendation:** Offer both on the Upgrade page.

- Stripe **Prices** map 1:1 to our `Subscription` catalog rows (`MonthlyStarter`, `YearlyStarter`).
- Employer picks interval at checkout.
- Yearly shows “Save ~17%” badge.

---

## 4. Trial expiry behavior

```
Registration → Trial (90d) → [expires] → Block new posts → Upgrade → Active paid → [cancel/fail] → Expired
```

| Status stored | Meaning |
|---------------|---------|
| `Trial` | On free trial, `SubscriptionStop` in future |
| `Active` | Paid subscription current |
| `PastDue` | Payment failed, Stripe retrying (optional grace) |
| `Canceled` | User canceled; access until period end |
| `Expired` | No valid trial or subscription |

**Job post gate** (already partially implemented):

```csharp
// BillingService.ValidateEmployerCanCreatePostAsync
// Allow if: Trial OR Active (and not PastDue if you want strict mode)
// Deny if: Expired / none / PastDue (configurable)
```

---

## 5. Stripe integration approach (recommended)

### Use **Stripe Checkout** (hosted) + **Customer Portal**

| Piece | Choice | Why |
|-------|--------|-----|
| Checkout | **Stripe Checkout** | PCI-safe, fast to ship, mobile-friendly |
| Manage/cancel | **Stripe Customer Portal** | Invoices, card update, cancel — no custom UI |
| Custom card form | **No** (v1) | More PCI/compliance work |

**Flow:**

1. Employer on expired trial opens **Upgrade** page.
2. Clicks “Subscribe monthly” or “Subscribe yearly”.
3. Backend creates `Stripe Checkout Session` → redirect to Stripe.
4. On success, Stripe webhook `checkout.session.completed` → we set `Employer.SubscriptionStart/Stop`, `StripeCustomerId`, `StripeSubscriptionId`.
5. “Manage billing” button → Customer Portal session.

**Not recommended for v1:** fully custom payment UI.

---

## 6. Webhook events (minimum set)

| Event | Action |
|-------|--------|
| `checkout.session.completed` | Activate subscription, store Stripe IDs |
| `customer.subscription.updated` | Sync period end, status (active/past_due/canceled) |
| `customer.subscription.deleted` | Mark expired after period end |
| `invoice.payment_failed` | Set `PastDue`, notify employer (email later) |
| `invoice.paid` | Confirm `Active` |

Webhook endpoint: `POST /api/v1/Billing/webhooks/stripe` (signature verified, no auth).

---

## 7. Data model extensions (proposed)

### Keep existing

- `Subscriptions` table — **plan catalog** (Trial, Monthly Starter, Yearly Starter)
- `Employer.SubscriptionId`, `SubscriptionStart`, `SubscriptionStop`

### Add to `Employer`

| Column | Type | Purpose |
|--------|------|---------|
| `StripeCustomerId` | string? | Stripe Customer |
| `StripeSubscriptionId` | string? | Active Stripe subscription |
| `BillingStatus` | string | `Trial`, `Active`, `PastDue`, `Canceled`, `Expired` |
| `BillingProvider` | string | `Stripe` (future-proof) |

### New table: `PaymentEvents` (audit / idempotency)

| Column | Purpose |
|--------|---------|
| `Id` | PK |
| `ProviderEventId` | Stripe event id (unique) |
| `Type` | e.g. `checkout.session.completed` |
| `Payload` | JSON snapshot |
| `ProcessedAtUtc` | |

### Interface isolation

```csharp
public interface IPaymentProvider
{
    Task<Result<string>> CreateCheckoutSessionAsync(Guid employerId, Guid planId, string successUrl, string cancelUrl);
    Task<Result<string>> CreateCustomerPortalSessionAsync(Guid employerId, string returnUrl);
    Task<Result> HandleWebhookAsync(string json, string signatureHeader);
}
```

`StripePaymentProvider` implements this. `BillingService` depends on `IPaymentProvider`, not Stripe types.

---

## 8. API endpoints (after approval)

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/v1/Billing/plans` | Public catalog (paid plans only) |
| GET | `/api/v1/Billing/me` | Current employer subscription + Stripe portal eligibility |
| POST | `/api/v1/Billing/checkout` | Body: `{ planId }` → `{ checkoutUrl }` |
| POST | `/api/v1/Billing/portal` | → `{ portalUrl }` |
| POST | `/api/v1/Billing/webhooks/stripe` | Stripe webhooks |

---

## 9. Frontend (after approval)

- `/billing/upgrade` — plan cards, current status, CTA → Stripe Checkout
- Trial banner links here when ≤14 days or expired
- Job post “Create” blocked → toast + redirect to Upgrade
- Post-checkout return URLs: `/billing/success`, `/billing/canceled`

---

## 10. Decisions needed from you

Please reply with approvals or changes:

1. **Plans:** Starter only, or Starter + Pro?
2. **Prices:** 4,990 / 49,900 RSD OK? Monthly + yearly?
3. **Trial expiry:** Hard block on day 91, or 7-day grace?
4. **Stripe:** Approve Checkout + Customer Portal?
5. **Currency:** RSD via Stripe (if supported for your account) or EUR?
6. **PastDue:** Block posting immediately on failed payment, or allow until Stripe cancels?

Once approved, implementation order:

1. Migration (Employer billing columns + PaymentEvents)
2. `IPaymentProvider` + `StripePaymentProvider`
3. Webhooks + checkout/portal endpoints
4. Upgrade page wired to Checkout
5. Seed paid plan rows + Stripe Price IDs (config/secrets)

---

## 11. Out of scope for first payment release

- Invoicing / e-faktura (Serbia)
- Per-post pay-as-you-go
- Employee payments
- Coupons / referrals
- Docker / hosting (you’ll handle separately)
