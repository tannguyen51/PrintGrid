using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public record GetLabsQuery(bool IncludeInactive = false, Guid? OwnerUserId = null) : IRequest<Result<IReadOnlyList<LabDto>>>;
