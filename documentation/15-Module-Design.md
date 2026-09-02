# Module Design - PrintGrid Platform

## Overview
This document provides detailed design for each module following Clean Architecture + Domain-Driven Design (DDD) principles.

---

## Module Design Principles

### 1. Clean Architecture Layers
```
Presentation → Application → Domain ← Infrastructure
```

### 2. Dependency Rule
- **Domain** has NO dependencies (pure business logic)
- **Application** depends only on Domain
- **Infrastructure** implements Domain interfaces
- **Presentation** depends on Application

### 3. DDD Tactical Patterns
- **Entities**: Objects with identity
- **Value Objects**: Immutable objects without identity
- **Aggregates**: Cluster of entities with root
- **Domain Services**: Business logic spanning multiple entities
- **Repositories**: Persistence abstraction
- **Domain Events**: Something that happened

---

## Customer Module

### Domain Layer

```csharp
// PrintGrid.Modules.Customer.Domain/Entities/Customer.cs
public class Customer : Entity<Guid>
{
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FullName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    
    private readonly List<Order> _orders = new();
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();
    
    private Customer() { } // EF Core
    
    public static Customer Create(string email, string passwordHash, string fullName)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FullName = fullName,
            CreatedAt = DateTime.UtcNow
        };
        
        customer.AddDomainEvent(new CustomerRegisteredEvent(customer.Id, email));
        return customer;
    }
    
    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
}

// PrintGrid.Modules.Customer.Domain/Entities/Order.cs
public class Order : AggregateRoot<Guid>
{
    public Guid CustomerId { get; private set; }
    public Guid QuoteId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalPrice { get; private set; }
    public Date DeliveryDate { get; private set; }
    public Address DeliveryAddress { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    
    private Order() { }
    
    public static Order CreateFromQuote(
        Guid customerId, 
        Quote quote, 
        Address deliveryAddress)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            QuoteId = quote.Id,
            Status = OrderStatus.PaymentPending,
            TotalPrice = quote.TotalPrice,
            DeliveryDate = quote.DeliveryDate,
            DeliveryAddress = deliveryAddress,
            CreatedAt = DateTime.UtcNow
        };
        
        foreach (var quoteItem in quote.Items)
        {
            order._items.Add(OrderItem.FromQuoteItem(order.Id, quoteItem));
        }
        
        return order;
    }
    
    public Result ConfirmPayment(string paymentTransactionId)
    {
        if (Status != OrderStatus.PaymentPending)
            return Result.Failure("Order payment already processed");
        
        Status = OrderStatus.Confirmed;
        
        AddDomainEvent(new OrderConfirmedEvent(
            Id, 
            Items.Select(i => new JobRequest(i.Id, i.Configuration)).ToList(),
            DeliveryDate));
        
        return Result.Success();
    }
    
    public void UpdateStatus(OrderStatus newStatus)
    {
        if (Status == newStatus) return;
        
        var oldStatus = Status;
        Status = newStatus;
        
        AddDomainEvent(new OrderStatusChangedEvent(Id, oldStatus, newStatus));
    }
}

// PrintGrid.Modules.Customer.Domain/Entities/OrderItem.cs
public class OrderItem : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid ModelId { get; private set; }
    public PrintConfiguration Configuration { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money TotalPrice { get; private set; }
    
    private OrderItem() { }
    
    internal static OrderItem FromQuoteItem(Guid orderId, QuoteItem quoteItem)
    {
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ModelId = quoteItem.ModelId,
            Configuration = quoteItem.Configuration,
            Quantity = quoteItem.Quantity,
            UnitPrice = quoteItem.UnitPrice,
            TotalPrice = quoteItem.TotalPrice
        };
    }
}

// PrintGrid.Modules.Customer.Domain/Entities/Quote.cs
public class Quote : Entity<Guid>
{
    public Guid CustomerId { get; private set; }
    public QuoteStatus Status { get; private set; }
    public Money TotalPrice { get; private set; }
    public PriceBreakdown Breakdown { get; private set; }
    public Date DeliveryDate { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public int ParameterVersionId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private readonly List<QuoteItem> _items = new();
    public IReadOnlyCollection<QuoteItem> Items => _items.AsReadOnly();
    
    private Quote() { }
    
    public static Quote Create(
        Guid customerId,
        List<QuoteItem> items,
        Money totalPrice,
        PriceBreakdown breakdown,
        Date deliveryDate,
        int parameterVersionId)
    {
        return new Quote
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = QuoteStatus.Valid,
            _items = items,
            TotalPrice = totalPrice,
            Breakdown = breakdown,
            DeliveryDate = deliveryDate,
            ExpiresAt = DateTime.UtcNow.AddHours(48),
            ParameterVersionId = parameterVersionId,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
    
    public Result MarkAsUsed()
    {
        if (IsExpired())
            return Result.Failure("Quote has expired");
        
        if (Status != QuoteStatus.Valid)
            return Result.Failure("Quote already used");
        
        Status = QuoteStatus.Used;
        return Result.Success();
    }
}

// PrintGrid.Modules.Customer.Domain/ValueObjects/Address.cs
public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }
    public string Country { get; }
    
    private Address() { }
    
    public Address(string street, string city, string state, string zipCode, string country)
    {
        Street = street ?? throw new ArgumentNullException(nameof(street));
        City = city ?? throw new ArgumentNullException(nameof(city));
        State = state;
        ZipCode = zipCode ?? throw new ArgumentNullException(nameof(zipCode));
        Country = country ?? throw new ArgumentNullException(nameof(country));
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return ZipCode;
        yield return Country;
    }
}

// PrintGrid.Modules.Customer.Domain/ValueObjects/PrintConfiguration.cs
public class PrintConfiguration : ValueObject
{
    public string Material { get; }
    public string Color { get; }
    public QualityGrade QualityGrade { get; }
    public int InfillDensity { get; }
    public List<string> PostProcessing { get; }
    
    private PrintConfiguration() { }
    
    public PrintConfiguration(
        string material, 
        string color, 
        QualityGrade qualityGrade, 
        int infillDensity,
        List<string> postProcessing)
    {
        if (infillDensity < 10 || infillDensity > 100)
            throw new ArgumentException("Infill density must be 10-100%");
        
        Material = material;
        Color = color;
        QualityGrade = qualityGrade;
        InfillDensity = infillDensity;
        PostProcessing = postProcessing ?? new List<string>();
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Material;
        yield return Color;
        yield return QualityGrade;
        yield return InfillDensity;
        foreach (var pp in PostProcessing.OrderBy(x => x))
            yield return pp;
    }
}

// PrintGrid.Modules.Customer.Domain/ValueObjects/Money.cs
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    private Money() { }
    
    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative");
        
        Amount = amount;
        Currency = currency;
    }
    
    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");
        
        return new Money(a.Amount + b.Amount, a.Currency);
    }
    
    public static Money operator *(Money money, decimal multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

// PrintGrid.Modules.Customer.Domain/Events/OrderConfirmedEvent.cs
public class OrderConfirmedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public List<JobRequest> JobRequests { get; }
    public Date DeliveryDate { get; }
    
    public OrderConfirmedEvent(Guid orderId, List<JobRequest> jobRequests, Date deliveryDate)
    {
        OrderId = orderId;
        JobRequests = jobRequests;
        DeliveryDate = deliveryDate;
    }
}

public class JobRequest
{
    public Guid OrderItemId { get; set; }
    public PrintConfiguration Configuration { get; set; }
}

// PrintGrid.Modules.Customer.Domain/Repositories/IOrderRepository.cs
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
}
```

---

### Application Layer

```csharp
// PrintGrid.Modules.Customer.Application/Commands/PlaceOrderCommand.cs
public class PlaceOrderCommand : IRequest<Result<Guid>>
{
    public Guid CustomerId { get; set; }
    public Guid QuoteId { get; set; }
    public AddressDto DeliveryAddress { get; set; }
    public PaymentMethodDto PaymentMethod { get; set; }
}

// PrintGrid.Modules.Customer.Application/Commands/PlaceOrderCommandHandler.cs
public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Result<Guid>>
{
    private readonly ICustomerRepository _customerRepo;
    private readonly IQuoteRepository _quoteRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken ct)
    {
        // 1. Get quote
        var quote = await _quoteRepo.GetByIdAsync(request.QuoteId, ct);
        if (quote == null)
            return Result<Guid>.Failure("Quote not found");
        
        // 2. Validate quote
        if (quote.IsExpired())
            return Result<Guid>.Failure("Quote has expired");
        
        var markUsedResult = quote.MarkAsUsed();
        if (!markUsedResult.IsSuccess)
            return Result<Guid>.Failure(markUsedResult.Error);
        
        // 3. Create order
        var address = new Address(
            request.DeliveryAddress.Street,
            request.DeliveryAddress.City,
            request.DeliveryAddress.State,
            request.DeliveryAddress.ZipCode,
            request.DeliveryAddress.Country);
        
        var order = Order.CreateFromQuote(request.CustomerId, quote, address);
        
        // 4. Process payment
        var paymentResult = await _paymentGateway.ChargeAsync(
            request.PaymentMethod,
            quote.TotalPrice.Amount,
            ct);
        
        if (!paymentResult.IsSuccess)
            return Result<Guid>.Failure("Payment failed: " + paymentResult.Error);
        
        // 5. Confirm order
        var confirmResult = order.ConfirmPayment(paymentResult.TransactionId);
        if (!confirmResult.IsSuccess)
            return Result<Guid>.Failure(confirmResult.Error);
        
        // 6. Save
        await _orderRepo.AddAsync(order, ct);
        await _quoteRepo.UpdateAsync(quote, ct);
        await _unitOfWork.CommitAsync(ct);
        
        // Domain events dispatched automatically by UnitOfWork
        
        return Result<Guid>.Success(order.Id);
    }
}

// PrintGrid.Modules.Customer.Application/Queries/GetOrderQuery.cs
public class GetOrderQuery : IRequest<OrderDto?>
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; } // For authorization
}

// PrintGrid.Modules.Customer.Application/Queries/GetOrderQueryHandler.cs
public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDto?>
{
    private readonly IOrderRepository _orderRepo;
    private readonly IMapper _mapper;
    
    public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken ct)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId, ct);
        
        if (order == null || order.CustomerId != request.CustomerId)
            return null;
        
        return _mapper.Map<OrderDto>(order);
    }
}

// PrintGrid.Modules.Customer.Application/DTOs/OrderDto.cs
public class OrderDto
{
    public Guid Id { get; set; }
    public string Status { get; set; }
    public decimal TotalPrice { get; set; }
    public string DeliveryDate { get; set; }
    public AddressDto DeliveryAddress { get; set; }
    public List<OrderItemDto> Items { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

### Infrastructure Layer

```csharp
// PrintGrid.Modules.Customer.Infrastructure/Persistence/CustomerDbContext.cs
public class CustomerDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Quote> Quotes { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("customer");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomerDbContext).Assembly);
    }
}

// PrintGrid.Modules.Customer.Infrastructure/Persistence/Configurations/OrderConfiguration.cs
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", "customer");
        
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.QuoteId).IsRequired();
        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(50);
        
        builder.OwnsOne(o => o.TotalPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("TotalAmount")
                .HasColumnType("decimal(18,2)");
            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3);
        });
        
        builder.OwnsOne(o => o.DeliveryAddress, addr =>
        {
            addr.Property(a => a.Street).HasColumnName("DeliveryStreet");
            addr.Property(a => a.City).HasColumnName("DeliveryCity");
            addr.Property(a => a.State).HasColumnName("DeliveryState");
            addr.Property(a => a.ZipCode).HasColumnName("DeliveryZipCode");
            addr.Property(a => a.Country).HasColumnName("DeliveryCountry");
        });
        
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => new { o.Status, o.CreatedAt });
        
        // Ignore domain events collection
        builder.Ignore(o => o.DomainEvents);
    }
}

// PrintGrid.Modules.Customer.Infrastructure/Repositories/OrderRepository.cs
public class OrderRepository : IOrderRepository
{
    private readonly CustomerDbContext _context;
    
    public OrderRepository(CustomerDbContext context)
    {
        _context = context;
    }
    
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }
    
    public async Task<List<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        return await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Include(o => o.Items)
            .ToListAsync(ct);
    }
    
    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _context.Orders.AddAsync(order, ct);
    }
    
    public Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Update(order);
        return Task.CompletedTask;
    }
}
```

---

## Scheduling Module (CORE)

### Domain Layer

```csharp
// PrintGrid.Modules.Scheduling.Domain/Entities/Job.cs
public class Job : AggregateRoot<Guid>
{
    public Guid OrderItemId { get; private set; }
    public Guid? MachineId { get; private set; }
    public Guid? LabId { get; private set; }
    public JobStatus Status { get; private set; }
    public JobPriority Priority { get; private set; }
    public Guid ModelId { get; private set; }
    public PrintConfiguration Configuration { get; private set; }
    public DateTime? InternalDueDate { get; private set; }
    public int EstimatedDurationSeconds { get; private set; }
    public int? ActualDurationSeconds { get; private set; }
    public decimal EstimatedMaterialGrams { get; private set; }
    public decimal? ActualMaterialGrams { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? OriginalJobId { get; private set; } // If reprint
    public int ReprintCount { get; private set; }
    
    private Job() { }
    
    public static Job CreateFromOrderItem(
        Guid orderItemId,
        Guid modelId,
        PrintConfiguration configuration,
        int estimatedDurationSeconds,
        decimal estimatedMaterialGrams,
        JobPriority priority = JobPriority.Normal)
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            OrderItemId = orderItemId,
            ModelId = modelId,
            Configuration = configuration,
            Status = JobStatus.Pending,
            Priority = priority,
            EstimatedDurationSeconds = estimatedDurationSeconds,
            EstimatedMaterialGrams = estimatedMaterialGrams,
            ReprintCount = 0,
            CreatedAt = DateTime.UtcNow
        };
        
        job.AddDomainEvent(new JobCreatedEvent(job.Id, orderItemId));
        return job;
    }
    
    public Result Assign(Guid labId, Guid machineId, DateTime internalDueDate)
    {
        if (Status != JobStatus.Pending)
            return Result.Failure("Job already assigned");
        
        LabId = labId;
        MachineId = machineId;
        InternalDueDate = internalDueDate;
        Status = JobStatus.Assigned;
        AssignedAt = DateTime.UtcNow;
        
        AddDomainEvent(new JobAssignedEvent(Id, labId, machineId, internalDueDate.Value));
        return Result.Success();
    }
    
    public Result Accept()
    {
        if (Status != JobStatus.Assigned)
            return Result.Failure("Job not in assigned state");
        
        Status = JobStatus.Accepted;
        AddDomainEvent(new JobAcceptedEvent(Id, LabId.Value, MachineId.Value));
        return Result.Success();
    }
    
    public Result Reject(string reason)
    {
        if (Status != JobStatus.Assigned)
            return Result.Failure("Job not in assigned state");
        
        Status = JobStatus.Rejected;
        AddDomainEvent(new JobRejectedEvent(Id, LabId.Value, reason));
        
        // Reset assignment
        LabId = null;
        MachineId = null;
        InternalDueDate = null;
        AssignedAt = null;
        
        return Result.Success();
    }
    
    public Result Complete(int actualDurationSeconds, decimal actualMaterialGrams)
    {
        if (Status != JobStatus.InProgress)
            return Result.Failure("Job not in progress");
        
        Status = JobStatus.Completed;
        ActualDurationSeconds = actualDurationSeconds;
        ActualMaterialGrams = actualMaterialGrams;
        CompletedAt = DateTime.UtcNow;
        
        AddDomainEvent(new JobCompletedEvent(
            Id, 
            LabId.Value, 
            EstimatedDurationSeconds, 
            actualDurationSeconds));
        
        return Result.Success();
    }
    
    public Result ReportFailure(string reason, int percentComplete)
    {
        if (Status != JobStatus.InProgress)
            return Result.Failure("Job not in progress");
        
        Status = JobStatus.Failed;
        AddDomainEvent(new PrintFailureEvent(Id, LabId.Value, MachineId.Value, reason, percentComplete));
        return Result.Success();
    }
    
    public static Job CreateReprint(Job originalJob)
    {
        var reprint = new Job
        {
            Id = Guid.NewGuid(),
            OrderItemId = originalJob.OrderItemId,
            ModelId = originalJob.ModelId,
            Configuration = originalJob.Configuration,
            Status = JobStatus.Pending,
            Priority = JobPriority.Urgent,
            EstimatedDurationSeconds = originalJob.EstimatedDurationSeconds,
            EstimatedMaterialGrams = originalJob.EstimatedMaterialGrams,
            OriginalJobId = originalJob.OriginalJobId ?? originalJob.Id,
            ReprintCount = originalJob.ReprintCount + 1,
            CreatedAt = DateTime.UtcNow
        };
        
        reprint.AddDomainEvent(new ReprintJobCreatedEvent(
            reprint.Id, 
            originalJob.Id, 
            reprint.ReprintCount));
        
        return reprint;
    }
}

// PrintGrid.Modules.Scheduling.Domain/Entities/Machine.cs
public class Machine : Entity<Guid>
{
    public Guid LabId { get; private set; }
    public string Name { get; private set; }
    public string Brand { get; private set; }
    public string Model { get; private set; }
    public Technology Technology { get; private set; }
    public BuildVolume BuildVolume { get; private set; }
    public LayerHeightRange LayerHeightRange { get; private set; }
    public Tolerance AchievableTolerance { get; private set; }
    public MachineStatus Status { get; private set; }
    public DateTime? MaintenanceUntil { get; private set; }
    
    private Machine() { }
    
    public bool CanProduce(JobSpecification spec)
    {
        if (Status != MachineStatus.Operational)
            return false;
        
        if (Technology != spec.RequiredTechnology)
            return false;
        
        if (!BuildVolume.CanFit(spec.BoundingBox))
            return false;
        
        if (AchievableTolerance.Value > spec.RequiredTolerance.Value)
            return false;
        
        return true;
    }
    
    public void SetMaintenance(DateTime until)
    {
        Status = MachineStatus.Maintenance;
        MaintenanceUntil = until;
    }
    
    public void SetOperational()
    {
        Status = MachineStatus.Operational;
        MaintenanceUntil = null;
    }
}

// PrintGrid.Modules.Scheduling.Domain/Entities/Lab.cs
public class Lab : Entity<Guid>
{
    public string Name { get; private set; }
    public string Location { get; private set; }
    public int TransferTimeToHubMinutes { get; private set; }
    public LabStatus Status { get; private set; }
    public double PerformanceScore { get; private set; }
    
    private readonly List<Machine> _machines = new();
    public IReadOnlyCollection<Machine> Machines => _machines.AsReadOnly();
    
    private readonly List<MaterialInventory> _inventory = new();
    public IReadOnlyCollection<MaterialInventory> Inventory => _inventory.AsReadOnly();
    
    private Lab() { }
    
    public bool HasMaterialInStock(string material, string color, decimal quantityGrams)
    {
        var item = _inventory.FirstOrDefault(i => 
            i.Material == material && 
            i.Color == color);
        
        return item != null && item.QuantityGrams >= quantityGrams;
    }
    
    public bool IsInGoodStanding()
    {
        return Status == LabStatus.Active && PerformanceScore >= 0.50;
    }
}

// PrintGrid.Modules.Scheduling.Domain/Services/CapabilityFilter.cs
public class CapabilityFilter
{
    public List<(Lab Lab, Machine Machine)> Filter(
        List<Lab> labs,
        JobSpecification spec)
    {
        var feasible = new List<(Lab, Machine)>();
        
        foreach (var lab in labs)
        {
            // Lab-level checks
            if (!lab.IsInGoodStanding())
                continue;
            
            if (!lab.HasMaterialInStock(
                spec.Material, 
                spec.Color, 
                spec.EstimatedMaterialGrams))
                continue;
            
            // Machine-level checks
            foreach (var machine in lab.Machines)
            {
                if (machine.CanProduce(spec))
                {
                    feasible.Add((lab, machine));
                }
            }
        }
        
        return feasible;
    }
}

// PrintGrid.Modules.Scheduling.Domain/Services/AssignmentScorer.cs
public class AssignmentScorer
{
    private readonly ScoringWeights _weights;
    
    public AssignmentScorer(ScoringWeights weights)
    {
        _weights = weights;
    }
    
    public double CalculateScore(
        Job job,
        Lab lab,
        Machine machine,
        DateTime internalDueDate)
    {
        var deadlineSlack = CalculateDeadlineSlack(internalDueDate, job.EstimatedDurationSeconds);
        var currentLoad = GetMachineLoad(machine);
        var qualityHistory = lab.PerformanceScore;
        var transferTime = lab.TransferTimeToHubMinutes;
        
        // Normalize to 0-1
        var normalizedScores = new Dictionary<string, double>
        {
            ["deadline_slack"] = Normalize(deadlineSlack, 0, 72 * 60), // 0-72h
            ["load"] = 1.0 - currentLoad, // Low load is good
            ["quality"] = qualityHistory, // Already 0-1
            ["transfer"] = 1.0 - Normalize(transferTime, 0, 480), // 0-8h
            ["cost"] = 0.5 // Placeholder, would estimate lab cost
        };
        
        var compositeScore = 
            normalizedScores["deadline_slack"] * _weights.DeadlineSlack +
            normalizedScores["load"] * _weights.CurrentLoad +
            normalizedScores["quality"] * _weights.QualityHistory +
            normalizedScores["transfer"] * _weights.TransferTime +
            normalizedScores["cost"] * _weights.InternalCost;
        
        return compositeScore;
    }
    
    private double CalculateDeadlineSlack(DateTime dueDate, int estimatedSeconds)
    {
        var availableTime = (dueDate - DateTime.UtcNow).TotalMinutes;
        var requiredTime = estimatedSeconds / 60.0;
        return Math.Max(0, availableTime - requiredTime);
    }
    
    private double GetMachineLoad(Machine machine)
    {
        // Would query current queue depth
        return 0.5; // Placeholder
    }
    
    private double Normalize(double value, double min, double max)
    {
        if (max == min) return 0;
        return Math.Clamp((value - min) / (max - min), 0, 1);
    }
}
```

---

## Summary

**Module Design Complete:**
- ✅ Customer Module: Orders, Quotes, Customers
- ✅ Scheduling Module (detailed): Jobs, Labs, Machines, Domain Services
- ✅ Clean Architecture layers per module
- ✅ DDD tactical patterns applied
- ✅ Domain events for loose coupling

**Remaining modules follow same pattern:**
- Lab Module
- Hub Module
- Analytics Module
- Admin Module

Each module is:
- **Self-contained** with clear boundaries
- **Testable** (domain logic has no dependencies)
- **Evolvable** (can change internals without affecting others)
- **Deployable** (could become microservice if needed)

**Key Patterns Used:**
- Aggregate Roots (Order, Job)
- Value Objects (Money, Address, BuildVolume)
- Domain Events (OrderConfirmedEvent, JobAssignedEvent)
- Repository Pattern (interface in Domain, impl in Infrastructure)
- CQRS (Commands vs Queries in Application layer)
- Result Pattern (instead of exceptions for business failures)
