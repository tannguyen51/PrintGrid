using PrintGrid.Modules.Scheduling.Application.DTOs;

namespace PrintGrid.Modules.Scheduling.Application.Mappings;

public static class JobMappings
{
    public static JobDto ToDto(Domain.Entities.Job job) => new(
        job.Id,
        job.OrderItemId,
        job.ModelId,
        job.Status.ToString(),
        job.InternalDueDate,
        job.EstimatedPrintMinutes,
        job.Specification.MaterialGrams,
        job.ActualPrintMinutes,
        job.ActualMaterialGrams,
        job.LabId,
        job.MachineId,
        job.PlannedStartUtc,
        job.PlannedEndUtc,
        job.StartedAtUtc,
        job.CompletedAtUtc,
        job.AssignedAtUtc,
        job.FailureReason,
        job.Specification.MaterialCode,
        job.Specification.ColorCode,
        job.Specification.LayerHeightMm,
        job.AttemptNumber,
        job.Quantity,
        job.ParentJobId);
}
