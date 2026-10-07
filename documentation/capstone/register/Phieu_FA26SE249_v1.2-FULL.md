# CAPSTONE PROJECT REGISTER — PrintGrid · FULL TEXT v1.2
*(self-contained: v1.0 approved text ⊕ rebuttal changes ⊕ Review 1 changes)*

**Change markers:** `[A¹·¹]` added / `[M¹·¹]` modified since v1.0 (rebuttal round) · `[A¹·²]` added / `[M¹·²]` modified / `[D¹·²]` deleted since v1.1 (GVHD Review 1, 30/09).
**When pasting into the school .docx template: strip the markers** — the authoritative history lives in the Record of Changes table below.
⚠ = pending supervisor sign-off (B19 deposit %, B20 forfeit policy, B22 zone rates & funding, B23 auto-lane threshold, B24 vouchers).

---

## 1–2. Register information

Supervisor: Nguyễn Tấn Phúc — phucnt40@fpt.edu.vn — Lecturer.
Students `[A¹·¹]`: Nguyễn Xuân Tân (se181599, Leader) · Đậu Nguyễn Bảo Tuấn (se185029) · Nguyễn Quang Minh (se181684) · Trương Quang Nhật (se181682). Class FA26SE249, 09/2026–03/2027, Profession SE.

## 3.1 Project name — unchanged from v1.0

English: *PrintGrid: Development of a Distributed 3D Printing Fulfillment and Scheduling Platform for a Network of Independent Printing Labs* · Vietnamese: *PrintGrid: Xây dựng nền tảng điều phối và tối ưu đơn hàng cho mạng lưới 3D Printing Lab phân tán* · Abbreviation: **PrintGrid**.

## 0′. Record of Changes

| VERSION | DATE | A/M/D | IN CHARGE | CHANGE DESCRIPTION | REFERENCE |
|---|---|---|---|---|---|
| 1.0 | 25/08/2026 | A | Supervisor | Original register approved | Assignment decision |
| 1.1 | …/…/2026 | M | Team leader | Pillar 4 narrowed: batch consolidation MVP → Should-have | Rebuttal §4 |
| 1.1 | …/…/2026 | M | Team leader | Pillar 6 narrowed: MVP repairs on decline + print failure; breakdown/reprint → Should | Rebuttal §4 |
| 1.1 | …/…/2026 | M | Team leader | Pillar 7 narrowed: manual calibration via admin UI; auto-regression → Could | Rebuttal §4 |
| 1.1 | …/…/2026 | M | Team leader | Pillar 9 narrowed: ledger records & displays; standing-into-score → Should | Rebuttal §4 |
| 1.1 | …/…/2026 | M | Team leader | Payment = real money via integrated gateway (tokens + txn id only, refund/reconciliation flows); carrier = manual waybill | B1/B2, 23/09 |
| 1.1 | …/…/2026 | A | Team leader | New fields: existing systems & gap; out of scope with reason types; risks | Rebuttal §2/§5 |
| 1.1 | …/…/2026 | A | Team leader | 4 ghost-bullet FRs specified in SRS (decision log, pricing computation, pack & ship, checklist manager) | 07_Traceability |
| 1.1 | …/…/2026 | M | Team leader | Research question rewritten; research part retained | Rebuttal §6 |
| 1.2 | …/…/2026 | M | Team leader | Staged payment: deposit on every order gates production, balance gates packing ⚠B19/B20 | Review 1 §4; P-109 |
| 1.2 | …/…/2026 | M | Team leader | Quoting not fully automatic: engine draft → staff publish (band-limited, logged) ⚠B23 | Review 1 §4; P-107 |
| 1.2 | …/…/2026 | M | Team leader | Assignment → offer fan-out to top-k labs, first valid accept wins, ops arbitrates | Review 1 §6; P-108 |
| 1.2 | …/…/2026 | A | Team leader | Quantity splitting before date change; public date-change proposal | Review 1 §1; P-106 |
| 1.2 | …/…/2026 | A | Team leader | File evidence chain (hash, receipt email, partner ack) + design copyright | Review 1 §12–17; P-111/115 |
| 1.2 | …/…/2026 | A | Team leader | Inventory transaction ledger, 1–5% tolerance, reserve/settle | Review 1 §2; P-116 |
| 1.2 | …/…/2026 | A | Team leader | Auto shipment merging (same customer+address) + scheduled lab→hub batch runs | Review 1 §4/§8–9; P-112 |
| 1.2 | …/…/2026 | A | Team leader | Shipping quote line w/ loss bound; transport allowance per zone ⚠B22; partner contract rates | Review 1 §2/§5/§7; P-113/118 |
| 1.2 | …/…/2026 | A | Team leader | Category-aware scoring; machine-or-workshop declaration level | Review 1 §8/§11; P-114/119 |
| 1.2 | …/…/2026 | A | Team leader | Design-service intake (staged design quote, customer-owned deliverable) | Review 1 §6; P-115 |
| 1.2 | …/…/2026 | M | Team leader | Lab self-QC photo proof + staff approval before hub dispatch | Review 1 §18–19; P-110 |
| 1.2 | …/…/2026 | A | Team leader | Customer response timeouts (48 h date-change, 7 d evidence) | Review 1 §9; P-117 |
| 1.2 | …/…/2026 | M | Team leader | Team size corrected to 4 members; review gates owned by Operations Manager (no new role) | roster; team decision 06/10 |
| 1.2 | …/…/2026 | — | Supervisor | ☐ Confirmed / ☐ Changes requested — signature: | |

## 3.2 a) Context

*[v1.0 paragraph 1 + 7 constraint bullets — verbatim, unchanged.]*

> Demand for 3D printing services is growing and diversifying, while the printing capacity that could serve it sits scattered across many small labs — university fablabs, maker spaces, small print shops — each with a different mix of machines, technologies, materials and technical skill. When every lab takes its own orders, the customer faces a different price, a different quality standard and a different lead time at each door, and the network as a whole runs its machines badly: one lab turns work away while another sits idle, because neither can see the other's queue. The obvious fix is a single platform that takes the order and distributes the work, but the difficulty is not the storefront. It is that the platform must commit to a price and a delivery date at the moment the customer clicks order, before it knows which machine will produce the part, and must then keep that promise across a production network it does not own and cannot directly control.

- Capacity in this domain is not a single number: … *[7 bullets as v1.0]*

`[A¹·¹]` **Evidence & conditions:** (i) small labs willing to join a coordinating platform — condition: working minute with the partner lab before week 8; (ii) customers accept ordering through an intermediary — condition: survey of ≥20 potential customers; (iii) an open-source slicer runs headless within the required time — condition: pilot on 20 real STL files. Results recorded in `workbook/02` §1 at submission time.

`[A¹·¹]` **Existing systems & gap:** Xometry/Protolabs/Craftcloud do automatic quoting and routing but are closed, proprietary, job-shop platforms without a central QC hub + fault attribution as design core. Academic scheduling assumes a single owned factory. PrintGrid's gap: linking quote-time scheduling ↔ customer commitment ↔ failure repair for a **network of independent labs**, validated by simulator + 1 real lab. Novelty type: new-in-combination-and-context. GN3D Studio and Craftcloud analyses in Report 1 §3.

`[A¹·²]` **Broader operational scope (Review 1):** the platform not only quotes, assigns and schedules — it also governs **staged money flow, file-handover evidence (IP), controlled inventory, and fair cost allocation between hub and labs**. These mechanisms turn the single-brand promise into operations independent partners can actually run under.

## 3.2 b) Proposed Solutions

*[Intro paragraph as v1.0]* Build PrintGrid, a platform where the customer deals with a single system … every completed job updates a performance ledger that shapes the next allocation decision.

1. **Geometry and Slicing Analysis Service** — as v1.0. `[M¹·¹]` MVP limited to FDM on one open-source headless engine (resin/SLS → out of scope). `[M¹·²]` Output includes a **printability verdict** (watertight/manifold/oversize) the quoting step must consume.
2. **Network-Uniform Pricing Engine** — as v1.0, parameter set versioned and frozen onto each quote. `[M¹·²]` The **shipping fee is a quote line** with a bounded platform loss tolerance (⚠B22); quote output is a **draft** until published (see Pillar 11).
3. **Capability-Filtered Assignment** — as v1.0. `[M¹·¹]` MVP fixed weights configured via admin. `[M¹·²]` Output is a set of **offers sent concurrently to the top-k labs**; first valid acceptance wins; ties break on score; ops arbitrates high-value jobs. `[A¹·²]` Scoring gains a **category-match** criterion (material specialty, customer feedback, price, contract rate).
4. **Deadline-Backward Scheduling** *(batch consolidation → Should-have `[M¹·¹]`)* — internal due dates derived backwards from the customer deadline through hub transfer, inspection and shipping, dispatching-rule placement. `[A¹·²]` On forecast lateness of the unstarted part, the engine first **splits item quantity across capable labs at unchanged customer price**, then reassigns, then — only if still late — raises a customer-approved date-change proposal.
5. **Speculative Quote-Time Scheduling** — kept; crude-first from iteration 1 `[M¹·¹]`. `[M¹·²]` The engine result is a **draft quote**: publication happens after staff review, or immediately via a configurable auto-lane for small orders (⚠B23); quote expiry counts from publication.
6. **Event-Driven Rescheduling** *(MVP scope decline+failure; breakdown/reprint → Should `[M¹·¹]`)* — repair of the unstarted portion only, bounded budget, dispatching fallback always retained.
7. **Estimate Calibration Loop** *(manual factors via admin in MVP; regression → Could `[M¹·¹]`)*.
8. **Central Hub Quality Control and Fault Attribution** — as v1.0. `[A¹·²]` A first QC tier is added **at the lab**: operators self-inspect with photographic proof; a hub-bound batch is released only after staff approval of the proof.
9. **Lab Performance Ledger** *(records & displays in MVP; standing-into-score → Should `[M¹·¹]`)*.
10. ~~Model File Access Control~~ → **File Evidence & Intellectual-Property Protection** `[M¹·²]` — besides short-lived revocable links and access logs: every upload hashed **SHA-256** at receipt, customer receives a **confirmation email** (time, file, size, hash), partners **acknowledge the received hash** at acceptance; design deliverables are **customer-owned and must never be reused** by platform or labs; records immutable for dispute reconstruction.

`[A¹·²]` 11. **Human Supervision Gates** — two Operations Manager checkpoints: *quote review workbench* (band-limited edits, before/after audit, configurable SLA) and *lab-proof approval queue* gating hub dispatch. The machine proposes; a person is accountable.

`[A¹·²]` 12. **Staged Payment & Partner Economics** — deposit on every order (configurable %, default 50 ⚠B19) gates production start; balance auto-invoiced, gates the pack step; cancellation/forfeit policy ⚠B20; lab payout = `base × contract rate + transport allowance(zone, weight) − reprint costs`, computed and displayed in-system, settled off-platform via runbook (no wallets).

`[A¹·²]` 13. **Design Intake** — idea-without-file customers submit a brief + reference images into the ops queue; staged design quotation reuses the Pillar-12 payment machinery; delivered model attaches to the order draft under Pillar-10 copyright rules.

`[D¹·² of the v1.1 footnote]` ~~settlement outside platform~~ → now: settlement executed off-platform **but computed in-platform per the Pillar-12 formula**; ~~lab cannot choose jobs~~ → superseded by offer model (Pillar 3).

## 3.2 c) Functional Requirements

- **Customer:** register/sign-in/addresses · upload + 3D preview · validation feedback · per-item configuration · service level & required-by `[M¹·¹]` · automatic quote with breakdown, committed date, expiry · **pay a deposit to start production and settle the balance before shipping through the integrated payment gateway** `[M¹·² — was "Confirm the order and pay" + "pay real money" (23/09)]` · **submit a design request (idea + references) and track its staged quotation** `[A¹·²]` · real-time tracking without lab visibility · reprint/complaint in window · model library.
- **Lab Manager:** register lab + calendar/capacity/transfer `[M¹·²]` with the option to declare capability **at machine or workshop level** · material inventory as a **transaction ledger** (code, reason, running total, 1–5% reserve tolerance) `[M¹·²]` · **receive job offers and accept/decline within the response window, acknowledging the received file hash on acceptance** `[M¹·² — was "accept or decline assigned jobs"]` · schedule timeline `[M¹·¹ extended]` · performance standing · **group proof-approved jobs into scheduled hub-bound shipment batches** `[A¹·²]`.
- **Lab Operator:** queue view · time-limited download · advance state · report actuals/incidents with photos · handover recording · **self-QC checklist with photographic proof attached at completion** `[A¹·²]`.
- **Hub QC Staff:** batch receipt & reconciliation · checklist execution · defect + attribution · reprint trigger — as v1.0.
- **Hub Fulfillment Staff:** consolidate & confirm completeness · **auto-merge shipments for the same customer and address (one waybill; complaints/refunds stay per original order)** `[M¹·²]` · pack, **record waybill manually**, ship `[M¹·¹ — was "hand over to the carrier"]` · delivery status.
- **Operations Manager:** network monitor · at-risk list · override with reason · priority/suspend/cap · SLA dashboards & export `[all v1.0]` · **review and publish engine-drafted quotes (adjust within band, logged)** `[A¹·²]` · **approve lab QC photo proofs before hub dispatch** `[A¹·²]` · **arbitrate simultaneous offer acceptances on high-value jobs** `[A¹·²]`.
- **Pricing and Scheduling Engine:** slice/analyse · compute price from active parameter set and freeze version · speculative placement at quote time (draft) · decompose orders into jobs **from frozen estimates, splitting quantity across labs when needed** `[M¹·²]` · filter + score + offer fan-out `[M¹·²]` · backward due dates, consolidation (extended) `[M¹·¹]` · bounded repair · recalibration (MVP manual) `[M¹·¹]` · decision log.
- **System Administrator:** accounts/roles/permissions · catalogues · pricing/weights/threshold configuration without redeployment **incl. deposit %, review band, shipping tolerance, zone rates, partner contract rates** `[M¹·²]` · checklists & taxonomy · audit review · ⚠ *[M¹·¹] "monitor background job health" pending C3 (proposed: runbook).*

*No new role exists: the two review gates belong to Operations Manager `[M¹·² — team decision 06/10, keeps actor set = the 9 BCD entities]`.*

## 3.2 d) Non-Functional Requirements

*[v1.0 12 bullets, with the thresholds `[A¹·¹]`: preliminary ≤5 s; draft quote ≤60 s p95; repair budget ⚠ → 30 s hard/15 s mean `[M¹·² B4 decision 23/09→confirmed]`; feasibility 0-violation independent suite; promises only under approved conditions; IP window = job + 4 h `[M¹·¹, ⚠B15]`; scale 50 labs/300 machines; shop-floor ≤3 taps, 2-min offline; config effective ≤1 min.]*

`[M¹·²]` **Quote responsiveness** — two metrics now: engine **draft** ≤ 60 s p95; **published** quote = review-queue SLA (default 2 h; auto-lane under threshold ⚠B23).
`[M¹·²]` **Schedule consistency** — double-booking impossible **at the database layer** (EXCLUDE constraint) at whatever granularity the lab declared (machine or workshop queue).
`[A¹·²]` **Auditability of file handover** — 100% uploads carry hash + receipt email ≤1 min; 100% acceptances include matching-hash acknowledgement; any sampled dispute reconstructs file/hash/time/sender/receiver.
`[M¹·¹]` **Fair work distribution** — exploration allowance → Could (deferred, design hook kept).

## 3.2 e) Theory & Practical

*[v1.0 paragraphs verbatim]* `[A¹·¹]` Tooling sources: open-source slicing engine [name+pinned at pilot ⚠]; EDD/ATC dispatching rules per standard scheduling literature. `[A¹·²]` Practical bullets added: implement the **staged-payment legs (deposit/balance) and daily reconciliation**; implement the **file evidence chain and its email receipts**; verify the **inventory ledger against a hand-tallied physical count** at the partner lab.

## 3.2 f) Products

Customer Web Portal `[M¹·²]` *(checkout shows two payment stages; adds **Design Request** screen)* · Lab Portal `[M¹·²]` *(adds **Inventory Ledger** and **Shipment Batch Planner**; self-QC proof capture)* · Central Hub Console `[M¹·¹]` *("packing and **manual shipment recording**")* · Operations Console `[M¹·²]` *(adds **Quote Review Workbench** and **Lab Proof Approval Queue**)* · Slicing & Geometry Service · Pricing Engine · Assignment & Scheduling Engine `[M¹·¹ consolidation: extended; M¹·² adds quantity splitting + offer fan-out]` · Estimate Calibration Module · SLA & Performance Analytics `[M¹·² partner settlement report]` · Simulation Harness `[M¹·¹ built from week 4]` · Algorithm Evaluation Report · System Documentation.

## 3.2 g) Proposed Tasks

`[M¹·²]` Team of **4 members** (register template said 5 — corrected to the approved roster), each owning a primary package: **Tân** — WP1 PM/BA (+deposit/cancellation policy tracking with supervisor) · **Minh** — WP2 Engine (+WP3 slicing wiring) · **Tuấn** — WP4 Customer/FE · **Nhật** — WP5 Lab/Hub + QA. WP2 Algorithm Engineer, WP3 Backend, WP4 FE, WP5 full-stack/QA descriptions as v1.0.
`[A¹·¹]` WP1: secure the partner-lab agreement before week 8; fallback in-house mini-trial. `[A¹·²]` WP1: **B17/B19/B20 cancellation & forfeit policy agreed with the supervisor before deposit code starts.** `[A¹·²]` WP3: staged-payment legs + reconciliation (after gateway, week 7). `[M¹·¹]` WP5: simulation harness delivered by end of week 4 and used as the integration testbed.

## 4. Other comments

*[v1.0 six guidance bullets — verbatim, unchanged: scheduler-is-the-contribution / no-lead-time-table / failure-in-first-design / simulator-required / one-real-lab / MVS-agreed-upfront]*

**Out of scope `[A¹·¹]`** — reason types: FSLA/SLA settlement between platform & labs (runbook; formula now in-system `[M¹·²]`) · carrier APIs (third-party dependency; manual waybill) · non-FDM technologies (resource) · consolidation/standing-in-score/auto-calibration/improvement search (phased — Extended list honoured) · fair-distribution exploration (Could) · native mobile apps (resource).
`[A¹·²]` + in-platform 3D modelling software (beyond resources) · lab e-wallets / automated payouts (outside goals) · voucher campaign engine (pending ⚠B24: fixed codes only if approved).

**Risks `[A¹·¹]`** — partner lab unsecured · headless slicer too slow · scheduling competence · customer IP · integration overrun. `[A¹·²]` + **deposit friction on conversion** (mitigation: auto-lane threshold ⚠B19) · **forfeit disputes on cancellation** (⚠B20 blocks deposit code).

**Research question `[M¹·¹]`**: "How does capacity-aware quoting and scheduling reduce weighted tardiness and increase on-time rate compared to dispatching-rule and nearest-available-lab baselines, under real-time quoting constraints?" Method: simulator (load + fault injection) + 3 baselines + calibration data from 1 real lab. Commitment: mechanism and measurements, not a new algorithm.

---
*Supervisors' signature block and date line as template. HCM, …/…/2026 — On behalf of the team: Team leader.*
