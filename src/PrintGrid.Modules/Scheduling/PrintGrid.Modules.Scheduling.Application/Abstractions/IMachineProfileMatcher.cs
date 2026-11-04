using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Application.Abstractions;

public interface IMachineProfileMatcher
{
    Task MatchAsync(Machine machine, CancellationToken cancellationToken);
}
