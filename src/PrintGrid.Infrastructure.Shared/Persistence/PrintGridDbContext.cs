using MediatR;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Outbox;
using PrintGrid.Infrastructure.Shared.Persistence.Notifications;
using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Infrastructure.Shared.Persistence;

public class PrintGridDbContext : DbContext
{
    private readonly IPublisher? _publisher;

    public PrintGridDbContext(DbContextOptions<PrintGridDbContext> options, IPublisher? publisher = null)
        : base(options)
    {
        _publisher = publisher;
    }

    /// <summary>Durable email outbox (shared infra holds no module of its own — BR-IP-002).</summary>
    public DbSet<EmailOutboxItem> EmailOutbox => Set<EmailOutboxItem>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var assembly in ModuleAssemblyRegistry.PersistenceAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        modelBuilder.Entity<EmailOutboxItem>(b =>
        {
            b.ToTable("email_outbox", "shared");
            b.HasKey(e => e.Id);
            b.Property(e => e.ToEmail).HasMaxLength(254).IsRequired();
            b.Property(e => e.Subject).HasMaxLength(500).IsRequired();
            b.Property(e => e.Body).IsRequired();
            b.Property(e => e.Status).HasMaxLength(16).IsRequired();
            b.HasIndex(e => new { e.Status, e.CreatedAtUtc });
        });

        modelBuilder.Entity<UserNotification>(b =>
        {
            b.ToTable("user_notifications", "shared");
            b.HasKey(n => n.Id);
            b.Property(n => n.Title).HasMaxLength(200).IsRequired();
            b.Property(n => n.Message).HasMaxLength(1000).IsRequired();
            b.Property(n => n.Type).HasMaxLength(64).IsRequired();
            b.Property(n => n.Link).HasMaxLength(512);
            b.HasIndex(n => new { n.UserId, n.ReadAtUtc, n.CreatedAtUtc });
        });

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = CollectDomainEvents();
        var affected = await base.SaveChangesAsync(cancellationToken);

        if (_publisher is not null)
        {
            foreach (var domainEvent in events)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }

        return affected;
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        var aggregates = ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());
        return events;
    }
}
