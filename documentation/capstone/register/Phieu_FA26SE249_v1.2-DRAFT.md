# Phiếu đăng ký v1.2 — BẢN DRAFT NỘI DUNG (sau Review 1 của GVHD, 30/09)

**Cách dùng:** kế thừa toàn bộ `Phieu_FA26SE249_v1.1-DRAFT.md` (giữ file v1.1 — quy tắc không sửa đè); mở bản v1.1, dán các khối dưới vào đúng mục template, **không đổi thứ tự/tên mục**. Record of Changes v1.2 đặt ngay sau các dòng v1.1.

**Nguồn quyết định:** Biên bản Review 1 (`workbook/06_Bien-ban-Review-1-GVHD.md`), bảng phương án (`workbook/05_Phan-hoi-GVHD-review-30-09.md`), nhóm chốt 4 dung hòa XĐ-1a/1b/2/4 (đầu tháng 10), quyết định gộp role **Order Staff → Operations Manager** (nhóm duyệt 06/10). Hồ sơ kỹ thuật tương ứng: 103 BR · 49 FR · 33 UC · 17 NFR core; nguồn P mới **P-106 → P-119**.

## 0. Record of Changes — các dòng v1.2 (thêm sau dòng v1.1)

| VERSION | DATE | A/M/D | IN CHARGE | CHANGE DESCRIPTION | REFERENCE |
|---------|------|-------|-----------|--------------------|-----------|
| 1.2 | …/…/2026 | M | Nhóm trưởng | Thanh toán đổi sang **nhiều giai đoạn**: mọi đơn trả deposit trước khi sản xuất, tất toán trước khi đóng gói (BR-PAY-007/008, FR-CUST-012, UC-031); đơn nhỏ vẫn một lần nếu dưới ngưỡng auto-lane ⚠B19 | Review 1 §4 (thầy); P-109 |
| 1.2 | …/…/2026 | M | Nhóm trưởng | Báo giá **không tự động hoàn toàn**: engine draft → nhân viên Operations Manager duyệt/căn chỉnh trong băng trước khi công bố (BR-QUOTE-008, FR-ANAL-006, UC-029) | Review 1 §4; P-107 |
| 1.2 | …/…/2026 | M | Nhóm trưởng | Phân công đổi từ gán tập trung sang **offer fan-out top-k**, nhà in nhận/từ chối, ops phân xử đơn lớn (BR-ASSIGN-010 thay BR-ASSIGN-008) | Review 1 §6; P-108 |
| 1.2 | …/…/2026 | A | Nhóm trưởng | Cơ chế **chia lô số lượng** sang nhiều lab khi đe dọa trễ; không cứu được → đề nghị dời ngày công khai với khách (BR-SCHED-010/011) | Review 1 §1; P-106 |
| 1.2 | …/…/2026 | A | Nhóm trưởng | **Chuỗi bằng chứng file**: SHA-256 lúc upload + email xác nhận cho khách + đối tác xác nhận nhận file khi accept; bản quyền file thiết kế thuộc khách, cấm tái sử dụng (BR-IP-001..004, NFR-LEGAL-003, UC-032) | Review 1 §12–17; P-111/P-115 |
| 1.2 | …/…/2026 | A | Nhóm trưởng | Tồn kho **dạng sổ giao dịch**: mỗi nhập/xuất có mã GD + lũy kế, dung sai 1–5%, reserve-khi-nhận/settle-khi-xong (BR-STOCK-001..003, FR-LAB-003) | Review 1 §2; P-116 |
| 1.2 | …/…/2026 | A | Nhóm trưởng | **Gom giao tự động** các đơn cùng khách+cùng địa chỉ (FR-HUB-006, BR-LOG-001/002) và **lô chuyến lab→hub** định kỳ (FR-LAB-008, BR-LOG-005) | Review 1 §4/§8–9; P-112 |
| 1.2 | …/…/2026 | A | Nhóm trưởng | Dòng tiền mở rộng: **phí vận chuyển** là dòng trong báo giá với ngưỡng lỗ cho phép (BR-LOG-004); **transport allowance** cho lab theo zone (BR-LOG-003); **tỷ giá % theo hợp đồng partner** công khai với partner (BR-PAY-009); sửa công thức quyết toán BR-PAY-002 | Review 1 §2/§5/§7; P-113/P-118 |
| 1.2 | …/…/2026 | A | Nhóm trưởng | Điều phối **theo category** (chất liệu/giá/feedback/%) thành tiêu chí chấm điểm thứ 6 (BR-ASSIGN-009); lab được khai năng lực **ở mức máy hoặc mức xưởng**, engine tự hạ cấp (BR-LAB-009) | Review 1 §8/§11; P-114/P-119 |
| 1.2 | …/…/2026 | A | Nhóm trưởng | **Dịch vụ thiết kế**: khách có ý tưởng không có file → form yêu cầu, báo giá nhiều giai đoạn, sản phẩm thuộc quyền khách; phần thiết kế làm ngoài nền tảng (FR-CUST-013, UC-032) | Review 1 §6; P-115 |
| 1.2 | …/…/2026 | M | Nhóm trưởng | QC thêm tầng lab: **tự kiểm + ảnh proof** trước khi gửi hub, nhân viên duyệt proof mới được bàn giao (BR-QC-011/012, FR-ANAL-007, UC-030) | Review 1 §18–19; P-110 |
| 1.2 | …/…/2026 | A | Nhóm trưởng | Chính sách **timeout phía khách**: 48h không duyệt dời ngày → giữ cam kết gốc + leo thang; 7 ngày không bổ sung bằng chứng → đóng STALE (BR-NOTIFY-003/004) | Review 1 §9; P-117 |
| 1.2 | …/…/2026 | — | GVHD | ☐ Xác nhận / ☐ Yêu cầu sửa — ký: | |

> ⚠ Mở theo phiếu: **B19/B20** (% deposit + chính sách mất cọc khi khách hủy), **B22** (zone allowance + nguồn tiền), **B23** (ngưỡng auto-lane duyệt giá), **B24** (voucher — thầy đề xuất, chờ xác nhận vì nhóm từng cắt promo-code). Voucher **chưa** đưa vào nội dung phiếu.

## 1. Mục 3.1 — kế thừa v1.0, không sửa (như v1.1)

## 2. Mục 3.2 a) Context — giữ khối v1.1, chèn thêm một câu

> **Phạm vi vận hành rộng hơn (Review 1):** nền tảng không chỉ quote-gán-xếp lịch mà còn quản **dòng tiền hai giai đoạn, bằng chứng bàn giao file (sở hữu trí tuệ), tồn kho có kiểm soát và chi phí/logistics phân bổ công bằng giữa hub và lab** — những cơ chế này biến cam kết "một thương hiệu" thành vận hành được với đối tác độc lập.

## 3. Mục 3.2 b) Proposed Solutions — cập nhật các trụ, thêm 3 trụ mới

Sửa 4 trụ (thay đoạn cũ trong v1.1):

> - **Trụ 4 Deadline-Backward Scheduling** bổ sung: khi forecast trễ cho phần chưa chạy, ưu tiên **chia lô số lượng** giữa các lab đủ khả năng (giá khách không đổi), kế đến gán lại, cuối cùng mới đề nghị dời ngày có duyệt của khách.
> - **Trụ 5 Speculative Quote-Time Scheduling** bổ sung: kết quả engine là **bản nháp**; báo giá chỉ tới khách sau khi nhân viên duyệt (hoặc auto-lane cho đơn nhỏ dưới ngưỡng cấu hình). Hạn báo giá tính từ lúc duyệt.
> - **Trụ 3 Capability-Filtered Assignment** bổ sung: đầu ra là **danh sách offer top-k** gửi đồng thời các lab; lab đầu tiên accept hợp lệ thắng; tie-break theo điểm; ops phân xử đơn giá trị cao. Tiêu chí scoring thêm **category-match** (chất liệu chuyên sâu, phản hồi khách, giá, % hợp đồng).
> - **Trụ 10 Model File Access Control** nâng thành **Trụ Bằng chứng & Sở hữu**: ngoài signed URL/log, mỗi file có **hash SHA-256** lúc upload, khách nhận **email xác nhận** (thời gian, dung lượng, hash), đối tác **xác nhận nhận đúng hash** khi accept job; mọi record bất biến phục hồi được khi tranh chấp.

Thêm 3 trụ (bổ sung v1.2):

> 11. **Human Supervision Gates** — hai chốt người của Operations Manager: duyệt báo giá nháp (workbench, điều chỉnh trong băng, audit before/after) và duyệt ảnh proof tự kiểm của lab trước khi lô được gửi về hub. Máy đề xuất, người chịu trách nhiệm.
> 12. **Staged Payment & Partner Economics** — deposit mọi đơn (cfg %, mặc định 50) mới vào sản xuất; tất toán trước gate đóng gói; phí ship là dòng minh bạch trong báo giá với ngưỡng lỗ nền tảng chịu; lab nhận `base × rate hợp đồng + allowance vận chuyển − phí in lại`, tất toán qua runbook, không ví nội bộ.
> 13. **Design Intake** — khách có ý tưởng chưa có file: form mô tả + ảnh tham khảo vào hàng đợi ops, báo giá thiết kế nhiều giai đoạn dùng lại máy staggered payment; file bàn giao thuộc bản quyền khách, nền tảng và lab cấm tái sử dụng.

## 4. Mục 3.2 c) Functional Requirements — sửa 3 vai, không thêm bullet

> - *Customer:* bullet pay → "Pay a **deposit** to start production and **settle the balance** before shipping, through the integrated payment gateway" · thêm bullet: "Submit a **design request** (idea + references) and track its staged quotation"
> - *Lab Manager:* bullet accept → "Receive **job offers** from the engine and accept/decline within the response window, acknowledging the received file hash"; bullet inventory → "Maintain material/colour inventory as a **transaction ledger** (code, reason, running total, 1–5% reserve tolerance)"; bullet machines → "Declare capability **at machine or workshop level**"; thêm bullet: "Group proof-approved jobs into **scheduled hub-bound shipment batches**"
> - *Operations Manager:* thêm 2 bullet: "**Review and publish** engine-drafted quotes (adjust within configurable band, logged)" và "**Approve lab QC photo proofs** before batches may ship to the hub"
> - *Hub Fulfillment:* bullet pack → "Consolidate and pack orders, **auto-merging shipments for the same customer and address**"
> - Các vai còn lại giữ như v1.1. (Không có role mới: hai chốt duyệt thuộc Operations Manager — quyết định nhóm 06/10, giữ Actor khớp BCD 9 thực thể.)

## 5. Mục 3.2 d) Non-Functional Requirements — sửa 2 gạch, thêm 1

> - Quote responsiveness: *draft* engine ≤ 60 s (p95) như v1.1; **thời gian công bố báo giá = SLA hàng đợi duyệt (cfg, mặc định 2 h)** — tách hai chỉ số.
> - Reliability: giữ; bổ sung "double-booking **bất khả thi tại tầng database** (EXCLUDE constraint) ở cấp khai báo của lab".
> - **MỚI — Auditability of file handover:** 100% upload có hash + email biên nhận ≤ 1 phút; 100% accept kèm xác nhận hash; mẫu tranh chấp bất kỳ phục hồi đủ file/hash/thời gian/người gửi-người nhận.

## 6. Mục 3.2 e) — giữ như v1.1 (không đổi)

## 7. Mục f) Products — cập nhật

> - **Operations Console** (gộp vào Ops Console đã có): thêm hai màn *Quote Review Workbench* và *Lab Proof Approval Queue*.
> - **Lab App**: thêm màn *Inventory ledger* và *Shipment batch planner*.
> - **Customer Portal**: thêm màn *Design request*; checkout hiển thị 2 giai đoạn thanh toán.
> - Hub Console: thêm gợi ý **kiện gộp cùng khách** tại màn pack.

## 8. Mục g) Proposed Tasks — 2 dòng

> - WP3: "staged-payment gateway legs (deposit/balance) and reconciliation" (sau SEPay tuần 7).
> - WP1: "B17/B19/B20 — chính sách hủy đơn/mất cọc — chốt với GVHD trước khi code deposit".

## 9. Out of scope — bổ sung 3 dòng (tiếp bảng v1.1)

| Không làm | Loại lý do | Diễn giải |
|-----------|-------------|-----------|
| Phần mềm thiết kế 3D trong nền tảng | Vượt nguồn lực | Nền tảng chỉ intake + chuyển giao kết quả; thiết kế là dịch vụ runbook |
| Ví điện tử / thanh toán tự động cho lab | Ngoài mục tiêu + phụ thuộc | Quyết toán bằng runbook có công thức minh bạch trong hệ thống |
| Campaign engine cho voucher | Chờ xác nhận B24 | Nếu thầy duyệt: chỉ mã cố định giảm %/tiền, không hệ thống chiến dịch |

## 10. Rủi ro — thêm 2 dòng cuối

> Rủi ro mới: **ma sát chuyển đổi do deposit mọi đơn** (đo qua NFR usability, đề xuất ngưỡng auto-lane); **tranh chấp cọc khi khách hủy** (B17/B20 chưa chốt — chặn code trước khi có chính sách).

## 11. Phần nghiên cứu — giữ như v1.1

---

## Checklist trước khi nộp v1.2

- ☐ Mọi dòng RoC trỏ đúng một mục biên bản Review 1 hoặc quyết định nhóm có ngày
- ☐ Không thêm role mới vào c) — hai chốt duyệt thuộc Operations Manager (nhóm chốt 06/10, tránh BCD vượt 9 thực thể)
- ☐ ⚠ B19/B20/B22/B23/B24 để nguyên, chờ chữ ký thầy — không tự chốt số
- ☐ P-106→P-119 khớp `workbook/07_Traceability.md`; không dùng lại số P cũ
- ☐ Con số hồ sơ (103 BR/49 FR/33 UC/17 NFR) khớp SRS — phiếu chỉ mô tả phạm vi, không liệt kê mã
- ☐ Voucher chưa vào nội dung, chỉ nằm ở Out-of-scope dạng "chờ B24"
