# PrintGrid Documentation — 2 nhánh

Thư mục chia làm hai dòng tài liệu riêng, đúng tính chất hồ sơ Capstone:

```
documentation/
├── project/    ← TÀI LIỆU KỸ THUẬT DỰ ÁN: specs để dev + trưởng nhóm tham chiếu hằng ngày
│   ├── overview/        (01–03)  bối cảnh, stakeholders, vấn đề
│   ├── requirements/    (04–13)  solutions, scope, BR, FR, NFR, User Stories, Use Cases, AC
│   └── design/          (14–17)  kiến trúc, module, ERD, API + sơ đồ drawio
└── capstone/   ← TÀI LIỆU ĐỀ TÀI/BÁO CÁO: thứ mang nộp Khoa, hội đồng, thầy Phúc
    ├── register/        phiếu đăng ký v1.0 (docx + trích), bản draft & markup v1.1
    ├── reports/         Report 2 (PMP), Report 5 (Test), Report 6 (User Guide), slides
    ├── planning/        Project-Schedule, ROADMAP 3 tuần
    ├── references/      nguồn tài liệu học thuật
    └── workbook/        quy trình 12 bước requirements (audit trail B1→B12)
```

**Quy tắc chọn nhánh khi thêm file mới:** trả lời câu "file này hội đồng/Khoa đọc hay dev đọc?" —
hội đồng → `capstone/`, dev → `project/`. Tài liệu `project/requirements/*` là **nguyên liệu** của
Report 3 (SRS) và Report 4 (SDD): khi viết 2 report đó, trích + tái cấu trúc từ đây, không viết lại từ đầu.

---

## 1. `capstone/` — hồ sơ đề tài (nộp)

### register/ — phiếu đăng ký theo phiên bản
| File | Vai trò | Trạng thái |
|------|---------|-----------|
| `Phieu_FA26SE249.docx` | **v1.0 baseline do Nhà trường duyệt — không bao giờ sửa đè** | ✅ bất biến |
| `Phieu_FA26SE249-extracted.md` | Bản trích text phục vụ đối chiếu/mã P-xx | ✅ |
| `Phieu_FA26SE249_v1.1-DRAFT.md` | Nội dung phiếu v1.1 + Record of Changes A/M/D, checklist nộp | ⏩ chờ B2 chốt ⚠ |
| `Phieu_FA26SE249_v1.1-MARKUP.md` | Phiếu gốc format lại, đánh dấu ⬜ GIỮ / 🟩 THÊM / 🟨 SỬA / ⚠ CHỜ đúng vị trí để dán vào docx | ⏩ như trên |

### workbook/ — quy trình 12 bước (tài liệu tham khảo GVHD v1.2)
| File | Bước | Nội dung |
|------|------|----------|
| **`FA26SE249_Requirements-Workbook_v1.xlsx`** | tổng hợp | 📊 **File Excel nộp theo Phụ lục A** — 8 sheet: README + 01_Extract (90 dòng P-xx) + 02_Assumptions + 04_BusinessRules (76) + 06_FR (43) + 07_NFR (16) + 05_UseCases (28) + 08_UserStories (44). Sinh tự động từ 5 bảng §0 trong các file md — build lại bằng script khi md đổi |
| `01_Extract-P-xx.md` | B1 | 90 dòng P-xx (mã phủ P-01→P-105, sản phẩm/WP gộp dãy) từ phiếu 3.2a–g + mục 4, cột Chưa rõ, câu hỏi, 8 giả định |
| `02_Phan-bien-de-tai.md` | B2 | Biên bản phản biện nháp: giả định nền/bằng chứng, so sánh hệ thống, trần ~210 ngày công, 13 quyết định trụ, rủi ro |
| `03_Audit-12-buoc.md` | — | Đối chiếu cả hồ sơ với 12 bước; tự chấm Phụ lục B; lộ trình vá |
| `04_Bien-ban-hop-GVHD.md` | B2→B3 | **File mang vào họp**: A1–A13 trụ + B1–B17 chính sách + C1–C7 xác nhận + ô chữ ký (đã chốt B1 tiền thật, B3 lab, B4, B14, B16 ngày 23/09) |
| `07_Traceability.md` | B12 | Ma trận Phiếu→FR→BR→Entity→Actor→UC + khoảng trống phát hiện + **ghi chú kiểm kê code 25/09 (0/18/25)** |
| **`../planning/PrintGrid-Ke-hoach-1-Trang.xlsx`** | kế hoạch | 📊 **BẢN DÙNG CHÍNH — một trang duy nhất**: 54 dòng xếp theo Tuần 1→8; mỗi dòng = chức năng + "cụ thể làm gì, xong thấy gì" (ý WBS ghi thẳng, không trỏ ID) + BR + NFR + công + dropdown người & tiến độ; kèm sheet "Tải 4 người" tự cộng. TỔNG 172d vs trần ~160 — các dòng cắt được đánh dấu "Nên làm / Có thể bỏ" |
| `../planning/Plan-ToanDu-An-v2.xlsx` · `Plan-3Tuan-TuChon.xlsx` | (lịch sử) | các bản 5–6 sheet trước khi gom về 1 trang — giữ làm tham chiếu, không cập nhật nữa |

### reports/ · planning/ · references/
`Report2` (quản lý dự án) · `Report5` (test) · `Report6` (hướng dẫn dùng) · `Presentation-Slides` (Review) · `Project-Schedule` + `ROADMAP-3-weeks` (kế hoạch nội bộ) · `References` (trích nguồn học thuật — vào mục e của report).

## 2. `project/` — tài liệu kỹ thuật (dev đọc hằng ngày)

### overview/
- [01-Context.md](project/overview/01-Context.md) — ngành, bài toán gốc, vì sao đáng làm
- [02-Stakeholders.md](project/overview/02-Stakeholders.md) — 13 nhóm liên quan, needs
- [03-Problems-Challenges.md](project/overview/03-Problems-Challenges.md) — 35+ vấn đề & challenge kỹ thuật, có phân mức ưu tiên

### requirements/  ⟶ nguyên liệu Report 3 (SRS)
- [04-Proposed-Solutions.md](project/requirements/04-Proposed-Solutions.md) — 11 thành phần giải pháp + thuật toán
- [05-Main-Features.md](project/requirements/05-Main-Features.md) — catalog feature theo 6 vai
- [06-System-Scope.md](project/requirements/06-System-Scope.md) — in/out scope, MVP vs Extended
- [07-Assumptions-Constraints.md](project/requirements/07-Assumptions-Constraints.md) — 38 giả định + 26 ràng buộc (Constraints **tách riêng khỏi NFR** — chuẩn B11)
- [08-Business-Rules.md](project/requirements/08-Business-Rules.md) — **74 BR**; §0 bảng phân loại chuẩn B6 (6 loại · cứng/mềm · biến động · nguồn P-xx · FR/UC thi hành); §13 12 rule mới; 4 rule chết cần quyết C2
- [09-Functional-Requirements.md](project/requirements/09-Functional-Requirements.md) — **42 FR**; §0 chỉ số MoSCoW/actor/Ư-L/UC/US; §7 4 FR ★ vá bullet ma; §8 patch phạm vi chờ B3
- [10-Non-Functional-Requirements.md](project/requirements/10-Non-Functional-Requirements.md) — **15 NFR chủ** đo được (§0, phủ 12/12 gạch d) của phiếu) + 28 engineering standards (giữ block chi tiết)
- [11-User-Stories.md](project/requirements/11-User-Stories.md) — **43 stories**, mỗi story có dòng `Traces: FR·UC·BR`; ma trận Story→FR→UC→BR
- [12-Use-Cases.md](project/requirements/12-Use-Cases.md) — **28 UC** (matrix §0 format nộp, tiếng Anh) đủ pre/post/main/exception + BR codes thật; UC-027 = Simulation Harness (order generator + fault injection), UC-028 = Refund & Reconciliation (payment 23/09)
- [13-Acceptance-Criteria.md](project/requirements/13-Acceptance-Criteria.md) — Gherkin, nguồn thẳng cho test case Report 5 (map TC vào 07_Traceability)

### design/  ⟶ nguyên liệu Report 4 (SDD)
- [14-System-Architecture.md](project/design/14-System-Architecture.md) — Modular Monolith + Clean Architecture, C4
- [15-Module-Design.md](project/design/15-Module-Design.md) — thiết kế module + DDD
- [16-Database-ERD.md](project/design/16-Database-ERD.md) — PostgreSQL schema-per-module (mức **Physical**; Review 1 cần bổ sung mức **Conceptual** — việc B8 trong audit)
- [17-API-Design.md](project/design/17-API-Design.md) — REST conventions, endpoint catalog
- `PrintGrid-CoreFlow.drawio` — sơ đồ luồng core (upload→quote→order)

---

## Conventions (không đổi)

| Mã | Nghĩa | Mã | Nghĩa |
|----|-------|----|-------|
| P-xx | dòng trích từ phiếu (B1) | BR-\<DOMAIN\>-NNN | business rule (08) |
| M01–M06 | phân hệ (07_Traceability) | FR-\<MODULE\>-NNN | functional req (09) |
| UC-NNN | use case (12) | NFR-\<CAT\>-NNN / N-xx | NFR (10) |
| US-NNN | user story (11) | TC-NNN | test case (Report 5) |

Priority: chuẩn báo cáo dùng **MoSCoW (Must/Should/Could/Won't)**; thang Critical/High/Medium cũ ánh xạ Critical→Must, High→Should, Medium→Could (ngoại lệ ghi ở 09 §0).
Quy trình sửa đổi: **nhóm trưởng là người duy nhất sửa phiếu/register**; mọi thay đổi có ý nghĩa → 1 dòng Record of Changes trỏ về 1 quyết định biên bản (`capstone/workbook/04`).

## Trạng thái đồng bộ hiện tại (09/2026)

- ✅ B1, B12 (workbook), requirements set v2 (08/09/10/11/12 nhất quán mã chéo, link thông)
- ⏩ B2→B3 chờ buổi họp GVHD (file `04_Bien-ban-hop-GVHD.md`) → xuất phiếu **v1.1 sạch** sau họp
- ❌ Việc kỹ thuật còn nợ theo audit: **B9 state tables (Job/Order/Quote)**, B8 Conceptual ERD + Entity List, actor map B4, phân loại Out-of-scope B5, điền TC vào 07
- ⚠ 4 rule chết (BR-PERF-006, BR-PAY-002/003/004) + câu hỏi P-67 + 6 ô ⚠ ngưỡng — chờ mục C2/C3/B4/B16 của biên bản họp
