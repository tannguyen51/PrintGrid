# Phản hồi review GVHD — phương án đề xuất cho 12 câu hỏi/đề xuất

*Nguồn: nhận xét GVHD sau buổi review hồ sơ (ghi nhận 30/09). Mỗi mục phân loại:
**[CÓ RỒI]** = đã có trong FR/BR/UC, dẫn mã; **[GAP → ĐỀ XUẤT]** = cần thêm; **[QUYẾT ĐỊNH]** = cần nhóm+thầy chốt chính sách.*

---

## A. Nhóm "hệ thống đã có sẵn" — trả lời thầy bằng mã tài liệu

### A1. In không kịp tiến độ (số lượng lớn) → điều phối chỗ khác hoặc báo khách công khai

**Đã có:** tái lập lịch sự kiện ≤30s (FR-SCHED-007, BR-RESCHED-001/002, UC-012); in lại URGENT thừa hưởng deadline (BR-RESCHED-003); đổi ngày giao chỉ theo **3 điều kiện + khách duyệt công khai** (BR-SCHED-008, NOTIFY-002, B16 đã chốt 23/09). Báo giá đã tính trước cả tải hiện hữu nhờ speculative placement (FR-SCHED-005).
**Gap nhỏ còn lại:** ý "chia **số lượng của một item** sang nhiều lab cùng chạy song song" chưa nói tường minh.
**Đề xuất thêm:**
- **BR-SCHED-010** (mới): khi forecast trễ cho phần chưa bắt đầu, engine ưu tiên **split-quantity** giữa các lab đủ khả năng (giá không đổi — đồng nhất BR-QUOTE-001) trước khi xét đổi ngày giao.
- **BR-SCHED-011** (mới): nếu vẫn không cứu được ngày cam kết → rơi đúng điều kiện 3 của B16: ops tạo **Date Change Request** gửi khách (notify + approve); khách từ chối → hoàn phần ảnh hưởng theo BR-PAY-004.
- FR: sửa **FR-SCHED-007** acceptance: "kịch bản job 200 pcs forecast trễ: split ≥2 lab trong ≤30s HOẶC sinh Date Change Request — không im lặng trễ".

### A2. Quản lý tồn kho

**Đã có:** FR-LAB-003 (khai báo tồn vật liệu×màu, trừ tự động, cảnh báo gần cạn) + BR-ASSIGN-001 (tồn kho là ràng buộc cứng khi lọc) + tồn kho lab đối tác là nguồn dữ liệu thật (FR-LAB-001..003).
**Đề xuất siết (nằm trong plan nhưng chưa thành BR):**
- **BR-STOCK-001** (mới, đặt tên nhóm STOCK): **reserve-on-accept** — nhận job là trừ tạm (soft lock) đúng số gam dự toán; decline/complete-hỏng → **release**; complete-DONE → **settle** theo số thực đo (thừa tạm → trả kho).
- **BR-STOCK-002**: số cần.reserve = `qty × grams ước lượng × (1 + buffer %)`, buffer cấu hình được (mặc định 10%) — plastite hao do test-in/support.
→ Gom vào **FR-LAB-003** acceptance mới; effort nhỏ (module Lab đang rỗng nên làm luôn, tuần 2).

### A3. "In số lượng nhựa có đáp ứng đơn hàng không?"

**Bản chất = A2 + A1:** bộ lọc khả thi loại lab không đủ nhựa cho job (BR-ASSIGN-001 + BR-STOCK-002); nếu **cả mạng** không đủ → UC-010 A1/A2 từ chối báo giá hoặc chào ngày xa hơn (đã thiết kế). Không cần cơ chế mới — cần **BR-STOCK-002 ở trên** để con số gam là *dự toán + đệm*, không phải đúng bằng ước lượng.

### A4. "Order cùng lúc thì thời gian giao thế nào, có gộp lại không?"

**Đã có:** mỗi đơn có ngày cam kết riêng tính trên **toàn bộ tải hiện hữu** — đơn vào sau nếu làm trễ đơn trước, engine dời lịch hoặc từ chối (FR-SCHED-005/006); đơn nhiều item có thể **tách in nhiều lab nhưng chỉ ship khi tất cả pass QC** (BR-QC-008) → bản chất đã "gộp ở hub" trong phạm vi một đơn.
**Chưa có:** gộp **giữa các đơn khác nhau** của cùng một khách → xem B2.

---

## B. Nhóm "có gap → đề xuất giải pháp"

### B1. Gom đơn để giao tối ưu (tự động) — **[GAP]**

**Đề xuất FR-HUB-006 (Should, tuần 5–6):** tại thời điểm chuẩn bị đóng gói, hệ thống tự nhóm các đơn **cùng khách + cùng địa chỉ** còn mở; điều kiện gộp: mọi item pass QC và ngày sẵn sàng chênh ≤ cửa sổ cấu hình (mặc định 24h). Một vận đơn + một kiện; phí ship (nếu tính) chỉ một lần. Khách được show lựa chọn "giao chung/giao riêng" trước khi seal đơn — không áp đặt.
BR mới: **BR-LOG-001** (điều kiện gộp), **BR-LOG-002** (đơn đã gộp = một đơn vận chuyển, khiếu nại/hoàn vẫn theo đơn gốc).

### B2. Xử lý bù trừ vận chuyển — **[GAP — GVHD xác nhận: NGHĨA 2, chốt 30/09]**

- **Nghĩa 1 — khách bị thiệt do vận chuyển (mất/hỏng):** không phải ý thầy. Đường xử cũ giữ nguyên: QC-010 incident + quy trách + in lại/hoàn (BR-PAY-004).
- **Nghĩa 2 — chi phí lab chịu khi chuyển hàng lên hub (bù trừ công bằng):** ✅ **đây là yêu cầu của GVHD.** Bài toán: lab xa hub vừa bị scoring logistics phạt (ít được chọn), vừa tự bù tiền xe — phạt kép, lab xa rời mạng → mất ý nghĩa "distributed".
  **Đề xuất:** đưa **transport allowance** vào bộ tham số giá có version (CONFIG): `allowance = zone_rate[zone(lab→hub)] × ceil(kg)`; công thức quyết toán đổi thành **BR-PAY-002 sửa**: `payout_lab = base_cost + transport_allowance − reprint_costs`, đối soát qua runbook như đã chốt 23/09. Trong hệ thống chỉ *tính và hiển thị*, không ví điện tử nội bộ.
- **Còn 3 quyết định con trước khi viết BR chính thức** (chờ nhóm/thầy):
  1. Bù theo **bảng zone** (đề xuất — chống khai gian) vs chi phí thực lab khai?
  2. Nguồn tiền: **nền tảng chịu từ margin** (đề xuất, gắn B3) vs cộng vào giá khách (loại — đụng BR-QUOTE-001)?
  3. Hiệu lực từ đầu hay khi có ≥2 lab ngoài zone 1? — phụ thuộc lab đối tác đang ở zone nào; nếu xa hub thì phải có từ tuần 4 vì pilot demo payout.

### B3. Phần trăm doanh thu Hub ↔ Lab — **[GAP nhỏ, rẻ]**

Mô hình hiện tại: khách trả **giá mạng**; lab nhận **base cost**; hiệu số = nền tảng + hub. Chưa có chỗ *khai* và *báo cáo* con số này.
**Đề xuất:** tham số `platform_margin_pct` (hoặc explicit hub_fee) trong parameter set giá, **phiên bản hóa + freeze theo quote** như mọi tham số khác (BR-CONFIG-001); mở rộng **FR-ANAL-003** thêm report doanh thu: `price − payout_lab − hub_ops_cost` theo kỳ. Không đổi trải nghiệm khách — chỉ làm sổ nội bộ minh bạch. Effort ~1 ngày (params + report).

### B4. Thuật toán điều phối theo category (đề xuất chia danh mục) — **[GAP trung bình]**

Hiện 5 tiêu chí scoring (thời gian/tải/quality/cost/logistics) + filter cứng — **chưa có khái niệm chuyên môn ngành hàng** (resin chi tiết nhỏ, FFF khổ lớn, hậu xử lý đặc thù…).
**Đề xuất:**
- Thêm trường **categories[]** vào khai báo năng lực lab (FR-LAB-002 sửa) — danh mục chuẩn do admin quản (FR-ADMIN-003 catalogue mở rộng).
- **BR-ASSIGN-009** (mới): tiêu chí *category-match* vào scoring với trọng số cfg; **không** vào filter cứng trừ grade đặc thù (tránh bỏ đói mạng lưới khi mới vào).
- Thuật toán tự điều phối đã là FR-SCHED-003/004; bổ sung category = thêm 1 tiêu chí, giữ kiến trúc. Effort ~2 ngày; đề xuất **Should**, vào sprint quản trị (tuần 4).

### B5. Voucher — **[QUYẾT ĐỊNH: từng chủ động cắt, nay thầy đề xuất — cần xác nhận]**

Lưu ý lịch sử: nhóm đã **cắt promo-code** (bản vá §8 của 09-FR, quyết định C1) để giữ phạm vi. Nếu thầy muốn có:
**Phương án tối thiểu (Should, tuần 7):** bảng voucher (mã, giảm %/số tiền, hiệu lực, trần dùng toàn chiến dịch, 1 lần/khách); áp dụng **tại bước báo giá**, giá sau giảm được freeze như mọi thành phần giá (BR-QUOTE-004 không đổi tính chất); cấm cộng dồn; hoàn tiền theo BR-PAY-006 hoàn đúng số đã trả sau voucher. Effort ~3 ngày (BE 2 + FE checkout 1). **Không** làm campaign engine.

### B6. Custom riêng — có ý tưởng, không có file 3D — **[QUYẾT ĐỊNH phạm vi]**

Đây là **dịch vụ thiết kế**, một mảng kinh doanh hoàn toàn mới (brief → designer → duyệt file → giá thiết kế). Rủi ro scope-creep cao nhất trong 12 câu.
**Khuyến nghị: không xây trong capstone.** Phương án trung dung (Could, nếu muốn "có mặt" trên hồ sơ): form **"Design Request"** — khách mô tả + ảnh tham khảo → vào hàng đợi ops → lab/designer báo giá thiết kế **tay** qua runbook → file được attach vào draft order như upload thường. Chi phí ~1 ngày chỉ lấy nhu cầu vào hệ thống, phần nghề để ngoài — ghi thẳng vào SRS mục **Out of Scope** kèm lý do (nguồn nhân lực thiết kế không thuộc mạng lab).

---

## C. Nhóm "chính sách — cần thầy và nhóm chốt"

### C1. Deposit toàn bộ đơn hàng (trả theo giai đoạn) — **[QUYẾT ĐỊNH LỚN NHẤT]**

Xung đột trực tiếp với **BR-PAY-001** hiện tại (trả đủ trước khi sản xuất). Phương án khả thi nếu chấp nhận deposit:
- **BR-PAY-007 (mới):** đơn chốt vào sản xuất sau khi **deposit ≥ X%** (X cfg, mặc định 50%) được xác minh (webhook+API như CUST-008); **BR-PAY-008:** phần còn lại phải trả **trước gate đóng gói** tại hub — pack gate hiện hữu (BR-QC-008) thêm điều kiện `PAYMENT = SETTLED`; QR second-leg do hệ thống chủ động gửi kèm notify.
- Hệ lụy phải trả lời đủ bộ (nếu không, thầy sẽ hỏi tiếp): (1) khách bỏ của deposit thì xử sao → **gắn vào nợ B17** (chính sách hoàn khi khách hủy — chưa chốt); (2) **đơn nhỏ** (vài chục nghìn đồng) mà 2 lần thanh toán = friction giết chuyển đổi, xung đột USE-001 (10 phút có đơn); (3) đối soát 2 giao dịch/đơn → NFR-SEC-009 mở rộng.
- **Khuyến nghị của tôi:** deposit chỉ khi đạt **ngưỡng** (`order ≥ 500k ĐOR lead time > 7 ngày`, ngưỡng cfg) — giữ BR-PAY-001 làm mặc định cho đơn nhỏ; trình thầy 2 lựa chọn **deposit-mọi-đơn vs deposit-ngưỡng** kèm 3 hệ lụy trên, đề bài C mới cho biên bản (B18?).
- Effort nếu làm sau SEPay (đang để tuần 7): +2 ngày.

### C2. Xử lý khi khách phản hồi chậm (deadline phía khách) — **[GAP chính sách]**

Hệ đã có trần chờ cho **lab** (2h auto-decline, ASSIGN-005) nhưng **chưa có trần cho khách**. Đề xuất đối xứng:
- **BR-NOTIFY-003:** Date-Change-Request không được khách duyệt trong **48h** → promise gốc **không đổi**; ops escalates; nếu promise gốc bất khả thi → hoàn theo BR-PAY-004 (đứng về phía khách).
- **BR-NOTIFY-004:** yêu cầu khách bổ sung (file sửa, ảnh khiếu nại) — **7 ngày** không trả lời → đóng request trạng thái `STALE`; bảo hành 30 ngày vẫn chạy theo đồng hồ gốc (QC-009).
Effort: 2 rule + 2 job quét hạn (đã có hạ tầng Hangfire). Nhỏ — đưa sprint 3.

---

## Bảng tổng hợp đề xuất đưa vào kế hoạch

| # | Đề xuất | Mã mới/sửa | MoSCoW | ƯL | Tuần đề xuất |
|---|---|---|---|---|---|
| A1 | Split-quantity + auto Date-Change | BR-SCHED-010/011, FR-SCHED-007 AC | Must (đã trong lõi) | +1d | 3 |
| A2/A3 | Reserve/settle + buffer gam | BR-STOCK-001/002, FR-LAB-003 AC | Must | 2d | 2 |
| B1 | Gom đơn tự động cùng khách | FR-HUB-006, BR-LOG-001/002 | Should | 2d | 5 |
| B2 | Transport allowance (nghĩa 2) | BR-PAY-002 sửa | Should | 1d | 6 |
| B3 | Margin hub/lab + report | CONFIG param, FR-ANAL-003 mở | Should | 1d | 6 |
| B4 | Category-aware assignment | LAB spec + BR-ASSIGN-009 | Should | 2d | 4 |
| B5 | Voucher tối thiểu | FR-CUST-012 mới | Should/Could | 3d | 7 |
| B6 | Design Request (chỉ ghi nhận) | form out-of-flow | Could | 1d | — |
| C1 | Deposit (ngưỡng hay mọi đơn) | BR-PAY-007/008 | QUYẾT ĐỊNH | +2d | sau SEPay |
| C2 | Timeout phía khách | BR-NOTIFY-003/004 | Must | 1d | 3 |

**Việc hồ sơ nếu nhóm duyệt:** cập nhật 08-BR (+~12 rule), 09-FR (2 FR mới + 4 AC sửa), 07_Traceability (P-xx nguồn cho các dòng mới — một số là P mới từ biên bản review, cần extract), 12-UC (UC mới cho deposit/voucher/gom đơn nếu chấp nhận), workbook 04 (thêm B18: kết quả review 30/09), và **bảng Excel kế hoạch 1 trang** thêm dòng tuần tương ứng.
