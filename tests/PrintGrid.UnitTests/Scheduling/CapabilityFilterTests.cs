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

    private static Lab LabWith(Machine machine)
    {
        var lab = Lab.Onboard("Test Lab", "Ha Noi", transitDaysToHub: 1);
        lab.AddMachine(machine);
        return lab;
    }
}
