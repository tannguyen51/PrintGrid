# 03_Audit — Đối chiếu hồ sơ PrintGrid với quy trình 12 bước (v1.2)

**Cách dùng:** mỗi bước có trạng thái ✅ đủ / ⚠️ có nhưng sai định chuẩn / ❌ thiếu, kèm hành động.
Điểm tự chấm theo checklist Phụ lục B (24 mục): **13/24** — dưới ngưỡng 20, **chưa nên lịch bảo vệ phạm vi** với GVHD cho tới khi làm xong các mục ưu tiên 1.

---

## Bảng đối chiếu từng bước

| Bước | Chuẩn của tài liệu v1.2 | Hồ sơ hiện có | Trạng thái | Hành động |
|------|------------------------|---------------|------------|-----------|
| **B1** Giải mã phiếu | Bảng P-xx + cột "Chưa rõ" + câu hỏi + giả định | Không có gì | ❌ → ✅ | **Đã tạo** `workbook/01_Extract-P-xx.md` (105 mã, 15 câu hỏi, 8 giả định) — mang vào họp GVHD |
| **B2** Phản biện | Biên bản: giả định nền, bảng so sánh ≥3 hệ thống, trần khối lượng, quyết định từng trụ, rủi ro 5 loại | 03-Problems (35+ challenges) và 07-Assumptions (38 cái) có nguyên liệu nhưng **không có quyết định**; chưa họp | ❌ → ⚠️ | **Đã tạo bản nháp** `workbook/02_Phan-bien-de-tai.md` — cần GVHD xác nhận để thành ✅ |
| **B3** Nâng cấp phiếu | v1.1 + Record of Changes A/M/D, giữ nguyên mã/tên | Chỉ có v1.0 | ❌ | Sau họp B2: tạo `Phieu_FA26SE249_v1.1.docx` + bảng RoC đầu phiếu; **không sửa lên v1.0** |
| **B4** Actor | internal/external/system/time + mục tiêu 1 câu + actor map + ma trận actor–phân hệ | 02-Stakeholders (13 nhóm, dạng needs) | ⚠️ | Tái cấu trúc thành bảng actor 4 loại. **Thiếu system actor:** Slicing Engine, Payment Gateway (sandbox), MinIO/Storage, Notification Service, Carrier (ngoại vi). **Thiếu time actor:** Scheduler (quote expiry P-24, response window P-32, recalibrate P-61, at-risk scan P-49, reprint window P-27). Mỗi từ "tự động/window/expiry" trong phiếu chưa có actor chịu trách nhiệm |
| **B5** Phân hệ & Scope | 4–6 phân hệ nghiệp vụ, đủ bản 4 ô (tên–actor–thực thể–kết quả), context diagram, Out of scope **≥5 mục mỗi cái phân loại 1 trong 5 loại lý do**, MoSCoW sơ bộ, chủ sở hữu từng phân hệ | 06-System-Scope có In/Out khá tốt + MVP vs Extended; FR doc có 6 module | ⚠️ | 6 phân hệ M01–M06 hợp chuẩn (4–6, khớp 5 WP). Cần: (a) gắn **chủ sở hữu WP** vào từng phân hệ; (b) phân loại 5 loại lý do cho từng dòng Out of scope (hiến viết dạng mô tả); (c) vẽ context diagram; (d) công bố **phân hệ lãi = M04 (Scheduling Engine)** theo P-100/P-101 — trùng đúng khuyến nghị "speculative từ iteration 1" |
| **B6** Business Rules | 15–40 BR, mỗi cái: 1 câu khẳng định, **1 trong 6 loại** (Term/Fact/Constraint/Action enabler/Computation/Inference), mức **cứng/mềm**, ngoại lệ, **cố định/cấu hình được**, **nguồn P-xx**, truy vết FR | 08-BR: 62 quy tắc, 12 domain, format Rule + Priority | ⚠️ | (a) 62 hơi vượt trần 60 — nghi ngờ trùng lặp; rà gộp (ứng viên: các rule notification lặp lại ở nhiều domain); (b) thêm 5 cột chuẩn vào từng BR — máy làm được: grep phát biểu "must/never/within"; (c) **mọi con số phải có chủ**: response window (P-32), quote expiry (P-24), reprint limit (P-51), batch bound (P-12), time budget (P-70), reprint window (P-27) — kiểm tra từng cái đã có BR chưa; (d) rule cấu hình được → phải chiếu đúng thực thể Configuration (PricingParameterSet ✓, Checklist ✓?, BatchBound ✗?) |
| **B7** Thực thể & thuộc tính | Entity List có mã E-xx, nhóm (6 loại), nhãn nghiệp vụ + quy tắc sinh mã; bảng 5 nhóm thuộc tính cho thực thể chính | 16-ERD mô tả **bảng vật lý** trực tiếp | ⚠️ | Từ 16-ERD ngược lên: lập Entity List (~25 thực thể: Customer, ModelFile, Quote, Order, OrderItem, Job, Lab, Machine, MaterialStock, InspectionResult, DefectReport, ReprintRequest, Shipment, Checklist, DefectTaxonomy, PricingParameterSet, CalibrationFactor, StandingMetric, AuditLog, ConfigParameter…) + rà 5 nhóm thuộc tính cho Job/Order/Quote. **Thiếu business key + quy tắc sinh mã** cho Job/Quote/Order — quy tắc sinh mã là BR (Bước 6) |
| **B8** ERD | Ma trận quan hệ, tên động từ 2 chiều, cardinality 2 đầu + bắt buộc, không còn N-N, **mức Conceptual nộp Review 1** | Chỉ có schema-per-module PostgreSQL (mức Physical) | ❌ | Vẽ lại 3 mức từ 16-ERD: Conceptual (nộp Review 1), Logical 3NF (Review 2), Physical (Report 4). **Bẫy sẵn có:** Job tự tham chiếu (reprint-of) — quan hệ đệ quy cần ghi điều kiện dừng; Order–Machine quan hệ N-N qua Job — đã phân rã ✓ |
| **B9** Vòng đời trạng thái | Bảng chuyển trạng thái đầy đủ (kể ô "không hợp lệ") + State Diagram cho 2–4 thực thể | Phiếu nêu "state machines for orders and jobs"; UC/BR mô tả rải rác; **không có bảng** | ❌ | Lập bảng cho **Job** (Created→Assigned→Accepted→Preparing→Printing→PostProcessing→Completed→HandedOver→Inspected→Passed/Failed→Reprinting + nhánh Rejected/Failed/Breakdown/Expired) và **Order** (Draft→Quoted→Confirmed→InProduction→AtHub→Consolidated→Shipped→Delivered→Completed + ReprintOpen/Cancelled) và **Quote** (Pending→Preliminary→Ready→Accepted/Expired). Mỗi mũi tên: sự kiện + người kích hoạt + guard + action + BR. Đây chính là thứ hội đồng 1.1 hỏi theo P-102 |
| **B10** Tính năng | FR-xx: actor chính **đúng 1**, BR áp dụng, precondition, main flow, **≥1 exception branch**, AC kiểm được, **MoSCoW**, **ước lượng S/M/L** theo ngày công; Must ≤ 60% khối lượng | 09-FR: 38 FR, priority Critical/High/Medium, description dạng bullet | ⚠️ | (a) đổi thang Critical/High/Medium → **Must/Should/Could/Won't** (Critical→Must, High→Should, Medium→Could, thêm Won't cho cái bị cắt ở B2); (b) thêm cột actor chính + BR + S/M/L — **đã soạn sẵn trong `workbook/07_Traceability.md`**; (c) tổng Must hiện ước ~205–215 ngày công = khớp trần (xem 02 §4) — công bố con số này trong slide 14 |
| **B11** NFR & Constraints | **8–15 NFR**, mỗi cái đủ 4 thành phần (chỉ số–ngưỡng–điều kiện đo–cách đo); Constraints tách riêng | 10-NFR: **43 cái**, format có Metric tốt; 07-Assumptions-Constraints đã tách riêng ✓ | ⚠️ | 43 vượt xa chuẩn: chọn **12–15 NFR chủ** đúng 12 nhóm ở mục d) phiếu (P-69→P-80 — mỗi P-xx nên có đúng 1 NFR đo được); phần còn lại hạ cấp thành "design guidelines" hoặc gộp, ghi rõ trong Report 5 cái nào test. Giữ nguyên doc 07 (Constraints tách riêng là **đúng chuẩn**, hội đồng không cãi được) |
| **B12** Truy vết | Ma trận FR→P-xx→M→Actor→BR→Entity→MoSCoW→UC; 6 dấu hiệu lỗi; 3 vòng rà soát | Không có | ❌ → ⚠️ | **Đã dựng** `workbook/07_Traceability.md` với 38 FR + **5 khoảng trống phát hiện được** (bullet ma chết); cần điền nốt cột TC- khi Report 5 map xong |

## Phát hiện lỗi đáng chú ý nhất (6 dấu hiệu — đã quét ở mức module/domain)

| Dấu hiệu | Phát hiện | Xử lý |
|----------|-----------|-------|
| **Gạch đầu dòng bỏ rơi** (P-xx không có FR nào phủ) | **P-62 Decision Log** (ghi mọi quyết định assignment với candidates+scores) — không có FR; **P-66 Checklist/DefectTaxonomy management** — không có FR ADMIN; **P-47/48** pack/ship/delivery update — chỉ có FR-HUB-004 consolidation, thiếu "ghi shipment & cập nhật delivery"; **P-55 Pricing computation** — không có FR riêng cho Pricing Engine (ẩn trong CUST-006 + ADMIN-002 → khó demo, khó test) | Đề xuất thêm: FR-SCHED-009 Assignment Decision Log (Must — vì P-76 auditability và hội đồng sẽ hỏi "lấy số đâu ra"), FR-ADMIN-005 Inspection Checklist Manager (Should), FR-HUB-005 Pack & Ship Recording (Must — dòng chảy kết thúc đơn), FR-SCHED-010 Pricing Computation (Must, tách từ CUST-006) |
| **Rule chết** | Chưa kiểm được từng BR↔FR (62 rule chưa có cột FR trong 08-BR) — sau khi thêm cột ở B6, chạy lại phép quét | ⚠️ tồn tại |
| **Actor < 3 FR** | Hub Fulfillment Staff: chỉ FR-HUB-004 (+ đề xuất 005) — thin actor, có thể gộp vào Hub Staff? Không: mục tiêu khác (QC vs logistics) → **giữ nhưng bù FR** | Đã có hướng |
| **Phân hệ > 40% FR** | M03 Hub chỉ 4/38 FR (đang dưới); M04 Engine 8+2 đề xuất /38 — cân bằng tốt sau khi thêm 4 FR | OK sau vá |
| **Thuộc tính trạng thái không có trong bảng chuyển trạng thái** | Mọi thứ — **chưa có bảng chuyển trạng thái nào** (B9 ❌) | Việc ưu tiên 1 |
| **Chuyển trạng thái không có FR** | Job "Advance state" gộp 1 FR cho 4+ chuyển — chuẩn tài liệu: mỗi mũi tên là 1 tính năng tiềm năng; chấp nhận được ở FR thô nhưng **use case phải tách branch** | Ghi vào khi viết 12-UC chi tiết |

## thứ tự việc khuyến nghị (2 tuần)

1. **Họp GVHD** với `01_Extract` + `02_Phan-bien` → chốt 15 câu hỏi + 13 quyết định trụ → **tạo phiếu v1.1 + Record of Changes** (B1→B3 ✅)
2. Vá 4 FR thiếu (decision log, checklist mgr, pack&ship, pricing) + thêm cột actor/BR/S-M-L/exception vào 09-FR (B10)
3. Bảng Job/Order/Quote state transition + 2 state diagram (B9) — vẽ cùng công cụ với ERD
4. Rà 62 BR → gộp trùng, thêm cột 6 loại / cứng-mềm / cấu hình / nguồn (B6); tách Conceptual ERD từ 16-ERD (B8); Entity List (B7)
5. Cắt 43 NFR → 15 (B11); dựng actor bảng 4 loại + ma trận actor–phân hệ (B4)
6. Chạy 3 vòng rà soát nội bộ trên `07_Traceability` (ngang/dọc/phản biện), chấm lại checklist — mục tiêu ≥ 20/24 trước khi xin lịch Review 1

## Bảng tự kiểm Phụ lục B — hiện trạng

| # | Mục | ✅/⚠️/❌ |
|---|-----|----------|
| 1 | Mọi gạch phiếu có mã P-xx | ✅ (01_Extract) |
| 2 | Cột "Chưa rõ" ≥5 câu thật | ✅ (15 câu) |
| 3 | Mỗi actor 1 mục tiêu + ≥3 FR | ⚠️ (Hub Fulfillment thin) |
| 4 | Có system + time actor | ❌ (B4) |
| 5 | 4–6 phân hệ, mỗi cái 1 chủ | ⚠️ (6 module ✓, owner chưa gắn) |
| 6 | Phân hệ lãi xác định, làm trước | ⚠️ (M04 — chưa công bố chính thức) |
| 7 | Out of scope ≥5, phân loại lý do | ⚠️ (có OOS, thiếu phân loại 5 loại) |
| 8 | ≥15 BR phân loại + cứng/mềm | ⚠️ (62 BR, thiếu cột) |
| 9 | Mọi con số trong 1 BR có mã | ⚠️ (5 con số chưa thấy chủ — xem B6) |
| 10 | ERD cardinality 2 đầu, mức Conceptual | ❌ (chỉ Physical) |
| 11 | Có Configuration entity cho rule đổi được | ⚠️ (Pricing ✓, rest chưa rà) |
| 12 | Giả định nền có mức bằng chứng | ✅ (02 §1 — chờ GVHD) |
| 13 | So sánh ≥3 hệ thống + khoảng trống | ✅ (02 §2) |
| 14 | Phân loại tính mới 1 trong 4 mức | ✅ (mới về cách làm + ngữ cảnh) |
| 15 | Mỗi trụ 1 quyết định kèm lý do | ✅ draft (02 §4) |
| 16 | Mỗi rủi ro có dấu hiệu sớm + dự phòng | ✅ (02 §5) |
| 17 | Kết luận phần nghiên cứu | ✅ (02 §6) |
| 18 | GVHD xác nhận biên bản | ❌ (chờ họp) |
| 19 | Phiếu v1.1 + Record of Changes | ❌ (B3) |
| 20 | Thay đổi lớn tách riêng | ✅ (chưa có thay đổi lớn) |
| 21 | State table + diagram thực thể chính | ❌ (B9) |
| 22 | FR có actor + BR + AC | ⚠️ (09-FR thiếu actor/BR) |
| 23 | MoSCoW 100%, Must ≤ 60% | ❌ (thang cũ Critical/High/Medium) |
| 24 | NFR có ngưỡng + cách đo | ✅ (format tốt — số lượng ⚠️) |

*(26 dòng trên gộp từ checklist 24 mục của Phụ lục B; dòng 25–26 về traceability & ngân sách: ❌/❌ — xem 07_Traceability và 02 §4. Điểm: 13✅ / 8⚠️ / 5❌)*
