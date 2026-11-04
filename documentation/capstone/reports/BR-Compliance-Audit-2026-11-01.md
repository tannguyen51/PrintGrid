# BR Compliance Audit — PrintGrid vs Business Rules (01/11/2026)

**Phạm vi:** 94 luật trong `documentation/project/requirements/08-Business-Rules.md` (BR-QUOTE, ASSIGN, SCHED,
RESCHED, ESTIM, OPS, PERF, ACCESS, STOCK, QC, PAY, CONFIG, LOG, NOTIFY) + FR-LAB-001..003 + scope 1 lab/manager.
**Cách rà:** đọc code trong `src/` và `frontend/`, đối chiếu từng luật; chỉ đánh "Implemented" khi tìm thấy
code thực thi, kèm `file:line`.
**Kết quả build:** `dotnet build` thành công, 0 lỗi. Migration `20261101143739_EnforceSingleLabPerOwner` đã apply.

---

## 1. Đã sửa trong đợt này

| # | Luật / quy tắc | Trước | Đã sửa |
|---|---|---|---|
| 1 | **Scope: 1 LabManager = 1 xưởng** (06-System-Scope §2.2) | DB cho phép nhiều lab/owner; `POST /labs` tạo được lab thứ 2; fallback gán lab vô chủ | Unique partial index `IX_labs_OwnerUserId ... WHERE OwnerUserId IS NOT NULL`; `RegisterLabCommandHandler` trả `409` nếu owner đã có lab; `GetMyLabQuery.AllowUnclaimedFallback` mặc định `false` |
| 2 | **BR-ACCESS-005 / bảo mật** | `POST /labs/self-register` chỉ có `[Authorize]` — mọi account đăng nhập đều tạo được lab | Thêm `[Authorize(Roles = "LabManager,LabOperator")]` |
| 3 | **BR-OPS-006** — duyệt lab cần khai báo đầy đủ | `Approve()` chỉ kiểm tra máy, **không** kiểm tra vật liệu | `Approve()` trả `Conflict` khi chưa có ≥1 `MaterialStock` |
| 4 | **BR-CONFIG-002** — mọi thay đổi cấu hình phải audit | Kích hoạt scoring weights không ghi audit | `ScoringConfigurationsController` ghi `AuditLogWriteRequest` (`scoring_configuration.activated`, before/after, reason, IP, trace) |
| 5 | **BR-ACCESS-002** — mọi lần tải file model phải log | Lab tải file theo job không ghi audit | `JobsController.GetJobFile` ghi audit `model.file_downloaded` (actor, role, job, IP, trace) |

**Tác động dữ liệu:** 4 lab đang có `OwnerUserId = NULL` vẫn giữ nguyên. Từ nay chúng **không còn tự động
gán** cho labmanager nào; chỉ Ops/Admin quản lý. Account chưa có lab phải vào `/lab/register`.

---

## 2. Tổng hợp

| Trạng thái | Số luật |
|---|---|
| Implemented | 42 (sau sửa: +3 → 45) |
| Partial | 39 (sau sửa: −3 → 36) |
| Missing | 11 |
| N/A (ngoài hệ thống / superseded) | 2 |
| **Tổng** | **94** |

---

## 3. Các luật CHƯA đạt — cần quyết định hoặc làm feature

### 3.1 Missing (không có code)

| Luật | Thiếu gì | Ghi chú |
|---|---|---|
| BR-ASSIGN-007 | Random exploration 5–10% | Matrix ghi **Deferred**, không phải MVP |
| BR-ASSIGN-009 | Category match là tiêu chí scoring | Cần thêm tag category cho Lab/Machine + trọng số |
| BR-ASSIGN-010 | Offer fan-out top-k, first valid accept wins | Hiện gán trực tiếp một lab; đây là thay đổi lớn (Review 1) |
| BR-SCHED-002/003/004 | Batch theo vật liệu/màu, ≤5 job/24h, urgent bypass | Matrix ghi Deferred→Should |
| BR-SCHED-009 | Handover trong 24h + cảnh báo quá hạn | Chưa có timer/alert |
| BR-QC-010 | Incident MISSING/DAMAGED + attribution trước khi reprint | Chỉ reject scan mismatch; chưa có entity Incident |
| BR-PERF-006 | Lab khiếu nại điểm, ops sửa | Matrix đánh dấu "dead rule — pending C2" |
| BR-PAY-004 | Platform-fault → full refund tự động | Chỉ có refund từ chối đổi ngày |
| BR-LOG-004 | Shipping là một dòng trong quote + ngưỡng lỗ | Quote/QuoteItem không có shipping line |

### 3.2 Partial (có nhưng chưa đủ)

| Luật | Khoảng cách |
|---|---|
| BR-QUOTE-004 | `ByVersion` chỉ biết 2 version hardcode rồi fallback Active → quote cũ không tái lập được |
| BR-QUOTE-005 | Breakdown chỉ có material + machine time; thiếu post-processing/hub/shipping |
| BR-QUOTE-007 | Slicing async nhưng quote vẫn sync, không có kênh báo hoàn tất |
| BR-ASSIGN-002 | Trọng số code 0.35/0.25/0.20/0.10/0.10 vs tài liệu 0.30/0.25/0.20/0.15/0.10 |
| BR-ASSIGN-005 | Cửa sổ 2h hardcode, chỉ kiểm tra lazy khi list job; không có job timeout |
| BR-ASSIGN-006 | Lý do từ chối chỉ cần non-empty, không có danh sách mã cố định |
| BR-SCHED-001 | Internal due = promised − 2 ngày cố định, không tách transit/packing/inspection |
| BR-SCHED-005 | Repair 30s có; budget quote 60s không tồn tại |
| BR-SCHED-011 | Chỉ đề xuất đổi ngày khi AttemptNumber ≥ 2 |
| BR-RESCHED-001 | Chỉ decline + print failure phát event; breakdown/reprint chưa nối |
| BR-ESTIM-001 | Actual material không bắt buộc, fallback reserved grams |
| BR-ESTIM-002 | Factor theo MachineId, không pool theo model máy |
| BR-ESTIM-004 | Calibration áp khi schedule, nhưng quote vẫn dùng estimate thô |
| BR-ESTIM-005 | MAPE có tính, ngưỡng 25% không có consumer → không alert |
| BR-OPS-003 | Suspend chỉ chặn assignment mới, không reassign job ASSIGNED |
| BR-OPS-008 | Simulator seed-reproducible nhưng không pin protocol/version |
| BR-PERF-001 | 90 ngày chỉ tính lúc đọc dashboard; metrics không cập nhật theo sự kiện |
| BR-PERF-002 | Neutral 0.70 chỉ ở DTO dashboard; assignment dùng rate thô |
| BR-PERF-003 | Scorer dùng trung bình on-time/first-pass, không dùng công thức Standing; metrics không refresh |
| BR-PERF-005 | Không có detector <0.50 trong 30 ngày |
| BR-ACCESS-001 | +4h chỉ áp sau khi Completed; không có pre-signed URL hết hạn |
| BR-ACCESS-005 | Domain ép 1 role nhưng cột `text[]` ở DB không ràng buộc |
| BR-STOCK-001 | Fault settle reserved grams thay vì release; lab không theo dõi vật liệu thì no-op |
| BR-STOCK-002 | Tolerance hardcode 0.03, không đọc config (guard 1–5% có) |
| BR-QC-002 | Không verify checklist đúng quality grade; grade suy ra từ layer height |
| BR-QC-003 | Ảnh bắt buộc nhưng không persist vào inspection record (chỉ phát event) |
| BR-QC-005 | CostBearer ghi trên reprint, không có payout để trừ tiền lab |
| BR-QC-006 | `CustomerFaultInspectionFailedEvent` không có handler → không gửi notify |
| BR-PAY-001 | Order commit ở ngưỡng cọc 50%, không phải full payment (theo BR-PAY-007) |
| BR-PAY-003 | Chỉ ghi cost bearer, không có payout deduction |
| BR-PAY-005 | Không có gateway/tokenised flow; payment do ops ghi tay |
| BR-PAY-006 | Chỉ set Approved; không execute gateway, không SLA 5 ngày, không idempotency key |
| BR-CONFIG-004 | Append-only nhưng không có retention 2 năm |
| BR-LOG-001 | Cửa sổ merge 24h hardcode |
| BR-LOG-003 | Zone rate đọc từ app config lúc start, không versioned; không settlement payout |
| BR-NOTIFY-001 | Chỉ upload + reprint-request queue email; QC fail/assignment không có handler |
| BR-NOTIFY-002 | Đổi ngày cần duyệt (đúng) nhưng không gửi email đề xuất |

---

## 4. Đã đạt (danh sách ID)

BR-QUOTE-001, 002, 003, 006, 008 · BR-ASSIGN-001, 002, 003, 004 · BR-SCHED-006, 007, 008, 010 ·
BR-RESCHED-002, 003, 004 · BR-ESTIM-003, 006 · BR-OPS-001, 002, 004, 005, 007 · BR-PERF-004 ·
BR-ACCESS-003, 004, 005, 006 · BR-STOCK-003 · FR-LAB-002 · Scope 1 lab/manager ·
BR-QC-001, 004, 007, 008, 009, 011, 012 · BR-CONFIG-001, 003 · BR-LOG-002 ·
+ 3 luật vừa sửa: BR-OPS-006, BR-CONFIG-002, BR-ACCESS-002.

---

## 5. Đề xuất thứ tự xử lý

1. **BR-ACCESS-001** — pre-signed URL hết hạn theo job + 4h (thay vì stream không hạn) — rủi ro IP cao.
2. **BR-QC-006** — handler gửi email khi lỗi do khách — đang hứa với khách mà không gửi.
3. **BR-ESTIM-002/004** — calibration theo model máy + áp vào quote — ảnh hưởng độ chính xác giá/ngày.
4. **BR-QUOTE-005 + BR-LOG-004** — thêm dòng shipping vào breakdown — khớp FR-CUST-006.
5. **BR-ASSIGN-010** — offer fan-out top-k — thay đổi lớn, cần chốt với GVHD trước.
