# Capstone Project Report
## Report 6 – Software User Guides

PrintGrid — Distributed 3D Printing Fulfillment & Scheduling Platform
Hanoi, March 2027


## I. Record of Changes

| Date | A/M/D | In charge | Change Description |
|---|---|---|---|
| 15/10/2026 | A | Whole team | Initial creation of the User Guides |
| 10/03/2027 | M | KhanhNTHE03579 | Updated to match the implemented release |


## II. Release Package & User Guides

### 1. Deliverable Package

This section lists the items delivered in the current release.

| No. | Deliverable Item | Description |
|---|---|---|
| 1 | Source codes (backend) | PrintGrid API: module-sliced .NET solution under src/ (PrintGrid.Api, PrintGrid.SharedKernel, PrintGrid.Infrastructure.Shared, modules Customer / Scheduling / Lab / Hub / Analytics / Admin) |
| 2 | Source codes (frontend) | React 19 + TypeScript SPA under frontend/ (auth, home, models, orders features) |
| 3 | Database schema & migrations | EF Core migrations under src/PrintGrid.Infrastructure.Shared/Persistence/Migrations/ (PostgreSQL, schema per module: customer, scheduling, ...) |
| 4 | Environment definition | docker-compose.yml (postgres, redis, minio, api, frontend) + .env.example |
| 5 | Test suite | tests/PrintGrid.UnitTests (27 passing unit tests) |
| 6 | Test documentation | documentation/Report5_TestDocumentation_PrintGrid.md |
| 7 | Project management documentation | documentation/Report2_ProjectManagementPlan_PrintGrid.md (Report 2) |
| 8 | User guide | documentation/Report6_SoftwareUserGuides_PrintGrid.md (this document) |
| 9 | Final report | Not yet written; planned as documentation/Report7_FinalProjectReport_PrintGrid.md (Report 7) |
| 10 | Requirements & design documentation | documentation/ (17 numbered markdown documents from Context to API Design) |
| 11 | Presentation slide | Separate deliverable, prepared before the final demo |


### 2. Installation Guides

#### 2.1 System Requirements

Software required to run the application:

| Software | Version | Purpose |
|---|---|---|
| Operating system | Windows 10/11, macOS, or Linux | Host environment |
| .NET SDK | 8.x (the solution targets net8.0; .NET 9 SDK also builds it) | Backend runtime and build |
| Node.js with npm | 22 or later | Frontend build and Vite dev server |
| Docker Desktop | 4.x | Runs PostgreSQL, Redis, MinIO containers |
| PostgreSQL | 16 (run via Docker image postgres:16-alpine) | Primary database |
| Redis | 7.x (run via Docker image redis:7-alpine) | Cache / session store |
| MinIO | latest (run via Docker image minio/minio) | Object storage for 3D model files and photos |
| Web browser | A recent Chrome, Firefox, or Edge | Client access to the SPA |

Recommended hardware for running the full stack locally: 8 GB RAM, 4 CPU cores, about 10 GB free disk space (Docker images, Node modules, .NET packages).

Ports used: 5432 (PostgreSQL), 6379 (Redis), 9000/9001 (MinIO API/console), 5000 (backend API), 5173 (Vite dev server), 3000 (frontend served via Nginx in Docker).

#### 2.2 Installation Instruction

The steps below reproduce the verified setup used by the team.

Step 1 — Get the source code.

    git clone <repository-url>
    cd PrintGrid

Step 2 — Create the environment file.

    cp .env.example .env

Edit .env and set three secrets that compose requires:
- POSTGRES_PASSWORD
- MINIO_ROOT_PASSWORD
- JWT_SIGNING_KEY (at least 32 characters; generate with: openssl rand -base64 48)

Step 3 — Restore dependencies.

    dotnet restore
    cd frontend && npm install && cd ..

Step 4 — Start the infrastructure services.

    docker compose up -d postgres redis minio

Wait until the health checks pass (the compose file defines healthcheck for each service).

Step 5 — Apply database migrations.

    dotnet ef database update --project src/PrintGrid.Infrastructure.Shared --startup-project src/PrintGrid.Api

This creates all module schemas through the single shared PrintGridDbContext.

Step 6 — Run the backend.

    dotnet run --project src/PrintGrid.Api

The API listens at http://localhost:5000.

Step 7 — Run the frontend.

    cd frontend && npm run dev

The SPA is served at http://localhost:5173. The Vite dev server proxies /api and /hubs to localhost:5000, so no separate CORS configuration is needed.

Alternative — run everything in containers:

    docker compose up -d --build

Frontend is then on http://localhost:3000, API on http://localhost:5000.

Useful endpoints after installation:

| Endpoint | URL |
|---|---|
| SPA (dev) | http://localhost:5173 |
| SPA (Docker) | http://localhost:3000 |
| Swagger / OpenAPI | http://localhost:5000/swagger |
| Health check | http://localhost:5000/health |
| Hangfire dashboard | http://localhost:5000/jobs |
| MinIO console | http://localhost:9001 |

Build verification commands:

    dotnet build
    dotnet test tests/PrintGrid.UnitTests
    cd frontend && npm run build


### 3. User Manual

#### 3.1 Overview

PrintGrid is a distributed 3D printing fulfillment and scheduling platform. A customer orders through one unified web interface, while the platform assigns production work to a network of independent printing labs. The academic core is the scheduling and assignment engine: delivery dates are derived from actual network capacity instead of fixed lead-time tables.

Home page (public) — dark theme with a purple accent — introduces the platform with sections: hero and key metrics, listed features, how it works, call to action, and a footer with links. The top navigation contains: Trang chủ (Home), Tính năng (Features), Cách hoạt động (How it works), Bắt đầu (Get started), plus Đăng nhập (Login) and Tạo tài khoản (Register) when logged out.

Feature flow implemented in this release:

    Register / Login (modal from home page)
            |
            v
    Home page (public landing)
            |
            v
    Model Library (add / view / search / edit / delete 3D models)
            |
            v
    Logout

The quotes and orders module has a partial implementation: the backend exposes a place-order endpoint and a customer order page exists, but the order list is not yet wired to the backend, so the order flow is not part of the verified user journeys in this release. It is described in the repository documentation (FR-CUST-006 to 009) and scheduled in the project plan.

#### 3.2 Workflow 1 — Account (Register, Login, Logout)

Purpose: create an account, sign in, and sign out. This is the entry gate to the customer areas.

Step-by-step guide:

1. Open the application. The home page is shown.
2. Click Đăng nhập (Login) or Tạo tài khoản (Register) in the top-right. A modal window opens over the current page (the login view shows the 3D model showcase on the right; the register view shows the form only).
3. Register:
   - Fill in Họ và tên (full name), Email, Mật khẩu (password, at least 6 characters), and Xác nhận mật khẩu (confirm password).
   - The form validates input as you type and shows Vietnamese messages under each invalid field (for example, Mật khẩu ít nhất 6 ký tự, Mật khẩu xác nhận không khớp).
   - Click TẠO TÀI KHOẢN (Create account). On success you are taken back to the home page and logged in.
   - If the email is already registered, the server returns the message Email ... is already registered and no account is created.
4. Login:
   - Enter email and password, click ĐĂNG NHẬP (Login).
   - If the credentials are wrong, the screen shows Email hoặc mật khẩu không đúng. The same message is shown for an unknown email and for a wrong password on a known account.
5. Logout:
   - When logged in, the navbar shows Thư viện model (Model library), Bảng điều khiển (Dashboard), and Đăng xuất (Logout). The model library page has a red Đăng xuất button next to Thêm model.
   - Clicking Đăng xuất clears the session tokens and returns to the public area.

Expected results:
- A valid registration logs the user in immediately and the navbar switches to the authenticated state.
- After logout, protected pages are no longer accessible (the route guard redirects to the login screen).

#### 3.3 Workflow 2 — Model Library (full CRUD)

Purpose: manage personal 3D model metadata. This is a complete Create, Read, Update, Delete feature: list, create, view details, edit, delete, and search.

Step-by-step guide:

1. Log in and click Thư viện model in the navbar, or open the /models route.
2. Create:
   - Click Thêm model (Add model).
   - In the dialog, fill in Tên model (name), Mô tả (optional description), Tên file (file name), Định dạng (format, for example STL/OBJ/3MF), Kích thước bytes (size in bytes), and Tags (comma-separated).
   - Click Thêm model. The table refreshes immediately and the new row appears.
3. Read and search:
   - The table shows: name, format, size, tags, created date, and action buttons.
   - Type in the search box to filter by name or description (the request is sent to the server with the query parameter search).
   - Click the eye icon to open a detail drawer with file name, description, tags, and timestamps.
4. Update:
   - Click the pencil icon on a row. The same dialog opens with the current values; change them and click Lưu thay đổi (Save changes). The table shows the updated data without reloading the page.
5. Delete:
   - Click the trash icon. A confirmation dialog asks whether to delete; click Xóa to confirm. The row is removed. Deleting a model that no longer exists reports an error.

Behavior worth knowing:
- Tags are de-duplicated, empty tags are dropped, and the format is stored in uppercase.
- Isolation is enforced by the backend: a signed-in customer only ever sees their own models. Accessing another account's model by its id returns 404, and foreign models never appear in the list. A request without a token returns 401.

#### 3.4 Workflow 3 — Print Ordering (planned, not yet end-to-end)

This workflow is described in the design documentation and scheduled in the project plan, but it is not yet complete in the current release. It is included here to state its intended behavior and its current status honestly.

Intended flow:

1. From the model library, select a model and start an order.
2. Configure the print: material, color, quality grade (Draft/Standard/High/Ultra), infill percentage, quantity, and post-processing.
3. Request a quote. The system analyzes and slices the model asynchronously, runs a trial placement across the network, and returns an itemized price with an estimated delivery date.
4. Confirm the order, complete payment in test mode, and track status (Confirmed, In Production, Quality Check, Shipping, Delivered).

Current status in this release:
- Backend: the OrdersController implements POST /api/v1/orders (place order) and the scheduling module exposes a job-assignment endpoint; the order aggregate, quote aggregate, and machine/lab/job entities are modeled with their repositories and migrations.
- Frontend: an order page (OrdersPage) exists and displays columns for order number, status, total, promised delivery date, and created date, but its data call (GET /orders) has no backend handler yet, so the page shows an error until that endpoint is implemented.
- The model upload and preview, configuration screen, quote screen, and payment screen are not yet implemented as customer-facing features in this release.

Because of the above, Workflow 3 is not part of the verified user journeys of this release; it is scheduled in the plan (sprint scope in documentation/Report2_ProjectManagementPlan_PrintGrid.md) and verified by the simulation framework on the scheduling side.


## Notes

- This document reflects the implemented functionality at the time of writing. Anything not implemented yet is stated as planned rather than described as working.
- The user interface language is Vietnamese; menu names above are given in Vietnamese with an English gloss in parentheses.


*Report prepared by the PrintGrid team · Capstone SEP490 · Supervisor: Nguyễn Tấn Phúc · Sep 2026 – Mar 2027*