using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

/// <summary>
/// Append-only record of one assignment decision (FR-SCHED-009 / BR-ASSIGN-004 /
/// NFR-LEGAL-002): the candidate set considered, every candidate's per-criterion scores,
/// the frozen scoring-config version, the final choice and the reason. Written inside the
/// same transaction as the assignment it describes — a log write that fails rolls the whole
/// decision back (FR-SCHED-009 E1). Rows are never updated or deleted; expired retention
/// archives instead (BR-CONFIG-004).
/// </summary>
public class AssignmentDecision : Entity<Guid>
{
    public Guid JobId { get; private set; }
    public Guid OrderItemId { get; private set; }

    /// <summary>Attempt number of the job at decision time (1 for the first placement).</summary>
    public int AttemptNumber { get; private set; }

    /// <summary>What provoked the decision: initial assignment, a reschedule trigger or an override.</summary>
    public string Trigger { get; private set; } = string.Empty;

    /// <summary>"system" for engine decisions, "ops" for human overrides.</summary>
    public string ActorType { get; private set; } = "system";

    public string? ActorId { get; private set; }

    /// <summary>assigned | no_capable_machine | no_feasible_slot | placement_race.</summary>
    public string Outcome { get; private set; } = string.Empty;

    public string? Reason { get; private set; }

    /// <summary>JSON: every capable candidate plus every machine rejection, as considered.</summary>
    public string CandidatesJson { get; private set; } = "[]";

    /// <summary>JSON: ranked placements with their per-criterion score breakdown.</summary>
    public string RankingJson { get; private set; } = "[]";

    public Guid? ChosenLabId { get; private set; }
    public Guid? ChosenMachineId { get; private set; }
    public decimal? ChosenScore { get; private set; }

    /// <summary>Frozen scoring-config version ("kỳ cấu hình") the decision was made under.</summary>
    public string ScoringConfigVersion { get; private set; } = string.Empty;

    /// <summary>JSON snapshot of the effective weights, so the score is reproducible.</summary>
    public string ScoringConfigJson { get; private set; } = "{}";

    /// <summary>Time budget applied to this decision (BR-SCHED-005).</summary>
    public int TimeBudgetMs { get; private set; }

    public int ElapsedMs { get; private set; }

    /// <summary>True when the time budget cut the search short (NFR-PERF-002 metric).</summary>
    public bool BudgetExceeded { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private AssignmentDecision() { }

    public static AssignmentDecision Record(
        Job job,
        string trigger,
        string outcome,
        string candidatesJson,
        string rankingJson,
        string scoringConfigVersion,
        string scoringConfigJson,
        int timeBudgetMs,
        int elapsedMs,
        bool budgetExceeded,
        string? reason = null,
        string actorType = "system",
        string? actorId = null,
        Guid? chosenLabId = null,
        Guid? chosenMachineId = null,
        decimal? chosenScore = null,
        DateTime? createdAtUtc = null)
    {
        return new AssignmentDecision
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            OrderItemId = job.OrderItemId,
            AttemptNumber = job.AttemptNumber,
            Trigger = trigger,
            ActorType = actorType,
            ActorId = actorId,
            Outcome = outcome,
            Reason = reason,
            CandidatesJson = candidatesJson,
            RankingJson = rankingJson,
            ChosenLabId = chosenLabId,
            ChosenMachineId = chosenMachineId,
            ChosenScore = chosenScore,
            ScoringConfigVersion = scoringConfigVersion,
            ScoringConfigJson = scoringConfigJson,
            TimeBudgetMs = timeBudgetMs,
            ElapsedMs = elapsedMs,
            BudgetExceeded = budgetExceeded,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };
    }
}
