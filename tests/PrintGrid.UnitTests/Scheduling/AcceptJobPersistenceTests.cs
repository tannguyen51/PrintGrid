using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Infrastructure;
using PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;

namespace PrintGrid.UnitTests.Scheduling;

public class AcceptJobPersistenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Accept_persists_new_material_reservation_and_preserves_existing_reservations(bool alreadyReserved)
    {
        ModuleAssemblyRegistry.Register(typeof(SchedulingModule).Assembly);
        var options = new DbContextOptionsBuilder<PrintGridDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var lab = Lab.Onboard("Lab", "HCM", 1);
        lab.SetMaterialStock("PLA", "BLACK", 1_000m);
        var machine = MachineBuilder.Default(labId: lab.Id);
        lab.AddMachine(machine);
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), SpecBuilder.Default(), 60,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));
        var start = DateTime.UtcNow.AddHours(1);
        job.AssignTo(lab.Id, machine.Id, start, start.AddHours(1), 1m);
        var otherJobId = Guid.NewGuid();
        lab.ReserveMaterial(otherJobId, "PLA", "BLACK", 10m);
        if (alreadyReserved)
            lab.ReserveMaterial(job.Id, "PLA", "BLACK", 45m);

        await using (var seed = new PrintGridDbContext(options))
        {
            seed.Add(lab);
            seed.Add(job);
            await seed.SaveChangesAsync();
        }

        await using (var context = new PrintGridDbContext(options))
        {
            var handler = new AcceptJobCommandHandler(new JobRepository(context),
                new LabRepository(context), new UnitOfWork(context));

            var result = await handler.Handle(new AcceptJobCommand(job.Id), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        await using var reader = new PrintGridDbContext(options);
        var savedJob = await reader.Set<Job>().SingleAsync(j => j.Id == job.Id);
        savedJob.Status.Should().Be(JobStatus.Accepted);
        var savedLab = await new LabRepository(reader).GetByIdAsync(lab.Id);
        savedLab!.MaterialReservations.Should().HaveCount(2);
        savedLab.MaterialReservations.Single(r => r.JobId == job.Id).ReservedGrams.Should().Be(46.35m);
        savedLab.MaterialReservations.Single(r => r.JobId == otherJobId).ReservedGrams.Should().Be(10.30m);
        savedLab.MaterialStocks.Single().ReservedGrams.Should().Be(56.65m);
        savedLab.MaterialStocks.Single().AvailableGrams.Should().Be(1_000m);
    }
}
