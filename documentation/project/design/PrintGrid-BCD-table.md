# Business Context Diagram — Bảng dữ liệu nguồn (Step 1–2, theo chuẩn BCD của GVHD 23/09)

Mọi tên là **danh từ / cụm danh từ**, ngôn ngữ = **English** (nhất quán với Use Case & SRS).
Chỉ entity **Confirmed** (có nguồn trong tài liệu dự án) được đưa vào sơ đồ chính thức.
Sơ đồ: `PrintGrid-Context.drawio` cùng thư mục.

## System

| Thành phần | Tên | Nguồn |
|---|---|---|
| System (1 khối đen duy nhất) | **PrintGrid — Distributed 3D Printing Fulfillment System** | Phiếu 3.1 (tên đã duyệt, đổi sang dạng noun) |

## Bảng External Entity — Information Sent / Received (bước 4 quy trình thầy)

| # | External Entity | Information Sent (→ System) | Information Received (System →) | Trạng thái | Nguồn |
|---|---|---|---|---|---|
| 1 | Customer | Print Order Request · Delivery Date Approval · Reprint Request | Quotation with Delivery Promise · Order Status Alert | Confirmed | Phiếu c) nhóm Độc giả/khách; UC-001,003,007; FR-CUST-* |
| 2 | Lab Manager | Lab Registration & Machine Specification · Material Inventory Update · Job Decision | Job Assignment · Performance Standing | Confirmed | Phiếu c) Lab Manager; UC-004,008,014 |
| 3 | Lab Operator | Print Progress & Incident Report · Actual Production Data | Print Job Queue | Confirmed | Phiếu c) Lab Operator; UC-005,015 |
| 4 | Hub QC Staff | Inspection Result | Batch Manifest & Checklist | Confirmed | Phiếu c) Hub QC; UC-006,016 |
| 5 | Hub Fulfillment Staff | Packing & Delivery Record | Consolidated Order List | Confirmed | Phiếu c) Hub Fulfillment; UC-017 |
| 6 | Operations Manager | Schedule Override & Refund Approval | At-Risk Order Report | Confirmed | Phiếu c) Ops Manager; UC-019,020,028 |
| 7 | System Administrator | Pricing & Policy Configuration | Audit Log Extract | Confirmed | Phiếu c) QTHT; UC-009,022,023,024; FR-ADMIN-004 |
| 8 | Payment Gateway | Payment Confirmation | Payment Request | Confirmed | Quyết định 23/09: cổng = SEPay (tên sản phẩm ghi ở tài liệu, trên sơ đồ dùng vai trò theo mục 3.5) |
| 9 | Notification Service | — | Order & Delivery Alerts | Confirmed | Hợp đồng RUN-01 (email provider); flow ra theo mục d) phiếu |

**Đủ 9 entity — trần "3–9" của mục 4.8; mỗi entity đúng 1 lần trên sơ đồ, mỗi chiều 1 mũi tên riêng có nhãn. Tổng 24 dòng thông tin (mũi tên một chiều).**

## Đã CÂN NHẮC và LOẠI khỏi BCD chính thức (kèm lý do theo checklist thầy)

| Ứng viên | Lý do loại |
|---|---|
| Carrier (đơn vị vận chuyển) | Không trao đổi thông tin **trực tiếp** với hệ thống — vận đơn nhập tay (quyết định B2 biên bản) → luật 7: entity gián tiếp không vẽ |
| Máy in của lab | Không có kết nối phần mềm; thợ in là kênh người → gộp vào dòng của Lab Operator |
| PostgreSQL / Redis / MinIO / Hangfire / Engine / AI | **Internal Component** — luật mục 4.1: System là khối đen |
| Ngân hàng phía sau Payment Gateway | Gián tiếp sau gateway — ví dụ cấm đúng nguyên văn mục 4.7 |
| Lịch/cron nhắc hạn (Scheduler) | Sự kiện theo thời gian **không phải entity** (luật 4.9) — thể hiện qua flow "Order Status Alert" ra Customer |
| Zalo OA / app mobile | Chưa có bằng chứng trong đề tài (Suggested) — luật AI Step 5: không vào sơ đồ chính thức |

## Đối chiếu nhanh "WHAT?" test và 12 lỗi thường gặp (mục 10)

- Không nhãn nào dạng Verb+Object ("Submit…", "Send…", "Process…") ✓
- Không nhãn cụt quá chung ("Data", "Notification" một mình) — mọi nhãn có danh từ xác định nội dung: *Quotation with Delivery Promise*, *Actual Production Data*… ✓
- Không flow nào nối 2 External Entity với nhau: việc khách quét QR trả tiền là Customer ↔ Payment Gateway → **ngoài** hệ thống, không vẽ ✓
- Không mũi tên 2 đầu, không entity nào xuất hiện 2 lần (không cần bản sao *) ✓
- Nhân viên nội bộ trường (Lab/Hub/Ops/Admin) đều đặt **ngoài** boundary — vai người dùng phần mềm, đúng luật 4.2 ✓

## Final Quality Test (mục 9) — người đọc trả lời được trong 30–60 giây?

1. System là gì? → 1 khối PrintGrid ở giữa.
2. Boundary ở đâu? → cạnh khối đen; mọi mũi tên cắt qua nó.
3. Entity nào trao đổi? → 9 hộp xung quanh.
4. Mỗi entity gửi/nhận gì? → nhãn trên từng mũi tên một chiều.
5. Có thành phần triển khai lọt vào không? → **không**.

**Việc còn lại của nhóm:** xác nhận bảng này tại buổi họp kế tiếp (đặc biệt: tách Lab Manager/Lab Operator hay gộp 1 entity "Lab Staff" để giảm còn 8 mũi tên nhóm), rồi mở `PrintGrid-Context.drawio` chỉnh vị trí và xuất PNG 2×.

---

## ⚠ Phụ lục 30/09 — tác động GVHD Review 1 (CHƯA vẽ lại, chờ duyệt B25)

Review 1 thêm actor nghiệp vụ **"Order Staff"** (duyệt báo giá UC-029 + duyệt proof lab UC-030) → BCD thành **10 entity, VƯỢT trần 3–9 (mục 4.8)**. Phương án đề xuất để về hợp lệ:
- Gộp **Hub QC Staff + Hub Fulfillment Staff + Order Staff** → 1 entity **"Hub & Order Staff"** (is-a hợp lệ: đều nhân sự nền tảng vận hành hub/đơn) → còn **8 entity**; UCD cấp 1 vẫn giữ actor tách (luật nhất quán chỉ bắt *tên khớp*, gộp cấp BCD là hợp pháp hóa theo mục 4.8).
- Flow mới của nhóm này: sends *Quote Approval · Proof Decision · Design Brief Response*; receives *Quote Draft Queue · Lab QC Proof · Design Requests*.
- Các dòng khác của bảng giữ nguyên hiệu lực; 4 luồng mới của Review 1 (hash-receipt email, offer, deposit/balance QR, batch seal) ánh xạ vào các entity đã có — không entity mới nào khác.
**Quyết chờ: B25.** Duyệt xong mới sửa `PrintGrid-Context.drawio` + `PrintGrid-UseCase.drawio` (thêm UC-029..033, offer/ack vào UC-004, deposit vào UC-001).
