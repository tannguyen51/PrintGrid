using MediatR;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
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

        // The stock ledger only moves when the lab reports what it actually used. Until the
        // completion form collects grams, the ledger is settled only when they are supplied.
        if (command.ActualMaterialGrams is { } actualGrams)
        {
            try { lab.SettleMaterial(job.Id, actualGrams); }
            catch (InvalidOperationException ex) { return Result.Failure(Error.Conflict(ex.Message)); }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
