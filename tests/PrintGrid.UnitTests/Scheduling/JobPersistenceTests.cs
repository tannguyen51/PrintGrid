using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Infrastructure;

namespace PrintGrid.UnitTests.Scheduling;

public class JobPersistenceTests
{
    [Fact]
    public async Task Job_specification_and_its_owned_volume_can_be_materialized()
    {
        ModuleAssemblyRegistry.Register(typeof(SchedulingModule).Assembly);
        var options = new DbContextOptionsBuilder<PrintGridDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), SpecBuilder.Default(), 60,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));

        await using (var writer = new PrintGridDbContext(options))
        {
            writer.Set<Job>().Add(job);
            await writer.SaveChangesAsync();
        }

        await using var reader = new PrintGridDbContext(options);
        var loaded = await reader.Set<Job>().SingleAsync(j => j.Id == job.Id);
        loaded.Specification.MaterialCode.Should().Be(job.Specification.MaterialCode);
        loaded.Specification.RequiredVolume.Should().Be(job.Specification.RequiredVolume);
    }
}
