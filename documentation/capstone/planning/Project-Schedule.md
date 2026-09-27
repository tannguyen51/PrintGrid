# Project Schedule — PrintGrid (điền theo mẫu Report2_Sample Project Schedule)

> Bảng theo đúng cột mẫu FPT: **ID · Name · Duration · Start · Finish · Predecessors · Resource Names · Notes**.
> Ngày làm việc Thứ 2–6; bắt đầu **07/09/2026 (Thứ 2)** → kết thúc **10/03/2027** — khớp "9/2026 → 3/2027" trong phiếu và bảng Deliverables của Report 2.
> **Resource**: WP1–WP5 theo Phieu (mục 3.2g) — khi nộp thay bằng tên thật từng thành viên.
> Duration = số ngày làm việc (không tính cuối tuần).

## Stage 1 — Initiation (tuần 1)

| ID | Name | Duration | Start | Finish | Predecessors | Resource Names | Notes |
|---|---|---|---|---|---|---|---|
| 1 | Project Start | 0 days | 07/09/26 08:00 | 07/09/26 17:00 | | | |
| 2 | **Initiation (Stage 1)** | 5 days | 07/09/26 08:00 | 11/09/26 17:00 | | All | Week 1 |
| 3 | Prepare Report 1 (Project Introduction) | 5 days | 07/09/26 08:00 | 11/09/26 17:00 | | WP1 | Summary from registration form |
| 4 | Deliver Report 1 (Project Introduction) | 0 days | 11/09/26 17:00 | 11/09/26 17:00 | 3 | WP1 | |

## Stage 2 — Planning & Requirement (tuần 2–4)

| ID | Name | Duration | Start | Finish | Predecessors | Resource Names | Notes |
|---|---|---|---|---|---|---|---|
| 5 | **Planning & Requirement (Stage 2)** | 13 days | 14/09/26 08:00 | 30/09/26 17:00 | | All | Week 2–4 |
| 6 | Project Planning | 13 days | 14/09/26 08:00 | 30/09/26 17:00 | 4 | WP1 | |
| 7 | Requirement Analyzing (44 FR, 43 NFR, 71 BR) | 13 days | 14/09/26 08:00 | 30/09/26 17:00 | 4 | WP1+WP2 | From documents 01–13 |
| 8 | Prepare Project Management Plan (Report 2) v0.9 | 13 days | 14/09/26 08:00 | 30/09/26 17:00 | 7 | WP1 | Deliverable #2 (30/09) |
| 9 | Deliver Report 2 (Project Management Plan) | 0 days | 30/09/26 17:00 | 30/09/26 17:00 | 8 | WP1 | |
| 10 | Technical Training / Environment Setup (Docker, .NET 8, React, Postgres, MinIO) | 10 days | 14/09/26 08:00 | 25/09/26 17:00 | 4 | All | Training Plan §2.3 |
| 11 | Create Technical Prototype (auth + model upload + quote mock) | 10 days | 14/09/26 08:00 | 25/09/26 17:00 | 10SS | WP3+WP4 | De-risk stack |
| 12 | Define architecture, DB schema & API contract (OpenAPI/ERD) | 8 days | 21/09/26 08:00 | 02/10/26 17:00 | 11 | WP3+WP2 | Deliverable #3 (15/10) |

## Stage 3 — Software Designing (tuần 4–5)

| ID | Name | Duration | Start | Finish | Predecessors | Resource Names | Notes |
|---|---|---|---|---|---|---|---|
| 13 | **Software Designing (Stage 3)** | 10 days | 05/10/26 08:00 | 16/10/26 17:00 | | WP3+WP2 | Week 4–5 |
| 14 | Write SRS v0.9 (final) | 8 days | 05/10/26 08:00 | 15/10/26 17:00 | 7 | WP1 | Deliverable #3 |
| 15 | Write SDS (System Design: architecture, ERD, API design) | 8 days | 05/10/26 08:00 | 15/10/26 17:00 | 12 | WP3+WP2 | Docs 14–17 |
| 16 | Deliver Report 3 (SRS v0.9) | 0 days | 15/10/26 17:00 | 15/10/26 17:00 | 14 | WP1 | |
| 17 | Deliver Report 4 (SDS + Architecture + ERD + API) | 0 days | 15/10/26 17:00 | 15/10/26 17:00 | 15 | WP1 | |

## Stage 4 — Implementation × 3 Iterations (tuần 5–13)

| ID | Name | Duration | Start | Finish | Predecessors | Resource Names | Notes |
|---|---|---|---|---|---|---|---|
| 18 | **Implementation (Stage 4)** | 44 days | 05/10/26 08:00 | 04/12/26 17:00 | | All | Week 5–13, MVP first |
| 19 | Code Iteration 1 — **Customer core flow** (upload → analyze → quote → order) | 15 days | 05/10/26 08:00 | 23/10/26 17:00 | 16;17 | WP3+WP4 | Deliverable #4a |
| 20 | Update SRS & SDS (Iteration 1) | 5 days | 05/10/26 08:00 | 09/10/26 17:00 | 16 | WP1 | |
| 21 | Create Test Cases (Iteration 1) | 5 days | 07/10/26 08:00 | 13/10/26 17:00 | 20SS+2 days | WP5 | |
| 22 | Code, UT, IT (Customer module + geometry/slicing) | 12 days | 07/10/26 08:00 | 23/10/26 17:00 | 20SS+2 days | WP3+WP4 | |
| 23 | Complete Software Package 1 | 0 days | 23/10/26 17:00 | 23/10/26 17:00 | 21;22 | | |
| 24 | **Code Iteration 2 — Scheduling Engine ⭐** (capability filter, scoring, backward scheduling, speculative quote placement) | 15 days | 26/10/26 08:00 | 13/11/26 17:00 | 23 | WP2+WP3 | Deliverable #4b — core contribution |
| 25 | Build Simulation Harness + lab simulator (fault injection) | 20 days | 26/10/26 08:00 | 20/11/26 17:00 | 23SS | WP5 | Built early, not as afterthought |
| 26 | Update SRS & SDS (Iteration 2) | 5 days | 26/10/26 08:00 | 30/10/26 17:00 | 23 | WP1 | |
| 27 | Create Test Cases (Iteration 2) | 5 days | 28/10/26 08:00 | 03/11/26 17:00 | 26SS+2 days | WP5 | |
| 28 | Code, UT, IT (Scheduling module domain services + API) | 12 days | 28/10/26 08:00 | 13/11/26 17:00 | 26SS+2 days | WP2+WP3 | |
| 29 | Complete Software Package 2 | 0 days | 13/11/26 17:00 | 13/11/26 17:00 | 27;28 | | |
| 30 | **Code Iteration 3 — Lab + Hub + event-driven reschedule** | 15 days | 16/11/26 08:00 | 04/12/26 17:00 | 29 | WP3+WP4+WP5 | Deliverable #5 |
| 31 | Update SRS & SDS (Iteration 3) | 5 days | 16/11/26 08:00 | 20/11/26 17:00 | 29 | WP1 | |
| 32 | Create Test Cases (Iteration 3) | 5 days | 18/11/26 08:00 | 24/11/26 17:00 | 31SS+2 days | WP5 | |
| 33 | Code, UT, IT (Lab portal, Hub QC + reprint, rescheduling events) | 12 days | 18/11/26 08:00 | 04/12/26 17:00 | 31SS+2 days | WP3+WP4+WP5 | |
| 34 | Complete Software Package 3 | 0 days | 04/12/26 17:00 | 04/12/26 17:00 | 32;33 | | |

## Stage 5 — Verification + Extended Scope (tuần 14–20)

| ID | Name | Duration | Start | Finish | Predecessors | Resource Names | Notes |
|---|---|---|---|---|---|---|---|
| 35 | **Verification + Extended (Stage 5)** | 27 days | 07/12/26 08:00 | 15/01/27 17:00 | | All | Week 14–18, feature freeze at W14 |
| 36 | Execute System Test & Fix Bugs | 10 days | 07/12/26 08:00 | 18/12/26 17:00 | 34 | WP5+All | |
| 37 | Extended scope: calibration loop, performance ledger, dashboards, model library | 15 days | 21/12/26 08:00 | 15/01/27 17:00 | 34 | WP2+WP3+WP4 | Deliverable #7 (15/02) partial |
| 38 | Simulation campaign: algorithm vs random & nearest-lab baselines | 15 days | 21/12/26 08:00 | 15/01/27 17:00 | 34;25 | WP2+WP5 | Weighted tardiness, on-time rate, load spread |
| 39 | Deliver Report 5 (Test Documentation) | 0 days | 15/01/27 17:00 | 15/01/27 17:00 | 36;37;38 | WP5+WP2 | Test docs + simulation results |

## Stage 6 — Transition + Partner Trial + Final (tuần 19–26)

| ID | Name | Duration | Start | Finish | Predecessors | Resource Names | Notes |
|---|---|---|---|---|---|---|---|
| 40 | **Transition (Stage 6)** | 30 days | 18/01/27 08:00 | 10/03/27 17:00 | | All | Week 19–24 + buffer |
| 41 | Real-lab trial with partner printing lab (calibration data, shop-floor UI test) | 12 days | 18/01/27 08:00 | 02/02/27 17:00 | 39 | WP2+WP4+WP5 | Deliverable #8 (28/02) |
| 42 | Prepare User Guides (Customer, Lab, Hub, Operations) | 10 days | 18/01/27 08:00 | 29/01/27 17:00 | 39 | WP1+WP4 | Deliverable #9 (10/03) |
| 43 | Software Optimization / Refactor | 5 days | 01/02/27 08:00 | 05/02/27 17:00 | 41 | WP3 | |
| 44 | Execute UAT & Fix Bugs | 5 days | 08/02/27 08:00 | 12/02/27 17:00 | 43 | WP4+WP5 | |
| 45 | Final integration & regression | 5 days | 15/02/27 08:00 | 19/02/27 17:00 | 44 | All | |
| 46 | Deliver Report 6 (Software User Guides) | 0 days | 19/02/27 17:00 | 19/02/27 17:00 | 42;45 | WP1 | |
| 47 | Deploy Final Code + Demo environment | 0 days | 05/03/27 17:00 | 05/03/27 17:00 | 45 | WP3+WP4 | |
| 48 | Prepare Thesis Presentation | 4 days | 01/03/27 08:00 | 05/03/27 17:00 | 46 | All | |
| 49 | Deliver Report 7 (Final Project Report) + Final Demo | 0 days | 10/03/27 17:00 | 10/03/27 17:00 | 47;48 | All | Deliverable #9 |

---
**Ghi chú bổ sung khi nộp:**
- Cột **Notes/Week** map tuần theo ROADMAP trong docs; nếu trường chấm theo mốc cố định (Report 1, 2, … 7) thì giữ nguyên cột Deliver Report.
- **Predecessors** dùng đúng ID của bảng (kiểu `31SS+2 days` như mẫu) → có thể re-import vào MS Project để tự vẽ Gantt.
- **Feature freeze tuần 14** (04/12/2026) khớp luật trong Report 2 §2.1.