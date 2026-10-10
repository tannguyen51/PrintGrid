using System.Text.Json;
using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

/// <summary>UC-020/025/043 trace lookup: why was this job placed where it was?</summary>
public record GetDecisionLogQuery(Guid JobId) : IRequest<Result<DecisionLogDto>>;

public class GetDecisionLogQueryHandler : IRequestHandler<GetDecisionLogQuery, Result<DecisionLogDto>>
{
    private readonly IJobRepository _jobs;
    private readonly IAssignmentDecisionRepository _decisions;

    public GetDecisionLogQueryHandler(IJobRepository jobs, IAssignmentDecisionRepository decisions)
    {
        _jobs = jobs;
        _decisions = decisions;
    }

    public async Task<Result<DecisionLogDto>> Handle(
        GetDecisionLogQuery query,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(query.JobId, cancellationToken);
        if (job is null)
            return Result.Failure<DecisionLogDto>(Error.NotFound("Job", query.JobId));

        var entries = await _decisions.GetByJobIdAsync(query.JobId, cancellationToken);

        var dtos = entries
            .Select(d => new DecisionEntryDto(
                d.Id,
                d.CreatedAtUtc,
                d.AttemptNumber,
                d.Trigger,
                d.ActorType,
                d.ActorId,
                d.Outcome,
                d.Reason,
                d.ChosenLabId,
                d.ChosenMachineId,
                d.ChosenScore,
                d.ScoringConfigVersion,
                d.TimeBudgetMs,
                d.ElapsedMs,
                d.BudgetExceeded,
                Parse(d.CandidatesJson),
                Parse(d.RankingJson),
                Parse(d.ScoringConfigJson)))
            .ToList();

        return Result.Success(new DecisionLogDto(job.Id, dtos));
    }

    /// <summary>
    /// Stored snapshots are already JSON; hand them back as JSON rather than as escaped
    /// strings. Clone() detaches the element from the document we are about to dispose.
    /// </summary>
    private static JsonElement Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            using var empty = JsonDocument.Parse("[]");
            return empty.RootElement.Clone();
        }
    }
}
