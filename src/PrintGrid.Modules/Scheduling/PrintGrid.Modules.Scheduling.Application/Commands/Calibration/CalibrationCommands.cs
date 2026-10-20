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

public class CollectCalibrationSamplesCommandHandler : IRequestHandler<CollectCalibrationSamplesCommand, Result<int>>
{
    private readonly IJobRepository _jobs;
    private readonly ICalibrationSampleRepository _samples;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public CollectCalibrationSamplesCommandHandler(
        IJobRepository jobs,
        ICalibrationSampleRepository samples,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _jobs = jobs;
        _samples = samples;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<int>> Handle(CollectCalibrationSamplesCommand command, CancellationToken cancellationToken)
    {
        var completedJobs = await _jobs.GetCompletedWithActualTimeAsync(cancellationToken);
        var candidateJobs = completedJobs
            .Where(j => j.MachineId.HasValue
                        && j.ActualPrintMinutes is > 0
                        && j.EstimatedPrintMinutes > 0)
            .ToList();
        if (candidateJobs.Count == 0) return Result.Success(0);

        var alreadySampled = await _samples.GetSampledJobIdsAsync(
            candidateJobs.Select(j => j.Id).ToList(), cancellationToken);

        var now = _clock.UtcNow;
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

        if (newSamples.Count > 0)
        {
            await _samples.AddRangeAsync(newSamples, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(newSamples.Count);
    }
}

public class RecalibrateCommandHandler : IRequestHandler<RecalibrateCommand, Result<CalibrationStatusDto>>
{
    private readonly ILabRepository _labs;
    private readonly ICalibrationSampleRepository _samples;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public RecalibrateCommandHandler(
        ILabRepository labs,
        ICalibrationSampleRepository samples,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _labs = labs;
        _samples = samples;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CalibrationStatusDto>> Handle(RecalibrateCommand command, CancellationToken cancellationToken)
    {
        var (lab, machine) = await FindMachineAsync(command.MachineId, cancellationToken);
        if (lab is null || machine is null)
            return Result.Failure<CalibrationStatusDto>(Error.NotFound("Machine", command.MachineId));

        var samples = await _samples.GetByMachineIdAsync(machine.Id, cancellationToken);
        var status = Apply(lab, machine, samples, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(status);
    }

    internal async Task<(Lab? Lab, Machine? Machine)> FindMachineAsync(Guid machineId, CancellationToken cancellationToken)
    {
        var labs = await _labs.GetAllAsync(cancellationToken);
        foreach (var lab in labs)
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

public class RecalibrateAllCommandHandler : IRequestHandler<RecalibrateAllCommand, Result<IReadOnlyList<CalibrationStatusDto>>>
{
    private readonly ILabRepository _labs;
    private readonly ICalibrationSampleRepository _samples;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public RecalibrateAllCommandHandler(
        ILabRepository labs,
        ICalibrationSampleRepository samples,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _labs = labs;
        _samples = samples;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<CalibrationStatusDto>>> Handle(
        RecalibrateAllCommand command,
        CancellationToken cancellationToken)
    {
        var labs = await _labs.GetAllAsync(cancellationToken);
        var samples = await _samples.GetAllAsync(cancellationToken);
        var byMachine = samples
            .GroupBy(s => s.MachineId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<CalibrationSample>)g.ToList());

        var now = _clock.UtcNow;
        var statuses = new List<CalibrationStatusDto>();
        foreach (var lab in labs)
        {
            foreach (var machine in lab.Machines)
            {
                var machineSamples = byMachine.GetValueOrDefault(machine.Id) ?? Array.Empty<CalibrationSample>();
                statuses.Add(RecalibrateCommandHandler.Apply(lab, machine, machineSamples, now));
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CalibrationStatusDto>>(statuses);
    }
}
