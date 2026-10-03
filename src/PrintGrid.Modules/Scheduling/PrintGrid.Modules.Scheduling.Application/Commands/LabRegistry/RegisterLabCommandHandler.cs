using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public class RegisterLabCommandHandler : IRequestHandler<RegisterLabCommand, Result<LabDto>>
{
    private readonly ILabRepository _labs;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterLabCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LabDto>> Handle(RegisterLabCommand command, CancellationToken cancellationToken)
    {
        var lab = Lab.Onboard(command.Name, command.City, command.TransitDaysToHub);

        await _labs.AddAsync(lab, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(LabMappings.ToDto(lab));
    }
}