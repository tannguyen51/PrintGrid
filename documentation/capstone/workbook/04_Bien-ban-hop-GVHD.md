# BIÊN BẢN HỌP GVHD — Checklist quyết phạm vi (B2 → B3)

**Đề tài:** PrintGrid (FA26SE249) · **GVHD:** Thầy Nguyễn Tấn Phúc · **Ngày họp:** ___/___/2026
**Thành phần:** ______________________________  **Thư ký:** Nhóm trưởng

**Cách dùng:** mỗi dòng có **Đề xuất của nhóm** — thầy ☑ Đồng ý hoặc ✍ Sửa trực tiếp vào cột "Kết luận".
Kết luận nào cũng phải ghi được vào 1 dòng Record of Changes phiếu v1.1. bringing: file này + `02_Phan-bien-de-tai.md` + phiếu v1.0.

---

## PHẦN I — QUYẾT PHẠM VI 13 TRỤ (từ `02_Phan-bien` §4 — cần nhất, quyết hết phần này là mở được B3)

| # | Trụ | Đề xuất | ☑/✍ | Kết luận |
|---|-----|---------|-----|----------|
| A1 | Slicing/Geometry | **Giữ**, giới hạn FDM + 1 engine mã nguồn mở | | |
| A2 | Pricing Engine | **Giữ** nguyên cam kết version-freeze | | |
| A3 | Capability filter + scoring | **Giữ**; weights MVP **cố định**, chưa cần UI chỉnh | | |
| A4 | Backward scheduling | **Giữ EDD; BỎ batch consolidation** khỏi cam kết MVP → Should (tiết ~10 ngày công, vượt trần 20–30%) | | |
| A5 | Speculative quote-time | **Giữ, crude-first**: v1 = queue remaining time + buffer (đúng định hướng "iteration 1" của thầy) | | |
| A6 | Event rescheduling | **Giữ; MVP chỉ repair khi lab từ chối + in hỏng**; breakdown/reprint → Should | | |
| A7 | Calibration | **Thu hẹp**: MVP nhập hệ số hiệu chỉnh thủ công qua màn admin; regression tự động → Could | | |
| A8 | Hub QC + fault attribution | **Giữ** nguyên | | |
| A9 | Performance ledger | **Thu hẹp**: MVP chỉ ghi + hiển thị; đưa standing vào điểm gán việc → Should | | |
| A10 | IP access control | **Giữ** (signed URL + log — rẻ, gần như sẵn có) | | |
| A11 | Fair-distribution exploration | **Loại khỏi MVP** (Could; đúng danh sách Extended của thầy) | | |
| A12 | Metaheuristic (annealing/tabu) | **Chỉ chạy offline phục vụ evaluation**, không vào đường sản phẩm | | |
| A13 | Payment | **Sandbox, chỉ ghi trạng thái** — xem B1 | | |

**Hệ quả:** ≈ 250−40 = **~205–215 ngày công ≈ trần 210** (5 người × 24 tuần × 2,5 ngày × 0,7). Cắt thêm hay giữ gì cứ so con số này.

## PHẦN II — 15 CÂU HỎI CHÍNH SÁCH (từ `01_Extract` — điền số/nghĩa vụ, không cần thảo luận dài)

| # | Câu hỏi | Đề xuất của nhóm | Kết luận |
|---|---------|------------------|----------|
| B1 | Tiền thật qua hệ thống? | Không. Sandbox (VNPay/MoMo test). Hoàn tiền = quy trình ops ghi nhận, **BR-PAY-004 hạ thành quy trình ngoại vi** | ✍ **CHỐT 23/09: TIỀN THẬT** — khách trả qua gateway thật; card data không chạm hệ thống (BR-PAY-005); refund có FR-ANAL-005 + UC-028; settlement lab vẫn ngoài hệ thống |
| B2 | Tích hợp API hãng vận chuyển? | Không. Nhập tay số vận đơn → ghi Out-of-scope loại "phụ thuộc bên thứ ba" | |
| B3 | **Lab đối tác — ai giới thiệu, deadline nào?** | Ký trước **tuần 8**; fallback: mini-lab nội bộ 1–2 máy. Đây là phụ thuộc rủi ro ĐỎ nhất | ✅ **CHỐT 23/09: ĐÃ CÓ LAB** — điền tên + ngày MOU + phạm vi dữ liệu được phép dùng: ____________ |
| B4 | Trần thời gian tính lịch? | Quote ≤ 60s, repair ≤ 30s (trung bình 15s) — chốt để N-02 đo được | ✅ **CHỐT 23/09: theo đề xuất** — repair ≤30s hard / TB ≤15s; quote ≤60s |
| B5 | Lab không trả lời trong window thì sao? | 2h = **tự động từ chối + gán lại** (không auto-accept); lab nào có lịch nghỉ đã khai thì được trừ | |
| B6 | Kiểm hàng ở hub: 100% hay kiểm theo mẫu? | 100% (sản lượng MVP nhỏ, và đây là lời hứa bảo chứng của đề tài) | |
| B7 | Preliminary quote ước từ gì? | Volume × tốc độ in theo loại máy + hằng số; sai số ±40% cảnh báo cho khách | |
| B8 | Lab có thấy giá nội bộ của mình? | Có, cho chính nó; không thấy của lab khác. **BR-PAY-002/003 settlement: tính ngoài hệ thống** (file công thức), hệ thống chỉ ghi đối soát | |
| B9 | Khách được thấy gì khi tracking? | Trạng thái thô: Đã nhận đơn → Đang sản xuất → Tại hub → Đang giao → Giao. Không lộ lab, không lộ lịch máy | |
| B10 | Lab mới cold-start standing = ? | Trung tính 0.70 + cap 5 job đồng thời cho tới khi đủ 10 đơn (đã có BR-PERF-002/005) | |
| B11 | Ops override có được đặt job lên máy KHÔNG đủ khả năng? | Không được bypass bộ lọc cứng (đúng tinh thần P-71 "correctness"); chỉ override trong tập khả thi | |
| B12 | Báo giá hết hạn bao lâu, gia hạn được không? | 48h, không gia hạn — khách bấm lại lấy quote mới | |
| B13 | Chuẩn đo "dùng được ở xưởng"? | ≤ 3 chạm từ hàng đợi → đổi trạng thái; chịu mất mạng 2 phút không mất dữ liệu đã nhập (N-11) | |
| B14 | Trần in lại mỗi đơn? | 2 lần; lần 3 chặn tự động, đẩy quản lý vận hành xử | ✅ **CHỐT 23/09: theo đề xuất** — 2 lần, BR-RESCHED-004 cfg = 2 |
| B15 | File link còn sống bao lâu sau xong việc? | job xong + 4h (đã ghi BR-ACCESS-001), reassign = thu hồi tức thì | |
| B16 | Điều kiện ĐƯỢC ĐỔI ngày giao đã hứa? *(điền để NFR-REL-006 có danh sách kiểm)* | Đề xuất 3: (1) khách xin dời · (2) BẤT KHẢ KHÁNG (cháy điện/thiên tai/lab sập mạng toàn phần) · (3) in lại ≥2 lần không kịp lịch. Mọi trường hợp: báo + xin khách trước | ✅ **CHỐT 23/09: theo đề xuất** — đúng 3 điều kiện, kèm notify + approval; NFR-REL-006 hết ⚠ |
| B17 | Hai lỗ hổng chính sách refund phát hiện khi soạn UC-028: (a) **khách tự hủy đơn đã trả tiền trước khi vào sản xuất** — hoàn 100% hay trừ phí xử lý? (b) **lỗi thanh toán trùng** (double-charge từ phía khách/gateway) — đối soát T-1 bắt được thì hoàn tự động hay cần ops duyệt? | (a) Đề xuất: hủy TRƯỚC trạng thái "đã gán lab nhận việc" = hoàn 100%; sau đó = xử như thay đổi phía khách, chỉ hoàn phần item chưa in. (b) Đề xuất: lệch do gateway sinh 2 txn cho 1 đơn → hoàn TỰ ĐỘNG txn thừa vì idempotency key đã chứng minh lỗi ở phía cổng, ops chỉ xem nhật ký | |

## PHẦN III — XÁC NHẬN MỨC/HỒ SƠ (5 phút cuối)

| # | Việc | Đề xuất | Kết luận |
|---|------|---------|----------|
| C1 | **Must = 31/42 FR (74%)** — vượt chuẩn 60% của tài liệu hướng dẫn | Xin thầy xác nhận là **ngoại lệ có chủ đích** vì lấy đúng MVS thầy đã viết ở mục 4 phiếu; thứ tự cắt khi trễ: LAB-001..003 seed tay → CUST-003 viewer → SCHED-009 rút gọn | |
| C2 | 4 "rule chết" BR-PERF-006 / BR-PAY-002/003/004 | Chuyển thành **quy trình ngoại vi ghi trong runbook**, khỏi sinh FR (đỡ ~4 ngày công); riêng appeal lab (PERF-006) có thể làm dạng email-manual | |
| C3 | P-67 — màn hình theo dõi job nền của admin | **Hạ thành runbook vận hành** (dùng dashboard Hangfire dựng sẵn), tiết 3–5 ngày công cho engine | |
| C4 | "Apply promo codes" trong FR-CUST-007 | **Cắt** — không có nguồn trong phiếu, là feature tự thêm | |
| C5 | Câu hỏi nghiên cứu: *"Capacity-aware quoting & scheduling giảm weighted tardiness và tăng tỷ lệ đúng hẹn thế nào so với dispatching-rule và nearest-available-lab baseline, dưới ràng buộc báo giá thời gian thực?"* — chứng minh bằng simulator + 1 lab thật, không cam kết thuật toán mới | ☑/✍ | |
| C6 | Mẫu kỳ này có mục 3.3 Research Information không? Có thì dời C5 vào 3.3 | Hỏi thầy | |
| C7 | Thay đổi lớn (tên/hướng/người) phát sinh tại họp? | Nếu CÓ: tách riêng để thầy báo Khoa — nhóm không tự sửa | |

## PHẦN IV — CAM KẾT SAU HỌP (thư ký điền hạn)

- [ ] Biên bản này được thầy xác nhận → nhóm trưởng dán kết luận vào Record of Changes, xuất phiếu **v1.1 sạch** (gỡ hết ⚠) — hạn: ___
- [ ] Điền B4/B12/B13/B14/B16 vào `10-NFR` §0 (N-02, N-05, N-10, N-11) + `08-BR` (rule tương ứng) — hạn: ___
- [ ] Chốt deadline lab đối tác (B3) vào lịch trình + WP1 Report 2 — hạn: ___
- [ ] B9 (bảng chuyển trạng thái Job/Order/Quote) + Conceptual ERD hoàn tất trước khi đăng ký lịch Review 1 — hạn: ___

**Chữ ký:**  GVHD ______________________   Nhóm trưởng ______________________
