# Báo giá theo CuraEngine

Windows local install, two complete test profiles, sample files and test commands:
[Local CuraEngine setup](../../../infra/slicer/README.md).

Luồng mới của `POST /api/v1/quotes` tính theo công thức đã chốt:

```text
Tiền_vật_liệu = tổng(gram từng màu / 1000 × giá màu đồng/kg)
Tiền_máy = tổng phút từ slicer / 60 × đơn giá đồng/giờ
Tiền_in_có_dự_phòng = (Tiền_vật_liệu + Tiền_máy) / (1 − tỷ_lệ_lỗi)
Tổng_đơn = làm_tròn_đồng(Tiền_in_có_dự_phòng + số_lượng × giá_đánh_bóng)
```

Không cộng điện, khấu hao, chuẩn bị, phí nền tảng, lợi nhuận hoặc phí thanh toán.
Tỷ lệ lỗi lưu dạng thập phân: 8% = `0.08`. Tỷ lệ này là mô hình kỳ vọng
in lại trọn lần; lab đã bao rủi ro trong đơn giá thì cấu hình bằng 0.
Vật liệu slicer báo đã bao gồm support/purge của profile; không cộng lần nữa.

## Size và màu

- `ORIGINAL`: giữ kích thước file. File 120 × 80 × 160 mm vẫn là 120 × 80 × 160 mm.
- `S`, `M`, `L`: cạnh lớn nhất lần lượt **100, 200, 300 mm**, giữ nguyên tỷ lệ ba trục.
  File trên chọn M sẽ thành **150 × 100 × 200 mm**. Đây là kích thước đích,
  khác với phân loại một file có sẵn vào khoảng S/M/L.
- Một màu: STL hoặc 3MF. Nhiều màu trên cùng mô hình: 3MF core có các vùng
  vật liệu là những khối kín riêng. Thứ tự `colorCodes` gán vào các vùng theo
  thứ tự tăng của cặp `(pid, pindex)` trong file. Không chia đều gram theo số màu.
- Chưa hỗ trợ màu nội suy trên mặt, kiểu tô màu riêng của Bambu/Orca không có
  phân vùng 3MF core, tham chiếu model ngoài, hoặc báo giá trực tiếp OBJ/GLB.
  Các file này vẫn được lưu/xem như trước; báo giá tự động trả lỗi cần xuất lại
  STL/3MF phù hợp. Không trả một giá giả khi không đọc được vùng màu.
- Hướng đặt giữ theo file. Nếu không vừa hướng hiện tại, trả lỗi; không tự xoay/ghép mô hình.

## Cấu hình máy chủ trước khi sử dụng

Adapter gọi executable CuraEngine qua `ProcessStartInfo.ArgumentList`, không qua shell.
Cấu hình resolved được chuyển thành JSON definition literal và gọi CLI chuẩn
`slice -j global.def.json -e0 -j extruder-0.def.json -l part-0.stl -o quote.gcode`.
Không cần GUI hoặc hỗ trợ tùy chọn `-r`. Đối chiếu CLI của bản cài:
https://github.com/Ultimaker/CuraEngine/blob/main/src/communication/CommandLine.cpp

Mỗi máy đăng ký trong DB cần một entry có cùng GUID dưới `QuoteSlicer:Machines`:

```json
{
  "QuoteSlicer": {
    "ExecutablePath": "/opt/cura/CuraEngine",
    "TimeoutSeconds": 120,
    "Machines": {
      "GUID-MAY-TRONG-DB": {
        "SettingsPath": "/opt/printgrid/profiles/may-01.resolved.json",
        "Materials": ["PLA"],
        "Colors": ["BLUE", "WHITE", "SIGNALRED"],
        "PricePerHour": 6000,
        "FailureRate": 0.08
      }
    }
  }
}
```

Đây là **ví dụ cấu hình**, không phải giá mặc định. Máy không có profile sẽ không
được đưa vào báo giá mới. Dùng profile đã lab hiệu chỉnh, gồm `global` và
`extruder.0`, `extruder.1`, ... với các giá trị đã resolved (không phải biểu thức
trong file `.def.json` hay chỉ vài setting). Giữ nguyên giới hạn chuyển động,
gia tốc, nhiệt độ, support, prime tower/purge và cơ cấu đổi màu đúng máy thực tế.
Nếu máy nhận nhiều vật liệu, thêm `SettingsByMaterial` ánh xạ từng mã vật liệu
sang file profile riêng. `SettingsPath` chỉ dùng khi máy khai báo đúng một vật liệu.
`machine_width/depth/height`, `machine_center_is_zero`, `material_diameter`
phải chính xác. Profile có thể khai báo các extruder/slot nhưng cần kiểm chứng
đúng cơ cấu thực tế; nhiều slot màu không đồng nghĩa nhiều đầu phun vật lý.
Adapter chỉ thay layer height, infill, gán các khối màu và tọa độ đã scale/đặt giữa bàn.

Header hỗ trợ Marlin/RepRap (độ dài m đổi sang thể tích nhờ đường kính sợi),
Marlin volumetric (`mm3`), Griffin (`VOLUME_USED` theo extruder).
UltiGCode không đủ vật liệu cho >2 màu nên không dùng cho báo giá nhiều màu.
G-code tạo trong thư mục tạm chỉ để báo giá, không được tự gửi đến máy in.
Adapter ưu tiên tổng kết thực tế trong log `Print time (s)`/`Total print time`
và `Filament (mm^3)`/`Filament N`; một số phiên bản CLI ghi header placeholder,
nên chỉ dùng header khi không có tổng kết trong log và đủ số liệu hợp lệ.

Mỗi lượt slice một mô hình hoàn chỉnh, sau đó nhân vật liệu và thời gian với
số lượng: sản xuất **từng chiếc nối tiếp**, chưa tối ưu xếp nhiều chiếc lên một bàn.
Tối đa hai engine chạy đồng thời mỗi tiến trình API; timeout và hủy request sẽ
dừng process con, dọn thư mục tạm. Request HTTP cần timeout lớn hơn thời gian
slice các máy ứng viên; hệ thống nhiều máy nên chuyển bước này thành job nền.

Không có executable hoặc profile thật thì API trả thông báo thiếu cấu hình;
không thay bằng bộ ước tính hình học cũ để giả làm kết quả Cura.

## Đơn giá thương mại

Trong trang Admin → Cấu hình thương mại, tạo và kích hoạt phiên bản có:

- Đơn giá vật liệu chuẩn: **đồng/g**, như hệ thống cũ.
- `colorPricePerKg`: bảng override theo khóa `PLA:BLUE`, `PLA:WHITE`, ...,
  đơn vị **đồng/kg**. Thiếu override thì lấy giá vật liệu chuẩn × 1000.
- `failureRate`: tỷ lệ lỗi mặc định; profile máy có thể override.
- `polishingPerUnit`: đồng/sản phẩm. Chọn đánh bóng khi chưa có đơn giá sẽ bị chặn.
- `pricePerPrintHour`: giá giờ mặc định; profile máy có thể override.

API không nhận giá hay gram do khách gửi. Hệ thống lọc máy có khổ/vật liệu/màu,
slice các máy khả thi, kiểm tra lại thời gian và tồn kho từng màu rồi chọn giá
thấp nhất; hòa giá thì chọn ngày giao sớm hơn. Bản tính đóng băng ở quote item,
copy sang order/job; sản xuất giữ máy có đúng profile đã dùng, không điều chỉnh
tốc độ/hiệu chuẩn lần nữa trên số giờ Cura đã tính cho máy đó.
Tổng tiền làm tròn một lần cho cả đơn; đơn giá/chiếc là số trung bình để hiển thị.

Ví dụ payload:

```json
{
  "modelId": "GUID-FILE-CUA-KHACH",
  "materialCode": "PLA",
  "colorCode": "BLUE",
  "colorCodes": ["BLUE", "WHITE", "SIGNALRED"],
  "sizeCode": "ORIGINAL",
  "layerHeightMm": 0.2,
  "infillPercent": 30,
  "quantity": 5,
  "toleranceMm": 0.2,
  "polishing": true
}
```

Phải áp dụng migration `AddSlicedQuotation` trước khi chạy phiên bản API mới.
Lịch sử báo giá cũ vẫn đọc được (`calculation = null`); không sửa giá đã chốt.
Các caller nội bộ cũ không truyền option mới giữ đường tính legacy để hỗ trợ
lịch sử/test; endpoint HTTP luôn đi qua luồng Cura với `ORIGINAL` mặc định.
