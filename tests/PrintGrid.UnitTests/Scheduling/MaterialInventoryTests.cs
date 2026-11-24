using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.UnitTests.Scheduling;

public class MaterialInventoryTests
{
    [Theory]
    [InlineData("SIGNALRED", "RED")]
    [InlineData("GREY", "GRAY")]
    public void Alias_stock_updates_and_reservations_use_the_same_ledger_row(string alias, string canonical)
    {
        var lab = Lab.Onboard("Lab", "HCM", 1);
        lab.SetMaterialStock("PLA", alias, 100);
        lab.MaterialStocks.Single().ColorCode.Should().Be(canonical);
        lab.AdjustMaterialStock("PLA", canonical, 20, "Restock");
        var jobId = Guid.NewGuid();
        lab.ReserveMaterial(jobId, "PLA", alias, 50);
        lab.ReserveMaterial(jobId, "PLA", canonical, 50);

        lab.MaterialStocks.Should().ContainSingle();
        lab.MaterialReservations.Should().ContainSingle();
        lab.MaterialStocks.Single().ReservedGrams.Should().Be(51.5m);
        lab.HasStock("PLA", alias, 70).Should().BeFalse();
        lab.SettleMaterial(jobId, 48);
        lab.MaterialStocks.Single().AvailableGrams.Should().Be(72);
        lab.MaterialStocks.Single().ReservedGrams.Should().Be(0);
    }

    [Fact]
    public void Initial_stock_count_is_stored_as_opening_balance_without_adjustment()
    {
        var lab = Lab.Onboard("Lab", "HCM", 1);

        lab.SetMaterialStock("PLA", "Black", 50m, 20m);

        var stock = lab.MaterialStocks.Single();
        stock.AvailableGrams.Should().Be(50m);
        stock.ReorderPointGrams.Should().Be(20m);
        lab.StockTransactions.Should().BeEmpty();
    }

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

    /// <summary>
    /// Opt-in ledger symmetry: ReserveMaterial silently skips a lab with no stock row for the
    /// material, so completing that job with a real gram figure must also skip — it used to
    /// throw "Job has no material reservation" and block lab completion end-to-end.
    /// </summary>
    [Fact]
    public void Completion_on_lab_without_stock_entry_is_a_no_op_not_an_error()
    {
        var lab = Lab.Onboard("Lab", "HCM", 1);
        var jobId = Guid.NewGuid();

        lab.ReserveMaterial(jobId, "PLA", "WHITE", 40m);
        lab.MaterialReservations.Should().BeEmpty();

        Action settle = () => lab.SettleMaterial(jobId, 38m);

        settle.Should().NotThrow();
        lab.MaterialReservations.Should().BeEmpty();
        lab.StockTransactions.Should().BeEmpty();
    }

    [Fact]
    public void Material_stock_without_reservations_can_be_removed()
    {
        var lab = Lab.Onboard("Lab", "HCM", 1);
        lab.SetMaterialStock("PLA", "BLACK", 100m, 25m);
        var stockId = lab.MaterialStocks.Single().Id;

        var removed = lab.RemoveMaterialStock(stockId);

        removed.Should().BeTrue();
        lab.MaterialStocks.Should().BeEmpty();
    }

    [Fact]
    public void Material_stock_reserved_for_a_job_cannot_be_removed()
    {
        var lab = Lab.Onboard("Lab", "HCM", 1);
        lab.SetMaterialStock("PLA", "BLACK", 100m, 25m);
        var stockId = lab.MaterialStocks.Single().Id;
        lab.ReserveMaterial(Guid.NewGuid(), "PLA", "BLACK", 40m);

        Action remove = () => lab.RemoveMaterialStock(stockId);

        remove.Should().Throw<InvalidOperationException>()
            .WithMessage("*đang được giữ cho đơn in*");
        lab.MaterialStocks.Should().ContainSingle();
    }
}
