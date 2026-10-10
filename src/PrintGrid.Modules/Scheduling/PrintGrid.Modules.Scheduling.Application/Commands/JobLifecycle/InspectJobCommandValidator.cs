using FluentValidation;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public class InspectJobCommandValidator : AbstractValidator<InspectJobCommand>
{
    private static readonly HashSet<string> _validChecklistStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pass", "Fail", "NotApplicable", "NA"
    };

    public InspectJobCommandValidator()
    {
        RuleFor(c => c.JobId).NotEmpty();
        RuleFor(c => c.Note).MaximumLength(500);

        // AC01 (FR-HUB-002, BR-QC-003): Photographic evidence required
        RuleFor(c => c.PhotoUrls)
            .NotNull()
            .Must(photos => photos != null && photos.Count > 0)
            .WithMessage("Photographic evidence is required for inspection (không ảnh => không lưu kết luận).");

        // AC02 (FR-HUB-002, BR-QC-002): Every checklist item must have PASS / FAIL / NA
        RuleFor(c => c.ChecklistResults)
            .NotNull()
            .Must(items => items != null && items.Count > 0)
            .WithMessage("Inspection checklist items are mandatory.");

        RuleForEach(c => c.ChecklistResults)
            .Must(item => !string.IsNullOrWhiteSpace(item.ItemName) && _validChecklistStatuses.Contains(item.Status))
            .WithMessage("Every checklist item must have status PASS, FAIL, or NotApplicable.");

        // AC03 (FR-HUB-002, BR-QC-004): Failed inspection must have fault attribution
        When(c => !c.Passed, () =>
        {
            RuleFor(c => c.FaultAttribution)
                .NotNull()
                .WithMessage("Fault attribution (Lab, Hub, or Customer) is required when inspection fails.");
        });
    }
}
