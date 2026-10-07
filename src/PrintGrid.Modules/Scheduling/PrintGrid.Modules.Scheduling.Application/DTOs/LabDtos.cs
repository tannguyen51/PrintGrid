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
    IReadOnlyCollection<string> SupportedMaterials);

public record LabDto(
    Guid Id,
    string Name,
    string City,
    bool IsActive,
    decimal OnTimeDeliveryRate,
    decimal FirstPassYield,
    int TransitDaysToHub,
    DateTime CreatedAt,
    IReadOnlyCollection<MachineDto> Machines);

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
    string? QcRejectionReason);
