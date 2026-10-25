using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PrintGrid.Modules.Scheduling.Application.EventHandlers;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Events;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Events;

namespace PrintGrid.UnitTests.Scheduling;

/// <summary>
/// An order item is only Completed when ALL of its jobs are Completed — a split item
/// (BR-SCHED-010) waits for every child batch to pass hub inspection.
/// </summary>
public sealed class JobStatusIntegrationPublisherTests
{
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly IJobRepository _jobs = Substitute.For<IJobRepository>();

    private JobStatusIntegrationPublisher Publisher() =>
        new(_publisher, _jobs, NullLogger<JobStatusIntegrationPublisher>.Instance);

    private static Job NewJob(Guid orderItemId) =>
        Job.Create(orderItemId, Guid.NewGuid(), SpecBuilder.Default(), 60,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));

    private static Job CompletedJob(Guid orderItemId)
    {
        var now = DateTime.UtcNow;
        var job = NewJob(orderItemId);
        job.AssignTo(Guid.NewGuid(), Guid.NewGuid(), now, now.AddHours(1), 1m);
        job.Accept(now);
        job.Start(now);
        job.Complete(now, 60, "QC passed", ["proof.jpg"], 250m);
        job.ReviewQcProof(true, Guid.NewGuid(), null, now);
        job.MarkInspectionPassed(["hub-inspection.jpg"]);
        return job;
    }

    private void PublishFrom(Job passed, params Job[] siblingJobs)
    {
        _jobs.GetByIdAsync(passed.Id, Arg.Any<CancellationToken>()).Returns(passed);
        _jobs.GetByOrderItemIdAsync(passed.OrderItemId, Arg.Any<CancellationToken>())
            .Returns(new[] { passed }.Concat(siblingJobs).ToList());
    }

    [Fact]
    public async Task Item_is_NOT_completed_while_a_sibling_job_is_still_pending()
    {
        var orderItemId = Guid.NewGuid();
        var passed = CompletedJob(orderItemId);
        var pendingSibling = NewJob(orderItemId);
        PublishFrom(passed, pendingSibling);

        await Publisher().Handle(new JobInspectionPassedEvent(passed.Id, []), default);

        await _publisher.DidNotReceive()
            .Publish(Arg.Any<ProductionStatusChangedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Item_is_NOT_completed_while_a_sibling_job_is_in_progress()
    {
        var orderItemId = Guid.NewGuid();
        var passed = CompletedJob(orderItemId);
        var printing = NewJob(orderItemId);
        var now = DateTime.UtcNow;
        printing.AssignTo(Guid.NewGuid(), Guid.NewGuid(), now, now.AddHours(1), 1m);
        printing.Accept(now);
        printing.Start(now);
        PublishFrom(passed, printing);

        await Publisher().Handle(new JobInspectionPassedEvent(passed.Id, []), default);

        await _publisher.DidNotReceive()
            .Publish(Arg.Any<ProductionStatusChangedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Item_is_completed_when_every_sibling_job_is_completed()
    {
        var orderItemId = Guid.NewGuid();
        var first = CompletedJob(orderItemId);
        var second = CompletedJob(orderItemId);
        PublishFrom(first, second);

        await Publisher().Handle(new JobInspectionPassedEvent(first.Id, []), default);

        await _publisher.Received(1).Publish(
            Arg.Is<ProductionStatusChangedIntegrationEvent>(integrationEvent =>
                integrationEvent.OrderItemId == orderItemId && integrationEvent.NewStatus == "Completed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_cancelled_original_does_not_block_the_split_children()
    {
        var orderItemId = Guid.NewGuid();
        var passedChild = CompletedJob(orderItemId);
        var cancelledOriginal = Job.Create(orderItemId, Guid.NewGuid(), SpecBuilder.Default(), 120,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), quantity: 2);
        cancelledOriginal.Split([1, 1]).IsSuccess.Should().BeTrue();
        PublishFrom(passedChild, cancelledOriginal);

        await Publisher().Handle(new JobInspectionPassedEvent(passedChild.Id, []), default);

        await _publisher.Received(1).Publish(
            Arg.Is<ProductionStatusChangedIntegrationEvent>(integrationEvent =>
                integrationEvent.OrderItemId == orderItemId && integrationEvent.NewStatus == "Completed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_unknown_job_is_ignored()
    {
        _jobs.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Job?)null);

        await Publisher().Handle(new JobInspectionPassedEvent(Guid.NewGuid(), []), default);

        await _publisher.DidNotReceive()
            .Publish(Arg.Any<ProductionStatusChangedIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _jobs.DidNotReceiveWithAnyArgs().GetByOrderItemIdAsync(default, default);
    }
}
