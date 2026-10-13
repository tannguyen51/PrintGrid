using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public record RegisterLabCommand(
    string Name,
    string City,
    int TransitDaysToHub,
    Guid? OwnerUserId = null,
    string WorkingDays = "Monday,Tuesday,Wednesday,Thursday,Friday",
    string OpeningTime = "08:00",
    string ClosingTime = "17:00") : IRequest<Result<LabDto>>;
