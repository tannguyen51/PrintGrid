# PrintGrid

Distributed 3D printing fulfillment and scheduling platform. Capstone project FA26SE249.

Full specification lives in [documentation/](documentation/) — start with [documentation/README.md](documentation/README.md).

## Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8, ASP.NET Core, Modular Monolith + Clean Architecture |
| CQRS / events | MediatR |
| Persistence | PostgreSQL 16 + EF Core 8 (schema per module) |
| Cache | Redis 7 |
| Background jobs | Hangfire (PostgreSQL storage) |
| Object storage | MinIO (S3-compatible) |
| Frontend | React 19 + TypeScript, Vite, MUI, TanStack Query |
| Real-time | SignalR |

## Layout

```
src/
  PrintGrid.Api/                    ASP.NET Core host: controllers, hubs, middleware, DI bootstrap
  PrintGrid.SharedKernel/           Entity, AggregateRoot, ValueObject, Result, Money, Address, IModule
  PrintGrid.Infrastructure.Shared/  DbContext, UnitOfWork, Redis cache, MinIO storage, EF migrations
  PrintGrid.Modules/
    Customer/    Domain | Application | Infrastructure
    Scheduling/  Domain | Application | Infrastructure   <- core algorithm lives here
    Lab/ Hub/ Analytics/ Admin/
tests/
  PrintGrid.UnitTests/          domain and algorithm tests
  PrintGrid.IntegrationTests/   API tests (Testcontainers)
frontend/                       React SPA
infra/postgres/init/            schema bootstrap SQL
```

Dependency rule per module: `Application -> Domain <- Infrastructure`. Modules never reference each
other directly; they communicate through domain events dispatched by MediatR on `SaveChangesAsync`.

## First-time setup

Prerequisites: .NET 8 SDK (or 9 — the solution targets `net8.0`), Node 22+, Docker Desktop.

```bash
cp .env.example .env       # then fill in POSTGRES_PASSWORD, MINIO_ROOT_PASSWORD, JWT_SIGNING_KEY
dotnet restore
cd frontend && npm install && cd ..
```

`JWT_SIGNING_KEY` must be at least 32 characters. Generate one with
`openssl rand -base64 48`. Compose refuses to start if any of the three secrets is missing.

## Running

Infrastructure only (recommended while developing — API and SPA run on the host):

```bash
docker compose up -d postgres redis minio
dotnet ef database update --project src/PrintGrid.Infrastructure.Shared --startup-project src/PrintGrid.Api
dotnet run --project src/PrintGrid.Api        # http://localhost:5000
cd frontend && npm run dev                    # http://localhost:5173
```

Everything in containers:

```bash
docker compose up -d --build                  # SPA on :3000, API on :5000
```

| Endpoint | URL |
|---|---|
| Swagger | http://localhost:5000/swagger |
| Health | http://localhost:5000/health |
| Hangfire dashboard | http://localhost:5000/jobs |
| MinIO console | http://localhost:9001 |

The Vite dev server proxies `/api` and `/hubs` to `localhost:5000`, so the SPA needs no CORS
configuration in development.

## Verify

```bash
dotnet build                                  # whole solution
dotnet test tests/PrintGrid.UnitTests         # domain + scheduling tests
cd frontend && npm run build                  # tsc -b then vite build
```

## Migrations

All modules share one `PrintGridDbContext`; each module's `IEntityTypeConfiguration` classes are
discovered through `ModuleAssemblyRegistry` at startup, so a single migration set covers every
schema.

```bash
dotnet ef migrations add <Name> \
  --project src/PrintGrid.Infrastructure.Shared \
  --startup-project src/PrintGrid.Api \
  --output-dir Persistence/Migrations
```

## Adding a module

1. Three class libraries under `src/PrintGrid.Modules/<Name>/` (Domain, Application, Infrastructure).
2. `Application` references `Domain`; `Infrastructure` references both plus `PrintGrid.Infrastructure.Shared`.
3. Add `AssemblyMarker`, `<Name>ApplicationExtensions`, `<Name>Module : IModule`, `<Name>ModuleRegistration`.
4. Register the module in [src/PrintGrid.Api/Bootstrap/ModuleRegistrar.cs](src/PrintGrid.Api/Bootstrap/ModuleRegistrar.cs).
5. Reference both new projects from `PrintGrid.Api.csproj` and add all three to the solution.

Package versions are centralized in [Directory.Packages.props](Directory.Packages.props) — reference
packages without a `Version` attribute.

## Security notes

- Every controller requires an authenticated JWT with a role policy; there are no anonymous endpoints
  beyond `/health`. Auth endpoints are not implemented yet — `POST /auth/login` and `/auth/refresh`
  are consumed by the SPA and still need handlers.
- The Hangfire dashboard at `/jobs` requires an authenticated user in the `Admin` role.
- `.env` is gitignored. Never commit real credentials; `appsettings.json` ships with empty secret
  values that must come from environment variables.
