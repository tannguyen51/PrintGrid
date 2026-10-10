# 07b — Đề xuất FR mới: Màn công khai Customer Portal (06/10/2026)

**Trạng thái:** 🟡 **CHỜ GVHD DUYỆT — chưa sửa 08/09/12/07 cho tới khi duyệt** (đúng nguyên tắc đã áp dụng với các XĐ Review 1: chỉ sửa bảng yêu cầu sau khi chốt).
**Nguồn phát sinh:** rà soát screen inventory cổng khách hàng phục vụ thiết kế Figma (04–05/10/2026). Code FE đã tham chiếu các màn `/samples`, `/pricing`, `/network` (navbar, trang chủ) nhưng **không có FR nào phủ** → nếu dựng sẽ vỡ trace 49/49 trong ma trận.

Phạm vi đề xuất: **3 FR mới (FR-CUST-014..016)** + **4 mã P mới (P-120..P-123)**. Không thêm UC/BR mới (lý do từng FR ở cột Notes). Các màn còn lại của inventory (`/order/new`, `/account`, `/payments`, `/claims`, `/design-request`, 403/404) **đã có FR phủ sẵn** — xem §4, không nằm trong đề xuất này.

---

## 1. Mã P mới (bổ sung vào `01_Extract-P-xx.md` nếu được duyệt)

> ⚠️ P-95..P-105 đã dùng (bài học trùng số Review 1). Đợt này lấy tiếp từ **P-120**.

| Mã | Nội dung trích | Loại | Ghi chú |
|----|----------------|------|--------|
| P-120 | Khách vãng lai cần xem mẫu in thật trước khi quyết định đăng ký/đặt đơn; nền tảng cần nguồn model "an toàn về IP" để demo giá và cấu hình | VK | Bắt nguồn từ `SampleModelsSection` + `SampleModelsData.ts` đã code sẵn (4 model placeholder, đang tắt `ENABLE_SAMPLES=false`) |
| P-121 | Minh bạch giá là lợi thế cạnh tranh của mạng lưới (vs báo giá tay từng cửa hàng, P-01); khách cần thấy cơ chế giá trước khi tải file | VK | Nội dung đã tồn tại ở `MaterialsSection` trang chủ + backend `PricingParameterSet` versioned (R1); trang public chỉ hiển thị tham số đang active |
| P-122 | Khách cần lý do tin "có thật một mạng lưới xưởng" trước khi giao tiền | VK | Số liệu `GET /api/v1/network/stats` (anon) đã có; phải giữ nguyên tắc ẩn danh như UC-003 |
| P-123 | (dự phòng — chưa dùng) hướng dẫn chuẩn bị file / FAQ tách từ trang chủ | — | Đề xuất **không** thành FR: đây là nội dung help, đưa vào runbook; chỉ đổi ý nếu GVHD muốn SEO page |

---

## 2. Hàng ma trận (format nộp — copy nguyên vào `09-Functional-Requirements.md` §0 khi được duyệt)

| Req ID | Type | Requirement Description | Category | Priority | Source | Acceptance Criteria | Status | Notes |
|--------|------|-------------------------|----------|----------|--------|---------------------|--------|-------|
| FR-CUST-014 | FR | Public sample library: curated platform-owned models, browsable/previewable by anonymous visitors; starting an order copies the sample into the customer's own library | Customer | Should | P-120 | Samples visible & previewable without login; "Đặt in" requires login then creates a private copy in the customer's library (original stays platform-owned, never listed in other customers' views); quote from a sample costs identically to a same-geometry upload (no sample premium) | **Đề xuất mới (06/10)** | Actor: Customer (+Public visitor) · BR: không cần BR mới — thừa hưởng ACCESS-002 (khu công khai) + IP-001 (bản sao thuộc khách) · feeds UC-001 alt-flow "start from sample" · US mới khi duyệt · S · ✅ nguồn mẫu (team chốt 06/10): **team tự modelling**, gán nhãn "PrintGrid original", không quét marketplace |
| FR-CUST-015 | FR | Pricing & materials page: public view shows computed price RANGES (khung từ–đến) derived from the active parameter set; exact unit prices, layer-height factors and hourly rate revealed only to authenticated customers | Customer | Could | P-121 | Anonymous endpoint returns **computed ranges only** — raw parameters never cross the wire before login (range computed server-side); all figures derive from active `PricingParameterSet` (no hand-typed numbers); version label + effective date shown; logged-in customer sees exact values | **Đề xuất mới (06/10) · phương án chốt 06/10** | Actor: Public visitor, Customer · BR: thừa hưởng QUOTE-001/CONFIG-001 (một nguồn tham số duy nhất) + ACCESS-002/005 (hai bậc công khai/đăng nhập) · không UC riêng — là màn tiền đề của UC-001 bước "understand pricing" · S · ràng buộc cứng: trang chỉ ĐỌC engine, không được nhân bản công thức ở FE |
| FR-CUST-016 | FR | Public network page: aggregated live stats (labs, machines, technologies, regions) + how-lab-selection-works explainer, with no per-lab identity | Customer | Could | P-122 | Only aggregates shown — page cannot enumerate or reverse-map a single lab; refresh ≤5 phút; empty-network state renders without breaking the page | **Đề xuất mới (06/10)** | Actor: Public visitor · BR: thừa hưởng NOTIFY/QC vô danh của UC-003 (nguyên tắc ẩn danh xưởng áp cho cả màn public) · S · dùng `GET /network/stats` hiện có, mở rộng vùng dữ liệu |

**Tổng sau duyệt (nếu đủ 3):** FR 49 → **52** · MoSCoW: +1 Should, +2 Could (không Must — không chặn golden path).

---

## 3. Chi tiết từng FR (format §1–§7 của file 09)

### FR-CUST-014: Public Sample Library
**Priority:** Medium
**Description:** A curated, platform-owned library of 3D models that anonymous visitors can browse and preview, and authenticated customers can order from with one click.

**Requirements:**
- Category grid + search for public visitors
- 3D preview per sample (reuse `ModelViewer` component from FR-CUST-003)
- Reference price range per material computed from active parameters (links to FR-CUST-015 source)
- "Đặt in" → login gate (if needed) → **private copy** created in the customer's model library → enters standard config wizard (`/order/new` path merges here)
- Admin: CRUD samples catalogue (deprecate, not hard-delete, when orders reference them)

**Acceptance Criteria:**
- Anonymous visitor can view, search and preview samples
- Ordering from a sample creates a copy owned by the customer; deleting the customer's copy never affects the platform sample
- Quote from sample = quote from an identical uploaded file (same geometry ⇒ same price)
- Sample catalogue empty state degrades to CTA "Tải model của bạn lên"

### FR-CUST-015: Pricing & Materials Page (public range / exact post-login)
**Priority:** Low (Could)
**Description:** Price transparency as a trust tool, but the engine's exact formula stays behind login. Public visitors get computed "từ–đến" ranges rendered server-side; authenticated customers get the full parameter table.

**Requirements:**
- Material cards (PLA/PETG/ABS/TPU/RESIN): colours, use-cases, and a price range per typical part size — computed server-side from the active `PricingParameterSet`, NOT raw unit prices
- Exact unit price/gram, layer-height factors, machine hourly rate, and the full worked breakdown: visible only with a Customer token
- Pricing version + effective date label on both views

**Acceptance Criteria:**
- The anonymous response contains no field from which an exact unit price can be back-solved (range endpoints may be rounded, e.g. ±10%)
- Logged-in view shows exact active values; both views derive from one parameter set — zero hardcoded prices in FE
- Changing parameters (FR-ADMIN-002) updates both views within normal propagation SLA
- Page states: loading (skeleton), engine unavailable (cached date + warning, never stale-looking silently)

### FR-CUST-016: Public Network Page
**Priority:** Low (Could)
**Description:** Trust-building public page showing the network is real and live, without exposing any partner.

**Requirements:**
- Aggregate tiles: labs count, machines, FDM/SLA split, regions
- "How we choose your lab" explainer (anonymity as a feature)
- Optional region grid — region level only, never lab-level markers

**Acceptance Criteria:**
- No request the page makes can return a single lab's identity (endpoint-level guarantee, not just UI)
- Numbers match admin ledger totals within 5 phút
- Zero-lab state shows roadmap copy (currently relevant — dev DB has real labs from pilot, production may not at demo time)

---

## 4. Những màn KHÔNG cần FR mới (đối chiếu nhanh)

| Màn | Đã phủ bởi | Ghi chú |
|-----|-----------|---------|
| `/order/new` (hub chọn nguồn model) | FR-CUST-001/002/005/006/007 (UC-001) | chỉ thiếu màn FE, không thiếu yêu cầu |
| `/account` hồ sơ + sổ địa chỉ | FR-CUST-001 (UC-026) | thiếu API self-service |
| `/payments`, thanh toán cọc/tất toán | FR-CUST-008, FR-CUST-012 (UC-031) | SEPay dời tuần 7, bridge mark-paid |
| `/claims` khiếu nại/in lại | FR-CUST-011 (UC-007) | thiếu API FE-facing |
| `/design-request` | FR-CUST-013 (UC-032) | Proposed (R1), chưa code |
| Thông báo 🔔 | FR-CUST-009 (BR-NOTIFY-001) | hub SignalR có, thiếu REST list |
| 403/404 | — | mục NFR/UX chung, không cần FR |

## 5. Ước lượng & ảnh hưởng kế hoạch (để cập nhật Excel sau khi duyệt)

| Hạng | Công | Ghế gợi ý |
|------|------|-----------|
| FR-CUST-014: `Sample` entity + catalogue API + copy-on-order + admin CRUD | 3 ngày | WP4 + Engine |
| FR-CUST-014: FE `/samples` + `/samples/:slug` (dùng ModelViewer mới) | 2 ngày | WP4 |
| FR-CUST-015: FE trang giá + endpoint đọc parameter-set active (anon) | 1 ngày | Engine nhỏ, WP4 làm |
| FR-CUST-016: mở rộng `network/stats` (theo vùng) + FE | 1 ngày | WP2 số liệu + WP4 |
| **Tổng** | **~7 ngày** | — |

Rủi ro mở — ✅ **team đã chốt cả 3 (06/10, qua Tân); còn chữ ký GVHD ở buổi họp kế** — spec dưới đây đã viết theo quyết định:

1. **Nguồn mẫu FR-CUST-014: team tự modelling.** Model gắn nhãn "PrintGrid original"; không quét Thingiverse/Printables (tránh dính license non-commercial trong khi đơn từ mẫu là đơn có thu tiền). Hệ quả kế hoạch: cần người vẽ + lịch công cụ CAD, tính vào 3d mục 014; fallback nếu team không vẽ kịp: lấy đúng 4 geometry của `SampleModelsData` rồi modelling lại đơn giản (khớp nối, vỏ hộp...)
2. **Trang giá FR-CUST-015: phương án trung gian.** Public chỉ thấy khung từ–đến tính server-side; công thức/đơn giá chính xác sau login. Hệ quả kỹ thuật: **2 endpoint** — `GET /pricing/preview` (anon, trả range đã làm tròn ±10%) và `GET /pricing` (RequireCustomer, trả parameter set active). FE không bao giờ nhận tham số thô khi chưa đăng nhập.
3. **Trang mạng lưới FR-CUST-016: ẩn danh tuyệt đối, cả lúc demo.** Aggregate + khu vực hoạt động, không tên lab (kể cả lab pilot FPT Bách Khoa). AC đã khoá ở cấp endpoint: không request nào của page trả được danh tính một lab — demo cũng chạy đúng một chế độ, không có "chế độ demo ngoại lệ".

## 6. Việc dây chuyền nếu GVHD duyệt

1. `01_Extract-P-xx.md`: thêm P-120..122 (kiểm tra không chồng trước khi ghi — bài học P-95..105)
2. `09-Functional-Requirements.md`: 3 hàng ma trận §0 + 3 mục chi tiết §1
3. `11-User-Stories.md`: +1 US (khách vãng lai xem mẫu — US-045)
4. `07_Traceability.md`: cập 3 FR vào ma trận 52/52
5. `12-Use-Cases.md`: KHÔNG thêm UC — UC-001 thêm alt-flow "A-Sample: start from sample library" (sửa 2 dòng, không đổi số UC)
6. Excel `Plan-ToanDu-An.xlsx`: +5 dòng task (7d) vào tuần tương ứng, cập G-A gate nếu team muốn demo bằng sample flow
7. `PrintGrid-Overview-Script.md`: số "49/49 FR traced" → "52/52"
