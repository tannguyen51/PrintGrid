using FluentAssertions;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.UnitTests.Scheduling;

/// <summary>
/// A print the platform caused (hub fault) must not cost the lab material: the consumption is
/// still recorded, and a matching credit restores the balance (BR-RESCHED-003, decided 07/10).
/// </summary>
public class MaterialCompensationTests
{
    private const string Reason = "Bồi hoàn do lỗi hub (hệ thống chịu phí)";

    [Fact]
    public void Compensation_restores_the_material_a_hub_fault_consumed()
    {
        var lab = Lab.Onboard("Lab A", "HCM", transitDaysToHub: 1);
        lab.SetMaterialStock("PLA", "BLACK", availableGrams: 1000m);

        var jobId = Guid.NewGuid();
        lab.ReserveMaterial(jobId, "PLA", "BLACK", estimatedGrams: 100m);
        lab.SettleMaterial(jobId, actualGrams: 100m);

        lab.MaterialStocks.Single().AvailableGrams.Should().Be(900m, "the print really consumed material");

        lab.CompensateMaterial(jobId, "PLA", "BLACK", grams: 100m, reason: Reason);

        lab.MaterialStocks.Single().AvailableGrams.Should().Be(
            1000m, "the lab must not pay for a print the platform caused");
        lab.MaterialStocks.Single().ReservedGrams.Should().Be(0m, "the reservation was settled, not left held");

        // Both movements stay in the ledger: the usage and the compensation (the seeding
        // adjustment is a separate transaction with no job attached).
        var jobMovements = lab.StockTransactions.Where(t => t.JobId == jobId).ToList();
        jobMovements.Should().HaveCount(2);
        jobMovements.Select(t => t.DeltaGrams).Should().Equal(-100m, 100m);
        jobMovements[^1].Reason.Should().Be(Reason);
    }

    [Fact]
    public void Compensation_leaves_a_lab_that_does_not_track_the_material_alone()
    {
        var lab = Lab.Onboard("Lab A", "HCM", transitDaysToHub: 1);

        var compensate = () => lab.CompensateMaterial(Guid.NewGuid(), "PLA", "BLACK", 50m, Reason);

        compensate.Should().NotThrow("the stock ledger is opt-in; untracked material has nothing to credit");
        lab.MaterialStocks.Should().BeEmpty();
        lab.StockTransactions.Should().BeEmpty();
    }

    [Fact]
    public void Compensation_ignores_a_non_positive_amount()
    {
        var lab = Lab.Onboard("Lab A", "HCM", transitDaysToHub: 1);
        lab.SetMaterialStock("PLA", "BLACK", availableGrams: 1000m);

        var compensatedJobId = Guid.NewGuid();
        lab.CompensateMaterial(compensatedJobId, "PLA", "BLACK", grams: 0m, reason: Reason);

        lab.MaterialStocks.Single().AvailableGrams.Should().Be(1000m);
        lab.StockTransactions.Should().NotContain(
            t => t.JobId == compensatedJobId, "a zero-gram compensation is not a ledger movement");
    }
}
