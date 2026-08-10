# Account deletion — legal retention open questions

**Status:** engineering hybrid deletion shipped; **retention policy not invented** — lawyer/accountant must decide.  
**Related:** soft-launch checklist in [`ROADMAP.md`](./ROADMAP.md); API `DELETE /api/v1/User/me`.

## What engineering does today (hybrid)

| Category | Behaviour |
|----------|-----------|
| Identity (`AspNetUsers`) | **Tombstone** — PII cleared, email/username scrambled to `deleted-{guid}@deleted.local`, permanent lockout, tokens/logins/roles removed, `DeletedAtUtc` set. Row kept for FK / chat / review integrity. |
| Personal-only rows | **Hard-delete** — favourites, notifications (as recipient), work experiences, conversation read states, removable applications, profile photo blob (best-effort). |
| Shared content | **Anonymize** — chat message bodies → `[deleted]`; contact messages matching old email; employer public display name/slug; location phones cleared where applicable. |
| Billing / company identifiers | **Retain** until legal says otherwise — wallet transactions, payment events, Stripe customer/subscription IDs, PIB/MB, employer wallet balance. |

Idempotent: second delete on an already-tombstoned user succeeds with no further mutation.

## Open decisions (do not guess)

Before marketing / public launch, counsel should answer:

1. **Wallet & payment ledger** — How long must `WalletTransactions` / `PaymentEvents` be kept after account deletion? Any anonymization of amounts vs full retention?
2. **Stripe IDs** — Retain `StripeCustomerId` / subscription ids on tombstoned employers for dispute/refund handling? Process to close Stripe customer?
3. **PIB / MB (company tax IDs)** — Keep on employer tombstone for tax/accounting, or move to an offline archive and clear from DB?
4. **Restaurant addresses / locations** — Keep full address after employer deletion (ops / legal), soft-clear only phones, or hard-delete locations?
5. **Chat message bodies** — Is `[deleted]` redaction enough, or must plaintext history be purged after N days?
6. **Reviews** — Ratings/comments stay attached to tombstone Guids; any obligation to remove free-text comments on request?
7. **Contact form copies** — Retention of anonymized vs original support inbox copies outside the app DB.
8. **Backup / PITR** — Deleted PII may remain in Azure SQL backups until retention window expires; document user-facing notice if required.

Update this file when counsel decides; then adjust `AccountDeletionService` accordingly.
