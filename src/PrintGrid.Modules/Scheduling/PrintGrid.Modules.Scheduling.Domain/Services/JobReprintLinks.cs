using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Modules.Scheduling.Domain.Services;

/// <summary>Resolve replacement attempts without confusing separate branches of a split.</summary>
public static class JobReprintLinks
{
    public static bool HasReplacement(Job failed, IReadOnlyList<Job> siblings) => FindReplacement(failed, siblings) is not null;

    public static Job? FindReplacement(Job failed, IReadOnlyList<Job> siblings) => siblings.FirstOrDefault(candidate =>
        candidate.OrderItemId == failed.OrderItemId &&
        (candidate.ReprintOfJobId == failed.Id ||
         // Legacy attempts have no direct link. Only an unsplit attempt root can replace a
         // failed job: a sibling batch with a higher index must never cover another branch.
         candidate.ReprintOfJobId == null && candidate.ParentJobId == null &&
         candidate.ReprintIndex > failed.ReprintIndex &&
         candidate.OriginalJobId == (failed.OriginalJobId ?? failed.Id)) &&
        (candidate.Status != JobStatus.Cancelled || siblings.Any(child => child.ParentJobId == candidate.Id)));
}
