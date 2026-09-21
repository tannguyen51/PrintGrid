using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public record RegisterLabCommand(
    string Name,
    string City,
    int TransitDaysToHub) : IRequest<Result<LabDto>>;