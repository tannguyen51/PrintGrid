# Project Register v1.2 — CONTENT DRAFT (after GVHD Review 1, 30/09) · English

**How to use:** inherits everything in `Phieu_FA26SE249_v1.1-DRAFT.md` (keep the v1.1 file — never edit an approved version in place); open the v1.1 file, paste the blocks below into the matching template fields, **do not change field order or names**. The v1.2 Record of Changes goes right after the v1.1 rows.

**Decision sources:** Review 1 minutes (`workbook/06_Bien-ban-Review-1-GVHD.md`), response proposals (`workbook/05_Phan-hoi-GVHD-review-30-09.md`), team resolutions on the four conflicts XĐ-1a/1b/2/4 (early Oct), and the 06/10 decision to merge the reviewing staff role into **Operations Manager** (no new actor, keeps BCD at 9 entities). Technical baseline: 103 BR · 49 FR · 33 UC · 17 core NFR; new source items **P-106 → P-119**.

## 0. Record of Changes — v1.2 rows (append after the v1.1 rows)

| VERSION | DATE | A/M/D | IN CHARGE | CHANGE DESCRIPTION | REFERENCE |
|---------|------|-------|-----------|--------------------|-----------|
| 1.2 | …/…/2026 | M | Team leader | Payment becomes **staged**: every order pays a deposit before production starts and settles the balance before packing (BR-PAY-007/008, FR-CUST-012, UC-031); small orders may stay single-payment via the auto-lane threshold ⚠B19 | Review 1 §4 (supervisor); P-109 |
| 1.2 | …/…/2026 | M | Team leader | Quoting is **not fully automatic**: engine produces a draft; an Operations Manager staff member reviews/adjusts within a configurable band before the quote is published (BR-QUOTE-008, FR-ANAL-006, UC-029) | Review 1 §4; P-107 |
| 1.2 | …/…/2026 | M | Team leader | Assignment changes from centralized push to **offer fan-out to the top-k labs**: partners accept/decline, ops arbitrates high-value jobs (BR-ASSIGN-010 supersedes BR-ASSIGN-008) | Review 1 §6; P-108 |
| 1.2 | …/…/2026 | A | Team leader | **Quantity splitting** across capable labs when lateness is forecast; if the date still cannot be held, a public date-change proposal to the customer (BR-SCHED-010/011) | Review 1 §1; P-106 |
| 1.2 | …/…/2026 | A | Team leader | **File evidence chain**: SHA-256 hash at upload + confirmation email to the customer + partner acknowledgement of the received hash at acceptance; design deliverables are customer-owned, reuse prohibited (BR-IP-001..004, NFR-LEGAL-003, UC-032) | Review 1 §12–17; P-111/P-115 |
| 1.2 | …/…/2026 | A | Team leader | Inventory becomes a **transaction ledger**: every in/out has a code, reason and running total; 1–5% reserve tolerance; reserve-on-accept / settle-on-done (BR-STOCK-001..003, FR-LAB-003) | Review 1 §2; P-116 |
| 1.2 | …/…/2026 | A | Team leader | **Automatic shipment merging** for orders to the same customer and address (FR-HUB-006, BR-LOG-001/002) and **scheduled lab→hub shipment batches** (FR-LAB-008, BR-LOG-005) | Review 1 §4/§8–9; P-112 |
| 1.2 | …/…/2026 | A | Team leader | Money model extended: **shipping fee** as a transparent quote line with a bounded platform loss tolerance (BR-LOG-004); **transport allowance** per lab zone (BR-LOG-003); **per-partner contract discount rate** disclosed to the partner (BR-PAY-009); payout formula BR-PAY-002 amended | Review 1 §2/§5/§7; P-113/P-118 |
| 1.2 | …/…/2026 | A | Team leader | **Category-aware dispatch** (material, price, customer feedback, rate) added as the 6th scoring criterion (BR-ASSIGN-009); labs may declare capability **at machine or workshop level**, the engine degrades granularity (BR-LAB-009) | Review 1 §8/§11; P-114/P-119 |
| 1.2 | …/…/2026 | A | Team leader | **Design service**: customers with an idea but no 3D file submit a request form, receive a staged design quotation; the deliverable is customer-owned (FR-CUST-013, UC-032) | Review 1 §6; P-115 |
| 1.2 | …/…/2026 | M | Team leader | QC gains a lab tier: **self-inspection with photo proof** before hub dispatch; a batch ships only after staff approval of the proof (BR-QC-011/012, FR-ANAL-007, UC-030) | Review 1 §18–19; P-110 |
| 1.2 | …/…/2026 | A | Team leader | **Customer-side response timeouts**: 48 h without date-change approval → original promise stands + escalation; 7 days without requested evidence → request closed STALE (BR-NOTIFY-003/004) | Review 1 §9; P-117 |
| 1.2 | …/…/2026 | — | Supervisor | ☐ Confirmed / ☐ Changes requested — signature: | |

> Open items carried on the register: **B19/B20** (deposit %, cancellation/forfeit policy), **B22** (zone rates & funding source), **B23** (auto-lane threshold for quote publishing), **B24** (vouchers — supervisor suggested, previously cut by the team; not yet in scope text).

## 1. Field 3.1 — inherited from v1.0, untouched (same as v1.1)

## 2. Field 3.2 a) Context — keep the v1.1 blocks, append one paragraph

> **Broader operational scope (Review 1):** the platform not only quotes, assigns and schedules — it also governs **staged money flow, file-handover evidence (intellectual property), controlled inventory, and fair cost allocation between the hub and independent labs**. These mechanisms turn the single-brand promise into operations that independent partners can actually run under.

## 3. Field 3.2 b) Proposed Solutions — amend four pillars, add three

Amend existing pillars (replace the v1.1 text):

> - **Pillar 4 Deadline-Backward Scheduling** — added: when the unstarted part of a plan is forecast late, the engine first **splits item quantity** across capable labs at unchanged customer price, then reassigns, and only then proposes a date change requiring customer consent.
> - **Pillar 5 Speculative Quote-Time Scheduling** — added: the engine output is a **draft quote**; a quote reaches the customer only after staff review (or the auto-lane for small orders under a configured threshold). Quote expiry counts from approval.
> - **Pillar 3 Capability-Filtered Assignment** — added: output is a set of **offers sent concurrently to the top-k labs**; the first valid acceptance wins, ties break on score, ops arbitrates high-value jobs. Scoring gains a **category-match** criterion (material specialty, customer feedback, price, contract rate).
> - **Pillar 10 Model File Access Control** becomes **Pillar Evidence & Ownership**: besides short-lived signed URLs and access logs, every file gets a **SHA-256 hash** at upload, the customer receives a **confirmation email** (time, size, hash), and partners **acknowledge the received hash** when accepting a job; all records are immutable and reconstructable in disputes.

New pillars (added in v1.2):

> 11. **Human Supervision Gates** — two Operations Manager checkpoints: reviewing/publishing engine-drafted quotes (workbench, band-limited edits, before/after audit) and approving lab self-QC photo proofs before a batch may ship to the hub. The machine proposes; a person is accountable.
> 12. **Staged Payment & Partner Economics** — deposit on every order (configurable %, default 50) gates production start; balance auto-invoiced and gates the pack step; shipping is a transparent quote line with a bounded platform loss; lab payout = `base × contract rate + transport allowance − reprint costs`, settled off-platform via a documented runbook — no in-app wallets.
> 13. **Design Intake** — customers with an idea but no file submit a brief + reference images into the ops queue; design quotation reuses the staged-payment machinery; the delivered model is customer-owned and may never be reused by the platform or any lab.

## 4. Field 3.2 c) Functional Requirements — amend three roles, add no new actor

> - *Customer:* payment bullet → "Pay a **deposit** to start production and **settle the balance** before shipping, through the integrated payment gateway" · add bullet: "Submit a **design request** (idea + references) and track its staged quotation"
> - *Lab Manager:* accept bullet → "Receive **job offers** from the engine and accept/decline within the response window, acknowledging the received file hash"; inventory bullet → "Maintain material/colour inventory as a **transaction ledger** (code, reason, running total, 1–5% reserve tolerance)"; machines bullet → "Declare capability **at machine or workshop level**"; add bullet: "Group proof-approved jobs into **scheduled hub-bound shipment batches**"
> - *Operations Manager:* add two bullets: "**Review and publish** engine-drafted quotes (adjust within a configurable band, logged)" and "**Approve lab QC photo proofs** before batches may ship to the hub"
> - *Hub Fulfillment:* pack bullet → "Consolidate and pack orders, **auto-merging shipments for the same customer and address**"
> - Remaining roles unchanged from v1.1. (No new role: both review gates belong to Operations Manager — team decision 06/10, keeps UCD actors aligned with the 9 BCD entities.)

## 5. Field 3.2 d) Non-Functional Requirements — amend two bullets, add one

> - Quote responsiveness: engine **draft** ≤ 60 s (p95) as in v1.1; **published-quote time = staff review-queue SLA** (configurable, default 2 h) — two distinct metrics.
> - Reliability: kept; add "machine double-booking is **impossible at the database layer** (EXCLUDE constraint) at whatever granularity the lab declared".
> - **NEW — Auditability of file handover:** 100% of uploads carry a hash + receipt email within 1 minute; 100% of job acceptances include a matching hash acknowledgement; any sampled dispute reconstructs file/hash/time/sender/receiver.

## 6. Field 3.2 e) — unchanged from v1.1

## 7. Field f) Products — update

> - **Operations Console** (extends the existing ops console): two new screens — *Quote Review Workbench* and *Lab Proof Approval Queue*.
> - **Lab App**: new screens — *Inventory Ledger* and *Shipment Batch Planner*.
> - **Customer Portal**: new *Design Request* screen; checkout shows the two payment stages.
> - **Hub Console**: pack screen proposes **merged shipments for the same customer**.

## 8. Field g) Proposed Tasks — two lines

> - WP3: "staged-payment gateway legs (deposit/balance) and reconciliation" (after SEPay, week 7).
> - WP1: "B17/B19/B20 — cancellation & deposit-forfeit policy — agreed with the supervisor before deposit code starts".

## 9. Out of scope — three extra rows (continues the v1.1 table)

| Excluded | Reason type | Note |
|-----------|-------------|------|
| In-platform 3D modelling software | Beyond resources | The platform only intakes and hands over results; design labour is an operations runbook |
| Lab e-wallets / automated payouts | Outside goals + third-party dependency | Settlement via runbook; the system computes and displays the formula result |
| Voucher campaign engine | Pending confirmation B24 | If approved: fixed codes with %/amount caps only, no campaign system |

## 10. Risks — two extra lines

> New risks: **conversion friction from deposits on every order** (measured via the usability NFR; mitigation: auto-lane threshold proposal B19); **deposit-forfeit disputes when customers cancel** (B17/B20 undecided — blocks deposit code until policy is signed).

## 11. Research section — unchanged from v1.1

---

## Pre-submission checklist (B3 self-check)

- ☐ Every RoC row points to one Review-1 minute item or a dated team decision
- ☐ No new role in field c) — both review gates belong to Operations Manager (06/10 decision; keeps BCD at 9 entities)
- ☐ ⚠ B19/B20/B22/B23/B24 left open — no numbers self-approved before the supervisor's signature
- ☐ P-106→P-119 consistent with `workbook/07_Traceability.md`; old P numbers never reused
- ☐ Register figures (103 BR / 49 FR / 33 UC / 17 NFR) live only in the reference header — the register describes scope, it does not enumerate codes
- ☐ Vouchers stay out of scope text, flagged "pending B24" only
