# Phiếu đăng ký v1.1 — BẢN DRAFT NỘI DUNG (chờ GVHD xác nhận B2)

**Cách dùng:** mở `Phieu_FA26SE249.docx`, **lưu thành** `Phieu_FA26SE249_v1.1.docx` (giữ file v1.0 nguyên — quy tắc "không sửa đè lên bản đã duyệt"), rồi dán nội dung các mục dưới vào đúng cấu trúc template của trường — **không đổi thứ tự mục, không thêm/bớt/xóa mục, không đổi tên mục**; hai mục mới (Out of scope, Rủi ro) đưa vào khối "4. Other comments" của template nếu template không có chỗ riêng.

**Ai điền:** nhóm trưởng là người duy nhất sửa phiếu. Kết quả họp GVHD ghi vào các chỗ ⚠ rồi mới chốt.

---

## 0. Record of Changes (đặt đầu phiếu, theo quy ước A/M/D)

| VERSION | DATE | A/M/D | IN CHARGE | CHANGE DESCRIPTION | REFERENCE |
|---------|------|-------|-----------|--------------------|-----------|
| 1.0 | 25/08/2026 | A | GVHD | Bản đăng ký gốc được Nhà trường phê duyệt | Quyết định giao đề tài |
| 1.1 | …/…/2026 | M | Nhóm trưởng | Thu hẹp trụ P-12: bỏ batch consolidation khỏi MVP → Should-have | Biên bản phản biện §4 |
| 1.1 | …/…/2026 | M | Nhóm trưởng | Thu hẹp trụ P-14: MVP chỉ repair khi lab từ chối + hỏng in; breakdown/reprint → Should | Biên bản phản biện §4 |
| 1.1 | …/…/2026 | M | Nhóm trưởng | Thu hẹp trụ P-15: hiệu chuẩn hệ số thủ công qua admin UI; regression tự động → Could | Biên bản phản biện §4 |
| 1.1 | …/…/2026 | M | Nhóm trưởng | Thu hẹp trụ P-17: ledger MVP chỉ ghi nhận & hiển thị; đưa standing vào điểm gán việc → Should | Biên bản phản biện §4 |
| 1.1 | …/…/2026 | M | Nhóm trưởng | Thanh toán: **tiền thật qua gateway tích hợp** (quyết 23/09 — B1 biên bản): chỉ giữ token + txn id (BR-PAY-005), sinh luồng refund/đối soát (FR-ANAL-005/UC-028); carrier: ghi tay vận đơn | Biên bản §4; B1/B2 chốt 23/09 |
| 1.1 | …/…/2026 | A | Nhóm trưởng | Bổ sung mục "Hệ thống hiện có và khoảng trống", "Out of scope kèm lý do", "Rủi ro và dự phòng" | Biên bản phản biện §2, §5 |
| 1.1 | …/…/2026 | A | Nhóm trưởng | Bổ sung 4 FR thiếu so với phiếu gốc: decision log, pricing computation, pack & ship, checklist manager | 07_Traceability (bullet ma P-47/48, P-55, P-62, P-66) |
| 1.1 | …/…/2026 | M | Nhóm trưởng | Viết lại câu hỏi nghiên cứu, giữ phần nghiên cứu | Biên bản §6 |
| 1.1 | …/…/2026 | — | GVHD | ☐ Xác nhận / ☐ Yêu cầu sửa — ký: | |

> Mọi dòng M/A đều trỏ về một quyết định cụ thể — đó là bảng chứng cho hội đồng thấy nhóm đã phản biện chứ không chép lại bản cũ.

## 1. Mục 3.1 — Kế thừa, TUYỆT ĐỐI KHÔNG SỬA

Mã đề tài, tên tiếng Anh *("PrintGrid: Development of a Distributed 3D Printing Fulfillment and Scheduling Platform for a Network of Independent Printing Labs")*, tên tiếng Việt, tên viết tắt **PrintGrid**, GVHD Nguyễn Tấn Phúc, danh sách sinh viên — **chép nguyên từ v1.0**. Không "làm cho khớp phạm vi mới".

## 2. Mục 3.2 a) Context — Giữ nguyên từng dòng, CHỈ chèn thêm

Năm gạch đầu dòng và 7 bullet vấn đề của v1.0: **giữ nguyên hàng và cách chữ**. Chèn thêm ở cuối mục a) ba đoạn sau (dán nguyên):

> **Bằng chứng và điều kiện (bổ sung v1.1):** Các giả định nền chưa có bằng chứng đang được kiểm chứng: (i) tồn tại mạng lab nhỏ chịu vào nền tảng điều phối — điều kiện: biên bản làm việc với lab đối tác trước tuần 8; (ii) khách chấp nhận đặt qua trung gian — điều kiện: khảo sát ≥20 khách tiềm năng; (iii) slicer đầu nguồn mở chạy headless đạt thời gian yêu cầu — điều kiện: pilot 20 file STL thật. Kết quả tại thời điểm nộp được ghi trong `workbook/02_Phan-bien-de-tai.md` §1.
>
> **Hệ thống hiện có và khoảng trống (bổ sung v1.1):** Xometry/Protolabs/Craftcloud làm được quote tự động và routing nhưng là nền tảng đóng, proprietary, hướng job-shop công nghiệp, không có hub QC tập trung + fault attribution làm lõi thiết kế. Cách làm thủ công hiện tại (lab nhận đơn riêng + slicer cục bộ) không điều phối được giữa các lab và hứa ngày bằng bảng lead-time hằng số. Học thuật giải bài scheduling NP-hard nhưng giả định nhà máy duy nhất, máy sở hữu riêng. Khoảng trống PrintGrid: liên kết quote-time scheduling ↔ cam kết khách hàng ↔ repair khi sản xuất thất bại cho **mạng lab độc lập**, có kiểm chứng bằng simulator + 1 lab thật. Tính mới xếp loại *mới về cách làm + ngữ cảnh*.
>
> **Con số bối cảnh cần đo:** số lab tiềm năng trong bán kính phục vụ hub, sản lượng lab đối tác/tháng — cập nhật sau buổi làm việc với lab (câu hỏi #3).

## 3. Mục 3.2 b) Proposed Solutions — Viết lại theo quyết định phản biện

Thay 10 đoạn trụ bằng bản dưới đây. Mỗi trụ đã bị cắt thì **viết phạm vi mới**, không giữ câu cũ:

> 1. **Geometry and Slicing Analysis Service** — như v1.0; MVP giới hạn công nghệ FDM với một engine đầu nguồn mở chạy headless (resin/SLS → Out of scope).
> 2. **Network-Uniform Pricing Engine** — như v1.0, giữ nguyên requirement tham số hoá có version đông onto quote.
> 3. **Capability-Filtered Assignment** — như v1.0; MVP dùng weights cố định cấu hình qua admin, phân tích độ nhạy trọng số dành cho phần evaluation.
> 4. **Deadline-Backward Scheduling** *(thu hẹp)* — internal due date lùi từ deadline khách qua hub transfer/inspection/shipping, placement theo quy tắc dispatching (EDD). **Batch consolidation chuyển sang Should-have** — không cam kết trong MVP vì vượt trần khối lượng (biên bản §3).
> 5. **Speculative Quote-Time Scheduling** — giữ, thực hiện theo lộ trình crude-first: phiên bản 1 ước từ queue remaining time + buffer, nâng cấp sau; có từ iteration đầu theo định hướng GVHD.
> 6. **Event-Driven Rescheduling** *(thu hẹp)* — MVP sửa kế hoạch khi lab từ chối và khi in hỏng; breakdown và reprint-triggered repair ở Should-have; mọi phiên bản giữ dispatching-rule solution làm fallback trong time budget.
> 7. **Estimate Calibration Loop** *(thu hẹp)* — MVP: correction factor theo machine model cập nhật thủ công qua màn admin; regression tự động là Could-have phụ thuộc dữ liệu lab thật.
> 8. **Central Hub Quality Control and Fault Attribution** — như v1.0, không đổi phạm vi.
> 9. **Lab Performance Ledger** *(thu hẹp)* — MVP ghi nhận và hiển thị đầy đủ metric; việc đưa standing vào điểm gán việc là Should-have.
> 10. **Model File Access Control** — như v1.0: signed URL ngắn hạn thu hồi được + access log (dùng năng lực object storage có sẵn, không tự xây DRM).
>
> Ngoài phạm vi (xem mục Out of scope): settlement/trả công giữa nền tảng và lab (quy trình ngoại vi), tích hợp API carrier tự động, phân phối việc công bằng có exploration. *Lưu ý 23/09: thanh toán tiền thật của khách **được đưa vào phạm vi** theo quyết định B1.*

## 4. Mục 3.2 c) Functional Requirements — Đồng bộ với b)

**Đính chính quan trọng:** 4 gạch "decision log" (P-62), "pricing computation" (P-55), "pack, record shipment… hand to carrier" (P-47/48), "manage inspection checklists…" (P-66) **đã có sẵn trong phiếu c) bản gốc**. Lỗ hổng "bullet ma" ở `07_Traceability` là ở chỗ **file 09-FR/SRS chưa đặc tả chúng**, không phải phiếu thiếu — vậy 4 FR mới phải thêm vào **09-FR/SRS**, KHÔNG thêm vào phiếu. Mục c) của phiếu v1.1 chỉ đổi câu chữ cho khớp b) và ⚠:

- *Customer:* "Confirm the order and pay" → "Confirm the order and pay via the integrated payment gateway (**quyết 23/09: tiền thật** — hosted checkout, chỉ token + transaction id được lưu, BR-PAY-005; đơn chỉ commit khi webhook xác nhận)"
- *Pricing and Scheduling Engine:* bullet placement — bỏ "with material and colour consolidation" khỏi câu cam kết, chuyển thành mệnh đề mở rộng; bullet recalibrate → "MVP applies correction factors through the administration interface; automatic regression is extended scope"
- *System Administrator:* bullet "Monitor background job health…" — ⚠ quyết tại họp: giữ trong phạm vi hay hạ thành ràng buộc vận hành
- 5 vai còn lại (Lab Manager, Lab Operator, Hub QC, Hub Fulfillment, Operations Manager): **không sửa gạch nào**

## 5. Mục 3.2 d) Non-Functional Requirements — Bổ sung ngưỡng đã biết, đánh dấu chờ

12 gạch của v1.0 giữ nguyên hàng. Chèn các ngưỡng/điều kiện đo **đã chốt được** (nguồn: 10-NFR + 02 phản biện):

> - Quote responsiveness: preliminary estimate ≤ 5 s; committed quote ≤ 60 s (95%); đo trên staging, mesh ≤ 50 MB, 30 phiên đồng thời.
> - Bounded scheduling computation: hard budget ⚠ [chờ quyết, đề xuất 10 s] cho một lần repair; fallback dispatching luôn khả dụng.
> - Feasibility correctness: 0 job gán cho máy không đủ khả năng; kiểm bằng test suite độc lập với scoring (bộ job chủ định không khả thi).
> - Stability of committed promises: ngày giao chỉ đổi khi ⚠ liệt kê điều kiện tại buổi họp (câu hỏi còn mở).
> - IP protection: link ≤ thời lượng job + grace ⚠ [đề xuất 24 h]; mọi access ghi log; không bulk download.
> - Scalability: ≥ 50 labs, ≥ 300 machines, không đổi kiến trúc; kiểm bằng simulator ở tải đó.
> - Shop-floor usability: ⚠ ngưỡng đo được chưa có — câu hỏi #13 (đề xuất: ≤ 3 tap từ queue tới cập nhật trạng thái, offline-tolerant tới 2 phút).
> - Configurability: đổi hn mức/ngưỡng/weights có hiệu lực không restart dịch vụ, thực hiện qua admin UI trong ≤ 1 phút.

## 6. Mục 3.2 e) Theory & Practical — Giữ + bổ sung nguồn

Không viết lại. Chèn câu cuối mục: *"Nguồn công cụ: slicing engine đầu nguồn mở [tên + phiên bản chốt ở pilot — ⚠]; dispatching rules EDD/ATC theo tài liệu scheduling chuẩn [ghi source khi viết Report 1]. Phần thực hành giữ nguyên cam kết: simulator + 3 baseline + trial 1 lab thật."*

## 7. Mục f) Products — Đồng bộ cắt/thêm

- Hub Console: đổi mô tả "packing and shipment tracking" → *"packing and manual shipment recording"*.
- Assignment and Scheduling Engine: **bỏ** cụm "with consolidation" khỏi MVP; ghi "(consolidation: extended)".
- Simulation Harness + Algorithm Evaluation Report: giữ, **đánh dấu built-from-week-4** (định hướng GVHD mục 4).
- Không sản phẩm nào bị xóa toàn phần (không trụ nào bị Loại bỏ — khớp biên bản §4).

## 8. Mục g) Proposed Tasks — Sửa 2 điểm

- WP5: ghi rõ *"simulation harness delivered by end of week 4 and used as the integration testbed thereafter"*.
- WP1: thêm dòng *"secure and document the partner-lab MOU before week 8; fallback: in-house mini-lab trial"* — đây là điều kiện giải quyết rủi ro đỏ nhất (biên bản §5).
- 5 WP giữ nguyên phân công — không thay đổi thành viên → không phải thay đổi lớn, không phải báo Khoa.

## 9. Mục mới (đặt cạnh 4. Other comments) — Out of scope kèm 5 loại lý do

| Không làm | Loại lý do | Diễn giải |
|-----------|-------------|-----------|
| Settlement/trả công giữa nền tảng và lab | Phụ thuộc bên thứ ba / ngoài mục tiêu | Cần tài khoản kho bạc + quy trình kế toán của trường; hệ thống chỉ ghi nhận số phải trả theo công thức (BR-PAY-002/003 runbook), thanh toán thật của khách **ở trong phạm vi** (quyết 23/09) |
| Tích hợp API carrier (GHN/DHL…) | Phụ thuộc bên thứ ba | Không có sandbox sớm; fulfillment ghi tay vận đơn |
| Công nghệ in khác FDM (resin, SLS, metal) | Vượt nguồn lực | Ma trận capability × công nghệ nhân đôi scope filter; FDM đủ chứng minh cơ chế |
| Batch consolidation, standing-in-score, auto-calibration regression, improvement search trong sản phẩm | Pha sau (đã có chỗ trong thiết kế) | Dữ liệu và hook đã nằm trong schema; bật sau khi MVP chạy — đúng danh sách Extended của mục 4 phiếu gốc |
| Fair-distribution exploration | Pha sau + chưa thiết kế xong | Cần mô phỏng dài để chỉnh tham số exploration; để ở Could |
| App di động riêng cho lab/khách | Vượt nguồn lực | Web responsive đủ cho tablet tại máy in (NFR shop-floor) |
## 10. Mục mới — Rủi ro chính (chép từ biên bản §5, 5 dòng đầu)

Lab đối tác chưa ký (MOU trước tuần 8; fallback mini-lab nội bộ) · slicer headless chậm (pilot 20 file trước sprint 1; pre-slice cache) · năng lực scheduling của nhóm (dispatching làm spine, metaheuristic chỉ chạy offline) · IP dữ liệu khách (signed URL + access log) · integration ăn phần còn lại của kỳ (simulator thành testbed từ tuần 4).

## 11. Phần nghiên cứu (nếu template có mục Research)

> Câu hỏi: *"Capacity-aware quoting and scheduling giảm weighted tardiness và tăng tỷ lệ giao đúng hẹn như thế nào so với dispatching-rule và nearest-available-lab baseline, dưới ràng buộc báo giá thời gian thực?"*
> Phương pháp: simulator (load + fault injection) + 3 baseline + số liệu calibration từ 1 lab thật. Không cam kết thuật toán mới; cam kết **cơ chế và số đo**.

---

## Checklist trước khi nộp v1.1 (tự kiểm B3)

- ☐ Mã/tên đề tài/tên viết tắt GVHD/thành viên chép nguyên v1.0
- ☐ Context giữ hàng, chỉ chèn; giả định chưa kiểm chứng viết dạng điều kiện
- ☐ Mục b) chỉ còn trụ Giữ + Thu hẹp, viết ở phạm vi mới
- ☐ c), d), f), g) đồng bộ với b); không còn sản phẩm của trụ bị loại
- ☐ Đã thêm hệ thống hiện có + khoảng trống, Out of scope có loại lý do, Rủi ro
- ☐ Record of Changes đủ, mọi dòng trỏ về một quyết định biên bản
- ☐ File v1.0 vẫn còn nguyên; cấu trúc template không đổi
- ☐ ⚠ đã được thay bằng quyết định của thầy Phúc sau buổi họp (đặc biệt: thanh toán #1, carrier #2, lab #3, các ngưỡng #4–#6/#12/#13)
- ☐ Không có thay đổi lớn (tên/thành viên/hướng) → không cần báo Khoa; nếu phát sinh khi họp → tách riêng cho GVHD báo Khoa, **không tự sửa vào phiếu**
