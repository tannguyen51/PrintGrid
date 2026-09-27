# 02_Phan-bien — Biên bản phản biện đề tài (BẢN NHÁP — làm việc với GVHD)

**Áp dụng Bước 2.** Đây là bản nháp nhóm tự soi để mang vào buổi họp với thầy Nguyễn Tấn Phúc,
**không phải kết luận**. Mọi quyết định ở đây chỉ có hiệu lực khi GVHD xác nhận (Bước 3).

---

## 1. Giả định nền và mức bằng chứng

| Giả định nền | Đứng sau | Mức bằng chứng | Xử lý |
|--------------|----------|----------------|-------|
| Các lab nhỏ tồn tại thật, có máy nhàn rỗi và chịu vào một mạng lưới điều phối | P-01 | **Chưa có** — mới nghe từ bối cảnh ngành | Đàm phán với lab đối tác trước tuần 8 (điều kiện của mục 4, P-104); nếu không được → hạ quy mô claim: 1 lab thật + simulator |
| Khách chấp nhận trả cho nền tảng trung gian thay vì lab quen | P-01, P-04 | **Chưa có** | Khảo sát nhanh 20–30 khách tiềm năng (maker community, đồ án SV); nếu không → giá trị nằm ở tiết kiệm thời gian báo giá, phải nói lại positioning |
| Lab khai báo năng lực trung thực ở mức dùng được | P-07, P-29, P-30 | **Chưa có** — chính phiếu cũng nhận diện incentive nói dối | Ledger declared-vs-delivered (P-17) chính là cơ chế tự sửa; chấp nhận được vì có cơ chế, không cần bằng chứng |
| Slicer đầu nguồn mở ước lượng đủ chính xác để lịch trình khả thi | P-05, A1 | **Tổng hợp** — tài liệu slicer + kinh nghiệm nhóm | Pilot 20 file thật đo thời gian slice + sai số; nếu slice > NFR → pre-slice cache hoặc async pipeline (đã có P-69) |
| 5 người làm được song song: engine thuật toán + 4 portal + simulator trong 1 kỳ | P-83→94 | **Phỏng đoán** | Xem thẩm định khả thi §3 — đây là giả định **nghiêm trọng nhất** |
| Data mesh từ khách phần lớn hợp lệ | A6 | **Chưa có** | Thu 50 STL thực tế từ lab đối tác, chạy validation thống kê tỷ lệ fail |

## 2. Thẩm định tính mới — so sánh hệ thống hiện có

| Hệ thống hiện có | Làm được gì | Không làm được gì (khoảng trống của PrintGrid) |
|-------------------|-------------|--------------------------------------------------|
| **Xometry, Protolabs, 3D Hubs/Craftcloud** (marketplace thương mại) | Quote tự động, routing tới supplier network | Đóng, proprietary; không công khai cơ chế; hướng tới job shop công nghiệp, không phải mạng lab nhỏ phân tán; không có hub QC tập trung + fault attribution làm lõi thiết kế; không đo được để nghiên cứu |
| **PrusaSlicer/CuraEngine + trang order riêng từng lab** (cách làm thủ công hiện tại) | Slice chính xác, nhận đơn trực tiếp | Không điều phối giữa các lab; lead-time bảng hằng số; không có promised-date từ capacity thật; không có lịch trình khi máy đang bận |
| **Bài toán trong học thuật** (CTPOP, scheduling not-related parallel machines) | Lý thuyết NP-hard, dispatching rules, lot-sizing | Hầu hết giả định single factory, owned machines; ít mô hình kết hợp **speculative quoting trướcCam kết + hub QC + self-reporting reputation** cho mạng lab độc lập |

**Phân loại:** **Miểu mới về cách làm + ngữ cảnh** — không mới về bài toán scheduling (NP-hard kinh điển). Giá trị thật nằm ở **liên kết quote-time scheduling ↔ cam kết khách hàng ↔ repair khi sản xuất thất bại** và kiểm chứng qua simulator + 1 lab thật.
→ Kết luận cho Report/slide: **không tuyên bố "thuật toán mới"**, tuyên bố "kiến trúc hệ thống + cơ chế cam kết date từ năng lực mạng, được đo lường". Hội đồng sẽ chấp nhận nếu phần evaluation (P-82) chứng minh được điều đó.

## 3. Thẩm định khả thi — trần khối lượng

```
Trần khối lượng (quy tắc trừ 30% tích hợp & tài liệu):
5 thành viên × ~24 tuần (09/2026–03/2027) × 2,5 ngày thực/tuần × 0,7 = ~210 ngày công
```

| # | Trụ giải pháp | Thô ước (S/M/L, ngày công) | Ghi chú |
|---|----------------|--------------------------|---------|
| 1 | Geometry & Slicing (P-09) | **L** (≈20) | Headless slicer + profile mapping + async pipeline; rủi ro kỹ thuật cao nhất |
| 2 | Pricing Engine (P-10) | M (≈8) | Công thức tham số + version freeze |
| 3 | Capability Filter + Scoring (P-11) | M (≈12) | Filter kiểm được độc lập; scoring cấu hình weights |
| 4 | Backward Scheduling + Consolidation (P-12) | **L** (≈20) | Chuỗi lùi deadline + batch bound |
| 5 | Speculative Quote-Time (P-13) | M (≈10) | Crude version trước (P-101 yêu cầu iteration 1) |
| 6 | Event-Driven Rescheduling (P-14) | **L** (≈18) | Chỉ sửa phần chưa chạy + fallback |
| 7 | Calibration Loop (P-15) | M (≈8) | Phụ thuộc data lab thật |
| 8 | Hub QC + Attribution (P-16) | M (≈12) | Checklist engine + defect taxonomy |
| 9 | Performance Ledger (P-17) | M (≈8) | Đếm metric dễ; đưa vào score là phần sau |
| 10 | Model Access Control (P-18) | S (≈3) | MinIO signed URLs — gần như miễn phí |
| — | 4 portal + auth + domain backend + simulator + tài liệu | **≈110–130** | Customer/Lab/Hub/Ops web; simulator là deliverable (P-103) |
| **Tổng** | | **≈250–270** | **Vượt trần ~20–30%** |

## 4. Quyết định cho từng trụ (đề xuất — chờ GVHD chốt)

| Trụ | Quyết định đề xuất | Lý do |
|-----|--------------------|-------|
| P-09 Slicing | **Giữ** — MVP giới hạn FDM + 1 slicer engine (A7) | Không bỏ được: là đầu vào của mọi thứ; thu hẹp bằng cách bỏ resin/SLS |
| P-10 Pricing | **Giữ** | Rẻ, là điều kiện cam kết giá |
| P-11 Filter + Score | **Giữ** | Feasibility filter = correctness (P-71); scoring giữ nhưng weights cố định ở MVP |
| P-12 Scheduling + consolidation | **Thu hẹp** | MVP: backward due-date + EDD dispatching; **bỏ batch consolidation** → Should-have (batch bound phức tạp, giá trị demo thấp hơn rủi ro) |
| P-13 Speculative quoting | **Giữ, crude-first** | Bắt buộc bởi P-101; phiên bản 1 = queue remaining time + buffer, nâng cấp sau |
| P-14 Rescheduling | **Giữ, thu hẹp phạm vi event** | MVP: repair khi lab decline + print fail; breakdown/reprint-triggered repair → Should |
| P-15 Calibration | **Thu hẹp** | MVP: correction factor hằng số theo machine model cập nhật thủ công qua admin UI; tự động regression → Could (phụ thuộc A2+A3) |
| P-16 Hub QC | **Giữ** | Là lời hứa bảo chứng chất lượng (P-08) |
| P-17 Ledger | **Thu hẹp** | MVP chỉ **ghi nhận & hiển thị** metric; đưa standing vào assignment score → Should (đúng như mục 4 xếp extended) |
| P-18 Access control | **Giữ nguyên** (signed URL + log) | Rẻ và là differentiator IP |
| Fair distribution (P-80) | **Loại khỏi MVP** | Mục 4 đã xếp extended → Could |
| Improvement search (SA/tabu) | **Giữ trong evaluator, không bắt buộc trong product path** | Dispatching rule đủ chạy; metaheuristic chạy offline cho evaluation report |
| Payment thật (P-25) | **QUYẾT 23/09: GIỮ TIỀN THẬT** | Khách trả qua gateway tích hợp thật (VNPay/MoMo); dữ liệu thẻ không chạm hệ thống — chỉ token (BR-PAY-005 mới); refund flow được đặc tả (FR-ANAL-005/UC-028, BR-PAY-004 hết là rule chết). Settlement trả công cho lab **vẫn ngoài hệ thống** (runbook). Chêch +8–10 ngày công so phương án sandbox — bù bằng các cắt ở các dòng trên, vẫn trong trần |
| Carrier integration (P-47) | **Loại** | Out of scope — ghi tay shipment number; lý do: phụ thuộc bên thứ ba |

**Nếu các quyết định trên được duyệt:** ≈ 250−(consolidation 10 + auto-calibration 6 + ledger-in-score 5 + payment 6 + carrier 5 + improvement-search-in-product 8) ≈ **205–215 ngày công** → vừa trần. Phải giữ nguyên, không được "làm thêm cho vui".

## 5. Rủi ro (5 loại) — dấu hiệu sớm & dự phòng

| Loại | Rủi ro | Dấu hiệu sớm | Dự phòng |
|------|--------|--------------|----------|
| **Bên thứ ba** | Lab đối tác không ký được hoặc không cho data (P-104) | Tuần 6 chưa có MOU | Phương án B: tự dựng "lab sân sau" — 1–2 máy của nhóm; trial nhỏ vẫn đo được calibration; nêu rõ giới hạn trong Report |
| **Dữ liệu** | Slicer headless chậm/không chạy ổn định (A1) | Pilot 20 file fail > 20% hoặc slice > 2 phút | Pre-slice cache theo mesh hash; async-first UX (đã có P-69); báo GVHD đổi engine |
| **Năng lực nhóm** | Chưa ai có kinh nghiệm scheduling metaheuristic + computational geometry | 2 sprint đầu engine không ra kết quả khả thi | WP2 bám dispatching rules làm spine; metaheuristics chỉ chạy trong evaluation; pairing + code review sớm |
| **Pháp lý / đạo đức** | Thanh toán tiền thật (QUYẾT 23/09: làm thật), dữ liệu thiết kế khách (IP) | Phí gateway bị chặn/tài khoản nhà cung cấp chậm kích hoạt; webhook lỗi làm đơn "treo" | Chỉ giữ token, card data không chạm hệ thống (BR-PAY-005); sandbox của gateway cho dev, prod bật sau khi có tài khoản KT của trường/đối tác; đối soát ngày (NFR-SEC-009); access log IP đầy đủ (P-75) |
| **Phạm vi** | 4 portal + engine = trải đều 5 người nhưng **integration** ăn phần còn lại | Sprint 3–4: E2E flow chưa thông | Simulator (P-103) dựng sớm từ tuần 4 làm testbed tích hợp — biến công cụ thành tấm khiên phạm vi |

## 6. Kết luận về phần nghiên cứu

Ba câu đầu "có": phiếu có mục e) Theory & Practical đầy đủ; hội đồng đánh giá cao phần evaluation; nhóm tiếp cận được data (simulator + 1 lab). Câu thứ tư (khối lượng nghiên cứu trong trần): **vừa** nếu cải tiến metaheuristic chỉ chạy offline.

→ **Giữ phần nghiên cứu.** Câu hỏi nghiên cứu đề xuất phát biểu lại cho gọn:

> *"Việc suy lịch trình trên năng lực mạng thực (capacity-aware quoting) giảm weighted tardiness và tăng tỷ lệ giao đúng hẹn như thế nào so với dispatching-rule và nearest-available-lab baseline, dưới ràng buộc báo giá thời gian thực?"*

Chứng minh bằng: simulator với fault injection + 3 baseline + số liệu calibration từ 1 lab thật (P-82, P-103, P-104). Không cam kết "thuật toán mới" (đã phân loại ở §2).

## 7. việc cần làm tiếp (chuyển sang Bước 3)

- [ ] Họp GVHD trình §1–§6; chốt từng quyết định §4 bằng chữ ký biên bản
- [x] Chốt 15 câu hỏi chưa rõ của `01_Extract-P-xx.md` — ✅ #1 TIỀN THẬT · ✅ #3 ĐÃ CÓ LAB (23/09) · ✅ #4 repair 30s/15s · ✅ #5 (B16) danh mục 3 điều kiện đổi date · ✅ #6 reprint limit 2; còn mở: #2 carrier, #7–#12, #13, #15
- [ ] Sau họp: lập `Phieu_FA26SE249_v1.1` + Record of Changes (theo mẫu Phụ lục B Bước 3: dòng nào cũng dẫn về một mục § trên)
- [ ] Tách riêng mọi thay đổi vượt phạm vi điều chỉnh (VD: đổi tên đề tài) để GVHD báo Khoa — hiện chưa có
