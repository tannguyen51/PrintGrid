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

## 5. Chuẩn bị database (một lần, sau khi Postgres chạy)

Container runtime không có `dotnet ef`, nên chạy migration từ máy local qua **TCP Proxy** của
Postgres (Railway → Postgres → Settings → Networking → TCP Proxy → lấy `DATABASE_PUBLIC_URL`).

PowerShell:
```powershell
$env:ConnectionStrings__DefaultConnection = "Host=<host>;Port=<port>;Database=railway;Username=postgres;Password=<password>"
dotnet ef database update --project src/PrintGrid.Infrastructure.Shared --startup-project src/PrintGrid.Api
```

Nạp dữ liệu demo (4 tài khoản demo, lab, máy in):
```powershell
# nếu máy có psql
psql "<DATABASE_PUBLIC_URL>" -f seed_demo.sql

# hoặc dùng container Postgres
docker run --rm -i -v "${PWD}:/w" postgres:16-alpine psql "<DATABASE_PUBLIC_URL>" -f /w/seed_demo.sql
```

Tài khoản demo sau khi seed: `demo@`, `lab@`, `qc@`, `ops@printgrid.dev`, mật khẩu `Demo@123`.

> Lưu ý: dashboard Hangfire ở `/jobs` dùng filter riêng; API hiện **không tự chạy migration**
> lúc khởi động. Nếu muốn tự động hoá, có thể thêm bước chạy migration trong CI hoặc một service
> one-off — đừng bật auto-migrate mù trên production.

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
