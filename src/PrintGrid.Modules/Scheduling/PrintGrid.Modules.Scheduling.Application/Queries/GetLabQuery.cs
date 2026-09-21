using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public record GetLabQuery(Guid LabId) : IRequest<Result<LabDto>>;