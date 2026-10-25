using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PrintGrid.Modules.Scheduling.Application.Commands.ShipmentBatches;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.UnitTests.Scheduling;

/// <summary>
/// FR-HUB-001 batch receipt: receiving a sealed batch closes it and moves every sealed job
/// back to hub inspection. Unknown batches, already-received batches and scan mismatches
/// are rejected without touching any job.
/// </summary>
public sealed class ReceiveShipmentBatchCommandHandlerTests
{
    private readonly IShipmentBatchRepository _batches = Substitute.For<IShipmentBatchRepository>();
    private readonly IJobRepository _jobs = Substitute.For<IJobRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private readonly DateTime _now = new(2026, 10, 25, 9, 0, 0, DateTimeKind.Utc);

    private ReceiveShipmentBatchCommandHandler Handler() =>
        new(_batches, _jobs, _unitOfWork, _clock, NullLogger<ReceiveShipmentBatchCommandHandler>.Instance);

    private static ShipmentBatch SealedBatch(int jobCount = 1) =>
        ShipmentBatch.Seal(
            labId: Guid.NewGuid(),
            scheduledTransferDate: new DateOnly(2026, 10, 26),
            transportZone: "LOCAL",
            weightClass: "UP_TO_5_KG",
            totalWeightGrams: 500m,
            zoneRate: 80_000m,
            weightMultiplier: 1m,
            transportAllowance: 80_000m,
            jobCount: jobCount,
            sealedBy: Guid.NewGuid(),
            nowUtc: new DateTime(2026, 10, 24, 8, 0, 0, DateTimeKind.Utc));

    private static Job InTransitJob(Guid batchId)
    {
        var now = new DateTime(2026, 10, 24, 8, 0, 0, DateTimeKind.Utc);
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), SpecBuilder.Default(), 60,
            new DateOnly(2026, 10, 27));
        job.AssignTo(Guid.NewGuid(), Guid.NewGuid(), now, now.AddHours(1), 1m);
        job.Accept(now);
        job.Start(now);
        job.Complete(now, 60, "QC passed", ["proof.jpg"], 250m);
        job.ReviewQcProof(true, Guid.NewGuid(), null, now);
        job.SealForHubShipment(batchId).IsSuccess.Should().BeTrue();
        return job;
    }

    private void Setup(ShipmentBatch batch, params Job[] jobs)
    {
        _clock.UtcNow.Returns(_now);
        _batches.GetByIdAsync(batch.Id, Arg.Any<CancellationToken>()).Returns(batch);
        _jobs.GetByShipmentBatchIdAsync(batch.Id, Arg.Any<CancellationToken>()).Returns(jobs.ToList());
    }

    [Fact]
    public async Task Receiving_a_sealed_batch_closes_it_and_moves_every_job_to_hub_inspection()
    {
        var batch = SealedBatch(jobCount: 2);
        var first = InTransitJob(batch.Id);
        var second = InTransitJob(batch.Id);
        Setup(batch, first, second);

        var result = await Handler().Handle(
            new ReceiveShipmentBatchCommand(batch.Id, Guid.NewGuid(), [first.Id, second.Id]), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.BatchId.Should().Be(batch.Id);
        result.Value.Status.Should().Be(nameof(ShipmentBatchStatus.Received));
        result.Value.ReceivedJobCount.Should().Be(2);
        result.Value.ReceivedAtUtc.Should().Be(_now);
        batch.Status.Should().Be(ShipmentBatchStatus.Received);
        batch.ReceivedAtUtc.Should().Be(_now);
        first.Status.Should().Be(JobStatus.AwaitingInspection);
        second.Status.Should().Be(JobStatus.AwaitingInspection);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_unknown_batch_is_reported_as_not_found()
    {
        var batchId = Guid.NewGuid();
        _batches.GetByIdAsync(batchId, Arg.Any<CancellationToken>()).Returns((ShipmentBatch?)null);

        var result = await Handler().Handle(new ReceiveShipmentBatchCommand(batchId, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task An_already_received_batch_is_a_conflict_and_its_jobs_are_untouched()
    {
        var batch = SealedBatch();
        var job = InTransitJob(batch.Id);
        Setup(batch, job);
        batch.MarkReceived(_now.AddHours(-1)).IsSuccess.Should().BeTrue();

        var result = await Handler().Handle(new ReceiveShipmentBatchCommand(batch.Id, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        job.Status.Should().Be(JobStatus.InTransit);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task A_scan_list_missing_a_job_is_a_validation_error()
    {
        var batch = SealedBatch(jobCount: 2);
        var first = InTransitJob(batch.Id);
        var second = InTransitJob(batch.Id);
        Setup(batch, first, second);

        var result = await Handler().Handle(
            new ReceiveShipmentBatchCommand(batch.Id, Guid.NewGuid(), [first.Id]), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation_error");
        result.Error.Message.Should().Contain("1 missing");
        batch.Status.Should().Be(ShipmentBatchStatus.Sealed);
        first.Status.Should().Be(JobStatus.InTransit);
        second.Status.Should().Be(JobStatus.InTransit);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task A_scan_list_with_a_job_outside_the_batch_is_a_validation_error()
    {
        var batch = SealedBatch();
        var job = InTransitJob(batch.Id);
        Setup(batch, job);

        var result = await Handler().Handle(
            new ReceiveShipmentBatchCommand(batch.Id, Guid.NewGuid(), [job.Id, Guid.NewGuid()]), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation_error");
        result.Error.Message.Should().Contain("1 unexpected");
        batch.Status.Should().Be(ShipmentBatchStatus.Sealed);
        job.Status.Should().Be(JobStatus.InTransit);
    }

    [Fact]
    public async Task A_job_outside_in_transit_fails_the_whole_receipt()
    {
        var batch = SealedBatch();
        var notInTransit = Job.Create(Guid.NewGuid(), Guid.NewGuid(), SpecBuilder.Default(), 60,
            new DateOnly(2026, 10, 27));
        Setup(batch, notInTransit);

        var result = await Handler().Handle(new ReceiveShipmentBatchCommand(batch.Id, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        notInTransit.Status.Should().Be(JobStatus.Pending);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
