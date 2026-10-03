# User Stories - PrintGrid Platform

## Overview
This document contains user stories for all platform personas, organized by Epic. Each story follows the format:
- **Story ID**
- **Title**
- **User Story:** As a [role], I want [goal], so that [benefit]
- **Acceptance Criteria**
- **Story Points** (Fibonacci: 1, 2, 3, 5, 8, 13)
- **Priority:** Critical / High / Medium / Low
- **Dependencies**

---

## 0. Story Matrix (submission format)

One row per story; full blocks with all acceptance criteria remain in the epics below.
**Priority** = MoSCoW of the owning FR (detailed blocks keep their original Critical/High/Medium labels).
**Status legend** (same as 09/10/12): `Approved` = traced to approved register scope · `Proposed` = new from v1.1 sync, to be frozen in register v1.2 · `Specified` = created by a decision (23/09) · `⚠` = open item referenced from `capstone/workbook/04`.

| Story ID | As a… | I want… | So that… | Acceptance Criteria | Priority | Related Req. | Status |
|---|---|---|---|---|---|---|---|
| US-001 | Customer | upload STL/OBJ/3MF files | I can get a printing quote | ≤50 MB accepted; bad format rejected with message; stored in object storage with id | Must | FR-CUST-002 · UC-001 · BR-ACCESS-006 | Approved |
| US-002 | Customer | preview my model in 3D in the browser | I verify it before ordering | rotate/zoom; bbox dims; stats; loads <5 s | Should | FR-CUST-003 · UC-001 | Approved · **cut-candidate #2 if late** |
| US-003 | Customer | get automatic geometry validation | I know the part is printable | watertight + manifold checks; oversize vs network rejected with reason; <10 s | Must | FR-CUST-004 · UC-002 · BR-QUOTE-006 | Approved |
| US-004 | Customer | configure print parameters per item | I get exactly what I need | material/colour/grade/infill/qty/post-processing; invalid combos disabled | Must | FR-CUST-005 · UC-001 · BR-CONFIG-003 | Approved |
| US-005 | Customer | receive an instant quote | I can decide to order | preliminary ≤5 s; full ≤60 s; breakdown sum = total; committed date; expiry shown | Must | FR-CUST-006, FR-SCHED-010 · UC-001/010 · BR-QUOTE-* | Approved |
| US-006 | Customer | confirm the quote and pay real money via gateway | the order enters production with a recorded payment | address+T&C enforced; hosted checkout, token only; commit only on verified webhook | Must | FR-CUST-007/008 · UC-001 · BR-PAY-001/005 · NFR-SEC-009 | Approved (payment decision 23/09) |
| US-007 | Customer | track my order in real time | I know when to expect delivery | 7 stages; live updates; lab identity invisible everywhere | Must | FR-CUST-009 · UC-003 · BR-NOTIFY-001 | Approved |
| US-008 | Customer | keep a personal model library | I can reorder quickly | thumbnails; metadata; delete confirm; reorder prefills config | Could | FR-CUST-010 · UC-026 · BR-ACCESS-006 | Approved |
| US-009 | Customer | request a reprint for a defective delivered part | I get a working product | request + photos inside 30 d; trackable; acknowledgment sent | Must | FR-CUST-011 · UC-007 · BR-QC-009 | Approved |
| US-010 | Lab Manager | register my lab with calendar and transfer time | the platform can allocate work to me | profile + completeness validated; PENDING→ACTIVE via admin approval | Must | FR-LAB-001 · UC-014 · BR-OPS-005/006 | Approved (partner lab confirmed 23/09) |
| US-011 | Lab Manager | register machines with full capability specs | only feasible jobs reach me | technology/volume/tolerance/layer state CRUD; delete blocked with active jobs; visible to filter <1 min | Must | FR-LAB-002 · UC-014 · BR-OPS-006 | Approved (lab confirmed 23/09) |
| US-012 | Lab Manager | track material and colour inventory | I never accept work I cannot run | stock levels; consumption auto-deduct; low-stock alert | Must | FR-LAB-003 · UC-014 · BR-ASSIGN-001(in) | Approved (lab confirmed 23/09) |
| US-013 | Lab Manager | review and accept/decline assigned jobs | I control what enters my queue | 2 h countdown; decline requires reason; timeout auto-decline | Must | FR-LAB-004 · UC-004 · BR-ASSIGN-005/006 | Approved (auto-decline ⚠B5) |
| US-014 | Lab Operator | manage the machine queue and update job states | the system reflects real progress | start/complete; actual duration+material mandatory; incident w/ photos; tablet-first | Must | FR-LAB-005 · UC-005 · BR-ESTIM-001, SCHED-007 | Approved |
| US-015 | Lab Manager | see the machine schedule as a timeline | I can plan around it | per-machine Gantt; updates after each reschedule | Should | FR-LAB-006 · UC-011(view) · BR-SCHED | Approved |
| US-016 | Lab Manager | see my performance standing and figures | I know how to improve | score + 4 metrics; network comparison anonymized; <10 jobs preliminary | Should | FR-LAB-007, FR-ANAL-002 · UC-008 · BR-PERF-* | Approved |
| US-017 | Hub QC Staff | receive and reconcile incoming lab batches | inspection starts from truth | batch QR scan; expected list; MISSING/DAMAGED flagged with incident + notify | Must | FR-HUB-001 · UC-016 · BR-QC-001/010 | Approved |
| US-018 | Hub QC Staff | inspect parts with the grade-bound checklist | quality decisions are consistent | all mandatory checks marked; photos required; fail → defect + attribution | Must | FR-HUB-002 · UC-006 · BR-QC-002/003/004 | Approved |
| US-019 | Hub QC Staff | trigger reprints automatically on lab/hub faults | customers still receive good parts | URGENT job inheriting deadline; limit 2 → escalation; cost flag | Must | FR-HUB-003 · UC-007/013 · BR-QC-005..007, RESCHED-003/004 | Approved (limit=2, B14) |
| US-020 | Hub Fulfillment Staff | consolidate order items and pack before shipping | customers receive complete orders | completeness gate blocks partial pack; packing list; waybill recorded | Must | FR-HUB-004/005 · UC-017 · BR-QC-008 | Approved |
| US-021 | System | analyze uploaded geometry automatically | quoting and scheduling have inputs | bbox ±0.1 mm; volume; integrity checks; <10 s | Must | FR-SCHED-001 · UC-002 | Approved |
| US-022 | System | slice headless and estimate time + material | promises rest on numbers | profile mapping per tech/grade; ≤60 s typical; async | Must | FR-SCHED-002 · UC-002 · NFR-ACC-001 | Approved (engine pinned in pilot ⚠A1) |
| US-023 | System | filter labs by hard constraints | infeasible assignments are impossible | 0 violations on adversarial suite run independent of scoring; <1 s | Must | FR-SCHED-003 · UC-011 · BR-ASSIGN-001 · NFR-REL-005 | Approved |
| US-024 | System | score feasible candidates on weighted criteria | allocation balances time/load/quality/cost/logistics | weights sum 1.0; normalized 0–1; deterministic per config version | Must | FR-SCHED-004 · UC-011 · BR-ASSIGN-002/003 | Approved (MVP weights fixed, A3) |
| US-025 | System | run trial placement at quote time | the promised date comes from real capacity | ≤60 s; speculative — real schedule untouched; date moves with load | Must | FR-SCHED-005 · UC-010 · BR-QUOTE-002 · NFR-PERF-001 | Approved (crude-first, A5) |
| US-026 | System | decompose orders and place jobs on machines | production has an executable plan | backward internal due-dates; serialized per machine; logged decision | Must | FR-SCHED-006/009 · UC-011 · BR-SCHED-001..004 · NFR-REL-004 | Approved (consolidation → Should, A4) |
| US-027 | System | repair the unstarted plan on failure events | deadlines survive reality | ≤30 s budget; in-progress untouched; fallback committed; approvals per B16 | Must | FR-SCHED-007 · UC-012 · BR-RESCHED-*, SCHED-005..008 · NFR-PERF-002/REL-006 | Approved (B4/B16 23/09) |
| US-028 | System | collect estimate-vs-actual samples per machine model | calibration has data | (estimated, actual) pairs on every completion; outliers filtered | Should | FR-SCHED-008 · UC-018 · BR-ESTIM-001..003 | Approved |
| US-029 | Admin | manage accounts, roles and states | access follows responsibility | RBAC; single staff role; self-lockout blocked; audited | Must | FR-ADMIN-001 · UC-022 · BR-ACCESS-005 | Approved |
| US-030 | Admin | adjust business parameters without deployment | policy changes don't need releases | versioned save; preview impact; ≤1 min effective; no retro | Must | FR-ADMIN-002 · UC-009 · BR-CONFIG-001..003 · NFR-MAINT-005 | Approved |
| US-031 | Admin | maintain shared catalogues | pricing and filtering have clean inputs | CRUD; unique codes; in-use = deprecate only | Must | FR-ADMIN-003 · UC-023 · BR-CONFIG-003 | Approved |
| US-032 | Admin | review the audit log | actions are attributable | append-only; search <2 s; export logged | Should | FR-ADMIN-004 · UC-025 · BR-CONFIG-002/004 | Approved |
| US-033 | Ops Manager | monitor network and at-risk orders | I intervene before promises break | at-risk = slack < cfg 4 h; refresh ≤30 s; drill-down | Should | FR-ANAL-001 · UC-019 · BR-OPS-007 | Approved |
| US-034 | Ops Manager | override assignment, priority and lab standing | exceptions have a human owner | reason enforced; feasibility advisory/force-policy; 2-step on customer-date impact | Should | FR-ANAL-004 · UC-020 · BR-OPS-001..004 | Approved (force-override ⚠B11) |
| US-035 | Ops Manager | generate and export SLA reports | performance is reviewed with numbers | period/scope filters; empty ≠ zero; export audited | Should | FR-ANAL-003 · UC-021 · BR-CONFIG-004, OPS-008 | Approved |
| US-036 | Lab Operator | record part handover to the hub | the hub expects the right batch | batch + QR; manual waybill; IN_TRANSIT; overdue >24 h soft alert | Must | FR-LAB-005 (handover) · UC-015 · BR-SCHED-009 | Proposed |
| US-037 | Admin | review gaps and apply calibration factors manually | MVP quotes improve without waiting for regression | ≥10-sample gate; preview re-quote; versioned; committed quotes frozen | Should | FR-SCHED-008 · UC-018 · BR-ESTIM-002..006 | Proposed (A7 MVP manual) |
| US-038 | Admin | author checklists and maintain the defect taxonomy | inspection is consistent and attributable | version per grade; running inspections pinned; used codes deprecate-only | Should | FR-ADMIN-005 · UC-024 · BR-QC-002/004 | Proposed |
| US-039 | Admin | watch background-job and storage health | silent failures don't eat orders | queue depth; failed-job replay; trend highlight | Could | FR-ADMIN-004 · UC-025 · ⚠P-67 | Draft ⚠C3 (may become runbook) |
| US-040 | Customer | manage profile, addresses and reorder from library | reordering takes under a minute | delete-address guard; quota message; 1-min reorder path | Must | FR-CUST-001/010 · UC-026 · BR-ACCESS-006 | Proposed |
| US-041 | Hub Fulfillment Staff | record final delivery | the guarantee window starts correctly | DeliveredAt set by staff/customer; reprint availability computed from it | Must | FR-HUB-005 · UC-017 · BR-QC-009 | Proposed |
| US-042 | Evaluator (WP1/WP2) | **generate virtual order streams and inject lab/machine/inspection faults against the real engines** | scheduler claims are measured, not asserted | order generator configurable (rate, mix, deadline tightness); 4 fault types incl. stage-distributed print failure & breakdown+MTTR; same seed ⇒ same result; ≥3 baselines; metrics exported via UC-021 | Must (research) | Products P-92/93 · UC-027 · BR-OPS-008, SCHED-005 · NFR-TEST-003 | Specified — **harness due week 4** |
| US-043 | Ops Manager | open the assignment decision log | lab disputes settle from the record | candidates + per-criterion scores + config version for any job; overrides appended; read-only | Must | FR-SCHED-009 · UC-011 · BR-ASSIGN-004/CONFIG-004 | Proposed |
| US-044 | Ops Manager | approve and execute refunds with daily reconciliation | real-money mistakes are corrected safely | full/partial per item; idempotent (0 double refund); webhook-confirmed; T-1 diff 0/explained; ≤5 d SLA | Must | FR-ANAL-005 · UC-028 · BR-PAY-004/006 · NFR-SEC-009 | Specified (23/09) ⚠4-eyes BR? |

---

## Epic 1: Customer Order Management

### US-001: Upload 3D Model
**Title:** Upload 3D Model Files  
**User Story:** As a customer, I want to upload my 3D model files (STL/OBJ/3MF), so that I can get a quote for printing.

**Acceptance Criteria:**
- AC1: User can select and upload STL, OBJ, or 3MF files up to 50 MB
- AC2: Upload progress indicator shows percentage complete
- AC3: System validates file format and shows error for unsupported formats
- AC4: Successfully uploaded files appear in upload confirmation
- AC5: File stored securely in MinIO with unique identifier

**Story Points:** 5  
**Priority:** Critical  
**Dependencies:** None

---

### US-002: Preview 3D Model
**Title:** Interactive 3D Model Preview  
**User Story:** As a customer, I want to preview my uploaded model in 3D, so that I can verify it's correct before ordering.

**Acceptance Criteria:**
- AC1: Model renders in browser using Three.js
- AC2: User can rotate, zoom, and pan the model
- AC3: Bounding box dimensions displayed (X, Y, Z in mm)
- AC4: Toggle between solid and wireframe view
- AC5: Model statistics shown (vertices, faces)
- AC6: Preview loads within 5 seconds for typical models

**Story Points:** 8  
**Priority:** High  
**Dependencies:** US-001

---

### US-003: Validate Model Geometry
**Title:** Automatic Model Validation  
**User Story:** As a customer, I want the system to automatically check my model for issues, so that I know it's printable before ordering.

**Acceptance Criteria:**
- AC1: System checks mesh watertightness
- AC2: System detects non-manifold edges
- AC3: System validates build volume feasibility across network
- AC4: Validation results displayed within 10 seconds
- AC5: Specific issues listed with locations
- AC6: Auto-repair suggested for fixable issues

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-001

---

### US-004: Configure Print Settings
**Title:** Select Print Configuration  
**User Story:** As a customer, I want to configure print parameters (material, color, quality), so that I get exactly what I need.

**Acceptance Criteria:**
- AC1: Material dropdown shows available materials (PLA, PETG, ABS, etc.)
- AC2: Color selection updates based on material
- AC3: Quality grade options: Draft, Standard, High, Ultra
- AC4: Infill density selector: 10%, 30%, 50%, 100%
- AC5: Quantity input (1-100)
- AC6: Post-processing options checkboxes
- AC7: Unavailable combinations disabled with tooltip explanation
- AC8: Configuration saved with model

**Story Points:** 5  
**Priority:** Critical  
**Dependencies:** US-003

---

### US-005: Receive Instant Quote
**Title:** Get Price Quote and Delivery Date  
**User Story:** As a customer, I want to receive an instant quote with price and delivery date, so that I can decide whether to order.

**Acceptance Criteria:**
- AC1: Preliminary estimate shown within 5 seconds
- AC2: Full quote (with trial placement) completes within 60 seconds
- AC3: Price breakdown displayed: material, print time, post-processing, inspection, shipping
- AC4: Committed delivery date shown (from capacity-based scheduling)
- AC5: Quote expiry time displayed (48 hours)
- AC6: "Save Quote" button stores for later retrieval
- AC7: Priority/rush option shown if available with surcharge

**Story Points:** 13  
**Priority:** Critical  
**Dependencies:** US-004

---

### US-006: Place Order
**Title:** Confirm Order and Pay  
**User Story:** As a customer, I want to review my quote and complete payment, so that my order enters production.

**Acceptance Criteria:**
- AC1: Order summary shows all items, configuration, and pricing
- AC2: Delivery address form with validation
- AC3: Payment method selection; real gateway (VNPay/MoMo) hosted checkout — card data never touches platform (BR-PAY-005); order commits only on verified webhook
- AC4: Terms and conditions acceptance checkbox
- AC5: "Place Order" button disabled until all required fields complete
- AC6: Payment processing with loading indicator
- AC7: Order confirmation page with order number
- AC8: Confirmation email sent to customer

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-005

---

### US-007: Track Order Status
**Title:** Real-Time Order Tracking  
**User Story:** As a customer, I want to track my order status in real-time, so that I know when to expect delivery.

**Acceptance Criteria:**
- AC1: Order status page shows current stage (7 stages)
- AC2: Progress bar or timeline visualization
- AC3: Estimated completion time updates dynamically
- AC4: Status descriptions user-friendly (no technical jargon)
- AC5: Email notifications on status change
- AC6: Real-time updates via SignalR (no page refresh needed)
- AC7: Lab identity NOT visible to customer
- AC8: Contact support button available

**Story Points:** 8  
**Priority:** High  
**Dependencies:** US-006

---

### US-008: Manage Model Library
**Title:** Personal Model Library  
**User Story:** As a customer, I want to save my uploaded models in a library, so that I can easily reorder them later.

**Acceptance Criteria:**
- AC1: Library page shows all customer's uploaded models
- AC2: Thumbnail previews generated for each model
- AC3: Model metadata displayed: name, dimensions, upload date
- AC4: Search and filter capabilities
- AC5: Delete model with confirmation dialog
- AC6: "Reorder" button pre-fills configuration from previous order
- AC7: Rename model functionality

**Story Points:** 5  
**Priority:** Medium  
**Dependencies:** US-001

---

### US-009: Request Reprint
**Title:** Quality Issue Reprint Request  
**User Story:** As a customer, I want to request a reprint if I receive a defective part, so that I get a quality product.

**Acceptance Criteria:**
- AC1: "Request Reprint" button on completed order
- AC2: Form to describe issue with predefined options
- AC3: Photo upload capability (multiple photos)
- AC4: Submit triggers review workflow
- AC5: Request status trackable
- AC6: Acknowledgment notification sent immediately
- AC7: Available within 30 days of delivery

**Story Points:** 5  
**Priority:** Medium  
**Dependencies:** US-007

---

## Epic 2: Lab Operations

### US-010: Register Lab
**Title:** Lab Registration  
**User Story:** As a lab owner, I want to register my lab on the platform, so that I can receive printing jobs.

**Acceptance Criteria:**
- AC1: Registration form captures: lab name, location, contact, business hours
- AC2: Distance/transfer time to hub input
- AC3: Bank account details for payments
- AC4: Business documentation upload (license, insurance)
- AC5: Submit triggers admin review workflow
- AC6: Email notification on approval/rejection

**Story Points:** 5  
**Priority:** High  
**Dependencies:** None

---

### US-011: Manage Machine Registry
**Title:** Add and Configure Machines  
**User Story:** As a lab manager, I want to register my 3D printers with specifications, so that appropriate jobs are assigned to me.

**Acceptance Criteria:**
- AC1: Add machine form: technology, brand, model, build volume (X/Y/Z), layer height range, tolerance
- AC2: Edit existing machine specifications
- AC3: Delete machine (blocked if active jobs)
- AC4: Mark machine status: operational, maintenance, offline
- AC5: Schedule planned maintenance windows
- AC6: View machine utilization statistics
- AC7: Changes reflect in assignment within 1 minute

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-010

---

### US-012: Track Material Inventory
**Title:** Material Inventory Management  
**User Story:** As a lab manager, I want to track material stock levels, so that I don't accept jobs I can't fulfill.

**Acceptance Criteria:**
- AC1: Add material: type, color, quantity (grams), reorder point
- AC2: Low stock alert when below reorder point
- AC3: Consumption auto-updated when job completed
- AC4: Restock entry with date and quantity
- AC5: Projected depletion date based on current queue
- AC6: Filter and search materials
- AC7: Inventory reflected in assignment within 1 minute

**Story Points:** 5  
**Priority:** Critical  
**Dependencies:** US-010

---

### US-013: Accept or Reject Job
**Title:** Job Assignment Review  
**User Story:** As a lab manager, I want to review assigned jobs before accepting, so that I can reject infeasible work.

**Acceptance Criteria:**
- AC1: Real-time notification when job assigned
- AC2: Job details page shows: model preview, configuration, estimated time, material required, internal due date
- AC3: Download model file button (time-limited access)
- AC4: Accept button commits to job
- AC5: Reject button requires reason selection
- AC6: Response deadline countdown visible (2 hours)
- AC7: Auto-reject if no response within deadline
- AC8: Acceptance/rejection logged in performance metrics

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-011, US-012

---

### US-014: Execute Production Workflow
**Title:** Lab Operator Job Management  
**User Story:** As a lab operator, I want to manage the production queue and update job status, so that the system knows progress.

**Acceptance Criteria:**
- AC1: View machine queue showing jobs in priority order
- AC2: Job detail view with all specifications
- AC3: Download model file for assigned job
- AC4: "Start Job" button marks as IN_PROGRESS
- AC5: "Report Completion" form captures actual print time and material used
- AC6: "Report Incident" button for failures with photo upload
- AC7: Status updates reflected system-wide immediately
- AC8: Mobile-responsive interface (tablet-optimized)
- AC9: Offline mode saves updates, syncs when connected

**Story Points:** 13  
**Priority:** Critical  
**Dependencies:** US-013

---

### US-015: View Schedule Timeline
**Title:** Machine Schedule Visualization  
**User Story:** As a lab manager, I want to see my machine schedules in a timeline, so that I can plan capacity.

**Acceptance Criteria:**
- AC1: Gantt chart per machine
- AC2: Jobs shown as bars with job ID and duration
- AC3: Color-coded by priority (normal, urgent, reprint)
- AC4: Click job for details popup
- AC5: Show internal due dates on hover
- AC6: Filter by machine or material type
- AC7: Total queue duration displayed
- AC8: Print/export schedule to PDF
- AC9: Updates after rescheduling

**Story Points:** 8  
**Priority:** Medium  
**Dependencies:** US-011

---

### US-016: Monitor Performance Metrics
**Title:** Lab Performance Dashboard  
**User Story:** As a lab manager, I want to see my performance metrics, so that I can identify improvement areas.

**Acceptance Criteria:**
- AC1: Overall performance score displayed prominently
- AC2: Metrics shown: on-time delivery rate, first-pass yield, acceptance rate, utilization
- AC3: Trend charts over time (last 90 days)
- AC4: Comparison to network average (anonymized)
- AC5: Defect breakdown by type
- AC6: Revenue summary for current month
- AC7: Top issues/recommendations section
- AC8: Updates daily

**Story Points:** 8  
**Priority:** Medium  
**Dependencies:** US-010

---

## Epic 3: Hub Quality Control

### US-017: Receive Lab Shipments
**Title:** Batch Receipt and Reconciliation  
**User Story:** As hub staff, I want to receive and reconcile incoming batches from labs, so that I know what to inspect.

**Acceptance Criteria:**
- AC1: Scan batch barcode from lab
- AC2: System displays expected jobs in batch
- AC3: Scan individual job QR codes
- AC4: Reconciliation summary: received vs. expected
- AC5: Flag discrepancies (missing items)
- AC6: Automatic notification to lab for discrepancies
- AC7: Move received items to inspection queue

**Story Points:** 5  
**Priority:** High  
**Dependencies:** US-014

---

### US-018: Inspect Part Quality
**Title:** Checklist-Driven Quality Inspection  
**User Story:** As QC inspector, I want to inspect parts using a standardized checklist, so that quality is consistent.

**Acceptance Criteria:**
- AC1: Select job from inspection queue
- AC2: Load checklist based on quality grade ordered
- AC3: Step through checks with pass/fail/N/A options
- AC4: Mandatory photo capture from multiple angles
- AC5: Overall pass/fail decision
- AC6: If FAIL: defect classification dropdown
- AC7: If FAIL: fault attribution (lab/hub/customer)
- AC8: Complete inspection within 5 minutes for standard parts
- AC9: Decision saved and triggers appropriate action

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-017

---

### US-019: Manage Reprints
**Title:** Reprint Initiation and Tracking  
**User Story:** As QC inspector, I want to automatically trigger reprints for failed parts, so that customers get quality products.

**Acceptance Criteria:**
- AC1: Lab fault → automatic reprint job created with URGENT priority
- AC2: Reprint inherits original deadline
- AC3: Customer fault → notification to customer, no auto-reprint
- AC4: Hub fault → reprint at platform expense
- AC5: Reprint status trackable
- AC6: Original job marked as "reprinting"
- AC7: Customer notified appropriately based on fault
- AC8: Reprint limit enforced (escalate after 2 reprints)

**Story Points:** 8  
**Priority:** High  
**Dependencies:** US-018

---

### US-020: Consolidate and Ship Orders
**Title:** Order Fulfillment  
**User Story:** As hub fulfillment staff, I want to consolidate all items for an order and ship, so that customers receive complete orders.

**Acceptance Criteria:**
- AC1: View orders ready for packing (all items passed inspection)
- AC2: Print packing slip
- AC3: Package items with protective material
- AC4: Record package dimensions and weight
- AC5: Generate shipping label
- AC6: Scan package to carrier
- AC7: Update order status to "shipped"
- AC8: Customer receives tracking notification

**Story Points:** 5  
**Priority:** High  
**Dependencies:** US-018

---

## Epic 4: Scheduling and Assignment

### US-021: Analyze Uploaded Models
**Title:** Geometry Analysis Service  
**User Story:** As the system, I want to analyze uploaded models automatically, so that I have data for scheduling.

**Acceptance Criteria:**
- AC1: Parse STL/OBJ/3MF file structure
- AC2: Calculate bounding box (X, Y, Z) accurate to 0.1mm
- AC3: Compute volume
- AC4: Check mesh watertightness
- AC5: Detect non-manifold edges
- AC6: Analysis completes within 10 seconds
- AC7: Results stored with model record

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-001

---

### US-022: Generate Print Estimates
**Title:** Slicing and Estimation Service  
**User Story:** As the system, I want to slice models and estimate print time, so that I can quote accurately.

**Acceptance Criteria:**
- AC1: Integrate CuraEngine or PrusaSlicer CLI
- AC2: Map configuration to slicer profile
- AC3: Execute slicing asynchronously
- AC4: Extract print time estimate from G-code
- AC5: Calculate material consumption (primary + support)
- AC6: Slicing completes within 60 seconds for typical models
- AC7: Store G-code (optional) or estimates with model
- AC8: Handle slicing errors gracefully

**Story Points:** 13  
**Priority:** Critical  
**Dependencies:** US-021

---

### US-023: Filter by Capability
**Title:** Hard Constraint Filtering  
**User Story:** As the system, I want to filter labs by capability constraints, so that I only assign feasible jobs.

**Acceptance Criteria:**
- AC1: Filter by technology match
- AC2: Filter by build volume sufficient
- AC3: Filter by tolerance achievable
- AC4: Filter by material in stock
- AC5: Filter by machine operational
- AC6: Filter by lab in good standing
- AC7: Return only labs passing ALL filters
- AC8: Filter executes in <1 second
- AC9: Log filtered candidates

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-011, US-012

---

### US-024: Score Assignment Candidates
**Title:** Multi-Criteria Assignment Scoring  
**User Story:** As the system, I want to score feasible labs using multiple criteria, so that I assign optimally.

**Acceptance Criteria:**
- AC1: Calculate deadline slack score
- AC2: Calculate current load score
- AC3: Calculate quality history score
- AC4: Calculate transfer time score
- AC5: Calculate internal cost score
- AC6: Apply configurable weights
- AC7: Normalize scores to 0-1 range
- AC8: Composite score calculated
- AC9: Log all scores for audit
- AC10: Scoring completes in <1 second

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-023

---

### US-025: Run Quote-Time Scheduling
**Title:** Speculative Trial Placement  
**User Story:** As the system, I want to run trial placement at quote time, so that I can promise realistic delivery dates.

**Acceptance Criteria:**
- AC1: Treat as speculative order (doesn't commit)
- AC2: Run full capability filter and scoring
- AC3: Place on machine schedule (trial)
- AC4: Calculate backward from available slots
- AC5: Return earliest feasible delivery date
- AC6: Calculate priority surcharge if urgent
- AC7: Complete within 60 seconds
- AC8: Does not affect real schedule
- AC9: Results cached with quote expiry

**Story Points:** 13  
**Priority:** Critical  
**Dependencies:** US-024

---

### US-026: Assign Jobs to Machines
**Title:** Job Assignment and Placement  
**User Story:** As the system, I want to assign confirmed jobs to specific machines, so that production can start.

**Acceptance Criteria:**
- AC1: Run capability filter and scoring
- AC2: Select top-scored lab/machine
- AC3: Calculate internal due date (backward from delivery)
- AC4: Check for batch consolidation opportunity
- AC5: Place on machine schedule
- AC6: Create job assignment record
- AC7: Notify assigned lab
- AC8: Update machine capacity
- AC9: Log assignment decision with scores

**Story Points:** 13  
**Priority:** Critical  
**Dependencies:** US-025

---

### US-027: Reschedule on Events
**Title:** Event-Driven Rescheduling  
**User Story:** As the system, I want to reschedule automatically when failures occur, so that deadlines are still met.

**Acceptance Criteria:**
- AC1: Trigger on: rejection, print failure, machine breakdown, reprint, urgent order
- AC2: Identify affected jobs
- AC3: Generate dispatching rule fallback solution
- AC4: Improve solution within 30-second time budget
- AC5: Commit best solution found
- AC6: In-progress jobs NOT modified
- AC7: Customer deadlines NOT silently changed
- AC8: Notify affected labs and customers
- AC9: Log rescheduling event and results

**Story Points:** 13  
**Priority:** Critical  
**Dependencies:** US-026

---

### US-028: Calibrate Estimates
**Title:** Print Time Calibration Loop  
**User Story:** As the system, I want to calibrate print time estimates from actual data, so that estimates improve over time.

**Acceptance Criteria:**
- AC1: Collect (estimated, actual) pairs from completed jobs
- AC2: Group by machine model
- AC3: Run linear regression per model
- AC4: Calculate correction factors
- AC5: Apply factors to future estimates
- AC6: Track MAPE over time
- AC7: Calibration runs weekly
- AC8: Minimum 10 samples before applying
- AC9: Outliers filtered (>3 std dev)

**Story Points:** 8  
**Priority:** High  
**Dependencies:** US-014

---

## Epic 5: Platform Administration

### US-029: Manage Users and Roles
**Title:** User and Role Management  
**User Story:** As an admin, I want to manage users and assign roles, so that access is controlled.

**Acceptance Criteria:**
- AC1: List all users with search and filter
- AC2: Create new user with role assignment
- AC3: Edit user details
- AC4: Disable/enable user accounts
- AC5: Reset user passwords
- AC6: View login history per user
- AC7: Audit log of admin actions
- AC8: Role changes take effect immediately

**Story Points:** 5  
**Priority:** High  
**Dependencies:** None

---

### US-030: Configure Business Parameters
**Title:** Configuration Management  
**User Story:** As an admin, I want to adjust pricing and scheduling parameters without code deployment, so that I can tune the system.

**Acceptance Criteria:**
- AC1: Edit pricing parameters: material rates, machine time rates, etc.
- AC2: Edit assignment weights (5 criteria)
- AC3: Edit scheduling limits: batch size, time budgets, etc.
- AC4: Edit SLA thresholds
- AC5: Changes create new version (not modify existing)
- AC6: Preview impact before saving
- AC7: Validation prevents invalid values (e.g., weights sum to 1.0)
- AC8: Audit trail of all changes

**Story Points:** 8  
**Priority:** High  
**Dependencies:** None

---

### US-031: Manage Catalogs
**Title:** Reference Data Management  
**User Story:** As an admin, I want to manage catalogs of materials, colors, and services, so that they stay up-to-date.

**Acceptance Criteria:**
- AC1: Materials: add, edit, delete (name, properties, default rate)
- AC2: Colors: add, edit, delete (name, premium flag)
- AC3: Technologies: add, edit, delete (name, description)
- AC4: Quality grades: add, edit, delete (name, checklist)
- AC5: Post-processing services: add, edit
- AC6: Cannot delete items in active use
- AC7: Changes propagate to UI immediately

**Story Points:** 5  
**Priority:** Medium  
**Dependencies:** None

---

### US-032: View Audit Logs
**Title:** System Audit Log  
**User Story:** As an admin, I want to view complete audit logs, so that I can investigate issues and ensure compliance.

**Acceptance Criteria:**
- AC1: Search by user, event type, date range
- AC2: View detailed event data (before/after values)
- AC3: Filter and sort results
- AC4: Export to CSV/Excel
- AC5: Logs immutable (no deletion)
- AC6: Search results within 2 seconds
- AC7: Sensitive events highlighted (payment, access control changes)

**Story Points:** 5  
**Priority:** Medium  
**Dependencies:** None

---

## Epic 6: Monitoring and Operations

### US-033: Monitor Network Status
**Title:** Real-Time Network Dashboard  
**User Story:** As an operations manager, I want to see real-time network status, so that I can identify issues quickly.

**Acceptance Criteria:**
- AC1: Active orders count
- AC2: Jobs at risk (deadline within 4 hours) highlighted
- AC3: Current network load percentage
- AC4: Lab status summary (operational/at capacity/offline)
- AC5: Recent alerts feed
- AC6: Updates every 30 seconds
- AC7: Drill-down to details on click
- AC8: Color-coded by urgency

**Story Points:** 8  
**Priority:** High  
**Dependencies:** US-026

---

### US-034: Manually Intervene
**Title:** Manual Override Tools  
**User Story:** As an operations manager, I want to manually override system decisions when needed, so that I can handle exceptions.

**Acceptance Criteria:**
- AC1: Reassign job to different lab (with justification)
- AC2: Adjust job priority (with justification)
- AC3: Extend delivery deadline (requires customer notification)
- AC4: Suspend lab (with reason and duration)
- AC5: Cap lab capacity (set max concurrent jobs)
- AC6: All interventions logged in audit trail
- AC7: Feasibility still validated
- AC8: Affected parties notified

**Story Points:** 8  
**Priority:** Medium  
**Dependencies:** US-033

---

### US-035: Generate SLA Reports
**Title:** SLA Reporting  
**User Story:** As an operations manager, I want to generate SLA compliance reports, so that I can track performance.

**Acceptance Criteria:**
- AC1: Network-wide SLA metrics calculated
- AC2: Per-lab breakdown available
- AC3: Time period selection (weekly, monthly, quarterly)
- AC4: Metrics: on-time rate, defect rate, etc.
- AC5: Trend analysis charts
- AC6: Export to PDF and Excel
- AC7: Report generates within 10 seconds
- AC8: Professional formatting

**Story Points:** 8  
**Priority:** Medium  
**Dependencies:** US-033

---

## Epic 7: Shop-Floor Handover & Hub Logistics (sync v1.1 — UC-015/016/017)

### US-036: Record Part Handover to Hub
**Title:** Lab Handover & Waybill Entry  
**User Story:** As a lab operator, I want to record which completed jobs leave my lab for the hub, so that the hub knows what to expect.

**Traces:** FR-LAB-005 (decomposed) · UC-015 · BR-SCHED-009, BR-QC-001  
**Acceptance Criteria:**
- AC1: Operator selects completed, not-yet-shipped jobs and creates a batch with QR reference
- AC2: Carrier + waybill number entered manually (no carrier API — out of scope)
- AC3: Jobs transition to IN_TRANSIT; hub receives expected-batch notification
- AC4: Handover timestamp and operator recorded for audit
- AC5: Overdue handover (> 24 h after completion) raises soft alert (BR-SCHED-009)

**Story Points:** 3  
**Priority:** Critical  
**Dependencies:** US-014

## Epic 8: Calibration & Evidence Operations

### US-037: Apply Calibration Factors Manually (MVP)
**Title:** Admin Calibration Review  
**User Story:** As an administrator, I want to review per-model estimate-vs-actual gaps and apply correction factors manually, so that MVP quotes improve without waiting for automatic regression.

**Traces:** FR-SCHED-008 (MVP scope per B2 §4) · UC-018 · BR-ESTIM-002/003/004/006  
**Acceptance Criteria:**
- AC1: Screen lists machine models with sample count, mean gap %, trend
- AC2: Proposal blocked when samples < minimum (BR-ESTIM-006)
- AC3: Factor saved as new version; preview re-quotes 3 recent orders before commit
- AC4: Existing committed quotes unaffected (BR-CONFIG-003)
- AC5: Automatic regression remains a separate (Could) story — do not build in MVP

**Story Points:** 3  
**Priority:** High  
**Dependencies:** US-028 (aggregation part)

### US-038: Manage Inspection Checklists & Defect Taxonomy
**Title:** QC Knowledge Base Administration  
**User Story:** As an administrator, I want to author checklists per quality grade and maintain the defect taxonomy, so that inspection is consistent and attributable.

**Traces:** FR-ADMIN-005 ★ · UC-024 · BR-QC-002/004, BR-CONFIG-001/003  
**Acceptance Criteria:**
- AC1: Checklist editor with pass criteria per item, mandatory flag, ordering
- AC2: Save creates new checklist version bound to grade; running inspections keep their version
- AC3: Taxonomy codes can be deprecated but never deleted once referenced by inspection history
- AC4: All edits audited

**Story Points:** 5  
**Priority:** High  
**Dependencies:** US-031 (quality grade exists)

### US-039: Monitor Background Job & Storage Health
**Title:** System Health Console  
**User Story:** As an administrator, I want to see queue depth, failed background jobs and storage usage, so that I can replay failures before they affect orders.

**Traces:** FR-ADMIN-004 · UC-025 · ⚠ open question (01_Extract P-67: keep as feature vs runbook constraint — decide with GVHD)  
**Acceptance Criteria:**
- AC1: Dashboard: Hangfire queue states, failed-job list with error, storage usage vs quota, integration status (email/MinIO)
- AC2: Failed job replay button (idempotent, audited)
- AC3: Repeated failure pattern highlighted for escalation

**Story Points:** 3  
**Priority:** Medium  
**Dependencies:** None

### US-040: Manage Profile, Addresses & Reorder from Library
**Title:** Customer Account Self-Service  
**User Story:** As a customer, I want to manage my profile, delivery addresses and reuse saved models, so that reordering takes under a minute.

**Traces:** FR-CUST-001/010 · UC-026 · BR-ACCESS-001/002/006  
**Acceptance Criteria:**
- AC1: Address CRUD with default; deleting an address referenced by an undelivered order is blocked
- AC2: Library reorder loads model + last config into a new quote draft
- AC3: Quota enforcement with prune guidance (BR-ACCESS-006)
- AC4: Password change requires re-authentication

**Story Points:** 3  
**Priority:** High  
**Dependencies:** US-008

### US-041: Confirm Delivery & Open Guarantee Window
**Title:** Delivery Completion Recording  
**User Story:** As hub fulfillment staff, I want to record final delivery, so that the guarantee window and order closure start correctly.

**Traces:** FR-HUB-005 ★ · UC-017 · BR-QC-009, BR-NOTIFY-001  
**Acceptance Criteria:**
- AC1: Staff records delivered/returned; or customer confirms in portal
- AC2: DeliveredAt timestamp drives guarantee window (BR-QC-009)
- AC3: Order lifecycle record closed to analytics; reprint button availability computed from DeliveredAt

**Story Points:** 3  
**Priority:** Critical  
**Dependencies:** US-020

### US-044: Process Refunds & Daily Reconciliation ★ (quyết định 23/09)
**Title:** Refund Execution & Payment Reconciliation  
**User Story:** As an operations manager, I want to approve and execute refunds through the gateway with a daily reconciliation report, so that real-money mistakes are corrected without double-refunding or silent drift.

**Traces:** FR-ANAL-005 ★ · UC-028 · BR-PAY-004/005/006, CONFIG-002  
**Acceptance Criteria:**
- AC1: Full and per-item partial refund with computed refundable amount
- AC2: Idempotency key prevents second successful refund on same txn
- AC3: Order state changes to REFUNDED only after verified gateway webhook
- AC4: Nightly reconciliation T-1: 0 unexplained diffs; mismatched txn auto-frozen
- AC5: Refund ≤5 business days or escalation entry with reason (BR-PAY-006)

**Story Points:** 5  
**Priority:** Critical  
**Dependencies:** US-006

---

## Epic 9: Evaluation & Evidence (simulator — UC-027)

### US-042: Run Network Simulation & Fault Injection
**Title:** Simulation Harness Runs  
**User Story:** As an evaluator (WP1/WP2), I want to generate virtual order streams and inject lab/machine/inspection faults against the real engines, so that scheduler claims are measured, not asserted.

**Traces:** Products P-92/P-93 · Practical e)-bullets 8–10 · UC-027 · BR-SCHED-005, BR-OPS-008 — note: research deliverable, NOT a production portal feature  
**Acceptance Criteria:**
- AC1: Config for network size, order generator (rate, size/material/deadline mix), machine model mix
- AC2: Fault injection: decline rate, print-failure with stage distribution, breakdown+MTTR, inspection failure per lab
- AC3: Same seed replays deterministically; nondeterministic runs quarantined
- AC4: Metrics exported via UC-021 pipeline: weighted tardiness, on-time %, utilisation, changeover, load Gini
- AC5: 4 strategies comparable on identical workload (capacity-aware / dispatching / nearest-lab / random)
- AC6: Deliverable date: harness usable by end of week 4 (B2 §5 commitment)

**Story Points:** 13  
**Priority:** Critical  
**Dependencies:** US-023, US-024, US-026 (real engines exist to drive)

### US-043: Review Assignment Decision Log
**Title:** Decision Log Viewer  
**User Story:** As an operations manager, I want to open any past assignment and see the candidate labs with their scores, so that lab disputes and bad allocations are settled from the record.

**Traces:** FR-SCHED-009 ★ · UC-011 (post) · BR-ASSIGN-004, BR-CONFIG-004  
**Acceptance Criteria:**
- AC1: Lookup by job/order/time; shows candidates, per-criterion scores, chosen pair, timestamp, config version
- AC2: Override and reschedule events appended to same thread
- AC3: Viewer is read-only; export audited

**Story Points:** 3  
**Priority:** Critical  
**Dependencies:** US-026

---

## Traceability Matrix (Story → FR → UC → BR domains)

| Story | FR | UC | BR domain(s) | Story | FR | UC | BR domain(s) |
|-------|----|----|--------------|-------|----|----|--------------|
| US-001 | CUST-002 | 001 | ACCESS | US-023 | SCHED-003 | 011 | ASSIGN |
| US-002 | CUST-003 | 001 | — | US-024 | SCHED-004 | 011 | ASSIGN/PERF |
| US-003 | CUST-004 | 002 | QUOTE | US-025 | SCHED-005 | 010 | QUOTE/SCHED |
| US-004 | CUST-005 | 001 | CONFIG | US-026 | SCHED-006 | 011 | SCHED |
| US-005 | CUST-006 (+SCHED-010★) | 001 | QUOTE | US-027 | SCHED-007 | 012 | RESCHED/SCHED |
| US-006 | CUST-007/008 | 001 | PAY | US-028 | SCHED-008 | 018 | ESTIM |
| US-007 | CUST-009 | 003 | NOTIFY | US-029 | ADMIN-001 | 022 | ACCESS |
| US-008 | CUST-010 | 026 | ACCESS | US-030 | ADMIN-002 | 009 | CONFIG |
| US-009 | CUST-011 | 007 | QC | US-031 | ADMIN-003 | 023 | CONFIG |
| US-010 | LAB-001 | 014 | OPS★ | US-032 | ADMIN-004 | 025 | CONFIG/ACCESS |
| US-011 | LAB-002 | 014 | OPS★ | US-033 | ANAL-001 | 019 | OPS★ |
| US-012 | LAB-003 | 014 | ASSIGN(in) | US-034 | ANAL-004 | 020 | OPS/SCHED/ACCESS |
| US-013 | LAB-004 | 004 | ASSIGN/ACCESS | US-035 | ANAL-003 | 021 | CONFIG★ |
| US-014 | LAB-005 | 005 | ESTIM/ACCESS | US-036 | LAB-005 | 015 | SCHED★ |
| US-015 | LAB-006 | 011v | SCHED | US-037 | SCHED-008 | 018 | ESTIM★ |
| US-016 | LAB-007 | 008 | PERF | US-038 | ADMIN-005★ | 024 | QC/CONFIG |
| US-017 | HUB-001 | 016 | QC | US-039 | ADMIN-004 | 025 | ⚠P-67 |
| US-018 | HUB-002 | 006 | QC | US-040 | CUST-001/010 | 026 | ACCESS★ |
| US-019 | HUB-003 | 013 | QC/RESCHED | US-041 | HUB-005★ | 017 | QC★/NOTIFY |
| US-020 | HUB-004/005★ | 017 | QC★ | US-042 | (WP harness) | 027 | OPS★/SCHED |
| US-021 | SCHED-001 | 002 | ESTIM | US-043 | SCHED-009★ | 011 | ASSIGN/CONFIG |
| US-022 | SCHED-002 | 002 | ESTIM | US-044 | ANAL-005★ | 028 | PAY★ |

*(★ = thêm mới ở sync v1.1 / quyết định 23/09)*

**Total Story Points: ~336** → 2-week sprints, velocity ~35 → **≈10 sprint**; gateway thật + refund cộng
thêm ~8–10 ngày so sandbox, bù bởi các cắt B2 §4 — vẫn trong trần **với điều kiện** không phát sinh
tính năng mới ngoài bảng này.

---

## Summary

**Total User Stories: 44**

| Epic | Stories | Total SP | Priority Distribution |
|------|---------|----------|----------------------|
| 1 Customer Order Management | 9 | 70 | Critical: 5, High: 2, Medium: 2 |
| 2 Lab Operations | 7 | 55 | Critical: 4, Medium: 3 |
| 3 Hub Quality Control | 4 | 26 | Critical: 1, High: 3 |
| 4 Scheduling and Assignment | 8 | 82 | Critical: 6, High: 2 |
| 5 Platform Administration | 4 | 23 | High: 2, Medium: 2 |
| 6 Monitoring and Operations | 3 | 24 | High: 1, Medium: 2 |
| 7 Shop-Floor Handover & Hub Logistics | 1 | 3 | Critical: 1 |
| 8 Calibration & Evidence Operations | 6 | 22 | Critical: 3, High: 3* |
| 9 Evaluation & Evidence | 2 | 16 | Critical: 2 |

*US-039 Medium — đếm ở Epic 8: Critical US-041/043=2, High US-037/038/040=3, Medium US-039=1 (tổng 18 SP; bảng làm tròn theo nhóm).

**Total Story Points: ~331** → 2-week sprints, velocity ~35 → **≈9–10 sprints**, vẫn trong khung 24 tuần khi giữ nguyên cut-list B2 §4.

**Sprint Planning Recommendation:**
- Sprint 1–2: US-001→006 (Customer core) + US-021/022 (phân tích + slicing — khởi động sớm vì critical path)
- Sprint 2–3: **US-042 một phần** (harness khung + order generator, deadline tuần 4 — B2 §5)
- Sprint 3–4: US-023→027 (engine CORE) + US-010→012 (lab setup để engine có dữ liệu filter)
- Sprint 5–6: US-013/014/036 (lab production chain) + US-017→020, US-041 (hub chain)
- Sprint 7–8: US-029→035, US-037–040, US-043 (admin/ops/evidence)
- Sprint 9–10: toàn bộ nhánh evaluation + hardening

**Critical Path:** US-001 → US-021 → US-022 → US-023 → US-024 → US-025 → US-026 → US-027 → US-042 (đo) → US-043 (giải trình)

**Đồng bộ danh ngữ:** Mọi story giờ có Traces: FR-xx · UC-xx · BR-xxx — khớp `capstone/workbook/07_Traceability.md`; story không traces = không được chấm acceptance.
