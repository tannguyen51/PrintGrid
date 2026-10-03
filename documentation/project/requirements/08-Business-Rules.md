# Business Rules - PrintGrid Platform

## Overview
This document defines the business rules that govern PrintGrid platform operations. Business rules are categorized by domain and assigned unique identifiers for traceability.

**Format:** `BR-[DOMAIN]-[NUMBER]: [Rule Statement]`

---

## 0. Business Rule Matrix (submission format)

One row per rule; full statements with condition/action/exception remain in §1–§13 below.
**Notes legend:** Type = T·Term / F·Fact / C·Constraint / E·Action Enabler / K·Computation / I·Inference;
H = Hard (block violation), S = Soft (warn + recorded reason); Var = Fixed / cfg (configurable) / period.
**Status legend:** `Approved` = derives from the approved register · `Specified` = created by v1.1 sync
or the 23/09 decisions, to be frozen in register v1.2 · `Deferred` = outside MVP with declared reason ·
`⚠` = open item tracked in `capstone/workbook/04`.

| Rule ID | Business Rule Description | Category | Rationale / Purpose | Source | Impact if Violated | Status | Related Req. | Notes |
|---|---|---|---|---|---|---|---|---|
| BR-QUOTE-001 | One price per configuration regardless of producing lab | Quoting | One brand promise to the customer | P-10 | Price disputes; heterogeneity exposed | Approved | FR-SCHED-010, FR-CUST-006 | C·H·cfg(rates)·UC-001/009 |
| BR-QUOTE-002 | Delivery dates from capacity-based trial placement, never lead-time tables | Quoting | Core differentiator of the platform | P-13 | Whole premise invalid; missed promises | Approved | FR-SCHED-005 | C·H·—·UC-001/010 |
| BR-QUOTE-003 | Every quote carries an expiry (default 48 h) | Quoting | Network state changes make quotes stale | P-24 | Stale price/date commitments honored | Approved | FR-CUST-006 | C·H·cfg(48h)·UC-001 · ⚠B12 |
| BR-QUOTE-004 | Quote freezes the pricing parameter version used | Quoting | Reproducibility and audit | P-10 | Quotes unreproducible after price change | Approved | FR-SCHED-010 | F·H·—·UC-001/009 |
| BR-QUOTE-005 | Itemized cost breakdown shown on every quote | Quoting | Transparency builds trust | P-24 | Complaints, support load | Approved | FR-CUST-006 | C·H·—·UC-001 |
| BR-QUOTE-006 | Parts no machine can produce are rejected before ordering | Quoting | Never sell the impossible | P-21 | Wasted production; guaranteed breach | Approved | FR-SCHED-003, FR-CUST-004 | C·H·—·UC-001/002 |
| BR-QUOTE-007 | Complex quoting processed asynchronously with progress | Quoting | UI must never block on slicing | P-69 | Perceived hang; abandoned orders | Approved | FR-CUST-006 | C·H·—·UC-001 |
| BR-ASSIGN-001 | Assign only to machines satisfying ALL hard capability constraints | Assignment | Feasibility is correctness, not preference | P-11, P-71 | Printed scrap; deadline missed | Approved | FR-SCHED-003 | C·H·—·UC-010/011 |
| BR-ASSIGN-002 | Choose among feasible labs by weighted multi-criteria score | Assignment | Balance time/load/quality/cost/logistics | P-11 | Sub-optimal global allocation | Approved | FR-SCHED-004 | K·H·cfg(weights)·UC-011 |
| BR-ASSIGN-003 | Scoring weights configurable without redeployment | Assignment | Business priorities shift per term | P-65, P-77 | Release needed for policy change | Approved | FR-ADMIN-002 | C·H·—·UC-009 |
| BR-ASSIGN-004 | Log candidates, per-criterion scores and choice for every decision | Assignment | Settle disputes; debug allocator | P-62 | Unanswerable lab disputes | Approved | FR-SCHED-009 | C·H·—·UC-011/020 |
| BR-ASSIGN-005 | Lab must accept/decline within window (default 2 h); timeout = auto-decline | Assignment | Deadlock prevention | P-32 | Jobs stuck; schedules frozen | Approved | FR-LAB-004 | C·H·cfg(2h)·UC-004 · ⚠B5 |
| BR-ASSIGN-006 | Declines require a reason from a fixed list | Assignment | Detect filter gaps and gaming | P-32 | Silent capacity drift | Approved | FR-LAB-004 | C·H·—·UC-004 |
| BR-ASSIGN-007 | Configurable share of assignments allocated randomly among feasible labs | Assignment | Network diversity; cold-start data | P-80 | Winner-take-all shrinks network | Deferred (ext. scope) | FR-SCHED-004 (Could) | E·H·cfg(5–10%)·UC-011 |
| BR-ASSIGN-008 | Labs cannot pick jobs; the platform assigns | Assignment | Prevent adverse selection | P-11 | Bad jobs orphaned | Approved | (design invariant) | C·H·— |
| BR-SCHED-001 | Internal due dates derived backward from the customer deadline | Scheduling | Customer date fixed; work backwards | P-12 | Systematic lateness | Approved | FR-SCHED-006 | K·H·—·UC-010/011 |
| BR-SCHED-002 | Jobs sharing material/colour consolidated on one machine | Scheduling | Remove 15–30 min changeovers | P-12 | Lost capacity | Deferred → Should (A4) | FR-SCHED-006 | E·H·cfg(on/off)·UC-011 |
| BR-SCHED-003 | Batch ≤ 5 jobs and ≤ 24 h | Scheduling | Batching must not starve urgent work | P-12 | Urgent jobs blocked | Deferred → Should (A4) | FR-SCHED-006 | C·H·cfg(5/24h)·UC-011 |
| BR-SCHED-004 | Urgent jobs (due < 48 h) bypass batching | Scheduling | Deadline beats efficiency | P-12 | Urgent orders late | Approved | FR-SCHED-006 | C·H·cfg(48h)·UC-011 |
| BR-SCHED-005 | Schedule computation bounded (repair ≤ 30 s, quote ≤ 60 s) | Scheduling | Production cannot wait | P-70 | Machines idle waiting on plans | Approved (B4 23/09) | FR-SCHED-007 | C·H·cfg(30/60s)·UC-012/027 |
| BR-SCHED-006 | A feasible dispatching-rule solution always retained as fallback | Scheduling | Slightly-worse on time beats late-but-optimal | P-14 | No plan at all on timeout | Approved | FR-SCHED-007 | C·H·—·UC-012 |
| BR-SCHED-007 | Never reschedule in-progress or completed jobs | Scheduling | Cannot change history mid-print | P-14 | Wasted prints; lab trust broken | Approved | FR-SCHED-007 | C·H·—·UC-012/020 |
| BR-SCHED-008 | Committed dates move only per approved conditions + customer consent | Scheduling | Trust is the product | P-73 | Silent re-promising; complaints | Approved (B16 23/09) | FR-SCHED-007, FR-ANAL-004 | C·H·—·UC-012/020 |
| BR-SCHED-009 | Hand over completed parts to hub transport within 24 h of completion | Scheduling | Hub pipeline depends on flow | P-40 | Late inspection → late delivery | Specified | FR-LAB-005 | C·S·cfg(24h)·UC-015 |
| BR-RESCHED-001 | Production events automatically trigger rescheduling | Rescheduling | Manual repair too slow for live floor | P-14 | Queues drift invalid | Approved | FR-SCHED-007 | E·H·—·UC-012 |
| BR-RESCHED-002 | Only jobs affected by the event are modified | Rescheduling | Limit schedule nervousness | P-14 | Operators lose stable plans | Approved | FR-SCHED-007 | C·S·—·UC-012 |
| BR-RESCHED-003 | Reprints created URGENT, inheriting the original deadline | Rescheduling | Customer unaware; promise unchanged | P-16 | Silent late delivery | Approved | FR-HUB-003 | E·H·—·UC-013 |
| BR-RESCHED-004 | After 2 reprints escalate to operations | Rescheduling | Repeated failure = systematic issue | P-51 | Unbounded reprint cost | Approved (B14 23/09) | FR-HUB-003, FR-ANAL-004 | C·H·cfg(2)·UC-007/013 |
| BR-QC-001 | Every part inspected at the hub before customer shipment | Quality Control | Platform sells the guarantee | P-08, P-16 | Unproven parts shipped | Approved | FR-HUB-001 | C·H·—·UC-006/016 |
| BR-QC-002 | Inspection follows the checklist bound to the ordered quality grade | Quality Control | Consistent, auditable standards | P-16 | Grade claims unfounded | Approved | FR-HUB-002, FR-ADMIN-005 | C·H·cfg(checklist)·UC-006/024 |
| BR-QC-003 | Photographic evidence for every inspection result | Quality Control | Evidence for disputes | P-16 | Attribution unresolvable | Approved | FR-HUB-002 | C·H·—·UC-006/016 |
| BR-QC-004 | Failed parts attributed to lab, hub or customer before proceeding | Quality Control | Cost and score follow the culprit | P-16 | Wrong party pays; scores lie | Approved | FR-HUB-002 | C·H·—·UC-006/016 |
| BR-QC-005 | Lab-fault failures trigger reprint charged to the lab | Quality Control | Producer accountability | P-16 | Platform absorbs lab waste | Approved | FR-HUB-003 | E·H·—·UC-013 |
| BR-QC-006 | Customer-fault failures notify customer; reprint only on approval | Quality Control | Platform cannot absorb bad geometry | P-16 | Cost leak; liability drift | Approved | FR-HUB-003 | E·H·—·UC-006/013 |
| BR-QC-007 | Hub-fault failures reprinted at platform expense | Quality Control | Hub is platform-controlled | P-16 | Accountability gap | Approved | FR-HUB-003 | E·H·—·UC-013 |
| BR-QC-008 | Order packing blocked until every item passed inspection | Quality Control | Ship complete, good orders only | P-46 | Partial/defective shipments | Specified | FR-HUB-004 | C·H·—·UC-017 |
| BR-QC-009 | Guarantee window = 30 days from delivery confirmation | Quality Control | Clear liability period | P-27 | Endless claim exposure | Specified | FR-CUST-011 | T·H·cfg(30d)·UC-007/017 |
| BR-QC-010 | Missing/damaged-in-transit items go to incident + attribution before reprint | Quality Control | Loss handled deliberately, not silently | P-41 | Lost parts auto-refunded/blamed | Specified | FR-HUB-001 | E·H·—·UC-016 |
| BR-PERF-001 | Lab metrics tracked continuously on a sliding 90-day window | Lab Performance | Recent behavior counts | P-17 | Lifetime averages hide decline | Approved | FR-ANAL-002 | C·H·cfg(90d)·UC-008 |
| BR-PERF-002 | No standing score below 10 completed jobs; neutral 0.70 meanwhile | Lab Performance | Cold-start fairness | P-17 | New labs unfairly starved | Approved | FR-ANAL-002 | C·H·cfg(10)·UC-008 · ⚠B10 |
| BR-PERF-003 | Standing score feeds the assignment score explicitly | Lab Performance | Quality affects opportunity | P-17 | No incentive to perform | Deferred → Should (A9) | FR-SCHED-004 | C·H·cfg(on/off)·UC-011 |
| BR-PERF-004 | Labs see their own metrics and network comparison | Lab Performance | Transparency enables improvement | P-34 | Opaque scoring disputes | Approved | FR-LAB-007 | C·H·—·UC-008 |
| BR-PERF-005 | Score < 0.50 for 30 days suspends new assignments pending review | Lab Performance | Protect customers from weak links | P-52 | Bad labs keep getting work | Approved | FR-ANAL-004 | E·H·cfg(.50/30d)·UC-008/020 |
| BR-PERF-006 | Labs can appeal disputed metrics; ops investigates and corrects | Lab Performance | Fairness; data can be wrong | P-34 | Appeal-less governance | ⚠ dead rule — pending C2 | none yet (email runbook proposed) | C·H·— |
| BR-ESTIM-001 | Operators report actual duration and material on completion | Estimation | Calibration needs ground truth | P-38 | Estimates never improve | Approved | FR-LAB-005 | C·S·—·UC-005 |
| BR-ESTIM-002 | Correction factors computed per machine model, never globally | Estimation | Machines behave differently | P-15 | Systematic bias retained | Approved | FR-SCHED-008 | C·H·—·UC-018 |
| BR-ESTIM-003 | Outliers (actual >3× or <0.3× estimate) excluded from calibration | Estimation | Failures skew regressions | P-15 | Corrupted factors | Approved | FR-SCHED-008 | C·H·cfg(3×/.3×)·UC-018 |
| BR-ESTIM-004 | Factors apply forward to future quoting and scheduling only | Estimation | No retroactive promise changes | P-15 | Committed quotes mutate | Approved | FR-SCHED-008 | E·H·—·UC-010/018 |
| BR-ESTIM-005 | MAPE per machine model monitored; alert above 25 % | Estimation | Calibration must be measured | P-74 | Silent accuracy decay | Approved | FR-ANAL-001 | C·H·cfg(25%)·UC-018/019 |
| BR-ESTIM-006 | Factor proposal requires ≥ 10 valid samples for that model | Estimation | Small samples mislead | P-61 | Noise-driven price/dates | Specified | FR-SCHED-008 | C·H·cfg(10)·UC-018 |
| BR-ACCESS-001 | Model files accessible only within job duration + 4 h grace | Access/IP | Prevent design hoarding by labs | P-18 | IP accumulation | Approved | FR-LAB-005 | C·H·cfg(+4h)·UC-004/005 · ⚠B15 |
| BR-ACCESS-002 | Every URL issuance and download logged (who, when, job, IP) | Access/IP | Accountability and dispute support | P-18 | Leaks untraceable | Approved | FR-ADMIN-004 | C·H·—·UC-025 |
| BR-ACCESS-003 | Reassignment instantly revokes the previous lab's access | Access/IP | No need-to-know afterwards | P-18 | Ex-lab retains files | Approved | FR-SCHED-007, FR-ANAL-004 | E·H·—·UC-020 |
| BR-ACCESS-004 | Per-job access only; bulk download prohibited | Access/IP | Library building forbidden | P-75 | Mass IP theft | Approved | FR-LAB-005 | C·H·—·UC-026 |
| BR-ACCESS-005 | One staff role per account; admins cannot lock/demote themselves | Access/IP | Least privilege; no self-stranding | P-63 | Orphaned admin / mixed duty | Specified | FR-ADMIN-001 | C·H·—·UC-022 |
| BR-ACCESS-006 | Per-customer storage quota (default 1 GB / 20 models) | Access/IP | Cost control; prune guidance | P-28 | Unbounded storage cost | Specified | FR-CUST-002/010 | C·H·cfg(1GB)·UC-026 |
| BR-PAY-001 | Payment completed before order commits to production | Payment | Platform bears fulfillment risk | P-25 | Unpaid production | Approved | FR-CUST-008 | C·H·—·UC-001 |
| BR-PAY-002 | Lab payment = base cost − reprint costs, after hub acceptance | Payment | Quality-contingent incentives | P-04 | (runbook) miscalculated payouts | Approved — settlement runbook (23/09) | out of system; FR-ANAL-003 reports | K·H·cfg(period)·UC-021 |
| BR-PAY-003 | Lab-fault reprint cost deducted from the lab's payment | Payment | Culprit bears cost | P-04 | Moral hazard | Approved — settlement runbook (23/09) | adjustment recorded in-system | K·H·—·UC-013 |
| BR-PAY-004 | Platform-caused delivery failure entitles full refund | Payment | Platform accountable for promises | P-73 | Trust + legal exposure | Approved (in-system via FR-ANAL-005, 23/09) | FR-ANAL-005 | E·H·—·UC-028 |
| BR-PAY-005 | Card/wallet credentials never stored; gateway token + txn id only | Payment | PCI exposure; customer safety | P-25 + 23/09 | Breach; gateway shutdown | Specified | FR-CUST-008 · NFR-SEC-009 | C·H·—·UC-001/028 |
| BR-PAY-006 | Approved refunds execute ≤ 5 business days; idempotent; partial allowed per item | Payment | Close the money loop responsibly | P-73 + 23/09 | Double refund; stale complaints | Specified | FR-ANAL-005 · NFR-SEC-009 | E·H·cfg(5d)·UC-028 |
| BR-OPS-001 | Every manual override carries written justification | Operations | Accountability; learn from interventions | P-50 | Unexplained human edits | Approved | FR-ANAL-004 | C·H·—·UC-020 |
| BR-OPS-002 | Overrides must still satisfy hard feasibility (force only with explicit flag) | Operations | Humans err too | P-50 | Infeasible assignments via UI | Approved | FR-ANAL-004 | C·H·—·UC-020 · ⚠B11 |
| BR-OPS-003 | Suspension immediately reassigns unaccepted jobs | Operations | Urgent protective action | P-52 | Suspended lab still holds work | Approved | FR-ANAL-004 | E·H·—·UC-020 |
| BR-OPS-004 | Ops can cap a lab's concurrent jobs (probation control) | Operations | Gradual trust building | P-52 | Weak labs over-loaded | Approved | FR-ANAL-004 | C·H·cfg(cap)·UC-020 |
| BR-OPS-005 | No assignment to a lab before admin approval of its declaration | Operations | Gate self-interested claims | P-29 | Junk labs absorb demand | Specified | FR-LAB-001, FR-ADMIN-001 | C·H·—·UC-014 |
| BR-OPS-006 | Lab activation requires complete declaration (≥1 machine, ≥1 material, calendar, transfer) | Operations | Partial data breaks the filter | P-29,30,31 | Silent infeasibility | Specified | FR-LAB-001..003 | C·H·—·UC-014 |
| BR-OPS-007 | Job is at-risk when slack < threshold (default 4 h); surfaced on monitor | Operations | Intervene before breaking | P-49 | Silent late promises | Specified | FR-ANAL-001 | C·H·cfg(4h)·UC-019 |
| BR-OPS-008 | Evaluation claims only from runs on a pinned, versioned protocol | Operations | Research integrity | P-82 | Unprovable algorithm claims | Specified | WP harness + FR-ANAL-003 | C·H·—·UC-027 |
| BR-CONFIG-001 | Parameter changes create new versions, never mutate existing | Configuration | Reproducibility | P-65 | History rewrites itself | Approved | FR-ADMIN-002 | C·H·—·UC-009/018/023 |
| BR-CONFIG-002 | All configuration changes audited (user, time, old/new) | Configuration | Debug + accountability | P-65,68 | Untraceable policy edits | Approved | FR-ADMIN-002 | C·H·—·UC-025 |
| BR-CONFIG-003 | Changes apply forward only; committed records keep their version | Configuration | Stability of commitments | P-10,65 | Quotes/decisions mutate | Approved | FR-ADMIN-002 | C·H·—·UC-009/018/023 |
| BR-CONFIG-004 | Audit/decision/evidence/report records retained ≥ 2 academic years, immutable | Configuration | Dispute and review horizon | P-68,77 | Evidence gone when needed | Specified | FR-ADMIN-004 | C·H·cfg(2y)·UC-021/025 |
| BR-NOTIFY-001 | Critical events notify the relevant party promptly | Notification | Information enables action | P-69 | Silent delays | Approved | (system-wide) | E·H·cfg(prefs)·UC-003/012/015–017 |
| BR-NOTIFY-002 | Delivery-date changes require customer notification and approval first | Notification | Consent before commitment move | P-73 | Broken-trust escalations | Approved | FR-ANAL-004, FR-CUST-009 | C·H·—·UC-012/020/028 |

**Total: 76 rules** = 62 inherited from v1.0 documents + 12 specified by the UC-014→027 sync (§13) + 2 payment rules from the 23/09 decision (§13). Status roll-up: 56 Approved · 17 Specified · 3 Deferred (BR-ASSIGN-007, SCHED-002/003 → Should, PERF-003) · 1 ⚠ dead (BR-PERF-006, decision pending C2) · ⚠ open flags on BR-QUOTE-003 (B12), ASSIGN-005 (B5), PERF-002 (B10), ACCESS-001 (B15), OPS-002 (B11).


---

## 1. Quoting Rules (BR-QUOTE)

### BR-QUOTE-001: Network-Wide Price Uniformity
**Rule:** The platform must present a single price for a given print configuration, regardless of which lab will ultimately produce the part.

**Condition:** Customer configures print (material, color, quality, quantity)  
**Action:** System calculates price using uniform pricing formula  
**Exception:** Priority/rush surcharges may apply based on deadline urgency  
**Priority:** Critical  
**Validation:** Price same for identical configs at same time  
**Rationale:** Customer buys from platform, not from individual labs; variable pricing would expose network heterogeneity

---

### BR-QUOTE-002: Capacity-Based Delivery Dates
**Rule:** Delivery dates presented in quotes must be derived from actual network capacity via trial placement, not from fixed lead-time tables.

**Condition:** Customer requests quote  
**Action:** System runs speculative placement to find earliest feasible completion  
**Exception:** If trial placement fails (no feasible lab), inform customer part cannot be produced  
**Priority:** Critical  
**Validation:** Delivery date changes when network load changes  
**Rationale:** Core differentiator; lead-time tables fail under variable load

---

### BR-QUOTE-003: Quote Expiry Time
**Rule:** Every quote must have an expiry time after which the price and delivery date are no longer guaranteed.

**Condition:** Quote generated  
**Action:** Set expiry time (default: 48 hours from quote time)  
**Exception:** None  
**Priority:** High  
**Validation:** Quote marked expired after expiry time  
**Rationale:** Prevents stale quotes when network state changes significantly

---

### BR-QUOTE-004: Quote Parameter Version Lock
**Rule:** Each quote must freeze the pricing parameter version used, ensuring quote reproducibility even after parameters change.

**Condition:** Quote generated  
**Action:** Store parameter version ID with quote  
**Exception:** None  
**Priority:** High  
**Validation:** Quote recalculation uses frozen version, not current  
**Rationale:** Customer expects price stability; auditing requires reproducibility

---

### BR-QUOTE-005: Cost Breakdown Transparency
**Rule:** Quotes must show itemized cost breakdown to the customer.

**Condition:** Quote presented  
**Action:** Display: material cost, print time cost, post-processing, hub handling, shipping  
**Exception:** None  
**Priority:** Medium  
**Validation:** Sum of breakdown equals total price  
**Rationale:** Transparency builds trust and helps customers understand pricing

---

### BR-QUOTE-006: Infeasible Part Rejection
**Rule:** If no lab in the network can produce a part (exceeds all build volumes, unsupported material, etc.), the quote must be rejected immediately with clear reason.

**Condition:** Model uploaded and analyzed  
**Action:** Check feasibility across network; if none feasible, reject with reason  
**Exception:** None  
**Priority:** Critical  
**Validation:** Reject if capability filter returns empty set  
**Rationale:** Prevents accepting orders that cannot be fulfilled

---

### BR-QUOTE-007: Asynchronous Quote Processing
**Rule:** Complex quotes (slicing required) must be processed asynchronously to avoid blocking the user interface.

**Condition:** Model requires slicing (STL/OBJ)  
**Action:** Show progress indicator, process in background, notify on completion  
**Exception:** Simple reorders with cached analysis can be instant  
**Priority:** High  
**Validation:** UI remains responsive during slicing  
**Rationale:** Slicing can take 30-60 seconds for complex models

---

## 2. Assignment Rules (BR-ASSIGN)

### BR-ASSIGN-001: Hard Constraint Filtering
**Rule:** Jobs must only be assigned to labs/machines that satisfy ALL hard constraints (technology, build volume, tolerance, material, state).

**Condition:** Job needs assignment  
**Action:** Filter labs by: technology match, build volume sufficient, tolerance achievable, material in stock, machine operational, lab in good standing  
**Exception:** None - all constraints are mandatory  
**Priority:** Critical  
**Validation:** Assigned machine can physically produce the part  
**Rationale:** Assigning infeasible jobs causes production failure

---

### BR-ASSIGN-002: Multi-Criteria Scoring
**Rule:** Among feasible labs, assignment must be made using weighted multi-criteria scoring.

**Condition:** Multiple labs pass hard filters  
**Action:** Score each on: deadline slack (0.30), current load (0.25), quality history (0.20), transfer time (0.15), internal cost (0.10)  
**Exception:** Exploration allocation may override (see BR-ASSIGN-007)  
**Priority:** Critical  
**Validation:** Assignment goes to highest-scored lab (or top N for random exploration)  
**Rationale:** Balances multiple business objectives beyond simple rules

---

### BR-ASSIGN-003: Scoring Weight Configurability
**Rule:** Assignment scoring weights must be configurable without code redeployment.

**Condition:** Operations manager wants to tune assignment behavior  
**Action:** Update weights via admin interface  
**Exception:** Weights must sum to 1.0  
**Priority:** High  
**Validation:** New weights applied to next assignment, old assignments unchanged  
**Rationale:** Business priorities may shift; should not require deployment

---

### BR-ASSIGN-004: Assignment Decision Logging
**Rule:** Every assignment decision must log the candidate labs considered, their scores, and the final choice.

**Condition:** Job assigned  
**Action:** Store: job_id, timestamp, candidate labs, scores per criterion, chosen lab, reason  
**Exception:** None  
**Priority:** High  
**Validation:** Audit log contains assignment decisions  
**Rationale:** Enables dispute resolution and algorithm debugging

---

### BR-ASSIGN-005: Lab Acceptance Window
**Rule:** Labs must accept or reject assigned jobs within a bounded time window (default: 2 hours).

**Condition:** Job assigned to lab  
**Action:** Notify lab, start timer  
**Exception:** If no response within window, auto-reject and reassign  
**Priority:** High  
**Validation:** Jobs not stuck in "assigned" state indefinitely  
**Rationale:** Prevents deadlock; ensures responsiveness

---

### BR-ASSIGN-006: Rejection Requires Justification
**Rule:** When a lab rejects an assigned job, they must provide a reason from a predefined list.

**Condition:** Lab rejects job  
**Action:** Prompt for reason: material out of stock, machine unavailable, infeasibility discovered, capacity overbooked  
**Exception:** None  
**Priority:** Medium  
**Validation:** All rejections have associated reason  
**Rationale:** Identifies capability filter gaps and lab behavior patterns

---

### BR-ASSIGN-007: Exploration Allocation for Fairness
**Rule:** A configurable percentage (default: 5-10%) of assignments must be allocated randomly among feasible labs to prevent winner-take-all and maintain network diversity.

**Condition:** Assignment decision  
**Action:** With probability P, assign randomly from feasible set instead of top-scored  
**Exception:** Urgent jobs bypass exploration  
**Priority:** Medium  
**Validation:** Over time, even lower-scored labs receive some work  
**Rationale:** Keeps smaller labs engaged; provides data for improving their scores

---

### BR-ASSIGN-008: No Self-Selection by Labs
**Rule:** Labs cannot select which jobs to take; jobs are assigned by the platform.

**Condition:** Job needs assignment  
**Action:** Platform assigns based on scoring  
**Exception:** Lab can reject after assignment (tracked in acceptance rate)  
**Priority:** Critical  
**Validation:** No "job marketplace" browsing interface for labs  
**Rationale:** Prevents adverse selection; platform controls optimization

---

## 3. Scheduling Rules (BR-SCHED)

### BR-SCHED-001: Backward Deadline Scheduling
**Rule:** Jobs must be scheduled backward from customer delivery date through hub operations to derive internal lab due date.

**Condition:** Job assigned to lab/machine  
**Action:** Calculate: delivery_date - shipping_time - packing_time - inspection_time - transfer_time = lab_handover_date; lab_handover_date - print_time - post_processing = start_date  
**Exception:** None  
**Priority:** Critical  
**Validation:** Internal due dates ensure customer deadline met  
**Rationale:** Customer deadline is fixed; work backward to find constraints

---

### BR-SCHED-002: Material/Color Batch Consolidation
**Rule:** Jobs using the same material and color should be batched on the same machine when possible to eliminate changeover time.

**Condition:** Multiple jobs queued for same machine  
**Action:** Group by (material, color), place in batches  
**Exception:** See BR-SCHED-003 (batch size limits)  
**Priority:** High  
**Validation:** Consecutive jobs on machine share material/color when possible  
**Rationale:** Changeover (material swap, purge) takes 15-30 minutes; batching saves time

---

### BR-SCHED-003: Batch Size Limits
**Rule:** Batches must be limited in size (max 5 jobs) and duration (max 24 hours) to prevent blocking urgent work.

**Condition:** Creating batch  
**Action:** Enforce: batch_size ≤ 5 AND batch_duration ≤ 24h  
**Exception:** None  
**Priority:** High  
**Validation:** No batch exceeds limits  
**Rationale:** Large batches delay urgent jobs; limit protects responsiveness

---

### BR-SCHED-004: Urgent Job Bypass
**Rule:** Jobs with internal due dates within urgent threshold (default: 48 hours) must bypass batching and be scheduled individually.

**Condition:** Job internal_due_date - now < 48 hours  
**Action:** Mark as urgent, do not add to batch, schedule at earliest slot  
**Exception:** None  
**Priority:** High  
**Validation:** Urgent jobs not delayed by batches  
**Rationale:** Deadline urgency overrides efficiency

---

### BR-SCHED-005: Bounded Scheduling Computation
**Rule:** Scheduling and rescheduling computations must complete within a time budget (default: 30 seconds for rescheduling, 60 seconds for quote-time).

**Condition:** Scheduling algorithm invoked  
**Action:** Set timer; if timeout, return best solution found so far  
**Exception:** None  
**Priority:** Critical  
**Validation:** Algorithm returns within time budget  
**Rationale:** Production is waiting; late plan worse than slightly suboptimal plan

---

### BR-SCHED-006: Feasible Fallback Guarantee
**Rule:** Scheduling algorithm must always maintain a feasible solution (dispatching rule) as fallback, even if improvement search is incomplete.

**Condition:** Improvement search running  
**Action:** Start with feasible dispatching rule solution; improve if time allows  
**Exception:** None  
**Priority:** Critical  
**Validation:** Algorithm never returns infeasible schedule  
**Rationale:** Feasibility is mandatory; optimality is nice-to-have

---

### BR-SCHED-007: In-Progress Jobs Untouched
**Rule:** Rescheduling must never modify jobs that are currently printing or already completed.

**Condition:** Rescheduling triggered  
**Action:** Lock jobs with status IN_PROGRESS or COMPLETED  
**Exception:** None  
**Priority:** Critical  
**Validation:** Rescheduled jobs are subset of NOT_STARTED jobs  
**Rationale:** Cannot change history; would disrupt production

---

### BR-SCHED-008: Customer Deadline Stability
**Rule:** Once a delivery date is committed to a customer, it may only be moved with customer notification and approval.

**Condition:** Rescheduling would delay customer delivery  
**Action:** Flag for manual review; notify customer; seek approval  
**Exception:** Customer-initiated changes  
**Priority:** Critical  
**Validation:** Committed dates not silently changed  
**Rationale:** Trust erosion if deadlines slip without communication

---

## 4. Rescheduling Rules (BR-RESCHED)

### BR-RESCHED-001: Event-Driven Trigger
**Rule:** Rescheduling must be triggered automatically by production events: job rejection, print failure, machine breakdown, inspection failure requiring reprint, new urgent order.

**Condition:** Production event occurs  
**Action:** Trigger rescheduling workflow  
**Exception:** None  
**Priority:** Critical  
**Validation:** Events logged and rescheduling initiated  
**Rationale:** Manual rescheduling too slow; events invalidate current plan

---

### BR-RESCHED-002: Affected Jobs Only
**Rule:** Rescheduling should only modify jobs affected by the triggering event, leaving unaffected jobs unchanged.

**Condition:** Rescheduling triggered  
**Action:** Identify affected jobs (same machine queue, same deadline dependencies); reschedule only those  
**Exception:** Network-wide events (e.g., new pricing) may affect all  
**Priority:** High  
**Validation:** Unaffected jobs retain original schedule  
**Rationale:** Minimizes schedule nervousness; operators rely on stable plans

---

### BR-RESCHED-003: Priority Reprint Handling
**Rule:** Reprint jobs (from quality failures) must be created with URGENT priority and inherit the original order's deadline.

**Condition:** Inspection failure attributed to lab or hub  
**Action:** Create reprint job with priority=URGENT, deadline=original_deadline  
**Exception:** None  
**Priority:** Critical  
**Validation:** Reprints inserted into schedule with high priority  
**Rationale:** Customer unaware of reprint; deadline commitment unchanged

---

### BR-RESCHED-004: Reprint Limit
**Rule:** If a job has been reprinted more than a threshold (default: 2 times), escalate to operations manager for manual review.

**Condition:** Reprint triggered  
**Action:** Check reprint_count; if > 2, flag for manual review instead of auto-reprint  
**Exception:** None  
**Priority:** High  
**Validation:** Excessive reprints not auto-triggered indefinitely  
**Rationale:** Repeated failures indicate systematic issue requiring human intervention

---

## 5. Quality Control Rules (BR-QC)

### BR-QC-001: Central Hub Inspection Mandatory
**Rule:** All completed parts must pass through central hub inspection before shipment to customer.

**Condition:** Lab ships completed part  
**Action:** Route to hub, perform inspection per quality grade checklist  
**Exception:** None  
**Priority:** Critical  
**Validation:** No part ships directly from lab to customer  
**Rationale:** Platform guarantees quality; cannot delegate to individual labs

---

### BR-QC-002: Quality Grade Checklist Binding
**Rule:** Inspection must follow the checklist defined for the quality grade ordered (draft, standard, high, ultra).

**Condition:** Part arrives at hub for inspection  
**Action:** Load checklist for ordered quality grade, execute all mandatory checks  
**Exception:** None  
**Priority:** Critical  
**Validation:** All checklist items marked pass/fail/N/A  
**Rationale:** Quality standards must be consistent and auditable

---

### BR-QC-003: Photographic Evidence Required
**Rule:** Inspection results (both pass and fail) must include photographic evidence of the part.

**Condition:** Inspection completed  
**Action:** Capture photos from multiple angles, store with inspection record  
**Exception:** None  
**Priority:** High  
**Validation:** Inspection record has associated photos  
**Rationale:** Evidence for dispute resolution and fault attribution

---

### BR-QC-004: Fault Attribution Required on Failure
**Rule:** When a part fails inspection, the fault must be attributed to lab, hub, or customer before proceeding.

**Condition:** Inspection result = FAIL  
**Action:** Classify defect type, determine responsible party  
**Exception:** Ambiguous cases flagged for manual review  
**Priority:** Critical  
**Validation:** All failures have attributed responsibility  
**Rationale:** Determines who pays for reprint and affects performance scores

---

### BR-QC-005: Lab Fault Triggers Reprint at Lab Expense
**Rule:** Parts failing inspection due to lab fault must trigger automatic reprint with cost charged to lab (deducted from payment).

**Condition:** Inspection FAIL + fault=lab  
**Action:** Create reprint job, mark as "lab_expense", update lab's first-pass yield metric  
**Exception:** None  
**Priority:** Critical  
**Validation:** Reprint job created, lab payment adjusted  
**Rationale:** Lab accountable for production quality

---

### BR-QC-006: Customer Fault Notification
**Rule:** Parts failing due to customer-supplied geometry issues must not trigger automatic reprint; customer must be notified and approve paid reprint.

**Condition:** Inspection FAIL + fault=customer  
**Action:** Notify customer, explain issue, offer paid reprint or refund  
**Exception:** None  
**Priority:** High  
**Validation:** No auto-reprint for customer-fault failures  
**Rationale:** Customer responsible for valid geometry; platform cannot absorb cost

---

### BR-QC-007: Hub Fault Platform Cost
**Rule:** Parts failing due to hub handling (damage in transit from lab to hub, handling damage) are platform cost; reprint at platform expense.

**Condition:** Inspection FAIL + fault=hub  
**Action:** Create reprint job, mark as "platform_expense"  
**Exception:** None  
**Priority:** High  
**Validation:** Reprint job created, platform absorbs cost  
**Rationale:** Hub is platform-controlled; platform accountable

---

## 6. Lab Performance Rules (BR-PERF)

### BR-PERF-001: Performance Metrics Continuous Tracking
**Rule:** Lab performance metrics (on-time delivery, first-pass yield, acceptance rate, utilization) must be tracked continuously with sliding time window (default: 90 days).

**Condition:** Job completes any state transition  
**Action:** Update relevant metrics for lab  
**Exception:** None  
**Priority:** High  
**Validation:** Metrics reflect recent performance, not lifetime  
**Rationale:** Recent performance more relevant; labs can improve

---

### BR-PERF-002: Minimum Sample Size for Scoring
**Rule:** Lab performance scores must not be calculated until lab has completed minimum number of jobs (default: 10).

**Condition:** Calculating performance score  
**Action:** If job_count < 10, use neutral score (0.70)  
**Exception:** None  
**Priority:** Medium  
**Validation:** New labs not penalized for insufficient data  
**Rationale:** Statistical validity requires minimum sample

---

### BR-PERF-003: Performance Score Influences Assignment
**Rule:** Lab performance score must be an explicit input to the assignment scoring function (quality history criterion).

**Condition:** Calculating assignment score  
**Action:** Include lab.performance_score in quality_history_score  
**Exception:** None  
**Priority:** Critical  
**Validation:** Higher-performing labs score better, receive more work  
**Rationale:** Incentivizes quality; protects customers

---

### BR-PERF-004: Performance Transparency to Labs
**Rule:** Labs must be able to view their own performance metrics and understand how they compare to network average.

**Condition:** Lab accesses performance dashboard  
**Action:** Display: own metrics, network average (anonymized), trend over time  
**Exception:** None  
**Priority:** Medium  
**Validation:** Lab dashboard shows performance data  
**Rationale:** Transparency enables improvement; builds trust

---

### BR-PERF-005: Lab Suspension on Poor Performance
**Rule:** Labs with performance score below minimum threshold (default: 0.50) for sustained period must be suspended from receiving new assignments pending review.

**Condition:** Performance score < 0.50 for 30 days  
**Action:** Mark lab as suspended, notify lab and operations manager  
**Exception:** Manual reinstatement after review  
**Priority:** High  
**Validation:** Suspended labs receive no new assignments  
**Rationale:** Protects network quality and customer experience

---

### BR-PERF-006: Performance Review Appeal
**Rule:** Labs must have ability to request review of performance metrics if they believe data is incorrect.

**Condition:** Lab disputes performance score  
**Action:** Create review ticket, operations manager investigates, corrects if warranted  
**Exception:** None  
**Priority:** Medium  
**Validation:** Review process exists and documented  
**Rationale:** Fairness; data errors can occur

---

## 7. Estimation and Calibration Rules (BR-ESTIM)

### BR-ESTIM-001: Actual Time Reporting Required
**Rule:** Lab operators must report actual print duration when marking job complete.

**Condition:** Job marked complete  
**Action:** Prompt operator for actual print duration  
**Exception:** Can be estimated if precise time unavailable  
**Priority:** High  
**Validation:** Completed jobs have actual_duration field populated  
**Rationale:** Calibration requires actual data

---

### BR-ESTIM-002: Calibration Per Machine Model
**Rule:** Estimation calibration correction factors must be calculated per machine model (brand + model name), not globally.

**Condition:** Calculating correction factors  
**Action:** Group by machine_model, run regression separately  
**Exception:** If insufficient data for model, use global factor  
**Priority:** High  
**Validation:** Different machine models have different factors  
**Rationale:** Different machines perform differently; generic factor insufficient

---

### BR-ESTIM-003: Outlier Filtering
**Rule:** Extreme outliers (e.g., actual time >3x or <0.3x estimated) must be filtered before calculating calibration factors.

**Condition:** Updating calibration data  
**Action:** Identify outliers using z-score or IQR method, exclude from regression  
**Exception:** Manual review of outliers to identify causes  
**Priority:** Medium  
**Validation:** Outliers logged but not used in calibration  
**Rationale:** Outliers (operator error, print failure) skew calibration

---

### BR-ESTIM-004: Forward Application of Calibration
**Rule:** Calibration correction factors must be applied to future estimates at quoting and scheduling time.

**Condition:** Estimating print time for job  
**Action:** Get raw slicer estimate, apply correction factor for machine model  
**Exception:** If no calibration data, use raw estimate  
**Priority:** High  
**Validation:** Calibrated estimates more accurate than raw over time  
**Rationale:** Learning from history improves future promises

---

### BR-ESTIM-005: Calibration Monitoring
**Rule:** Prediction error (MAPE: Mean Absolute Percentage Error) must be tracked as a monitored quality metric.

**Condition:** Continuous monitoring  
**Action:** Calculate MAPE weekly, alert if exceeds threshold (e.g., >25%)  
**Exception:** None  
**Priority:** Medium  
**Validation:** MAPE visible on operations dashboard  
**Rationale:** Calibration effectiveness must be measurable

---

## 8. File Access and IP Protection Rules (BR-ACCESS)

### BR-ACCESS-001: Time-Limited Model Access
**Rule:** Lab access to customer 3D model files must be limited to the duration of job assignment plus a small buffer (default: job duration + 4 hours).

**Condition:** Lab assigned job  
**Action:** Generate pre-signed URL with expiry = expected_end_time + 4 hours  
**Exception:** None  
**Priority:** Critical  
**Validation:** URL expires after window; new URL cannot be generated  
**Rationale:** Protects customer IP from accumulation by labs

---

### BR-ACCESS-002: Access Logging Mandatory
**Rule:** Every generation of model file access URL and every file download must be logged with timestamp, job ID, lab ID, and IP address.

**Condition:** Model access requested or file downloaded  
**Action:** Write to audit log  
**Exception:** None  
**Priority:** Critical  
**Validation:** Complete audit trail of file access  
**Rationale:** Accountability; investigation capability for IP disputes

---

### BR-ACCESS-003: Revocation on Reassignment
**Rule:** If a job is reassigned from one lab to another, the original lab's file access must be immediately revoked.

**Condition:** Job reassigned  
**Action:** Mark original lab's access URL as revoked, cannot generate new one  
**Exception:** None  
**Priority:** Critical  
**Validation:** Original lab cannot access file after reassignment  
**Rationale:** No need-to-know after reassignment

---

### BR-ACCESS-004: No Bulk Download
**Rule:** Labs must not be able to download multiple customer models in bulk; access is strictly per-job.

**Condition:** File access request  
**Action:** Verify request is for assigned job only  
**Exception:** None  
**Priority:** Critical  
**Validation:** API enforces one file per assigned job  
**Rationale:** Prevents IP hoarding

---

## 9. Payment and Financial Rules (BR-PAY)

### BR-PAY-001: Payment on Order Confirmation
**Rule:** Customer must complete payment before order is committed and enters production.

**Condition:** Customer confirms quote  
**Action:** Process payment; if successful, commit order; if failed, quote remains uncommitted  
**Exception:** None (no credit/invoicing for MVP)  
**Priority:** Critical  
**Validation:** No production starts without payment  
**Rationale:** Platform carries fulfillment risk; payment upfront

---

### BR-PAY-002: Lab Payment on Completion
**Rule:** Labs are paid for completed jobs after successful hub inspection, minus any costs for failed parts.

**Condition:** Job passes inspection or completes fulfillment cycle  
**Action:** Calculate lab payment = base_cost - reprint_costs; mark for payment  
**Exception:** Payment frequency (weekly/monthly) is operational detail  
**Priority:** High  
**Validation:** Labs paid only for accepted work  
**Rationale:** Quality-contingent payment incentivizes quality

---

### BR-PAY-003: Reprint Cost Deduction
**Rule:** If a reprint is required due to lab fault, the cost of the reprint must be deducted from the lab's payment for the original job.

**Condition:** Reprint triggered, fault=lab  
**Action:** Calculate reprint_cost, deduct from lab's payment for original job  
**Exception:** None  
**Priority:** High  
**Validation:** Lab payment adjusted for reprints  
**Rationale:** Lab bears cost of their quality failures

---

### BR-PAY-004: Refund on Platform Failure
**Rule:** If platform cannot fulfill an order within promised delivery date due to platform issues (not force majeure), customer is entitled to full refund.

**Condition:** Delivery date missed, fault=platform  
**Action:** Issue full refund, notify customer  
**Exception:** Customer-caused delays, customer approves extension  
**Priority:** High  
**Validation:** SLA breach triggers refund review  
**Rationale:** Platform accountable for promises made

---

## 10. Operational Intervention Rules (BR-OPS)

### BR-OPS-001: Manual Override Justification Required
**Rule:** Any manual override of automated assignment or scheduling decisions must include a written justification.

**Condition:** Operations manager overrides system  
**Action:** Prompt for justification text, store with override record  
**Exception:** None  
**Priority:** High  
**Validation:** All overrides have justification in audit log  
**Rationale:** Accountability; learning from interventions

---

### BR-OPS-002: Override Does Not Invalidate Feasibility
**Rule:** Manual assignment overrides must still satisfy hard capability constraints.

**Condition:** Operations manager manually assigns job to lab  
**Action:** Run capability filter; if fails, warn and require confirmation  
**Exception:** Can force with explicit override flag  
**Priority:** High  
**Validation:** Overrides usually feasible  
**Rationale:** Prevents errors; humans make mistakes too

---

### BR-OPS-003: Lab Suspension Immediate Effect
**Rule:** When a lab is suspended, all assigned but not-yet-accepted jobs must be immediately reassigned.

**Condition:** Lab marked as suspended  
**Action:** Retrieve jobs in ASSIGNED state, trigger reassignment  
**Exception:** Jobs already IN_PROGRESS continue  
**Priority:** High  
**Validation:** Suspended labs receive no work  
**Rationale:** Suspension is urgent action; must take effect immediately

---

### BR-OPS-004: Capacity Capping
**Rule:** Operations manager can set a maximum number of concurrent jobs for a lab (capacity cap).

**Condition:** Lab under observation or probationary  
**Action:** Enforce max_concurrent_jobs limit in assignment  
**Exception:** Cap can be removed after review  
**Priority:** Medium  
**Validation:** Lab never assigned more than cap  
**Rationale:** Gradual trust building; risk mitigation

---

## 11. Configuration and Admin Rules (BR-CONFIG)

### BR-CONFIG-001: Parameter Changes Create New Version
**Rule:** Changes to pricing parameters, scoring weights, or other configuration must create a new version, not modify existing.

**Condition:** Admin updates configuration  
**Action:** Create new version with timestamp and user ID; existing records reference old version  
**Exception:** None  
**Priority:** Critical  
**Validation:** Old quotes/decisions remain valid under their version  
**Rationale:** Immutability ensures reproducibility

---

### BR-CONFIG-002: Configuration Audit Trail
**Rule:** All configuration changes must be logged with user, timestamp, old value, and new value.

**Condition:** Configuration updated  
**Action:** Write to audit log  
**Exception:** None  
**Priority:** High  
**Validation:** Complete history of configuration changes  
**Rationale:** Accountability; debugging unexpected behavior

---

### BR-CONFIG-003: No Retroactive Application
**Rule:** Configuration changes apply only to new decisions made after the change, never retroactively to existing orders or assignments.

**Condition:** Configuration changed  
**Action:** New config_version_id for future; existing records unchanged  
**Exception:** None  
**Priority:** Critical  
**Validation:** Existing orders not affected by config changes  
**Rationale:** Stability; customer expectations based on rules at order time

---

## 12. Notification Rules (BR-NOTIFY)

### BR-NOTIFY-001: Critical Events Require Notification
**Rule:** Critical events (order status changes, job assignments, quality failures, delivery delays) must trigger notifications to relevant stakeholders.

**Condition:** Critical event occurs  
**Action:** Send notification via appropriate channel (email, in-app, SignalR)  
**Exception:** User can configure notification preferences  
**Priority:** High  
**Validation:** Notifications sent and logged  
**Rationale:** Timely information enables action

---

### BR-NOTIFY-002: Customer Delivery Date Change Approval
**Rule:** If a delivery date must be changed after commitment, customer must be notified and approval obtained before finalizing.

**Condition:** Rescheduling would delay delivery  
**Action:** Notify customer, explain reason, offer alternatives, await approval  
**Exception:** Customer-initiated changes  
**Priority:** Critical  
**Validation:** No date changes without customer consent  
**Rationale:** Trust; customer may have dependencies on date

---

## 13. Rules bổ sung khi đồng bộ UC-014→027 (v1.1 sync)

### BR-SCHED-009: Lab Handover Deadline
**Rule:** Completed parts must be recorded as handed over to hub transport within 24 hours of job completion.
**Condition:** Job status = COMPLETED at lab
**Action:** Start handover timer; "Report Handover" (UC-015) stops it
**Exception:** Soft — overdue raises alert to lab manager and operations; repeated overdue feeds performance ledger
**Priority:** High · **Type:** Constraint (Mềm) · **Variability:** cfg · **Source:** P-40 · **Enforced by:** FR-LAB-005 · **Test:** TC for UC-015

### BR-QC-008: Completeness Gate Before Packing
**Rule:** An order may not be packed until every item of the order has passed hub inspection; packing is blocked with an explicit outstanding-item report.
**Condition:** Fulfillment staff initiates pack (UC-017)
**Action:** Verify all order items status = PASSED_QC
**Exception:** None — partial shipment is out of scope
**Priority:** Critical · **Type:** Constraint (Cứng) · **Source:** P-46 · **Enforced by:** FR-HUB-004

### BR-QC-009: Guarantee Window Definition
**Rule:** The reprint/complaint window starts at delivery confirmation and lasts 30 days; requests outside the window are rejected.
**Condition:** Customer opens completed order
**Action:** Compare now − DeliveredAt ≤ 30 days
**Exception:** Operations manager may grant a one-time extension (audited)
**Priority:** High · **Type:** Term (Cứng) · **Variability:** cfg · **Source:** P-27 · **Enforced by:** FR-CUST-011 · **Test:** TC-UC-007 A1

### BR-QC-010: Missing / Transit-Damage Routing
**Rule:** Items flagged MISSING or DAMAGED-IN-TRANSIT at batch receipt are routed to incident handling and attributed (packaging fault = lab; transport fault = hub/platform) before any reprint decision.
**Condition:** Reconciliation (UC-016) flags discrepancy
**Action:** Create incident, notify lab + ops, hold reprint until attribution
**Exception:** None
**Priority:** High · **Type:** Action enabler (Cứng) · **Source:** P-41 · **Enforced by:** FR-HUB-001

### BR-ESTIM-006: Calibration Minimum Sample
**Rule:** A correction factor may only be proposed for a machine model with ≥ 10 valid (non-outlier) samples in the window; below that, the previous factor or global default is retained.
**Condition:** Calibration review (UC-018)
**Action:** Enforce sample count gate before recommendation
**Priority:** Medium · **Type:** Constraint (Cứng) · **Variability:** cfg · **Source:** P-61 · **Enforced by:** FR-SCHED-008

### BR-ACCESS-005: Staff Role Exclusivity & Self-Lockout Guard
**Rule:** Each staff account (lab, hub, ops, admin) holds exactly one role; an administrator cannot deactivate or demote their own account.
**Condition:** Role mutation in admin UI (UC-022)
**Action:** Validate role cardinality = 1; block target == actor
**Priority:** High · **Type:** Constraint (Cứng) · **Source:** P-63 · **Enforced by:** FR-ADMIN-001

### BR-ACCESS-006: Per-Customer Storage Quota
**Rule:** Each customer account has a model storage quota (default 1 GB / 20 models); uploads beyond quota are blocked with guidance to prune the library.
**Condition:** Upload (UC-026/UC-001)
**Action:** Check quota before accepting
**Priority:** Medium · **Type:** Constraint (Cứng) · **Variability:** cfg · **Source:** P-28 · **Enforced by:** FR-CUST-002/010

### BR-OPS-005: Lab Onboarding Approval
**Rule:** A registered lab receives no assignments until a Platform Administrator approves its declaration; approval/rejection is notified and audited.
**Condition:** Lab submits registration (UC-014)
**Action:** Status PENDING_APPROVAL → ACTIVE only via admin
**Priority:** Critical · **Type:** Constraint (Cứng) · **Source:** P-29 · **Enforced by:** FR-LAB-001 + FR-ADMIN-001

### BR-OPS-006: Declaration Completeness
**Rule:** Lab activation requires a complete declaration: ≥ 1 machine with full spec (technology, build volume, layer height, tolerance) and ≥ 1 material stock entry, plus working calendar and hub transfer time.
**Condition:** Submission/approval validation
**Priority:** High · **Type:** Constraint (Cứng) · **Source:** P-29, P-30, P-31 · **Enforced by:** FR-LAB-001/002/003

### BR-OPS-007: At-Risk Threshold
**Rule:** A job is "at risk" when remaining slack to its internal due date falls below the configurable threshold (default 4 hours); at-risk items must surface on the operations monitor.
**Condition:** Monitoring cycle / dashboard refresh (UC-019)
**Priority:** High · **Type:** Computation+Constraint (Cứng) · **Variability:** cfg · **Source:** P-49 · **Enforced by:** FR-ANALYTICS-001

### BR-OPS-008: Evaluation Protocol Immutability
**Rule:** Algorithm evaluation claims may only derive from simulation runs executed against a pinned, versioned evaluation protocol (baselines, workload set, metric definitions); protocol changes after runs are invalidation events requiring re-run.
**Condition:** Producing entries for the Algorithm Evaluation Report (UC-027)
**Priority:** High · **Type:** Constraint (Cứng) · **Source:** P-82 · **Enforced by:** WP2/WP5 harness + FR-ANALYTICS-003 pipeline

### BR-CONFIG-004: Retention of Audit & Report Records
**Rule:** Audit entries, assignment decision logs, inspection evidence and generated reports are retained ≥ 2 academic years and are immutable; export/review actions are themselves logged.
**Condition:** Record lifecycle (UC-021/025)
**Priority:** High · **Type:** Constraint (Cứng) · **Variability:** cfg · **Source:** P-68, P-77 · **Enforced by:** FR-ADMIN-004 · **NFR link:** NFR-SEC/REL retention

### BR-PAY-005: Card Data Never Touches the Platform ★ (quyết định 23/09 — payment tiền thật)
**Rule:** All card/wallet credentials are handled exclusively by the payment gateway's hosted page or tokenised fields; the platform stores only the gateway token, transaction id and payment state — never PAN/CVV/expiry.
**Condition:** Customer pays (UC-001 step 19)
**Action:** Redirect/hosted checkout; webhook signature verified before any order transition
**Exception:** None — hard security constraint; a build that persists raw card data fails review gate
**Priority:** Critical · **Type:** Constraint (Cứng) · **Source:** P-25 + decision 23/09 · **Enforced by:** FR-CUST-008 · **NFR link:** NFR-SEC-009 · **Test:** DB inspection + webhook replay suite
**Gateway pattern (SEPay, gate 25/09):** payment is committed only when BOTH the webhook arrives AND the order-confirmation API call matches amount + transaction description; the browser return URL is navigation evidence only and must never move order state.

### BR-PAY-006: Refund SLA
**Rule:** Approved refunds execute against the gateway within 5 business days of the decision (BR-PAY-004 trigger); partial refunds allowed per item; each refund references the original transaction id and is audited.
**Condition:** Operations manager approves refund (UC-028)
**Priority:** High · **Type:** Action enabler + Computation (Cứng) · **Variability:** cfg (5 ngày) · **Source:** P-73 + decision 23/09 · **Enforced by:** FR-ANAL-005 · **Test:** refund timing + idempotency (no double refund)

---

## Summary

**Total Business Rules: 76** (62 gốc + 12 bổ sung v1.1 + 2 rules payment 23/09; số "71" trước đây là lỗi đếm — thực tế bản cũ có 62 rule được đánh mã)

| Domain | Count | Critical | High | Medium |
|--------|-------|----------|------|--------|
| Quoting | 7 | 4 | 2 | 1 |
| Assignment | 8 | 4 | 3 | 1 |
| Scheduling | 9 | 6 | 3 | 0 |
| Rescheduling | 4 | 3 | 1 | 0 |
| Quality Control | 10 | 6 | 4 | 0 |
| Lab Performance | 6 | 1 | 3 | 2 |
| Estimation/Calibration | 6 | 0 | 4 | 2 |
| File Access/IP | 6 | 5 | 1 | 0 |
| Payment/Financial | 6 | 3 | 3 | 0 |
| Operational Intervention | 8 | 1 | 6 | 1 |
| Configuration/Admin | 4 | 3 | 1 | 0 |
| Notification | 2 | 2 | 1 | 0 |

**Implementation Priority:**
1. ✅ **Critical:** Must implement in MVP
2. ✅ **High:** Should in MVP, must in Extended
3. 🔄 **Medium:** Extended scope / future enhancement

**Vi phạm guideline cần xử lý ở B6-sync (ghi trong `capstone/workbook/03_Audit`):**
- 74 rules vượt khuyến nghị 15–40 của tài liệu quy trình — chấp nhận vì domain phức tạp, nhưng **rà trùng** khi dịch sang bảng `04_BusinessRules` của workbook (ứng viên gộp: các rule notification lặp trong QC/ACCESS flows)
- **Rule chết còn lại: 1** — BR-PERF-006 (lab appeal) chưa có FR; đề xuất giữ dạng quy trình email-manual ghi runbook (mục C2 biên bản họp). **Đã giải quyết 23/09:** BR-PAY-004 nay có FR-ANAL-005 + UC-028 (quyết định payment tiền thật); BR-PAY-002/003 xác nhận settlement ngoài hệ thống — runbook có chủ đích, không còn là lỗi truy vết.
