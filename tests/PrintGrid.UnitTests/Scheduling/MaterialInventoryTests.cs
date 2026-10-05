using FluentAssertions;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using Xunit;

namespace PrintGrid.UnitTests.Scheduling;

public class MaterialInventoryTests
{
    [Fact]
    public void Restock_from_50_by_20_has_running_total_70()
    {
        var lab = Lab.Onboard("Lab", "HCM", 1);
        lab.SetMaterialStock("PLA", "Black", 50m, 20m);

        lab.AdjustMaterialStock("PLA", "BLACK", 20m, "Restock");

        var stock = lab.MaterialStocks.Single();
        stock.AvailableGrams.Should().Be(70m);
        lab.StockTransactions.Last().DeltaGrams.Should().Be(20m);
        lab.StockTransactions.Last().RunningTotalGrams.Should().Be(70m);
        lab.StockTransactions.Last().TransactionCode.Should().StartWith("STK-");
    }

    [Fact]
    public void Stock_below_reorder_point_is_low_and_cannot_be_assigned_when_reserved()
    {
        var lab = Lab.Onboard("Lab", "HCM", 1);
        lab.SetMaterialStock("PLA", "BLACK", 50m, 60m);

        lab.MaterialStocks.Single().IsLowStock.Should().BeTrue();
        lab.ReserveMaterial(Guid.NewGuid(), "PLA", "BLACK", 40m, .05m);
        lab.HasStock("PLA", "BLACK", 10m).Should().BeFalse();
    }

    [Fact]
    public void Completion_settles_reservation_with_actual_usage_and_writes_ledger()
    {
        var lab = Lab.Onboard("Lab", "HCM", 1);
        lab.SetMaterialStock("PLA", "BLACK", 100m, 25m);
        var jobId = Guid.NewGuid();
        lab.ReserveMaterial(jobId, "PLA", "BLACK", 50m);

        lab.SettleMaterial(jobId, 48m);

        var stock = lab.MaterialStocks.Single();
        stock.AvailableGrams.Should().Be(52m);
        stock.ReservedGrams.Should().Be(0m);
        lab.MaterialReservations.Should().BeEmpty();
        lab.StockTransactions.Last().JobId.Should().Be(jobId);
        lab.StockTransactions.Last().DeltaGrams.Should().Be(-48m);
    }
}
