# Hợp đồng & quy ước repo — PrintGrid

Khóa ngày 27/09 trong nhiệm vụ **"Thống nhất hợp đồng API và nền móng repo chung"**
(dòng RUN-01 của `documentation/capstone/planning/PrintGrid-Ke-hoach-1-Trang.xlsx`).

## 3 tệp là hợp đồng — sửa ở đây trước, code sau

| Tệp | Chứa | Ai có quyền merge |
|---|---|---|
| `contracts/openapi.yaml` | Mọi endpoint + schema DTO. `x-milestone: existing\|T1..T8` = tuần dự kiến có mã thật | Ghế Nền tảng (chủ vỏ) |
| `contracts/events.md` | Tên + payload sự kiện miền; enum | Ghế Nền tảng |
| mục dưới đây | Quy ước thư mục + nhánh | Cả nhóm đồng thuận, chủ vỏ giữ luật |

## Quy ước cấu trúc FE (chống dẫm chân)

```
frontend/src/features/
  auth/  account/        ← ghế Nền tảng (F1)
  admin/                 ← ghế Nền tảng (F2: cấu hình, danh mục, checklist, người dùng, nhật ký)
  quotes/                ← ghế Đơn hàng: checkout/order-config
    QuoteResult/         ← thành phần riêng của ghế Engine (chip giá + ngày giao) — Engine tự sửa
  tracking/ notifications/ reprint/   ← ghế Cam kết (F7)
  lab/                   ← ghế Xưởng (F4)
  hub/                   ← ghế Hub (F5)
  ops/
    decisionlog/ override/   ← ghế Engine (F3)
    refunds/                 ← ghế Đơn hàng (F6)
    monitor/ reports/        ← ghế Hub/Nền tảng (ANAL)
  home/ shared/ routes/  ← CHỦ VỎ: không ai tự sửa, cần gì mở PR 1 dòng xin chủ vỏ
```

**Luật:** một thư mục features/<F>/ — một tuần — một người. Trang nào ghép của nhiều ghế thì chỉ ghép
qua component đã export công khai + DTO theo openapi, không importInternals của nhau.

## Quy ước backend

- Endpoint mới phải có trong `openapi.yaml` **trước khi** viết controller (file này sinh Swagger đối chiếu).
- Enum trả về client luôn là **string** (bài học OrderDto.Status số hóa).
- Payload hướng KHÁCH qua whitelist DTO — không có trường `labId/machineId` (nghiệm thu bằng test chuỗi).
- Mọi handler event đặt tại module TIÊU THỤ, đăng ký ở `events.md`.

## Nhánh & hợp nhất

- Tên nhánh: `f/<F#>-<wTuần>-<ngắn-gọn>` — ví dụ `f/SCHED-005-w2-speculative-placement`.
- Merge mỗi tối; nhánh chạm `shared/ routes/ docker-compose.yml openapi.yaml` quá 3 ngày = cấm.
- CI (GitHub Actions): `dotnet build` + `dotnet test` + `tsc --noEmit` — đỏ thì không merge.

## Nền máy dùng chung (đã kiểm 27/09)

| Thành phần | Cổng host | Kiểm |
|---|---|---|
| API | 5000 | /health 200; /network/stats 200 |
| Frontend + proxy /api | 3000 | gọi API xuyên nginx 200 |
| Postgres / Redis / MinIO(console) | 5432 / 6379 / 9000(9001) | healthcheck `healthy` |

Lệnh một phát cho máy mới: `git clone → docker compose up -d` (script seed-dev riêng — KHÔNG tự chạy dữ liệu mẫu từ tuần 26/09).
