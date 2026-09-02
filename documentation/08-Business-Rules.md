# Business Rules - PrintGrid Platform

## Overview
This document defines the business rules that govern PrintGrid platform operations. Business rules are categorized by domain and assigned unique identifiers for traceability.

**Format:** `BR-[DOMAIN]-[NUMBER]: [Rule Statement]`

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

## Summary

**Total Business Rules: 71**

| Domain | Count | Critical | High | Medium |
|--------|-------|----------|------|--------|
| Quoting | 7 | 4 | 2 | 1 |
| Assignment | 8 | 4 | 3 | 1 |
| Scheduling | 8 | 6 | 2 | 0 |
| Rescheduling | 4 | 3 | 1 | 0 |
| Quality Control | 7 | 5 | 2 | 0 |
| Lab Performance | 6 | 1 | 3 | 2 |
| Estimation/Calibration | 5 | 0 | 3 | 2 |
| File Access/IP | 4 | 4 | 0 | 0 |
| Payment/Financial | 4 | 2 | 2 | 0 |
| Operational Intervention | 4 | 0 | 3 | 1 |
| Configuration/Admin | 3 | 3 | 1 | 0 |
| Notification | 2 | 2 | 1 | 0 |

**Implementation Priority:**
1. ✅ **Critical (39 rules)**: Must be implemented in MVP
2. ✅ **High (23 rules)**: Should be implemented in MVP, must be in Extended
3. 🔄 **Medium (9 rules)**: Extended scope or future enhancement

All business rules are **testable**, **traceable**, and **aligned with functional requirements**.
