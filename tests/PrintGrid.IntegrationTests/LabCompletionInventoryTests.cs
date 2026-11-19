using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PrintGrid.Api.Bootstrap;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.IntegrationTests;

public sealed class LabCompletionInventoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Completion_inserts_new_ledger_rows_and_releases_reservations(bool systemPays)
    {
        new ServiceCollection().AddApplicationModules(new ConfigurationBuilder().Build());
        var options = new DbContextOptionsBuilder<PrintGridDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new PrintGridDbContext(options);
        var lab = Lab.Onboard("Completion inventory test", "HCM", 1);
        lab.SetMaterialStock("PLA", "BLACK", 100);
        var spec = JobSpecification.Create(BuildVolume.Create(20, 20, 20), "PLA", "BLACK", .2m, .2m, PrintTechnology.Fdm, 45);
        var original = Job.Create(Guid.NewGuid(), Guid.NewGuid(), spec, 60, new DateOnly(2026, 11, 25));
        var job = systemPays ? Job.CreateUrgentReprint(original, 1, costBearer: CostBearer.System) : original;
        var now = DateTime.UtcNow;
        job.AssignTo(lab.Id, Guid.NewGuid(), now, now.AddHours(1), 1m);
        job.Accept(now);
        job.Start(now);
        lab.ReserveMaterial(job.Id, "PLA", "BLACK", 45);
        db.Add(lab);
        db.Add(job);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(now.AddHours(1));
        var handler = new CompleteJobCommandHandler(new JobRepository(db), new LabRepository(db), new UnitOfWork(db), clock);

        var result = await handler.Handle(new CompleteJobCommand(job.Id, 60, "QC passed", ["qc/test/proof.png"], 40), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.ChangeTracker.Clear();
        (await db.Set<Job>().SingleAsync()).Status.Should().Be(JobStatus.AwaitingInspection);
        var stock = await db.Set<MaterialStock>().SingleAsync();
        stock.ReservedGrams.Should().Be(0);
        stock.AvailableGrams.Should().Be(systemPays ? 100 : 60);
        (await db.Set<MaterialReservation>().CountAsync()).Should().Be(0);
        var ledger = await db.Set<StockTransaction>().ToListAsync();
        ledger.Should().HaveCount(systemPays ? 2 : 1);
        ledger.Should().OnlyContain(transaction => transaction.JobId == job.Id);
        ledger.Sum(transaction => transaction.DeltaGrams).Should().Be(systemPays ? 0 : -40);
    }
}
