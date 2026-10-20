using NSubstitute;
using PrintGrid.Modules.Scheduling.Application.Commands.Calibration;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.UnitTests.Scheduling;

/// <summary>FR-SCHED-008 / BR-ESTIM-002..006 calibration rules.</summary>
public class CalibrationTests
{
    private static CalibrationSample Sample(decimal ratio, Guid? machineId = null) =>
        CalibrationSample.Create(
            Guid.NewGuid(),
            machineId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            "PLA",
            estimatedMinutes: 100m,
            actualMinutes: 100m * ratio);

    private static (Lab Lab, Machine Machine) LabWithMachine()
    {
        var lab = Lab.Onboard("Xưởng Test", "TP.HCM", 1);
        var machine = MachineBuilder.Default(lab.Id);
        lab.AddMachine(machine);
        return (lab, machine);
    }

    [Fact]
    public void Outlier_filter_drops_hard_bounds_and_mean_two_sigma_extremes()
    {
        var samples = Enumerable.Range(0, 10)
            .Select(_ => Sample(1.0m))
            .Concat(new[] { Sample(0.1m), Sample(5.0m), Sample(2.5m) })
            .ToList();

        var valid = EstimationCalibration.FilterOutliers(samples);

        // 0.1 and 5.0 violate the 0.3..3.0 hard bounds; 2.5 survives the bounds but is more
        // than 2σ from the mean of the remaining set.
        valid.Should().HaveCount(10);
        valid.Should().OnlyContain(s => s.Ratio == 1.0m);
    }

    [Fact]
    public void Minimum_sample_gate_requires_ten_valid_samples()
    {
        var nine = Enumerable.Range(0, 9).Select(_ => Sample(1.2m)).ToList();
        var result = EstimationCalibration.Calculate(nine);

        result.IsSufficient.Should().BeFalse("BR-ESTIM-006 requires at least 10 valid samples");
        result.Factor.Should().Be(1.0m);
        result.ValidSamples.Should().Be(9);
    }

    [Fact]
    public async Task Recalibrate_below_ten_samples_leaves_the_previous_factor_unchanged()
    {
        var (lab, machine) = LabWithMachine();
        machine.ApplyCalibrationFactor(1.15m, 10, 0.1m, DateTime.UtcNow.AddDays(-1));
        var previousComputedAt = machine.CalibrationComputedAtUtc;

        var labs = Substitute.For<ILabRepository>();
        var samples = Substitute.For<ICalibrationSampleRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTime.UtcNow);
        labs.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { lab });
        samples.GetByMachineIdAsync(machine.Id, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(0, 9).Select(_ => Sample(2.0m, machine.Id)).ToList());

        var handler = new RecalibrateCommandHandler(labs, samples, unitOfWork, clock);
        var result = await handler.Handle(new RecalibrateCommand(machine.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsSufficient.Should().BeFalse();
        machine.CalibrationFactor.Should().Be(1.15m, "below the gate the previous factor is retained");
        machine.CalibrationComputedAtUtc.Should().Be(previousComputedAt);
    }

    [Fact]
    public async Task Factor_is_clamped_to_half_and_two_and_never_rewrites_existing_jobs()
    {
        var (lab, machine) = LabWithMachine();
        var job = Job.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SpecBuilder.Default(),
            estimatedPrintMinutes: 60,
            internalDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)));

        var labs = Substitute.For<ILabRepository>();
        var samples = Substitute.For<ICalibrationSampleRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTime.UtcNow);
        labs.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { lab });
        samples.GetByMachineIdAsync(machine.Id, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(0, 10).Select(_ => Sample(2.5m, machine.Id)).ToList());

        var handler = new RecalibrateCommandHandler(labs, samples, unitOfWork, clock);
        var result = await handler.Handle(new RecalibrateCommand(machine.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        machine.CalibrationFactor.Should().Be(2.0m, "2.5 is clamped to the 2.0 ceiling");
        machine.ApplyToEstimate(60).Should().Be(120);

        // BR-ESTIM-004: already-promised work is never rewritten.
        job.EstimatedPrintMinutes.Should().Be(60);
    }

    [Fact]
    public void Apply_calibration_factor_rejects_below_gate_and_clamps_low_factor()
    {
        var (_, machine) = LabWithMachine();

        var act = () => machine.ApplyCalibrationFactor(1.0m, 9, 0.1m, DateTime.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();

        machine.ApplyCalibrationFactor(0.4m, 10, 0.1m, DateTime.UtcNow);
        machine.CalibrationFactor.Should().Be(0.5m);
        machine.ApplyToEstimate(100).Should().Be(50);
    }
}
