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
        // MVP rule: one LabManager owns exactly one lab (06-System-Scope §2.2 keeps
        // multi-lab chains out of scope). Self-registration updates the owned lab instead;
        // this admin/lab-created path must not create a second one.
        if (command.OwnerUserId is { } ownerId)
        {
            var existing = await _labs.GetByOwnerUserIdAsync(ownerId, cancellationToken);
            if (existing is not null)
                return Result.Failure<LabDto>(
                    Error.Conflict("Tài khoản này đã sở hữu một xưởng in. Mỗi quản lý chỉ được một xưởng trong MVP."));
        }

        Lab lab;
        try
        {
            lab = Lab.Onboard(command.Name, command.City, command.TransitDaysToHub, command.OwnerUserId,
                command.WorkingDays, command.OpeningTime, command.ClosingTime,
                district: command.District);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<LabDto>(Error.Validation(exception.Message));
        }

        await _labs.AddAsync(lab, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(LabMappings.ToDto(lab));
    }
}
