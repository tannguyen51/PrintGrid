using MediatR;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public class AcceptJobCommandHandler(IJobRepository jobs, ILabRepository labs, IUnitOfWork unitOfWork)
    : IRequestHandler<AcceptJobCommand, Result>
{
    public async Task<Result> Handle(AcceptJobCommand command, CancellationToken cancellationToken)
    {
        var job = await jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null) return Result.Failure(Error.NotFound("Job", command.JobId));

        if (job.LabId is null) return Result.Failure(Error.Conflict("Job has no assigned lab"));
        var lab = await labs.GetByIdAsync(job.LabId.Value, cancellationToken);
        if (lab is null) return Result.Failure(Error.NotFound("Lab", job.LabId.Value));
        var existingReservations = lab.MaterialReservations.Select(reservation => reservation.Id).ToHashSet();
        try
        {
            var demands = job.MaterialByColor();
            if (demands.Any(d => !lab.HasStock(job.Specification.MaterialCode, d.Key, d.Value * 1.03m)))
                return Result.Failure(Error.Conflict("Không đủ vật liệu cho tất cả màu của đơn."));
            foreach (var demand in demands)
                lab.ReserveMaterial(job.Id, job.Specification.MaterialCode, demand.Key, demand.Value);
        }
        catch (InvalidOperationException ex) { return Result.Failure(Error.Conflict(ex.Message)); }

        var result = job.Accept();
        if (result.IsFailure) return result;

        // Reservations have application-assigned IDs. Mark new rows as Added explicitly
        // so EF inserts them instead of attempting to update rows that do not exist.
        foreach (var reservation in lab.MaterialReservations.Where(reservation => !existingReservations.Contains(reservation.Id)))
            labs.AddMaterialReservation(reservation);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
