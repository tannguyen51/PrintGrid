using MediatR;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public class CompleteJobCommandHandler : IRequestHandler<CompleteJobCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly ILabRepository _labs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public CompleteJobCommandHandler(IJobRepository jobs, ILabRepository labs, IUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _jobs = jobs;
        _labs = labs;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(CompleteJobCommand command, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null) return Result.Failure(Error.NotFound("Job", command.JobId));

        if (job.LabId is null) return Result.Failure(Error.Conflict("Job has no assigned lab"));
        var lab = await _labs.GetByIdAsync(job.LabId.Value, cancellationToken);
        if (lab is null) return Result.Failure(Error.NotFound("Lab", job.LabId.Value));

        var result = job.Complete(
            _clock.UtcNow,
            command.ActualPrintMinutes,
            command.SelfReport,
            command.PhotoKeys,
            command.ActualMaterialGrams);
        if (result.IsFailure) return result;

        // The lab reports what it used; when it does not, what it reserved on acceptance is the
        // honest estimate of what the print consumed — settling on that also releases the
        // reservation instead of leaving it held forever.
        var reservedGrams = lab.MaterialReservations
            .FirstOrDefault(r => r.JobId == job.Id)?.ReservedGrams;
        var consumedGrams = command.ActualMaterialGrams ?? reservedGrams;

        if (consumedGrams is { } consumed)
        {
            try { lab.SettleMaterial(job.Id, consumed); }
            catch (InvalidOperationException ex) { return Result.Failure(Error.Conflict(ex.Message)); }
        }

        // A print the platform caused must not cost the lab material: the reprint of a
        // hub-caused failure is credited back to whoever printed it (CostBearer.System).
        if (job.CostBearer == CostBearer.System && consumedGrams is { } compensated)
        {
            lab.CompensateMaterial(
                job.Id,
                job.Specification.MaterialCode,
                job.Specification.ColorCode,
                compensated,
                "Bồi hoàn do lỗi hub (hệ thống chịu phí)");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
