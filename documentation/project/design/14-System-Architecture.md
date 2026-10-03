# System Architecture - PrintGrid Platform

## 1. Architecture Overview

### 1.1 Architecture Style
**Modular Monolith with Clean Architecture**

PrintGrid uses a **Modular Monolith** architecture where the system is organized into loosely coupled modules within a single deployable unit. Each module follows **Clean Architecture** principles with clear layer separation.

**Why Modular Monolith?**
- ✅ Appropriate for team size (5 developers)
- ✅ Simpler deployment and operations
- ✅ Reduced operational complexity vs. microservices
- ✅ Clear module boundaries enable future microservices migration if needed
- ✅ Single database transaction scope (important for scheduling consistency)

---

### 1.2 High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                      Client Layer                                │
├─────────────────────────────────────────────────────────────────┤
│  React SPA (TypeScript)                                          │
│  - Customer Portal                                               │
│  - Lab Portal                                                    │
│  - Hub Console                                                   │
│  - Operations Dashboard                                          │
│  - Admin Console                                                 │
└────────────┬────────────────────────────────────────────────────┘
             │ HTTPS / REST API / SignalR
┌────────────▼────────────────────────────────────────────────────┐
│                    API Gateway / BFF Layer                       │
│  ASP.NET Core Web API                                            │
│  - Authentication Middleware (JWT)                               │
│  - Authorization (RBAC)                                          │
│  - Rate Limiting                                                 │
│  - CORS                                                          │
│  - SignalR Hubs (real-time)                                      │
└────────────┬────────────────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────────────────┐
│              Application Core (Modular Monolith)                 │
├──────────┬──────────┬──────────┬───────────┬────────┬───────────┤
│ Customer │   Lab    │   Hub    │Scheduling │Analytics│  Admin   │
│  Module  │  Module  │  Module  │  Module   │ Module  │  Module  │
├──────────┴──────────┴──────────┴───────────┴────────┴───────────┤
│              Shared Kernel (Common Domain)                       │
│  - Common Value Objects, Base Entities, Domain Events            │
└────────────┬────────────────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────────────────┐
│                  Infrastructure Layer                            │
├─────────────────┬──────────────┬──────────────┬─────────────────┤
│   PostgreSQL    │    Redis     │    MinIO     │   Hangfire      │
│  (Primary Data) │ (Cache/Pub)  │(File Storage)│(Background Jobs)│
└─────────────────┴──────────────┴──────────────┴─────────────────┘
```

---

## 2. Clean Architecture Layers

Each module follows Clean Architecture with four layers:

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  Controllers, ViewModels, DTOs                               │
│  Dependency: Application Layer                               │
└────────────┬────────────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────────────┐
│                   Application Layer                          │
│  Use Cases, Commands, Queries, Handlers                      │
│  Application Services, DTOs                                  │
│  Dependency: Domain Layer                                    │
└────────────┬────────────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────────────┐
│                     Domain Layer                             │
│  Entities, Value Objects, Domain Services                    │
│  Business Rules, Domain Events                               │
│  Repository Interfaces                                       │
│  NO DEPENDENCIES (Pure business logic)                       │
└────────────▲────────────────────────────────────────────────┘
             │
┌────────────┴────────────────────────────────────────────────┐
│                 Infrastructure Layer                         │
│  Repository Implementations (EF Core)                        │
│  External Service Integrations                               │
│  Persistence, File Storage, Email, etc.                      │
│  Dependency: Domain Layer (implements interfaces)            │
└─────────────────────────────────────────────────────────────┘
```

**Dependency Rule:** Dependencies point inward. Domain has no dependencies.

---

## 3. Module Structure

### 3.1 Module Organization

```
src/
├── PrintGrid.Api/                          # API Entry Point
│   ├── Controllers/                        # API Controllers
│   ├── Hubs/                              # SignalR Hubs
│   ├── Middleware/                        # Custom Middleware
│   ├── Program.cs                         # Application Bootstrap
│   └── appsettings.json                   # Configuration
│
├── PrintGrid.Modules/                      # Modules Root
│   │
│   ├── Customer/                           # Customer Module
│   │   ├── PrintGrid.Modules.Customer.Domain/
│   │   │   ├── Entities/
│   │   │   │   ├── Customer.cs
│   │   │   │   ├── Order.cs
│   │   │   │   ├── OrderItem.cs
│   │   │   │   └── Quote.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── Address.cs
│   │   │   │   ├── PrintConfiguration.cs
│   │   │   │   └── Money.cs
│   │   │   ├── Events/
│   │   │   │   ├── OrderPlacedEvent.cs
│   │   │   │   └── OrderCompletedEvent.cs
│   │   │   └── Repositories/
│   │   │       ├── ICustomerRepository.cs
│   │   │       └── IOrderRepository.cs
│   │   │
│   │   ├── PrintGrid.Modules.Customer.Application/
│   │   │   ├── Commands/
│   │   │   │   ├── PlaceOrderCommand.cs
│   │   │   │   ├── PlaceOrderCommandHandler.cs
│   │   │   │   └── CreateQuoteCommand.cs
│   │   │   ├── Queries/
│   │   │   │   ├── GetOrderQuery.cs
│   │   │   │   └── GetOrderQueryHandler.cs
│   │   │   ├── Services/
│   │   │   │   └── QuoteService.cs
│   │   │   ├── DTOs/
│   │   │   │   ├── OrderDto.cs
│   │   │   │   └── QuoteDto.cs
│   │   │   └── Validators/
│   │   │       └── PlaceOrderValidator.cs
│   │   │
│   │   └── PrintGrid.Modules.Customer.Infrastructure/
│   │       ├── Persistence/
│   │       │   ├── CustomerDbContext.cs
│   │       │   ├── Repositories/
│   │       │   │   ├── CustomerRepository.cs
│   │       │   │   └── OrderRepository.cs
│   │       │   └── Configurations/
│   │       │       ├── OrderConfiguration.cs
│   │       │       └── CustomerConfiguration.cs
│   │       └── ExternalServices/
│   │           └── PaymentGateway.cs
│   │
│   ├── Scheduling/                         # Scheduling Module ⭐ CORE
│   │   ├── PrintGrid.Modules.Scheduling.Domain/
│   │   │   ├── Entities/
│   │   │   │   ├── Job.cs
│   │   │   │   ├── Machine.cs
│   │   │   │   ├── Lab.cs
│   │   │   │   ├── Assignment.cs
│   │   │   │   └── Schedule.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── BuildVolume.cs
│   │   │   │   ├── Tolerance.cs
│   │   │   │   └── JobSpecification.cs
│   │   │   ├── Services/                   # Domain Services
│   │   │   │   ├── CapabilityFilter.cs
│   │   │   │   ├── AssignmentScorer.cs
│   │   │   │   ├── Scheduler.cs
│   │   │   │   └── Rescheduler.cs
│   │   │   ├── Events/
│   │   │   │   ├── JobAssignedEvent.cs
│   │   │   │   ├── PrintFailureEvent.cs
│   │   │   │   └── ReschedulingTriggeredEvent.cs
│   │   │   └── Repositories/
│   │   │       ├── IJobRepository.cs
│   │   │       ├── ILabRepository.cs
│   │   │       └── IMachineRepository.cs
│   │   │
│   │   ├── PrintGrid.Modules.Scheduling.Application/
│   │   │   ├── Commands/
│   │   │   │   ├── AssignJobCommand.cs
│   │   │   │   ├── RescheduleCommand.cs
│   │   │   │   └── GenerateQuoteScheduleCommand.cs
│   │   │   ├── Queries/
│   │   │   │   ├── GetScheduleQuery.cs
│   │   │   │   └── GetLabCapacityQuery.cs
│   │   │   └── Services/
│   │   │       ├── SlicingService.cs
│   │   │       ├── GeometryAnalysisService.cs
│   │   │       └── CalibrationService.cs
│   │   │
│   │   └── PrintGrid.Modules.Scheduling.Infrastructure/
│   │       ├── Persistence/
│   │       │   ├── SchedulingDbContext.cs
│   │       │   └── Repositories/
│   │       └── ExternalServices/
│   │           ├── CuraEngineAdapter.cs
│   │           └── SlicerProxy.cs
│   │
│   ├── Lab/                                # Lab Module
│   ├── Hub/                                # Hub Module
│   ├── Analytics/                          # Analytics Module
│   └── Admin/                              # Admin Module
│
├── PrintGrid.SharedKernel/                 # Shared Domain
│   ├── Common/
│   │   ├── Entity.cs                      # Base Entity
│   │   ├── ValueObject.cs                 # Base VO
│   │   ├── AggregateRoot.cs
│   │   └── DomainEvent.cs
│   ├── Interfaces/
│   │   ├── IRepository.cs
│   │   └── IUnitOfWork.cs
│   └── Results/
│       └── Result.cs                       # Result pattern
│
└── PrintGrid.Infrastructure.Shared/        # Shared Infrastructure
    ├── Persistence/
    │   ├── PrintGridDbContext.cs          # Main DbContext
    │   └── UnitOfWork.cs
    ├── EventBus/
    │   └── MediatREventBus.cs             # Domain Events via MediatR
    ├── BackgroundJobs/
    │   └── HangfireJobScheduler.cs
    └── FileStorage/
        └── MinioFileStorage.cs
```

---

## 4. Module Communication

### 4.1 Communication Patterns

**Within a Module:** Direct method calls (compile-time dependency)

**Between Modules:** Domain Events (loose coupling)

```csharp
// Customer Module publishes event
public class Order : AggregateRoot
{
    public void Confirm()
    {
        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id, Items));
    }
}

// Scheduling Module subscribes
public class OrderConfirmedEventHandler 
    : INotificationHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent evt, ...)
    {
        // Trigger job assignment
        foreach (var item in evt.Items)
        {
            await _assignmentService.AssignJob(item);
        }
    }
}
```

**Key Events:**
- `OrderConfirmedEvent` → triggers job assignment
- `JobAssignedEvent` → notifies lab
- `PrintFailureEvent` → triggers rescheduling
- `InspectionFailedEvent` → creates reprint job

---

## 5. Data Architecture

### 5.1 Database Strategy

**Single Database, Schema Per Module**

```sql
-- PostgreSQL Database: printgrid_db

-- Customer Schema
CREATE SCHEMA customer;
CREATE TABLE customer.customers (...);
CREATE TABLE customer.orders (...);
CREATE TABLE customer.quotes (...);

-- Scheduling Schema
CREATE SCHEMA scheduling;
CREATE TABLE scheduling.jobs (...);
CREATE TABLE scheduling.labs (...);
CREATE TABLE scheduling.machines (...);
CREATE TABLE scheduling.assignments (...);

-- Lab Schema
CREATE SCHEMA lab;
CREATE TABLE lab.material_inventory (...);
CREATE TABLE lab.job_queue (...);

-- Hub Schema
CREATE SCHEMA hub;
CREATE TABLE hub.inspection_results (...);
CREATE TABLE hub.shipments (...);

-- Shared Schema
CREATE SCHEMA shared;
CREATE TABLE shared.users (...);
CREATE TABLE shared.audit_log (...);
```

**Why Single Database?**
- ✅ ACID transactions across modules when needed
- ✅ Simpler operations (one backup, one connection pool)
- ✅ Appropriate for modular monolith
- ✅ Schema isolation provides logical separation
- 🔄 Can split databases per module if migrating to microservices

---

### 5.2 Entity Framework Core Configuration

```csharp
// Modular DbContext approach
public class PrintGridDbContext : DbContext
{
    // Each module configures its own entities
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("customer");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CustomerModule).Assembly);
        
        modelBuilder.HasDefaultSchema("scheduling");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SchedulingModule).Assembly);
        
        // ... other modules
    }
}

// Module-specific configuration
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", "customer");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Status).HasConversion<string>();
        builder.OwnsOne(o => o.DeliveryAddress);
        // ...
    }
}
```

---

## 6. Technology Stack Details

### 6.1 Backend Stack

```
┌─────────────────────────────────────────┐
│ Framework: .NET 8.0                     │
│ Language: C# 12                         │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Web Framework: ASP.NET Core 8.0         │
│ - Web API                               │
│ - SignalR (real-time)                   │
│ - JWT Authentication                    │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ ORM: Entity Framework Core 8.0          │
│ - Code-First Migrations                 │
│ - LINQ Queries                          │
│ - Change Tracking                       │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ CQRS & Mediator: MediatR                │
│ - Command/Query separation              │
│ - Domain Event dispatching              │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Validation: FluentValidation            │
│ - Request validation                    │
│ - Business rule validation              │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Background Jobs: Hangfire               │
│ - Async quote generation                │
│ - Scheduled calibration                 │
│ - Email notifications                   │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Mapping: AutoMapper                     │
│ - Entity → DTO mapping                  │
└─────────────────────────────────────────┘
```

---

### 6.2 Frontend Stack

```
┌─────────────────────────────────────────┐
│ Framework: React 18                     │
│ Language: TypeScript 5.0                │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ State Management: React Query (TanStack)│
│ - Server state caching                  │
│ - Automatic refetching                  │
│ - Optimistic updates                    │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Routing: React Router v6                │
│ - Client-side routing                   │
│ - Protected routes                      │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ UI Components: Material-UI (MUI)        │
│ - Pre-built components                  │
│ - Responsive design                     │
│ - Theming support                       │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ 3D Rendering: Three.js + React-Three    │
│ - Model preview                         │
│ - Interactive viewer                    │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Forms: React Hook Form                  │
│ - Form state management                 │
│ - Validation                            │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Real-Time: SignalR Client               │
│ - Order status updates                  │
│ - Job notifications                     │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ HTTP Client: Axios                      │
│ - API requests                          │
│ - Interceptors for auth                 │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Build Tool: Vite                        │
│ - Fast dev server                       │
│ - Optimized production builds           │
└─────────────────────────────────────────┘
```

---

### 6.3 Infrastructure Stack

```
┌──────────────────────────────────────────────┐
│ Database: PostgreSQL 14+                     │
│ - Primary data store                         │
│ - JSONB for flexible schema                  │
│ - Full-text search                           │
│ - Materialized views for analytics           │
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│ Cache: Redis 7+                              │
│ - Session store                              │
│ - Response caching                           │
│ - Pub/Sub for events                         │
│ - Rate limiting                              │
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│ Object Storage: MinIO                        │
│ - S3-compatible API                          │
│ - Customer model files                       │
│ - Inspection photos                          │
│ - Pre-signed URLs                            │
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│ Background Jobs: Hangfire                    │
│ - Persistent job queue                       │
│ - Retry logic                                │
│ - Dashboard UI                               │
│ - Backed by PostgreSQL                       │
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│ Containerization: Docker                     │
│ - Application container                      │
│ - PostgreSQL container                       │
│ - Redis container                            │
│ - MinIO container                            │
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│ Orchestration: Docker Compose                │
│ - Local development                          │
│ - Service dependencies                       │
│ - Volume management                          │
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│ Reverse Proxy: Nginx                         │
│ - SSL termination                            │
│ - Static file serving                        │
│ - Load balancing (future)                    │
└──────────────────────────────────────────────┘
```

---

## 7. Deployment Architecture

### 7.1 Docker Compose Setup

```yaml
version: '3.8'

services:
  # Backend API
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=printgrid_db;Username=postgres;Password=***
      - Redis__Configuration=redis:6379
      - MinIO__Endpoint=minio:9000
    depends_on:
      - postgres
      - redis
      - minio
    networks:
      - printgrid-network

  # Frontend
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "3000:80"
    depends_on:
      - api
    networks:
      - printgrid-network

  # PostgreSQL
  postgres:
    image: postgres:14-alpine
    environment:
      - POSTGRES_DB=printgrid_db
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=***
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    networks:
      - printgrid-network

  # Redis
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    networks:
      - printgrid-network

  # MinIO
  minio:
    image: minio/minio:latest
    command: server /data --console-address ":9001"
    ports:
      - "9000:9000"
      - "9001:9001"
    environment:
      - MINIO_ROOT_USER=admin
      - MINIO_ROOT_PASSWORD=***
    volumes:
      - minio-data:/data
    networks:
      - printgrid-network

  # Nginx
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./certs:/etc/nginx/certs
    depends_on:
      - api
      - frontend
    networks:
      - printgrid-network

volumes:
  postgres-data:
  redis-data:
  minio-data:

networks:
  printgrid-network:
    driver: bridge
```

---

### 7.2 Container Architecture

```
┌────────────────────────────────────────────────────────┐
│                        Nginx                           │
│              (Reverse Proxy / SSL)                     │
│                    Port 80/443                         │
└───────────────┬────────────────────────┬───────────────┘
                │                        │
                │                        │
┌───────────────▼────────────┐  ┌────────▼──────────────┐
│     Frontend Container     │  │   API Container       │
│       React + Vite         │  │    .NET 8 WebAPI      │
│        Port 3000           │  │      Port 5000        │
└────────────────────────────┘  └───────┬───────────────┘
                                        │
                        ┌───────────────┼──────────────┐
                        │               │              │
            ┌───────────▼────┐ ┌────────▼────┐ ┌──────▼──────┐
            │   PostgreSQL   │ │    Redis    │ │    MinIO    │
            │   Container    │ │  Container  │ │  Container  │
            │   Port 5432    │ │  Port 6379  │ │ Port 9000   │
            └────────────────┘ └─────────────┘ └─────────────┘
```

---

## 8. Security Architecture

### 8.1 Authentication & Authorization

```
┌─────────────────────────────────────────────────────┐
│                  Authentication                      │
│  - JWT Bearer Tokens                                │
│  - Token expiry: 1 hour                             │
│  - Refresh tokens: 7 days                           │
│  - Password hashing: BCrypt (cost factor 12)        │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│                  Authorization                       │
│  - Role-Based Access Control (RBAC)                 │
│  - Roles: Customer, LabManager, LabOperator,        │
│           HubQC, HubFulfillment, OpsManager, Admin  │
│  - Policy-based authorization                       │
│  - [Authorize(Policy = "RequireLabRole")]           │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│                  API Security                        │
│  - HTTPS only (TLS 1.2+)                            │
│  - CORS configured per origin                       │
│  - Rate limiting via Redis                          │
│  - Anti-CSRF tokens                                 │
│  - Input validation (FluentValidation)              │
└─────────────────────────────────────────────────────┘
```

---

### 8.2 Data Security

```
┌─────────────────────────────────────────────────────┐
│              Data at Rest                            │
│  - PostgreSQL: Encrypted volumes                    │
│  - MinIO: Server-side encryption                    │
│  - Passwords: BCrypt hashed                         │
│  - Sensitive config: Environment variables          │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│             Data in Transit                          │
│  - TLS 1.2+ for all HTTP traffic                    │
│  - SignalR over WSS (WebSocket Secure)              │
│  - Database connections over SSL                    │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│             File Access Control                      │
│  - MinIO pre-signed URLs (time-limited)             │
│  - URL expiry: job_duration + 4 hours               │
│  - Access logging to audit table                    │
│  - Revocation on job reassignment                   │
└─────────────────────────────────────────────────────┘
```

---

## 9. Performance Considerations

### 9.1 Caching Strategy

```csharp
// Redis caching layers
public class CachingStrategy
{
    // L1: Response Caching (API level)
    [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "id" })]
    public async Task<IActionResult> GetLab(string id) { }
    
    // L2: Distributed Cache (cross-instance)
    var labs = await _cache.GetOrCreateAsync(
        "labs:active",
        TimeSpan.FromMinutes(5),
        () => _repository.GetActiveLabs());
    
    // L3: Query Result Cache (read-heavy data)
    var materials = _context.Materials
        .Where(m => m.IsActive)
        .AsSplitQuery()
        .ToList(); // Cached by EF Second Level Cache
}
```

**Cache Invalidation:**
- Lab capabilities: TTL 5 minutes
- Material inventory: Invalidate on update
- Lab performance: Recalculate daily
- Quote results: TTL = quote expiry time

---

### 9.2 Database Optimization

```sql
-- Key Indexes
CREATE INDEX idx_jobs_status_machine 
ON scheduling.jobs(status, machine_id) 
WHERE status IN ('ASSIGNED', 'ACCEPTED', 'IN_PROGRESS');

CREATE INDEX idx_jobs_internal_due 
ON scheduling.jobs(internal_due_date) 
WHERE status != 'COMPLETED';

CREATE INDEX idx_machines_lab_status 
ON scheduling.machines(lab_id, status);

-- Materialized View for Analytics
CREATE MATERIALIZED VIEW analytics.lab_performance AS
SELECT 
    lab_id,
    COUNT(*) FILTER (WHERE completed_on_time) * 100.0 / COUNT(*) as odr,
    COUNT(*) FILTER (WHERE passed_first_inspection) * 100.0 / COUNT(*) as fpy
FROM scheduling.jobs
WHERE completed_at > NOW() - INTERVAL '90 days'
GROUP BY lab_id;

-- Refresh daily
REFRESH MATERIALIZED VIEW CONCURRENTLY analytics.lab_performance;
```

---

### 9.3 Async Processing

```csharp
// Background job for expensive operations
[Queue("quotes")]
public class GenerateQuoteJob
{
    public async Task ExecuteAsync(Guid modelId, PrintConfig config)
    {
        // 1. Slice model (30-60s)
        var estimate = await _slicingService.SliceAsync(modelId, config);
        
        // 2. Run trial placement (10-30s)
        var deliveryDate = await _scheduler.SpeculativeScheduleAsync(...);
        
        // 3. Calculate price
        var price = _pricingService.Calculate(estimate, deliveryDate);
        
        // 4. Store quote
        await _quoteRepository.SaveAsync(quote);
        
        // 5. Notify customer via SignalR
        await _hub.Clients.User(userId).SendAsync("QuoteReady", quote);
    }
}

// Enqueue from controller
public async Task<IActionResult> RequestQuote([FromBody] QuoteRequest req)
{
    var jobId = BackgroundJob.Enqueue<GenerateQuoteJob>(
        x => x.ExecuteAsync(req.ModelId, req.Config));
    
    return Accepted(new { JobId = jobId, Status = "Processing" });
}
```

---

## 10. Monitoring and Observability

### 10.1 Logging

```csharp
// Structured logging with Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.PostgreSQL(connectionString, "logs")
    .Enrich.WithProperty("Application", "PrintGrid")
    .Enrich.FromLogContext()
    .CreateLogger();

// Usage
_logger.LogInformation(
    "Job {JobId} assigned to Lab {LabId} with score {Score}",
    jobId, labId, score);
```

---

### 10.2 Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres")
    .AddRedis(redisConfig, name: "redis")
    .AddCheck<MinioHealthCheck>("minio")
    .AddCheck<HangfireHealthCheck>("hangfire");

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new() {
    Predicate = check => check.Tags.Contains("ready")
});
```

---

## 11. Migration Path to Microservices (Future)

If network grows beyond modular monolith capacity, migration path:

```
Phase 1: Extract Scheduling Module
- Deploy Scheduling as separate service
- Inter-service communication via message queue (RabbitMQ)
- Separate database for scheduling

Phase 2: Extract High-Traffic Modules
- Customer module (public-facing traffic)
- Lab module (many labs = high writes)

Phase 3: Event-Driven Architecture
- Event bus (RabbitMQ/Kafka)
- Saga pattern for distributed transactions
- API Gateway (Ocelot/YARP)
```

**Current architecture supports this** via:
- Clear module boundaries
- Domain events for communication
- Independent schemas per module

---

## Summary

**Architecture Style:** Modular Monolith + Clean Architecture  
**Deployment:** Docker Compose (containers)  
**Backend:** .NET 8, EF Core, MediatR, Hangfire  
**Frontend:** React 18, TypeScript, Material-UI  
**Database:** PostgreSQL (single DB, schema per module)  
**Cache:** Redis  
**Storage:** MinIO  
**Communication:** REST API + SignalR (real-time)

**Key Decisions:**
✅ Modular Monolith: Right size for 5-person team, 50 labs  
✅ Clean Architecture: Testable, maintainable, clear boundaries  
✅ Single Database: ACID transactions, simpler ops  
✅ Domain Events: Loose module coupling  
✅ Docker Compose: Simple deployment for MVP  

**Scalability Path:** Can migrate to microservices module-by-module if needed.
