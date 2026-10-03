using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public class SetLabActiveCommandHandler : IRequestHandler<SetLabActiveCommand, Result>
{
    private readonly ILabRepository _labs;
    private readonly IUnitOfWork _unitOfWork;

    public SetLabActiveCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetLabActiveCommand command, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null)
            return Result.Failure(Error.NotFound("Lab", command.LabId));

        if (command.IsActive) lab.Activate();
        else lab.Deactivate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}