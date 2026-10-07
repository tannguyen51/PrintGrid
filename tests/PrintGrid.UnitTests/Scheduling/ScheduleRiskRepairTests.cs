using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.UnitTests.Scheduling;

public class ScheduleRiskRepairTests
{
    [Fact]
    public void Split_bulk_job_creates_children_with_same_order_item_and_exact_totals()
    {
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), SpecBuilder.Default(materialGrams: 1_000m), 2_000, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), 100);

        var result = job.Split([60, 40]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2).And.OnlyContain(x => x.ParentJobId == job.Id && x.OrderItemId == job.OrderItemId);
        result.Value.Sum(x => x.Quantity).Should().Be(100);
        result.Value.Sum(x => x.EstimatedPrintMinutes).Should().Be(2_000);
        result.Value.Sum(x => x.Specification.MaterialGrams).Should().Be(1_000m);
        job.Status.Should().Be(JobStatus.Cancelled);
    }

    [Theory]
    [InlineData(50, 49)]
    [InlineData(50, 0)]
    public void Split_rejects_loss_or_zero_quantity(int first, int second)
    {
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), SpecBuilder.Default(), 1_000, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), 100);
        job.Split([first, second]).IsFailure.Should().BeTrue();
    }
}
