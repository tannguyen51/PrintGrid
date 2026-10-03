# 01_Extract — Bảng trích xuất Phiếu đăng ký v1.0 sang P-xx

**Áp dụng Bước 1** của tài liệu "Từ Phiếu đăng ký đến Đặc tả Yêu cầu v1.2".
Nguồn: `Phieu_FA26SE249-extracted.md` — mục 3.2 a)→g) và mục 4. Quy tắc: mỗi gạch đầu dòng đúng
thành một dòng P-xx, không ghép hai ý, không sửa câu cho hay hơn. Cột "Chưa rõ" là danh sách
câu hỏi mang đi họp.

**Loại dự kiến:** VĐ = vấn đề · TR = trụ giải pháp · YCF = yêu cầu chức năng thô · RC = ràng buộc / quy tắc · SP = sản phẩm · WP = gói công việc · DH = định hướng GVHD

## a) Context

| Mã | Trích từ phiếu | Loại | Suy ra được gì | Chưa rõ |
|----|----------------|------|----------------|---------|
| P-01 | Nhu cầu in 3D tăng, năng lực nằm rải rác nhiều lab nhỏ; khách đối mặt giá/chất lượng/lead time khác nhau ở mỗi cửa; mạng lưới chạy kém: một lab từ chối việc trong khi lab khác chờ không | VĐ | Bài toán là **điều phối** giữa các lab, không phải storefront; cần một nền tảng thống nhất giá/chất lượng/thời hạn | Khách hàng mục tiêu là cá nhân hay B2B? Có bao nhiêu lab sẵn sàng tham gia? |
| P-02 | Năng lực không phải một con số: khả năng nhận việc là hội của các ràng buộc cứng (build volume, dung sai, vật liệu & màu tồn kho) | VĐ | Bộ lọc khả thi (feasibility filter) phải kiểm tra **hội điều kiện**, không suy từ tải | Danh sách ràng buộc cứng đầy đủ là gì? Dung sai đo bằng gì? |
| P-03 | Ngày giao phải được hứa **trước khi** biết máy nào in; bảng lead-time cố định hoặc quá rộng mất đơn, hoặc hứa hổng | VĐ | Phải scheduling suy đoán (speculative placement) ngay tại lúc báo giá — đây là lõi của đề tài | Speculative chạy đồng bộ hay bất đồng bộ? Bao lâu là chấp nhận được? |
| P-04 | Một giá phải phủ chi phí khác biệt giữa các lab (heterogeneous cost base): cùng chi tiết tốn khác nhau ở mỗi lab | VĐ | Tách **giá bán** khỏi **chi phí nội bộ lab**; chênh lệch hấp thụ ở cấp mạng lưới | Cơ chế hấp thụ lỗ/lãi? Có chia tiền lại cho lab theo giá nội bộ không? |
| P-05 | Ước lượng thời gian in từ slicer lệch theo máy (tuổi, hiệu chuẩn, tay nghề) | VĐ | Sinh vòng lặp calibration (khối KQ ước lượng vs thực tế theo model máy) | Lấy dữ liệu thực tế từ đâu? Bao nhiêu job/máy mới đủ hiệu chuẩn? |
| P-06 | Hỏng in là chuyện thường quy (fail ở 80% thời lượng, chết giữa queue, pass ở lab nhưng fail ở hub) | VĐ | Repair/replan là chế độ vận hành chuẩn, không phải lỗi; state machine phải thiết kế nhánh fail từ đầu | Tỷ lệ fail thực tế bao nhiêu? Ai chịu chi phí in lại? |
| P-07 | Lab tự khai khả năng & công suất; động cơ của lab là nhận nhiều việc nhất có thể | VĐ | Sinh thực thể **Lab Performance Ledger** + đối chiếu declared vs delivered; standing là input của scoring | Ngưỡng nào thì bị cap/suspend? Ai quyết? Có cho lab khiếu nại scoring không? |
| P-08 | Nền tảng mang bảo chứng chất lượng mà không trực tiếp sản xuất; khách không biết lab nào in | VĐ | QC + fault attribution phải **tập trung tại hub**; chuẩn chất lượng không giao cho lab tự xử | Kiểm gì ở hub, kiểm theo mẫu hay 100%? Quy trình đổi trả với khách cuối? |

## b) Proposed Solutions (10 trụ)

| Mã | Trụ giải pháp | Loại | Suy ra được gì | Chưa rõ |
|----|--------------|------|----------------|---------|
| P-09 | Geometry & Slicing Analysis Service (STL/OBJ/3MF → bbox, volume, mesh integrity, thời gian in, vật liệu chính + support) | TR | Phân hệ/engine riêng; **cần engine mã nguồn mở**; sinh validation feedback cho khách | Dùng slicer mã nguồn mở nào? Chạy CPU bao lâu cho mesh 50MB? |
| P-10 | Network-Uniform Pricing Engine — một công thức tham số hoá, **versioned & frozen onto quote** | TR | Sinh thực thể PricingParameterSet + báo giá phải tái lập được sau khi đổi giá | Công thức thành phần? Biên lợi nhuận đặt ở đâu? |
| P-11 | Capability-Filtered Assignment — filter cứng rồi score đa tiêu chí (deadline slack, capacity, quality history, internal cost, transfer time) | TR | Hai bước lọc/chấm điểm tách rời, independently testable; weights cấu hình được | Bao nhiêu tiêu chí ở MVP? Trọng số mặc định? |
| P-12 | Deadline-Backward Scheduling with Batch Consolidation — internal due date lùi từ deadline khách qua hub transfer/inspection/shipping; gộp batch cùng vật liệu+màu; batch bound bảo vệ việc khẩn | TR | Sinh chuỗi: customer deadline → hub ship-by → hub QC by → lab due date; batch bound là rule | Batch bound cụ thể (VD: không đẩy job khẩn quá X giờ)? |
| P-13 | Speculative Quote-Time Scheduling — trial placement tại quote time → ngày giao sớm khả thi + priority surcharge | TR | Quote = kết quả scheduling, **không phải hằng số**; mục 4 của phiếu yêu cầu làm từ iteration 1 | Crude form chấp nhận được là gì (queue remaining time)? |
| P-14 | Event-Driven Rescheduling — chỉ sửa phần chưa chạy, bounded time budget, giữ dispatching-rule solution làm fallback | TR | Sinh actor System (Rescheduler); rule: job đang in không bị dịch | Time budget bao nhiêu giây? Alarm khi repair fail? |
| P-15 | Estimate Calibration Loop — actual vs estimate theo machine model → correction factors ngược vào quoting+scheduling | TR | Sinh dữ liệu calibration; phụ thuộc lab thật (mục 4) | Bao lâu chạy lại? Factor theo machine model hay từng máy? |
| P-16 | Central Hub QC & Fault Attribution — checklist theo quality grade, phân loại lỗi, quy lỗi lab/hub/geometry-khách, tự tạo reprint job kế thừa deadline | TR | Sinh Checklist + DefectTaxonomy + fault attribution; reprint priority | Checklist do ai duyệt? Fail do geometry khách thì xử sao? |
| P-17 | Lab Performance Ledger — on-time, first-pass yield, acceptance rate, utilisation, declared-vs-delivered gap → standing | TR | Audit group + báo cáo; standing vào score (điều kiện cần cho P-07) | Cold start cho lab mới? Công khai cho lab khác thấy không? |
| P-18 | Model File Access Control — short-lived revocable links, log mọi access, không bulk download | TR | Constraints về IP (mục d)); thiết kế MinIO signed URL | Thời hạn link = duration job + bao nhiêu grace? |

## c) Functional Requirements thô (60 gạch, theo 8 vai)

### Customer (P-19 → P-28)

| Mã | YCF thô | Loại | Chưa rõ |
|----|---------|------|---------|
| P-19 | Register, sign in, quản lý tài khoản & địa chỉ giao hàng | YCF | SSO/social login? |
| P-20 | Upload STL/OBJ/3MF + xem 3D trong browser với bbox/dimension readout | YCF | Giới hạn dung lượng file? |
| P-21 | Nhận phản hồi validation mesh & chi tiết vượt build volume lớn nhất mạng | YCF | Từ chối luôn hay cho scale? |
| P-22 | Khai per-item config: material, colour, layer height, infill, post-processing, quantity | YCF |Ai quyết danh mục config hợp lệ — admin catalogue? |
| P-23 | Chọn service level + required-by date, thấy ngay date khả thi hay không | YCF | Service level = thường/gấp? Surcharge %? |
| P-24 | Nhận báo giá tự động: cost breakdown + committed delivery date + expiry time | YCF | Expiry mặc định bao lâu (rule)? |
| P-25 | Confirm order và **pay** | YCF | Payment gateway nào? Prepaid hay giữ thẻ? (RC pháp lý) |
| P-26 | Track order/item real-time, **không thấy lab nào đang in** | YCF + RC | Mức chi tiết track được công khai tới đâu? |
| P-27 | Raise reprint/complaint sau giao trong window cấu hình | YCF | Window bao nhiêu ngày? Kênh complaint? |
| P-28 | Quản lý thư viện model cá nhân, tái sử dụng cho đơn sau | YCF | Giới hạn lưu trữ mỗi khách? |

### Lab Manager (P-29 → P-34)

| Mã | YCF thô | Loại | Chưa rõ |
|----|---------|------|---------|
| P-29 | Đăng ký lab + khai working calendar, capacity, transfer time to hub | YCF | Ai duyệt lab vào mạng? |
| P-30 | Machine registry: technology, build volume, layer height/tolerance đạt được, operational state | YCF | Ai kiểm chứng khai báo (đối chiếu P-07)? |
| P-31 | Material & colour inventory, stock levels, consumption tracking | YCF | Trừ stock khi nào: accept job hay hoàn thành? |
| P-32 | Accept/decline job trong **bounded response window**, decline phải có lý do | YCF + RC | Window bao nhiêu giờ? Không trả lời = auto-decline hay auto-accept? |
| P-33 | Xem machine-level schedule đề xuất dạng timeline | YCF | Lab có quyền đảo thứ tự trong máy của mình không? |
| P-34 | Xem standing của chính lab + các con số behind it | YCF | Công thức standing minh bạch tới đâu? |

### Lab Operator (P-35 → P-40)

| Mã | YCF thô | Loại | Chưa rõ |
|----|---------|------|---------|
| P-35 | Xem job queue theo máy: model file, config, internal due time | YCF | — |
| P-36 | Tải model qua time-limited link | YCF + RC | Liên kết P-18 |
| P-37 | Advance state: preparation → printing → post-processing → completion | YCF + RC | Nhánh fail bắt buộc ở bước nào? |
| P-38 | Report actual print duration + material consumed khi hoàn thành | YCF | Có bắt buộc không? Không báo thì sao? |
| P-39 | Report incidents: print failure, machine breakdown, material shortage + **photographic evidence** | YCF | Ảnh bắt buộc? Dung lượng/chế độ ảnh offline? |
| P-40 | Record handover thành phẩm về hub | YCF | Tracking số vận đơn hay chỉ xác nhận tay? |

### Hub QC Staff (P-41 → P-45)

| Mã | YCF thô | Loại | Chưa rõ |
|----|---------|------|---------|
| P-41 | Nhận batch từ lab, reconcile với expected jobs | YCF | Lệch reconcile xử sao (thiếu hàng, hỏng vận chuyển)? |
| P-42 | Chạy inspection checklist bind với quality grade của item | YCF | Checklist template do ai ban hành? |
| P-43 | Ghi pass/fail per unit + defect classification + ảnh | YCF | Kiểm theo mẫu 100% hay theo batch size? |
| P-44 | Attribute lỗi cho lab / hub / customer geometry | YCF | Trọng tài khi lab khiếu nại attribution? |
| P-45 | Trigger reprint job + xác nhận revised delivery commitment | YCF | Reprint bao nhiêu lần thì dừng → hoàn tiền? |

### Hub Fulfillment Staff (P-46 → P-48)

| Mã | YCF thô | Loại | Chưa rõ |
|----|---------|------|---------|
| P-46 | Consolidate items của một order, xác nhận đủ trước khi pack | YCF | — |
| P-47 | Pack, ghi shipment details, bàn giao carrier | YCF | Tích hợp carrier API hay ghi tay? |
| P-48 | Cập nhật delivery status tới khi hoàn tất | YCF | Khách xác nhận nhận hàng trong app? |

### Operations Manager (P-49 → P-53)

| Mã | YCF thô | Loại | Chưa rõ |
|----|---------|------|---------|
| P-49 | Monitor network: open orders, jobs at-risk, load & standing per lab | YCF | Ngưỡng at-risk (slack < X)? |
| P-50 | Override assignment/schedule với **lý do ghi nhận** | YCF + RC | Override có bypass feasibility filter không? |
| P-51 | Adjust order priority, xử lý order vượt reprint limit | YCF | Reprint limit mặc định? |
| P-52 | Suspend/reinstate lab, cap workload lab đang bị theo dõi | YCF + RC | Tiêu chí suspend tự động hay manual? |
| P-53 | SLA dashboards + export báo cáo | YCF | Export format? |

### Pricing & Scheduling Engine (P-54 → P-62) — đây là **system actor**, không phải vai người

| Mã | YCF thô | Loại | Chưa rõ |
|----|---------|------|---------|
| P-54 | Slice & analyse models → print time + material | YCF (sys) | Async job, retries? |
| P-55 | Compute price từ active parameter set, **freeze version onto quote** | YCF (sys) | Xem P-10 |
| P-56 | Run speculative network-wide placement tại quote time | YCF (sys) | Xem P-13 |
| P-57 | Decompose confirmed order → jobs theo item, quantity, build-volume limits | YCF (sys) | Chi tiết khi nào tách thành nhiều job? |
| P-58 | Filter labs by hard constraints + score feasible set | YCF (sys) | Xem P-11 |
| P-59 | Place jobs on specific machines, backward due dates + consolidation | YCF (sys) | Xem P-12 |
| P-60 | Repair unstarted schedule portion trên reject/fail/breakdown/reprint, bounded budget | YCF (sys) | Xem P-14 |
| P-61 | Recalibrate duration estimates per machine model từ reported actuals | YCF (sys) | Xem P-15 |
| P-62 | **Record every assignment decision với candidate set + scores** | YCF (sys) | ⚠️ Không thấy FR nào đặc tả decision log → xem 07_Traceability |

### System Administrator (P-63 → P-68)

| Mã | YCF thô | Loại | Chưa rõ |
|----|---------|------|---------|
| P-63 | Quản lý accounts, roles, permissions (customer/lab/hub) | YCF | — |
| P-64 | Catalogues: materials, colours, technologies, quality grades, post-processing | YCF | — |
| P-65 | Cấu hình pricing params, assignment weights, scheduling limits, SLA thresholds **không redeploy** | YCF + RC | Có cần approval khi đổi giá? |
| P-66 | Quản lý inspection checklists + defect taxonomy | YCF | ⚠️ Chưa thấy FR ADMIN riêng |
| P-67 | Monitor background job health, storage, integration status | YCF | Health dashboard hay log alert? |
| P-68 | Review audit log of manual interventions & parameter changes | YCF | — |

## d) Non-Functional Requirements thô

| Mã | Phiếu viết | Loại | Ghi chú |
|----|-----------|------|---------|
| P-69 | Quote responsiveness: async, preliminary estimate nhanh, committed figure khi xong, không block màn hình | RC | Chuyển thành NFR đo được ở B11 — **đã có ngưỡng chưa có** |
| P-70 | Bounded scheduling computation: hard time budget + luôn giữ feasible fallback | RC | Ngưỡng cụ thể thiếu |
| P-71 | Feasibility correctness: không bao giờ gán job cho máy không sản xuất được — **correctness, không phải quality** | RC | Test suite độc lập với scoring |
| P-72 | Schedule consistency under concurrency: serialise per machine, double-booking impossible **by construction** | RC | Lock/transaction strategy thuộc SDD |
| P-73 | Stability of committed promises: ngày giao chỉ đổi trong điều kiện xác định + notify | RC | Liệt kê điều kiện được đổi — chưa làm |
| P-74 | Estimation accuracy: đo liên tục gap estimate vs actual per machine model và hiệu chỉnh | RC | Thành phần đo chưa nêu |
| P-75 | IP protection: link ngắn hạn thu hồi được, log access, không bulk download | RC | Đã khá đo được |
| P-76 | Decision auditability: reconstruct mọi assignment/reschedule/override với candidates + scores | RC | Phụ thuộc P-62 (decision log) |
| P-77 | Configurability without redeployment | RC | Sinh thực thể Config (admin) |
| P-78 | Shop-floor usability: tablet, kết nối kém, tay bẩn, tối thiểu thao tác, không mất dữ liệu khi rớt mạng | RC | **Đo bằng gì?** Số tap tối đa? |
| P-79 | Network scalability: ≥ 50 labs, vài trăm machines không đổi kiến trúc | RC | Có ngưỡng |
| P-80 | Fair work distribution: exploration allowance cấu hình được, không dồn việc vào lab điểm cao | RC | ⚠️ Thuộc extended scope theo mục 4 |

## e) Theory & Practical · f) Products · g) Tasks · mục 4

| Mã | Trích | Loại | Suy ra |
|----|-------|------|--------|
| P-81 | 10 nền tảng lý thuyết (parallel machine scheduling NP-hard, dispatching rules + local search, batching/setup, CTPOP, MCDA, rescheduling stability, computational geometry/slicing, statistical calibration, reputation, QM & fault attribution) | Nền tảng | Xác nhận **đề tài có phần nghiên cứu**; mọi mục e chỉ để biện minh thuật toán, không sinh FR — đúng khuyến nghị tài liệu (đừng "báo cáo nghiên cứu hoá") |
| P-82 | 11 hoạt động thực hành (implement slicer, frozen quote test, feasibility test suite, weight sensitivity, backward due-date, time-budget search, event repair, **network+lab simulator**, baseline comparison, **real lab trial**, adversarial access test) | Nền tảng | Đối chiếu 1-1 với các mục f), g) được ở dưới; simulator là deliverable không phải tooling |
| P-83 → P-94 | 12 Products: Customer Portal · Lab Portal · Hub Console · Operations Console · Slicing Service · Pricing Engine · Assignment & Scheduling Engine · Calibration Module · SLA Analytics · Simulation Harness · Algorithm Evaluation Report · System Documentation | SP | Mỗi sản phẩm phải có ≥1 FR; **Simulation Harness + Evaluation Report** là 2 SP dễ bị quên nhất |
| P-95 → P-99 | WP1 PM/BA · WP2 Algorithm · WP3 Backend · WP4 FE Customer+Ops · WP5 Fullstack Lab/Hub+Sim+QA — mỗi WP 1 chủ sở hữu | WP | Khớp "mỗi phân hệ có chủ sở hữu"; đối chiếu với phân hệ M01–M06 ở B5 |
| P-100 | "Scheduler là contribution, storefront không phải — behaviour phải **visible in the product**" | DH | Quyết định demo: ops console phải nhìn thấy schedule & repair |
| P-101 | "Date từ bảng lead-time = phủ nhận toàn bộ tiền đề; speculative phải có từ iteration 1, crude form cũng được" | DH | **Bắt buộc lịch trình**: P-13 vào MVP |
| P-102 | "Failure & reprint thuộc thiết kế đầu tiên; simulator inject fault từ ngày đầu" | DH | State machine Job/Order thiết kế cùng rescheduler |
| P-103 | "Không có simulator thì không có gì để evaluate" | DH | Simulation Harness nâng tầm deliverable, vào Must |
| P-104 | "Một lab thật đáng hơn simulation lớn cho bài toán estimation; arrange **before** implementation" | DH | Phụ thuộc bên thứ ba — rủi ro lớn nhất, cần hạn chót ký partnership |
| P-105 | MVS: upload→quote→pay→track + uniform pricing + capability filter + scored assignment + machine scheduling dispatching rule + lab/hub apps + hub inspection w/ reprint. Extended: improvement search, calibration, ledger feeding allocation, fair-distribution exploration | DH | **Đây chính là MoSCoW baseline** của bước 5 & 10 |

## Danh sách câu hỏi cho GVHD / đối tác lab (sắp theo mức ảnh hưởng phạm vi)

1. **(P-25)** Thanh toán qua gateway nào, xử lý tiền/hoàn tiền nằm trong phạm vi hệ thống hay chỉ ghi nhận trạng thái? — *nếu làm thật: thêm phân hệ tài chính; nếu không: Out-of-scope + lý do pháp lý*
2. **(P-47, P-48)** Tích hợp carrier API (GHN/GHTK/DHL…) hay fulfillment ghi tay? — *quyết định 1 phân hệ tích hợp*
3. **(P-29, P-104)** Lab đối tác thật đã chốt chưa, ai duyệt lab vào mạng, có thoả thuận dữ liệu không? — *nếu chưa: calibration + trial là rủi ro đỏ*
4. **(P-12)** Batch bound và internal due-date buffer: công thức lùi deadline cụ thể (hub transfer X giờ, inspection Y giờ, shipping Z ngày)?
5. **(P-32)** Response window của lab là bao lâu; im lặng = auto-decline (rủi ro) hay auto-accept (lab bị ép)?
6. **(P-43)** Kiểm hub 100% hay sampling? Ngưỡng sampling theo gì?
7. **(P-13)** Preliminary quote ước lượng bằng gì trong crude form (queue remaining + hằng số)? Độ chính xác chấp nhận được ở MVP?
8. **(P-04/P-10)** Internal cost của lab hiển thị cho lab, hay lab chỉ nhận payout theo bảng giá nền tảng? — *quyết định mô hình kinh doanh và thực thể settlement*
9. **(P-26)** Khách được thấy trạng thái tới mức nào (không lộ lab)?
10. **(P-07/P-17)** Cold-start standing cho lab mới = giá trị nào, bao lâu thì standing có hiệu lực vào score?
11. **(P-50)** Ops override có được bypass feasibility filter không (đặt hàng khách quen trên máy không đủ khả thi)?
12. **(P-24)** Quote expiry mặc định bao lâu, gia hạn được không?
13. **(P-78)** Chỉ số đo "tối thiểu thao tác" cho shop-floor: số tap tối đa từ queue → update state?
14. **(P-45, P-51)** Reprint limit mỗi order (số lần) và escalation khi vượt?
15. **(P-18)** Grace period của file link sau khi job hoàn tất?

## Giả định đang tạm đúng (phải ghi chú ⚠ xác nhận lại)

| # | Giả định | Nguồn | Nếu sai thì sao |
|---|----------|-------|-----------------|
| A1 | Slicer mã nguồn mở (PrusaSlicer/CuraEngine) chạy được headless trên server, đủ nhanh cho MVP | P-09 | Nếu không: thời gian quote >> NFR, phải đổi engine hoặc pre-slice cache |
| A2 | Lab chịu report actual duration + material thật | P-38 | Calibration loop chết (P-15, P-61), về estimation hằng số |
| A3 | Một lab đối tác ký được trước tuần 8 | P-104 | Mất real trial; chỉ còn simulation — phải nói rõ trong Report |
| A4 | Transfer time lab→hub khai được và ổn định | P-29 | Backward due date sai hệ thống, mọi cam kết trễ |
| A5 | Payment chỉ ghi nhận trạng thái (không xử lý tiền trong hệ thống) | P-25 | Thêm phạm vi reconciliation/refund |
| A6 | Mesh từ khách "thường hợp lệ", validation fail là ngoại lệ có hướng dẫn sửa | P-21 | Tỷ lệ fail cao → cần bước repair/self-fix trước quote |
| A7 | 5 máy chủ yếu FDM; công nghệ khác (resin/SLS) Out of scope MVP | P-09 | Ma trận capability phình, filter phức tạp |
| A8 | Simulator tạo workload phân phối Poisson là đủ đại diện | P-82 | Kết quả evaluation bị hội đồng chất vấn tính tổng quát |

---
*Cột "Suy ra" của dòng nào còn trống ở bảng c) nghĩa là dòng đó chỉ đặc tả FR — đã có FR tương ứng kiểm ở 07_Traceability.*
