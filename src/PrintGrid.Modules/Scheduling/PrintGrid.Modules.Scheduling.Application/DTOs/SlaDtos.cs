namespace PrintGrid.Modules.Scheduling.Application.DTOs;

/// <summary>
/// One lab's contribution to the FR-ANAL-002/003 SLA report. Rates are 0..1 fractions;
/// the CSV export and UI render them as percentages.
/// </summary>
public sealed record SlaLabRowDto(
    Guid? LabId,
    string LabName,
    decimal OnTimeDeliveryRate,
    decimal FirstPassYield,
    decimal AcceptanceRate,
    decimal UtilizationRate,
    int CompletedJobs,
    int AssignedJobs,
    int LateJobs,
    bool IsProvisional);

/// <summary>
/// 90-day network SLA report (BR-PERF-001). <see cref="Labs"/> is empty when the network has
/// no labs, which the CSV export surfaces as a "no data" line instead of a row of zeroes.
/// </summary>
public sealed record SlaReportDto(
    DateTime GeneratedAtUtc,
    DateTime WindowStartUtc,
    DateTime WindowEndUtc,
    int WindowDays,
    Guid? LabFilter,
    SlaLabRowDto Network,
    IReadOnlyList<SlaLabRowDto> Labs);
