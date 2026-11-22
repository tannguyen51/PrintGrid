using System.Text.Json;

namespace PrintGrid.Modules.Admin.Application.Configuration;

public sealed record CommercialParametersDto(
    IReadOnlyDictionary<string, decimal> MaterialRates,
    IReadOnlyDictionary<string, decimal> MaterialDensities,
    decimal PricePerPrintHour,
    decimal HubHandlingFee,
    decimal PlatformMarginPercent,
    decimal PrioritySurchargePercent,
    decimal AutoLaneThreshold,
    int QuoteValidityHours,
    IReadOnlyDictionary<string, decimal> AssignmentWeights,
    int MaxBatchSize,
    int SchedulingTimeBudgetSeconds,
    int SlaRiskThresholdHours,
    IReadOnlyDictionary<string, decimal>? ColorPricePerKg = null,
    decimal FailureRate = 0m,
    decimal PolishingPerUnit = 0m,
    decimal LabProvisionalBaseScore = 0.5m,
    int LabProvisionalWeightK = 5,
    int LabProvisionalThreshold = 10,
    decimal PinSurchargeCoefficient = 1.15m,
    decimal MaterialWasteRate = .03m,
    decimal SetupFeePerPlate = 20_000m,
    decimal FailureCompletionRatio = .5m,
    int PriceRoundingIncrement = 1000,
    int MinPinMachines = 3,
    int MinPinLabs = 2,
    IReadOnlyDictionary<string, Guid>? ReferenceProfileIds = null);

public sealed record CommercialConfigurationDto(
    Guid Id, int VersionNumber, string Name, string Status,
    CommercialParametersDto Parameters, string ChangeSummary,
    Guid CreatedBy, DateTime CreatedAtUtc, DateTime? ActivatedAtUtc, DateTime? ScheduledActivationAtUtc);

public sealed record SharedCatalogItemDto(
    Guid Id, string CatalogType, string Code, int Revision, string Name,
    string? Description, decimal? UnitRate, bool IsPremium,
    JsonElement Properties, bool IsDeprecated, DateTime CreatedAtUtc);

public sealed record MutationResult<T>(T? Value, string? ErrorCode = null, string? ErrorMessage = null)
{
    public bool IsSuccess => ErrorCode is null;
    public static MutationResult<T> Success(T value) => new(value);
    public static MutationResult<T> Failure(string code, string message) => new(default, code, message);
}
