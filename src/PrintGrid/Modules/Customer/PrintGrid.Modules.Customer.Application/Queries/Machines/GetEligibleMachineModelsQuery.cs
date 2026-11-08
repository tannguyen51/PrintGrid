using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Queries.Machines;

public sealed record GetEligibleMachineModelsQuery(Guid? MaterialCode) : IRequest<Result<IReadOnlyList<ModelSummary>>>;

public sealed record ModelSummary(Guid Id, string Name, int ApprovedMachineCount, int LabCount);
