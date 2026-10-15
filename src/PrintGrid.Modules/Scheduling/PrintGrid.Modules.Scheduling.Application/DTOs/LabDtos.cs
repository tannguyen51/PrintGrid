using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Modules.Scheduling.Application.DTOs;

public record MachineDto(
    Guid Id,
    string Name,
    string Model,
    PrintTechnology Technology,
    decimal BuildWidthMm,
    decimal BuildDepthMm,
    decimal BuildHeightMm,
    decimal MinLayerHeightMm,
    decimal AchievableToleranceMm,
    decimal SpeedFactor,
    MachineStatus Status,
    IReadOnlyCollection<string> SupportedMaterials,
    DateTime? MaintenanceFromUtc = null,
    DateTime? MaintenanceUntilUtc = null,
    string? MaintenanceReason = null);

public record LabDto(
    Guid Id,
    string Name,
    string City,
    Guid? OwnerUserId,
    bool IsActive,
    bool IsInGoodStanding,
    int? MaxConcurrentJobs,
    decimal OnTimeDeliveryRate,
    decimal FirstPassYield,
    int TransitDaysToHub,
    string WorkingDays,
    string OpeningTime,
    string ClosingTime,
    int MaterialStockCount,
    DateTime CreatedAt,
    IReadOnlyCollection<MachineDto> Machines,
    string Status = "Active",
    string OperatingHours = "T2-T7 08:00-18:00",
    Guid? OwnerUserId = null,
    string? ContactPhone = null,
    string? Address = null,
    string? RejectionReason = null,
    bool IsDeclarationComplete = true);

public record MachineTimelineSlotDto(
    Guid JobId,
    string? OrderNumber,
    string? ModelFileName,
    string MaterialCode,
    string ColorCode,
    string Status,
    int AttemptNumber,
    DateOnly InternalDueDate,
    int EstimatedPrintMinutes,
    DateTime PlannedStartUtc,
    DateTime PlannedEndUtc);

public record MachineTimelineDto(
    Guid MachineId,
    string MachineName,
    string Model,
    PrintTechnology Technology,
    MachineStatus Status,
    DateTime? MaintenanceFromUtc,
    DateTime? MaintenanceUntilUtc,
    string? MaintenanceReason,
    int TotalQueueMinutes,
    IReadOnlyList<MachineTimelineSlotDto> Slots);

public record LabTimelineDto(
    Guid LabId,
    string LabName,
    string OperatingHours,
    int TransitDaysToHub,
    DateTime GeneratedAtUtc,
    IReadOnlyList<MachineTimelineDto> Machines);

public record LabPerformanceMetricsDto(
    decimal OnTimeDeliveryRate,
    decimal FirstPassYield,
    decimal AcceptanceRate,
    decimal UtilizationRate);

public record LabPerformanceDto(
    Guid LabId,
    string LabName,
    int CompletedJobsCount,
    int TotalAssignedJobsCount,
    bool IsProvisional,
    decimal StandingScore,
    string StandingLabel,
    LabPerformanceMetricsDto LabMetrics,
    LabPerformanceMetricsDto NetworkAnonymousAverage,
    decimal NetworkAverageStandingScore,
    int ActiveLabsInBenchmark);

public record MaterialStockDto(
    Guid Id,
    string MaterialCode,
    string ColorCode,
    decimal AvailableGrams,
    decimal ReservedGrams,
    decimal AssignableGrams,
    decimal ReorderPointGrams,
    bool IsLowStock,
    DateTime UpdatedAtUtc);

public record StockTransactionDto(
    string TransactionCode,
    Guid MaterialStockId,
    decimal DeltaGrams,
    decimal RunningTotalGrams,
    string Reason,
    Guid? JobId,
    DateTime CreatedAtUtc);

public record JobDto(
    Guid Id,
    Guid OrderItemId,
    Guid ModelId,
    string Status,
    DateOnly InternalDueDate,
    int EstimatedPrintMinutes,
    decimal EstimatedMaterialGrams,
    int? ActualPrintMinutes,
    decimal? ActualMaterialGrams,
    Guid? LabId,
    Guid? MachineId,
    DateTime? PlannedStartUtc,
    DateTime? PlannedEndUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? AssignedAtUtc,
    DateTime? AcceptanceDeadlineUtc,
    string? FailureReason,
    string MaterialCode,
    string ColorCode,
    decimal LayerHeightMm,
    int AttemptNumber,
    int Quantity,
    Guid? ParentJobId,
    string QcProofStatus,
    string? QcSelfReport,
    IReadOnlyList<string> QcProofPhotoKeys,
    Guid? QcReviewedBy,
    DateTime? QcReviewedAtUtc,
    string? QcRejectionReason,
    // Order/model context (08/10): specs ops and lab need to place and print the job.
    // Filled from JobSpecification; the three trailing fields are enriched by the query
    // handler (order number + file name + hash) — no customer PII crosses this line.
    decimal BoundingWidthMm,
    decimal BoundingDepthMm,
    decimal BoundingHeightMm,
    decimal ToleranceMm,
    string Technology,
    string? OrderNumber = null,
    string? ModelFileName = null,
    string? Sha256 = null);
