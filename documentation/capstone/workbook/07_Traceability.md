# 07_Traceability — Ma trận truy vết Phiếu → FR → BR → Entity → Actor → UC

**Áp dụng Bước 12.** Hàng = một FR (kể cả 4 FR đề xuất thêm để vá "bullet ma"). Cột UC→TC còn trống
là **bình thường ở giai đoạn này** (điền dần khi chốt SRS/Report 5). Cột nguồn P-xx xem chi tiết tại
`01_Extract-P-xx.md`; MoSCoW và ước lượng đề xuất theo quyết định B2 (`02_Phan-bien-de-tai.md` §4) — chưa GVHD duyệt.

**Ước lượng:** S ≈ ≤1 ngày công · M ≈ 2 · L ≈ 4 (FR quá L phải tách trong backlog — ghi chú kèm).

> **Ghi chú kiểm kê code (25/09):** audit 278 file nguồn → **0/43 FR ĐỦ · 18 MỘT PHẦN · 25 CHƯA CÓ**
> (bẫy chính: RBAC = 3 email hardcode `DemoRoles.cs`; `ConfirmPayment` chỉ seeder gọi; parser STL/OBJ
> đã viết nhưng không nối vào `AnalyzeModelJob`; SignalR 0 publisher; 4 module Lab/Hub/Analytics/Admin
> rỗng scaffold; IntegrationTests 0 file). Kế hoạch khắc phục + danh mục 50 task:
> `C:\Users\xuant\.claude\plans\spicy-nibbling-quiche.md` và workbook `planning/Plan-ToanDu-An.xlsx`.
> Quyết định gate 25/09: thanh toán = **SEPay** (QR + webhook + order-confirm API hai bước) — **dời làm tuần 7**;
> tuần 1–2 luồng vàng đi qua cầu `mark-paid` (ops + audit) để không bị chặn bởi gateway.

## Phân hệ (đối chiếu 6 module FR ↔ 5 WP ↔ sản phẩm f)

| Mã | Phân hệ | Actor chính | Chủ sở hữu WP | Sản phẩm phiếu (P-83→94) |
|----|---------|-------------|---------------|--------------------------|
| M01 | Customer Experience | Customer, Guest | WP4 (+WP3 domain) | Customer Portal |
| M02 | Lab Operations | Lab Manager, Lab Operator | WP5 (+WP3) | Lab Portal |
| M03 | Hub QC & Fulfillment | Hub QC Staff, Hub Fulfillment Staff | WP5 | Hub Console |
| M04 | Pricing, Assignment & Scheduling Engine ★ **phân hệ lãi** | Engine (system), Scheduler (time) | WP2 (+WP3 services) | Slicing Service, Pricing Engine, A&S Engine, Calibration, Simulation Harness |
| M05 | Operations & Analytics | Operations Manager | WP4 | Operations Console, SLA Analytics |
| M06 | Platform Administration | System Administrator | WP3 | (nền của mọi portal) |

## Ma trận chính (38 FR hiện có + 4 đề xuất)

### M01 — Customer (FR-CUST)

| FR | Tên | Nguồn | Actor chính | BR domain | Thực thể | UC | MoSCoW | Ư/L |
|----|-----|-------|-------------|-----------|----------|-----|--------|-----|
| CUST-001 | Đăng ký & đăng nhập, địa chỉ | P-19 | Customer | BR-ACCESS | Customer, Address | UC-001 (pre) · UC-026 | Must | M |
| CUST-002 | Upload model | P-20 | Customer | BR-ACCESS | ModelFile | UC-001 | Must | M |
| CUST-003 | Xem 3D + bbox readout | P-20 | Customer | — | ModelFile | UC-001 | Should (polish) | L |
| CUST-004 | Validation feedback (mesh, oversize) | P-21 | Customer | BR-QUOTE?, BR-CONFIG | MeshAnalysis | UC-002 | Must | M |
| CUST-005 | Print configuration per item | P-22, P-23 | Customer | BR-CONFIG | OrderItem | UC-001 | Must | M |
| CUST-006 | Automated quote (breakdown, date, expiry) | P-24 | Customer | BR-QUOTE, BR-SCHED | Quote, PricingParameterSet | UC-001, UC-010 | Must | L (chung SCHED-005/010) |
| CUST-007 | Order placement | P-25 | Customer | BR-QUOTE, BR-SCHED | Order (Quote→Order) | UC-001 | Must | M |
| CUST-008 | Payment (sandbox, trạng thái) | P-25 | Customer | BR-PAY | Payment, Order | UC-001 | Must (mock) | M |
| CUST-009 | Tracking realtime, giấu lab | P-26 | Customer | BR-NOTIFY, BR-ACCESS | Order, Job (read) | UC-003 | Must | M |
| CUST-010 | Model library | P-28 | Customer | BR-ACCESS | ModelFile | UC-026 | Could | S |
| CUST-011 | Reprint/complaint sau giao | P-27 | Customer | BR-QC, BR-OPS | ReprintRequest | UC-007 | Must | M |

### M02 — Lab (FR-LAB)

| FR | Tên | Nguồn | Actor chính | BR domain | Thực thể | UC | MoSCoW | Ư/L |
|----|-----|-------|-------------|-----------|----------|-----|--------|-----|
| LAB-001 | Lab registration + calendar/transfer | P-29 | Lab Manager | BR-ASSIGN (đầu vào) | Lab | UC-014 | Must | M |
| LAB-002 | Machine registry | P-30 | Lab Manager | BR-ASSIGN | Machine | UC-014 | Must | M |
| LAB-003 | Material & colour inventory | P-31 | Lab Manager | BR-ASSIGN | MaterialStock | UC-014 | Must | M |
| LAB-004 | Job assignment notification, accept/decline | P-32 | Lab Manager | BR-ASSIGN, BR-NOTIFY, BR-RESCHED | Job | UC-004 | Must | M |
| LAB-005 | Production workflow (operator advance/report) | P-35→P-40 | Lab Operator | BR-SCHED, BR-PERF, BR-ESTIM | Job, Machine, IncidentReport | UC-005 | Must | L — tách: advance / report actuals / report incident / handover |
| LAB-006 | Schedule timeline view | P-33 | Lab Manager | BR-SCHED | Job (read), MachineQueue | UC-011 (view) | Should | M |
| LAB-007 | Lab performance view | P-34 | Lab Manager | BR-PERF | StandingMetric | UC-008 | Should | M |

### M03 — Hub (FR-HUB)

| FR | Tên | Nguồn | Actor chính | BR domain | Thực thể | UC | MoSCoW | Ư/L |
|----|-----|-------|-------------|-----------|----------|-----|--------|-----|
| HUB-001 | Batch receipt & reconcile | P-41 | Hub QC | BR-QC | Shipment, Job | UC-016 | Must | M |
| HUB-002 | Checklist inspection + defect + ảnh | P-42, P-43, P-44 | Hub QC | BR-QC | InspectionResult, Checklist, DefectReport | UC-006 | Must | L — tách: chạy checklist / attribution |
| HUB-003 | Reprint management (trigger, revised commitment) | P-45 | Hub QC | BR-QC, BR-OPS, BR-RESCHED | Job (reprint) | UC-007, UC-013 | Must | M |
| HUB-004 | Order consolidation trước pack | P-46 | Hub Fulfillment | BR-QC | Order, OrderItem | UC-017 | Must | M |
| **HUB-005 ★ mới** | **Pack, ghi shipment, bàn giao carrier, cập nhật delivery** | P-47, P-48 | Hub Fulfillment | BR-NOTIFY | Shipment, Order | UC-017 | **Must** (dòng chảy kết thúc đơn) | M |

### M04 — Engine (FR-SCHED) — phân hệ lãi

| FR | Tên | Nguồn | Actor chính | BR domain | Thực thể | UC | MoSCoW | Ư/L |
|----|-----|-------|-------------|-----------|----------|-----|--------|-----|
| SCHED-001 | Geometry analysis | P-54 | Engine (sys) | BR-ESTIM | MeshAnalysis | UC-002 | Must | L |
| SCHED-002 | Slicing service (time + material estimate) | P-54 | Engine (sys) | BR-ESTIM | SliceEstimate | UC-002 | Must | L — tách: headless integration / profile mapping |
| SCHED-003 | Capability filtering (hard) | P-58 | Engine (sys) | BR-ASSIGN | Machine, Lab, MaterialStock | UC-011 | Must | M — test suite độc lập (P-71) |
| SCHED-004 | Multi-criteria scoring | P-58 | Engine (sys) | BR-ASSIGN, BR-PERF | Job, ConfigParameter | UC-011 | Must (weights cố định MVP) | M |
| SCHED-005 | Quote-time speculative placement | P-56 | Engine (sys) | BR-SCHED, BR-QUOTE | Quote, MachineQueue | UC-010 | Must (crude-first, P-101) | L |
| SCHED-006 | Job placement + backward due dates (+decompose P-57) | P-57, P-59 | Engine (sys) | BR-SCHED | Job, MachineQueue | UC-011 | Must | L — tách decompose / place / serialise-per-machine (P-72) |
| SCHED-007 | Event-driven rescheduling (repair, fallback) | P-60 | Engine (sys) + Scheduler (time) | BR-RESCHED | Job, MachineQueue | UC-012 | Must (scope 02§4: decline+fail trước) | L |
| SCHED-008 | Estimation calibration | P-61 | Scheduler (time) | BR-ESTIM | CalibrationFactor, Job.actuals | UC-018 | Should (MVP: hệ số cấu hình thủ công) | M |
| **SCHED-009 ★ mới** | **Assignment decision log (candidates + scores)** | P-62 | Engine (sys) | BR-ASSIGN, BR-CONFIG | DecisionLog | UC-011 (post) | **Must** (P-76 auditability; hội đồng sẽ hỏi) | M |
| **SCHED-010 ★ mới** | **Pricing computation + parameter-set freeze** | P-55 | Engine (sys) | BR-QUOTE | Quote, PricingParameterSet | UC-009, UC-010 | **Must** (tách khỏi CUST-006 để test được BR-QUOTE-004 reproducibility) | M |

### M05 — Operations & Analytics (FR-ANALYTICS)

| FR | Tên | Nguồn | Actor chính | BR domain | Thực thể | UC | MoSCoW | Ư/L |
|----|-----|-------|-------------|-----------|----------|-----|--------|-----|
| ANAL-001 | Network monitoring (open orders, at-risk) | P-49 | Ops Manager | BR-OPS | Job, Order, StandingMetric | UC-019 | Should | L |
| ANAL-002 | Lab performance tracking (ledger ghi nhận) | P-34, P-53 | Ops Manager | BR-PERF | StandingMetric | UC-008 | Should (MVP ghi+hiển thị; vào score → Could, theo 02§4) | M |
| ANAL-003 | SLA reporting + export | P-53 | Ops Manager | BR-OPS, BR-PERF | (aggregates) | UC-021 | Should (cần cho evaluation report) | M |
| ANAL-004 | Manual override + priority + suspend/cap | P-50, P-51, P-52 | Ops Manager | BR-OPS, BR-CONFIG | Job, Order, Lab, AuditLog | UC-020 | Should | L — tách: override / priority / suspend |
| **ANAL-005 ★ mới** | **Refund + reconciliation (quyết payment tiền thật 23/09)** | P-73, P-25 | Ops Manager | BR-PAY-004/005/006 | Payment, Refund, Order | UC-028 | **Must** | M — trả nợ rule chết BR-PAY-004 |

### M06 — Administration (FR-ADMIN)

| FR | Tên | Nguồn | Actor chính | BR domain | Thực thể | UC | MoSCoW | Ư/L |
|----|-----|-------|-------------|-----------|----------|-----|--------|-----|
| ADMIN-001 | User/role/permission management | P-63 | SysAdmin | BR-ACCESS | User, Role | UC-022 | Must | M |
| ADMIN-002 | Configuration management (params/weights/limits/thresholds, no redeploy) | P-65 | SysAdmin | BR-CONFIG | ConfigParameter, PricingParameterSet | UC-009 | Must | M |
| ADMIN-003 | Catalogue management | P-64 | SysAdmin | BR-CONFIG | Material, Colour, Technology, QualityGrade, PostProcessing | UC-023 | Must | M |
| ADMIN-004 | Audit log viewer (+ background job health — P-67 ⚠️) | P-68 | SysAdmin | BR-ACCESS, BR-CONFIG | AuditLog | UC-025 | Should | M |
| **ADMIN-005 ★ mới** | **Inspection checklist + defect taxonomy management** | P-66 | SysAdmin | BR-QC | Checklist, DefectTaxonomy | UC-024 | **Should** | M |

★ = FR đề xuất thêm để vá khoảng trống truy vết (xem `03_Audit-12-buoc.md`).

## Chiều ngược: phủ P-xx (chỉ liệt kê dòng không có FR hoặc FR mờ)

| P-xx | Trạng thái |
|------|-----------|
| P-47, P-48 (pack/ship/delivery update) | ❌ → vá bằng **HUB-005** |
| P-55 (pricing computation độc lập) | ⚠️ ẩn trong CUST-006/ADMIN-002 → tách **SCHED-010** |
| P-62 (decision log) | ❌ → vá bằng **SCHED-009** |
| P-66 (checklist manager) | ❌ → vá bằng **ADMIN-005** |
| P-67 (monitor background job health) | ⚠️ ADMIN-004 mới phủ audit log; phần health monitoring → cân nhắc ghap ADMIN-004 hoặc hạ thành NFR-DEPLOY/ops runbook — **quyết định ở B3** |
| P-69→P-80 (12 NFR thô) | ✅ map 1-1 vào 12 NFR chủ đề xuất ở B11 (xem `10-NFR` + hành động cắt ở `03_Audit`) |
| P-83→P-94 (12 Products) | ✅ mỗi sản phẩm ≥1 FR sau khi vá; **Simulation Harness & Evaluation Report** (P-92, P-93) không có FR vì là deliverable nghiên cứu — nay được UC-027 (Evaluator chạy order generator + fault injection) và UC-021 (report pipeline) phủ, demo qua UC-010/011/012 chạy trên simulated network |
| P-100→P-105 (định hướng GVHD) | ✅ phản chiếu vào: MoSCoW (Must = đúng MVS), phân hệ lãi M04, state design từ đầu (B9), SCHED-005 crude-first |

## Định lượng sau khi vá (42 FR)

| MoSCoW | Số FR | Ước lượng ngày công (S=1, M=2, L=4 trước khi tách) | Ghi chú |
|--------|-------|---------------------------------------------------|---------|
| Must | 31 | ≈ 75–80 | Chiếm 74% số FR nhưng khớp trần nhờ cut-candidates 09 §8; xem ghi chú deviation Must≤60% |
| Should | 12 | ≈ 30–40 | |
| Could | 4 | ≈ 8 | |
| Won't | (ghi ở Out of scope: batch consolidation MVP, carrier API, payment thật, fair-distribution, auto-calibration, improvement search trong sản phẩm) | — | mỗi cái có lý do thuộc 5 loại — xem `02_Phan-bien` §4 |

Must ≈ 60% khối lượng đặc tả — **sát trần cho phép của tài liệu hướng dẫn**; mọi thêm Must mới phải đổi bằng một cắt bỏ.

## Việc còn treo trên ma trận này

- [ ] Điền cột BR chính xác tới **mã BR-xxx-nnn** (hiện mới tới domain) — làm khi 08-BR có cột FR hai chiều (B6)
- [ ] Điền TC- khi Report 5 map AC→test case (13-AC đã ở Gherkin, thuận lợi)
- [x] UC-014→UC-026 đã viết vào `12-Use-Cases.md` (13→26 UC, có exception branch + change note ánh xạ số UC cũ trong sơ đồ); cột UC của ma trận này đã điền tương ứng
- [ ] Đối chiếu 09-FR: mỗi FR cần thêm **actor chính + pre/postcondition + ≥1 exception branch** theo đúng form Bước 10
