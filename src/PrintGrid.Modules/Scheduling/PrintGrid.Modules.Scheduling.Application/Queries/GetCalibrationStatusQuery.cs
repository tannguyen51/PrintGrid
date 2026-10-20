using MediatR;
using PrintGrid.Modules.Scheduling.Application.Commands.Calibration;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

/// <summary>
/// FR-SCHED-008 calibration status per machine: the applied factor, the number of valid
/// (outlier-filtered) samples, MAPE, and whether the ten-sample gate is met.
/// Deliberately read-only — recalibration is an explicit command.
/// </summary>
public record GetCalibrationStatusQuery(Guid? LabId = null) : IRequest<Result<IReadOnlyList<CalibrationStatusDto>>>;

public class GetCalibrationStatusQueryHandler
    : IRequestHandler<GetCalibrationStatusQuery, Result<IReadOnlyList<CalibrationStatusDto>>>
{
    private readonly ILabRepository _labs;
    private readonly ICalibrationSampleRepository _samples;

    public GetCalibrationStatusQueryHandler(ILabRepository labs, ICalibrationSampleRepository samples)
    {
        _labs = labs;
        _samples = samples;
    }

    public async Task<Result<IReadOnlyList<CalibrationStatusDto>>> Handle(
        GetCalibrationStatusQuery query,
        CancellationToken cancellationToken)
    {
        var labs = await _labs.GetAllAsync(cancellationToken);
        if (query.LabId.HasValue)
            labs = labs.Where(l => l.Id == query.LabId.Value).ToList();

        var samples = await _samples.GetAllAsync(cancellationToken);
        var byMachine = samples
            .GroupBy(s => s.MachineId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var statuses = labs
            .SelectMany(lab => lab.Machines.Select(machine => new
            {
                Lab = lab,
                Machine = machine,
                Valid = EstimationCalibration.CountValid(
                    byMachine.GetValueOrDefault(machine.Id) ?? new List<CalibrationSample>())
            }))
            .OrderBy(x => x.Lab.Name)
            .ThenBy(x => x.Machine.Name)
            .Select(x => new CalibrationStatusDto(
                x.Machine.Id,
                x.Lab.Id,
                x.Lab.Name,
                x.Machine.Name,
                x.Machine.Model,
                x.Machine.CalibrationFactor,
                x.Valid,
                x.Machine.CalibrationMape,
                x.Machine.CalibrationComputedAtUtc,
                x.Valid >= EstimationCalibration.MinimumSamples))
            .ToList();

        return Result.Success<IReadOnlyList<CalibrationStatusDto>>(statuses);
    }
}
