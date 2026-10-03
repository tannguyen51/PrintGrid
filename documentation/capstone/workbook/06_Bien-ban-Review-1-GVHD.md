# Biên bản Review 1 (GVHD) — đối chiếu với đặc tả hiện hành & phương án

*Biên bản gốc thầy gửi gồm 2 phần: (I) "Biên bản Review 1" 9 gạch đầu dòng trả lời các câu hỏi; (II) bản ghi 19 điểm mô hình vận hành thầy muốn. Bản này lưu ý chính + map từng điểm vào mã FR/BR/UC hiện hành. Ký hiệu:*
- ✅ **KHỚP** — đã có, chỉ dẫn mã.
- 🔁 **ĐIỀU CHỈNH** — có nền rồi, sửa/tăng tốc độ người-can.
- ⚠️ **XUNG ĐỘT** — trái quyết định đã chốt, cần nhóm + thầy quyết (xem mục C).
- 🆕 **MỚI** — chưa có gì tương ứng.

---

## A. Map từng điểm của thầy → hiện trạng (19 điểm chính)

| # | Thầy nói | Hiện trạng | Verdict → hành động |
|---|---|---|---|
| I.1 | In không kịp → **phân lô (100 chia ra)**; SLL → liên hệ KH đề xuất tiến độ | Đề xuất BR-SCHED-010 (split-qty) + B16 date-change | 🔁 chính thức hóa: BR-SCHED-010 *split-lô* (Mới duyệt từ biên bản này) + BR-SCHED-011 *liên hệ KH công khai* — khớp 05_Phan-hoi A1 |
| I.2 | Tồn kho: nhập/xuất, **sai số 1–5%**, đề xuất cho KH, **tính tổng +- liên tục, phải có mã giao dịch** | FR-LAB-003 + BR-STOCK-001/002 (đang chờ duyệt) | 🔁 nâng cấp thành bộ **BR-STOCK-001..004**: reserve/settle + buffer **1–5% cfg** (thay 10% đề xuất cũ) + **mã GD mỗi lượt nhập/xuất** (traceable ledger — không phải con số trừ dồn) |
| I.3 | Không đủ nhựa → liên hệ KH | UC-010 A1/A2 (từ chối/chào ngày xa) | ✅ có; bổ sung notify "đề xuất KH" chủ động (BR-NOTIFY-001 đã bao phủ) |
| I.4 | **Deposit áp dụng TẤT CẢ đơn hàng** + thanh toán theo tiến độ giao hàng | BR-PAY-001 (trả đủ trước sản xuất); khuyến nghị ngưỡng của nhóm | ⚠️ **XĐ-4** — thầy ghi rõ "tất cả"; cần nhóm chọn: tuân thủ đầy đủ (2 stage: deposit→sản xuất, balance→trước đóng gói) hoặc trình lại lý do friction đơn nhỏ |
| I.5 / II.2 | Bù trừ vận chuyển; **phí ship tính vào đơn khách, cho phép lỗ có ngưỡng, thành BR** | 05_Phan-hoi B2 chốt nghĩa 2 | 🆕+ bộ đôi: **BR-LOG-003** transport allowance cho lab (chờ 3 quyết con) + **BR-LOG-004** phí ship khách: quote kèm shipping line, *shipping_loss_tolerance* cfg — phần vượt ngưỡng nền tảng chịu, lỗ ghi vào report |
| I.6 | Custom không file: **thêm luồng lên ý tưởng** (thu phí, nhiều giai đoạn), **bản quyền KH — cấm dùng lại** | 05_Phan-hoi B6 đề xuất out-of-scope | ⚠️ **XĐ-3** — thầy MUỐN có luồng. Phương án tối thiểu khả thi: Design Request thành đơn loại mới + multi-stage payment (dùng chung máy của XĐ-4) + **BR-IP-004** "file thiết kế trả khách, nền tảng/lab cấm tái sử dụng, vi phạm = sự cố pháp lý" |
| I.7 | % Hub/Lab: **công khai giá với Partner, thương lượng %** | 05_Phan-hoi B3 (margin tham số) | 🔁 thêm **BR-PAY-009**: partner có *rate% theo hợp đồng* (bảng thương lượng tay, nhập hệ thống, version hóa); report payout theo partner = base + allowance − reprint (BR-PAY-002 sửa) |
| I.8 / II.7 | Thuật toán điều phối theo category (chất liệu, giá, feedback, %); **>1 chục BR phân công** | FR-SCHED-003/004 + 8 ASSIGN + 9 SCHED + 4 RESCHED = 21 rule đã có | ✅ "hơn một chục" đã vượt (21); 🔁 thêm **BR-ASSIGN-009** category-match + trường feedback-KH vào scoring (I.8 liệt kê đúng 4 nhóm tiêu chí mình đã có sẵn 3) |
| I.9 | Trễ deadline phía KH: **log mỗi request**, liên hệ chỉnh ngày, hoặc hủy đơn sau đặt | BR-NOTIFY-003/004 (đề xuất C2) + B17 nợ | 🔁 chốt cả gói: request-log (audit đã có) + timeout khách + **B17 thành rule hủy-đơn sau deposit** (bắt buộc nếu XĐ-4 chọn deposit-toàn-bộ) |
| II.1 | Item đủ metadata (kích thước, chất liệu, số màu, STL); KH không biết ai in/máy nào/xưởng nào | OrderItem + FR-CUST-009 ẩn lab | ✅ khớp tuyệt đối (đây là invariant cốt) |
| II.3 | Quy trình Customer Order → kiểm in được → tính giá → Printing Order → phân phối → sản xuất → kiểm → về trung tâm → đóng gói → giao | UC-001→002→010→011→004→005→015→016→006→017 | ✅ 10 bước khớp gần 1-1; khác tên gọi: *Customer Order* = Order, *Printing Order* = Job |
| II.4 | **Tính giá không tự động hoàn toàn — nhân viên thẩm định** | BR-QUOTE tự động 100%, NFR-PERF-001 60s | ⚠️ **XĐ-1b** — xung đột trực tiếp "quote-time placement tự động". Phương án dung hòa: engine **đề xuất giá+ngày** (giữ toàn bộ machinery nghiên cứu), thêm **gate "Staff Review"** trước khi publish cho khách (workbench mới: adjust trong band %, log before/after, SLA duyệt ~2h) — giá tự động thành "draft", nhân viên mới là người chốt |
| II.5 | Customer Order → phân rã Printing Order, hệ thống hỗ trợ, **người quyết cuối** | FR-SCHED-006 tự động | 🔁 như XĐ-1b: decomposition tự động + **confirm bước phân rã bởi staff** cho đơn ≥ ngưỡng |
| II.6 | Phân phối: **nhiều nhà in cùng nhận offer → pick → hệ thống/nhân viên chọn** | BR-ASSIGN-008: nền tảng gán, lab không chọn | ⚠️ **XĐ-2** — đối đầu cơ chế. Phương án: đổi thành **offer fan-out top-k** (engine xếp hạng, gửi offer đồng thời k nhà in, first-accept thắng trong cửa sổ, ops duyệt với đơn giá trị cao) — giữ được scoring (điểm nghiên cứu), thêm quyền chọn của partner theo thầy |
| II.7 | >1 chục BR phân công | 21 rule | ✅ (khớp, dẫn bảng 08 §0) |
| II.8/9 | **BATCH**: gom nhiều Printing Order/ngày gửi đối tác 1 lần — tiết kiệm ship + vận hành | BR-SCHED-002/003 batch gom *theo vật liệu trong 1 máy* — khác khái niệm! | 🆕 thực chất là thực thể mới **ShipmentBatch (lab→hub)** ≠ batch-slicing cũ. Thêm FR-LAB-008 "gom việc của cùng lab vào chuyến trả hub định kỳ (cfg lịch xe)" + BR-LOG-005. Khái niệm batch cũ giữ nguyên tên trong SRS nhưng cần chú thích phân biệt |
| II.10 | File Server riêng, đối tác truy cập qua hệ thống | **MinIO + presigned URL** (đã build) | ✅ khớp — thầy đang mô tả đúng cái đã có; dẫn contracts + ACCESS rules |
| II.11 | **Không quản máy in nội bộ đối tác** — chỉ 1 máy tính kết nối, "quan tâm sản phẩm đạt, không quản chi tiết máy móc" | Toàn bộ core: machine registry (FR-LAB-002), máy-level timeline, chống đặt TRÙNG CẤP MÁY (NFR-REL-004) | ⚠️⚠️ **XĐ-1a — XUNG ĐỘT LỚN NHẤT** (mục B dưới) |
| II.12/13/14/15/16/17 | File server bảo mật; **hash file khi upload; email xác nhận cho KH (time/size/hash); partner xác nhận đã nhận file; mục đích = bằng chứng bàn giao & sở hữu, không phải chống mất** | ACCESS-001..004 rất gần (cửa sổ + log + no-bulk) nhưng **chưa có hash và email-by-proof-chain** | 🆕+🔁 thêm bộ **BR-IP**: hash SHA-256 lúc upload lưu metadata (**FR-CUST-002 AC sửa**), **email xác nhận nhận file** (handler của CustomerRegisteredEvent-pattern — reuse Notification Service đã BCD hóa), **partner Ack file receipt** bắt buộc khi accept job (FR-LAB-004 AC sửa: accept = tick "đã nhận file, hash khớp"), record 5 trường (file/hash/time/sender/receiver) vào audit — khớp 100% mục tiêu "chứng minh bàn giao" của thầy, chi phí ~2 ngày |
| II.18 | Đối tác **tự QC + chụp hình + upload proof** trước khi gửi về hub | QC hiện chỉ ở hub (BR-QC-001); UC-005 không có bước self-QC proof | 🆕 **BR-QC-011**: lab attach ảnh proof khi complete; FR-LAB-005 AC sửa — "complete hợp lệ khi có actuals + proof ảnh" |
| II.19 | **Nhân viên phụ trách đơn duyệt proof**, đạt mới cho đóng gói/gửi về | Không có | 🆕 **BR-QC-012 + FR-ANAL-006?**: order-staff approval gate giữa lab-complete và handover; role mới **"Order Staff"** trong RBAC (BR-ACCESS-005 ảnh hưởng: một người một role ✓ vẫn hợp) |

## B. 4 xung đột cần họp nhóm + chốt với thầy TRƯỚC khi sửa đặc tả

**XĐ-1a · Machine-level vs Partner-level (II.11 vs toàn bộ SCHED core).**
Thầy mô hình hóa đối tác như hộp đen chỉ có 1 máy tính; đặc tả hiện tại quản **từng máy** (đăng ký spec, lịch máy, chống double-booking cấp slot). Đây không phải chi tiết — nó là *lớp sâu nhất của đóng góp nghiên cứu* (đặt lịch trên máy cụ thể). Ba lựa chọn:
- **(a) Giữ machine-level** nhưng làm giao diện partner "mềm": partner khai máy (có thể khai 1 máy ảo "tôi là 1 xưởng"), hệ thống *có thể* quản tới máy — ai không khai thì engine tự hạ cấp về partner-capacity. Nghiên cứu giữ nguyên; hồ sơ giữ nguyên. *(Khuyến nghị — thêm BR-LAB-009 "lab có thể tự khai ở mức máy hoặc mức xưởng; engine tự degrade".)*
- (b) Hạ về partner-level: đơn giản đúng lời thầy nhưng bỏ mất speculative placement trên timeline máy + NFR-REL-004 (đặt trùng) — nghiên cứu nghèo đi, hồ sơ đã duyệt với GVHD từ phiếu gốc (P-30 khai máy) phải cải tiến lùi.
- (c) Giữ nguyên như cũ, không phản hồi — không ổn, thầy đã nêu thành văn bản.

**XĐ-1b · Auto-quote vs Staff-approve (II.4/5).** Dung hòa đã phác: engine draft → người duyệt có band điều chỉnh → log. Chi phí thật: màn workbench mới (~3d) + thêm role Order Staff + ảnh hưởng SLA báo giá 60s (NFR-PERF-001 cần tách 2 chỉ số: draft ≤60s, publish ≤ SLA duyệt). Câu hỏi cho thầy: **duyệt 100% hay chỉ đơn ≥ ngưỡng / khác thường** (giá lệch X% so với band engine)?

**XĐ-2 · Assign vs Offer-pick (II.6).** Fan-out top-k + first-accept + ops-duyệt-đơn-lớn là dung hòa khả thi; đụng BR-ASSIGN-008 (invariant hiện tại). Cần thầy xác nhận k và cơ chế chọn khi 2 partner nhận gần đồng thời (điểm số cao hơn thắng hay nhanh tay thắng?).

**XĐ-4 · Deposit mọi đơn (I.4).** Ràng buộc kép với nhau: deposit-toàn-bộ ⇒ *bắt buộc* định nghĩa B17 (khách bỏ đơn mất deposit?) + hoàn theo tiến độ + 2 giao dịch/đơn. Nếu chấp nhận: viết BR-PAY-007/008 bỏ điều kiện ngưỡng; USE-001 cần test lại. Đề xuất giữ lập trường chuyên môn: trình thầy phương án ngưỡng (≥500k hoặc lead >7 ngày) kèm 3 hệ lụy đo được; nếu thầy vẫn muốn toàn bộ — tuân thủ, vì đây là capstone theo GVHD.

## C. Hệ quả hồ sơ (sau khi 4 XĐ chốt)

1. `08-BR`: +~15 rule (STOCK×4, LOG×3, IP×4, QC×2, ASSIGN×1, PAY×3 sửa/mới) → tổng ~90; đánh dấu BR-ASSIGN-008 và BR-QUOTE-* bị sửa bởi Review 1.
2. `09-FR`: +FR-LAB-008 (batch chuyến), FR-ANAL-006/007 (quote-review workbench, proof-approval queue), FR-CUST-012 (voucher — chờ C1 cũ), AC sửa: CUST-002 (hash), LAB-003 (mã GD + 1–5%), LAB-004 (ack file), LAB-005 (proof ảnh), CUST-006/008 (staff gate + deposit).
3. `12-UC`: UC mới ước lượng: **UC-029 Staff Duyệt Báo Giá, UC-030 Duyệt Proof Lab, UC-031 Thanh Toán Nhiều Giai Đoạn, UC-032 Yêu Cầu Thiết Kế** (chờ XĐ chốt); actor mới **"Order Staff"** → BCD phải thêm entity/role (đúng quy trình: bảng nguồn → duyệt → vẽ lại BCD + UCD).
4. `07_Traceability`: mọi dòng mới gắn P mới — cần **extract thêm từ chính biên bản này** (đánh số tiếp P-90.. như quy trình B1).
5. Workbook 04: thêm **B19..B24** (mở: XĐ-1a, XĐ-1b, XĐ-2, XĐ-4, ngưỡng sai số tồn kho 1–5%, k của fan-out).
6. Excel kế hoạch: +~12–15 ngày công cho các gate người-duyệt — **đây là chi phí thật của Review 1**, phải đối chiếu lại trần năng lực trước khi nói "xong sớm".

## D. Việc làm ngay được không cần chờ (verdict ✅/🆕 đã rõ)

- Hash + email bằng chứng + partner ack (II.12–17): thiết kế đã chín, đưa sprint 3 (ghế Nền tảng) — **1,5 ngày**.
- Stock ledger có mã GD + sai số 1–5% (I.2): vào sprint Lab tuần 2 (module đang rỗng).
- BR-QC-011 proof ảnh của lab: sửa AC FR-LAB-005 + events.md (`JobCompletedEvent` thêm `ProofPhotoKeys[]`) — viết vào contracts ngay.
- ShipmentBatch (II.8): đặt tên + mô hình hóa trước trên giấy (tránh nhầm với batch-slicing), code để tuần 5.
