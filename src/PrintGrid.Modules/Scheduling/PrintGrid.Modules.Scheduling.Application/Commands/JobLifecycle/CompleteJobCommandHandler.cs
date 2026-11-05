using MediatR;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public class CompleteJobCommandHandler(
    IJobRepository jobs,
    ILabRepository labs,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<CompleteJobCommand, Result>
{
    public async Task<Result> Handle(CompleteJobCommand command, CancellationToken cancellationToken)
    {
        var job = await jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null) return Result.Failure(Error.NotFound("Job", command.JobId));

        if (job.LabId is null) return Result.Failure(Error.Conflict("Job has no assigned lab"));
        var lab = await labs.GetByIdAsync(job.LabId.Value, cancellationToken);
        if (lab is null) return Result.Failure(Error.NotFound("Lab", job.LabId.Value));

        var result = job.Complete(
            clock.UtcNow,
            command.ActualPrintMinutes,
            command.SelfReport,
            command.PhotoKeys,
            command.ActualMaterialGrams);
        if (result.IsFailure) return result;

        // The lab reports what it used; when it does not, what it reserved on acceptance is the
        // honest estimate of what the print consumed — settling on that also releases the
        // reservation instead of leaving it held forever.
        decimal? reservedGrams = lab.MaterialReservations.Any(r => r.JobId == job.Id)
            ? lab.MaterialReservations.Where(r => r.JobId == job.Id).Sum(r => r.ReservedGrams) : null;
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
            var colors = job.MaterialByColor();
            var total = colors.Values.Sum();
            foreach (var color in colors)
            {
                lab.CompensateMaterial(job.Id, job.Specification.MaterialCode, color.Key,
                    compensated * color.Value / total, "Bồi hoàn do lỗi hub (hệ thống chịu phí)");
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
