using MediatR;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Queries.Machines;

public sealed class GetEligibleMachineModelsQueryHandler(PrintGridDbContext db) : IRequestHandler<GetEligibleMachineModelsQuery, Result<IReadOnlyList<ModelSummary>>>
{
    public async Task<Result<IReadOnlyList<ModelSummary>>> Handle(GetEligibleMachineModelsQuery request, CancellationToken cancellationToken)
    {
        const int minMachines = 3;
        const int minLabs = 2;

        var models = await db.Set<Machine>()
            .Include(m => m.PrinterConfiguration)
            .Where(m => m.ProfileStatus == "Approved" && !m.IsDeprecated)
            .GroupBy(m => new { m.Model, m.CuraProfile?.Manufacturer })
            .Select(g => new ModelSummary(
                Id: g.Key.Manufacturer != null ? Guid.Parse(g.Key.Manufacturer) : Guid.Empty,
                Name: g.Key.Model ?? "",
                ApprovedMachineCount: g.Count(),
                LabCount: g.Select(m => m.LabId).Distinct().Count()
            ))
            .Where(x => x.ApprovedMachineCount >= minMachines && x.LabCount >= minLabs)
            .OrderByDescending(x => x.ApprovedMachineCount)
            .ToListAsync(cancellationToken);

        return models.AsReadOnly();
    }
}
