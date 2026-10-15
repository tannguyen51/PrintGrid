using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public record SelfRegisterLabCommand(
    string Name,
    string City,
    int TransitDaysToHub,
    string OperatingHours,
    Guid? OwnerUserId = null,
    string? ContactPhone = null,
    string? Address = null) : IRequest<Result<LabDto>>;

public class SelfRegisterLabCommandHandler : IRequestHandler<SelfRegisterLabCommand, Result<LabDto>>
{
    private readonly ILabRepository _labs;
    private readonly IUnitOfWork _unitOfWork;

    public SelfRegisterLabCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LabDto>> Handle(SelfRegisterLabCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.City))
            return Result.Failure<LabDto>(Error.Validation("Tên xưởng và thành phố là bắt buộc."));

        if (string.IsNullOrWhiteSpace(command.OperatingHours))
            return Result.Failure<LabDto>(Error.Validation("Lịch làm việc của xưởng là bắt buộc."));

        if (command.TransitDaysToHub < 0)
            return Result.Failure<LabDto>(Error.Validation("Thời gian vận chuyển về Hub không hợp lệ."));

        if (command.OwnerUserId.HasValue)
        {
            var existing = await _labs.GetByOwnerUserIdAsync(command.OwnerUserId.Value, cancellationToken);
            if (existing is not null)
            {
                existing.UpdateProfile(
                    command.Name,
                    command.City,
                    command.TransitDaysToHub,
                    command.OperatingHours,
                    command.ContactPhone,
                    command.Address);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success(LabMappings.ToDto(existing));
            }
        }

        var lab = Lab.SelfRegister(
            command.Name,
            command.City,
            command.TransitDaysToHub,
            command.OperatingHours,
            command.OwnerUserId,
            command.ContactPhone,
            command.Address);

        await _labs.AddAsync(lab, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(LabMappings.ToDto(lab));
    }
}

