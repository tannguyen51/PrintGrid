using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public record ChecklistItemResult(string ItemName, string Status, string? Note = null);

public record InspectJobCommand(
    Guid JobId,
    bool Passed,
    IReadOnlyList<ChecklistItemResult> ChecklistResults,
    IReadOnlyList<string> PhotoUrls,
    FaultAttribution? FaultAttribution = null,
    string? Note = null) : IRequest<Result>;
