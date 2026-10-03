# Use Cases - PrintGrid Platform

## Overview
This document details the use cases for PrintGrid platform, describing interactions between actors and the system. Each use case includes:
- Use Case ID, Name, and Description
- Actors (Primary and Secondary)
- Preconditions and Postconditions
- Main Flow (Happy Path)
- Alternate Flows (Exceptions and Variations)
- Business Rules Referenced

> **Change note (sync v1.1 workbook):** UC-014→UC-026 added to close the gaps flagged in
> `capstone/workbook/07_Traceability.md` ("UC mới" markers). The legacy diagram planned UC-020/029/031/033/
> 034/035 that were never detailed — those intents are now realized under the final numbering below
> (map: Fulfill→UC-017, Manage Users→UC-022, Manage Catalogs→UC-023, Monitor→UC-019, Override→UC-020,
> Reports→UC-021). All former **[BR?]** placeholders were resolved in the Step-6 sync — the 12 new
> rules now live in 08-Business-Rules.md section 13 (BR-OPS-005/006/007/008, BR-QC-008/009/010,
> BR-SCHED-009, BR-ESTIM-006, BR-ACCESS-005/006, BR-CONFIG-004).

---

## 0. Use Case Matrix (submission format)

Full specifications live in §UC-001…UC-028 below; this index carries one row per use case.
**Status legend:** `Approved` = within register v1.0 scope / decisions confirmed · `Specified` = created by
the v1.1 sync, to be frozen in register v1.2 · `⚠` = open item (see `capstone/workbook/04`).

| Use Case ID | Use Case Name | Actor | Description | Preconditions | Main Flow | Alternate Flow | Postconditions | Related Req. | Priority | Status |
|---|---|---|---|---|---|---|---|---|---|---|
| UC-001 | Place Order for 3D Printing | Customer | Upload model, configure, receive capacity-based quote, pay with real gateway, place order | Registered, authenticated account | Upload → validate → preview → config → quote (UC-002/010) → checkout → webhook payment → order → trigger assignment (24 steps) | A1 invalid format · A2 mesh issues · A3 oversize · A4 quote expired · A5 payment failed | Order committed & paid; jobs enter assignment; confirmation sent | FR-CUST-002..008; FR-SCHED-005/010; BR-QUOTE-001..006, PAY-001/005 | Must | Approved (payment real, 23/09) |
| UC-002 | Analyze Model Geometry | System (Slicing Service) | Parse mesh, compute bbox/volume, check watertight/manifold, validate against network capacity | Valid model file uploaded | Receive file → parse → extract mesh → bbox → volume → integrity checks → network check → store analysis | A1 corrupted file · A2 exceeds all machines → INFEASIBLE | Analysis stored with model; status returned | FR-SCHED-001/002; FR-CUST-004; BR-QUOTE-006 | Must | Approved |
| UC-003 | Track Order | Customer | Real-time status view without lab identity | Authenticated; order exists | List orders → detail → stages/timeline → live updates (SignalR) | A1 order delayed → badge + notification | Customer informed; no lab exposure | FR-CUST-009; BR-NOTIFY-001, ACCESS-* | Must | Approved |
| UC-004 | Lab Accepts Job Assignment | Lab Manager | Review assigned job, accept or decline with reason within window | Lab approved; machines registered; job assigned | Notify → detail review → accept → queue entry + capacity update + file link | A1 decline w/ reason → UC-012 · A2 no response in 2 h → auto-decline → UC-012 | Job accepted into queue or reassigned; metrics updated | FR-LAB-004; BR-ASSIGN-005/006; BR-ACCESS-001/002 | Must | Approved (auto-decline ⚠B5) |
| UC-005 | Execute Print Job | Lab Operator | Run job on machine, report actual duration/material or incident | Job accepted; materials available | Queue → download (time-limited) → prepare → start → print → post-process → report actuals → notify hub | A1 print failure → incident + photos → UC-012 · A2 machine breakdown → offline → queue-wide UC-012 | Job COMPLETED with actuals (calibration feed) or incident recorded | FR-LAB-005; BR-ESTIM-001; BR-SCHED-007 | Must | Approved |
| UC-006 | Inspect Part Quality | Hub QC Staff | Run grade-bound checklist on received part, record result with evidence | Part received & reconciled (UC-016) | Select job → load checklist → step checks → photos → pass/fail → yield update | A1 fail → defect class + attribution (lab/hub/customer) → UC-013 or customer notify | Inspection result stored w/ evidence; lab yield updated | FR-HUB-002; BR-QC-002..004 | Must | Approved |
| UC-007 | Customer Requests Reprint | Customer | Raise post-delivery defect complaint within guarantee window | Delivered ≤ 30 days (BR-QC-009) | Request form → photos → ops review → approval → URGENT reprint inheriting deadline | A1 outside window → rejected · A2 denied w/ explanation (→ refund path UC-028) | Reprint job created or request closed with reason | FR-CUST-011; FR-HUB-003; BR-QC-006, RESCHED-004 | Must | Approved |
| UC-008 | Lab Manager Views Performance | Lab Manager | Inspect own standing metrics and improvement areas | Lab has completed jobs | Dashboard → 90-day metrics → network comparison (anonymized) → trends | A1 < 10 jobs → preliminary, no score · A2 score < 0.50 sustained → review notice | Lab informed; transparency satisfied | FR-LAB-007; FR-ANAL-002; BR-PERF-001..005 | Should | Approved |
| UC-009 | Admin Configures Pricing | Platform Administrator | Edit pricing parameters, versioned and audited | Admin authenticated | Current params → edit → validate → preview impact → save new version → audit | A1 validation fails → keep draft | New version active for future quotes; old quotes frozen | FR-ADMIN-002; FR-SCHED-010; BR-CONFIG-001..003, QUOTE-001 | Must | Approved |
| UC-010 | System Runs Speculative Scheduling | Scheduling Engine | Trial placement at quote time to derive earliest feasible date | Model estimated; config selected | Read network state → filter+score (UC-011 steps) → trial place → backward due-date → compute date + surcharge | A1 no feasible lab → reject quote · A2 cannot meet required date → offer earliest | Credible date+price returned; nothing committed | FR-SCHED-005; BR-QUOTE-002, SCHED-001; NFR-PERF-001 | Must | Approved (crude-first A5) |
| UC-011 | System Assigns Job to Lab | Scheduling Engine | Assign confirmed jobs: hard filter → score → place on machine; full decision log | Order confirmed+paid; decomposed into jobs | Capability filter → feasible set → scoring → select → internal due date → machine placement → log + notify lab (2 h window) | A1 no feasible lab → escalate ops · A2 exploration pick (Could) · A3 urgent bypass | Job assigned to (lab, machine); decision reconstructible | FR-SCHED-003/004/006/009; BR-ASSIGN-001..007, SCHED-*; NFR-REL-004/005 | Must | Approved (exploration → Could) |
| UC-012 | System Reschedules on Event | Scheduling Engine | Repair unstarted plan after rejection/failure/breakdown/reprint within time budget | Production event occurs | Identify affected → lock in-progress → dispatching fallback → improve ≤ 30 s → commit → notify → log | A1 customer deadline must change → ops + customer approval (B16 list) · A2 machine breakdown → reassign whole queue | Repaired schedule; committed promises untouched | FR-SCHED-007; BR-RESCHED-001..004, SCHED-005..008; NFR-PERF-002/REL-006 | Must | Approved (B4 23/09) |
| UC-013 | System Creates Reprint Job | System | Auto-create URGENT reprint inheriting original deadline on lab/hub fault | UC-006 fail; attribution ≠ customer | Failure event → create job (copy config, inherit deadline, cost flag) → ≤2 check → assign via UC-011 → notify | A1 customer fault → offer paid reprint/refund · A2 limit exceeded → ops escalation | Reprint in schedule; original marked REPRINTING | FR-HUB-003; BR-QC-005..007, RESCHED-003/004 | Must | Approved (limit=2, B14) |
| UC-014 | Onboard Lab, Machines & Inventory | Lab Manager, Platform Administrator | Register lab, declare machines/stock, admin approves; becomes allocatable | LAB_PENDING account | Profile+calendar+transfer → machines → materials → completeness → PENDING → admin approve → ACTIVE | A1 rejected w/ reason → resubmit · A2 later spec edits audited for declared-vs-delivered | Lab live in capability filter | FR-LAB-001..003; BR-OPS-005/006 | Must | Approved (partner lab confirmed 23/09) |
| UC-015 | Hand Over Completed Parts to Hub | Lab Operator | Record batch handover with manual waybill; hub informed | Jobs COMPLETED at lab | Select completed → batch + QR → carrier/waybill manual → IN_TRANSIT → notify hub → audit | A1 mis-packed item removed pre-departure | Batch expected at hub; traceable handover | FR-LAB-005 (handover); BR-SCHED-009, QC-001 | Must | Approved |
| UC-016 | Receive & Reconcile Hub Batch | Hub QC Staff | Verify physical receipt against expected jobs | Batch expected (UC-015) | Scan batch QR → expected list → confirm/flag RECEIVED-MISSING-DAMAGED → close receipt → move to inspection | A1 MISSING → incident + notify · A2 DAMAGED-in-transit → photos, attribution → UC-013 | Batch reconciled; inspection queue set | FR-HUB-001; BR-QC-001/003/004/010 | Must | Approved |
| UC-017 | Consolidate, Pack & Ship Order | Hub Fulfillment Staff | Gather order items, pack, ship, confirm delivery, open guarantee window | All items PASSED_QC | Open order → scan items → completeness gate → packing list → waybill manual → SHIPPED + notify → DELIVERED → guarantee clock starts | A1 incomplete set → block + outstanding report (reprint pending) | Order shipped/delivered; lifecycle closed | FR-HUB-004/005; BR-QC-008/009, NOTIFY-001 | Must | Proposed (FR-HUB-005 pending v1.2) |
| UC-018 | Update Estimate Calibration Factors | Platform Administrator (Scheduler triggers review) | Review per-model estimate-vs-actual gaps, apply correction factors (MVP manual) | ≥10 valid samples per model | Aggregate samples → weekly review task → show gap/trend → propose factor → preview re-quote → save version → engines consume | A1 insufficient samples → keep previous factor | New factor version active; history frozen | FR-SCHED-008; BR-ESTIM-002..006, CONFIG-001/003; NFR-ACC-001 | Should | Approved (A7 MVP manual) |
| UC-019 | Monitor Network & At-Risk Orders | Operations Manager | KPI overview + at-risk list with drill-down | Ops authenticated | KPI cards → at-risk (slack < cfg) → sort → drill into order lifecycle → act or acknowledge | A1 reprint-limit orders panel → link to resolution | Risk visible; intervention initiated | FR-ANAL-001; BR-OPS-007, RESCHED-004 | Should | Approved |
| UC-020 | Override Assignment, Priority & Lab Standing | Operations Manager | Manual reassign/prioritize/suspend/cap with reason, impact preview, audit | Ops authenticated | Select object → action (reassign/reorder/priority/suspend/cap) → reason + impact preview → confirm → UC-012 where needed → log + notify | A1 committed customer date affected → 2-step approval + notification | Decision changed with full trail | FR-ANAL-004; BR-OPS-001..004, SCHED-007/008, ACCESS-003 | Should | Approved (force-override ⚠B11) |
| UC-021 | Generate & Export SLA / Performance Reports | Operations Manager | Period reports per network/lab/material with export | Completed data in period | Pick period/scope → aggregate metrics → filters → render → export + audit | A1 empty period → explicit no-data (not zeros) | Reports exported; same pipeline feeds evaluation on simulated data | FR-ANAL-003; BR-CONFIG-004, OPS-008 | Should | Approved |
| UC-022 | Manage Users, Roles & Permissions | Platform Administrator | CRUD accounts, role assignment, lockout | Admin authenticated | List/search → create/edit role/state → exclusivity validation → revoke sessions → audit | A1 self-lockout attempt → blocked | Access controlled; mutations audited | FR-ADMIN-001; BR-ACCESS-005 | Must | Approved |
| UC-023 | Manage Shared Catalogs | Platform Administrator | Maintain materials, colours, technologies, grades, post-processing | Admin authenticated | Open catalogue → edit entry → uniqueness+rate-link validation → deprecate (no delete in use) → audit + notify pricing | A1 deprecate in-use → new-quotes-only block, history preserved | Reference data consistent for pricing/filter/QC | FR-ADMIN-003; BR-CONFIG-001/003, QUOTE-001 | Must | Approved |
| UC-024 | Manage Inspection Checklists & Defect Taxonomy | Platform Administrator | Author grade-bound checklists and taxonomy; versioned | Grades exist (UC-023) | Select grade → edit checklist items/criteria → save version → running inspections keep version; taxonomy add/deprecate | A1 taxonomy code referenced → DEPRECATED only, delete blocked | QC knowledge base versioned and attributable | FR-ADMIN-005; BR-QC-002/004, CONFIG-001/003 | Should | Proposed (FR-ADMIN-005 pending v1.2) |
| UC-025 | Review Audit Log & System Health | Platform Administrator | Read-only immutable audit + queue/storage health, replay failed jobs | Admin authenticated | Filter audit → view before/after → open health (queue depth, failures, storage, integrations) → idempotent replay → actions logged | A1 repeated failure pattern → runbook escalation | Anomalies acknowledged/escalated; no log mutation | FR-ADMIN-004; BR-CONFIG-002/004, ACCESS-002; ⚠ P-67 scope | Should | Draft ⚠C3 (health vs runbook) |
| UC-026 | Manage Account, Addresses & Model Library | Customer | Self-service profile, addresses, library with one-click reorder | Authenticated session | Profile edit + re-auth → address CRUD (delete guard) → library list → reorder loads model+config → quota check | A1 quota reached → block + prune guidance | Account data current; models private per IP rules | FR-CUST-001/010; BR-ACCESS-001/002/006 | Must/Could split | Approved |
| UC-027 | Configure & Run Network Simulation (Algorithm Evaluation) | Evaluator (WP1/WP2 research role) | **Order generator + fault injection harness driving the real engines** to produce measured evaluation evidence | Engines in simulation mode; seed chosen | Configure network (50 labs/300 machines, declared-vs-delivered bias) → **order stream (Poisson rate, size/grade mix, deadline tightness)** → **fault injection: decline rate, print failure with stage distribution, machine breakdown+MTTR, inspection failure per lab** → pick 4 strategies → run virtual clock → metrics (tardiness, on-time %, Gini…) → export to UC-021 | A1 seed non-reproducible → run quarantined · A2 saturated-load collapse → kept as stress scenario · A3 time-budget breach counted as NFR metric | Reproducible metric dataset for Algorithm Evaluation Report + demo scenario | Register e)-Practical 8–9, f)-P-92/93; BR-OPS-008, SCHED-005; NFR-TEST-003 | Must (research) | Specified (WP5 harness due wk 4) |
| UC-028 | Process Refund & Reconciliation | Operations Manager | Approve and execute real refunds via gateway with idempotency and daily reconciliation | Order paid via real gateway; trigger per BR-PAY-004/B16 | Refund queue → scope full/partial per item → reason + approval → idempotent gateway call → webhook confirm → state + notify → T-1 reconciliation → audit | A1 gateway error → safe retry, no silent commit · A2 reconciliation mismatch → auto-freeze txn · A3 5-day SLA breach → escalation | Refund executed and reconciled; customer informed | FR-ANAL-005; BR-PAY-004/005/006, NOTIFY-002; NFR-SEC-009 | Must | Specified (23/09) ⚠4-eyes BR? |

---

## UC-001: Place Order for 3D Printing

**Description:** Customer uploads a 3D model, configures print settings, receives a quote, and places an order.

**Actors:**
- Primary: Customer
- Secondary: Scheduling System, Payment Gateway

**Preconditions:**
- Customer has registered account
- Customer is authenticated

**Postconditions:**
- Order created in system
- Payment processed
- Jobs assigned to labs
- Customer receives confirmation

**Main Flow:**
1. Customer navigates to "Upload Model" page
2. Customer selects and uploads 3D model file (STL/OBJ/3MF)
3. System validates file format and size
4. System analyzes model geometry (UC-002)
5. System displays model preview in 3D viewer
6. Customer reviews validation results
7. Customer configures print settings:
   - Selects material (e.g., PLA)
   - Selects color
   - Selects quality grade (Standard)
   - Sets infill density (30%)
   - Sets quantity (1)
   - Optionally adds post-processing
8. Customer clicks "Get Quote"
9. System performs slicing analysis (async)
10. System shows preliminary estimate immediately
11. System runs speculative quote-time scheduling (UC-010)
12. System displays full quote with:
    - Total price with breakdown
    - Committed delivery date
    - Quote expiry time (48 hours)
13. Customer reviews quote
14. Customer clicks "Place Order"
15. Customer enters/confirms delivery address
16. Customer selects payment method
17. Customer accepts terms and conditions
18. Customer clicks "Confirm and Pay"
19. System redirects to payment gateway hosted checkout (VNPay/MoMo); card/wallet data never touches platform (BR-PAY-005)
20. Gateway webhook (signature verified) confirms payment; only then system commits order
21. System creates order record
22. System triggers job assignment (UC-011)
23. System sends confirmation email to customer
24. System displays order confirmation page with order number

**Alternate Flows:**

**A1: Invalid File Format (Step 3)**
- 3a. System detects unsupported format
- 3b. System displays error: "File format not supported. Please upload STL, OBJ, or 3MF"
- 3c. Return to step 2

**A2: Model Validation Fails (Step 6)**
- 6a. System detects geometry issues (non-manifold, not watertight)
- 6b. System lists specific issues with locations
- 6c. System offers auto-repair if possible
- 6d. If customer accepts repair: system repairs and continues to step 7
- 6e. If customer declines: return to step 2 to upload different model

**A3: Model Exceeds Build Volume (Step 6)**
- 6a. System checks model against network capacity
- 6b. No lab has sufficient build volume
- 6c. System displays error: "Model exceeds maximum build volume of network (X×Y×Z mm). Please scale down or split model."
- 6d. Cannot continue to quote

**A4: Quote Expired (Step 13)**
- 13a. Customer returns to quote after 48 hours
- 13b. System shows "Quote Expired" message
- 13c. Offer "Get New Quote" button
- 13d. If clicked, return to step 9 with same configuration

**A5: Payment Fails (Step 19)**
- 19a. Payment gateway returns error
- 19b. System displays error message with reason
- 19c. Offer "Try Again" or "Change Payment Method"
- 19d. If retry: return to step 19
- 19e. If change method: return to step 16

**Business Rules:**
- BR-QUOTE-001: Network-wide price uniformity
- BR-QUOTE-002: Capacity-based delivery dates
- BR-QUOTE-003: Quote expiry time
- BR-QUOTE-006: Infeasible part rejection
- BR-ASSIGN-001: Hard constraint filtering
- BR-PAY-001: Payment on order confirmation

---

## UC-002: Analyze Model Geometry

**Description:** System automatically analyzes uploaded 3D model to extract scheduling inputs and validate printability.

**Actors:**
- Primary: System
- Secondary: Slicing Service

**Preconditions:**
- Valid 3D model file uploaded

**Postconditions:**
- Model analyzed and results stored
- Bounding box calculated
- Validation status determined

**Main Flow:**
1. System receives uploaded model file from MinIO
2. System parses file format (STL/OBJ/3MF)
3. System extracts mesh data (vertices, faces)
4. System calculates bounding box (X, Y, Z dimensions)
5. System computes volume
6. System checks mesh watertightness
7. System detects non-manifold edges
8. System validates against network max build volume
9. System stores analysis results with model record
10. System returns validation status to caller

**Alternate Flows:**

**A1: Corrupted File (Step 2)**
- 2a. Parser cannot read file structure
- 2b. Return error: "File corrupted or invalid"

**A2: Exceeds Network Capacity (Step 8)**
- 8a. Bounding box exceeds all machines in network
- 8b. Set validation status: INFEASIBLE
- 8c. Store reason: "Exceeds max build volume"

**Business Rules:**
- BR-QUOTE-006: Infeasible part rejection

---

## UC-003: Track Order

**Description:** Customer views real-time status of their order.

**Actors:**
- Primary: Customer

**Preconditions:**
- Customer authenticated
- Order exists for customer

**Postconditions:**
- Customer informed of current status

**Main Flow:**
1. Customer navigates to "My Orders"
2. System displays list of customer's orders
3. Customer clicks on order to view details
4. System retrieves order status
5. System displays:
   - Order number and date
   - Items with thumbnails
   - Current status stage (e.g., "In Production")
   - Progress bar showing stages
   - Estimated completion time
   - Delivery date
6. System updates status in real-time via SignalR
7. Customer can click "Contact Support" if needed

**Alternate Flows:**

**A1: Order Delayed**
- 6a. System detects order behind schedule
- 6b. Estimated completion time updated
- 6c. "Delayed" badge shown
- 6d. Notification sent to customer

**Business Rules:**
- BR-NOTIFY-001: Critical events require notification

---

## UC-004: Lab Accepts Job Assignment

**Description:** Lab receives job assignment, reviews details, and accepts or rejects.

**Actors:**
- Primary: Lab Manager
- Secondary: Lab Operator

**Preconditions:**
- Lab registered and approved
- Lab has machines registered
- Job assigned to lab by scheduling system

**Postconditions:**
- Job acceptance/rejection recorded
- If accepted: job enters lab's queue
- If rejected: job reassigned by system

**Main Flow:**
1. System assigns job to lab (via UC-011)
2. System sends real-time notification to lab
3. Lab manager receives notification
4. Lab manager opens job assignment details
5. System displays:
   - Model preview
   - Print configuration
   - Estimated print time
   - Material required (type, color, quantity)
   - Internal due date
   - Download model button
6. Lab manager reviews capacity and materials
7. Lab manager clicks "Accept Job"
8. System records acceptance
9. System updates job status to ACCEPTED
10. System adds job to machine queue
11. System updates lab capacity
12. System sends confirmation to lab
13. System provides time-limited model file access

**Alternate Flows:**

**A1: Lab Rejects Job (Step 7)**
- 7a. Lab manager clicks "Reject Job"
- 7b. System prompts for reason:
  - Material out of stock
  - Machine unavailable
  - Infeasibility discovered
  - Capacity overbooked
- 7c. Lab manager selects reason and submits
- 7d. System records rejection
- 7e. System updates lab acceptance rate metric
- 7f. System triggers rescheduling (UC-012)

**A2: No Response Within Deadline (After Step 3)**
- 3a. 2 hours pass without response
- 3b. System auto-rejects job
- 3c. System records timeout
- 3d. System triggers rescheduling (UC-012)

**Business Rules:**
- BR-ASSIGN-005: Lab acceptance window
- BR-ASSIGN-006: Rejection requires justification
- BR-ACCESS-001: Time-limited model access
- BR-ACCESS-002: Access logging mandatory

---

## UC-005: Execute Print Job

**Description:** Lab operator executes printing job and reports completion.

**Actors:**
- Primary: Lab Operator

**Preconditions:**
- Job accepted by lab
- Job in machine queue
- Materials available

**Postconditions:**
- Job completed
- Actual time and materials recorded
- Part ready for handover

**Main Flow:**
1. Lab operator views machine queue on tablet
2. System displays jobs in priority order
3. Operator selects next job
4. System displays job details and instructions
5. Operator clicks "Download Model"
6. System provides time-limited download URL
7. Operator downloads STL file
8. Operator prepares machine:
   - Loads material
   - Calibrates bed
   - Imports file to slicer
   - Starts print
9. Operator clicks "Start Job" in system
10. System updates job status to IN_PROGRESS
11. Operator monitors print
12. Print completes successfully
13. Operator performs post-processing (support removal)
14. Operator clicks "Report Completion"
15. System prompts for:
    - Actual print duration
    - Actual material consumed
16. Operator enters data and confirms
17. System records actual values
18. System updates job status to COMPLETED
19. System triggers calibration data collection
20. System updates machine queue (remove job)
21. System notifies hub of completed part
22. Operator packages part and ships to hub

**Alternate Flows:**

**A1: Print Fails (Step 12)**
- 12a. Print fails (layer separation, warping, etc.)
- 12b. Operator clicks "Report Incident"
- 12c. System prompts for:
  - Incident type: Print Failure
  - Stage: percentage complete when failed
  - Description
  - Photos
- 12d. Operator uploads photos and submits
- 12e. System records incident
- 12f. System triggers rescheduling (UC-012) for reprint
- 12g. Operations manager notified

**A2: Machine Breaks Down (During Step 11)**
- 11a. Machine fails during print
- 11b. Operator clicks "Report Incident"
- 11c. System prompts for incident type: Machine Breakdown
- 11d. Operator marks machine as OFFLINE
- 11e. System triggers rescheduling (UC-012) for all jobs in machine queue
- 11f. Maintenance workflow initiated

**Business Rules:**
- BR-SCHED-007: In-progress jobs untouched (for rescheduling)
- BR-ESTIM-001: Actual time reporting required
- BR-ACCESS-001: Time-limited model access

---

## UC-006: Inspect Part Quality

**Description:** Hub QC staff inspect completed parts against quality checklist.

**Actors:**
- Primary: QC Inspector

**Preconditions:**
- Part received at hub from lab
- Part reconciled in batch receipt

**Postconditions:**
- Part inspection completed
- Pass/fail decision recorded
- If fail: fault attributed and reprint triggered

**Main Flow:**
1. QC inspector selects job from inspection queue
2. System loads inspection checklist for job's quality grade
3. System displays:
   - Job details
   - Part specifications
   - Quality checklist
4. Inspector retrieves physical part
5. Inspector steps through checklist:
   - Dimensional accuracy within tolerance
   - No visible defects (warping, layer separation)
   - Correct material and color
   - Support material removed
   - Surface finish meets standard
6. Inspector marks each check: PASS / FAIL / N/A
7. Inspector captures photos from multiple angles
8. Inspector uploads photos
9. All checks pass
10. Inspector clicks "Pass Inspection"
11. System records PASS result
12. System updates job status to PASSED_QC
13. System moves part to fulfillment queue
14. System updates lab's first-pass yield metric

**Alternate Flows:**

**A1: Part Fails Inspection (Step 9)**
- 9a. One or more checks fail
- 9b. System prompts for defect classification:
  - Dimensional inaccuracy
  - Surface defect
  - Wrong material/color
  - Incomplete post-processing
  - Physical damage
- 9c. Inspector selects defect type
- 9d. System prompts for fault attribution
- 9e. Inspector reviews defect and determines fault:
  - Lab (production issue)
  - Hub (handling damage)
  - Customer (geometry issue)
- 9f. Inspector selects fault party
- 9g. Inspector clicks "Fail Inspection"
- 9h. System records FAIL result with fault attribution
- 9i. System updates lab performance metrics (if lab fault)
- 9j. If lab or hub fault: System creates reprint job (UC-013)
- 9k. If customer fault: System notifies customer (no auto-reprint)

**Business Rules:**
- BR-QC-001: Central hub inspection mandatory
- BR-QC-002: Quality grade checklist binding
- BR-QC-003: Photographic evidence required
- BR-QC-004: Fault attribution required on failure
- BR-QC-005: Lab fault triggers reprint at lab expense
- BR-QC-006: Customer fault notification

---

## UC-007: Customer Requests Reprint

**Description:** Customer receives defective part and requests reprint.

**Actors:**
- Primary: Customer
- Secondary: Operations Manager

**Preconditions:**
- Order delivered to customer
- Within 30-day quality guarantee window

**Postconditions:**
- Reprint request logged
- Request reviewed by operations
- If approved: reprint job created

**Main Flow:**
1. Customer opens completed order
2. Customer clicks "Request Reprint"
3. System checks if within guarantee window (30 days)
4. System displays reprint request form
5. Customer describes issue
6. Customer uploads photos of defect
7. Customer submits request
8. System creates reprint request record
9. System notifies operations manager
10. System sends acknowledgment to customer
11. Operations manager reviews request and photos
12. Defect confirmed as platform responsibility
13. Operations manager approves reprint
14. System creates reprint job with priority URGENT
15. System inherits original deadline
16. System triggers job assignment (UC-011)
17. Customer notified of approval and new timeline

**Alternate Flows:**

**A1: Outside Guarantee Window (Step 3)**
- 3a. More than 30 days since delivery
- 3b. System displays message: "Quality guarantee period expired"
- 3c. Cannot submit request

**A2: Request Denied (Step 13)**
- 13a. Operations manager determines not platform fault (e.g., user damage)
- 13b. Operations manager denies request with explanation
- 13c. Customer notified of denial

**Business Rules:**
- BR-QC-006: Customer fault notification
- BR-RESCHED-003: Priority reprint handling

---

## UC-008: Lab Manager Views Performance

**Description:** Lab manager reviews performance metrics and identifies improvement areas.

**Actors:**
- Primary: Lab Manager

**Preconditions:**
- Lab registered and has completed jobs

**Postconditions:**
- Lab manager informed of performance

**Main Flow:**
1. Lab manager navigates to "Performance Dashboard"
2. System calculates metrics (90-day sliding window)
3. System displays:
   - Overall performance score (0.0 - 1.0)
   - Standing tier (Excellent/Good/Acceptable/Poor)
   - On-time delivery rate (%)
   - First-pass yield (%)
   - Acceptance rate (%)
   - Average utilization (%)
   - Revenue this month
4. System displays trend charts over time
5. System shows comparison to network average (anonymized)
6. System displays defect breakdown by type
7. System shows top issues and recommendations
8. Lab manager reviews metrics
9. Lab manager identifies improvement areas

**Alternate Flows:**

**A1: Insufficient Data (Step 2)**
- 2a. Lab has completed <10 jobs
- 2b. System displays message: "Insufficient data for scoring"
- 2c. Show preliminary metrics without composite score

**A2: Poor Performance (Step 3)**
- 3a. Performance score < 0.50
- 3b. System displays warning: "Performance Below Acceptable"
- 3c. System shows review notice if sustained for 30 days

**Business Rules:**
- BR-PERF-001: Performance metrics continuous tracking
- BR-PERF-002: Minimum sample size for scoring
- BR-PERF-004: Performance transparency to labs
- BR-PERF-005: Lab suspension on poor performance

---

## UC-009: Admin Configures Pricing

**Description:** Administrator adjusts pricing parameters without code deployment.

**Actors:**
- Primary: Platform Administrator

**Preconditions:**
- Admin authenticated with appropriate role

**Postconditions:**
- New pricing parameter version created
- Future quotes use new version
- Existing quotes unchanged

**Main Flow:**
1. Admin navigates to "Configuration > Pricing"
2. System displays current pricing parameters:
   - Material rates (per gram per type)
   - Machine time rates (per hour per technology)
   - Post-processing costs
   - Hub handling costs
   - Shipping rates
3. Admin clicks "Edit Parameters"
4. System loads editable form with current values
5. Admin modifies values (e.g., PLA rate from $0.32 to $0.35/gram)
6. System validates inputs (positive numbers, reasonable ranges)
7. Admin clicks "Preview Impact"
8. System shows sample quote before/after comparison
9. Admin reviews and clicks "Save as New Version"
10. System creates new parameter version record
11. System stores admin ID and timestamp
12. System writes to audit log
13. System displays success message
14. Future quotes now use new version
15. System displays version history

**Alternate Flows:**

**A1: Validation Fails (Step 6)**
- 6a. Invalid value entered (e.g., negative number)
- 6b. System displays validation error
- 6c. Return to step 5

**Business Rules:**
- BR-CONFIG-001: Parameter changes create new version
- BR-CONFIG-002: Configuration audit trail
- BR-CONFIG-003: No retroactive application

---

## UC-010: System Runs Speculative Scheduling

**Description:** System performs trial placement at quote time to determine realistic delivery date.

**Actors:**
- Primary: Scheduling System

**Preconditions:**
- Model sliced and estimated
- Print configuration selected

**Postconditions:**
- Earliest feasible delivery date determined
- Priority surcharge calculated if applicable

**Main Flow:**
1. System receives quote request
2. System retrieves current network state (labs, machines, queues)
3. System creates temporary/speculative order
4. System runs capability filtering (UC-011 steps 2-3)
5. System runs multi-criteria scoring (UC-011 steps 4-5)
6. System selects top-scored lab/machine
7. System calculates internal due date (backward from today + N days)
8. System checks machine schedule for earliest slot
9. System considers batch consolidation if applicable
10. System places job in trial schedule (not committed)
11. System calculates delivery date = slot_end + transfer_time + inspection_time + shipping_time
12. System checks if delivery date meets customer requirement
13. Delivery date feasible
14. System calculates deadline slack
15. If slack < urgent threshold: calculate priority surcharge
16. System stores trial placement results with quote
17. System returns delivery date and price to caller

**Alternate Flows:**

**A1: No Feasible Lab (Step 6)**
- 6a. Capability filter returns empty set
- 6b. Cannot place job
- 6c. Return error: "No lab can fulfill this order"

**A2: Delivery Date Not Achievable (Step 13)**
- 13a. Earliest delivery > customer required date
- 13b. Inform customer: "Cannot meet required date"
- 13c. Offer earliest feasible date as alternative

**Business Rules:**
- BR-QUOTE-002: Capacity-based delivery dates
- BR-QUOTE-007: Asynchronous quote processing
- BR-SCHED-001: Backward deadline scheduling

---

## UC-011: System Assigns Job to Lab

**Description:** System assigns confirmed order job to specific lab/machine.

**Actors:**
- Primary: Scheduling System

**Preconditions:**
- Order confirmed and paid
- Order decomposed into jobs

**Postconditions:**
- Job assigned to lab and machine
- Lab notified
- Assignment logged

**Main Flow:**
1. System receives job assignment request
2. System runs capability filtering:
   - Technology matches
   - Build volume sufficient
   - Tolerance achievable
   - Material in stock
   - Machine operational
   - Lab in good standing
3. System gets list of feasible (lab, machine) pairs
4. System runs multi-criteria scoring for each pair:
   - Calculate deadline slack
   - Calculate current load
   - Retrieve quality history
   - Calculate transfer time
   - Estimate internal cost
   - Normalize and weight
5. System sorts by composite score (descending)
6. System checks exploration allocation (5% random)
7. Not exploration case: select top-scored pair
8. System calculates internal due date:
   - delivery_date - shipping - inspection - transfer = lab_handover
   - lab_handover - print_time - post_process = start_date
9. System checks machine queue for batch consolidation opportunity
10. Same material/color batch exists and not full
11. System adds job to batch
12. System creates job assignment record
13. System logs assignment decision with all scores
14. System updates machine capacity
15. System sends notification to assigned lab
16. Lab has 2 hours to respond

**Alternate Flows:**

**A1: No Feasible Labs (Step 3)**
- 3a. Capability filter returns empty
- 3b. Escalate to operations manager
- 3c. Manual intervention required

**A2: Exploration Allocation (Step 7)**
- 7a. Random number < 0.05 (5% exploration)
- 7b. Select randomly from top 5 feasible labs
- 7c. Continue to step 8

**A3: Urgent Job - No Batching (Step 10)**
- 10a. Job deadline urgent (internal_due < 48h from now)
- 10b. Skip batch consolidation
- 10c. Place individually at earliest slot

**Business Rules:**
- BR-ASSIGN-001: Hard constraint filtering
- BR-ASSIGN-002: Multi-criteria scoring
- BR-ASSIGN-004: Assignment decision logging
- BR-ASSIGN-007: Exploration allocation for fairness
- BR-SCHED-001: Backward deadline scheduling
- BR-SCHED-002: Material/color batch consolidation
- BR-SCHED-004: Urgent job bypass

---

## UC-012: System Reschedules on Event

**Description:** System automatically replans when production events invalidate current schedule.

**Actors:**
- Primary: Scheduling System
- Secondary: Operations Manager (for notifications)

**Preconditions:**
- Production event occurs (rejection, failure, breakdown, reprint)

**Postconditions:**
- Schedule repaired
- Affected jobs reassigned
- Stakeholders notified

**Main Flow:**
1. System receives production event (e.g., print failure)
2. System identifies event type and affected jobs
3. Event is print failure for JOB-X
4. System retrieves job details
5. System removes JOB-X from current schedule
6. System identifies other affected jobs (same machine queue)
7. System generates dispatching-rule fallback solution:
   - Earliest Due Date rule
   - Ensures feasibility
8. System stores fallback as best_solution
9. System starts improvement search (30-second time budget)
10. For remaining time:
    - Try swap or transfer neighborhood moves
    - If improvement found: update best_solution
11. Time budget expires
12. System commits best_solution found
13. System updates machine schedules
14. System checks if any customer deadlines affected
15. No customer deadlines changed
16. System creates reprint job for JOB-X with priority URGENT
17. System assigns reprint job (UC-011)
18. System notifies affected labs of schedule changes
19. System logs rescheduling event and results

**Alternate Flows:**

**A1: Customer Deadline Must Change (Step 15)**
- 15a. Rescheduling cannot meet original deadline
- 15b. System flags for operations manager review
- 15c. Operations manager contacts customer
- 15d. Customer approves new deadline
- 15e. System commits schedule with new deadline

**A2: Machine Breakdown (Step 3)**
- 3a. Event is machine breakdown
- 3b. System retrieves all jobs in machine's queue
- 3c. System removes machine from available pool
- 3d. System reassigns all jobs to other machines
- 3e. Continue to step 7

**Business Rules:**
- BR-RESCHED-001: Event-driven trigger
- BR-RESCHED-002: Affected jobs only
- BR-RESCHED-003: Priority reprint handling
- BR-SCHED-005: Bounded scheduling computation
- BR-SCHED-006: Feasible fallback guarantee
- BR-SCHED-007: In-progress jobs untouched
- BR-SCHED-008: Customer deadline stability

---

## UC-013: System Creates Reprint Job

**Description:** System automatically creates reprint job when part fails inspection.

**Actors:**
- Primary: System

**Preconditions:**
- Part failed inspection (UC-006)
- Fault attributed to lab or hub

**Postconditions:**
- Reprint job created with URGENT priority
- Job assigned to lab
- Customer notified appropriately

**Main Flow:**
1. System receives inspection failure event
2. System checks fault attribution
3. Fault is LAB or HUB (not customer)
4. System retrieves original job details
5. System creates new job record:
   - Copy model, configuration from original
   - Set priority = URGENT
   - Set deadline = original_order.delivery_date
   - Mark as reprint (links to original job)
6. If fault = LAB: mark cost as "lab_expense"
7. If fault = HUB: mark cost as "platform_expense"
8. System checks reprint count for order
9. Reprint count <= 2 (not exceeded limit)
10. System triggers job assignment (UC-011)
11. System updates original job status to "REPRINTING"
12. System sends notification to customer:
    - "Quality issue detected during inspection"
    - "Reprint initiated at no charge"
    - "Delivery date remains [date]"
13. System logs reprint event

**Alternate Flows:**

**A1: Customer Fault (Step 3)**
- 3a. Fault attributed to CUSTOMER
- 3b. Do not create reprint job automatically
- 3c. Notify customer of geometry issue
- 3d. Offer paid reprint or refund
- 3e. Await customer decision

**A2: Reprint Limit Exceeded (Step 9)**
- 9a. Reprint count > 2
- 9b. Escalate to operations manager
- 9c. Manual review required
- 9d. Operations manager decides action

**Business Rules:**
- BR-RESCHED-003: Priority reprint handling
- BR-RESCHED-004: Reprint limit
- BR-QC-005: Lab fault triggers reprint at lab expense
- BR-QC-006: Customer fault notification
- BR-QC-007: Hub fault platform cost

---

## UC-014: Onboard Lab, Machines & Inventory

**Description:** A new printing lab registers on the platform, declares its machines and material stock, and is activated after administration approval.

**Actors:**
- Primary: Lab Manager
- Secondary: Platform Administrator

**Preconditions:**
- Lab Manager holds a registered account (role LAB_PENDING)

**Postconditions:**
- Lab active with declared calendar, transfer time, machines and inventory
- Capability data becomes available to the assignment filter

**Main Flow:**
1. Lab Manager opens "Register Lab"
2. Lab Manager submits lab profile: name, address, contact, working calendar, transfer time to hub
3. System validates calendar and transfer time (positive, within bounds) **BR-OPS-006**
4. Lab Manager adds machines one by one: technology, build volume (X·Y·Z), layer height range, achievable tolerance, operational state
5. Lab Manager declares material stock: material type, colour, spool quantity
6. System runs completeness check (≥1 machine, ≥1 material)
7. System sets lab status PENDING_APPROVAL and notifies Platform Administrator
8. Platform Administrator reviews declaration and clicks Approve
9. System activates lab (status ACTIVE) and notifies Lab Manager
10. System includes lab in the next capability filter refresh (UC-011 step 2)

**Alternate Flows:**

**A1: Approval Rejected (Step 8)**
- 8a. Administrator rejects with reason (incomplete specs, suspected over-claim)
- 8b. System sets lab REJECTED with reason; Lab Manager may resubmit from step 2

**A2: New machine added later (After step 10)**
- 10a. Lab Manager edits registry; changed specs log an update event for declared-vs-delivered audit (supports lab performance ledger)

**Business Rules:**
- BR-ASSIGN-001 (input: hard constraint data), BR-PERF-005 (suspension applies later), **BR-OPS-005**

---

## UC-015: Hand Over Completed Parts to Hub

**Description:** Lab operator records physical handover of finished parts (courier drop-off) so the hub expects an incoming batch.

**Actors:**
- Primary: Lab Operator
- Secondary: Hub QC Staff (receiver)

**Preconditions:**
- Job COMPLETED at lab (UC-005 step 18)
- Parts packaged

**Postconditions:**
- Handover record links jobs to an outbound batch; hub receipt queue updated

**Main Flow:**
1. Operator opens "Handover" screen; system lists completed jobs not yet shipped
2. Operator selects jobs for this shipment
3. System creates a batch with QR code and prints/labels reference
4. Operator confirms courier drop-off and enters carrier + waybill number (manual entry — carrier API out of scope)
5. System sets jobs status IN_TRANSIT and notifies hub of expected batch
6. System records handover timestamp and operator id (audit)

**Alternate Flows:**

**A1: Wrong part packed (Step 2)**
- 2a. Operator removes job from selection; nothing shipped for it; job stays COMPLETED awaiting next batch

**Business Rules:**
- BR-QC-001 (hub inspection mandatory after handover), **BR-SCHED-009**

---

## UC-016: Receive & Reconcile Hub Batch

**Description:** Hub QC receives a lab batch and reconciles physical contents against expected jobs before inspection.

**Actors:**
- Primary: Hub QC Staff

**Preconditions:**
- Batch expected (UC-015 step 5)
- Parts arrived at hub

**Postconditions:**
- Batch reconciled; each job released to inspection queue or flagged missing/damaged

**Main Flow:**
1. QC staff scans batch QR at receipt
2. System shows expected job list for the batch
3. QC staff counts and confirms received items
4. QC staff marks each expected job: RECEIVED / MISSING / DAMAGED-IN-TRANSIT
5. RECEIVED items move to inspection queue (UC-006)
6. System closes batch receipt with receiver id + timestamp
7. System updates order progress ("At hub")

**Alternate Flows:**

**A1: Item MISSING (Step 4)**
- 4a. QC flags MISSING; system notifies lab manager and operations
- 4b. Job routed to incident/loss handling (treated like lab fault candidate)

**A2: DAMAGED-IN-TRANSIT (Step 4)**
- 4a. QC photographs damage, flags DAMAGED
- 4b. Attribution review: packaging fault (lab) vs carrier handling (hub/platform)
- 4c. If reprint needed → UC-013

**Business Rules:**
- BR-QC-003 (photographic evidence), BR-QC-004 (attribution), BR-RESCHED-003

---

## UC-017: Consolidate, Pack & Ship Order

**Description:** Fulfillment staff gathers all passed items of one customer order, packs, records shipment and hands to carrier; delivery status follows to completion.

**Actors:**
- Primary: Hub Fulfillment Staff
- Secondary: Notification Service

**Preconditions:**
- All items of the order PASSED_QC (UC-006 step 13)

**Postconditions:**
- Order shipped and eventually DELIVERED; customer notified at each step

**Main Flow:**
1. Fulfillment staff opens order in fulfillment queue
2. System lists expected items with bin locations
3. Staff scans each item; system verifies against order completeness gate — packing blocked until all present **BR-QC-008**
4. Staff confirms consolidation; system generates packing list
5. Staff packs and clicks "Record Shipment"
6. Staff enters carrier + waybill number (manual — carrier API out of scope)
7. System sets order status SHIPPED and sends tracking notification to customer
8. On delivery confirmation (manual tick or customer confirm), system sets DELIVERED and starts the 30-day guarantee window (UC-007 precondition)
9. System closes order lifecycle record for analytics

**Alternate Flows:**

**A1: Incomplete set (Step 3)**
- 3a. Missing item detected → pack blocked; system shows which job is outstanding and why (e.g., reprinting)
- 3b. If reprint open (UC-013), order waits; operations manager is flagged when delay threatens committed date (UC-019)

**Business Rules:**
- BR-NOTIFY-001, **BR-QC-009**

---

## UC-018: Update Estimate Calibration Factors

**Description:** Per machine model, the gap between slicer estimates and reported actuals is turned into a correction factor used by quoting and scheduling. MVP: reviewed and entered manually by admin; automatic regression is extended scope.

**Actors:**
- Primary: Platform Administrator
- Secondary: Scheduler (time actor, weekly reminder — MVP), Scheduling System (consumer)

**Preconditions:**
- ≥ N completed jobs per machine model with reported actual durations **BR-ESTIM-006**

**Postconditions:**
- New factor version active; quotes and schedules use updated estimates

**Main Flow:**
1. System aggregates estimate-vs-actual samples per machine model
2. Scheduler raises weekly review task to admin dashboard
3. Admin opens calibration screen; system shows sample count, mean gap %, trend
4. Admin proposes new factor (e.g., 1.12 for machine model X)
5. System preview-impact: re-quotes 3 recent orders with new factor for comparison
6. Admin saves; system versions the factor set (same pattern as pricing: BR-CONFIG-001)
7. Scheduling/quoting services pick up active factor version (BR-CONFIG-003 — no retroactive effect on committed promises)

**Alternate Flows:**

**A1: Insufficient samples (Step 3)**
- 3a. System shows preliminary gap statistics without a recommended factor; admin keeps previous factor

**Business Rules:**
- BR-ESTIM-001 (actual reporting feeds loop), BR-CONFIG-001, BR-CONFIG-003

---

## UC-019: Monitor Network & At-Risk Orders

**Description:** Operations manager oversees network state: open orders, jobs at risk of lateness, per-lab load and standing; drill-down to any order.

**Actors:**
- Primary: Operations Manager

**Preconditions:**
- Ops role authenticated

**Postconditions:**
- Risk detected → intervention initiated (UC-020) or reschedule accepted

**Main Flow:**
1. Ops manager opens Network Monitor
2. System shows KPI cards: open orders, jobs at-risk, on-time forecast, lab load distribution
3. At-risk definition: remaining slack < threshold (configurable) **BR-OPS-007**
4. System lists at-risk orders sorted by slack, with reason (queue too long, machine down, reprint pending)
5. Ops selects an order; system drills into full lifecycle timeline (quote → jobs → hub)
6. Ops selects a job and jumps to UC-020 for intervention, or acknowledges with note

**Alternate Flows:**

**A1: Reprint limit orders (Dashboard panel)**
- 6a. System lists orders exceeding reprint limit awaiting decision (UC-013 A2) — links to resolution

**Business Rules:**
- BR-NOTIFY-001, BR-RESCHED-004 (limit visibility)

---

## UC-020: Override Assignment, Priority & Lab Standing

**Description:** Operations manager manually intervenes in automated decisions; every action requires a recorded reason and lands in the audit/decision log.

**Actors:**
- Primary: Operations Manager
- Secondary: Scheduling System

**Preconditions:**
- Ops role authenticated

**Postconditions:**
- Schedule/priority/lab state changed with justification logged

**Main Flow:**
1. Ops opens the job, order or lab in question (from UC-019)
2. Ops chooses action:
   - a) Reassign job to another (lab, machine) pair — system runs capability filter as advisory; if target infeasible, block unless "force override" ticked **BR-OPS-002 — open question #11 still decides its Exception clause**
   - b) Move job within machine queue (sequence change)
   - c) Change order priority (NORMAL→URGENT or back)
   - d) Suspend / reinstate lab, or cap lab WIP (observed lab)
3. System requires free-text reason (min length enforced) and shows downstream impact preview (affected deadlines, displaced jobs)
4. Ops confirms
5. System applies change, triggers UC-012 where schedule recompute needed
6. System writes action + candidates + before/after snapshot to decision/audit log (BR-ASSIGN-004, BR-CONFIG-002)
7. Affected labs notified (BR-NOTIFY-001)

**Alternate Flows:**

**A1: Customer date impact (Step 3)**
- 3a. Impact preview shows a committed customer date moving → confirmation requires a second-step approval and customer notification path (BR-SCHED-008)

**Business Rules:**
- BR-ASSIGN-004, BR-CONFIG-002, BR-SCHED-007, BR-SCHED-008, BR-PERF-005, BR-NOTIFY-001

---

## UC-021: Generate & Export SLA / Performance Reports

**Description:** Produce period reports on network SLA and lab performance for management review and for the algorithm evaluation dataset.

**Actors:**
- Primary: Operations Manager
- Secondary: Lab Manager (own-lab report view, UC-008)

**Preconditions:**
- Completed order history within report period

**Postconditions:**
- Report generated and exported (CSV/PDF)

**Main Flow:**
1. Ops opens Reports; selects period (week/month/semester) and scope (network / per lab / per material)
2. System aggregates: on-time %, weighted tardiness, first-pass yield, utilisation, reprint rate, changeover count
3. Ops applies filters (quality grade, service level)
4. System renders tables + charts
5. Ops clicks Export; system writes CSV/PDF and records export in audit log
6. (Evaluation use) same aggregation is exposed for simulator runs — the Algorithm Evaluation Report reuses this pipeline on simulated data

**Alternate Flows:**

**A1: Empty period (Step 2)**
- 2a. No data → system shows explicit "no completed orders in period" instead of zeros (quality: don't measure nothing)

**Business Rules:**
- BR-PERF-001, BR-CONFIG-002 (export audit), **BR-CONFIG-004**

---

## UC-022: Manage Users, Roles & Permissions

**Description:** Administrator creates and maintains accounts across customers, labs and hub staff, including role assignment and lockout.

**Actors:**
- Primary: Platform Administrator

**Preconditions:**
- Admin role authenticated

**Postconditions:**
- Account state changed; all mutations audited

**Main Flow:**
1. Admin opens User Management; system lists accounts with role and state
2. Admin searches (email, code, role)
3. Admin creates account or edits: role assignment (Customer / Lab Manager / Lab Operator / QC / Fulfillment / Ops / Admin), status (ACTIVE / LOCKED / DEACTIVATED)
4. System enforces role exclusivity rules for staff roles **BR-ACCESS-005**
5. On LOCK/DEACTIVATE, system requires reason; active sessions revoked within policy window
6. System writes every change to audit log (actor, before/after)

**Alternate Flows:**

**A1: Self-lockout attempt (Step 3)**
- 3a. Admin tries to deactivate own account → blocked (system must never strand admin)

**Business Rules:**
- BR-ACCESS-002, BR-CONFIG-002, **BR-ACCESS-005**

---

## UC-023: Manage Shared Catalogs

**Description:** Administrator maintains the reference data consumed by quoting, filtering and QC: materials, colours, technologies, quality grades, post-processing options.

**Actors:**
- Primary: Platform Administrator

**Preconditions:**
- Admin role authenticated

**Postconditions:**
- Catalog entries active/deprecated; quotes/policies unaffected retroactively

**Main Flow:**
1. Admin opens Catalog Management; tabs per catalogue
2. Admin adds/edits entry (e.g., material PETG-Grey, machine technology, quality grade fields incl. tolerance tier and inspection checklist binding)
3. System validates uniqueness (code) and price-availability links (material ↔ rate rows)
4. Admin deprecates an entry; system blocks use in NEW quotes but preserves history (BR-CONFIG-003)
5. System audits change and notifies pricing config when a material's rate row needs input

**Alternate Flows:**

**A1: Deprecate in-use material (Step 4)**
- 4a. System lists open quotes/orders referencing it; deprecation allowed for new business only; running orders keep the entry

**Business Rules:**
- BR-CONFIG-001, BR-CONFIG-003, BR-QUOTE-001 (uniform price inputs)

---

## UC-024: Manage Inspection Checklists & Defect Taxonomy

**Description:** Administrator authors the QC knowledge base: checklists bound to quality grades and the defect type taxonomy used in UC-006 attribution.

**Actors:**
- Primary: Platform Administrator

**Preconditions:**
- Quality grades exist (UC-023)

**Postconditions:**
- Active checklist versions bound per grade; taxonomy stable ids referenced by historical inspection records

**Main Flow:**
1. Admin opens QC Management; selects quality grade
2. Admin edits checklist: items with pass criteria (dimensional tolerance, surface, colour match…), order, mandatory flag
3. Admin saves as new version; version becomes active for inspections from next receipt (running inspections keep their version — reproducibility, mirrors BR-CONFIG-003)
4. Admin maintains defect taxonomy: add/rename categories (dimensional, surface, material, post-processing, damage); deactivation of used codes forbidden (history integrity)
5. System audits all changes

**Alternate Flows:**

**A1: Taxonomy code used by past inspections (Step 4)**
- 4a. System blocks delete, allows DEPRECATED flag only — old records keep readable attribution

**Business Rules:**
- BR-QC-002 (checklist bound to grade), BR-QC-004 (taxonomy drives attribution), BR-CONFIG-001/002/003

---

## UC-025: Review Audit Log & System Health

**Description:** Administrator inspects the tamper-evident record of manual interventions and parameter changes, and monitors background job / storage health.

**Actors:**
- Primary: Platform Administrator
- Secondary: Hangfire/queue health source, storage quota monitor

**Preconditions:**
- Admin role authenticated

**Postconditions:**
- Anomalies acknowledged or escalated; no log mutation possible

**Main Flow:**
1. Admin opens Audit Log: filter by actor, action class (override, config change, access grant, export), date
2. System renders immutable entries (who, when, what before/after, reason) — append-only store
3. Admin opens System Health: queue depth, failed job list, storage usage vs quota, integration status (email, storage)
4. Admin retries/replays failed background jobs from here (idempotent replay)
5. System records each admin action on health items into the same audit log

**Alternate Flows:**

**A1: Repeated failures of one job type (Step 3)**
- 3a. System highlights trend; admin escalates to development per runbook (not auto-fix — MVP scope boundary)

**Business Rules:**
- BR-ACCESS-002, BR-CONFIG-002, BR-NOTIFY-001, **BR-CONFIG-004**

---

## UC-026: Manage Account, Addresses & Model Library

**Description:** Customer self-service: profile, delivery addresses, and the personal model library enabling reuse of uploaded models in later orders.

**Actors:**
- Primary: Customer

**Preconditions:**
- Authenticated session

**Postconditions:**
- Profile/address/library state updated; models kept private per IP rules

**Main Flow:**
1. Customer opens Account area
2. Customer edits profile (name, contact, password via change form with re-auth)
3. Customer manages delivery addresses (add/edit/set-default; delete blocked while an undelivered order references the address)
4. Customer opens Model Library: list of uploaded models with thumbnail, last-used date
5. Customer clicks "Reorder" on a library item; system loads model + last print configuration into a new quote draft (UC-001 from step 9)
6. System applies BR-ACCESS-001/002: library files never exposed except through job-scoped links

**Alternate Flows:**

**A1: Storage quota reached (Step 4 upload path)**
- 4a. System blocks new upload with quota message and offers deletion of unused old models

**Business Rules:**
- BR-ACCESS-001, BR-ACCESS-002, **BR-ACCESS-006**

---

## UC-027: Configure & Run Network Simulation (Algorithm Evaluation)

> **System boundary note:** This use case belongs to the project deliverable `Simulation Harness`
> (register Products P-92; Tasks WP5; supervisor comment P-103) — an offline evaluation system that
> drives the real engines. It is part of the complete PrintGrid system as specified, but is deployed
> and operated outside the production portals: its user is the Evaluator (research role), never a
> customer, lab or hub actor. Requirements traced here: e)-Practical bullets 8–9, f)-Products.

**Description:** The research evaluator configures a virtual 3D-printing network — synthetic order streams (order generator) and virtual labs with injected faults — runs the REAL assignment/scheduling engines against it under accelerated time, and exports evaluation metrics against baselines. This is the product `Simulation Harness` from the register (P-92) and the evidence engine behind the Algorithm Evaluation Report (P-93).

**Actors:**
- Primary: Evaluator (research role — WP1/WP2 member; operates through the harness CLI/config, not the portals)
- Secondary: Assignment & Scheduling Engine (system under test), Metrics/Report pipeline (UC-021 aggregation)

**Preconditions:**
- Engine deployed in simulation mode (virtual clock + virtual storage; no real payment/gateway calls)
- Seed value chosen (reproducibility)

**Postconditions:**
- Completed run with deterministic replay (same seed ⇒ same result)
- Metric dataset exported for the evaluation report and demo scenario selection

**Main Flow:**
1. Evaluator creates a run config: network size (e.g., 20–50 labs, up to 300 machines), machine model mix, per-lab declared capability + declared-vs-delivered bias factor
2. Evaluator configures the **order generator**: arrival process (rate, distribution e.g. Poisson), job size/material/colour/quality-grade mix, required-by tightness distribution, customer priority mix
3. Evaluator configures **fault injection** on the lab simulator:
   - accept/decline rate of virtual labs (+decline reason mix)
   - print failure rate with stage distribution (e.g., failures concentrated at 60–90% of duration)
   - machine breakdown arrivals (rate, mean downtime, repair)
   - hub inspection failure rate per lab (first-pass yield variance)
4. Evaluator selects allocation strategies to compare in the same generated workload: (a) capacity-aware proposal, (b) dispatching-rule-only, (c) nearest-available-lab, (d) random baseline
5. Evaluator starts the run; system advances virtual time (hours/sec), replaying the full production loop per job: quote (UC-010 path) → order → assignment (UC-011) → accept/decline → print with injected failures (UC-005 events) → handover/receipt (UC-015/016) → inspection (UC-006) → rescheduling triggers (UC-012)
6. Engine components under test are the real services — the simulator only fuses the physical/clock layers
7. Run completes at configured horizon; system computes metrics: weighted tardiness, on-time %, utilisation, changeover count, load spread (Gini across labs), reschedule count/nervousness, estimate-vs-actual error
8. System writes run artifacts: event log, decision log extracts (BR-ASSIGN-004 format), metric tables, seed + config snapshot
9. Evaluator exports to the UC-021 report pipeline → Algorithm Evaluation Report and demo scenario shortlist (a run containing a dramatic mid-queue failure repaired live = the "scheduling visible in product" demo the supervisor asked for, P-100)

**Alternate Flows:**

**A1: Run non-reproducible (Step 4)**
- 4a. Replaying same seed yields different schedule → bug in nondeterministic component; run quarantined, flagged to WP2 (correctness gate for evaluation claims)

**A2: Fault rates make network collapse (Step 7)**
- 7a. > X% orders unfulfillable; system flags run as saturated-load; evaluator keeps it as stress-scenario data (still useful — documents breaking point)

**A3: Budget guard tripped during run (Step 5)**
- 5a. Improvement search exceeds its time budget in virtual time too → counted as NFR violation metric, reported separately, not silently retried

**Business Rules:**
- BR-ASSIGN-004 (decision log format reused), BR-SCHED-005 (bounded computation measured here), **BR-OPS-008**

---

## UC-028: Process Refund & Reconciliation ★ (quyết định payment tiền thật 23/09)

**Description:** Operations approves and executes a refund via the payment gateway after a platform-failure
trigger (BR-PAY-004), with idempotent API calls and a daily transaction reconciliation report.

**Actors:**
- Primary: Operations Manager
- Secondary: Payment Gateway (system actor), Notification Service

**Preconditions:**
- Order was paid through the real gateway (transaction id present)
- Refund trigger established: platform missed committed date without B16-approved condition / complaint C2-denied path with refund offer / customer-approved change (BR-SCHED-008 list)

**Postconditions:**
- Refund recorded against original transaction; order marked REFUNDED/PARTIALLY_REFUNDED
- Customer notified; gateway + order states reconciled in T-1 report

**Main Flow:**
1. Ops opens refund queue (from complaint UC-007, missed-depromise alert UC-019, or QC escalation)
2. Ops selects refund scope: FULL, or PARTIAL per item (system computes refundable amount = paid − fees of non-refunded items)
3. System requires reason + approval (second staff for partial >50% value — 4-eyes) **[BR? : 4-eyes threshold]**
4. System issues gateway refund call with idempotency key (txn id + attempt) (BR-PAY-006)
5. Gateway webhook confirms; system marks order REFUNDED/PARTIALLY_REFUNDED and stores refund id
6. System notifies customer with amount + expected arrival window
7. Nightly reconciliation job diffs orders ↔ gateway transactions; zero-diff closes day, mismatches raise ops alert
8. All steps audited (actor, reason, before/after) — BR-CONFIG-002

**Alternate Flows:**

**A1: Gateway rejects / webhook missing (Step 4-5)**
- 4a. System retries with same idempotency key up to policy limit; order state unchanged until confirmed
- 5a. Never flips to REFUNDED on assumption — no silent success

**A2: Reconciliation mismatch (Step 7)**
- 7a. Paid-but-not-in-production or production-but-not-paid rows → auto-freeze refunds on that txn, page ops

**A3: Exceeds refund SLA (5 business days) (BR-PAY-006)**
- 3a. Escalation entry with reason; weekly exception report to operations review

**Business Rules:**
- BR-PAY-004: Refund on platform failure (now enforced in-system)
- BR-PAY-006: Refund SLA ★
- BR-PAY-005: Card data never touches platform ★
- BR-NOTIFY-002: customer notification
- BR-CONFIG-002: audit trail

---

## Use Case Diagram

```
                         PrintGrid System (UC-001 … UC-026)

 Customer (UC-001,003,007,026)          Lab Manager (UC-004,008,014)     Lab Operator (UC-005,015)
        │                                        │                               │
 Hub QC Staff (UC-006,016)              Hub Fulfillment (UC-017)        Ops Manager (UC-019,020,021)
        │                                        │                               │
 Platform Admin (UC-009,018,022,023,024,025)   Scheduler/Time (UC-018)  Notification Svc (003,007,012,015..017,028)
 Ops Manager also: UC-028 (refund, + Payment Gateway system actor)
        │
 Scheduling System / Engine: UC-002, UC-010, UC-011, UC-012, UC-013
        ▲ (drives real engine in sim mode)
 Evaluator — UC-027 (order generator + fault injection → metrics → UC-021 pipeline)

 Core flows:  UC-001→002→010→(011) → UC-004→005→015→016→006→(017 or 013→011)
 Repair loop: UC-011 ↔ UC-012 ; exception feeds: 005/016 → 013 ; oversight: 019→020→021
 Onboarding:  UC-014 ; platform base: 009,018,022–026 ; account self-service: 026
 Evidence:    UC-027 (simulation) + UC-021 (report) → Algorithm Evaluation Report
```

*(Legacy note: the old diagram referenced unplanned IDs UC-020/029/031/033/034/035; see Change note at top for the mapping to final numbering.)*

---

## Summary

**Total Use Cases: 28 Detailed**

| Category | Use Cases |
|----------|-----------|
| Customer | UC-001, UC-003, UC-007, UC-026 |
| Lab | UC-004, UC-005, UC-008, UC-014, UC-015 |
| Hub | UC-006, UC-016, UC-017 |
| Operations | UC-019, UC-020, UC-021, UC-028 |
| Administration | UC-009, UC-018, UC-022, UC-023, UC-024, UC-025 |
| System / Engine (incl. time actor) | UC-002, UC-010, UC-011, UC-012, UC-013, UC-018 (Scheduler) |
| Research / Evaluation | UC-027 (Evaluator) |

**Key Integration Points:**
- Order flow: UC-001 → UC-002 → UC-010 → (confirm) UC-011 → UC-004 → UC-005 → UC-015 → UC-016 → UC-006 → UC-017 → delivery
- Quality loop: UC-006 → (fail) → UC-013 → UC-011 → … → UC-006; customer side: UC-007
- Resilience loop: UC-011 ↔ UC-012; oversight: UC-019 → UC-020 → (UC-012 / notifications)
- Calibration loop: UC-005 (actuals) → UC-018 → UC-010/UC-011 (estimates)
- Platform base: UC-014 feeds capability data to UC-011; UC-022/023/024 feed UC-001/009/006

**Traceability:**
- Each use case references Business Rules from 08-Business-Rules.md — all referenced codes exist there (74 rules, classification table in section 0); Step-6 sync also exposed 4 **rule chết** (BR-PERF-006, BR-PAY-002/003/004) needing FR or reclassification — tracked in `capstone/workbook/03_Audit`
- FR↔UC mapping lives in `capstone/workbook/07_Traceability.md` (column "UC → TC"); UC→TC filled when Report 5 maps test cases
