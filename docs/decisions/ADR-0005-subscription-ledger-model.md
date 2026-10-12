# ADR-0005: Subscription Credit Ledger Model

**Status:** Proposed (shape only — parameters remain open)

## Context

Per the product brief, subscription lesson/credit accounting must use a ledger model rather than a mutable `lessonsRemaining` integer (see `docs/product/SUBSCRIPTION_JOURNEY.md`).

## Decision (shape, not parameters)

- Every credit grant, consumption, expiry, and manual adjustment is an immutable `CreditLedgerEntry`.
- Balance is a derived sum over entries, never a stored mutable field.
- Entry types are extensible (grant/consume/expire/adjust at minimum) but exact entry taxonomy, expiry rules, and rollover behavior depend on pricing/cycle decisions that are **`[OPEN — Product Owner]`** (see `SUBSCRIPTION_JOURNEY.md`).

## Status

Proposed for the ledger *shape*. Explicitly not proposing cycle length, pricing, or rollover rules — those remain open and this ADR must not be read as settling them.
