# PrintGrid — Kế hoạch triển khai 3 tuần (Roadmap)

> Bản này chia các task để hoàn thành **trong 3 tuần**, dựa trên hiện trạng code và 17 tài liệu trong `documentation/`.
> Lưu ý: tài liệu gốc mô tả dự án 6 tháng (Sep 2026 – Mar 2027). Với 3 tuần, chúng ta **nhắm mục tiêu "Pass Threshold" + một phần "Good"** trong `06-System-Scope.md §4` — demo loại bỏ được phần rụng, đúng thông điệp: *"Hội đồng chấm thuật toán lập lịch, không phải vẻ đẹp của storefront"*.

---

## 0. Hiện trạng code (đã kiểm tra)

### ✅ Đã có
| Mảng | Chi tiết |
|---|---|
| Backend skeleton | 6 module (Customer, Scheduling, Lab, Hub, Analytics, Admin) đủ Domain / Application / Infrastructure theo Clean Architecture; single `PrintGridDbContext`, migrations |
| Auth | JWT register/login/refresh (Customer) + refresh token + Bcrypt; `AuthController`, policies theo vai trò |
| Customer | Model CRUD + `ModelsController` (MinIO), `PlaceOrder` + `OrdersController`; entities: Customer, Model, Order, OrderItem, Quote, QuoteItem |
| Scheduling | Job/Lab/Machine, `CapabilityFilter`, `AssignmentScorer` (đa tiêu chí), `AssignJobCommandHandler` (capability → timeline placement → scorRank), `MachineTimelineService` (chèn vào slot trống, utilization) |
| Frontend | Login/Register (dark showcase R3F), HomePage, ModelLibrary, Orders, LabQueue, SchedulingBoard, routing + `ProtectedRoute` + `AuthContext` |
| Tests | `AuthTests`, `ModelCrudTests`, `OrderTests`, `AssignmentScorerTests`, `CapabilityFilterTests` |

### ❌ Còn thiếu (phải làm)
1. **Slicing / ước tính thời gian in** (`FR-SCHED-001/002`) — chưa có service, chưa tích hợp CuraEngine (rủi ro cao nhất, xem `06-System-Scope.md §5.1`).
2. **Quote có lập lịch dự phòng (speculative)** (`FR-CUST-006`, `FR-SCHED-005`) — entity Quote đã có nhưng chưa có command/handler tạo quote.
3. **Rescheduling theo sự kiện** (`FR-SCHED-007`) — chưa có.
4. **Lab module**: Lab registration, Machine registry API, Material inventory, Accept/Reject, Production workflow, Incident report — chỉ có scaffold.
5. **Hub module**: Batch receipt, Quality inspection + fault attribution, Reprint, Consolidation — chỉ có scaffold.
6. **Tracking + thông báo realtime** (SignalR) — chưa có hub flow cho customer.
7. **Simulation framework + baseline comparison** (phần học thuật cần demo).
8. **Analytics/Admin**: network dashboard, lab performance, config — chỉ có scaffold.

---

## 1. Khung ưu tiên (đừng sa đà)

| Ưu tiên | Mục | Căn cứ |
|---|---|---|
| P0 | Demo miễn nhiễm theo trục dọc: **upload → quote (ngày giao từ capacity thật) → đặt hàng → assign → lab nhận/sản xuất → hub QC → tracking** | `06-System-Scope.md §4.1` |
| P0 | **Rescheduling**: một lỗi sản xuất (fail/reject/breakdown) → lịch được vẽ lại, đúng hạn vẫn đảm bảo | `FR-SCHED-007`, điểm cốt lõi học thuật |
| P1 | **Simulation**: so sánh scheduler của mình vs. 2 baseline (random, nearest-lab) | `06-System-Scope.md §1.2`, điều hội đồng chấm |
| P1 | Lab/Hub UI làm việc được (không cần đẹp) | `05-Main-Features.md` F2/F3 |
| P2 | Nếu còn thời gian: calibration loop, dashboard ops, admin config | `06-System-Scope.md §3.2` |

**Nguyên tắc cắt giảm:** tính năng nào không nằm trong `06-System-Scope.md` → cho vào backlog cuối file. Quá 20% thời gian cho UI polish là dấu hiệu đáng báo (`§3.3`).

---

## 2. Phân công theo track (3 track song song — khớp team 5 người)

| Track | Người (thay theo team) | Trách nhiệm |
|---|---|---|
| **A — Scheduling core** | WP2 (algorithm) + WP3 (backend) | slicing/estimate → quote speculative → assign → reschedule → simulation |
| **B — Customer portal** | WP4 (frontend) + WP3 | UI: config → quote → order → tracking; kết nối true API đã có |
| **C — Lab/Hub/Admin + QA** | WP5 (fullstack/QA) + WP1 (PM/BA) | Lab API+UI, Hub API+UI, admin đơn giản, test, seed data |

Giao diện "mượn" `PrintGrid.SharedKernel` và theo module có sẵn. Plugin module mới theo README §"Adding a module".

---

## 3. Tuần 1 — Nền tảng: khép quote pipeline (FR-CUST-006, FR-SCHED-001/002/005, FR-LAB-002/003/004)

**Milestone tuần 1:** *Customer upload model → hệ thống slice ước tính (~phút) → tạo quote với delivery date từ speculative placement → đặt hàng → job tự assign → lab nhìn thấy job trong queue.*

### 📋 Chức năng hoàn thành trong Tuần 1 (theo người dùng cuối)

| # | Chức năng | FR-ID | Mô tả ngắn | Thoát nghiệm |
|---|---|---|---|---|
| T1-1 | Upload & phân tích model | FR-CUST-002, FR-SCHED-001 | Upload STL/OBJ/3MF lên MinIO, parse lấy bounding box + volume; trả lại preview trong trình duyệt | Upload OK, kích thước hiện chính xác, file lưu được |
| T1-2 | Slicing & ước tính thời gian in | FR-SCHED-002 | Adapter CuraEngine (hoặc fallback mock): sau upload → slice job chạy nền (Hangfire) → ra `EstimatedPrintMinutes` + lượng vật liệu | Quote dùng được con số ước tính; thời gian slicing ≤60s |
| T1-3 | Cấu hình in | FR-CUST-005 | Chọn material, màu, quality grade, infill %, số lượng, post-processing, ngày cần giao | Config màn hình đầy đủ, tổ hợp không khả dụng bị disable kèm giải thích |
| T1-4 | Tạo quote có lập lịch dự phòng | FR-CUST-006, FR-SCHED-005 | Slice xong → chạy trial placement (không commit) → trả price breakdown + delivery date từ capacity thật + thời gian hết hạn quote | Quote ra trong ≤60s, ngày giao hợp lý, quote hết hạn sau 48h |
| T1-5 | Đặt hàng | FR-CUST-007 | Review cart từ quote, nhập địa chỉ giao, confirm order | Order tạo từ quote, không cho đặt khi quote hết hạn |
| T1-6 | Tự động assign job | FR-SCHED-006 | `OrderConfirmedEvent` → chạy capability filter + scoring + chèn slot → tạo Assignment cho lab/máy tốt nhất | Job gán vào máy khả dụng, internal due đảm bảo deadline, chỉ một solver |
| T1-7 | Quản lý lab & máy | FR-LAB-001/002 | CRUD lab + machine registry (build volume, layer height, tolerance, status) để nuôi network demo | Máy thêm/sửa/tắt trạng thái được; máy có job đang chạy không xoá được |
| T1-8 | Tồn kho vật liệu (tối giản) | FR-LAB-003 | Mỗi lab khai báo material + màu + số lượng tồn | Capability filter loại lab hết vật liệu |
| T1-9 | Nhận/trả lời job của lab | FR-LAB-004 | Lab thấy job mới qua notification (SignalR), xem chi tiết, accept hoặc reject kèm lý do | Reject có lý do, status cập nhật, notification đến lab khác |
| T1-10 | Hàng chờ lab nhìn từ API thật | FR-LAB-005 (phần) | LabQueuePage đọc queue thật thay vì mock | Lab thấy đúng job được gán, đúng thứ tự |

**Tổng kết:** 10 chức năng = **luồng trục dọc "khách đặt → hệ thống tự xếp lịch"** — đủ chứng minh *delivery date đến từ capacity thật chứ không phải bảng lead-time* (điểm học thuật chính).

| Ngày | Track A (Scheduling core) | Track B (Customer) | Track C (Lab/Hub/QA) |
|---|---|---|---|
| **1** | Chốt hợp đồng API: OpenAPI cho create-quote, slicing job, job-assigned event | Giữ vững 2 flow đã làm (auth, model), đọc lại `17-API-Design.md` | Seed 3–5 lab + 10–15 máy (script) với spec khác nhau; đảm bảo **ép được** capability filter loại máy không đủ build volume |
| **2** | **Slicing/estimate service**: adapter CuraEngine/PrusaSlicer CLI; parse STL → bounding box, volume, time estimate. **Có fallback mock** (bảng tra tạm) nếu engine chưa chạy được | Thiết kế UI config (material, quality, infill, qty) theo `05-Main-Features.md` F1.2 | **Lab registry API**: CRUD lab + máy (status operational/maintenance/offline) — bước nền cho mọi thứ |
| **3** | Chạy slicing qua **Hangfire** (async), lưu `EstimatedPrintMinutes` lên Job | Ui config nối được vào model đã upload | Material inventory đơn giản (stock theo lab); liếc `08-Business-Rules.md` phần inventory |
| **4** | **Quote handler** (`CreateQuoteCommand`): spec → capability filter → trial placement (dùng lại `MachineTimelineService`, **không commit** sang job thật) → earliest feasible delivery + giá (itemized) | Trang **"Request Quote"** gọi API, hiển thị breakdown + delivery date | API **accept/reject job** (JobStatus Assigned→Accepted/Rejected) + lý do |
| **5** | **OrderConfirmedEvent → AssignJob**: xâu pipeline quote→order→assign bằng domain event (mẫu trong `14-System-Architecture.md §4`) | Trang **PlaceOrder** chọn delivery address dựa trên quote (đã có `OrdersController`) | **Notification** cho lab khi job mới (SignalR nhóm lab); đơn giản là đẩy message |
| **6** | Quote **expiry** (Hangfire cancel sau 48h); nếu hết hạn → quote REJECTED | Home/hero liên kết: upload→config→quote→order một mạch | Job queue UI (LabQueuePage) đọc từ API thật thay vì mock |
| **7** | **DEMO 1**: chạy toàn bộ flow; ghi lại những chỗ break; fix nhanh | — | Test tích hợp `PrintGrid.IntegrationTests` cho POST quote + assign |

**Thoát (Definition of Done) Tuần 1:** Luồng "quote → order → assigned" chạy end-to-end được; delivery date thay đổi khi network tải (chứng minh quote dùng capacity thật — không phải bảng lead-time).

---

## 4. Tuần 2 — Sản xuất & rescheduling: khép trục Lab-Hub (FR-SCHED-007, FR-LAB-005/006, FR-HUB-002/003, FR-CUST-009)

**Milestone tuần 2:** *Job fail/breakdown → hệ thống kích hoạt reschedule → lịch mới hợp lệ → hub QC pass/fail kèm fault attribution → reprint tự tạo khi lỗi lab → customer thấy trạng thái realtime.*

### 📋 Chức năng hoàn thành trong Tuần 2 (theo người dùng cuối)

| # | Chức năng | FR-ID | Mô tả ngắn | Thoát nghiệm |
|---|---|---|---|---|
| T2-1 | Quy trình sản xuất của operator | FR-LAB-005 | Bắt đầu in, hoàn thành, báo actual time + vật liệu thật | Actual (time, material) được ghi lại để sau phục vụ calibration |
| T2-2 | Báo sự cố / incident | FR-LAB-005(phần) | Incident: print failure (kèm % hoàn thành), machine breakdown, thiếu vật liệu, kém chất lượng + ảnh | Incident lưu được, phát sinh event ra hệ thống |
| T2-3 | Reschedule theo sự kiện | FR-SCHED-007 | Nhận event (reject, fail, breakdown, reprint) → vẽ lại lịch ≤30s, không chạm job đang in, thông báo người liên quan | 3+ loại event kích hoạt được; job InProgress bất biến |
| T2-4 | Tracking đơn hàng realtime | FR-CUST-009 | 7 trạng thái (Confirmed → … → Delivered) qua SignalR; ETA cập nhật sau reschedule; **không lộ lab nào đang in** | Customer thấy đủ trạng thái, không thấy tên lab |
| T2-5 | Nhận lô hàng tại hub | FR-HUB-001 | Nhận batch từ lab, đối soát job (QR sim), báo thiếu | Báo đủ/thiếu item, lab được notify khi lệch |
| T2-6 | Kiểm tra chất lượng (QC) | FR-HUB-002 | Checklist theo quality grade, pass/fail từng mục, chụp ảnh bắt buộc khi fail, classify defect, fault attribution (Lab/Hub/Customer) | Quyết định QC kéo theo hành động đúng (pass→fulfillment, fail→reprint…) |
| T2-7 | Tự động tạo reprint | FR-HUB-003 | Inspection lab-fail → job URGENT kế thừa deadline gốc, đi qua allocation bình thường | Reprint tạo tự động, customer được giữ nguyên ngày giao |
| T2-8 | Xem lịch máy (Gantt) | FR-LAB-006 | SchedulingBoardPage / timeline từng máy: job đang chạy, job chờ, màu theo độ ưu tiên | Timeline khớp dữ liệu thật, cập nhật sau mỗi reschedule |
| T2-9 | Ledger hiệu suất lab (nền tảng) | FR-ANALYTICS-002 (phần) | Ghi nhận per job: giao đúng hạn? qua QC lượt đầu? | Hằng số chuẩn để tuần 3 tính ODR/FPY |

**Tổng kết:** 9 chức năng = **"hệ thống chịu được thực tế": fail xảy ra = chuyện thường, lịch tự đứng dậy** — điểm "Good" của `§4.2`.

| Ngày | Track A (Scheduling core) | Track B (Customer) | Track C (Lab/Hub/QA) |
|---|---|---|---|
| **8** | Định nghĩa **event types**: `JobRejectedEvent`, `PrintFailureEvent`, `MachineBreakdownEvent`, `InspectionFailedEvent` | UX trang **Order tracking** (stage theo `05-Main-Features.md` F1.5 — không lộ lab) | **Production workflow API**: start/complete, báo actual time + material; incident report (type, stage, ảnh) |
| **9** | **Rescheduler** v1: nhận event → xác định job bị ảnh hưởng (không đụng job đang in) → dispatching-rule fallback: đẩy job còn DeadlineFrank vào slot sớm nhất của lab/máy khác mình còn khả năng | Order page: trạng thái live; actual ETA cập nhật sau reschedule | **Hub: batch receipt** tối giản (nhận lô, đối soát job QR — có thể để scan sim: nhập mã) |
| **10** | Rescheduler: đóng khung trong **30s budget**; kết `best solution found`; audit log mọi thay đổi | SignalR customer hub: event `OrderStatusChanged` | **Hub: quality inspection**: checklist theo quality grade, pass/fail, **photo bắt buộc khi fail**, classify defect, fault attribution (Lab/Hub/Customer) |
| **11** | Nối `InspectionFailedEvent` (lỗi lab) → **auto tạo reprint job**: priority URGENT, kế thừa deadline gốc, đi qua allocation bình thường | — | **Incident → reschedule** end-to-end test đầu tiên (thủ công trigger) |
| **12** | Slicer calibration điểm tạm: thu `(estimated, actual)`; tính MAPE sơ bộ (chỉ cần bảng, chưa cần regression loop — nâng cấp ở tuần 3) | Order tracking: thông báo email/signalR khi có thay đổi | **Lab performance ledger**: ghi nhận completed_on_time, passed_first_inspection cho lab |
| **13** | Machine-level **Gantt/Schedule view API** (dùng `MachineTimelineService`) cho SchedulingBoardPage thật | — | UI stable cho Lab & Hub core flow (đọc API thật); gom mock out |
| **14** | **DEMO 2**: fail giữa lúc in → reschedule → reprint → hub pass → customer thấy trạng thái. Fix đắp | — | Integration test: reschedule đảm bảo job đang InProgress không bị chạm |

**Thoát Tuần 2:** Ít nhất **3 loại sự kiện** kích hoạt được reschedule (`06-System-Scope.md §4.2 "Good"`); reprint tự động khi InspectionFailed với lỗi thuộc lab; customer tracking realtime không lộ thông tin lab.

---

## 5. Tuần 3 — Simulation, đo lường, polish, đóng gói (FR-SCHED-008, FR-ANALYTICS-001/002)

**Milestone tuần 3:** *Simulation chạy được, kết quả scheduler vượt baseline; demo script chạy 1 nước; tài liệu demo + test đầy đủ.*

### 📋 Chức năng hoàn thành trong Tuần 3 (theo người dùng cuối)

| # | Chức năng | FR-ID | Mô tả ngắn | Thoát nghiệm |
|---|---|---|---|---|
| T3-1 | Simulation framework | S-1.2 (`06-Scope`), FR-SCHED-005 | Sinh luồng order cấu hình được + lab simulator có fault injection (tỷ lệ fail thay đổi được) + chạy 50 lab | Chạy lại được nhiều seed, tham số đổi được |
| T3-2 | Baseline comparison | S-1.2 | Scheduler của mình vs (a) random assignment, (b) nearest-lab heuristic — cùng đầu vào | Có bảng/kết quả 3 cột cho slide |
| T3-3 | Metric thu thập | S-1.2 | Weighted tardiness, on-time rate, utilization, độ chênh phân phối | Số liệu in ra được; tệp kết quả lưu lại |
| T3-4 | Network dashboard (ops view) | FR-ANALYTICS-001 | Active orders, at-risk jobs, network load %, lab status (operational/at-capacity/offline), alerts | Số liệu từ DB thật, refresh ≤30s |
| T3-5 | Hiệu suất lab | FR-ANALYTICS-002 | ODR, FPY, acceptance rate, score 90 ngày; so sánh network trung bình (ẩn danh) | Số tính đúng, hiển thị cho lab |
| T3-6 | (Điều kiện) Calibration loop | FR-SCHED-008 | Regression theo machine model → correction factor cho estimate sau; MAPE qua các kỳ | ≥10 mẫu rồi mới áp; MAPE giảm dần |
| T3-7 | (Điều kiện) Admin config tối giản | FR-ADMIN-002 | Sửa pricing rates + assignment weights (versioned) đủ để demo đổi thông số | Đổi không cần redeploy, version rõ ràng |
| T3-8 | Đóng gói demo | — | Demo script chạy 1 nước, dữ liệu seed đẹp, README cập nhật, ảnh/số liệu chuẩn báo cáo | Buổi demo final không vấp |

**Tổng kết:** 8 chức năng = **bằng chứng học thuật**: *scheduler của bạn tốt hơn quyết định ngẫu nhiên hoặc "gần nhất", và cải thiện theo thời gian* (T3-6). Phần T3-6/T3-7 chỉ làm nếu hết sớm — không làm cũng đạt "Good".

| Ngày | Track A (Scheduling core) | Track B (Customer) | Track C (Lab/Hub/QA) |
|---|---|---|---|
| **15** | **Simulation harness**: order stream generator (tần suất, spec ngẫu nhiên đúng miền dữ liệu), lab simulator với fault injection (tỷ lệ fail cấu hình được) | UI đánh bóng mức vừa phải (responsive, state loading/error) | Lock scope: **feature freeze** — chỉ bugfix + refinement từ đây (`06-System-Scope.md §3.3`) |
| **16** | Định nghĩa **baseline**: (1) random assignment, (2) nearest-available-lab simple heuristic — chạy cùng đầu vào | Bảo đảm mọi trang app đi được trên mobile/tablet (NFR usability) | **Network dashboard** (ops view): active orders, at-risk jobs, load %, lab status — số liệu từ real DB |
| **17** | Capture metrics: weighted tardiness, on-time rate, utilization, distribution fairness; **bảng so sánh 3 cột** cho slide | — | Lab performance metrics (ODR/FPY/acceptance) tính 90 ngày, hiển thị cho lab |
| **18** | (Nếu đủ) **Calibration loop** đơn giản: regression theo machine model → correction factor áp vào estimate sau; MAPE kỳ sau | — | Admin tối giản: **configuration CRUD** (pricing rates, weights) đủ đổi giữa demo; role user admin đơn giản |
| **19** | **Demo script** viết + dry run: kịch bản từng bước, dữ liệu seed đẹp, số liệu simulation in sẵn để báo cáo | — | Test toàn diện: chạy `dotnet test`, integration, smoke test toàn flow |
| **20** | Buffer: sửa lỗi phát hiện từ demo; tối ưu nếu có | Buffer | Cập nhật README + ảnh demo; ghi lại quyết định thiết kế (cho phần "defend design decisions") |
| **21** | **DEMO FINAL**: chạy kịch bản hoàn chỉnh trước hội đồng | — | Burn-down: mọi task P0 đều demo được, P1 đa phần |

**Thoát Tuần 3:** Module simulation+metric chạy và có kết quả **so với ít nhất 2 baseline**; demo script 1 nước không vấp; toàn bộ mục `06-System-Scope.md §4.1 (Pass Threshold)` ×; README cập nhật.

---

## 6. Demo script gợi ý (kịch bản hội đồng)

1. **Customer** upload 1 STL → config → quote: **giao đúng lúc, giá breakdown rõ** → đặt hàng.
2. Mở **Scheduling Board**: thấy job đã assign vào lab/máy khả dụng, slot trên timeline.
3. **Lab operator** nhận job → bắt đầu in → **report failure ở 78%**.
4. **Reschedule** tự chạy: job dời sang lab khác (hoặc reprint khẩn) → deadline gốc **vẫn giữ**.
5. **Hub QC**: fail được phát hiện (lỗi lab) → **reprint auto tạo** → pass → consolidate.
6. Nháy **Simulation**: biểu đồ scheduler của mình vs random vs nearest-lab → chỉ số rõ ràng.
7. Khép: **customer tracking** thấy trạng thái → delivered.

---

## 7. Backlog (có điều kiện / sau 3 tuần)

- Calibration loop hoàn chỉnh (weekly regression + MAPE tracking) — nếu còn giờ tuần 3
- Material inventory nâng cao (low-stock alert, projection)
- SLA reporting + export Excel/PDF
- Audit log viewer + đầy đủ admin CRUD
- Model library: reorder flow lưu config, thumbnails
- Manual intervention tools (reassign do Ops, suspend lab) — có thì tốt cho demo
- Real lab trial: dành cho các tuần sau trước Hội đồng (tháng 3)

---

## 8. Rủi ro chính & cách đối phó

| Rủi ro | Biện pháp |
|---|---|
| CuraEngine không chạy được trên môi trường dev | Ngay từ ngày 2 có **fallback mock**: estimate theo bảng + kích thước; ghi rõ trong báo cáo là test cần engine thật |
| Quá tải vẽ UI | Rule `§3.3`: >20% giờ vào polish = cắt; lab/hub UI giữ chức năng trước, đẹp sau |
| Feature creep | Bất kỳ tính năng nào không thuộc `documentation\06-System-Scope.md` → backlog |
| Dữ liệu simulation không đẹp | Generator cấu hình được; chạy nhiều seed, chọn bộ số ổn, có kịch bản khó cho phần thảo luận |
| Slipping lịch | Hard freeze tuần 3 ngày 15; mọi P1 còn lại hạ xuống backlog |

---

*Files tham chiếu: `06-System-Scope.md`, `09-Functional-Requirements.md`, `14-System-Architecture.md`, `05-Main-Features.md`, `README.md`.*