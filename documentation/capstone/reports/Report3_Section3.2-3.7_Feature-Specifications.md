# Report 3 — 3.2 → 3.7 Feature Specifications (PrintGrid)

**Nguồn:** `frontend/src/routes/AppRoutes.tsx`, `src/PrintGrid.Api/Controllers/*`, `BackgroundJobs/*`,
`documentation/project/requirements/09-Functional-Requirements.md`, `12-Use-Cases.md`.
Mọi endpoint ghi trong mục *Interface* là route thật trong controller (đã đối chiếu ngày 28/10/2026).

**Cấu trúc:** 6 feature · 36 function · mỗi function đủ 4 phần theo template
(*Function trigger · Function description · Screen layout · Function details*).

> **Quy ước ảnh:** `[ẢNH 3.2-a]` là chỗ dán screenshot; mã ảnh khớp với danh sách chụp màn hình.
> Với function không có màn hình, mục *Screen layout* ghi rõ "No dedicated screen" và chỉ ra màn
> hiển thị kết quả để chụp.

---

# 3.2 Customer Ordering & Account

Covers FR-CUST-001 … FR-CUST-013 plus the quote-time calls into the engine (FR-SCHED-005/010).

| Mục | Function | FR | UC |
|-----|----------|----|----|
| 3.2.1 | Configure and Place an Order | CUST-002…008, SCHED-005/010 | UC-001 |
| 3.2.2 | Upload and Preview a Model | CUST-002/003/004, SCHED-001 | UC-001/002 |
| 3.2.3 | Pay Deposit and Balance | CUST-008/012 | UC-031 |
| 3.2.4 | Track Order in Real Time | CUST-009 | UC-003 |
| 3.2.5 | Raise a Reprint or Complaint | CUST-011 | UC-007 |
| 3.2.6 | Manage Account, Addresses and Model Library | CUST-001/010 | UC-026 |
| 3.2.7 | Request a Design Service | CUST-013 | UC-032 |

## 3.2.1 Configure and Place an Order

**Function trigger**
`Model Library (/models)` → select a model → **"Đặt in"** → `Order Configuration (/models/:modelId/order)`;
alternatively `Start Order (/order/new)` → upload or pick a model. The function ends when the customer
presses **"Xác nhận & thanh toán cọc"**.

**Function description**
- **Actors:** Customer (primary); Pricing Engine and Scheduling Engine (system); Payment Gateway (external);
  Operations Manager (approves the quote draft when the auto-lane is not used).
- **Purpose:** Turn a validated model plus a print configuration into a priced, date-committed order whose
  production can start once the deposit is verified.
- **Interface:** `POST /api/v1/quotes` · `GET /api/v1/quotes/{quoteId}` · `POST /api/v1/orders` ·
  `GET /api/v1/orders/{orderId}/payment` · SignalR `OrderHub`.
- **Data processing:**
  1. Load the model and its stored analysis; block configuration when the model is `INFEASIBLE`.
  2. Each configuration change invalidates the previous quote.
  3. On "Nhận báo giá": the quote request calls the Pricing Engine (freezes the active parameter version)
     and the Scheduling Engine (speculative placement → earliest feasible date).
  4. Quote is stored `DRAFT`; it becomes `PUBLISHED` after `POST /quote-reviews/{quoteId}/approve`
     (or immediately when the auto-lane threshold is met). Only then does the expiry clock start.
  5. Checkout validates the address and terms, then creates the `Order` in one transaction with the quote.
  6. A deposit payment record is issued; the order moves to `PAID` only after a verified gateway callback.

**Screen layout**
`[ẢNH 3.2-a]` — `Order Configuration (/models/:modelId/order)`, full page, three zones:
(A) 3D viewer + bounding box; (B) configuration form; (C) sticky quote panel with breakdown, total,
committed delivery date and expiry. Capture a state where the quote is already published (not "Đang phân tích…").
Supporting: `[ẢNH 3.2-b]` Model Library with the "Thêm model mới" dialog open.

**Function details**

| Field | Control | Validation / rule |
|-------|---------|-------------------|
| Material | Select | Only catalogue materials valid for the selected technology; invalid options disabled with a reason (BR-CONFIG-003) |
| Colour | Select | Filtered by material; unavailable colour disabled |
| Layer height / quality | Select | Allowed set per machine profile; affects time and price |
| Infill | Slider/Select | Range defined by configuration |
| Post-processing | Multi-select | Each option adds its own lead time to the due-date computation |
| Quantity | Number | ≥ 1; a multi-quantity item may be split across capable labs at an unchanged unit price (BR-SCHED-010) |
| Service level | Select | Standard / Express; Express may add a surcharge |
| Required-by date | Date | Must be ≥ the earliest feasible date returned by speculative placement |
| Delivery address | Select + add | Mandatory at checkout; an address used by an open order cannot be deleted |
| Terms | Checkbox | Mandatory before order creation |

**Business rules:** BR-QUOTE-001…006 (one formula, one active parameter set, frozen version, breakdown sums
to total), BR-LOG-004 (shipping is an explicit line), BR-QUOTE-008 (customer-visible only after approval),
BR-CONFIG-003 (invalid combinations disabled), BR-SCHED-010 (splitting never changes price).

**Normal case:** model analysed → configure → quote draft → approval → published quote → checkout →
deposit verified → order `PAID` → jobs assigned → confirmation email.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Mesh not watertight / oversize | Validation list per item; quote button disabled (FR-CUST-004) |
| No feasible lab for the requested date | Quote refused; earliest feasible date offered instead |
| Quote expires before checkout | Checkout blocked; a fresh quote is required |
| Gateway callback not verified | Order stays `PAYMENT_PENDING`; production does not start |
| Customer abandons mid-configuration | Draft configuration persists and is restored |

---

## 3.2.2 Upload and Preview a Model

**Function trigger**
`Model Library (/models)` → **"Thêm model mới"**, or `Start Order (/order/new)` → **"Tải file 3D mới"**.
Preview is opened by clicking a model row.

**Function description**
- **Actors:** Customer (primary); `AnalyzeModelJob` (system, asynchronous); email outbox (system).
- **Purpose:** Accept a printable file, prove custody of it (hash + receipt), and produce the geometry data
  every later step depends on.
- **Interface:** `GET /api/v1/models/quota` · `POST /api/v1/models/upload` · `GET /api/v1/models` ·
  `GET /api/v1/models/{modelId}` · `GET /api/v1/models/{modelId}/file` · `DELETE /api/v1/models/{modelId}`.
- **Data processing:**
  1. Validate format, size and the account quota before accepting the stream.
  2. Store the file under a per-customer key.
  3. Compute SHA-256 and persist it with size and format.
  4. Queue a receipt email containing time, size and hash (BR-IP-002).
  5. Enqueue `AnalyzeModelJob`; the model shows `PENDING` until analysis returns `ANALYSED` or `INFEASIBLE`.
  6. The preview reads the stored bbox/volume and printability flags.

**Screen layout**
`[ẢNH 3.2-b]` — Model Library with the upload dialog open (file picker, name field, validation error area).
`[ẢNH 3.2-c]` — Model Preview showing bbox X×Y×Z, volume, print-time estimate and the printability badge.

**Function details**

| Rule | Value / behaviour |
|------|-------------------|
| Accepted formats | STL, OBJ, 3MF, GLB |
| Maximum size | 50 MB per file (configurable) |
| Quota | Per-account; exceeded quota blocks upload and shows prune guidance |
| Integrity evidence | SHA-256 stored; receipt email sent to the customer |
| Analysis latency | Target < 10 s for a typical model; UI shows a progress state |
| Delete guard | A model referenced by an open order cannot be deleted |

**Normal case:** upload accepted → hash + receipt → analysis completes → model appears as `ANALYSED` with a
preview thumbnail and can be configured.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Unsupported extension / oversize | Upload rejected with an inline message; nothing stored |
| Quota exhausted | Blocked with "xóa model không còn sử dụng" guidance |
| Corrupted mesh | Analysis returns `INFEASIBLE` with the reason; configuration is disabled |
| Analysis worker failure | Model stays `PENDING`; retry is available and the failure is logged |

---

## 3.2.3 Pay Deposit and Balance

**Function trigger**
Deposit: **"Xác nhận & thanh toán cọc"** at checkout. Balance: when the order reaches pack-readiness, the
customer opens the order and pays the outstanding amount from the payment panel.

**Function description**
- **Actors:** Customer; Payment Gateway (external); Operations Manager (bridge path while the gateway is
  not live).
- **Purpose:** Collect money in two legs so production starts on a verified deposit and shipping is gated
  on a settled balance.
- **Interface:** `GET /api/v1/orders/{orderId}/payment` · `GET /api/v1/orders/pending-payment` ·
  `POST /api/v1/orders/{orderId}/mark-paid` (bridge/dev only) · gateway webhook.
- **Data processing:**
  1. Order is created as `PAYMENT_PENDING` with `DepositAmount` computed from the configured percentage.
  2. A QR/payment record is issued with the same expiry as the quote.
  3. A verified gateway callback (signature + transaction id) is required; the call is idempotent by
     transaction id, so a duplicated webhook cannot double-charge.
  4. On confirmation the order becomes `PAID`, the deposit is recorded in `PaidAmount`, and assignment runs.
  5. When the order becomes ready for packing, the balance leg is invoiced; until it is settled the hub
     pack gate refuses the order.
  6. While the gateway is not live, Operations records the payment through `mark-paid`, which writes the
     reference and an audit entry.

**Screen layout**
`[ẢNH 3.2-d]` — the payment panel inside the Order Details drawer: amount, QR, expiry and current state.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Idempotency | Keyed on gateway transaction id; replaying the callback changes nothing |
| QR validity | Equal to the quote validity; expired QR requires a new quote |
| Deposit percentage | Commercial configuration (FR-ADMIN-002), not hard-coded |
| Balance gate | Hub cannot pack or ship until `PaidAmount` covers the total |
| No card data | Only gateway token, transaction id and state are stored (BR-PAY-005) |
| Unpaid balance | Escalation after the configured window; packing stays blocked |

**Normal case:** checkout → deposit QR → verified callback → `PAID` → production starts → balance invoiced
at pack-readiness → paid → pack gate opens.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Unverified/forged webhook | Ignored; order remains unpaid; attempt is logged |
| Duplicate webhook | Idempotent no-op |
| Deposit expires | Re-quote required; the old order is not silently revived |
| Balance unpaid past the window | Escalated to Operations; shipment held |
| Gateway timeout | Safe retry; no silent commit |

---

## 3.2.4 Track Order in Real Time

**Function trigger**
Navigation to `My Orders (/orders)`; the order details drawer opens from a row.

**Function description**
- **Actors:** Customer; SignalR `OrderHub` (system).
- **Purpose:** Show progress and the promised date without ever revealing which lab is producing the part.
- **Interface:** `GET /api/v1/orders` · `GET /api/v1/orders/{id}/timeline` · SignalR `OrderHub`.
- **Data processing:**
  1. List the customer's own orders only (ownership isolation at query level).
  2. Compute an at-risk flag from the slack between now and the promised date.
  3. The timeline endpoint returns stage events with timestamps; lab name/id are never projected.
  4. Status changes are pushed over SignalR and reflected without a manual refresh.

**Screen layout**
`[ẢNH 3.2-e]` — `My Orders` with at least three different statuses visible.
`[ẢNH 3.2-f]` — Order Details drawer showing the timeline block.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Visibility | Customer-scoped query; no cross-customer data |
| Lab anonymity | No lab name, id or location in any customer payload (BR-NOTIFY-001) |
| Status set | Chờ thanh toán → Đã xác nhận → Đang sản xuất → Kiểm tra chất lượng → Đang giao → Đã giao |
| At-risk badge | Shown when remaining slack falls below the configured threshold |
| Delivery confirmation | Customer can confirm receipt, which starts the guarantee window |

**Normal case:** order list loads → drawer shows the timeline → status updates arrive live.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| No orders yet | Explicit empty state with a CTA to start an order |
| SignalR disconnected | Falls back to polling/refresh; no stale badge without a timestamp |
| Delayed order | At-risk badge plus a notification; the promised date is not silently changed |

---

## 3.2.5 Raise a Reprint or Complaint

**Function trigger**
`My Orders` → open the Order Details drawer → **"Yêu cầu in lại"** (visible only inside the guarantee window).

**Function description**
- **Actors:** Customer; Operations Manager (reviews); Hub QC (if a new inspection is required).
- **Purpose:** Give the customer a documented post-delivery remedy instead of an ad-hoc support channel.
- **Interface:** `POST /api/v1/orders/{id}/reprint-request` · `GET /api/v1/orders/{id}/reprint-requests`.
- **Data processing:**
  1. Validate the order is delivered and inside the guarantee window (30 days from delivery).
  2. Store reason, description and photo evidence with status `PENDING`.
  3. Operations reviews; approval creates an urgent reprint job that inherits the original deadline
     (FR-HUB-003), or the request is closed with a written reason that may hand off to a refund.
  4. Every state change is visible in the drawer and audited.

**Screen layout**
`[ẢNH 3.2-g]` — the Reprint Request panel with the form open and a submitted request listed.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Window | Requests after 30 days are rejected with an explanation (BR-QC-009) |
| Evidence | At least one photo is expected; description is mandatory |
| Traceability | Request and photos remain attached to the order |
| Outcome | Approved → reprint job; denied → reason recorded, refund path available |
| Reprint limit | The system counts reprints; beyond the limit the case escalates to Operations |

**Normal case:** request submitted → acknowledged → approved → reprint job created and scheduled → customer
sees the new job in the timeline.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Outside the window | Rejected immediately with the policy text |
| Duplicate request for the same order | Blocked; existing request is shown instead |
| Attribution to the customer | Paid reprint or refund offered instead of a free reprint |
| Reprint limit reached | Escalated to Operations for a decision |

---

## 3.2.6 Manage Account, Addresses and Model Library

**Function trigger**
`Account (/account)` for profile and addresses; `Model Library (/models)` for library actions.

**Function description**
- **Actors:** Customer.
- **Purpose:** Self-service maintenance of the data every order depends on.
- **Interface:** `GET/PUT /api/v1/me` · `GET/POST /api/v1/me/addresses` ·
  `PUT/DELETE /api/v1/me/addresses/{addressId}` · `GET /api/v1/models` · `DELETE /api/v1/models/{modelId}`.
- **Data processing:**
  1. Profile update validates the email uniqueness and re-authenticates sensitive changes.
  2. Address create/update enforces required fields; exactly one default is kept.
  3. Delete is blocked when the address is referenced by an open order.
  4. Reorder copies the model and its last configuration into a new quote draft.

**Screen layout**
`[ẢNH 3.2-h]` — `Account & Addresses` with the profile block and at least two addresses (one default).

**Function details**

| Rule | Behaviour |
|------|-----------|
| Password | Minimum 8 characters; change requires the current password |
| Email | Verified address required before ordering |
| Default address | Exactly one; the checkout screen preselects it |
| Address delete | Blocked while an open order references it |
| Reorder | Prefills model + last configuration; the customer still confirms the quote |

**Normal case:** profile edited → address added and defaulted → checkout preselects it.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Email already used | Validation error; no partial save |
| Deleting the only/default address | Allowed only if no open order references it; default is reassigned |
| Reorder with a deprecated material | Configuration wizard opens with the invalid option disabled and a reason |

---

## 3.2.7 Request a Design Service

**Function trigger**
Navigation to `Design Request (/design-request)`; submission of the intake form.

**Function description**
- **Actors:** Customer; Operations Manager (quotes the design stages); designer (out of system).
- **Purpose:** Convert an idea without a 3D file into a paid design deliverable linked to an order.
- **Interface:** `POST /api/v1/design-requests` · `GET /api/v1/design-requests/mine` ·
  `POST /api/v1/design-requests/images` · `POST /api/v1/design-requests/{id}/status` (ops).
- **Data processing:**
  1. Customer submits title, description and reference images.
  2. Request enters the ops queue; operations quotes the design stages using the staged-payment machinery.
  3. On payment, the designer delivers an STL which is attached to the customer's order draft.
  4. The deliverable is owned by the customer; reuse by the platform or other labs is prohibited (BR-IP-004).

**Screen layout**
`[ẢNH 3.2-i]` — `Design Request` with the intake form and a previously submitted request in the list.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Required fields | Title and description; at least one reference image recommended |
| Status set | Submitted → Quoted → In design → Delivered → Converted to order |
| Payment | Reuses the deposit/balance legs of FR-CUST-012 |
| Ownership | Deliverable belongs to the customer; platform reuse prohibited |
| Iteration cap | Rounds beyond the configured cap require a re-quote |

**Normal case:** brief submitted → ops quotes → customer pays → STL delivered → order draft created.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Brief rejected | Refund per the configured policy; reason recorded |
| Iterations exceed the cap | Re-quote required before more work |
| Customer abandons payment | Request stays `Quoted` and expires |

---

# 3.3 Lab Operations

| Mục | Function | FR | UC |
|-----|----------|----|----|
| 3.3.1 | Register Lab, Machines and Inventory | LAB-001/002/003 | UC-014 |
| 3.3.2 | Respond to Job Offers | LAB-004 | UC-004 |
| 3.3.3 | Execute a Print Job | LAB-005 | UC-005 |
| 3.3.4 | Review Timeline and Standing | LAB-006/007 | UC-008 |
| 3.3.5 | Plan a Lab→Hub Shipment Batch | LAB-008 | UC-033 |

## 3.3.1 Register Lab, Machines and Inventory

**Function trigger**
First sign-in of a `LabManager` account → `Lab Registration (/lab/register)`; machines and stock are
maintained from `/lab/machines` and `/lab/:labId/inventory`.

**Function description**
- **Actors:** Lab Manager (primary); System Administrator (approves).
- **Purpose:** Produce a complete, verifiable capability declaration so the assignment filter can trust it.
- **Interface:** `POST /api/v1/labs/self-register` · `POST /api/v1/labs/{labId}/machines` ·
  `PUT/DELETE /api/v1/labs/{labId}/machines/{machineId}` · `GET/PUT /api/v1/labs/{labId}/inventory` ·
  `POST /api/v1/labs/{labId}/inventory/adjustments` · `POST /api/v1/labs/{labId}/approve|reject`.
- **Data processing:**
  1. Capture profile, location, working calendar and hub transfer time.
  2. Declare each machine's technology, build volume, tolerance and supported materials.
  3. Declare stock per material/colour with a reorder point.
  4. Submit → status `PENDING`; the lab is invisible to the assignment filter.
  5. Admin inspects the declarations, then approves (`ACTIVE`) or rejects with a reason.

**Screen layout**
`[ẢNH 3.3-a]` — `Lab Registration` with all three blocks (profile, calendar/transfer, machine list).
`[ẢNH 3.3-b]` — `Machine Registry` with the maintenance dialog open.
`[ẢNH 3.3-c]` — `Inventory` showing stock and the transaction ledger.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Activation gate | Only `ACTIVE` labs enter the capability filter |
| Machine spec | Technology, build volume, tolerance and materials are all required |
| Maintenance window | A machine inside its maintenance window is not offered |
| Delete guard | A machine with active jobs cannot be removed |
| Stock tolerance | Reservations use quantity × grams × (1 + tolerance) (BR-STOCK-002) |
| Movement record | Every in/out/adjustment writes a `stock_transactions` row with a running total |

**Normal case:** register → declare machines and stock → admin approves → lab receives offers.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Incomplete declaration | Submit blocked with the missing field highlighted |
| Admin rejects | Reason is shown in the registration banner; resubmission allowed |
| Spec edited after approval | Audited as declared-vs-delivered; the filter uses the new spec within 1 minute |
| Stock below reorder point | Low-stock alert; offers requiring that material are filtered out |

---

## 3.3.2 Respond to Job Offers

**Function trigger**
A `JobAssigned` event pushes an offer to `LabHub`; the offer appears in `Lab Queue (/lab/queue)` with a
2-hour countdown.

**Function description**
- **Actors:** Lab Manager; Scheduling Engine (system).
- **Purpose:** Accept or decline production work within a bounded window so the plan can be repaired quickly.
- **Interface:** `GET /api/v1/jobs` · `POST /api/v1/jobs/{jobId}/accept` ·
  `POST /api/v1/jobs/{jobId}/decline` · `GET /api/v1/jobs/{jobId}/file`.
- **Data processing:**
  1. Offer is delivered with the job configuration and the available window.
  2. Accepting reserves material and places the job in the machine queue; the acceptance records the file
     hash acknowledgement (BR-IP-003).
  3. Declining requires a reason and immediately triggers the rescheduler.
  4. No response within 2 hours is treated as an auto-decline and also triggers the rescheduler.
  5. First valid acceptance wins when several labs are offered the same job.

**Screen layout**
`[ẢNH 3.3-d]` — `Lab Queue` with an offer showing the countdown and the Accept/Decline actions.
`[ẢNH 3.3-e]` — the Decline dialog with the mandatory reason field.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Response window | 2 hours, visible as a countdown |
| Decline | Reason mandatory; recorded on the job |
| File access | Time-limited download link, only while the job is active |
| Acceptance side effects | Material reservation + machine queue entry + hash acknowledgement |
| Timeout | Auto-decline + rescheduler (configured policy) |

**Normal case:** offer received → reviewed → accepted → job queued → operator starts it.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Timeout | Auto-decline; the plan is repaired and the lab's reliability metric is affected |
| Decline | Reason stored; the job is reassigned |
| File link expired | Re-request from the job screen; link regenerated while the job is active |
| Two labs accept | Only the first valid acceptance is committed; the other receives a withdrawn notice |

---

## 3.3.3 Execute a Print Job

**Function trigger**
An accepted job in `Lab Queue` → open job → **"Bắt đầu"**; then **"Hoàn thành"** or **"Báo lỗi"**.

**Function description**
- **Actors:** Lab Operator (primary); Order Staff via QC-proof review (secondary); rescheduler (system).
- **Purpose:** Drive the job through production while capturing the actuals needed for calibration and the
  evidence needed for hand-over.
- **Interface:** `POST /api/v1/jobs/{jobId}/start` · `POST /api/v1/jobs/{jobId}/complete` ·
  `POST /api/v1/jobs/{jobId}/fail` · `POST /api/v1/jobs/{jobId}/inspection-photos`.
- **Data processing:**
  1. Start records the start timestamp and moves the job to `IN_PROGRESS`.
  2. Complete requires actual print minutes and actual material grams; both feed calibration.
  3. Completion requires the self-QC report and at least one photo; the job enters `AWAITING_QC_PROOF`.
  4. Operations approves the proof, which unlocks hand-over.
  5. A failure records the incident with photos and triggers the rescheduler.

**Screen layout**
`[ẢNH 3.3-f]` — the Complete Job dialog with actual duration, material and the proof upload.
`[ẢNH 3.3-g]` — the Report Incident dialog with photo evidence.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Actuals | Duration and material are mandatory on completion (BR-ESTIM-001) |
| Self-QC proof | Photo + quality report required; hand-over blocked until approved (BR-QC-011/012) |
| Stock settlement | Reserved grams are settled against actual grams; the difference is written as a transaction |
| Incident | Type, description and photos; failure enters the rescheduling path |
| Immutability | Completed actuals are not editable; corrections go through an incident |

**Normal case:** start → print → complete with actuals and proof → proof approved → ready for hand-over.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Print failure | Incident recorded; rescheduler repairs the plan |
| Proof rejected | Job returns to the lab with the rejection reason |
| Proof rejected twice | Hub inspection is forced regardless (QC decides) |
| Machine breakdown | Machine marked offline; the whole queue is rescheduled |

---

## 3.3.4 Review Timeline and Standing

**Function trigger**
Navigation to `Timeline & Score (/lab/timeline)` or `/lab/score` (same screen).

**Function description**
- **Actors:** Lab Manager, Lab Operator (read-only for operators).
- **Purpose:** Show the lab's own schedule and performance without exposing other labs.
- **Interface:** `GET /api/v1/labs/me` · `GET /api/v1/jobs` (queue view).
- **Data processing:**
  1. Build a Gantt per machine from planned start/end and queue order.
  2. Aggregate the last 90 days: completed jobs, on-time rate, first-pass yield, score.
  3. Compare against the anonymised network average; never show another lab's identity.

**Screen layout**
`[ẢNH 3.3-h]` — `Timeline & Score`: Gantt on top, standing metrics below.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Refresh | Timeline reflects a reschedule after the next refresh/push |
| Minimum sample | Below 10 completed jobs the score shows as preliminary |
| Anonymity | Comparison is aggregate only |
| Explainability | Each metric links to the underlying jobs |

**Normal case:** lab sees its queue, upcoming due dates and current standing.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| No lab profile | Screen explains registration is required |
| Insufficient history | Provisional score with an explicit note |

---

## 3.3.5 Plan a Lab→Hub Shipment Batch

**Function trigger**
Navigation to `Shipment Batch Planner (/lab/:labId/shipments)`.

**Function description**
- **Actors:** Lab Manager / Lab Operator.
- **Purpose:** Replace per-parcel hand-over with scheduled runs that the hub can reconcile.
- **Interface:** `GET /api/v1/lab/shipment-batches/planner` ·
  `POST /api/v1/lab/shipment-batches/preview` · `POST /api/v1/lab/shipment-batches/seal`.
- **Data processing:**
  1. List jobs that are completed **and** whose QC proof is approved.
  2. Preview the batch: total weight, weight class, zone allowance and the declared vehicle calendar.
  3. Seal creates the batch with a waybill; jobs move to `IN_TRANSIT`.
  4. The hub receives the batch and reconciles it (3.4.1).

**Screen layout**
`[ẢNH 3.3-i]` — the planner with eligible jobs selected, weight and allowance preview visible.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Eligibility | Completed + proof approved only |
| Weight | Total weight drives the weight class and multiplier |
| Allowance | Zone rate and allowance previewed before sealing |
| Calendar | Batch date must match the declared transfer calendar |
| Urgent exception | A single urgent job may be released early with a logged reason |

**Normal case:** eligible jobs grouped → preview → seal → waybill → in transit → hub receives.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| No eligible jobs | Empty state explaining the proof-approval requirement |
| Calendar conflict | Next available run proposed |
| Job removed before seal | Batch preview recalculates; sealing is blocked until resolved |

---

# 3.4 Hub Quality & Fulfilment

| Mục | Function | FR | UC |
|-----|----------|----|----|
| 3.4.1 | Receive and Reconcile a Batch | HUB-001 | UC-016 |
| 3.4.2 | Run an Inspection | HUB-002 | UC-006 |
| 3.4.3 | Trigger a Reprint | HUB-003 | UC-013 |
| 3.4.4 | Consolidate, Pack and Ship | HUB-004/005/006 | UC-017 |

## 3.4.1 Receive and Reconcile a Batch

**Function trigger**
A sealed batch arrives; the hub opens `Hub Shipments (/hub/shipments)` and selects the expected batch.

**Function description**
- **Actors:** Hub QC Staff / Hub Fulfilment Staff.
- **Purpose:** Establish exactly what physically arrived before quality inspection starts.
- **Interface:** `GET /api/v1/hub/shipment-batches` · `POST /api/v1/hub/shipment-batches/{batchId}/receive`.
- **Data processing:**
  1. Load the expected job list for the batch.
  2. Mark each job RECEIVED, MISSING or DAMAGED.
  3. Missing or damaged items create an incident and notify the lab and Operations the same day.
  4. Closing the receipt moves received jobs into the inspection queue.

**Screen layout**
`[ẢNH 3.4-a]` — `Hub Shipments` with the reconciliation panel and one item flagged MISSING.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Expected list | Generated from the sealed batch, not typed manually |
| Discrepancy | Creates an incident with photos; notifications sent |
| Partial receipt | Allowed; the batch stays open for the remainder |
| Status transition | Received jobs become `AWAITING_INSPECTION` |

**Normal case:** scan batch → all received → close → inspection queue populated.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Missing item | Incident + notification; the order cannot pass the pack gate |
| Damaged in transit | Photos + attribution; rescheduler may issue a reprint |
| Wrong batch scanned | Rejected; the expected batch is shown |

---

## 3.4.2 Run an Inspection

**Function trigger**
A received job appears in `QC Console (/hub/qc)`; the inspector opens the inspection dialog.

**Function description**
- **Actors:** Hub QC Staff.
- **Purpose:** Apply the grade-bound checklist and record evidence so pass/fail is defensible.
- **Interface:** `POST /api/v1/jobs/{jobId}/inspect` · `GET /qc-knowledge` (checklist read).
- **Data processing:**
  1. Load the inspection checklist version bound to the order's quality grade.
  2. Record each criterion's result against its pass condition.
  3. On fail, require a defect code, fault attribution (Lab / Hub / Customer) and photos.
  4. Persist the result with the pinned checklist version.
  5. A pass makes the item eligible for packing; a fail enters the reprint path.

**Screen layout**
`[ẢNH 3.4-b]` — `QC Console`.
`[ẢNH 3.4-c]` — the Inspection Record dialog with checklist items, result, defect and photos.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Checklist binding | Version pinned at inspection start; later edits do not change a running inspection |
| Mandatory items | Items flagged mandatory must be evaluated |
| Fail evidence | Defect code + attribution + at least one photo |
| Attribution | Determines whether a reprint is free (Lab/Hub) or paid (Customer) |
| Immutability | Results cannot be edited; a re-inspection creates a new record |

**Normal case:** checklist completed → PASS → item eligible for consolidation.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Fail with lab fault | Urgent reprint created (3.4.3) |
| Fail with customer fault | Paid reprint or refund offered |
| No checklist for the grade | Inspection blocked with a configuration error |
| Disputed result | A second inspection by a different inspector is recorded |

---

## 3.4.3 Trigger a Reprint

**Function trigger**
Automatic on an inspection FAIL whose attribution is not the customer; also available to Operations for a
manual decision.

**Function description**
- **Actors:** Scheduling Engine (primary); Hub QC Staff; Operations Manager.
- **Purpose:** Produce a replacement part without losing the customer's deadline.
- **Interface:** `POST /api/v1/scheduling/jobs/{jobId}/reprint` · escalation via `/api/v1/ops/escalations`.
- **Data processing:**
  1. Copy the original job's configuration and quantity.
  2. Inherit the original delivery deadline; mark the job URGENT.
  3. Set the cost bearer from the attribution.
  4. Increment the reprint index; beyond the limit (2) escalate to Operations.
  5. The original job is marked `REPRINTING`; the new job goes through assignment (3.5.4).

**Screen layout**
No dedicated screen — output is visible in `QC Console (/hub/qc)` (job marked reprinting) and
`Escalation Queue (/ops/escalations)` when the limit is exceeded. Capture both.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Deadline | Inherited, never extended silently (BR-QC-006) |
| Cost | Borne by the responsible party (lab, hub or customer) |
| Limit | 2 reprints; the third case escalates |
| Priority | URGENT — bypasses normal queue ordering |
| Traceability | Reprint chain is visible from the original job |

**Normal case:** fail → reprint created → assigned → printed → inspected → shipped within the original promise.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| No feasible lab for the deadline | Escalated; the customer may be offered a date change |
| Reprint limit reached | Escalation queue; Operations decides refund or exception |
| Customer fault | Paid reprint or refund instead of a free one |

---

## 3.4.4 Consolidate, Pack and Ship

**Function trigger**
All items of an order pass inspection → `Complete Orders (/hub/complete-orders)`.

**Function description**
- **Actors:** Hub Fulfilment Staff; Customer (receives tracking).
- **Purpose:** Close the physical fulfilment loop and start the guarantee clock.
- **Interface:** `GET /api/v1/hub/orders/shipment-queue` · `POST /api/v1/hub/orders/{orderId}/ship` ·
  `POST /api/v1/hub/orders/ship-consolidated` · `POST /api/v1/hub/orders/{orderId}/deliver` ·
  `POST /api/v1/hub/orders/deliver-consolidated` · `POST /api/v1/hub/orders/{orderId}/refund`.
- **Data processing:**
  1. Verify the pack gate: every item PASSED_QC and the balance settled.
  2. Build the packing list; record the manual waybill.
  3. Optionally merge eligible orders of the same customer and address into one shipment.
  4. Mark SHIPPED and notify the customer.
  5. On delivery, record `DeliveredAt`, which opens the 30-day guarantee window.

**Screen layout**
`[ẢNH 3.4-d]` — `Complete Orders` with the pack gate status and outstanding items.
`[ẢNH 3.4-e]` — the Record Waybill dialog and the Merge Shipments dialog.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Pack gate | Blocked unless all items are PASSED_QC and the balance is settled |
| Waybill | Manual entry; no carrier API in this release |
| Merge eligibility | Same customer + address, all QC-passed, fully settled, customer has not opted out |
| Guarantee | 30 days computed from `DeliveredAt` (BR-QC-009) |
| Notification | SHIPPED and DELIVERED both notify the customer |
| Per-item refund | A merged shipment still refunds per original order/item |

**Normal case:** gate passes → packing list → waybill → SHIPPED → DELIVERED → guarantee clock starts.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| An item is still in reprint | Gate blocked with the outstanding report |
| Balance unpaid | Gate blocked; escalation to Operations |
| Merge not eligible | Button disabled with the reason |
| Delivery confirmation missing | Order stays SHIPPED; the guarantee window has not started |

---

# 3.5 Pricing, Assignment & Scheduling Engine ★

Core contribution. Every function in this section is non-screen; results surface on customer, lab and
operations screens listed under *Screen layout*.

| Mục | Function | FR | UC |
|-----|----------|----|----|
| 3.5.1 | Analyze Geometry and Estimate | SCHED-001/002 | UC-002 |
| 3.5.2 | Compute Quote and Freeze Pricing | SCHED-010 | UC-010 |
| 3.5.3 | Speculative Placement | SCHED-005 | UC-010 |
| 3.5.4 | Assign Job to Lab | SCHED-003/004/006/009 | UC-011 |
| 3.5.5 | Reschedule on Events | SCHED-007 | UC-012 |
| 3.5.6 | Calibrate Estimates | SCHED-008 | UC-018 |

## 3.5.1 Analyze Geometry and Estimate

**Function trigger**
Enqueued automatically after a successful upload.

**Function description**
- **Actors:** Engine (system); Customer (sees the result).
- **Purpose:** Produce the geometry facts and a time/material estimate that every quote and feasibility
  check depends on.
- **Interface:** `AnalyzeModelJob` (Hangfire) · `GET /api/v1/models/{modelId}` (result read).
- **Data processing:**
  1. Parse the mesh and compute bounding box, volume, watertightness and manifold status.
  2. Reject or flag files that cannot be printed.
  3. Validate the bounding box against the maximum build volume of the network.
  4. Produce a reference print time and material quantity per machine profile.
  5. Store the result with the model; the UI reads `GeometryStatus` and `IsPrintable`.

**Screen layout**
No dedicated screen — result is visible in `Model Preview (/models/:modelId/preview)`: bbox X×Y×Z,
volume, estimated print time and the printability badge. Capture that screen.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Accuracy | Bounding box to 0.1 mm |
| Latency | Target < 10 s for a typical model |
| Infeasible | Model flagged; configuration disabled |
| Reproducibility | Raw estimate values stored with the model |
| Failure | Status remains `PENDING`; retry is available and logged |

**Normal case:** upload → analysis → `ANALYSED` with bbox/volume/time.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Corrupted file | `INFEASIBLE` with reason |
| Model exceeds all machine volumes | `INFEASIBLE` with an oversize message |
| Non-watertight mesh | Flagged; the customer is warned before configuring |
| Slicer unavailable | Job retried; quote is refused rather than estimated blindly |

---

## 3.5.2 Compute Quote and Freeze Pricing

**Function trigger**
`POST /api/v1/quotes` from the configuration screen.

**Function description**
- **Actors:** Pricing Engine (system); Customer.
- **Purpose:** Produce a reproducible price whose inputs are frozen at the moment of quoting.
- **Interface:** `POST /api/v1/quotes` · `GET /api/v1/quotes/{quoteId}`.
- **Data processing:**
  1. Read the active commercial parameter version.
  2. Compute material cost, machine time, post-processing, and shipping as a separate line.
  3. Sum to the total; store the breakdown per item.
  4. Freeze the parameter version label on the quote (`PricingVersion`).
  5. Return a draft quote (pending staff approval unless the auto-lane applies).

**Screen layout**
No dedicated screen — the result is the quote panel on `Order Configuration (/models/:modelId/order)`.
Capture the panel with the breakdown expanded.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Single formula | One formula for every quote; no hand-typed prices (BR-QUOTE-001) |
| Frozen version | Re-running with the same version yields the same price (BR-QUOTE-004) |
| Breakdown sums to total | Enforced; shipping is an explicit line (BR-LOG-004) |
| Parameter change | Affects new quotes only; published quotes keep their version |
| Validation | Invalid configuration is rejected before pricing |

**Normal case:** configuration → price with breakdown and committed date → draft quote.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| No active parameter set | Quote refused with a configuration error |
| Infeasible model | No pricing; validation message returned |
| Breakdown mismatch | Quote is rejected by a consistency check |

---

## 3.5.3 Speculative Placement

**Function trigger**
Runs as part of the quote request, immediately after pricing.

**Function description**
- **Actors:** Scheduling Engine (system).
- **Purpose:** Derive a delivery date that reflects real network load instead of a fixed lead-time table.
- **Interface:** internal call from `POST /api/v1/quotes`.
- **Data processing:**
  1. Snapshot the current network state (queues, load, lab calendars).
  2. Run the same filter/score logic as real assignment in a trial sandbox.
  3. Place the job tentatively and compute backward due dates.
  4. Return the earliest feasible date and any surcharge.
  5. Nothing is committed to the real schedule.

**Screen layout**
No dedicated screen — the date appears on the quote panel and in the quote record. Capture
`Order Configuration` showing "NGÀY GIAO CAM KẾT".

**Function details**

| Rule | Behaviour |
|------|-----------|
| Isolation | Trial placement never mutates real queues |
| Latency | ≤ 60 s target |
| Load sensitivity | The date moves when network load changes |
| No feasible lab | Quote refused; earliest possible date offered |
| Determinism | Same inputs + same version ⇒ same date |

**Normal case:** trial placement returns a credible date and the quote is published with it.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Network saturated | Earliest date offered; surcharge applied per policy |
| Required-by date not achievable | Customer offered the earliest feasible date |
| Timeout | Quote fails safely; no partial placement is committed |

---

## 3.5.4 Assign Job to Lab

**Function trigger**
Order reaches `PAID`; automatic call to `POST /api/v1/scheduling/jobs/{jobId}/assign`. Can be re-entered by
the rescheduler or manually by Operations.

**Function description**
- **Actors:** Scheduling Engine (primary); Lab Manager (receives the offer); Operations Manager (escalation).
- **Purpose:** Choose the best feasible (lab, machine) pair and make the choice fully reconstructible.
- **Interface:** `POST /api/v1/scheduling/jobs/{jobId}/assign` ·
  `GET /api/v1/ops/decisions/{jobId}` (decision log) · SignalR `LabHub` (offer).
- **Data processing:**
  1. Load the job, its configuration and the candidate set.
  2. Apply the hard feasibility filter.
  3. Score each feasible pair with the active weight version.
  4. Select the highest score; deterministic tie-break.
  5. Decompose the item into jobs, compute backward internal due dates, place on the machine queue.
  6. Persist the decision log **before** publishing; a failed log write rolls the assignment back.
  7. Publish `JobAssigned` and start the 2-hour offer window.

**Screen layout**
No dedicated screen. Capture:
`[ẢNH 3.5-a]` — `Scheduling Board (/scheduling)` showing the assigned job on a machine queue.
`[ẢNH 3.5-b]` — `Decision Trace (/ops/decisions)` showing candidates and per-criterion scores.

**Function details**

*Hard constraints (any failure ⇒ not a candidate)*

| Constraint | Source |
|------------|--------|
| Technology match | Machine capability |
| Build volume fit (including orientation margin) | Machine + analysis |
| Achievable tolerance ≥ required | Machine |
| Material and colour stock ≥ reserved | MaterialStock + reservation |
| Lab status = ACTIVE | Lab |
| Standing above the configured floor | Standing metric |

*Scoring criteria*

| Criterion | Weight | Meaning |
|-----------|--------|---------|
| Due-date slack | 0.35 | Buffer remaining if this lab takes the job |
| Lab reliability | 0.25 | On-time and first-pass history |
| Load balance | 0.20 | Avoids overloading one lab |
| Cost | 0.10 | Production cost to the platform |
| Transit | 0.10 | Lab→hub transfer time |

Weights are a versioned configuration and must total exactly 1.0.

**Normal case:** paid order → filter → score → best pair → placement → decision logged → lab offered → accepted.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| No feasible lab | Escalation created; the customer promise is not changed silently |
| Decline / 2-hour timeout | Rescheduler repairs the plan |
| Two jobs race for the last capacity | Placement is serialised per machine; zero double-booking expected |
| Decision log write fails | Assignment rolled back; no unlogged assignment exists |

---

## 3.5.5 Reschedule on Events

**Function trigger**
Domain events: lab decline, 2-hour timeout, print failure, machine breakdown, reprint, in-transit damage.
Also callable for risk repair via `POST /api/v1/scheduling/jobs/{jobId}/repair-risk`.

**Function description**
- **Actors:** Scheduling Engine / scheduler (system); Operations Manager (when a customer date must change).
- **Purpose:** Repair the unstarted portion of the plan while keeping committed promises intact.
- **Interface:** `POST /api/v1/scheduling/jobs/{jobId}/repair-risk` · automatic on events.
- **Data processing:**
  1. Identify affected jobs.
  2. Lock `IN_PROGRESS` jobs — they are never moved.
  3. Try repair in the preference order: split quantity → reassign → propose a date change.
  4. Keep a committed fallback; commit the improved plan only if it beats the fallback.
  5. If the customer's date must change, open a `DateChangeRequest` and notify.
  6. Log the repair and publish updates.

**Screen layout**
No dedicated screen. Capture:
`[ẢNH 3.5-c]` — `Scheduling Board` after a repair (job moved to a new machine).
`[ẢNH 3.5-d]` — `Date Change Decision (/date-change/:token)` for the customer-facing branch.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Time budget | ≤ 30 s hard limit |
| In-progress jobs | Never rescheduled |
| Preference order | Split quantity → reassign → date change (BR-SCHED-010/011) |
| Fallback | Always committed if improvement fails |
| Customer date | Never changed silently; consent token required |

**Normal case:** decline → plan repaired within budget → job reassigned → promises unchanged.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| No repair possible | Date change proposed to the customer |
| Budget exceeded | Fallback committed; breach counted as an NFR metric |
| Customer rejects the date change | Escalated to Operations; refund path available |
| Machine breakdown | Whole machine queue rescheduled |

---

## 3.5.6 Calibrate Estimates

**Function trigger**
Nightly Hangfire job at 02:17 (`17 2 * * *`); also triggerable via
`POST /api/v1/calibration/samples/collect` and `POST /api/v1/calibration/recalibrate`.

**Function description**
- **Actors:** Scheduler (system); System Administrator (reviews).
- **Purpose:** Close the gap between estimated and actual print time so quotes and dates stay credible.
- **Interface:** `POST /api/v1/calibration/samples/collect` · `POST /api/v1/calibration/recalibrate` ·
  `POST /api/v1/calibration/recalibrate/{machineId}` · `GET /api/v1/calibration/status`.
- **Data processing:**
  1. Collect (estimated, actual) pairs from completed jobs.
  2. Enforce the minimum sample gate per machine model.
  3. Fit a correction factor and compute MAPE.
  4. Store the factor and its version on the machine.
  5. Future quotes and placements consume the new factor; historical quotes are untouched.

**Screen layout**
No dedicated screen. Capture `Admin → Calibration status` if exposed, otherwise the `Background Job
Dashboard (/jobs)` showing the recurring schedule.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Minimum samples | Below the gate the previous factor is kept |
| Traceability | Factor version, computed time and MAPE stored |
| Scope | Per machine model, not per individual machine only |
| Historical safety | Quotes keep their frozen pricing version |
| Manual override | Allowed with a reason and audit entry |

**Normal case:** nightly collection → factor updated → tomorrow's quotes closer to reality.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Insufficient samples | No change; status explains why |
| Outlier actuals | Excluded by the fit; MAPE reported |
| Job fails | Retried; failure recorded in the job dashboard |

---

# 3.6 Operations & Analytics

| Mục | Function | FR | UC |
|-----|----------|----|----|
| 3.6.1 | Approve Quote Draft | ANAL-006 | UC-029 |
| 3.6.2 | Approve Lab QC Proof | ANAL-007 | UC-030 |
| 3.6.3 | Monitor Network and At-Risk Orders | ANAL-001/002 | UC-019 |
| 3.6.4 | Override Assignment, Priority and Standing | ANAL-004 | UC-020 |
| 3.6.5 | SLA Reporting and Export | ANAL-003 | UC-021 |
| 3.6.6 | Refund and Reconciliation | ANAL-005 | UC-028 |
| 3.6.7 | Run Network Simulation | — | UC-027 |

## 3.6.1 Approve Quote Draft

**Function trigger**
`Quote Review (/quote-reviews)`; the queue is populated by engine drafts.

**Function description**
- **Actors:** Operations Manager (in the current build this policy is `RequireQuoteReview`, shared with Admin).
- **Purpose:** Keep a human in the pricing loop while preserving machine-speed drafting.
- **Interface:** `GET /api/v1/quote-reviews` · `POST /api/v1/quote-reviews/{quoteId}/approve`.
- **Data processing:**
  1. Load the draft with the price breakdown and the scheduling basis.
  2. Staff may adjust within the configured band; a reason is mandatory.
  3. Approving publishes the quote; the expiry clock starts at this moment.
  4. Rejecting returns the draft for re-generation.
  5. Adjustments are logged before/after with the reason.

**Screen layout**
`[ẢNH 3.6-a]` — `Quote Review` with a draft selected, breakdown and scheduling basis visible.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Auto-lane | Drafts within the configured deviation threshold skip the queue |
| Band | Edits outside the band escalate to Administration |
| Reason | Mandatory for any adjustment |
| Expiry | Starts at approval, not at generation |
| SLA | Queue-age target configured (default 2 h); breach is flagged |

**Normal case:** draft → review → approve → customer sees the quote and its expiry.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Adjustment outside band | Blocked; escalation to pricing admin |
| Queue SLA breached | Flagged and notified to Operations |
| Draft rejected | Re-draft loop; the customer still sees nothing |
| Quote expired while queued | Published as expired; a fresh quote is required |

---

## 3.6.2 Approve Lab QC Proof

**Function trigger**
`QC Proof Review (/staff/qc-proofs)` — populated when a lab completes a job with proof.

**Function description**
- **Actors:** Operations Manager (policy shared with Admin).
- **Purpose:** Stop defective batches before they travel to the hub.
- **Interface:** `GET /api/v1/jobs/qc-proofs` · `POST /api/v1/jobs/{jobId}/qc-proof/review`.
- **Data processing:**
  1. Open the proof: photos plus the lab's quality report.
  2. Compare against the grade criteria.
  3. Approve unlocks hand-over; reject returns the job with a reason.
  4. A second rejection forces hub inspection regardless.

**Screen layout**
`[ẢNH 3.6-b]` — `QC Proof Review` with the proof detail dialog (photos + report).

**Function details**

| Rule | Behaviour |
|------|-----------|
| Hand-over gate | Batch sealing is blocked until the proof is approved |
| Rejection | Reason mandatory; job returns to the lab |
| Repeat rejection | Forces hub inspection |
| Audit | Every approval/rejection is recorded with the reviewer |
| Dispute | Hub re-inspection wins over the lab's own report |

**Normal case:** proof approved → job becomes eligible for the batch planner.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Missing photos | Review blocked; the lab must re-upload |
| Rejected twice | Forced hub inspection |
| Dispute | Hub QC result is authoritative |

---

## 3.6.3 Monitor Network and At-Risk Orders

**Function trigger**
`Scheduling Board (/scheduling)`; refresh is live.

**Function description**
- **Actors:** Operations Manager.
- **Purpose:** Surface the orders that will miss their promise before they do.
- **Interface:** `GET /api/v1/ops/monitor` · `GET /api/v1/ops/escalations`.
- **Data processing:**
  1. Compute slack per open order (promised date − projected completion).
  2. Flag orders below the configured threshold as at-risk.
  3. Aggregate lab load, standing and queue depth.
  4. Support drill-down to a single order/job.

**Screen layout**
`[ẢNH 3.6-c]` — `Scheduling Board` with the at-risk panel and lab load columns.

**Function details**

| Rule | Behaviour |
|------|-----------|
| At-risk definition | Slack below the configured threshold (default 4 h) |
| Refresh | ≤ 30 s |
| Drill-down | To job level, including decision trace |
| Action | Acknowledge or intervene (3.6.4) |

**Normal case:** board shows healthy load; at-risk list empty.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Threshold breached | At-risk badge + notification |
| Lab overloaded | Load column highlights; assignment scoring already penalises it |
| Stale data | Last-refresh timestamp shown; never presented as live |

---

## 3.6.4 Override Assignment, Priority and Standing

**Function trigger**
`Scheduling Board` → select a job → **"Can thiệp"**; lab actions from `Ops Labs (/ops/labs)`.

**Function description**
- **Actors:** Operations Manager.
- **Purpose:** Let a human correct the engine without breaking feasibility or the audit trail.
- **Interface:** `GET /api/v1/ops/jobs/{jobId}/reassign-preview` ·
  `POST /api/v1/ops/jobs/{jobId}/reassign` · `PATCH /api/v1/labs/{labId}/standing` ·
  `PATCH /api/v1/labs/{labId}/capacity-limit` · `PATCH /api/v1/labs/{labId}/active`.
- **Data processing:**
  1. Step 1 previews the reassignment through the capability filter.
  2. If the customer's promised date would change, step 2 requires an additional reason.
  3. Confirm writes the decision, triggers a repair if needed, and notifies affected parties.
  4. Suspension and capacity caps immediately change the candidate set.

**Screen layout**
`[ẢNH 3.6-d]` — the two-step Reassign & Impact Preview dialog.
`[ẢNH 3.6-e]` — `Ops Labs` with the capacity-cap dialog.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Hard filter | An override cannot bypass feasibility constraints |
| Impact preview | Mandatory before confirming |
| Customer date change | Two-step approval + notification |
| Reason | Mandatory for every override |
| Audit | Before/after recorded in the audit log |

**Normal case:** preview shows a feasible move with no date change → confirm → job reassigned.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| No feasible target | Preview explains which constraint failed |
| Date change required | Step 2 requires a reason and opens a customer decision request |
| Override would overload the lab | Warned in the preview; capacity cap can be set first |
| Lab suspended mid-flight | In-progress jobs are not cancelled; new offers stop |

---

## 3.6.5 SLA Reporting and Export

**Function trigger**
`SLA Report (/ops/sla)`.

**Function description**
- **Actors:** Operations Manager.
- **Purpose:** Produce period reports for network performance and evaluation.
- **Interface:** `GET /api/v1/sla/report` · `GET /api/v1/sla/report/export`.
- **Data processing:**
  1. Select period and scope (network, lab, material).
  2. Aggregate on-time rate, tardiness, acceptance, capacity and completion.
  3. Render with filters; export CSV.
  4. The same pipeline consumes simulation output for the evaluation report.

**Screen layout**
`[ẢNH 3.6-f]` — `SLA Report` with a populated period and the export button.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Empty period | Explicit "no data", never zeros |
| Scope filters | Network / lab / material |
| Export | CSV (and PDF where enabled) |
| Reproducibility | Same period + same data = same figures |
| Audit | Exports are logged |

**Normal case:** period selected → metrics rendered → CSV exported.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| No data | "No data" state, export disabled |
| Partial period | Marked as partial in the header |
| Export failure | Error surfaced; no empty file produced |

---

## 3.6.6 Refund and Reconciliation

**Function trigger**
`Refunds (/ops/refunds)`; triggered by a cancelled order, a denied reprint or a customer dispute.

**Function description**
- **Actors:** Operations Manager; Payment Gateway.
- **Purpose:** Return money correctly, exactly once, and reconcile it daily.
- **Interface:** `GET /api/v1/refunds` · `POST /api/v1/refunds/{id}/process` ·
  `GET /api/v1/refunds/export` · `GET /api/v1/orders/pending-payment` ·
  `POST /api/v1/orders/{orderId}/mark-paid` (bridge).
- **Data processing:**
  1. Queue the refund with scope (full or per item) and reason.
  2. Approve; the gateway call is idempotent by transaction id.
  3. Confirm via webhook; update order amounts and notify the customer.
  4. Daily T-1 reconciliation compares settlement to recorded state; a mismatch freezes the transaction.

**Screen layout**
`[ẢNH 3.6-g]` — `Refunds` with the Process Refund dialog.
`[ẢNH 3.6-h]` — `Payments` showing pending and settled legs.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Idempotency | One refund per transaction id |
| Scope | Full order or per item |
| SLA | Refund executed within 5 business days (BR-PAY-006) |
| Reconciliation | Mismatch auto-freezes and alerts |
| Audit | Reason, approver and transaction state recorded |

**Normal case:** refund approved → gateway success → order amounts updated → customer notified →
reconciliation clean.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Gateway error | Safe retry; no silent commit |
| Duplicate processing | Idempotent no-op |
| Reconciliation mismatch | Transaction frozen; Operations investigates |
| SLA breach | Escalated |

---

## 3.6.7 Run Network Simulation

**Function trigger**
Manual run by an authorised evaluator (research role); not exposed in the customer UI.

**Function description**
- **Actors:** Evaluator (Admin granted simulation access).
- **Purpose:** Produce reproducible evidence for the algorithm evaluation report.
- **Interface:** simulation harness (specified; drives the real engines in simulation mode).
- **Data processing:**
  1. Configure a synthetic network (labs, machines, declared-vs-delivered bias).
  2. Generate an order stream (rate, size mix, grade mix, deadline tightness).
  3. Inject faults (decline rate, print failure distribution, breakdown + MTTR, inspection failure).
  4. Run strategies against the same seed with a virtual clock.
  5. Export metrics to the SLA/report pipeline.

**Screen layout**
No dedicated screen — outputs surface in `SLA Report (/ops/sla)`. Capture the exported metric table.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Reproducibility | Fixed seed; a non-reproducible run is quarantined |
| Isolation | Simulation never touches production data |
| Metrics | Tardiness, on-time %, Gini, utilisation |
| Stress scenario | Saturation collapse retained as a stress case |

**Normal case:** seed chosen → run → metrics exported → report generated.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Seed mismatch | Run discarded from evidence |
| Time budget exceeded | Counted as an NFR metric rather than hidden |

---

# 3.7 Platform Administration

| Mục | Function | FR | UC |
|-----|----------|----|----|
| 3.7.1 | Manage Users, Roles and Permissions | ADMIN-001 | UC-022 |
| 3.7.2 | Manage Shared Catalogues | ADMIN-003 | UC-023 |
| 3.7.3 | Configure Commercial Parameters | ADMIN-002 | UC-009 |
| 3.7.4 | Configure Scoring Weights | ADMIN-002 | UC-009 |
| 3.7.5 | Manage QC Checklists and Defect Taxonomy | ADMIN-005 | UC-024 |
| 3.7.6 | Approve Lab Registration | LAB-001 | UC-014 |
| 3.7.7 | Review Audit Log and Job Health | ADMIN-004 | UC-025 |

## 3.7.1 Manage Users, Roles and Permissions

**Function trigger**
`Users & Roles (/admin/users)`.

**Function description**
- **Actors:** System Administrator.
- **Purpose:** Keep the account/role set correct so every portal boundary holds.
- **Interface:** `GET/POST /api/v1/users` · `PUT /api/v1/users/{id}/role` ·
  `PUT /api/v1/users/{id}/deactivate` · `PUT /api/v1/users/{id}/activate` · `GET /api/v1/users/roles`.
- **Data processing:**
  1. Create an account with a role; the role determines the landing portal.
  2. Role change revokes existing sessions.
  3. Deactivation blocks sign-in without deleting history.
  4. Self-lockout (removing your own last admin role) is rejected.

**Screen layout**
`[ẢNH 3.7-a]` — `Users & Roles` with the Create User dialog open.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Roles | Exactly the seven roles in `Roles.cs` |
| Self-lockout | Blocked |
| Session revocation | On role change and deactivation |
| Audit | Every mutation recorded |
| Historical integrity | Deactivation preserves references in history |

**Normal case:** account created with the right role → user lands in the matching portal.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Duplicate email | Validation error |
| Self-lockout attempt | Rejected with an explanation |
| Role without a landing screen | Impossible — every role maps to a portal |

---

## 3.7.2 Manage Shared Catalogues

**Function trigger**
`Shared Catalog (/admin/catalogs)` — five tabs.

**Function description**
- **Actors:** System Administrator.
- **Purpose:** Maintain the reference data used by pricing, the capability filter and QC.
- **Interface:** `GET /api/v1/admin/catalog-items` · `PUT /api/v1/admin/catalog-items/{type}/{code}` ·
  `DELETE /api/v1/admin/catalog-items/{type}/{code}`.
- **Data processing:**
  1. Select a tab (Material, Colour, Technology, Quality Grade, Post-processing).
  2. Create or edit an entry; editing an in-use entry creates a revision.
  3. In-use entries can only be deprecated, never deleted.
  4. Changes propagate to pricing/filter/QC within the propagation SLA.

**Screen layout**
`[ẢNH 3.7-b]` — `Shared Catalog` with the Materials tab and the add/edit dialog.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Delete | Blocked when referenced; use deprecation |
| Revision | Every edit creates a revision |
| Propagation | New quotes use the new value within 1 minute |
| Uniqueness | `CatalogType + Code` is unique |

**Normal case:** item added → appears in configuration → used by new quotes.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Duplicate code | Validation error |
| Delete in use | Blocked with the referencing count |
| Deprecate in use | Allowed; only new quotes are affected |

---

## 3.7.3 Configure Commercial Parameters

**Function trigger**
`Commercial Config (/admin/commercial-configurations)`.

**Function description**
- **Actors:** System Administrator.
- **Purpose:** Change pricing inputs, deposit percentage, shipping thresholds and review bands without a
  redeploy, while keeping history.
- **Interface:** `GET /api/v1/admin/commercial-configurations` ·
  `POST /api/v1/admin/commercial-configurations` ·
  `POST /api/v1/admin/commercial-configurations/{id}/activate`.
- **Data processing:**
  1. Create a new version (draft) with a name and parameter set.
  2. Validate the parameter set; invalid drafts cannot be activated.
  3. Activate with an effective date; the previous version stays readable.
  4. New quotes use the active version; existing quotes keep theirs.

**Screen layout**
`[ẢNH 3.7-c]` — `Commercial Configuration` with the versions list and the New Version dialog.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Immutability | Activated versions are not edited; create a new one |
| Effective date | Supports scheduling a future activation |
| Reproducibility | Quotes freeze the version label |
| Audit | Every creation/activation recorded |

**Normal case:** new version drafted → validated → activated → next quote uses it.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Invalid parameters | Activation blocked |
| Two activations at once | Only one version is active; the conflict is rejected |
| Rollback | Activate a previous version; history remains |

---

## 3.7.4 Configure Scoring Weights

**Function trigger**
`Scoring Config (/admin/scoring-configurations)`.

**Function description**
- **Actors:** System Administrator.
- **Purpose:** Tune assignment priorities (slack, reliability, load, cost, transit) without redeploying.
- **Interface:** `GET /api/v1/admin/scoring-configurations` · `POST /api/v1/admin/scoring-configurations`.
- **Data processing:**
  1. Enter five weights and a change reason.
  2. Validate the sum is exactly 1.0 and no weight is negative.
  3. Activate the version; assignments from then on log the new version.
  4. Historical decisions keep their original version and JSON snapshot.

**Screen layout**
`[ẢNH 3.7-d]` — `Scoring Configuration` with the Activate Weights dialog.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Sum | Exactly 1.0 |
| Negative weights | Rejected |
| Reason | Minimum length enforced |
| Freeze | Decision log stores the version + full config JSON |
| Effect | Applies to new assignments only |

**Normal case:** weights entered → validated → activated → next assignment uses them.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Sum ≠ 1.0 | Validation error; activation blocked |
| Reason too short | Rejected |
| Extreme weights | Warning in the impact preview |

---

## 3.7.5 Manage QC Checklists and Defect Taxonomy

**Function trigger**
`QC Knowledge (/admin/qc-knowledge)` — two tabs.

**Function description**
- **Actors:** System Administrator.
- **Purpose:** Keep inspection criteria and defect codes consistent and versioned.
- **Interface:** `GET /api/v1/admin/qc/checklists` · `POST /api/v1/admin/qc/checklists/{grade}` ·
  `GET /api/v1/admin/qc/defects` · `PUT /api/v1/admin/qc/defects/{code}` ·
  `DELETE /api/v1/admin/qc/defects/{code}`.
- **Data processing:**
  1. Pick a quality grade and author an ordered checklist (code, name, pass condition, mandatory flag).
  2. Saving creates a new immutable checklist version.
  3. Defect codes are added or deprecated; referenced codes cannot be deleted.
  4. Running inspections keep the version they started with.

**Screen layout**
`[ẢNH 3.7-e]` — `QC Knowledge`, Checklists tab with the version dialog.
`[ẢNH 3.7-f]` — Defect taxonomy tab with the defect dialog.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Versioning | Each save is a new version |
| Running inspections | Pinned to their version |
| Deprecation | Referenced code can only be deprecated |
| Mandatory items | Must be evaluated by the inspector |
| Audit | Author and timestamp recorded |

**Normal case:** new checklist version saved → used by the next inspection of that grade.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Grade without a checklist | Inspections for that grade are blocked |
| Delete referenced defect | Blocked; deprecate instead |
| Empty checklist | Save rejected |

---

## 3.7.6 Approve Lab Registration

**Function trigger**
`Lab Approval (/admin/labs)` — populated by lab self-registration.

**Function description**
- **Actors:** System Administrator (activation only); Operations Manager (network operations).
- **Purpose:** Prevent an unverified lab from entering the assignment pool.
- **Interface:** `POST /api/v1/labs/{labId}/approve` · `POST /api/v1/labs/{labId}/reject`.
- **Data processing:**
  1. Open the pending lab and inspect the declared machines, calendar and transfer time.
  2. Approve → status `ACTIVE`, the lab enters the capability filter.
  3. Reject with a reason → shown on the lab's registration screen for resubmission.

**Screen layout**
`[ẢNH 3.7-g]` — `Lab Approval` with a pending lab and its machine declarations expanded.

**Function details**

| Rule | Behaviour |
|------|-----------|
| Completeness | Machines, calendar and transfer time must be declared |
| Separation of duties | Admin activates; Operations runs the network |
| Rejection | Reason mandatory and visible to the lab |
| Audit | Approver and timestamp recorded |

**Normal case:** pending lab reviewed → approved → appears in assignment candidates.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Incomplete declaration | Approve disabled with the missing fields listed |
| Duplicate lab | Rejected with a reason |
| Spec changed after approval | Audited; if it invalidates capability the lab is suspended |

---

## 3.7.7 Review Audit Log and Job Health

**Function trigger**
`Audit Log (/admin/audit-logs)`; background-job health from `Background Job Dashboard (/jobs)`.

**Function description**
- **Actors:** System Administrator.
- **Purpose:** Provide an immutable trace of who changed what, plus visibility of failing background work.
- **Interface:** `GET /api/v1/audit-logs` · `GET /api/v1/audit-logs/export` · Hangfire dashboard `/jobs`.
- **Data processing:**
  1. Filter by actor, entity type, action and time range.
  2. Open a record to see before/after values.
  3. Inspect queue depth, recurring schedules and failures.
  4. Replay a failed job; the replay itself is audited.

**Screen layout**
`[ẢNH 3.7-h]` — `Audit Log` with filters and the record-detail dialog.
`[ẢNH 3.7-i]` — Hangfire dashboard showing recurring jobs (email every minute, calibration 02:17).

**Function details**

| Rule | Behaviour |
|------|-----------|
| Immutability | No update or delete path exists |
| Search | Filtered query under 2 s |
| Before/after | Stored as JSON |
| Replay | Idempotent; replay actions are logged |
| Health scope | Queue depth, failures, storage, integrations |

**Normal case:** filter → inspect a record → confirm the change; failed job replayed.

**Abnormal cases**

| Condition | Behaviour |
|-----------|-----------|
| Repeated failures | Escalated to the runbook |
| Log export too large | Chunked export |
| Replay fails again | Failure count increments; alerting continues |

---

## Appendix — Ảnh cần chụp (checklist)

| Mã | Màn | Route |
|----|-----|-------|
| 3.2-a | Order Configuration | `/models/:modelId/order` |
| 3.2-b | Model Library + Upload dialog | `/models` |
| 3.2-c | Model Preview | `/models/:modelId/preview` |
| 3.2-d | Payment panel | `/orders` (drawer) |
| 3.2-e | My Orders | `/orders` |
| 3.2-f | Order Details drawer | `/orders` (drawer) |
| 3.2-g | Reprint Request panel | `/orders` (drawer) |
| 3.2-h | Account & Addresses | `/account` |
| 3.2-i | Design Request | `/design-request` |
| 3.3-a | Lab Registration | `/lab/register` |
| 3.3-b | Machine Registry + maintenance dialog | `/lab/machines` |
| 3.3-c | Inventory | `/lab/:labId/inventory` |
| 3.3-d | Lab Queue (offer + countdown) | `/lab/queue` |
| 3.3-e | Decline Job dialog | `/lab/queue` |
| 3.3-f | Complete Job dialog | `/lab/queue` |
| 3.3-g | Report Incident dialog | `/lab/queue` |
| 3.3-h | Timeline & Score | `/lab/timeline` |
| 3.3-i | Shipment Batch Planner | `/lab/:labId/shipments` |
| 3.4-a | Hub Shipments (reconcile) | `/hub/shipments` |
| 3.4-b | QC Console | `/hub/qc` |
| 3.4-c | Inspection Record dialog | `/hub/qc` |
| 3.4-d | Complete Orders (pack gate) | `/hub/complete-orders` |
| 3.4-e | Waybill + Merge dialogs | `/hub/complete-orders` |
| 3.5-a | Scheduling Board (assigned) | `/scheduling` |
| 3.5-b | Decision Trace | `/ops/decisions` |
| 3.5-c | Scheduling Board (after repair) | `/scheduling` |
| 3.5-d | Date Change Decision | `/date-change/:token` |
| 3.6-a | Quote Review | `/quote-reviews` |
| 3.6-b | QC Proof Review | `/staff/qc-proofs` |
| 3.6-c | Scheduling Board (at-risk) | `/scheduling` |
| 3.6-d | Reassign & Impact Preview | `/scheduling` |
| 3.6-e | Ops Labs + capacity dialog | `/ops/labs` |
| 3.6-f | SLA Report | `/ops/sla` |
| 3.6-g | Refunds + Process dialog | `/ops/refunds` |
| 3.6-h | Payments | `/ops/payments` |
| 3.7-a | Users & Roles + dialog | `/admin/users` |
| 3.7-b | Shared Catalog | `/admin/catalogs` |
| 3.7-c | Commercial Config | `/admin/commercial-configurations` |
| 3.7-d | Scoring Config | `/admin/scoring-configurations` |
| 3.7-e | QC Knowledge — Checklists | `/admin/qc-knowledge` |
| 3.7-f | QC Knowledge — Defects | `/admin/qc-knowledge` |
| 3.7-g | Lab Approval | `/admin/labs` |
| 3.7-h | Audit Log + detail | `/admin/audit-logs` |
| 3.7-i | Hangfire dashboard | `/jobs` |
