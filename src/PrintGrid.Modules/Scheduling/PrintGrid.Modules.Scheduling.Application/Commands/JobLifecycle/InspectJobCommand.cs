using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public record ChecklistItemResult(string ItemName, string Status, string? Note = null, string? ItemCode = null);

public record InspectJobCommand(
    Guid JobId,
    bool Passed,
    IReadOnlyList<ChecklistItemResult> ChecklistResults,
    IReadOnlyList<string> PhotoUrls,
    FaultAttribution? FaultAttribution = null,
    string? Note = null,
    Guid? ChecklistVersionId = null,
    string? DefectCode = null) : IRequest<Result>;
