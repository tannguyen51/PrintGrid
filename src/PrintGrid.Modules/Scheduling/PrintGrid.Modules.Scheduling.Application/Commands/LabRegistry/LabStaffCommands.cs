using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public sealed record LabStaffDto(Guid UserId, DateTime CreatedAtUtc);

public record GetLabStaffQuery(Guid LabId) : IRequest<Result<IReadOnlyList<LabStaffDto>>>;

public record AddLabStaffCommand(Guid LabId, Guid UserId) : IRequest<Result>;

public record RemoveLabStaffCommand(Guid LabId, Guid UserId) : IRequest<Result>;

public class GetLabStaffQueryHandler : IRequestHandler<GetLabStaffQuery, Result<IReadOnlyList<LabStaffDto>>>
{
    private readonly ILabRepository _labs;

    public GetLabStaffQueryHandler(ILabRepository labs) => _labs = labs;

    public async Task<Result<IReadOnlyList<LabStaffDto>>> Handle(GetLabStaffQuery query, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(query.LabId, cancellationToken);
        if (lab is null) return Result.Failure<IReadOnlyList<LabStaffDto>>(Error.NotFound("Lab", query.LabId));

        IReadOnlyList<LabStaffDto> staff = lab.Staff
            .Select(s => new LabStaffDto(s.UserId, s.CreatedAtUtc))
            .ToList();
        return Result.Success(staff);
    }
}

public class AddLabStaffCommandHandler : IRequestHandler<AddLabStaffCommand, Result>
{
    private readonly ILabRepository _labs;
    private readonly IUnitOfWork _unitOfWork;

    public AddLabStaffCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddLabStaffCommand command, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null) return Result.Failure(Error.NotFound("Lab", command.LabId));

        var result = lab.AddStaff(command.UserId);
        if (result.IsFailure) return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class RemoveLabStaffCommandHandler : IRequestHandler<RemoveLabStaffCommand, Result>
{
    private readonly ILabRepository _labs;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveLabStaffCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveLabStaffCommand command, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null) return Result.Failure(Error.NotFound("Lab", command.LabId));

        if (!lab.RemoveStaff(command.UserId))
            return Result.Failure(Error.NotFound("LabStaff", command.UserId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
