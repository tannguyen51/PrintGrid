# PHIẾU GỐC + ĐÁNH DẤU VỊ TRÍ THÊM/SỬA CHO v1.1

**Đối chiếu bản gốc:** `Phieu_FA26SE249.docx` (bản trích: `Phieu_FA26SE249-extracted.md`).
Bản này giữ nguyên văn bản gốc làm xương sống; mọi khối đánh dấu là thứ dán vào file `..._v1.1.docx`.

| Ký hiệu | Nghĩa | Cách hành động |
|---------|-------|----------------|
| ⬜ GIỮ | Đã rà — không thay đổi gì | Copy nguyên văn sang v1.1 |
| 🟩 THÊM | Khối hoàn toàn mới | Dán đúng vị trí ghi trong ngoặc, không đụng nội dung quanh nó |
| 🟨 SỬA | Thay câu/cụm từ | ~~gạch ngang~~ = câu cũ bị thay; **đậm** = câu mới dán vào |
| ⚠ CHỜ | Chưa được điền | Chỉ chốt sau buổi họp GVHD (dán tiếp [câu hỏi #n] từ `workbook/01_Extract` §câu hỏi) |

> Văn bản THÊM/SỬA viết bằng **tiếng Anh** vì docx gốc tiếng Anh. Chú thích ký hiệu chỉ để nhóm đọc, không dán vào phiếu.

---

# 1. Register information for supervisor — ⬜ GIỮ NGUYÊN (chép từ docx)

# 2. Register information for students — ⬜ GIỮ NGUYÊN
*(trong bản trích các dòng còn trống — lấy đúng danh sách 5 SV từ file docx gốc)*

# 3. Register content of Capstone Project

## (*) 3.1. Capstone Project name — ⬜ GIỮ NGUYÊN TUYỆT ĐỐI

> 3.1.1. English: PrintGrid: Development of a Distributed 3D Printing Fulfillment and Scheduling Platform for a Network of Independent Printing Labs
> 3.1.2. Vietnamese: PrintGrid: Xây dựng nền tảng điều phối và tối ưu đơn hàng cho mạng lưới 3D Printing Lab phân tán
> Abbreviation: PrintGrid

*(Quy tắc B3: không đổi kể cả khi phạm vi thu hẹp — mất mã là mất cả bộ hồ sơ.)*

## (*) 3.2. Main proposal content

### a) Context

**[⬜ GIỮ]** Toàn bộ đoạn mở đầu ("Demand for 3D printing services is growing…") và 7 gạch đầu dòng
("Capacity in this domain is not a single number" … "The platform carries the quality guarantee") —
**giữ nguyên từng dòng, không sửa câu chữ**. Đây là các mã P-01→P-08 trong `workbook/01_Extract`.

🟩 **[THÊM v1.1 — dán 3 đoạn sau vào CUỐI mục a), sau gạch "The platform carries the quality guarantee…"]**

> *Evidence and conditions (added v1.1).* The load-bearing assumptions of this proposal are under
> verification at the time of this revision: (i) a real network of small labs willing to join a
> coordination platform — condition: signed working agreement with a partner lab before week 8;
> (ii) customers willing to order through an intermediary rather than a familiar lab — condition:
> survey of at least 20 prospective customers; (iii) an open-source slicer suitable for headless
> server use within the quote time budget — condition: pilot run on 20 real STL files. Verification
> results are recorded in the team requirements workbook (02_Phan-bien-de-tai, section 1).
>
> *Existing systems and the gap (added v1.1).* Commercial marketplaces (Xometry, Protolabs,
> Craftcloud) automate quoting and supplier routing, but they are closed platforms aimed at
> industrial job shops, with no centrally enforced quality guarantee as a design core and no
> evidence base open for study. The current practical alternative — each lab taking orders on its
> own page and slicing locally — cannot coordinate across labs and promises delivery dates from a
> static lead-time table. Academic scheduling literature solves the underlying NP-hard problem
> largely under single-factory, owned-machine assumptions. The gap PrintGrid occupies is the link
> between quote-time scheduling, customer commitment and plan repair across an independent-lab
> network, validated by simulation and one real lab. Novelty class: novel in approach and context.
>
> *Context numbers to complete (added v1.1).* ⚠ [câu hỏi #3 — sau buổi làm việc lab đối tác:
> số lab trong bán kính hub, sản lượng lab/tháng, cơ cấu máy FDM]

### b) Proposed Solutions

**[⬜ GIỮ]** đoạn mở đầu "Build PrintGrid, a platform where the customer deals…" — nguyên văn.
10 gạch trụ đánh số theo thứ tự gốc:

**1. Geometry and Slicing Analysis Service** — 🟨 SỬA (thêm mệnh đề cuối):
> ~~…for a given print configuration.~~
> …for a given print configuration. **The MVP supports FDM technology with one open-source
> slicing engine running headless; resin and SLS are declared out of scope.**

**2. Network-Uniform Pricing Engine** — ⬜ GIỮ nguyên.

**3. Capability-Filtered Assignment** — 🟩 THÊM 1 câu vào cuối gạch:
> **In the MVP the score weights are fixed and editable through administration; weight-sensitivity
> analysis is performed in the evaluation study.**

**4. Deadline-Backward Scheduling with Batch Consolidation** — 🟨 SỬA (thu hẹp theo quyết định B2):
> Jobs are placed on individual machines against internal due dates derived by working backwards
> from the customer deadline through hub transfer, inspection and shipping, ~~with jobs sharing
> material and colour consolidated on a machine to remove changeover time, bounded so
> consolidation cannot push urgent work behind a large batch.~~
> **Placement in the MVP follows dispatching rules (EDD); material and colour consolidation is
> extended scope, bounded so that consolidation can never delay urgent work.**

**5. Speculative Quote-Time Scheduling** — 🟩 THÊM 1 câu (giữ nguyên văn bản cũ phía trên):
> **The mechanism ships from the first iteration in a simple form (queue remaining-time estimate)
> and is refined later; a promised date must never come from a static lead-time table.**

**6. Event-Driven Rescheduling** — 🟨 SỬA phạm vi trigger:
> ~~Rejection, failure, breakdown and reprint~~ trigger repair of the unstarted portion of the plan
> only, under a bounded computation budget, with the dispatching-rule solution always retained as a
> feasible fallback. → **MVP: "Rejection and print failure trigger repair…"; breakdown and
> reprint-triggered repair follow in extended scope.** *(sửa thành: "Rejection and print failure
> trigger repair of the unstarted portion of the plan only, under a bounded computation budget
> ⚠ [ngưỡng — câu hỏi #4, đề xuất 10 s], with the dispatching-rule solution always retained as a
> feasible fallback; breakdown and reprint repair are extended scope.")*

**7. Estimate Calibration Loop** — 🟨 SỬA:
> Actual print durations reported by labs are compared against slicer estimates per machine model,
> and the resulting correction factors are fed back into both quoting and scheduling ~~so the
> network's promises improve as it accumulates history~~. **In the MVP the factors are applied
> manually through the administration interface; automatic regression is extended scope.**

**8. Central Hub Quality Control and Fault Attribution** — ⬜ GIỮ nguyên.

**9. Lab Performance Ledger** — 🟨 SỬA câu cuối:
> An auditable record of on-time delivery, first-pass yield, acceptance rate, utilisation and the
> gap between declared and delivered capacity, ~~aggregated into a standing that is an explicit
> input to the assignment score~~. **The MVP records, aggregates and displays the standing; using
> it as an explicit input to the assignment score is extended scope.**

**10. Model File Access Control** — ⬜ GIỮ nguyên (signed URL + log là đã đúng cam kết).

🟩 **[THÊM v1.1 — một dòng chốt phạm vi, ngay sau gạch 10]**
> *Explicitly not undertaken (v1.1): lab settlement & invoicing (handled by offline runbook), automatic
> carrier API integration, fair-distribution exploration — see Out of scope under section 4. (Payment of
> customers in real money is IN scope per decision 23/09 — see c-Customer bullet and FR-ANAL-005.)*

### c) Functional Requirements (thô, theo vai)

**[⬜ GIỮ TOÀN BỘ CẤU TRÚC THEO VAI]** — 8 nhóm, 60 gạch. ⚠ Lưu ý đã đính chính: 4 gạch
"decision log / pricing computation / pack & ship / checklist management" **đã có sẵn trong phiếu
gốc** (P-62/P-55/P-47-48/P-66) — không thêm gì vào phiếu; cái thiếu là FR đặc tả chúng trong hồ sơ
SRS (`workbook/07_Traceability` đã đề xuất FR-SCHED-009/010, FR-HUB-005, FR-ADMIN-005).

Chỉ 3 gạch sau thay đổi:

- *Customer* — 🟨 SỬA gạch "Confirm the order and pay":
  > Confirm the order and ~~pay~~ **pay via the integrated payment gateway; the order commits only on a
  > verified gateway webhook. The platform stores token and transaction id only — never card credentials
  > (BR-PAY-005).** ✅ [quyết 23/09 — B1 biên bản: TIỀN THẬT; refund/đối soát = FR-ANAL-005/UC-028, NFR-SEC-009]

- *Pricing and Scheduling Engine* — 🟨 SỬA 2 gạch:
  > ~~Place jobs on specific machines against backward-derived internal due dates, with material
  and colour consolidation~~ → **Place jobs on specific machines against backward-derived internal
  due dates (consolidation: extended scope).**
  > Recalibrate duration estimates per machine model from reported actuals → thêm: **In the MVP
  correction factors are entered through the administration interface.**

- *System Administrator* — ⚠ CHỜ ở gạch "Monitor background job health, storage and integration
  status": giữ nguyên hoặc hạ thành ràng buộc vận hành — [câu hỏi cần nêu tại họp, xem
  `01_Extract` mục treo P-67].

5 vai còn lại: ⬜ GIỮ nguyên 100%.

### d) Non-Functional Requirements (thô)

**[⬜ GIỮ]** 12 gạch nguyên văn. 🟩 **[THÊM v1.1 — sau MỖI gạch tương ứng, chèn mệnh đề
trong ngoặc vuông dưới đây]** (nguồn: đã chốt ở `10-NFR` + quyết định B2; phần ⚠ chờ họp):

1. Quote Responsiveness → 🟩 *[Preliminary estimate within 5 s; committed quote within 60 s at the
   95th percentile, measured on staging with meshes up to 50 MB and 30 concurrent sessions.]*
2. Bounded Scheduling Computation → 🟩 *[Hard budget ⚠ [câu hỏi #4 — đề xuất 10 s per repair run];
   the fallback schedule is always available.]*
3. Feasibility Correctness → 🟩 *[Verified by a test suite of jobs deliberately infeasible for
   specific machines, run independently of the scoring logic.]*
4. Schedule Consistency Under Concurrency → 🟩 *[Verified by concurrent placement tests on a
   single machine queue; zero double-bookings.]*
5. Stability of Committed Promises → 🟩 *[Defined change conditions: ⚠ [câu hỏi còn mở — liệt kê
   tại họp: khách dời date, reprint, bất khả kháng]; every change notifies the customer.]*
6. Estimation Accuracy and Calibration → 🟩 *[Estimated-vs-actual gap reported per machine model
   in the operations dashboard.]*
7. Intellectual Property Protection → 🟩 *[Access links valid for the assigned job plus a ⚠
   [câu hỏi #15 — đề xuất 24 h] grace period; every access logged; bulk download blocked.]*
8. Decision Auditability → ⬜ *(không thêm — phụ thuộc FR-SCHED-009 ở SRS)*
9. Configurability Without Redeployment → 🟩 *[Parameter change effective within 1 minute without
   service restart.]*
10. Shop-Floor Usability → 🟩 *[ [câu hỏi #13 — đề xuất: state update reachable within 3 taps
    from the queue; input survives a 2-minute connection loss.]*]*
11. Scalability of the Network Model → 🟩 *[Verified in the simulator at 50 labs / 300 machines
    and 1000 orders per month.]*
12. Fair Work Distribution → 🟩 *[Extended scope — declared in Out of scope for the MVP.]*

### e) Theory & Practical

**[⬜ GIỮ]** toàn bộ 10 bullet Theory + 11 bullet Practical.
🟩 **[THÊM v1.1 — chèn 1 câu vào CUỐI đoạn Practical, sau bullet "Test model file access control
adversarially…"]**
> **Tooling sources are pinned during the pilot: the open-source slicing engine is recorded with
> version in the technical appendix, and the EDD/ATC dispatching rules are cited from standard
> scheduling literature. Research question (restated v1.1): does capacity-aware quote-time
> scheduling and its repair reduce weighted tardiness and increase on-time delivery versus
> dispatching-rule and nearest-available-lab baselines, under real-time quoting constraints?**

*(Nếu template đang dùng có mục "3.3 Research Information" thì chuyển câu in đậm sau "Research
question" vào 3.3; bản trích phiếu này không có 3.3.)*

### f) Products (Expected Deliverables)

**[⬜ GIỮ 9/12 sản phẩm]** — chỉ 3 gạch thay đổi:

- *Central Hub Console* — 🟨 SỬA cụm cuối:
  > …reprint initiation, order consolidation, packing and ~~shipment tracking~~
  **manual shipment recording**.
- *Assignment and Scheduling Engine* — 🟨 SỬA cụm giữa:
  > …backward due-date derivation, machine-level placement ~~with consolidation~~
  **(consolidation: extended)**, time-budgeted improvement search…
- *Simulation Harness* — 🟩 THÊM câu:
  > **The harness is delivered by week 4 and serves as the integration testbed thereafter.**

### g) Proposed Tasks

**[⬜ GIỮ]** 5 WP, nguyên phân công — không đổi thành viên → không phải thay đổi lớn, không báo Khoa.
🟩 **[THÊM v1.1 — 2 mệnh đề]**
- WP1 → thêm: **…including securing and documenting the partner-lab working agreement before
  week 8, with an in-house mini-lab trial as the fallback.**
- WP5 → sửa cụm cuối: ~~integration testing, trial execution and documentation~~ →
  **simulation-harness delivery by week 4, integration testing, trial execution and documentation.**

## 4. Other comments

**[⬜ GIỮ nguyên văn]** 6 gạch định hướng của GVHD (scheduler is the contribution… / a delivery date
quoted from a lead-time table… / failure and reprint belong… / without a simulator… / one real lab…
/ minimum viable scope…) — đây là text của thầy, nhóm **không sửa một chữ**.

🟩 **[THÊM v1.1 — dán 2 khối dưới vào CUỐI mục 4, sau gạch "Minimum viable scope…"]**

> **Out of scope (v1.1, each with reason class):**
> - Lab settlement & invoicing — *third-party/organizational dependency*: paying labs for completed work
>   requires school finance procedures; the system computes and records payable amounts (BR-PAY-002/003),
>   disbursement runs offline. (Customer-side payment IS in scope per decision 23/09: real gateway,
>   token-only storage, refund + daily reconciliation — FR-CUST-008, FR-ANAL-005, NFR-SEC-009.)
> - Carrier API integration — *third-party dependency*: no early sandbox access; fulfillment staff
>   record waybill numbers manually.
> - Non-FDM technologies (resin, SLS, metal) — *exceeds resources*: the capability matrix per
>   technology doubles the filter scope; FDM suffices to prove the mechanism.
> - Batch consolidation, standing-in-score, automatic calibration regression, in-product
>   improvement search — *deferred, already designed for*: hooks exist in schema and configuration;
>   enabled after the MVP runs (matches the Extended scope list in the supervisor's own comment
>   above).
> - Fair-distribution exploration — *deferred*: needs long simulation runs to tune.
> - Native mobile apps for lab or customer — *exceeds resources*: responsive web covers the tablet
>   shop-floor case (NFR 10).
>
> **Principal risks and contingencies (v1.1):**
> - Partner lab not secured (early signal: no MOU by week 6) — fallback: in-house 1–2-machine
>   mini-lab trial; limitations stated in the final report.
> - Headless slicer too slow or unstable (signal: pilot failure rate > 20% or slice > 2 min on 20
>   sample files) — pre-slice caching keyed by mesh hash; asynchronous quoting UX (NFR 1); report
>   engine change to supervisor.
> - No team experience with scheduling metaheuristics — dispatching rules are the product spine;
>   simulated annealing/tabu run offline for evaluation only.
> - Customer IP exposure — signed short-lived links, full access log, adversarial test
>   (Practical bullet 11).
> - Integration consuming the second half of the semester — simulation harness from week 4 as the
>   standing integration testbed.

---

## Bản đồ vị trí — tóm tắt để không dán lệch

| Vị trí trên phiếu gốc | Hành động | Số khối |
|------------------------|-----------|---------|
| 1, 2, 3.1, tên GVHD/SV | ⬜ chép nguyên | — |
| a) Context — sau gạch cuối | 🟩 chèn 3 đoạn (Evidence / Existing systems / Numbers⚠) | 3 |
| b) — gạch 1,3,4,5,6,7,9 | 🟨 sửa 4 · 🟩 thêm 2 · tổng 7 chỉnh | 7 |
| b) — sau gạch 10 | 🟩 1 dòng chốt phạm vi | 1 |
| c) — Customer / Engine / SysAdmin | 🟨 3 gạch · ⚠ 1 · còn lại ⬜ | 4 |
| d) — 12 gạch | 🟩 chèn mệnh đề ngoặc vuông vào 10 gạch (⚠ 4 gạch chờ) | 10 |
| e) — cuối Practical | 🟩 1 đoạn (Research question) | 1 |
| f) — Hub/A&S/Simulator | 🟨 2 · 🟩 1 | 3 |
| g) — WP1 / WP5 | 🟩 2 mệnh đề | 2 |
| 4. Other comments — 6 gạch thầy viết | ⬜ tuyệt đối giữ | — |
| 4. — cuối mục | 🟩 Out of scope + Risks | 2 |
| Đầu file | 🟩 bảng Record of Changes (lấy trong `Phieu_FA26SE249_v1.1-DRAFT.md` §0) | 1 |

**Trước khi nộp:** chạy checklist 10 mục cuối `Phieu_FA26SE249_v1.1-DRAFT.md`; mọi ⚠ phải thành
quyết định có chữ thầy Phúc — phiếu còn ⚠ tức là chưa nộp.
