using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public sealed record LabStaffDto(Guid UserId, DateTime CreatedAtUtc);

public record GetLabStaffQuery(Guid LabId) : IRequest<Result<IReadOnlyList<LabStaffDto>>>;

public record AddLabStaffCommand(Guid LabId, Guid UserId) : IRequest<Result>;

public record RemoveLabStaffCommand(Guid LabId, Guid UserId) : IRequest<Result>;

public class GetLabStaffQueryHandler(ILabRepository labs)
    : IRequestHandler<GetLabStaffQuery, Result<IReadOnlyList<LabStaffDto>>>
{
    public async Task<Result<IReadOnlyList<LabStaffDto>>> Handle(GetLabStaffQuery query, CancellationToken cancellationToken)
    {
        var lab = await labs.GetByIdAsync(query.LabId, cancellationToken);
        if (lab is null) return Result.Failure<IReadOnlyList<LabStaffDto>>(Error.NotFound("Lab", query.LabId));

        IReadOnlyList<LabStaffDto> staff =
        [
            .. lab.Staff
                .Select(s => new LabStaffDto(s.UserId, s.CreatedAtUtc))
        ];
        return Result.Success(staff);
    }
}

public class AddLabStaffCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    : IRequestHandler<AddLabStaffCommand, Result>
{
    public async Task<Result> Handle(AddLabStaffCommand command, CancellationToken cancellationToken)
    {
        var lab = await labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null) return Result.Failure(Error.NotFound("Lab", command.LabId));

        var result = lab.AddStaff(command.UserId);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class RemoveLabStaffCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveLabStaffCommand, Result>
{
    public async Task<Result> Handle(RemoveLabStaffCommand command, CancellationToken cancellationToken)
    {
        var lab = await labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null) return Result.Failure(Error.NotFound("Lab", command.LabId));

        if (!lab.RemoveStaff(command.UserId))
            return Result.Failure(Error.NotFound("LabStaff", command.UserId));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
