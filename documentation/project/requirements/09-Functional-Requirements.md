# Functional Requirements - PrintGrid Platform

## 0. Bảng yêu cầu (requirement matrix — format nộp)

**Legend Status:** `Approved` = đã nằm trong phiếu v1.0 được duyệt · `Proposed` = FR sinh ra từ phân tích, chờ chốt ở phiếu v1.1/v1.2 · `⚠B3` = nội dung phụ thuộc quyết định trong biên bản họp GVHD.
**Priority:** MoSCoW. **Source:** mã P-xx từ `capstone/workbook/01_Extract-P-xx.md`.

| Req ID | Type | Requirement Description | Category | Priority | Source | Acceptance Criteria | Status | Notes |
|--------|------|-------------------------|----------|----------|--------|---------------------|--------|-------|
| FR-CUST-001 | FR | Customer registers, signs in, manages profile and delivery addresses | Customer | Must | P-19 | Verified email required to order; address delete blocked while undelivered order exists | Approved | Actor: Customer · BR: ACCESS-005 · UC-001,026 · US-001,040 · M |
| FR-CUST-002 | FR | Upload 3D model (STL/OBJ/3MF, ≤50 MB) into secure storage | Customer | Must | P-20 | Unsupported format rejected with message; quota per account enforced; **SHA-256 hash recorded + customer email receipt (time,size,hash)** | Approved · **R1 amended** | Actor: Customer · BR: ACCESS-006, IP-001/002 · UC-001 · US-001 · M |
| FR-CUST-003 | FR | In-browser 3D preview with bounding-box readout | Customer | Should | P-20 | Model renders; rotate/zoom; dims match analysis result | Approved | Actor: Customer · UC-001 · US-002 · L — cut-candidate #2 |
| FR-CUST-004 | FR | Validation feedback on mesh integrity and oversize | Customer | Must | P-21 | Issues listed per item; part exceeding max build volume cannot proceed to quote | Approved | Actor: Customer · BR: QUOTE-006 · UC-002 · US-003 · M |
| FR-CUST-005 | FR | Per-item print configuration (material, colour, layer, infill, post-process, qty, service level, required-by) | Customer | Must | P-22, P-23 | Invalid combinations disabled with reason; config persists with draft | Approved | Actor: Customer · BR: CONFIG-003 · UC-001 · US-004 · M |
| FR-CUST-006 | FR | Automatic quote: cost breakdown, committed delivery date, expiry | Customer | Must | P-24 | Breakdown sum = total **incl. shipping line (BR-LOG-004)**; date from trial placement not constant table; expiry enforced **from approval moment**; **output = engine DRAFT pending staff/auto-lane approval (BR-QUOTE-008)** | Approved · **R1 amended** | Actor: Customer · BR: QUOTE-001..008, PAY-001.. · UC-001/029 · US-005 · L* (đếm công ở SCHED-005/010) |
| FR-CUST-007 | FR | Confirm quote into order (address, terms) | Customer | Must | P-25 | Cannot checkout without address + T&C; order created in one txn with Quote | Approved | Actor: Customer · BR: QUOTE-003, PAY-001 · UC-001 · US-006 · M · **C4: cắt promo codes** |
| FR-CUST-008 | FR | Pay real money via SEPay hosted QR checkout; commit order only on verified webhook + order-confirmation API call; system stores token + txn id + state only | Customer | Must | P-25 | QR expiry = quote expiry; fake/unconfirmed webhook never commits order; idempotent by txnId; no card data in DB (BR-PAY-005) | Approved (SEPay — **dời tuần 7**; T1–2 dùng bridge mark-paid) | Actor: Customer · BR: PAY-001, PAY-005 · UC-001 · US-006 · M · +reconciliation = NFR-SEC-009 |
| FR-CUST-009 | FR | Track order/items in real time without lab visibility | Customer | Must | P-26 | No lab name/id anywhere in customer views; status change notified <1 phút | Approved | Actor: Customer · BR: NOTIFY-001 · UC-003 · US-007 · M |
| FR-CUST-010 | FR | Personal model library with reorder | Customer | Could | P-28 | Reorder prefills model + last config into new quote draft | Approved | Actor: Customer · BR: ACCESS-006 · UC-026 · US-008,040 · S |
| FR-CUST-011 | FR | Raise reprint/complaint within guarantee window | Customer | Must | P-27 | Request rejected after 30d; request + photos trackable | Approved | Actor: Customer · BR: QC-009, RESCHED-004 · UC-007 · US-009 · M |
| FR-LAB-001 | FR | Register lab: calendar, capacity declaration, transfer time | Lab | Must | P-29 | Activation only after admin approval; declaration validated complete | Approved (lab confirmed 23/09) | Actor: Lab Mgr · BR: OPS-005/006 · UC-014 · US-010 · M · partner lab = user đầu tiên của form này |
| FR-LAB-002 | FR | Maintain machine registry with full capability spec | Lab | Must | P-30 | Filter sees spec change <1 phút; delete blocked with active jobs | Approved (lab confirmed 23/09) | Actor: Lab Mgr · BR: OPS-006 · UC-014 · US-011 · M · spec thật của lab đối tác nhập qua đây |
| FR-LAB-003 | FR | Maintain material/colour inventory | Lab | Must | P-31, P-116 | Consumption auto-deducted on completion; low-stock alert fires; **reserve-on-accept / settle-with-actuals (BR-STOCK-001); reserve = qty×g×(1+1–5% tol) (BR-STOCK-002); every movement = transaction record w/ code + running total (BR-STOCK-003)** | Approved · **R1 amended** | Actor: Lab Mgr · BR: ASSIGN-001(in), STOCK-001..003 · UC-014 · US-012 · M · nguồn stock thật cho filter |
| FR-LAB-004 | FR | Respond to job offers (accept/decline) within response window, reason on decline | Lab | Must | P-32, P-108 | 2h countdown visible; timeout auto-declines + triggers repair; **offers arrive via fan-out top-k, first valid accept wins (BR-ASSIGN-010); acceptance includes file-hash acknowledgement (BR-IP-003)** | Approved · **R1 amended** | Actor: Lab Mgr · BR: ASSIGN-005/006/010, IP-003 · UC-004 · US-013 · M · B5 chờ xác nhận auto-decline |
| FR-LAB-005 | FR | Operator workflow: queue view, advance state, report actuals/incidents, handover | Lab | Must | P-35→P-40, P-110 | Actual duration mandatory on completion; incident accepts photos; handover creates batch; **self-QC photo proof + quality report required at completion (BR-QC-011); handover blocked until staff approves proof (BR-QC-012)** | Approved · **R1 amended** | Actor: Lab Op · BR: ESTIM-001, SCHED-009, QC-011/012 · UC-005,015 · US-014,036 · L — tách 4 sub |
| FR-LAB-006 | FR | View machine-level schedule timeline | Lab | Should | P-33 | Gantt per machine updates after every reschedule | Approved | Actor: Lab Mgr · BR: SCHED · UC-011(view) · US-015 · M |
| FR-LAB-007 | FR | View own performance standing and underlying figures | Lab | Should | P-34 | Metrics match ledger; anonymized network comparison | Approved | Actor: Lab Mgr · BR: PERF-004 · UC-008 · US-016 · M |
| FR-HUB-001 | FR | Receive lab batches, reconcile against expected jobs | Hub | Must | P-41 | MISSING/DAMAGED flags create incident + notify lab & ops same day | Approved | Actor: Hub QC · BR: QC-001/010 · UC-016 · US-017,036 · M |
| FR-HUB-002 | FR | Execute inspection checklist bound to quality grade; record pass/fail + defect + fault | Hub | Must | P-42, P-43, P-44 | Every fail has defect class + attribution + photos; version of checklist stored | Approved | Actor: Hub QC · BR: QC-002/003/004 · UC-006 · US-018 · L — tách run/attribution |
| FR-HUB-003 | FR | Trigger reprint and confirm revised commitment | Hub | Must | P-45 | Reprint inherits original deadline; limit 2 → escalation | Approved | Actor: Hub QC · BR: QC-005..007, RESCHED-003 · UC-007,013 · US-019 · M |
| FR-HUB-004 | FR | Consolidate order items, verify completeness before packing | Hub | Must | P-46 | Pack blocked unless all items PASSED_QC; outstanding report shown | Approved | Actor: Hub Fulf. · BR: QC-008 · UC-017 · US-020 · M |
| FR-HUB-005 | FR | Pack, record waybill (manual), ship and confirm delivery opening guarantee window | Hub | Must | P-47, P-48 | DeliveredAt recorded; 30d window computed from it; SHIPPED notify customer | Proposed | Actor: Hub Fulf. · BR: QC-009, NOTIFY-001 · UC-017 · US-041 · M · carrier API = No (B2) |
| FR-SCHED-001 | FR | Analyze geometry: bbox, volume, watertight, manifold | Scheduling | Must | P-54 | <10s; bbox chính xác 0.1mm; results stored with model | Approved | Actor: Engine · BR: ESTIM · UC-002 · US-021 · L |
| FR-SCHED-002 | FR | Slice headless to estimate print time + materials | Scheduling | Must | P-54 | ≤60s typical model; estimate reported with raw values | ⚠B3 | Actor: Engine · BR: ESTIM · UC-002 · US-022 · L · engine chọn ở pilot (giả định A1) |
| FR-SCHED-003 | FR | Hard feasibility filter (technology, volume, tolerance, stock, state, standing) | Scheduling | Must | P-58 | 0 infeasible assignment in adversarial suite — test độc lập scoring | Approved | Actor: Engine · BR: ASSIGN-001 · UC-011 · US-023 · M |
| FR-SCHED-004 | FR | Multi-criteria weighted scoring over feasible set | Scheduling | Must | P-58 | Weights sum 1.0; score 0–1; deterministic with same inputs+version | Approved | Actor: Engine · BR: ASSIGN-002/003/007 · UC-011 · US-024 · M · A3: MVP weights cố định |
| FR-SCHED-005 | FR | Speculative quote-time placement returning earliest feasible date (+surcharge) | Scheduling | Must | P-56 | ≤60s; trial không đụng lịch thật; date đổi khi load đổi | Approved | Actor: Engine · BR: SCHED-001, QUOTE-002 · UC-010 · US-025 · L · A5 crude-first |
| FR-SCHED-006 | FR | Decompose order into jobs; place on machine queues with backward due dates | Scheduling | Must | P-57, P-59 | Internal due ≥ print+post time; 0 double-booking under concurrency test **(at declared granularity — workshop-level labs get single virtual machine, BR-LAB-009)**; **multi-qty item splittable across capable labs at unchanged price (BR-SCHED-010)** | Approved · **R1 amended** | Actor: Engine · BR: SCHED-001..004/010, REL(N-04), LAB-009 · UC-011 · US-026 · L · A4: consolidation → Should |
| FR-SCHED-007 | FR | Repair unstarted plan on events within time budget; keep fallback | Scheduling | Must | P-60 | ≤30s hard; IN_PROGRESS jobs untouched; fallback always committed if improve fail; **repair order of preference: split-quantity → reassign → propose date change to customer, never silent lateness (BR-SCHED-010/011)** | Approved · **R1 amended** | Actor: Engine/Scheduler · BR: RESCHED-001..004, SCHED-005..008/010/011 · UC-012 · US-027 · L · A6 scope |
| FR-SCHED-008 | FR | Calibration loop: samples per machine model → correction factors applied to quotes/schedule | Scheduling | Should | P-61 | MVP: admin nhập factor có preview impact; MAPE hiển thị | ⚠B3 | Actor: Scheduler · BR: ESTIM-002..006 · UC-018 · US-028,037 · M · A7 |
| FR-SCHED-009 | FR | Assignment decision log: candidates + per-criterion scores + config version | Scheduling | Must | P-62 | Bất kỳ job đã gán truy vấn ra đúng candidate set; log fail ⇒ rollback gán | Proposed | Actor: Engine · BR: ASSIGN-004, CONFIG-004 · UC-011 · US-043 · M |
| FR-SCHED-010 | FR | Pricing engine: one formula, active parameter set, frozen onto quote | Scheduling | Must | P-55 | Re-run quote với version đóng băng = cùng giá sau khi đổi tham số | Proposed | Actor: Engine · BR: QUOTE-001..004 · UC-009,010 · US-005 · M |
| FR-ANAL-001 | FR | Network monitor: open orders, at-risk jobs, load & standing per lab | Analytics | Should | P-49 | At-risk = slack < ngưỡng cfg (4h); refresh ≤30s; drill-down tới từng job | Approved | Actor: Ops · BR: OPS-007 · UC-019 · US-033 · L |
| FR-ANAL-002 | FR | Lab performance ledger: metrics computed on sliding window | Analytics | Should | P-34, P-53 | Metrics tái dựng từ job history; min sample gate hoạt động | Approved | Actor: Ops · BR: PERF-001/002 · UC-008 · US-016,033 · M · A9: chưa vào score |
| FR-ANAL-003 | FR | SLA reports & export (CSV/PDF) | Analytics | Should | P-53 | Report period đúng số liệu; export rỗng hiện "no data" thay vì 0 | Approved | Actor: Ops · BR: CONFIG-004, OPS-008 · UC-021 · US-035 · M |
| FR-ANAL-004 | FR | Manual intervention: reassign, priority, suspend/cap — có lý do, audit | Analytics | Should | P-50, P-51, P-52 | Không override bypass filter cứng (B11); impact preview; 2-step khi đổi date khách | Approved | Actor: Ops · BR: OPS-001..004 · UC-020 · US-034 · L — tách 3 |
| **FR-ANAL-005** | FR | Approve & execute refund via gateway (full/partial per item) with daily reconciliation report | Analytics | Must | P-73 + 23/09 | Refund ≤5 business days (BR-PAY-006); idempotent — không hoàn 2 lần cùng txn; đối soát lệch = 0 | Proposed (quyết 23/09) | Actor: Ops · BR: PAY-004/006 · UC-028 · US-044 · M |
| FR-ADMIN-001 | FR | Manage accounts, roles, permissions across all parties | Admin | Must | P-63 | RBAC 100%; self-lockout blocked; single staff role | Approved | Actor: SysAdmin · BR: ACCESS-005 · UC-022 · US-029 · M |
| FR-ADMIN-002 | FR | Configure pricing/weights/limits/thresholds without redeployment | Admin | Must | P-65 | Đổi có hiệu lực ≤1 phút, không restart; quote cũ giữ version cũ | Approved | Actor: SysAdmin · BR: CONFIG-001..003 · UC-009 · US-030 · M |
| FR-ADMIN-003 | FR | Maintain shared catalogues (materials, colours, tech, grades, post-processing) | Admin | Must | P-64 | In-use item chỉ deprecate, không delete; propagate <1 phút | Approved | Actor: SysAdmin · BR: CONFIG-003 · UC-023 · US-031 · M |
| FR-ADMIN-004 | FR | Audit log viewer + background job/storage health | Admin | Should | P-67, P-68 | Log append-only tìm <2s; failed job replay được | ⚠B3 | Actor: SysAdmin · BR: CONFIG-002/004 · UC-025 · US-032,039 · M · **C3: đề xuất hạ phần health về runbook** |
| FR-ADMIN-005 | FR | Manage inspection checklists and defect taxonomy | Admin | Should | P-66 | Checklist versioned; taxonomy đã dùng chỉ DEPRECATED | Proposed | Actor: SysAdmin · BR: QC-002 · UC-024 · US-038 · M |
| FR-CUST-012 | FR | Staged payment: verified deposit commits order to production; balance auto-invoiced, gate blocks pack until settled | Customer | Must | P-109 | Deposit ≥ cfg % confirmed (webhook+API) → production start; balance unpaid → pack gate refused; each txn idempotent; QR expiry rules apply to both legs | Proposed (R1) | Actor: Customer · BR: PAY-007/008 · UC-031 · M ⚠B19/B20 · bridge mark-paid T1–2 covers deposit leg only |
| FR-CUST-013 | FR | Design-request intake: idea + references → ops queue → staged design quote → deliverable STL attached to order draft | Customer | Should | P-115 | Request logged & trackable; deliverable customer-owned, reuse by platform/labs prohibited (BR-IP-004); payment stages reuse FR-CUST-012 machinery | Proposed (R1) | Actor: Customer, Order Staff · UC-032 · M · design work itself = runbook, out of system |
| FR-ANAL-006 | FR | Quote review workbench: staff sees engine draft (price, date, breakdown), adjusts within band, approves → publish to customer | Analytics | Must | P-107 | Draft ≤60s stays machine SLA; publish SLA = staff queue cfg (default 2h); adjustments logged before/after + reason; auto-lane skips queue under cfg threshold | Proposed (R1) | Actor: Order Staff · BR: QUOTE-008 · UC-029 · L · feeds quote expiry from approval time |
| FR-ANAL-007 | FR | Lab QC-proof approval queue: staff approves/rejects photo proof before batch may ship to hub | Analytics | Must | P-110 | Batch handover blocked until proof APPROVED; rejection returns job to lab w/ reason; every approval audited | Proposed (R1) | Actor: Order Staff · BR: QC-011/012 · UC-030 · M |
| FR-HUB-006 | FR | Auto-merge shipments: same customer+address open orders grouped when ready-window met; one waybill | Hub | Should | P-112 | Merge only all-QC-passed + balance settled; customer may opt out per order; refunds/complaints remain per original order (BR-LOG-002) | Proposed (R1) | Actor: Hub Fulfillment · BR: LOG-001/002 · UC-017 · M |
| FR-LAB-008 | FR | ShipmentBatch planner: accumulate completed+proof-approved jobs per lab into scheduled hub-bound runs | Lab | Should | P-112 | Batch lists jobs + weights + zone allowance preview; run creation respects lab's declared transfer calendar | Proposed (R1) | Actor: Lab Manager · BR: LOG-005 · UC-015,033 · M |

\* CUST-006 đếm ngày công một lần qua SCHED-005/010. Bảng giữ nguyên chi tiết từng FR ở các mục 1–7 bên dưới.

---

## 1. Customer Module (FR-CUST)

### FR-CUST-001: User Registration and Authentication
**Priority:** High  
**Description:** Customers must be able to register and authenticate to access the platform.

**Requirements:**
- Register with email, password, full name
- Email verification required
- Login with email and password
- Password reset functionality
- Session management
- Remember me option

**Acceptance Criteria:**
- User can create account and receive verification email
- User can login with verified credentials
- Password meets security requirements (min 8 chars, uppercase, lowercase, number)
- Failed login attempts tracked and limited

---

### FR-CUST-002: 3D Model Upload
**Priority:** Critical  
**Description:** Customers can upload 3D model files in standard formats.

**Requirements:**
- Support STL, OBJ, 3MF formats
- File size limit: 50 MB
- Upload progress indicator
- Multi-file upload for batch orders
- Model stored securely in MinIO

**Acceptance Criteria:**
- Valid files upload successfully
- Invalid formats rejected with clear error
- Upload progress shown in real-time
- Files stored and retrievable

---

### FR-CUST-003: 3D Model Preview
**Priority:** High  
**Description:** Customers can preview uploaded models in browser before ordering.

**Requirements:**
- Interactive 3D viewer (rotate, zoom, pan)
- Display bounding box dimensions
- Show model statistics (vertices, faces)
- Wireframe and solid view modes

**Acceptance Criteria:**
- Model renders correctly in browser
- Controls responsive and intuitive
- Dimensions displayed accurately
- Works on desktop and tablet

---

### FR-CUST-004: Model Validation
**Priority:** Critical  
**Description:** System validates model geometry and provides feedback.

**Requirements:**
- Check mesh watertightness
- Detect non-manifold edges
- Validate build volume feasibility
- Suggest auto-repair for common issues

**Acceptance Criteria:**
- Valid models pass validation
- Invalid models rejected with specific issues listed
- Auto-repair offers for fixable issues
- Validation completes within 10 seconds

---

### FR-CUST-005: Print Configuration
**Priority:** Critical  
**Description:** Customers configure print parameters for their model.

**Requirements:**
- Material selection (PLA, PETG, ABS, etc.)
- Color selection
- Quality grade (Draft, Standard, High, Ultra)
- Infill density (10%, 30%, 50%, 100%)
- Quantity
- Post-processing options
- Required delivery date

**Acceptance Criteria:**
- All configuration options visible and selectable
- Unavailable combinations disabled with explanation
- Configuration saved with model
- Can modify before ordering

---

### FR-CUST-006: Automated Quoting
**Priority:** Critical  
**Description:** System generates quotes with price and delivery date.

**Requirements:**
- Async processing for complex models
- Progress indicator during quote generation
- Price breakdown display
- Committed delivery date from trial placement
- Quote expiry time shown
- Save quote for later

**Acceptance Criteria:**
- Quote generated within 60 seconds
- Price accurate based on configuration
- Delivery date realistic and achievable
- Quote expiry enforced

---

### FR-CUST-007: Order Placement
**Priority:** Critical  
**Description:** Customers can review and confirm orders.

**Requirements:**
- Review cart before checkout
- Apply promo codes
- Add delivery address
- Select shipping method
- Terms and conditions acceptance

**Acceptance Criteria:**
- Order summary shows all details correctly
- Cannot proceed without required info
- Validation prevents invalid orders

---

### FR-CUST-008: Payment Processing
**Priority:** Critical  
**Description:** Secure payment processing for orders.

**Requirements:**
- Integration with SEPay (create transaction → VietQR hosted page/QR image)
- QR/payment deadline equals quote expiry (default 48h)
- Webhook received → MUST call SEPay order-confirmation API (amount + description match) before any state change; return URL is navigation only, never proof of payment
- Idempotent handling by transaction id (webhook may arrive multiple times)
- Failed/expired payment handling; receipt generation

**Acceptance Criteria:**
- Payment processed securely
- Confirmation displayed immediately
- Failed payments handled gracefully
- Receipt emailed to customer

---

### FR-CUST-009: Order Tracking
**Priority:** High  
**Description:** Real-time order status visibility.

**Requirements:**
- Order status stages displayed
- Estimated completion time
- Real-time updates via SignalR
- Email notifications on status change
- No visibility of which lab is producing

**Acceptance Criteria:**
- Status updates reflect actual progress
- Notifications received timely
- Estimated times update dynamically
- Lab identity hidden from customer

---

### FR-CUST-010: Model Library
**Priority:** Medium  
**Description:** Manage previously uploaded models.

**Requirements:**
- View all uploaded models
- Thumbnail previews
- Model metadata (name, dimensions, upload date)
- Delete models
- Reorder from library

**Acceptance Criteria:**
- Library shows all customer's models
- Thumbnails load quickly
- Reorder pre-fills configuration
- Deletion requires confirmation

---

### FR-CUST-011: Reprint/Complaint Request
**Priority:** Medium  
**Description:** Request reprint or raise quality complaints.

**Requirements:**
- Request form with issue description
- Photo upload capability
- Reason selection
- Reference original order
- Track request status

**Acceptance Criteria:**
- Request submitted successfully
- Photos attached and viewable
- Customer receives acknowledgment
- Status trackable

---

## 2. Lab Module (FR-LAB)

### FR-LAB-001: Lab Registration
**Priority:** High  
**Description:** Labs can register to join the network.

**Requirements:**
- Lab name, location, contact info
- Business hours and working calendar
- Distance/transfer time to hub
- Bank account details
- Documentation upload (business license, etc.)

**Acceptance Criteria:**
- Registration form validates input
- Submission triggers admin review
- Lab notified of approval status

---

### FR-LAB-002: Machine Registry
**Priority:** Critical  
**Description:** Labs maintain catalog of their machines.

**Requirements:**
- Add/edit/delete machines
- Specify: technology, brand, model, build volume, layer height range, tolerance
- Mark machine status (operational, maintenance, offline)
- Schedule maintenance windows
- View machine utilization

**Acceptance Criteria:**
- Machines listed with full specifications
- Status changes reflected in assignment
- Cannot delete machine with active jobs

---

### FR-LAB-003: Material Inventory
**Priority:** Critical  
**Description:** Track material stock levels.

**Requirements:**
- Add material types and colors
- Record quantity in stock
- Low stock alerts
- Consumption tracking per job
- Restock history

**Acceptance Criteria:**
- Inventory reflects current stock
- Alerts triggered at reorder point
- Consumption auto-updated on job completion

---

### FR-LAB-004: Job Assignment Notification
**Priority:** Critical  
**Description:** Receive and review assigned jobs.

**Requirements:**
- Real-time notification of assignment
- View job details (model preview, config, due date)
- Download model file (time-limited)
- Accept or reject with reason
- Response deadline enforced

**Acceptance Criteria:**
- Notifications received immediately
- Job details complete and accurate
- Model downloadable within time window
- Acceptance/rejection recorded

---

### FR-LAB-005: Production Workflow
**Priority:** Critical  
**Description:** Operators manage job execution.

**Requirements:**
- View machine queue
- Start job (marks in progress)
- Update status during production
- Report completion with actual time and material
- Report incidents with photos

**Acceptance Criteria:**
- Queue shows jobs in priority order
- Status updates reflected system-wide
- Actual data captured for calibration
- Incidents logged immediately

---

### FR-LAB-006: Schedule Visualization
**Priority:** Medium  
**Description:** View proposed schedule timeline.

**Requirements:**
- Machine-level Gantt chart
- Color-coded by priority
- Show job IDs and durations
- Filter by machine or material
- Print/export schedule

**Acceptance Criteria:**
- Timeline accurate and readable
- Updates after rescheduling
- Interactive (click for details)

---

### FR-LAB-007: Performance Dashboard
**Priority:** Medium  
**Description:** View own performance metrics.

**Requirements:**
- Display: ODR, FPY, acceptance rate, utilization
- Trend charts over time
- Comparison to network average
- Defect breakdown
- Revenue summary

**Acceptance Criteria:**
- Metrics calculated correctly
- Charts render clearly
- Network comparison anonymized
- Updates daily

---

## 3. Hub Module (FR-HUB)

### FR-HUB-001: Batch Receipt
**Priority:** High  
**Description:** Receive incoming shipments from labs.

**Requirements:**
- Scan batch barcode
- View expected jobs in batch
- Scan individual job QR codes
- Reconcile received vs expected
- Report discrepancies

**Acceptance Criteria:**
- Scanning captures job IDs correctly
- Discrepancies flagged immediately
- Labs notified of issues

---

### FR-HUB-002: Quality Inspection
**Priority:** Critical  
**Description:** Inspect parts per quality checklist.

**Requirements:**
- Load checklist for job's quality grade
- Mark each check pass/fail
- Capture photos (multiple angles)
- Overall pass/fail decision
- Classify defect if fail
- Attribute fault (lab/hub/customer)

**Acceptance Criteria:**
- Checklist items mandatory
- Photos required for failures
- Fault attribution rules enforced
- Decision triggers appropriate action

---

### FR-HUB-003: Reprint Management
**Priority:** High  
**Description:** Initiate reprints for failed parts.

**Requirements:**
- Auto-create reprint job on lab fault
- Set priority to URGENT
- Inherit original deadline
- Notify customer if delay expected
- Track reprint status

**Acceptance Criteria:**
- Reprint job created automatically
- Assigned via normal allocation
- Customer kept informed

---

### FR-HUB-004: Order Consolidation
**Priority:** High  
**Description:** Combine items for fulfillment.

**Requirements:**
- View orders ready for packing
- Check all items passed inspection
- Print packing slip
- Record package details
- Generate shipping label
- Scan to carrier

**Acceptance Criteria:**
- Only complete orders packed
- Packing slip accurate
- Tracking number captured
- Customer notified of shipment

---

## 4. Scheduling and Assignment Module (FR-SCHED)

### FR-SCHED-001: Geometry Analysis
**Priority:** Critical  
**Description:** Analyze uploaded models for scheduling inputs.

**Requirements:**
- Parse STL/OBJ/3MF files
- Calculate bounding box
- Compute volume
- Check mesh integrity

**Acceptance Criteria:**
- Analysis completes in <10s
- Dimensions accurate to 0.1mm
- Detects common issues

---

### FR-SCHED-002: Slicing Service
**Priority:** Critical  
**Description:** Generate toolpaths and estimates.

**Requirements:**
- Integrate CuraEngine or PrusaSlicer
- Map configuration to slicer profile
- Execute slicing asynchronously
- Extract print time estimate
- Calculate material consumption
- Store G-code (optional)

**Acceptance Criteria:**
- Slicing completes within 60s for typical models
- Estimates within 20% of actual (before calibration)
- Supports all material/technology combinations

---

### FR-SCHED-003: Capability Filtering
**Priority:** Critical  
**Description:** Filter labs by hard constraints.

**Requirements:**
- Technology match
- Build volume sufficient
- Tolerance achievable
- Material in stock
- Machine operational
- Lab in good standing

**Acceptance Criteria:**
- Only feasible labs returned
- Filter executes in <1s
- 100% accurate (no infeasible assignments)

---

### FR-SCHED-004: Multi-Criteria Scoring
**Priority:** Critical  
**Description:** Score feasible assignments.

**Requirements:**
- Calculate scores for: deadline slack, current load, quality history, transfer time, internal cost
- Apply configurable weights
- Normalize across different units
- Log scores for audit

**Acceptance Criteria:**
- Scores range 0-1
- Higher score = better fit
- Weights sum to 1.0
- Transparent and explainable

---

### FR-SCHED-005: Quote-Time Scheduling
**Priority:** Critical  
**Description:** Run trial placement for quoting.

**Requirements:**
- Treat as speculative order
- Run full assignment and placement
- Return earliest feasible delivery date
- Calculate priority surcharge if urgent
- Async execution

**Acceptance Criteria:**
- Completes within 60s
- Delivery date achievable
- Does not affect real schedule
- Quote expiry accounts for staleness

---

### FR-SCHED-006: Job Placement
**Priority:** Critical  
**Description:** Assign confirmed orders to machines.

**Requirements:**
- Run capability filter and scoring
- Select top-scored lab/machine
- Calculate internal due date (backward from delivery)
- Check for batch consolidation opportunity
- Create job assignment record

**Acceptance Criteria:**
- Jobs assigned to feasible machines
- Internal due dates allow deadline
- Batching saves changeover time
- Assignment logged

---

### FR-SCHED-007: Event-Driven Rescheduling
**Priority:** Critical  
**Description:** Replan on production events.

**Requirements:**
- Trigger on: rejection, failure, breakdown, reprint, urgent insertion
- Identify affected jobs
- Generate dispatching rule fallback
- Improve within time budget (30s)
- Commit best solution found
- Notify stakeholders

**Acceptance Criteria:**
- Rescheduling completes within 30s
- Feasibility maintained
- In-progress jobs untouched
- Notifications sent

---

### FR-SCHED-008: Estimation Calibration
**Priority:** High  
**Description:** Learn from actual outcomes.

**Requirements:**
- Collect (estimated, actual) pairs
- Group by machine model
- Run regression per model
- Calculate correction factors
- Apply to future estimates
- Track MAPE over time

**Acceptance Criteria:**
- Calibration runs weekly
- Factors improve accuracy
- MAPE decreases over time
- Min 10 samples before applying

---

## 5. Analytics and Operations Module (FR-ANALYTICS)

### FR-ANALYTICS-001: Network Monitoring Dashboard
**Priority:** High  
**Description:** Real-time network visibility.

**Requirements:**
- Active orders count
- Jobs at risk (deadline within 4h)
- Current network load %
- Lab status summary (operational/at capacity/offline)
- Recent alerts

**Acceptance Criteria:**
- Updates every 30 seconds
- Drill-down to details
- Color-coded by urgency
- Exportable data

---

### FR-ANALYTICS-002: Lab Performance Tracking
**Priority:** High  
**Description:** Calculate and display lab metrics.

**Requirements:**
- On-time delivery rate
- First-pass yield
- Acceptance rate
- Utilization
- Performance score
- 90-day sliding window

**Acceptance Criteria:**
- Metrics accurate
- Updates daily
- Transparent calculation
- Historical trends visible

---

### FR-ANALYTICS-003: SLA Reporting
**Priority:** Medium  
**Description:** Generate compliance reports.

**Requirements:**
- Network-wide SLA metrics
- Per-lab breakdown
- Time period selection
- Export to PDF/Excel
- Trend analysis

**Acceptance Criteria:**
- Reports accurate and complete
- Generates within 10s
- Formatted professionally

---

### FR-ANALYTICS-004: Manual Intervention Tools
**Priority:** Medium  
**Description:** Operations manager overrides.

**Requirements:**
- Reassign job to different lab
- Adjust job priority
- Extend deadline (with customer notification)
- Suspend lab
- Cap lab capacity
- All with justification required

**Acceptance Criteria:**
- Interventions logged in audit trail
- Justification mandatory
- Feasibility still validated
- Stakeholders notified

---

## 6. Administration Module (FR-ADMIN)

### FR-ADMIN-001: User Management
**Priority:** High  
**Description:** Manage users and roles.

**Requirements:**
- Create/edit/disable users
- Assign roles (Customer, Lab Manager, Lab Operator, Hub Staff, Operations Manager, Admin)
- Reset passwords
- View login history
- Audit user actions

**Acceptance Criteria:**
- RBAC enforced
- Role changes immediate
- Disabled users cannot login

---

### FR-ADMIN-002: Configuration Management
**Priority:** High  
**Description:** Adjust business parameters.

**Requirements:**
- Pricing parameters (material rates, machine time, etc.)
- Assignment weights
- Scheduling limits (batch size, time budgets)
- SLA thresholds
- All versioned
- Audit trail

**Acceptance Criteria:**
- Changes create new version
- Old records use old version
- No code deployment required
- Validation prevents invalid values

---

### FR-ADMIN-003: Catalog Management
**Priority:** Medium  
**Description:** Maintain reference data.

**Requirements:**
- Materials (name, properties, rate)
- Colors (name, premium flag)
- Technologies (name, description)
- Quality grades (name, checklist)
- Post-processing services

**Acceptance Criteria:**
- CRUD operations functional
- Changes propagate to UI
- Cannot delete in-use items

---

### FR-ADMIN-004: Audit Log Viewer
**Priority:** Medium  
**Description:** Review system activity.

**Requirements:**
- Search by user, event type, date range
- View detailed event data
- Export logs
- Filter and sort

**Acceptance Criteria:**
- All sensitive actions logged
- Logs immutable
- Search performs <2s

---

## 7. FR bổ sung — vá "bullet ma" từ phiếu (sync `capstone/workbook/07_Traceability`)

### FR-HUB-005: Pack, Ship & Delivery Confirmation ★
**MoSCoW:** Must · **Actor chính:** Hub Fulfillment Staff · **Ư/L:** M · **UC:** 017 · **US:** 041
**Description:** Ghi nhận đóng gói, vận đơn (nhập tay — carrier API ngoài phạm vi) và hoàn tất giao hàng;
mở window bảo hành 30 ngày từ thời điểm xác nhận giao.
**Preconditions:** Toàn bộ items của đơn PASSED_QC (BR-QC-008).
**Main flow:** Quét items → gate completeness → tạo packing list → nhập carrier + waybill →
trạng thái SHIPPED + notify khách → xác nhận DELIVERED → mở BR-QC-009 window → đóng vòng đời đơn.
**Exception branches:** (E1) thiếu item → chặn pack, hiển thị job outstanding + lý do (đang reprint);
(E2) khách trả hàng/hỏng khi nhận → incident, quy lỗi hub/vận chuyển, không auto-refund (BR-PAY-004 là
quy trình ops, xem note rule chết ở 08 §0).
**Acceptance criteria:** (1) Không đơn nào chuyển SHIPPED khi còn item chưa PASSED_QC — kiểm bằng
b test; (2) guarantee window = DeliveredAt + 30 cfg; (3) mọi lần đổi trạng thái có actor + timestamp
trong audit log.
**Entities:** Shipment, Order, OrderItem · **BR:** QC-008, QC-009, NOTIFY-001, CONFIG-004

### FR-SCHED-009: Assignment Decision Log ★
**MoSCoW:** Must · **Actor chính:** Scheduling Engine (system) · **Ư/L:** M · **UC:** 011(post) · **US:** 043
**Description:** Lưu mọi quyết định gán/replan/override: tập candidate, điểm từng tiêu chí, config
version đang dùng, quyết định cuối và lý do — để giải trình khiếu nại của lab và debug thuật toán
(trả nợ NFR "decision auditability" P-76).
**Preconditions:** Một quyết định assignment/reschedule/override vừa được commit.
**Main flow:** Dựng snapshot candidate set (lab, machine, per-criterion scores, tổng điểm) → ghi
kèm seed config + actor (system hay ops) → append-only → cấp API trace lookup cho UC-020/025/043.
**Exception branches:** (E1) log write fail → transaction rollback toàn quyết định (không nhận việc
mà không có log — log là điều kiện cam kết); (E2) retention hết hạn → archive, không xóa (BR-CONFIG-004).
**Acceptance criteria:** (1) Với job bất kỳ đã gán, truy vấn trả về đúng danh sách candidate + điểm đã
chấm; (2) 100% override có entry nối vào decision thread của job; (3) không API nào sửa/xóa log.
**Entities:** DecisionLog, Job, ConfigParameter · **BR:** ASSIGN-004, CONFIG-002/004

### FR-SCHED-010: Network-Uniform Pricing Engine ★
**MoSCoW:** Must · **Actor chính:** Scheduling/Pricing Engine (system) · **Ư/L:** M · **UC:** 009,010 · **US:** 005
**Description:** Tính giá từ **một** bộ tham số đang active trên toàn mạng, đóng băng (freeze) phiên bản
tham số vào quote để tái lập được sau khi giá đổi. Tách khỏi FR-CUST-006 để kiểm thử độc lập.
**Preconditions:** PricingParameterSet version active tồn tại; kết quả phân tích geometry có sẵn.
**Main flow:** Nhận (volume, print time, material, grade, post-process, urgency) → tra rates của
version active → tính: material + machine time + post-processing + hub handling + shipping (+priority
surcharge) → gắn config_version_id vào quote.
**Exception branches:** (E1) material/grade không có rate → từ chối tính giá, báo admin qua catalog
gap (không nhân hệ số ngầm); (E2) quote cũ request lại sau khi đổi giá → dùng **version đóng băng**,
không dùng version mới (BR-QUOTE-004).
**Acceptance criteria:** (1) Cùng config + cùng version ⇒ cùng giá, chạy lại sau khi đổi tham số vẫn
đúng; (2) tổng breakdown = total (BR-QUOTE-005); (3) giá không phụ thuộc lab sẽ sản xuất (BR-QUOTE-001).
**Entities:** Quote, PricingParameterSet, OrderItem · **BR:** QUOTE-001..005

### FR-ADMIN-005: Inspection Checklist & Defect Taxonomy Manager ★
**MoSCoW:** Should · **Actor chính:** System Administrator · **Ư/L:** M · **UC:** 024 · **US:** 038
**Description:** Soạn checklist bound với quality grade và bảo trì defect taxonomy — tri nợ P-66 của
phiếu (SysAdmin FR thô đã có, đặc tả chưa).
**Preconditions:** Quality grades tồn tại (FR-ADMIN-003).
**Main flow:** Chọn grade → editor checklist (item, pass criteria, mandatory flag, thứ tự) → save
thành version mới → inspection đang chạy giữ version cũ; taxonomy add/rename/deprecate.
**Exception branches:** (E1) mã taxonomy đã được inspection history tham chiếu → chỉ DEPRECATED, cấm
delete (toàn vẹn attribution quá khứ); (E2) grade không còn checklist active → QC queue block + alert.
**Acceptance criteria:** (1) Inspection mới nhận đúng checklist version đang active; (2) version cũ
vẫn render được cho report quá khứ; (3) mọi edit có audit entry (BR-CONFIG-002).
**Entities:** Checklist, ChecklistItem, DefectTaxonomy, QualityGrade · **BR:** QC-002/004, CONFIG-001/003

---

### FR-ANAL-005: Refund Processing & Payment Reconciliation ★ (quyết định 23/09 — payment tiền thật)
**MoSCoW:** Must · **Actor chính:** Operations Manager · **Ư/L:** M · **UC:** 028 · **US:** 044
**Description:** Quản lý duyệt và thực thi hoàn tiền qua gateway (toàn phần hoặc theo item), kèm báo cáo
đối soát giao dịch hằng ngày giữa đơn hàng – giao dịch gateway – trạng thái đơn.
**Preconditions:** Đơn đã thanh toán thật; BR-PAY-004 trigger xác lập (lỗi nền tảng / từ chối reprint C2 / khách hủy theo điều kiện B16).
**Main flow:** Ops mở danh sách khiếu nại đủ điều kiện → chọn full/partial (chọn item) → hệ thống tính
số tiền theo công thức còn lại sau phí → gọi gateway API idempotent (refund key = txn + lần) → webhook
xác nhận → cập nhật trạng thái + notify khách → ghi audit + đưa vào đối soát ngày.
**Exception branches:** (E1) gateway từ chối/lỗi ⇒ retry có kiểm soát, không đổi trạng thái đơn khi chưa xác nhận; (E2) phát hiện lệch đối soát (txn success nhưng đơn production, hoặc ngược lại) ⇒ alert ops + khóa refund tự động của txn đó; (E3) refund quá hạn 5 ngày làm việc ⇒ escalation + lý do ghi log (BR-PAY-006).
**Acceptance criteria:** (1) Không tồn tại 2 refund thành công cho cùng một khoản đã hoàn; (2) báo cáo
đối soát T-1 chênh lệch = 0 hoặc mọi dòng lệch có lý do; (3) mọi refund có duyệt viên + lý do + thời gian.
**Entities:** Payment, Refund, Order, AuditLog · **BR:** PAY-004, PAY-006, NOTIFY-002

---

## 8. Ghi chú phạm vi sau phản biện (B2 §4) — sửa vào các block chi tiết khi chốt phiếu v1.1

| FR | Chỉnh |
|----|-------|
| CUST-008 | **25/09: payment để sau** — tuần 1–2 dùng BRIDGE `POST /orders/{id}/mark-paid` (ops, reason + audit) để luồng vàng không bị chặn; SEPay (QR + webhook + confirm-API hai bước, idempotent) dời **tuần 7** kèm refund/đối soát; xin app/secret trước tuần 7 |
| SCHED-006 | Batch consolidation → extended; MVP: dispatching EDD; "batching saves changeover" AC dời xuống Should-backlog |
| SCHED-004 | MVP weights **cố định** (cfg UI vẫn Must qua ADMIN-002 nhưng không phải chỉnh được ở release đầu) |
| SCHED-007 | Trigger MVP = rejection + print failure; breakdown/reprint → Should |
| SCHED-008 | MVP = nhập factor thủ công (US-037); auto-regression = Could |
| LAB-001/002/003 | **23/09: lab đối tác ĐÃ CÓ** → không còn là "seed tay" cut-candidate nữa; onboarding tự phục vụ vẫn Must (lab thật + mọi lab tương lai). Cut-candidate #1 giờ chuyển cho CUST-003 viewer |
| CUST-007 | Bỏ "apply promo codes" — không nằm ở bất kỳ đâu trong phiếu (feature tự thêm) |
| HUB-002 | Thêm AC: checklist version đóng băng theo UC-024 (BR-QC-002) |

---

## Summary

**Total Functional Requirements: 43** (38 đặc tả cũ — bản tổng kết cũ ghi "44" là lỗi đếm — + 4 ★ vá bullet ma + 1 ★ FR-ANAL-005 refund theo quyết định payment 23/09)

| Module | FRs | Must | Should | Could |
|--------|-----|------|--------|-------|
| Customer (CUST) | 11 | 9 | 1 | 1 |
| Lab (LAB) | 7 | 5 | 2 | 0 |
| Hub (HUB) | 5 | 5 | 0 | 0 |
| Scheduling/Pricing/Assign (SCHED) | 10 | 9 | 1 | 0 |
| Analytics/Ops (ANAL) | 5 | 1 | 4 | 0 |
| Admin (ADMIN) | 5 | 3 | 2 | 0 |
| **Tổng** | **49** | **35 (71%)** | **13 (27%)** | **1 (2%)** |

⚠ **Deviation có chủ đích với chuẩn "Must ≤ 60%":** tỷ lệ 71% (sau Review 1: 49 FR) là vì **baseline MVS của chính GVHD**
(P-105 mục 4 phiếu) liệt kê gần đủ 31 FR này. Khối lượng ước lượng Must ≈ 75–80 ngày công — vẫn khớp
trần 02 §4 **chừng nào** cut-candidates ở mục 8 được thực hiện khi trượt deadline (thứ tự cắt:
LAB-001/002/003 seed tay → CUST-003 viewer → SCHED-009 rút gọn M→S). Ghi chú này để hội đồng thấy
nhóm biết mình lệch chuẩn ở đâu và có kế hoạch, không phải chưa xét.

Mọi FR có: 1 actor chính ✓ · BR áp dụng ✓ · exception branch (trong UC) ✓ · AC đúng/sai ✓ ·
Ư/L ✓ · truy vết P-xx (xem `capstone/workbook/07_Traceability`) ✓.
