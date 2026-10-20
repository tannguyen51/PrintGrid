using PrintGrid.Modules.Scheduling.Application.Services;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.UnitTests.Scheduling;

public sealed class ShipmentBatchTests
{
    [Fact]
    public void Allowance_preview_uses_zone_rate_and_weight_class()
    {
        var policy = new TransportAllowancePolicy(
            new Dictionary<string, decimal> { ["NEAR"] = 80_000m },
            mediumMultiplier: 1.6m,
            largeMultiplier: 2.4m);

        var quote = policy.Quote("near", 1_250m);

        quote.WeightClass.Should().Be("UP_TO_5_KG");
        quote.ZoneRate.Should().Be(80_000m);
        quote.Allowance.Should().Be(128_000m);
    }

    [Fact]
    public void Lab_next_transfer_date_respects_declared_calendar()
    {
        var lab = Lab.Onboard("Lab A", "HCM", 1, transportZone: "LOCAL", transferDaysOfWeek: "Tuesday,Friday");

        lab.NextTransferDate(new DateOnly(2026, 10, 12)).Should().Be(new DateOnly(2026, 10, 13));
    }

    [Fact]
    public void Seal_only_accepts_proof_approved_completed_job()
    {
        var job = CreateAwaitingInspectionJob();
        job.SealForHubShipment(Guid.NewGuid()).IsFailure.Should().BeTrue();

        job.ReviewQcProof(true, Guid.NewGuid(), null, DateTime.UtcNow).IsSuccess.Should().BeTrue();
        var batchId = Guid.NewGuid();
        var result = job.SealForHubShipment(batchId);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.InTransit);
        job.ShipmentBatchId.Should().Be(batchId);
        job.SealForHubShipment(Guid.NewGuid()).IsFailure.Should().BeTrue();
    }

    private static Job CreateAwaitingInspectionJob()
    {
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), SpecBuilder.Default(), 60, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));
        job.AssignTo(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 1m);
        job.Accept();
        job.Start(DateTime.UtcNow);
        job.Complete(DateTime.UtcNow, 60, "QC passed", ["proof.jpg"], 250m);
        return job;
    }
}
