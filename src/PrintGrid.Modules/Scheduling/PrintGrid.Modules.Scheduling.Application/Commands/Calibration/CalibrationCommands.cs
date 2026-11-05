using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.Calibration;

/// <summary>
/// FR-SCHED-008 sample collector: every CLOSED job that reported an actual duration becomes
/// one (estimated, actual) pair. Sampling is idempotent by JobId, so the nightly job can run
/// after a retry without double-counting.
/// </summary>
public record CollectCalibrationSamplesCommand : IRequest<Result<int>>;

/// <summary>Recalibrates one machine; insufficient data leaves the current factor untouched.</summary>
public record RecalibrateCommand(Guid MachineId) : IRequest<Result<CalibrationStatusDto>>;

/// <summary>Recalibrates every registered machine (nightly job entry point).</summary>
public record RecalibrateAllCommand : IRequest<Result<IReadOnlyList<CalibrationStatusDto>>>;

public sealed record CalibrationStatusDto(
    Guid MachineId,
    Guid LabId,
    string LabName,
    string MachineName,
    string MachineModel,
    decimal Factor,
    int SampleCount,
    decimal Mape,
    DateTime? ComputedAtUtc,
    bool IsSufficient);

public class CollectCalibrationSamplesCommandHandler(
    IJobRepository jobs,
    ICalibrationSampleRepository samples,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<CollectCalibrationSamplesCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CollectCalibrationSamplesCommand command, CancellationToken cancellationToken)
    {
        var completedJobs = await jobs.GetCompletedWithActualTimeAsync(cancellationToken);
        var candidateJobs = completedJobs
            .Where(j => j.MachineId.HasValue
                        && j is { ActualPrintMinutes: > 0, EstimatedPrintMinutes: > 0 })
            .ToList();
        if (candidateJobs.Count == 0) return Result.Success(0);

        var alreadySampled = await samples.GetSampledJobIdsAsync(
            [.. candidateJobs.Select(j => j.Id)], cancellationToken);

        var now = clock.UtcNow;
        var newSamples = candidateJobs
            .Where(j => !alreadySampled.Contains(j.Id))
            .Select(j => CalibrationSample.Create(
                j.Id,
                j.MachineId!.Value,
                j.ModelId,
                j.Specification.MaterialCode,
                j.EstimatedPrintMinutes,
                j.ActualPrintMinutes!.Value,
                now))
            .ToList();

        if (newSamples.Count <= 0) return Result.Success(newSamples.Count);
        await samples.AddRangeAsync(newSamples, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(newSamples.Count);
    }
}

public class RecalibrateCommandHandler(
    ILabRepository labs,
    ICalibrationSampleRepository samples,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<RecalibrateCommand, Result<CalibrationStatusDto>>
{
    public async Task<Result<CalibrationStatusDto>> Handle(RecalibrateCommand command, CancellationToken cancellationToken)
    {
        var (lab, machine) = await FindMachineAsync(command.MachineId, cancellationToken);
        if (lab is null || machine is null)
            return Result.Failure<CalibrationStatusDto>(Error.NotFound("Machine", command.MachineId));

        var samples1 = await samples.GetByMachineIdAsync(machine.Id, cancellationToken);
        var status = Apply(lab, machine, samples1, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(status);
    }

    private async Task<(Lab? Lab, Machine? Machine)> FindMachineAsync(Guid machineId, CancellationToken cancellationToken)
    {
        var labs1 = await labs.GetAllAsync(cancellationToken);
        foreach (var lab in labs1)
        {
            var machine = lab.Machines.SingleOrDefault(m => m.Id == machineId);
            if (machine is not null) return (lab, machine);
        }

        return (null, null);
    }

    internal static CalibrationStatusDto Apply(
        Lab lab,
        Machine machine,
        IReadOnlyCollection<CalibrationSample> samples,
        DateTime nowUtc)
    {
        var result = EstimationCalibration.Calculate(samples);

        // BR-ESTIM-006: below ten valid samples the previous factor is retained unchanged.
        if (result.IsSufficient)
            machine.ApplyCalibrationFactor(result.Factor, result.ValidSamples, result.Mape, nowUtc);

        return new CalibrationStatusDto(
            machine.Id,
            lab.Id,
            lab.Name,
            machine.Name,
            machine.Model,
            machine.CalibrationFactor,
            result.ValidSamples,
            machine.CalibrationMape,
            machine.CalibrationComputedAtUtc,
            result.IsSufficient);
    }
}

public class RecalibrateAllCommandHandler(
    ILabRepository labs,
    ICalibrationSampleRepository samples,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<RecalibrateAllCommand, Result<IReadOnlyList<CalibrationStatusDto>>>
{
    public async Task<Result<IReadOnlyList<CalibrationStatusDto>>> Handle(
        RecalibrateAllCommand command,
        CancellationToken cancellationToken)
    {
        var labs1 = await labs.GetAllAsync(cancellationToken);
        var samples1 = await samples.GetAllAsync(cancellationToken);
        var byMachine = samples1
            .GroupBy(s => s.MachineId)
            .ToDictionary(g => g.Key, IReadOnlyCollection<CalibrationSample> (g) => [.. g]);

        var now = clock.UtcNow;
        var statuses = (from lab in labs1 from machine in lab.Machines let machineSamples = byMachine.GetValueOrDefault(machine.Id) ?? Array.Empty<CalibrationSample>() select RecalibrateCommandHandler.Apply(lab, machine, machineSamples, now)).ToList();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CalibrationStatusDto>>(statuses);
    }
}
