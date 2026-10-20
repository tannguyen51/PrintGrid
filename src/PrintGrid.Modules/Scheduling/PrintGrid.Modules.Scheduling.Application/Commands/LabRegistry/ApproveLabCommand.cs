using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public record ApproveLabCommand(Guid LabId, Guid? ApprovedByUserId = null) : IRequest<Result<LabDto>>;

public record RejectLabCommand(Guid LabId, string Reason) : IRequest<Result<LabDto>>;

public class ApproveLabCommandHandler :
    IRequestHandler<ApproveLabCommand, Result<LabDto>>,
    IRequestHandler<RejectLabCommand, Result<LabDto>>
{
    private readonly ILabRepository _labs;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveLabCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LabDto>> Handle(ApproveLabCommand command, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null)
            return Result.Failure<LabDto>(Error.NotFound("Lab", command.LabId));

        var result = lab.Approve(command.ApprovedByUserId);
        if (result.IsFailure)
            return Result.Failure<LabDto>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(LabMappings.ToDto(lab));
    }

    public async Task<Result<LabDto>> Handle(RejectLabCommand command, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null)
            return Result.Failure<LabDto>(Error.NotFound("Lab", command.LabId));

        var result = lab.Reject(command.Reason);
        if (result.IsFailure)
            return Result.Failure<LabDto>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(LabMappings.ToDto(lab));
    }
}

