using System.Text.Json;

namespace PrintGrid.Modules.Scheduling.Application.DTOs;

/// <summary>
/// Decision trace for one job (FR-SCHED-009 / BR-ASSIGN-004): the candidate set, every
/// candidate's per-criterion score, the frozen config version and the final choice, exactly
/// as they were when the decision was made. Contract: GET /ops/decisions/{jobId}.
/// </summary>
public record DecisionLogDto(
    Guid JobId,
    IReadOnlyList<DecisionEntryDto> Decisions);

public record DecisionEntryDto(
    Guid Id,
    DateTime CreatedAtUtc,
    int AttemptNumber,
    string Trigger,
    string ActorType,
    string? ActorId,
    string Outcome,
    string? Reason,
    Guid? ChosenLabId,
    Guid? ChosenMachineId,
    decimal? ChosenScore,
    string ScoringConfigVersion,
    int TimeBudgetMs,
    int ElapsedMs,
    bool BudgetExceeded,
    JsonElement Candidates,
    JsonElement Ranking,
    JsonElement ScoringConfig);

/// <summary>Operations queue item (BR-RESCHED-004 / BR-SCHED-011).</summary>
public record OpsEscalationDto(
    Guid Id,
    Guid JobId,
    Guid OrderItemId,
    string Kind,
    string Reason,
    string Status,
    DateTime CreatedAtUtc);
