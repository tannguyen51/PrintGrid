using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;

namespace PrintGrid.Modules.Scheduling.Domain.Services;

public record CapabilityCandidate(Lab Lab, Machine Machine);

public record CapabilityRejection(Guid MachineId, string Reason);

public record CapabilityFilterResult(
    IReadOnlyList<CapabilityCandidate> Candidates,
    IReadOnlyList<CapabilityRejection> Rejections);

public class CapabilityFilter
{
    public CapabilityFilterResult Filter(JobSpecification spec, IEnumerable<Lab> labs)
    {
        var candidates = new List<CapabilityCandidate>();
        var rejections = new List<CapabilityRejection>();

        foreach (var lab in labs.Where(l => l.IsActive))
        {
            if (!lab.IsInGoodStanding)
            {
                rejections.AddRange(lab.Machines.Select(m => new CapabilityRejection(m.Id, "lab_not_in_good_standing")));
                continue;
            }

            foreach (var machine in lab.Machines)
            {
                var reason = Reject(lab, machine, spec);
                if (reason is null)
                {
                    candidates.Add(new CapabilityCandidate(lab, machine));
                }
                else
                {
                    rejections.Add(new CapabilityRejection(machine.Id, reason));
                }
            }
        }

        return new CapabilityFilterResult(candidates, rejections);
    }

    private static string? Reject(Lab lab, Machine machine, JobSpecification spec)
    {
        if (machine.Status == MachineStatus.Offline) return "machine_offline";
        if (machine.Status == MachineStatus.Maintenance) return "machine_in_maintenance";
        if (machine.Technology != spec.Technology) return "technology_mismatch";
        if (!machine.BuildVolume.CanFit(spec.RequiredVolume)) return "build_volume_too_small";
        if (machine.MinLayerHeightMm > spec.LayerHeightMm) return "layer_height_unachievable";
        if (machine.AchievableToleranceMm > spec.ToleranceMm) return "tolerance_unachievable";
        if (!machine.SupportedMaterials.Contains(spec.MaterialCode)) return "material_unsupported";
        if (!lab.HasStock(spec.MaterialCode, spec.ColorCode, spec.MaterialGrams)) return "material_or_color_out_of_stock";
        return null;
    }
}
