# Report 3 — 3.1.2 Screen Descriptions (draft cho PrintGrid)

**Nguồn:** `frontend/src/routes/AppRoutes.tsx` + rà trực tiếp từng page (tiêu đề, khối nội dung, dialog)
ngày 28/10/2026. Mỗi mô tả dưới đây bám đúng nhãn UI có trong code, không suy diễn.

> **Cách dùng:** copy bảng A vào mục 3.1.2. Bảng B (pop-up) và C (màn nhiều tab) là phần bổ sung —
> template chỉ yêu cầu bảng screen, nhưng có 2 bảng này thì mục 3.1.1 và 3.1.3 sẽ khớp nhau.

**Quy ước đếm:** 47 route nhưng **44 screen** vì có 3 cặp route dùng chung một component:
`/models` = `/library`, `/lab/timeline` = `/lab/score`, `/hub/shipments` = `/hub/complete-orders`.
`/lab/settings` render cùng component với `/ops/labs` nhưng scope khác (xưởng tự xem vs Ops xem toàn mạng)
nên vẫn tính là 2 screen.

---

## A. Flat screens (44)

| # | Feature | Screen | Description |
|---|---------|--------|-------------|
| 1 | Public | Home | Public landing page: value proposition for the print-on-demand network, material/quality showcase, aggregated network statistics (no lab identity), and calls to action "Đặt in ngay" / sign-up. |
| 2 | Authentication | Login | Email/password or Google sign-in. On success the user is redirected to the landing screen of their role; wrong credentials and locked accounts show an inline error. |
| 3 | Authentication | Register | Customer self-registration: full name, email, password (≥ 8 characters) and terms acceptance. Submitting sends a verification email; the account cannot order until verified. |
| 4 | Authentication | Verify Email | Consumes the emailed verification token and confirms the address. Invalid or expired tokens offer a resend action. |
| 5 | Authentication | Forgot Password | Accepts an email address and always returns the same confirmation, so the screen cannot be used to discover whether an account exists. |
| 6 | Authentication | Reset Password | Consumes a one-use token (30-minute expiry) and sets a new password; the token is invalidated after a successful reset. |
| 7 | Model Management | Model Library | Table of the customer's own models (name, format, size, analysis state) with upload, 3D preview and delete. Quota usage is shown; models referenced by an open order cannot be deleted. |
| 8 | Model Management | Model Preview | In-browser 3D viewer with rotate/zoom, bounding-box readout (X × Y × Z), volume, print-time estimate and printability badges ("Có thể in", "Không kín", "Non-manifold", "Không thể in"). Entry point to configuration. |
| 9 | Ordering | Start Order | "Choose from library" or "Upload new 3D file" — the source selector that routes into Order Configuration. Shows analysis state before configuration is allowed. |
| 10 | Ordering | Order Configuration | Per-item print configuration (material, colour, layer height/quality, infill, post-processing, quantity, service level, required-by date) beside a live quote panel with breakdown, total, committed delivery date and quote expiry. |
| 11 | Design Service | Design Request | Customer briefs for a model that does not exist yet: title, description and reference images, plus the list and status of previously submitted requests. |
| 12 | Order Tracking | My Orders | The customer's orders with status chips (Chờ thanh toán → Đã xác nhận → Đang sản xuất → Kiểm tra chất lượng → Giao hàng), promised date and at-risk indicator. Opens the Order Details drawer. No lab identity is ever displayed. |
| 13 | Order Tracking | Date Change Decision | Reached from a notification link when production cannot meet the promised date. The customer accepts or rejects the proposed new date; the decision and timestamp are recorded. |
| 14 | Account | Account & Addresses | Profile information and the delivery-address book. The default address is prioritised at checkout; an address used by an open order cannot be deleted. |
| 15 | Lab Onboarding | Lab Registration | Lab profile and location, production calendar and hub transfer time, and the declared machine list. Submitting moves the lab to PENDING; an admin rejection reason is shown in a banner. |
| 16 | Lab Onboarding | Machine Registry | Machines with their full capability specification (technology, build volume, tolerance, supported materials). Add/edit is done in a dialog; a machine with active jobs cannot be removed. |
| 17 | Lab Onboarding | Inventory | Material and colour stock levels with reserved quantity, low-stock warnings and the stock-transaction ledger (in / out / adjustment with running total). |
| 18 | Lab Operations | Lab Settings | The lab's own profile, calendar and capacity declaration. Same console component as Ops Labs, but scoped to the signed-in lab and without cross-network data. |
| 19 | Lab Operations | Lab Queue | Job offers and accepted jobs with the 2-hour response countdown. Actions: accept, decline (reason required), start, complete (actuals required) and report an incident. |
| 20 | Lab Operations | Timeline & Score | Gantt of the lab's machine queues plus the performance standing: completed jobs in the last 90 days, score, and anonymised comparison against the network average. |
| 21 | Lab Operations | Shipment Batch Planner | Groups eligible jobs (completed **and** QC-proof approved) into a hub-bound run, previews weight and zone allowance against the declared vehicle calendar, then seals the batch and records the waybill. |
| 22 | Hub | Hub Shipments | Fulfilment console. Receiving side: expected batches, reconciliation as RECEIVED / MISSING / DAMAGED. Outbound side: pack gate (all items QC-passed), waybill recording, shipment merging and delivery confirmation. |
| 23 | Hub | QC Console | Inspection queue. Loads the checklist bound to the order's quality grade, records each criterion and its pass condition, and captures pass/fail with defect class, fault attribution and photo evidence. |
| 24 | Operations | Quote Review | Queue of engine-drafted quotes. Staff inspect the price breakdown and the scheduling basis, may adjust within the configured band (reason required), then approve (publishes to the customer and starts the expiry clock) or reject. |
| 25 | Operations | QC Proof Review | Queue of lab self-QC evidence. Staff compare the photos and quality report against the grade criteria; approval unlocks the lab's hand-over batch, rejection returns the job with a reason. |
| 26 | Operations | Scheduling Board | Live operations board: open orders, at-risk orders (slack below threshold), lab load and machine queues. Manual intervention uses a two-step reassign flow with an impact preview before any customer date changes. |
| 27 | Operations | Escalation Queue | Open alerts that the engine cannot resolve alone — reprint-limit breaches, no feasible lab, SLA risk — each with the affected order and the recommended action. |
| 28 | Operations | Decision Trace | Enter a job code to reconstruct why the engine chose a lab: candidate set that passed the capability filter, per-criterion scores and the configuration version used. |
| 29 | Operations | Ops Labs | Network view of labs including PENDING and suspended ones. Actions: add lab, edit machines, set a concurrency cap and reject a pending lab with a reason. |
| 30 | Operations | SLA Report | Period and scope selection, then aggregated on-time rate, tardiness, acceptance rate, capacity and completion metrics, with CSV export. An empty period shows "no data" rather than zeros. |
| 31 | Operations | Payments | Pending and settled payment legs (deposit / balance), gateway state and the mark-paid confirmation used by the bridge path while the gateway is not live. |
| 32 | Operations | Refunds | Refund queue by status. Staff choose full or partial scope per item, record a reason, approve, and the gateway call is idempotent by transaction id. |
| 33 | Operations | Design Requests (ops) | Ops-side queue of customer design briefs: review the brief, quote the design stages, track status and attach the delivered STL to the customer's order draft. |
| 34 | Administration | Admin Overview | Landing dashboard for platform KPIs (accounts, labs, orders, jobs) and a shortcut to background-job health. |
| 35 | Administration | Users & Roles | Account list with role assignment and activation state. Create/edit happens in a dialog; role changes revoke sessions and self-lockout is blocked. |
| 36 | Administration | Audit Log | Append-only system log with actor, entity and time filters. Selecting a row opens the immutable record with before/after values. No update or delete path exists. |
| 37 | Administration | Commercial Configuration | Versioned commercial parameters (pricing inputs, deposit percentage, shipping thresholds, review band). Each change creates a new version with an effective date; historical versions stay readable. |
| 38 | Administration | Scoring Configuration | Versioned assignment weights. A new version must total exactly 1.0 and requires a change reason; activation is confirmed in a dialog. |
| 39 | Administration | Shared Catalog | **Multi-tab screen** — Materials, Colours, Technologies, Quality Grades, Post-processing. Entries in use can only be deprecated, never deleted; changes propagate to pricing and the capability filter. |
| 40 | Administration | QC Knowledge | **Multi-tab screen** — Checklists and Defect taxonomy. Checklists are versioned per quality grade; a defect code already referenced can only be deprecated. |
| 41 | Administration | Lab Approval | Reviews PENDING lab applications: inspects the declared machines and calendar, then approves (lab becomes selectable by the engine) or rejects with a reason. Note the separation of duties — Admin approves activation, OpsManager runs the network. |
| 42 | System | Forbidden (403) | Shown when an authenticated account opens a route outside its role, with a link back to its own landing screen. |
| 43 | System | Not Found (404) | Catch-all for unknown URLs; prevents the blank render the application previously produced. |
| 44 | Operations (internal) | Background Job Dashboard | Hangfire dashboard, admin-only: queue depth, recurring schedules (email outbox every minute, calibration daily at 02:17), failures and replay. |

---

## B. Pop-ups, drawers and panels (30)

| # | Parent screen | Pop-up / panel | Type | Description |
|---|---------------|----------------|------|-------------|
| P1 | Model Library, Start Order | Upload / Edit Model | Dialog | File picker (`.stl .obj .3mf .glb`), name and metadata; shows upload progress and validation errors before the analysis job runs. |
| P2 | Model Library | Delete Confirm | Dialog | Confirmation before deleting a model; blocked with an explanation when the model belongs to an open order. |
| P3 | Account & Addresses | Add / Edit Address | Dialog | Recipient, phone, street, city and address type (home/office); required fields gate the save button. |
| P4 | My Orders | Order Details | Drawer | Side drawer with three blocks: status timeline, payment (deposit/balance) and reprint actions. The only place lab-adjacent progress is shown, still anonymised. |
| P5 | Order Details | Reprint Request | Panel | Complaint form with reason and photo upload, allowed only inside the 30-day guarantee window. |
| P6 | Order Details | Payment Panel | Panel | Deposit or balance QR, amount, expiry and current payment state; refreshes when the gateway webhook confirms. |
| P7 | Lab Queue | Decline Job | Dialog | Reason is mandatory; submitting triggers the rescheduler and is recorded on the job. |
| P8 | Lab Queue | Complete Job | Dialog | Actual print duration and material used (both mandatory) plus the self-QC photo proof required before hand-over. |
| P9 | Lab Queue | Report Incident | Dialog | Incident type, description and photo evidence; a print failure or breakdown enters the rescheduling path. |
| P10 | Machine Registry | Maintenance Schedule | Dialog | Maintenance window per machine so the engine stops offering it during that period. |
| P11 | QC Console | Inspection Record | Dialog | Full inspection record: checklist items with pass conditions, result per item, defect class, fault attribution and photos. |
| P12 | Hub Shipments | Record Waybill | Dialog | Manual waybill number for an order or merged shipment; no carrier API in this release. |
| P13 | Hub Shipments | Merge Shipments | Dialog | Groups open orders of the same customer and address into one package and one waybill; only all-QC-passed, fully settled orders are eligible. |
| P14 | Hub Shipments | Refund by Original Order | Dialog | Partial refund scoped per item of the original order; used when one item of a merged shipment is refunded. |
| P15 | QC Proof Review | QC Proof Detail | Dialog | Lab photos and quality report side by side with the grade criteria the reviewer must judge against. |
| P16 | QC Proof Review | Reject Proof | Dialog | Mandatory rejection reason; the job returns to the lab and the rejection is audited. |
| P17 | Scheduling Board | Reassign & Impact Preview | Dialog (2 steps) | Step 1 previews the reassignment through the capability filter; step 2 is only shown when the customer's promised date would change, and requires an additional reason. |
| P18 | Ops Labs | Add Lab | Dialog | Creates a lab record (name, location, transfer time) before its machines are declared. |
| P19 | Ops Labs | Add / Edit Machine | Dialog | Machine capability specification for a specific lab; feeds the engine's capability filter. |
| P20 | Ops Labs | Capacity Cap | Dialog | Maximum concurrent jobs for a lab; empty means unlimited. |
| P21 | Ops Labs | Reject Lab | Dialog | Rejection reason shown to the lab on its registration screen. |
| P22 | Payments | Mark Paid Confirm | Dialog | Confirms a manual deposit/settlement record on the bridge path; requires a reference and writes an audit entry. |
| P23 | Refunds | Process Refund | Dialog | Full or per-item partial refund with reason and approval; the gateway call is idempotent. |
| P24 | Users & Roles | Create User | Dialog | Email, name and role; the role determines which portal the account lands in after login. |
| P25 | Commercial Configuration | New Version | Dialog | Name, summary and the parameter set; saving creates a draft version rather than mutating the active one. |
| P26 | Scoring Configuration | Activate Weights | Dialog | Five weights (due-date slack, reliability, load balance, cost, transit) that must total 1.0, plus a change reason. |
| P27 | Shared Catalog | Add / Edit Catalogue Item | Dialog | Code, display name and attributes for the selected tab; editing an in-use item creates a revision. |
| P28 | QC Knowledge | Checklist Version | Dialog | Grade, ordered criteria (code, name, pass condition, mandatory flag) saved as a new immutable version. |
| P29 | QC Knowledge | Defect Item | Dialog | Defect code, name, description, default fault attribution and deprecation flag. |
| P30 | Audit Log | Audit Record Detail | Dialog | Immutable record view with actor, entity, action, timestamp and before/after values. |

---

## C. Multi-tab screens (2)

| Screen | Tabs | Note |
|--------|------|------|
| Shared Catalog | Materials · Colours · Technologies · Quality Grades · Post-processing | Tab bar is scrollable; each tab is an independent CRUD list sharing the same dialog. |
| QC Knowledge | Checklists · Defect taxonomy | Checklist tab is grouped by quality grade; defect tab is a flat code list. |

> Đây là **toàn bộ** màn nhiều tab trong hệ thống. Nếu Report cần ví dụ "screen with multiple information
> tab", dùng Shared Catalog. `Order Details` **không** phải màn tab — nó là Drawer 3 khối, đừng ghi sai
> để tránh bị đối chiếu với ảnh chụp.

---

## D. Shared UI patterns (có thể viết 1 đoạn dưới bảng)

- **Two-tier navigation.** Public/auth screens use no navbar; customer screens share `CustomerLayout`;
  all staff consoles share `StaffLayout`; administration shares `AdminLayout`. The navbar never disappears
  when moving between screens of the same role.
- **Role-based landing.** Login redirects by role: Customer → `/models`, Lab → `/lab/queue`,
  Hub → `/hub/qc`, OpsManager/Admin → `/scheduling`, Admin → `/admin`.
- **List → detail pattern.** Every console is a filterable list plus a drawer or dialog for the detail, so
  no list screen needs a second route.
- **Destructive actions always confirm** and every mutation writes an audit record (see 3.1.4 #16).
- **Empty and error states are explicit** ("Chưa có job nào…", "Không có dữ liệu báo cáo") instead of
  blank tables or zeros.

---

## E. Đối chiếu nhanh với mục 3.1.3

Nếu bảng phân quyền (3.1.3) dùng tên role khác với bảng này, sửa ở **một chỗ duy nhất** rồi copy sang:
`Guest · Customer · Lab Manager · Lab Operator · Hub QC · Hub Fulfilment · Operations Manager ·
System Administrator` — đúng 7 role khớp `Roles.cs`, cộng Guest cho trạng thái chưa đăng nhập.
