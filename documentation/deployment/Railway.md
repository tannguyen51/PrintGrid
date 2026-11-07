# Triển khai PrintGrid trên Railway

Tài liệu này mô tả cấu hình đã được chuẩn bị trong repo và các biến môi trường cần đặt trên
Railway. Repo đã có `railway.json` cho API; frontend dùng Dockerfile riêng.

## 1. Mô hình service

| Service | Nguồn | Root Directory | Dockerfile | Public port | Ghi chú |
|---|---|---|---|---|---|
| `api` | repo | `/` (mặc định) | `src/PrintGrid.Api/Dockerfile` | **8080** | healthcheck `/health` (đã khai báo trong `railway.json`) |
| `frontend` | repo | `frontend` | `Dockerfile` | **80** | build arg `VITE_GOOGLE_CLIENT_ID` |
| `Postgres` | plugin Railway | — | — | — | dùng cho EF + Hangfire |
| `Redis` | plugin Railway | — | — | — | cache cho quên mật khẩu + `ICacheService` |
| `minio` | Docker image | — | `minio/minio` | **9000** | volume mount `/data`, command `server /data --console-address ":9001"` |

> Nếu chưa muốn tự chạy MinIO, có thể dùng **Cloudflare R2** (S3-compatible): đặt
> `MinIO__Endpoint` = `<account>.r2.cloudflarestorage.com`, `MinIO__PublicEndpoint` = cùng giá trị,
> `MinIO__Region=auto`, `MinIO__UseSsl=true`. Không cần service/volume.

## 2. Biến môi trường — API

Đặt trong service `api` → Variables. Cú pháp `${{TênService.BIẾN}}` là reference của Railway;
đổi `Postgres`/`Redis` cho khớp tên service thực tế của bạn.

```env
ASPNETCORE_ENVIRONMENT=Production

# Bắt buộc khi chạy sau proxy TLS của Railway (nếu không /health bị 307 loop)
HttpsRedirection__Enabled=false

# Postgres
ConnectionStrings__DefaultConnection=Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}}

# Redis — StackExchange.Redis không nhận redis:// URI, phải dùng host:port,password=...
ConnectionStrings__Redis=${{Redis.REDISHOST}}:${{Redis.REDISPORT}},password=${{Redis.REDISPASSWORD}}

# JWT — chuỗi ngẫu nhiên >= 32 ký tự
Jwt__Key=<chuỗi-ngẫu-nhiên-dài>
Jwt__Issuer=PrintGrid
Jwt__Audience=PrintGrid.Client

# Link trong email reset mật khẩu trỏ về domain frontend thật
App__FrontendUrl=https://<frontend-domain>

# Google
Authentication__Google__ClientId=321822470971-86scd5gkq8jub3h5h8hl8jq2dn6uk411.apps.googleusercontent.com

# SMTP
Email__Smtp__Enabled=true
Email__Smtp__Host=smtp.gmail.com
Email__Smtp__Port=587
Email__Smtp__Username=printgridproduction@gmail.com
Email__Smtp__Password=<Gmail-App-Password>
Email__Smtp__FromEmail=printgridproduction@gmail.com
Email__Smtp__FromName=PrintGrid

# MinIO (endpoint KHÔNG kèm scheme)
MinIO__Endpoint=<minio-public-host>
MinIO__PublicEndpoint=<minio-public-host>
MinIO__UseSsl=true
MinIO__AccessKey=<minio-access-key>
MinIO__SecretKey=<minio-secret-key>
MinIO__Region=us-east-1

# CORS — chỉ cần nếu gọi API trực tiếp (frontend proxy same-origin)
Cors__AllowedOrigins__0=https://<frontend-domain>

# Cọc
Payment__AutoConfirmOnPlace=false
Payment__DepositPercent=50

# CuraEngine — slicer cho báo giá đa màu FDM.
# Binary đã được build vào Dockerfile ở build stage; biến này chỉ trỏ đường dẫn tại runtime.
QuoteSlicer__ExecutablePath=/opt/cura/CuraEngine
```

`ASPNETCORE_HTTP_PORTS=8080` đã nằm trong Dockerfile. Trong **Settings → Networking → Public
Networking**, đặt **Target Port = 8080**.

## 3. Biến môi trường — Frontend

```env
API_UPSTREAM=http://<api-service>.railway.internal:8080
VITE_GOOGLE_CLIENT_ID=321822470971-86scd5gkq8jub3h5h8hl8jq2dn6uk411.apps.googleusercontent.com
```

- `API_UPSTREAM`: nginx dùng để proxy `/api/` và `/hubs/` sang API. Có thể dùng public URL
  `https://<api-domain>` nếu private networking không phân giải được.
- `VITE_GOOGLE_CLIENT_ID` là **build-time**: Railway truyền biến của service làm build arg cho
  Dockerfile (`ARG VITE_GOOGLE_CLIENT_ID`). Sau khi đổi phải **redeploy có clear build cache**,
  không chỉ restart.
- Target port của frontend: **80**.

## 4. Google Cloud Console

Thêm **Authorized JavaScript origins**:
- `https://<frontend-domain>`
- `http://localhost:3000` (để chạy Docker local)

Nếu frontend đổi domain, phải cập nhật cả origin lẫn `App__FrontendUrl`.

## 5. Database & migrations

### 5.1 Tự động bằng pre-deploy (khuyến nghị)

`src/PrintGrid.Api/Dockerfile` đã:
- sinh `/app/migrations.sql` ở **build stage** bằng `dotnet ef migrations script --idempotent`;
- cài `psql` trong runtime image (chỉ để chạy bước này).

Cấu hình trên Railway:

1. Service **`api` → Variables**, thêm biến tham chiếu:
   ```
   DATABASE_URL=${{Postgres.DATABASE_URL}}
   ```
2. Service **`api` → Settings → Deploy → Pre-deploy Command**:
   ```
   sh -c 'psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f /app/migrations.sql'
   ```
   Phải bọc trong `sh -c '...'`: Railway chạy lệnh không qua shell, nên nếu viết trực tiếp
   `psql "$DATABASE_URL" ...` thì biến không được expand và `psql` rơi về socket local (không tồn tại).
3. Deploy lại.

Kết quả: **mỗi lần deploy**, Railway chạy script một lần trước khi container mới nhận traffic.
Script là idempotent — không có migration mới thì không làm gì; migration lỗi thì deployment bị
chặn, container cũ vẫn chạy.

> Migration là một chiều: rollback app **không** rollback schema. Vì vậy mỗi migration nên
> backward-compatible (thêm cột/bảng trước, xoá/đổi tên ở lần deploy sau) để cửa sổ rolling
> deploy không làm container cũ vỡ.

### 5.1b Slicer — CuraEngine & profile máy

**Binary**: đã build tự động trong Dockerfile `src/PrintGrid.Api/Dockerfile` từ source
(`Ultimaker/CuraEngine@2.7.0`). Không cần download thêm file nào.

**Biến môi trường**: chỉ cần `QuoteSlicer__ExecutablePath=/opt/cura/CuraEngine` (đã liệt kê ở mục 2).
Không cần `QuoteSlicer__Machines__*` — tất cả profile lưu trong database (`cura_profiles`) và được
Admin upload qua giao diện `/admin/cura-profiles`. Mỗi profile chứa resolved JSON + metadata
(manufacturer, model, material, nozzle diameter…) và dùng khi khách đặt hàng có multi-color.

**Quy trình admin upload profile**:

1. Mở `/admin/cura-profiles`, nhập form (tên profile, hãng, model, vật liệu, số đầu đùn).
2. Upload file `.resolved.json` (720 settings đã evaluate formula) hoặc paste nội dung JSON.
3. Lưu → hệ thống tự đối chiếu với các máy in trong lab. Máy khớp sẽ chuyển trạng thái
   `PendingReview`. Admin bấm **Duyệt báo giá** trên trang này.
4. Khi khách chọn màu nhiều hơn khả năng máy (`extruderCount`), API tự reject quotation.

> Profile test mẫu (virtual) có sẵn tại `infra/slicer/profiles/local-standard-pla.resolved.json`
> và `local-fast-pla.resolved.json`. Đây là hồ sơ giả lập cho local smoke test; production cần
> profile từ nhà sản xuất máy thật (extract từ bản Cura desktop tương thích).

### 5.2 Thủ công (fallback / lần đầu trước khi bật pre-deploy)

Container runtime không có `dotnet ef`, nên chạy từ máy local qua **TCP Proxy** của Postgres
(Railway → Postgres → Settings → Networking → TCP Proxy → lấy `DATABASE_PUBLIC_URL`).

PowerShell:
```powershell
$env:ConnectionStrings__DefaultConnection = "Host=<host>;Port=<port>;Database=railway;Username=postgres;Password=<password>"
dotnet ef database update --project src/PrintGrid.Infrastructure.Shared --startup-project src/PrintGrid.Api
```

Hoặc áp thẳng SQL đã chuẩn bị (idempotent, dùng khi không có `dotnet ef`):
```powershell
psql "<DATABASE_PUBLIC_URL>" -f documentation/deployment/sql/20261104_apply_pending_migrations.sql
```

### 5.3 Nạp dữ liệu demo

```powershell
# nếu máy có psql
psql "<DATABASE_PUBLIC_URL>" -f seed_demo.sql

# hoặc dùng container Postgres
docker run --rm -i -v "${PWD}:/w" postgres:16-alpine psql "<DATABASE_PUBLIC_URL>" -f /w/seed_demo.sql
```

Tài khoản demo sau khi seed: `demo@`, `lab@`, `qc@`, `ops@printgrid.dev`, mật khẩu `Demo@123`.

> Dashboard Hangfire ở `/jobs` dùng filter riêng. API **không** tự migrate lúc khởi động —
> migration chỉ chạy ở bước pre-deploy (5.1) hoặc thủ công (5.2).

## 6. MinIO

1. Deploy image `minio/minio`, command `server /data --console-address ":9001"`.
2. Gắn **Volume** tại `/data` (nếu không, file model/ảnh QC mất khi redeploy).
3. Đặt `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` cho service MinIO, dùng đúng cặp đó cho
   `MinIO__AccessKey` / `MinIO__SecretKey` của API.
4. Bật public networking cho cổng **9000**; dùng host đó cho cả `Endpoint` và `PublicEndpoint`.

Ứng dụng tự tạo bucket khi upload lần đầu (`printgrid-models`, `printgrid-slices`, `printgrid-photos`).

⚠ `MinIO__UseSsl` áp dụng cho **cả** client nội bộ và client tạo presigned URL. Vì vậy đặt
`Endpoint` = host public + `UseSsl=true` là cách chạy được ngay trên Railway. Nếu muốn API gọi MinIO
qua private networking (`http://minio.railway.internal:9000`) trong khi trình duyệt dùng presigned
URL HTTPS, cần tách thêm `PublicUseSsl` trong `MinioFileStorage` — chưa có trong code hiện tại.

## 7. Kiểm tra sau deploy

1. `GET https://<api-domain>/health` → 200.
2. `POST https://<api-domain>/api/v1/auth/login` với `ops@printgrid.dev` / `Demo@123` → 200.
3. Mở `https://<frontend-domain>/login` → nút Google hiển thị (không còn cảnh báo "chưa cấu hình").
4. Bấm Google → đăng nhập → quay về đúng trang.
5. `/forgot-password` với email thật → nhận được mail, link trỏ về domain Railway.
6. Upload một model ở `/models` → file lưu vào MinIO; tải lại được.
7. Đặt một đơn → job tự gán cho lab → trang `/ops/payments` (Ops) hiển thị đơn chờ cọc nếu tắt
   `Payment__AutoConfirmOnPlace`.
8. Chọn máy đã có profile được duyệt → báo giá đa màu → file slicing thành công (thời gian + vật liệu từng màu).

## 8. Sự cố thường gặp

| Hiện tượng | Nguyên nhân | Xử lý |
|---|---|---|
| Deploy fail healthcheck `/health` | app nghe cổng khác target port | đặt Target Port 8080; giữ `ASPNETCORE_HTTP_PORTS=8080` |
| `/health` trả 307 liên tục | HTTPS redirection sau proxy | `HttpsRedirection__Enabled=false` |
| Trang gọi API 502 | `API_UPSTREAM` sai/không phân giải | đổi sang public API URL rồi redeploy |
| Nút Google biến mất | thiếu `VITE_GOOGLE_CLIENT_ID` lúc build | thêm biến + redeploy clear cache |
| Google báo `origin_mismatch` | origin chưa khai báo | thêm domain Railway vào OAuth client |
| Quên mật khẩu 500 | Redis chưa cấu hình | đặt `ConnectionStrings__Redis` đúng |
| File upload mất sau redeploy | MinIO không có volume | gắn volume `/data` |
| Email không gửi | Railway chặn SMTP ra ngoài (một số plan) | kiểm tra log; nếu bị chặn dùng Resend/SendGrid SMTP |
| Quotation slice thất bại 500 | thiếu `QuoteSlicer__ExecutablePath` hoặc binary không executable | đặt biến = `/opt/cura/CuraEngine`; kiểm tra `chmod 755` trong container |
| Slice reject "profile máy chưa có profile phù hợp" | máy chưa được admin duyệt trên `/admin/cura-profiles` | bấm **Tìm lại profile** → **Duyệt báo giá** cho máy đó |
