using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;

namespace PrintGrid.UnitTests.Scheduling;

public class CapabilityFilterTests
{
    private readonly CapabilityFilter _filter = new();

    [Fact]
    public void Filter_keeps_machine_that_satisfies_every_constraint()
    {
        var lab = LabWith(MachineBuilder.Default());

        var result = _filter.Filter(SpecBuilder.Default(), new[] { lab });

        result.Candidates.Should().HaveCount(1);
        result.Rejections.Should().BeEmpty();
    }

    [Fact]
    public void Filter_rejects_machine_whose_build_volume_is_too_small()
    {
        var lab = LabWith(MachineBuilder.Default(buildVolume: BuildVolume.Create(100, 100, 100)));
        var spec = SpecBuilder.Default(volume: BuildVolume.Create(200, 200, 200));

        var result = _filter.Filter(spec, new[] { lab });

        result.Candidates.Should().BeEmpty();
        result.Rejections.Single().Reason.Should().Be("build_volume_too_small");
    }

    [Fact]
    public void Filter_rejects_machine_that_cannot_hold_the_requested_tolerance()
    {
        var lab = LabWith(MachineBuilder.Default(achievableTolerance: 0.5m));
        var spec = SpecBuilder.Default(tolerance: 0.1m);

        var result = _filter.Filter(spec, new[] { lab });

        result.Rejections.Single().Reason.Should().Be("tolerance_unachievable");
    }

    [Fact]
    public void Filter_rejects_machine_without_the_requested_material()
    {
        var lab = LabWith(MachineBuilder.Default(materials: new[] { "PLA" }));
        var spec = SpecBuilder.Default(material: "ABS");

        var result = _filter.Filter(spec, new[] { lab });

        result.Rejections.Single().Reason.Should().Be("material_unsupported");
    }

    [Fact]
    public void Filter_skips_inactive_labs_entirely()
    {
        var lab = LabWith(MachineBuilder.Default());
        lab.Deactivate();

        var result = _filter.Filter(SpecBuilder.Default(), new[] { lab });

        result.Candidates.Should().BeEmpty();
        result.Rejections.Should().BeEmpty();
    }

    [Fact]
    public void Filter_rejects_exact_material_when_requested_color_has_zero_stock()
    {
        var lab = LabWith(MachineBuilder.Default(materials: new[] { "PLA" }));
        lab.SetMaterialStock("PLA", "BLACK", 0m);
        lab.SetMaterialStock("PLA", "WHITE", 1_000m);

        var result = _filter.Filter(SpecBuilder.Default(material: "PLA", color: "BLACK"), new[] { lab });

        result.Candidates.Should().BeEmpty();
        result.Rejections.Single().Reason.Should().Be("material_or_color_out_of_stock");
    }

    [Fact]
    public void Filter_rejects_stock_that_cannot_cover_estimated_consumption()
    {
        var lab = LabWith(MachineBuilder.Default());
        lab.SetMaterialStock("PLA", "BLACK", 44.99m);

        var result = _filter.Filter(SpecBuilder.Default(materialGrams: 45m), new[] { lab });

        result.Candidates.Should().BeEmpty();
        result.Rejections.Single().Reason.Should().Be("material_or_color_out_of_stock");
    }

    [Fact]
    public void Filter_rejects_suspended_lab_before_scoring()
    {
        var lab = LabWith(MachineBuilder.Default());
        lab.SuspendFromAssignments();

        var result = _filter.Filter(SpecBuilder.Default(), new[] { lab });

        result.Candidates.Should().BeEmpty();
        result.Rejections.Single().Reason.Should().Be("lab_not_in_good_standing");
    }

    public static IEnumerable<object[]> AdversarialCases()
    {
        // 30 independently filtered cases: every fifth case is feasible; each other case
        // violates exactly one hard constraint. Scoring is deliberately never invoked.
        for (var i = 0; i < 30; i++)
            yield return new object[] { i, i % 5 == 0 };
    }

    [Theory]
    [MemberData(nameof(AdversarialCases))]
    public void Adversarial_suite_never_leaks_an_infeasible_candidate(int caseNumber, bool expectedFeasible)
    {
        var machine = (caseNumber % 5) switch
        {
            1 => MachineBuilder.Default(technology: PrintTechnology.Sla),
            2 => MachineBuilder.Default(buildVolume: BuildVolume.Create(20, 20, 20)),
            3 => MachineBuilder.Default(achievableTolerance: 0.8m),
            4 => MachineBuilder.Default(materials: new[] { "ABS" }),
            _ => MachineBuilder.Default()
        };
        var lab = LabWith(machine);
        if (caseNumber >= 25) lab.SuspendFromAssignments();

        var result = _filter.Filter(SpecBuilder.Default(), new[] { lab });
        var trulyFeasible = expectedFeasible && caseNumber < 25;

        result.Candidates.Should().HaveCount(trulyFeasible ? 1 : 0);
    }

    private static Lab LabWith(Machine machine)
    {
        var lab = Lab.Onboard("Test Lab", "Ha Noi", transitDaysToHub: 1);
        lab.AddMachine(machine);
        lab.SetMaterialStock("PLA", "BLACK", 10_000m);
        lab.SetMaterialStock("PETG", "BLACK", 10_000m);
        return lab;
    }
}
