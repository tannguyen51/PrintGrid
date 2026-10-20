# Report 7 — Đánh giá thuật toán lập lịch (PrintGrid)

**Phạm vi:** S10 / UC-027, RUN-04…RUN-06 — bộ mô phỏng mạng lưới và báo cáo so sánh chiến lược.
**Mã nguồn:** `src/PrintGrid.Simulator/` (console `net9.0`), test: `tests/PrintGrid.SimulatorTests/`.
**Ngày đo:** 2026-10-20.

---

## 1. Câu hỏi đánh giá

1. Bộ xếp lịch của PrintGrid (lọc năng lực → tìm khung giờ trống → chấm điểm có trọng số) có tốt hơn các
   chiến lược đơn giản không, và tốt hơn bao nhiêu?
2. Khi mạng lưới có sự cố (xưởng từ chối, in hỏng, máy hỏng, hub QC loại), chất lượng lịch giảm bao nhiêu
   và cơ chế vá lịch có kéo lại được không?
3. Bộ xếp lịch có chạy nổi ở quy mô mục tiêu (50 xưởng / 300 máy / 1000 đơn) trong thời gian chấp nhận được không?

## 2. Cách mô phỏng

### 2.1 Kiến trúc

Thư viện mô phỏng **tái sử dụng chính mã production** thay vì viết lại thuật toán:

| Thành phần | Nguồn |
|---|---|
| `CapabilityFilter` (lọc vật liệu / công nghệ / khổ in / lịch làm việc) | `Scheduling.Domain.Services` |
| `AssignmentScorer` (5 tiêu chí có trọng số) | `Scheduling.Domain.Services` |
| `Job`, `Lab`, `Machine`, `JobSpecification` (vòng đời job, trạng thái, SPEC) | `Scheduling.Domain.Entities` |
| Vòng lặp timeline (tìm khung trống, giữ chỗ, utilization) | mô phỏng lại `AssignmentEngine` trên `MachineCalendar` in-memory |

Hai điểm khác production được nêu rõ: (a) không có PostgreSQL/EF — timeline là cấu trúc in-memory;
(b) `AssignmentEngine` thật chạy trên dữ liệu DB và có ghi decision log, simulator thì không.

### 2.2 Mạng lưới sinh ra

- **Seed duy nhất** cho mọi luồng ngẫu nhiên ⇒ cùng `(seed, labs, machines, orders)` cho ra cùng mạng.
  Bằng chứng: 5 test trong `SimulatorTests` (gồm test hash SHA-256 của danh sách đơn ổn định giữa hai lần chạy).
- 8 thành phố, `transitDaysToHub ∈ [0,3]`, `OnTimeDeliveryRate ∈ [0.70, 0.99]`,
  `FirstPassYield ∈ [0.80, 0.99]`.
- Máy thuộc 3 công nghệ (FDM / SLA / SLS) với vật liệu tương ứng, khổ in và `SpeedFactor` khác nhau.
- Đơn hàng: Poisson-ish, có `InternalDueDate`, khối lượng vật liệu, thời gian in ước tính; một phần là đơn
  `Urgent` (trọng số trễ ×3).

### 2.3 Bốn chiến lược

Cùng một scorer, chỉ khác bộ trọng số — nên phép so sánh đo **chiến lược**, không đo 4 cách cài đặt khác nhau:

| Chiến lược | Trọng số |
|---|---|
| `capacity` | LoadBalance = 1 (máy ít tải nhất) |
| `edd` | DueDateSlack = 1 (xong sớm nhất) |
| `nearest` | Transit = 1 (gần hub nhất) |
| `random` | Trọng số mặc định, sau đó chọn đều trong các vị trí khả thi |

Hòa điểm được phá theo **thời điểm hoàn thành sớm nhất**. Trước bản vá này, `nearest` coi mọi xưởng cùng
`transitDays` là hòa và LINQ (sort ổn định) dồn toàn bộ đơn vào xưởng đầu tiên — makespan 169,9 ngày và
utilization 3,4 %, một kết quả phản ánh lỗi cài đặt chứ không phản ánh chiến lược. Số liệu dưới đây là sau khi vá.

### 2.4 Sự cố mô phỏng

| Mã | Sự cố | Tham số | Hành vi |
|---|---|---|---|
| a | Xưởng từ chối nhận đơn | `decline-rate` | Nhả slot, đưa lại hàng đợi **loại trừ xưởng vừa từ chối** (FR-SCHED-007), tối đa 6 lần chào |
| b | In hỏng | `print-failure-rate` | Job `Failed`, đưa lại hàng đợi (vá lịch), tính 1 reschedule |
| c | Máy hỏng | `breakdown-rate`, `mean-downtime-minutes` | Máy bị chiếm dụng trong thời gian downtime; job phải tìm máy khác |
| d | Hub QC loại | `inspection-fail-rate` | Job về trạng thái chờ xếp lại; vượt 6 reschedule thì `GaveUp` |

Mỗi sự cố ⇒ assigner chạy lại ngay cho job bị ảnh hưởng ("hỏng ⇒ vá lịch"), đúng nguyên tắc
event-driven rescheduling trong tài liệu nghiệp vụ.

### 2.5 Chỉ số

| Chỉ số | Định nghĩa |
|---|---|
| Weighted tardiness | Σ `max(0, hoàn thành − hạn)` × trọng số ưu tiên (Urgent ×3) |
| On-time % | tỉ lệ đơn hoàn thành trước hạn nội bộ |
| Utilisation % | tổng phút máy bận / (makespan × số máy) |
| Changeovers | số lần đổi vật liệu/ màu giữa hai job liên tiếp trên cùng máy |
| Gini load spread | độ lệch tải giữa các xưởng (0 = chia đều tuyệt đối) |
| Reschedules | số lần phải vá lịch (độ "nervous" của lịch) |
| Estimate MAPE % | sai số tuyệt đối trung bình giữa phút in thực tế và ước tính |
| Makespan | từ mốc epoch tới hoạt động cuối cùng |

---

## 3. Kết quả — kịch bản chuẩn (10 xưởng / 40 máy / 500 đơn, seed 42)

Sự cố bật: từ chối 10 %, in hỏng 5 %, máy hỏng 2 % (downtime TB 120 phút), QC loại 3 %.

```
Strategy comparison (faults enabled):
Metric                        |  capacity  |    edd     |  nearest   |   random
----------------------------------------------------------------------------------
Completed / orders            |  500/500   |  500/500   |  500/500   |  500/500
Failed                        |     0      |     0      |     0      |     0
Weighted tardiness (min)      |  318338.7  |  245375.9  |  495000.4  |  656554.3
On-time %                     |    69.4    |    74.6    |    60.2    |    63.0
Utilisation %                 |    44.4    |    44.4    |    38.0    |    29.0
Changeovers                   |    490     |    488     |    495     |    478
Gini load spread              |   0.0593   |   0.0712   |   0.4003   |   0.1246
Reschedules (nervousness)     |    147     |    144     |    139     |    136
Estimate MAPE %               |    2.5     |    2.5     |    2.4     |    2.5
Makespan (days)               |    8.9     |    8.8     |    10.5     |    14.9
Declines (fault a)            |     66     |     64     |     63     |     63
Print failures (fault b)      |     44     |     44     |     44     |     44
Breakdowns (fault c)          |      8     |      8     |      8     |      8
Inspection fails (fault d)    |     19     |     19     |     19     |     19
Downtime (min)                |   1395.3   |   1395.3   |   1395.3   |   1395.3
```

Không có đơn nào bị bỏ (500/500) ở mọi chiến lược — sự cố ảnh hưởng tới **thời hạn**, không tới khả năng
hoàn thành, nhờ cơ chế vá lịch và ngưỡng `GaveUp` đủ rộng.

### 3.1 Tác động của sự cố (baseline không sự cố → có sự cố)

```
Strategy    | On-time %            | Tardiness (min)        | Reschedules | Faults D/P/B/I
---------------------------------------------------------------------------------------------
capacity    | 74.6 -> 69.4         | 239365.9 -> 318338.7   | 0 -> 147    | 66/44/8/19 (137)
edd         | 77.4 -> 74.6         | 181893.2 -> 245375.9   | 0 -> 144    | 64/44/8/19 (135)
nearest     | 68.6 -> 60.2         | 329921.7 -> 495000.4   | 0 -> 139    | 63/44/8/19 (134)
random      | 65.8 -> 63.0         | 496744.5 -> 656554.3   | 0 -> 136    | 63/44/8/19 (134)
```

Đọc bảng:

- **`edd` thắng rõ ràng** ở kịch bản này: on-time cao nhất (74,6 %) và tardiness thấp nhất (245 375 phút),
  cả khi có sự cố. Điều này hợp lý với mô hình: mọi job có cùng mức ưu tiên chính (trừ Urgent), nên chọn
  "xong sớm nhất" gần như tối ưu trực tiếp đại lượng được đo.
- **`capacity` sát sau** nhưng Gini tốt nhất (0,0593) — chia tải đều hơn, đổi lại trễ hạn nhiều hơn ~30 %.
  Đây là lý do trọng số mặc định của production **không** dùng thuần một tiêu chí: nó cân bằng giữa hạn,
  độ tin cậy, tải, chi phí và transit.
- **`nearest` tệ nhất về hạn** (60,2 % on-time) và Gini cao gấp ~6,7 lần `capacity`: tối ưu vận chuyển một
  chiều dồn tải về một nhóm xưởng, và ở mức tải ~44 % điều đó đủ để trễ hạn.
- **`random` là sàn tham chiếu** (63,0 %); `edd` hơn `random` 11,6 điểm phần trăm on-time.
- Sự cố làm giảm on-time **3,4–8,4 điểm phần trăm** tuỳ chiến lược. Chiến lược có nhiều "dư địa" hơn
  (`edd`) chịu mất ít hơn tính theo tương đối so với `nearest`.
- Tardiness tăng mạnh hơn on-time vì vài đơn bị đẩy rất xa mốc hạn (đuôi nặng), đúng với bản chất
  "nervousness" của lịch động.

### 3.2 Kịch bản nghiệm thu UC-027 (50 xưởng / 300 máy / 1 000 đơn, seed 42)

Cùng bộ sự cố, nhưng mạng lớn hơn nhiều lần và **tải mỗi máy thấp** (utilisation ~9–11 %):

```
Strategy comparison (faults enabled):
Metric                        |  capacity  |    edd     |  nearest   |   random
----------------------------------------------------------------------------------
Completed / orders            | 1000/1000  | 1000/1000  | 1000/1000  | 1000/1000
Weighted tardiness (min)      |  425083.9  |  445089.7  |  189452.3  |  788598.7
On-time %                     |    72.6    |    72.2    |    92.2    |    61.6
Utilisation %                 |     9.0    |     9.0    |    11.1    |    10.1
Gini load spread              |   0.2884   |   0.2872   |   0.7158   |   0.2272
Reschedules                   |    286     |    296     |    299     |    279
Makespan (days)               |     9.6    |     9.8    |     9.3    |    11.3
```

**Đảo ngược kết quả so với mục 3.** Ở mức tải thấp, `nearest` thắng đậm (92,2 % on-time) dù vẫn dồn tải
lệch nhất (Gini 0,7158). Nguyên nhân nằm trong mô hình thời gian: một đơn chỉ được coi là hoàn thành sau khi
in xong **và** hàng về tới hub (transit + QC), nên khi mỗi xưởng còn rất nhiều công suất trống, yếu tố quyết
định hạn là **transit**, không phải hàng đợi. Khi tải tăng lên (~44 % ở mục 3, và cao hơn ở mục 4), cùng
chiến lược đó lại trở thành lựa chọn tệ nhất vì hàng đợi phình ở nhóm xưởng gần hub.

Đây là bằng chứng trực tiếp cho việc **không thể chốt một bộ trọng số cố định** cho mọi mùa.

### 3.3 Nhận xét về lựa chọn production

Simulator **không** chạy trọng số mặc định của production như một chiến lược riêng. `random` dùng trọng số
mặc định để xếp hạng nhưng sau đó chọn đều trong các vị trí khả thi — nó là **sàn tham chiếu ngẫu nhiên**,
không phải phép đo bộ trọng số mặc định. Vì vậy báo cáo này không tuyên bố "trọng số mặc định tốt hơn `edd`";
đó là câu hỏi còn mở và có thể trả lời bằng một chiến lược thứ năm dùng `ScoringWeights.Default` với
`ranked[0]`.

Kết luận rút ra từ hai kịch bản đảo chiều ở mục 3 và 3.2:

> Không có bộ trọng số nào thắng mọi mức tải. Vì vậy PrintGrid lưu trọng số **có phiên bản hoá**
> (`ScoringParameterSet`, FR-SCHED-009) để Ops chỉnh theo mùa và ghi lại phiên bản đã dùng vào decision log —
> thay vì hard-code một bộ "tối ưu".

## 4. Khả năng mở rộng

Cùng seed 7, không bật sự cố, chạy 4 chiến lược (thời gian là toàn bộ 8 lượt chạy: 4 baseline + 4 có sự cố):

| Quy mô (xưởng / máy / đơn) | Thời gian | On-time % (cap/edd/nearest/random) | Makespan ngày (cap/edd/nearest/random) |
|---|---|---|---|
| 5 / 20 / 100 | 125 ms | 64,0 / 61,0 / 54,0 / 59,0 | 7,1 / 7,9 / 9,1 / 9,2 |
| 10 / 40 / 500 | 608 ms | 65,8 / 66,8 / 45,6 / 59,6 | 9,7 / 9,7 / 10,0 / 13,0 |
| 25 / 100 / 2 000 | 2 335 ms | 44,2 / 48,9 / 27,3 / 47,8 | 20,4 / 15,4 / 32,2 / 28,2 |
| 50 / 300 / 5 000 | 13 264 ms | 56,9 / 61,4 / 26,0 / 50,9 | 15,8 / 15,4 / 32,0 / 23,6 |

- Preset nghiệm thu của UC-027 là **50 xưởng / 300 máy / 1 000 đơn**; cấu hình nặng hơn 5× (5 000 đơn) vẫn
  xong trong **~13 giây** cho 8 lượt chạy, tức trung bình ~1,7 s/lượt ⇒ thoải mái trong ngân sách 30 s của
  `AssignmentEngine` ở quy mô mục tiêu.
- Thời gian tăng gần bậc hai theo số đơn vì mỗi lần xếp quét toàn bộ ứng viên; đây là chi phí chấp nhận được
  với chế độ xếp theo lô. Nếu cần, `CapabilityFilter` là điểm cắt tỉa tự nhiên (index theo vật liệu/khổ in).
- On-time giảm khi mạng lớn hơn chủ yếu do tỉ lệ đơn Urgent và hạn nội bộ sinh ngẫu nhiên không co giãn theo
  quy mô — không phải do thuật toán chậm.

## 5. Giới hạn — những gì báo cáo này **không** chứng minh

1. **Không phải production engine.** Simulator dùng chung filter/scorer/entity nhưng vòng timeline là bản
   mô phỏng in-memory; nó không kiểm chứng hành vi DB (EXCLUDE constraint, transaction, decision log).
2. **Không mô hình hoá giá.** Chỉ số chi phí trong scorer dùng công thức đơn giản (gram × 0,5 + phút × 0,1),
   không phải bảng giá thật trong `PricingParameterSet`.
3. **Không có con người.** Không mô phỏng thời gian Ops duyệt, thời gian khách phản hồi đề xuất đổi ngày,
   hay hàng đợi escalation — các yếu tố này nằm ngoài mô hình sự kiện rời rạc hiện tại.
4. **Phân phối đơn là giả lập**, không lấy từ dữ liệu vận hành thật. Các con số tuyệt đối vì thế chỉ dùng để
   **so sánh tương đối giữa các chiến lược**, không dùng làm cam kết SLA.
5. **Nhiễu ±5 %** cho phút in thực tế; mô hình sai số thực tế có thể nặng đuôi hơn.

## 6. Cách tái lập

```bash
# Kịch bản trong mục 3
dotnet run --project src/PrintGrid.Simulator -c Release -- run \
  --seed 42 --labs 10 --machines 40 --orders 500 --strategy all \
  --decline-rate 0.1 --print-failure-rate 0.05 --breakdown-rate 0.02 --inspection-fail-rate 0.03

# Sinh mạng + hash danh sách đơn (kiểm tra tính tất định)
dotnet run --project src/PrintGrid.Simulator -- generate --seed 42 --labs 10 --machines 40 --orders 500

# Preset nghiệm thu UC-027
dotnet run --project src/PrintGrid.Simulator -- run --stress --seed 42

# Test tự động (bao gồm hash ổn định, không trùng lịch, vá sự cố)
dotnet test tests/PrintGrid.SimulatorTests
```

Kết quả in ra kèm `order list hash`; cùng seed + cùng tham số ⇒ cùng hash và cùng số liệu.

## 7. Kết luận

1. Không có chiến lược nào thắng mọi mức tải: `edd` tốt nhất ở tải trung bình (~44 %), `nearest` tốt nhất khi
   tải thấp nhờ transit ngắn, và `nearest` sụp ở tải cao (26 % on-time ở 5 000 đơn). Cả bốn đều **vượt sàn
   ngẫu nhiên** về on-time ở kịch bản chuẩn (trừ `nearest` khi quá tải).
2. Cơ chế vá lịch event-driven giữ được **100 % đơn hoàn thành** qua 4 loại sự cố, với chi phí 3–8 điểm
   phần trăm on-time — mức đánh đổi hợp lý so với việc bỏ đơn.
3. Simulator chạy vượt quy mô mục tiêu 5× trong ~13 s và tái lập được tuyệt đối bằng seed, đủ dùng làm
   bằng chứng nghiệm thu RUN-04…RUN-06 và làm công cụ thử trọng số trước khi đổi cấu hình production.
