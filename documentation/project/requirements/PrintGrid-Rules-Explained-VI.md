# Giải thích toàn bộ BR · FR · NFR — PrintGrid (bản tiếng Việt dễ hiểu)

Tài liệu học nội bộ: giải thích **76 Business Rule, 43 Functional Requirement, 16 Non-Functional Requirement cốt lõi** bằng tiếng Việt bình dân — mỗi mục kèm *quy định gì* và *nếu bỏ nó thì chuyện gì xảy ra*. Nguồn chân lý vẫn là `08/09/10-*.md`; đây là bản rút gọn để đọc-hiểu và trả lời hội đồng.

## Cách đọc ký hiệu trong bảng gốc (để khỏi hỏi lại)

| Ký hiệu | Nghĩa |
|---|---|
| Loại `C·K·E·T·F·H` | Constraint (ràng buộc cứng) · Knowledge (sự kiện đã biết) · Event-action (có biến thì làm gì) · Timing (mốc thời gian) · Fact (định nghĩa) · Invariant (bất biến — vi phạm là bug, không phải ngoại lệ) |
| `cfg(2h)` | tham số **cấu hình được**, mặc định trong ngoặc — không hard-code |
| `·UC-004` | rule này được thi hành trong use case nào |
| ⚠B5, ⚠B12… | còn chờ xác nhận GVHD (mã buổi họp trong `capstone/workbook/04`) |

---

# PHẦN 1 — 76 BUSINESS RULES (luật nghiệp vụ)

## Nhóm QUOTE (7) — báo giá phải thật

- **QUOTE-001** — Mỗi cấu hình in có **một giá duy nhất**, xưởng nào in cũng vậy. *Bỏ thì:* lộ giá theo xưởng → quay về mặc cả → chết thương hiệu (vấn đề P5).
- **QUOTE-002** — Ngày giao lấy từ **đặt lịch thử trên năng lực thật**, cấm tra bảng lead-time tĩnh. *Bỏ thì:* mất luôn hồn đề tài (P2).
- **QUOTE-003** — Báo giá hết hạn sau 48h (cấu hình được). Mạng lưới đổi từng giờ; báo giá cũ là báo giá sai. ⚠B12 chờ thầy chốt con số.
- **QUOTE-004** — Báo giá **đóng băng phiên bản tham số giá** đã dùng. Tuần sau đổi bảng giá, báo giá hôm qua vẫn tái lập từng đồng → hết tranh cãi "sao hôm qua rẻ hơn".
- **QUOTE-005** — Mọi báo giá hiển thị **dòng chi phí tách nhỏ** (vật liệu, thời gian, hậu xử lý…). Minh bạch = niềm tin.
- **QUOTE-006** — Chi tiết **không máy nào in được** bị chặn ngay từ báo giá, trước khi trả tiền. Không bán cái bất khả thi.
- **QUOTE-007** — Báo giá nặng chạy **bất đồng bộ kèm tiến độ**, UI không bao giờ treo chờ slicing.

## Nhóm ASSIGN (8) — gán việc

- **ASSIGN-001** — Chỉ gán cho máy thỏa **toàn bộ** ràng buộc cứng (công nghệ, volume, dung sai, tồn kho, trạng thái). Khả thi là correctness, không phải sở thích.
- **ASSIGN-002** — Trong tập khả thi, chọn bằng **điểm tổng hợp có trọng số** (thời gian/tải/chất lượng/giá/logistics). *Bỏ thì:* chọn bừa, hỏng cả mạng — tổng trọng số luôn = 1.0, cùng input cho ra cùng score.
- **ASSIGN-003** — Trọng số **đổi được không cần deploy** — mỗi học kỳ ưu tiên khác nhau.
- **ASSIGN-004** — Mọi quyết định gán ghi **nhật ký: tập ứng viên + điểm từng tiêu chí + lựa chọn** → xử tranh chấp, debug thuật toán.
- **ASSIGN-005** — Xưởng phải trả lời trong **2h**, im lặng = tự động từ chối → khỏi kẹt lịch. ⚠B5 chờ duyệt.
- **ASSIGN-006** — Từ chối phải **chọn lý do từ danh sách cố định** → phát hiện lọc khả thi có lỗ hoặc xưởng "cherry-pick".
- **ASSIGN-007** — (Hoãn/ngoài phạm vi) Một tỷ lệ nhỏ (~5–10%) ca gán **ngẫu nhiên trong nhóm khả thi** để xưởng mới có dữ liệu, chống winner-take-all.
- **ASSIGN-008** — Xưởng **không được quyền chọn job**; hệ thống gán. *Bỏ:* job xấu bị bỏ đói — bất biến thiết kế.

## Nhóm SCHED (9) — lập lịch

- **SCHED-001** — Deadline nội bộ suy **ngược từ ngày giao của khách** (trừ vận chuyển, kiểm, hậu xử lý).
- **SCHED-002/003** — Job cùng vật liệu+màu được **gom in một máy** (tiết kiệm 15–30 phút đổi màu), nhưng batch **≤5 job / ≤24h** để không chặn việc gấp. (A4: hạ xuống Should.)
- **SCHED-004** — Job **gấp (deadline <48h) vượt quyền gom batch** — deadline thắng hiệu suất.
- **SCHED-005** — Tính lịch có **ngân sách thời gian cứng**: sửa ≤30s, báo giá ≤60s (đã chốt B4 ngày 23/09). Máy không chờ kế hoạch.
- **SCHED-006** — Hết giờ vẫn phải có **kế hoạch fallback theo quy tắc đơn giản** — thà kém tối ưu còn hơn không có lịch.
- **SCHED-007** — **Không đụng job đang in / đã xong** — lịch sử không sửa được giữa chừng.
- **SCHED-008** — Ngày giao đã cam kết chỉ đổi theo **3 điều kiện đã duyệt + khách đồng ý** (B16). Niềm tin chính là sản phẩm.
- **SCHED-009** — Chi tiết in xong phải **giao xe lên hub trong 24h** — ống dẫn chất lượng không được nghẽn tại sân xưởng.

## Nhóm RESCHED (4) — sửa lịch khi biến

- **RESCHED-001** — Có biến (từ chối/hỏng/chết máy/QC fail) → **tự động** sửa lịch; đàm phán tay quá chậm.
- **RESCHED-002** — Chỉ sửa **những job liên quan** — chống "schedule nervousness" khiến thợ mất lịch ổn định.
- **RESCHED-003** — Job in lại **nhãn URGENT, thừa hưởng deadline gốc** — khách không biết có sự cố thì không được trễ hơn.
- **RESCHED-004** — Trùng **2 lần in lại → leo thang ops** (đã chốt B14) — lỗi lặp là lỗi hệ thống, không đổ thêm chi phí.

## Nhóm QC (10) — chất lượng tại hub

- **QC-001** — **Mọi** chi tiết đều qua kiểm tại hub trước khi tới khách — nền tảng bán "bảo chứng", không bán "hy vọng".
- **QC-002** — Kiểm theo **checklist gắn với grade khách mua**, có phiên bản → nói được vì sao hàng "Standard" khác "Premium".
- **QC-003** — Mỗi kết quả kiểm có **ảnh bằng chứng** — cãi nhau bằng ảnh, không bằng lời.
- **QC-004** — Hàng lỗi phải **quy trách nhiệm** (LAB/HUB/LỖI-FILE-KHÁCH) trước khi làm tiếp — tiền và điểm theo người có lỗi.
- **QC-005/006/007** — Ba nhánh chi phí: lỗi tại xưởng → in lại **trừ vào công xưởng**; lỗi tại file khách → **hỏi khách** rồi mới in (tiền khách chịu); lỗi tại hub → **nền tảng trả**. Không vùng xám.
- **QC-008** — **Chặn đóng gói** khi còn item chưa qua kiểm — không ship đơn khiếm khuyết hoặc thiếu.
- **QC-009** — **Bảo hành 30 ngày tính từ lúc xác nhận delivered** — hết thời hạn tính được, không khiếu nại vô thời hạn.
- **QC-010** — Lô **thiếu/hư hại vận chuyển** → incident + quy trách trước khi in lại — không mất tiền/im lặng đổ lỗi.

## Nhóm PERF (6) — điểm tín nhiệm xưởng

- **PERF-001** — Điểm tính trên **cửa sổ trượt 90 ngày** — trung bình trọn đời che mắt sự sa sút gần đây.
- **PERF-010** — Dưới **10 job hoàn thành** chưa tính điểm, tạm gán trung tính 0.70 → xưởng mới bị "đói việc" vì cold start. ⚠B10.
- **PERF-003** — Điểm tín nhiệm **nạp thẳng vào công thức gán việc** (A9: MVP để Should) — làm tốt mới được việc ngon.
- **PERF-004** — Xưởng **xem được điểm của mình** + so sánh mạng lưới đã ẩn danh — chấm điểm mà giấu đáp án thì chỉ gây ấm ức và khiếu nại.
- **PERF-005** — Điểm **<0.50 suốt 30 ngày → tạm dừng nhận job mới** chờ rà soát — cắt nguồn bệnh trước khi lan sang khách.
- **PERF-006** — Xưởng được **khiếu nại số liệu sai**; ops điều tra, đính chính. ⚠ Đây là **dead rule duy nhất** — chưa có FR nào thi hành; phương án hiện tại: khiếu nại qua email + runbook (chờ C2).

## Nhóm ESTIM (6) — hiệu chuẩn ước lượng

- **ESTIM-001** — Thợ in **bắt buộc khai thời gian + gam vật liệu thực tế** khi xong job — không có ground truth thì không bao giờ giỏi hơn.
- **ESTIM-002** — Hệ số hiệu chuẩn tính **theo từng dòng máy**, không dùng một số toàn mạng — mỗi đời máy một tính nết.
- **ESTIM-003** — Loại **outlier** (thực tế >3× hoặc <0.3× dự đoán) khỏi hồi quy — một lần chết máy không được đổi giá mọi báo giá.
- **ESTIM-004** — Hệ số chỉ áp **cho tương lai** — báo giá/cam kết cũ không bị hồi tố sửa.
- **ESTIM-005** — **MAPE mỗi dòng máy** được giám sát, vượt 25% → cảnh báo — độ chính xác phải được đo, không được tin.
- **ESTIM-006** — Đề xuất hệ số cần **≥10 mẫu hợp lệ** cho đúng dòng máy đó — mẫu ít là nhiễu.

## Nhóm ACCESS (6) — IP & phân quyền

- **ACCESS-001** — File model chỉ tải được trong **thời gian job + 4h dự trù** — xưởng không gom thư viện thiết kế của khách. ⚠B15.
- **ACCESS-002** — **Mọi** lượt cấp URL/lượt tải ghi log (ai, lúc nào, job nào, IP) — lộ là truy được.
- **ACCESS-003** — Gán lại job → **thu hồi quyền xưởng cũ tức thì** — hết "cần biết" là hết được biết.
- **ACCESS-004** — Cấm **tải hàng loạt** — chặn hành vi scrape cả kho.
- **ACCESS-005** — Một tài khoản **một vai trò staff**; admin không tự khóa/hạ cấp chính mình — khỏi tự dồn mình ra khỏi hệ thống.
- **ACCESS-006** — Hạn mức lưu trữ **1GB/20 model mỗi khách**, chạm trần thì hướng dẫn dọn — chi phí lưu không vô đáy.

## Nhóm PAY (6) — tiền

- **PAY-001** — **Tiền xong mới sản xuất** — nền tảng không in hộ đơn chưa trả.
- **PAY-002/003** — Quyết toán xưởng = **giá gốc − phí in lại do lỗi xưởng**, thực hiện sau khi hub **nghiệm thu lô** — làm ẩu thì nhận ít (thuộc **runbook đối soát ngoài hệ thống**, không tính trong app — quyết định 23/09).
- **PAY-004** — Lỗi giao thất **do nền tảng → hoàn đủ tiền** — cam kết của ai thì ví của đó chịu.
- **PAY-005** — **Không lưu thẻ/ví** — chỉ token + mã giao dịch; chốt đơn chỉ khi **webhook đã ký + API xác nhận cùng khớp**; URL return không bao giờ là bằng chứng.
- **PAY-006** — Hoàn tiền đã duyệt chạy **≤5 ngày làm việc**, idempotent (không hoàn 2 lần), hoàn **một phần theo item** được.

## Nhóm OPS (8) — vận hành

- **OPS-001** — Mọi can thiệp tay **kèm lý do bằng chữ** — có vết để học.
- **OPS-002** — Override **vẫn phải qua bộ lọc khả thi**; muốn "đè" thì tích cờ force tường minh. ⚠B11 — người cũng sai.
- **OPS-003** — Đình chỉ xưởng → **job chưa nhận được gán lại ngay** — bảo vệ khách không chờ bộ máy.
- **OPS-004** — Ops **khóa trần số job đồng thời** của xưởng đang thử thách — cho leo cấp từng bậc.
- **OPS-005/006** — Chưa **admin duyệt** thì chưa được gán việc; hồ sơ đăng ký phải **đủ** (≥1 máy, ≥1 vật liệu, lịch, thời gian trung chuyển) — lời tự khai có động cơ, cần cổng kiểm.
- **OPS-007** — Job **at-risk khi slack < 4h** (cấu hình được) → hiện trên monitor — can trước khi vỡ.
- **OPS-008** — Mọi **số liệu nghiên cứu chỉ được trích từ run có giao thức chốt version** — claim thuật toán mà tái lập không được là bịa.

## Nhóm CONFIG (4) + NOTIFY (2)

- **CONFIG-001/002/003** — Đổi tham số tạo **version mới** (không ghi đè), **audit đủ old/new/ai/lúc nào**, và **chỉ áp dụng forward** — lịch sử không tự viết lại.
- **CONFIG-004** — Audit log, decision log, bằng chứng kiểm, báo giá **giữ ≥2 năm học, bất biến** — tranh chấp của kỳ sau còn hồ sơ của kỳ này.
- **NOTIFY-001** — Biến cố quan trọng phải **báo đúng người, kịp thời** — thông tin nằm im là thông tin vô dụng.
- **NOTIFY-002** — Đổi ngày giao: **báo và xin khách duyệt trước**, không "đã đổi, thông báo sau".

---

# PHẦN 2 — 43 FUNCTIONAL REQUIREMENTS (yêu cầu chức năng)

FR = thứ hệ thống PHẢI LÀM được (không có nó, một nghiệp vụ không chạy). Mỗi FR đã gắn P-xx (nguồn phiếu đề tài), UC (use case), US (user story). Bản đầy đủ có acceptance criteria từng dòng trong `09-*.md`; ở đây là "nó là gì + vì sao có mặt".

## Customer Portal (FR-CUST-001..011) — hành trình khách

| Mã | Bình dân: hệ thống cho khách làm gì | MoSCoW |
|---|---|---|
| CUST-001 | Đăng ký/đăng nhập, sửa hồ sơ, quản lý địa chỉ (xác minh email mới được đặt đơn) | Must |
| CUST-002 | Tải file STL/OBJ/3MF ≤50MB vào kho bảo mật | Must |
| CUST-003 | Xem trước 3D trên trình duyệt kèm kích thước bao | Should (ứng viên cắt) |
| CUST-004 | Nhận phản hồi lỗi file: thủng lưới, đa dạng không kín, quá khổ máy | Must |
| CUST-005 | Cấu hình in từng item: vật liệu, màu, lớp, đặc/rỗng, hậu xử lý, số lượng, dịch vụ, cần-xong | Must |
| CUST-006 | Nhận báo giá tự động: tách chi phí + ngày giao cam kết + hạn dùng | Must |
| CUST-007 | Chốt báo giá thành đơn (bắt buộc địa chỉ + đồng ý điều khoản) | Must |
| CUST-008 | Trả **tiền thật** bằng QR SEPay; đơn chỉ chốt khi webhook + API xác nhận cùng khớp (hạn QR = hạn báo giá) | Must (dời tuần 7; T1–2 dùng cầu tạm mark-paid) |
| CUST-009 | Theo dõi đơn realtime, **không thấy tên xưởng nào** | Must |
| CUST-010 | Thư viện model cá nhân, đặt lại 1 chạm (prefill config cũ) | Could |
| CUST-011 | Khiếu nại/in lại trong cửa sổ bảo hành 30 ngày | Must |

## Lab (FR-LAB-001..007) — phía xưởng

| Mã | Nội dung | MoSCoW |
|---|---|---|
| LAB-001 | Đăng ký xưởng: lịch hoạt động, khai năng lực, giờ trung chuyển; chỉ hoạt động khi admin duyệt | Must |
| LAB-002 | Quản lý danh mục máy + đặc tính (khổ in, vật liệu, dung sai…) | Must |
| LAB-003 | Quản tồn kho vật liệu×màu, trừ tự động khi xong job, cảnh báo gần cạn | Must |
| LAB-004 | Nhận/Từ chối job trong cửa sổ 2h, từ chối chọn lý do; đếm ngược hiện trên UI | Must |
| LAB-005 | Quy trình thợ in: hàng đợi, tải file trong hạn, chuyển trạng thái, khai thực tế, báo sự cố kèm ảnh, tạo lô bàn giao | Must |
| LAB-006 | Xem timeline lịch theo từng máy (Gantt) | Should |
| LAB-007 | Xem điểm tín nhiệm + so sánh ẩn danh toàn mạng | Should |

## Hub (FR-HUB-001..005) — chất lượng & hoàn tất

| Mã | Nội dung | MoSCoW |
|---|---|---|
| HUB-001 | Nhận lô từ xưởng, đối soát khớp/thiếu/hỏng → incident + báo | Must |
| HUB-002 | Kiểm hàng theo checklist của grade: từng bước đạt/không, ảnh, mã lỗi, quy trách | Must |
| HUB-003 | Phát hành in lại (kế thừa deadline, giới hạn 2 lần → leo thang) | Must |
| HUB-004 | Gom đủ bộ đơn; **chặn đóng gói** khi còn item chưa pass | Must |
| HUB-005 | Đóng gói, ghi vận đơn (nhập tay), xác nhận shipped/delivered → mở đồng hồ bảo hành | Must (carrier API = out of scope) |

## Scheduling Engine (FR-SCHED-001..010) — trái tim đề tài

| Mã | Nội dung | MoSCoW |
|---|---|---|
| SCHED-001 | Phân tích hình học file: bbox, thể tích, watertight, manifold (<10s, chính xác 0.1mm) | Must |
| SCHED-002 | Cắt lớp không giao diện → ước lượng thời gian in + vật liệu | Must |
| SCHED-003 | Lọc khả thi cứng: loại mọi máy không in được job này | Must |
| SCHED-004 | Chấm điểm đa tiêu chí có trọng số trên tập khả thi | Must |
| **SCHED-005 ★** | **Đặt lịch suy đoán lúc báo giá**: chạy thử trên lịch thật → ngày sớm nhất khả thi + phụ phí nhanh | Must |
| SCHED-006 | Tách đơn→job, đặt vào hàng đợi máy với deadline lùi dần; **không đặt trùng slot** | Must |
| SCHED-007 | Sửa phần lịch chưa chạy khi có sự cố ≤30s, giữ job đang in, luôn commit fallback | Must |
| SCHED-008 | Vòng hiệu chuẩn: mẫu thực tế → hệ số theo dòng máy → áp cho báo giá tương lai | Should |
| SCHED-009 | Nhật ký gán việc: tập ứng viên + điểm từng tiêu chí + version cấu hình | Must |
| SCHED-010 | Engine giá một công thức, đọc bộ tham số **đang có hiệu lực**, **đóng băng version lên báo giá** | Must |

## Analytics & Ops (FR-ANAL-001..005) — mắt nhìn & bàn tay vận hành

| Mã | Nội dung | MoSCoW |
|---|---|---|
| ANAL-001 | Monitor mạng: đơn mở, job at-risk (slack<4h), tải từng xưởng, refresh ≤30s, drill-down | Should |
| ANAL-002 | Sổ điểm xưởng trên cửa sổ trượt, dựng lại được từ lịch sử job | Should |
| ANAL-003 | Báo cáo SLA theo kỳ + xuất CSV/PDF (kỳ rỗng → "no data", không số 0 giả) | Should |
| ANAL-004 | Can thiệp: gán lại, ưu tiên, treo xưởng, khóa trần — kèm lý do, preview ảnh hưởng, audit | Should |
| ANAL-005 | Hoàn tiền toàn phần/bộ phân theo item + đối soát hằng ngày | Must |

## Administration (FR-ADMIN-001..005) — nền móng quản trị

| Mã | Nội dung | MoSCoW |
|---|---|---|
| ADMIN-001 | Quản tài khoản/vai trò/quyền mọi phía; chặn tự khóa chính mình | Must |
| ADMIN-002 | Đổi giá/trọng số/ngưỡng **nóng ≤1 phút không restart**; hồ sơ cũ giữ version cũ | Must |
| ADMIN-003 | Danh mục dùng chung (vật liệu/màu/công nghệ/grade); đang dùng chỉ **deprecate**, không delete | Must |
| ADMIN-004 | Xem audit log bất biến + health hàng đợi/lưu trữ; replay job lỗi | Should (⚠C3 đề xuất hạ phần health về runbook) |
| ADMIN-005 | Soạn checklist kiểm + phân loại lỗi, có version | Should |

---

# PHẦN 3 — 16 NON-FUNCTIONAL REQUIREMENTS (yêu cầu phi chức năng)

NFR = **chất lượng phải đạt mức nào**, mỗi cái có 4 thành phần theo chuẩn thầy: phát biểu → chỉ số → môi trường → phương pháp đo. Không có NFR, hội đồng hỏi "nhanh là bao nhanh?" là chết.

## Hiệu năng & mở rộng (3+1)

1. **PERF-001** — Báo giá chạy nền: sơ bộ **≤5s**, chốt **≤60s p95** — đo k6 trên staging, file ≤50MB, 30 phiên đồng thời. *Ý nghĩa:* khách đứng chờ là khách bỏ đơn.
2. **PERF-002** — Sửa lịch **≤30s cứng / ≤15s trung bình**, hết giờ vẫn commit fallback — đo trên tải giả lập 50 xưởng/300 máy. *Máy đang chờ lịch — mỗi giây engine ngồi là tiền chết.*
3. **PERF-003** — API tương tác **p95 ≤2s** dưới lưu lượng thật (Should).
4. **SCALE-001** — **50 xưởng / 300 máy / 1.000 đơn-tháng / 500 job đang chạy** không phải đổi kiến trúc; chứng minh bằng chính các run đo PERF/REL, không phải tuyên bố.

## Độ tin cậy & đúng đắn (4) — nhóm "số 0"

5. **REL-002** — **0 mất** đơn đã chốt/gán việc/file khách: giết tiến trình giữa giao dịch không tạo bản ghi dở; diễn tập backup/restore hằng tháng.
6. **REL-004** — **0 đặt trùng máy**: N worker chạy song song trên một hàng đợi mà không có 2 job cùng slot — do **ràng buộc DB (EXCLUDE)** dựng sẵn, không phải "canh me" bằng code.
7. **REL-005** — **0 gán bất khả thi**: bộ job "cố tình không máy nào làm nổi" chạy độc lập với scoring, hệ thống phải từ chối hết.
8. **REL-006** — Ngày cam kết **0 đổi ngoài 3 điều kiện duyệt**, mọi lần đổi có log báo + duyệt của khách — đo trên audit dữ liệu reschedule.

## Bảo mật (2)

9. **SEC-007** — File model **chỉ** trong cửa sổ job, log 100% lượt truy cập, 0 tải loạt — test đối kháng là bài 11 trong Practical.
10. **SEC-009** — **0 dữ liệu thẻ** trong DB/log; 100% webhook chữ ký sai bị đuổi; đối soát mỗi ngày **0 lệch không giải thích được**; **0 hoàn trùng** (idempotency key).

## Dễ dùng (2)

11. **USE-001** — 5/5 người lạ **không được học gì** đặt xong đơn ≤10 phút — moderated usability test.
12. **USE-006** — App xưởng cạnh máy in: mọi cập nhật trạng thái **≤3 chạm**, nhập liệu sống sót **2 phút mất mạng** — đo ngay tại xưởng đối tác (đã có lab). ⚠B13.

## Kiểm toán & vận trì (2)

13. **LEGAL-002** — **50 quyết định** rút mẫu bất kỳ (gán/override/hoàn/QC) tái dựng đủ ứng viên + điểm + version cấu hình.
14. **MAINT-005** — Đổi tham số thương mại **hiệu lực ≤1 phút, không restart**; quote cũ giữ version đóng băng — e2e qua FR-ADMIN-002.

## Nghiên cứu & chính xác (2)

15. **TEST-003** — Mọi claim thuật toán **đo với ≥3 baseline** trên cùng seed; seed giống → kết quả giống; run trôi nổi bị cách ly. *Đây là NFR biến capstone thành nghiên cứu.*
16. **ACC-001** — MAPE ước lượng in **<25%/dòng máy và phải giảm** sau mỗi vòng hiệu chuẩn — nuôi bằng dữ liệu in thật của lab đối tác.

> 33 NFR phụ (chuẩn ISO 25010 từng dòng) nằm ở phần appendix `10-*.md` — 16 cái trên là **bộ đem ra bảo vệ**; khi hội đồng hỏi NFR, chỉ đánh vào 16 số này.

---

# Mối quan hệ ba tầng — một câu nhớ mãi

- **BR** = *luật chơi* (đúng/sai nghiệp vụ — vd: "từ chối không lý do = không được").
- **FR** = *nước cờ* (chức năng cụ thể thi hành luật — vd: LAB-004 có dropdown lý do + countdown).
- **NFR** = *thể lực người chơi* (luật/cờ phải chạy ở mức nào — vd: luật sửa lịch thắng trong 30 giây).

Khi bị hỏi "rule BR-XXX này nằm ở đâu trong app": đọc cột **Related Req / UC** trong bảng gốc — mỗi BR có mặt ở ≥1 FR hoặc đã ghi nhận dead rule (đúng 1: PERF-006, chờ quyết định C2).
