# Use Case Diagram — Bảng phân tích nguồn (Step 1–4, theo chuẩn UCD của GVHD)

Sơ đồ nhiều trang: `PrintGrid-UseCase.drawio` (Cấp 0 tổng quan + 6 sơ đồ Cấp 1 theo phân hệ).
Mã giữ nguyên **UC-001…UC-028** định dạng 3 số đã dùng trong `12-Use-Cases.md` và ma trận truy vết
→ tuân thủ mục 8.1 quy tắc 4–5 (mã bất biến, không đánh số lại); độ dài 3 số cố định, không trộn UC-1/UC-01.

## Bước 1 — Actor (lấy từ External Entity của BCD, tính chất Primary/Secondary)

| Actor | Loại | Khớp BCD? | Ghi chú |
|---|---|---|---|
| Customer | Primary | ✓ | khởi phát UC-001/003/007/026 |
| Lab Manager | Primary | ✓ | UC-004/008/014 |
| Lab Operator | Primary | ✓ | UC-005/015 |
| Hub QC Staff | Primary | ✓ | UC-006/016 |
| Hub Fulfillment Staff | Primary | ✓ | UC-017 |
| Operations Manager | Primary | ✓ | UC-019/020/021/028 |
| System Administrator | Primary | ✓ | UC-009/022/023/024/025 (+second trên UC-014 duyệt lab) |
| Payment Gateway | **Secondary** | ✓ | «actor», đặt bên phải, chỉ nối UC-001/028 |
| Notification Service | **Secondary** | ✓ | «actor», nối UC-001/003/017 (gửi cảnh báo) |

- **9/9 actor khớp 9/9 External Entity BCD** — checklist A4 đạt, không lỗi #11.
- Actor **Scheduler/Time không vẽ**: mọi UC tự động đều có đường tới qua include/extend từ UC gốc
  (mục 4.1.6 cho phép "qua Use Case gốc"), và "khi phân vân thì đừng vẽ quan hệ" (mục 4.4/7) → chọn mô tả ở bảng, không thêm actor lạ vào BCD.
- **Evaluator (UC-027) gộp vào System Administrator**: vai nghiên cứu là người dùng được cấp quyền Simulator;
  không thêm actor lạ ngoài BCD (tránh lỗi #31 — nội dung chưa xác nhận). Ghi chú ở cột Relationships.

## Bước 2–3 — Mục tiêu → Use Case (Verb+Object, mức User Goal) — đã đối chiếu 12-Use-Cases.md

28 UC **tất cả Confirmed** (mỗi UC có FR + nguồn trong §0 của 12-Use-Cases.md), không có UC Suggested.
Goal Test: mỗi tên qua "Actor dừng lại và hài lòng?" — các UC tự động (002/010/011/012/013) là
**hành vi dùng chung bắt buộc** nên đứng ở phân hệ Engine và được include/extend, không phải mục tiêu người dùng → không vi phạm mức Subfunction vì được ≥2 UC dùng chung (mục 4.3).

## Bước 4 — Bảng Phân hệ × Use Case (nhóm theo đúng 6 nhóm Scope tài liệu 06)

| # | Phân hệ (Package) | Use Case (mã + tên nguyên văn như §0) | Actor trên sơ đồ | Cấp 1 |
|---|---|---|---|---|
| 1 | Customer Portal | UC-001 Place Order for 3D Printing · UC-003 Track Order · UC-007 Customer Requests Reprint · UC-026 Manage Account, Addresses & Model Library | Customer (+PG, NS secondary) | ✓ |
| 2 | Lab Network | UC-004 Lab Accepts Job Assignment · UC-005 Execute Print Job · UC-008 Lab Manager Views Performance · UC-014 Onboard Lab, Machines & Inventory · UC-015 Hand Over Completed Parts to Hub | Lab Manager, Lab Operator (+Admin duyệt UC-014) | ✓ |
| 3 | Hub Operations | UC-006 Inspect Part Quality · UC-016 Receive & Reconcile Hub Batch · UC-017 Consolidate, Pack & Ship Order | Hub QC Staff, Hub Fulfillment Staff | ✓ |
| 4 | Scheduling & Assignment (Core Contribution) | UC-002 Analyze Model Geometry · UC-010 System Runs Speculative Scheduling · UC-011 System Assigns Job to Lab · UC-012 System Reschedules on Event · UC-013 System Creates Reprint Job · UC-018 Update Estimate Calibration Factors | Admin (UC-018); 5 UC còn lại: qua UC gốc include/extend (ghost UC-001/004/005 vẽ xám) | ✓ |
| 5 | Analytics, Operations & Evaluation | UC-019 Monitor Network & At-Risk Orders · UC-020 Override Assignment, Priority & Lab Standing · UC-021 Generate & Export SLA / Performance Reports · UC-027 Configure & Run Network Simulation · UC-028 Process Refund & Reconciliation | Operations Manager (+Admin cho UC-027, +PG cho UC-028) | ✓ |
| 6 | Platform Administration | UC-009 Admin Configures Pricing · UC-022 Manage Users, Roles & Permissions · UC-023 Manage Shared Catalogs · UC-024 Manage Inspection Checklists & Defect Taxonomy · UC-025 Review Audit Log & System Health | System Administrator | ✓ |

**Đủ 28/28 UC, mỗi UC đúng 1 phân hệ; mọi sơ đồ cấp 1 ≤ 6 UC và ≤ 3 actor** — Size Rule mục 5.1 đạt dư dả.

## Quan hệ (mục 4.3/4.4 — chỉ vẽ khi đúng định nghĩa; vượt 5 mũi tên/sơ đồ thì để bảng)

Vẽ trên sơ đồ (chỉ trong phân hệ Scheduling, dùng ghost UC xám làm gốc):

| Mũi tên | Loại | Biện minh |
|---|---|---|
| UC-001 → «include» UC-002 | include | Phân tích lưới là hành vi bắt buộc của đặt hàng; dùng chung UC-001 & UC-026 (reorder) |
| UC-001 → «include» UC-010 | include | Báo giá ngày giao luôn chạy placement thử (hồn đề tài); gốc không hoàn chỉnh nếu bỏ |
| UC-001 → «include» UC-011 | include | Sau thanh toán thành công bắt buộc gán việc |
| UC-012 → «extend» UC-004 | extend | Chỉ khi lab từ chối/chờ 2h; UC-004 vẫn hoàn chỉnh nếu không mất lịch |
| UC-012 → «extend» UC-005 | extend | Chỉ khi in hỏng/chết máy; UC-005 vẫn hoàn chỉnh khi chạy đúng |

Ghi ở cột Relationships của Use Case List (không vẽ — giữ ≤5 mũi tên, mục 5.1):

| Quan hệ | Loại | Lý do không vẽ |
|---|---|---|
| UC-013 «extend» UC-006 ; UC-013 «include» UC-011 | đúng định nghĩa | Bases ở 2 phân hệ khác nhau → để bảng + ghi chú trên sơ đồ |
| UC-012 «extend» UC-016 (hư hại vận chuyển) | đúng | như trên |
| UC-027 «include» UC-011/012 (chạy engine thật trong simulator) | debatable | **Phân vân → không vẽ** (mục 4.4 dòng cuối) |
| Log In / đăng nhập | — | **Không** include vào UC nào — đã đăng nhập là Precondition (mục 4.3, lỗi #20) |
| Trình tự Upload→Quote→Pay | — | **Không** vẽ chuỗi UC (lỗi #25) — trình tự thuộc Activity Diagram |

## Đối chiếu E. Phạm vi & truy vết — mỗi Information Flow trên BCD có ≥1 Use Case xử lý (mục 8.3, lỗi #30)

| Flow BCD | UC xử lý | Flow BCD | UC xử lý |
|---|---|---|---|
| Print Order Request | UC-001 | Job Assignment | UC-011 |
| Quotation with Delivery Promise | UC-001 (+010) | Performance Standing | UC-008 |
| Order Status Alert | UC-003 | Print Progress & Incident Report | UC-005 |
| Delivery Date Approval | UC-012 (nhánh đổi hẹn) | Actual Production Data | UC-005 (+018) |
| Reprint Request | UC-007 | Print Job Queue | UC-005 |
| Lab Registration & Machine Specification | UC-014 | Inspection Result | UC-006 |
| Material Inventory Update | UC-014 | Batch Manifest & Checklist | UC-016 (+024) |
| Job Decision | UC-004 | Packing & Delivery Record / Consolidated Order List | UC-017 |
| Schedule Override & Refund Approval | UC-020 / UC-028 | Pricing & Policy Configuration | UC-009/022/023/024 |
| At-Risk Order Report | UC-019 | Audit Log Extract | UC-025 |
| Payment Request / Payment Confirmation | UC-001 / UC-028 (secondary PG) | Order & Delivery Alerts | UC-001/003/017 (secondary NS) |

→ **24/24 flow có UC, không flow mồ côi.** FR↔UC đã có trong `capstone/workbook/07_Traceability.md`.

## Tiến trình Step 5–7 (cập nhật 28/09)

1. ✅ **Human Review đạt** — nhóm chốt 28/09: (D1) Evaluator→System Administrator, (D2) không vẽ Scheduler/Time, 5 mũi tên quan hệ như bảng trên.
2. ✅ **Đã vá cột §8.2**: Secondary Actor · Subsystem · Relationships cho cả 28 UC → §0.1 của `12-Use-Cases.md`.
3. ⬜ Việc cuối: mở `PrintGrid-UseCase.drawio` (7 trang), tinh chỉnh vị trí, xuất PNG từng trang (File → Export as → PNG → tích "Each page"), đổi `v0.1 draft` → `v1.0 confirmed <ngày>` trên title sau khi thầy ký.
