using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.Modules.Scheduling.Application.EventHandlers;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.UnitTests.Scheduling;

public sealed class OrderConfirmedEventHandlerTests
{
    [Fact]
    public async Task Confirmation_creates_pending_jobs_for_ops_without_assigning_labs_or_machines()
    {
        var jobs = Substitute.For<IJobRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var created = new List<Job>();
        jobs.AddAsync(Arg.Do<Job>(created.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var date = new DateOnly(2026, 11, 25);
        var notification = new OrderConfirmedEvent(Guid.NewGuid(),
            [new OrderConfirmedItem(Guid.NewGuid(), Guid.NewGuid(), 3, "PLA", "BLUE", .2m, 60, 10m, 20m, 30m, 40m),
             new OrderConfirmedItem(Guid.NewGuid(), Guid.NewGuid(), 1, "PLA", "BLACK", .1m, 30, 5m, 10m, 20m, 30m)], date);

        var handler = new OrderConfirmedEventHandler(jobs, unitOfWork, NullLogger<OrderConfirmedEventHandler>.Instance);
        await handler.Handle(notification, CancellationToken.None);

        created.Should().HaveCount(2);
        foreach (var job in created)
        {
            job.Status.Should().Be(JobStatus.Pending);
            job.LabId.Should().BeNull();
            job.MachineId.Should().BeNull();
            job.PlannedStartUtc.Should().BeNull();
            job.InternalDueDate.Should().Be(date.AddDays(-4));
        }
        created[0].Quantity.Should().Be(3);
        created[0].EstimatedPrintMinutes.Should().Be(180);
        created[0].Specification.MaterialGrams.Should().Be(30);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pending_job_keeps_the_quoted_machine_profile_without_assigning_that_machine()
    {
        var jobs = Substitute.For<IJobRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        Job? created = null;
        jobs.AddAsync(Arg.Do<Job>(job => created = job), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var machine = Guid.NewGuid();
        const string json = "{\"Colors\":[{\"ColorCode\":\"BLACK\",\"Grams\":30}]}";
        var item = new OrderConfirmedItem(Guid.NewGuid(), Guid.NewGuid(), 3, "PLA", "BLACK", .2m, 60, 10m,
            20m, 30m, 40m, CalculationJson: json, QuotedMachineId: machine);
        var notification = new OrderConfirmedEvent(Guid.NewGuid(), [item], new DateOnly(2026, 11, 25));

        await new OrderConfirmedEventHandler(jobs, unitOfWork, NullLogger<OrderConfirmedEventHandler>.Instance)
            .Handle(notification, CancellationToken.None);

        created!.Status.Should().Be(JobStatus.Pending);
        created.ProductionQuoteJson.Should().Be(json);
        created.QuotedMachineId.Should().Be(machine);
        created.MachineId.Should().BeNull();
        created.MaterialByColor()["BLACK"].Should().Be(30);
    }
}
