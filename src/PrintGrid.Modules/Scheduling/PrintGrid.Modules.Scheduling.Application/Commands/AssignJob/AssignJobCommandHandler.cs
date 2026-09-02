using MediatR;
using Microsoft.Extensions.Logging;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.AssignJob;

public class AssignJobCommandHandler : IRequestHandler<AssignJobCommand, Result<AssignmentResultDto>>
{
    private readonly IJobRepository _jobs;
    private readonly ILabRepository _labs;
    private readonly CapabilityFilter _capabilityFilter;
    private readonly AssignmentScorer _scorer;
    private readonly IMachineTimelineService _timeline;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AssignJobCommandHandler> _logger;

    public AssignJobCommandHandler(
        IJobRepository jobs,
        ILabRepository labs,
        CapabilityFilter capabilityFilter,
        AssignmentScorer scorer,
        IMachineTimelineService timeline,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<AssignJobCommandHandler> logger)
    {
        _jobs = jobs;
        _labs = labs;
        _capabilityFilter = capabilityFilter;
        _scorer = scorer;
        _timeline = timeline;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<AssignmentResultDto>> Handle(
        AssignJobCommand command,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null)
            return Result.Failure<AssignmentResultDto>(Error.NotFound("Job", command.JobId));

        var labs = await _labs.GetActiveWithMachinesAsync(cancellationToken);
        var filterResult = _capabilityFilter.Filter(job.Specification, labs);

        if (filterResult.Candidates.Count == 0)
        {
            _logger.LogWarning(
                "Job {JobId} has no capable machine; {RejectionCount} machines rejected",
                job.Id, filterResult.Rejections.Count);
            return Result.Failure<AssignmentResultDto>(
                Error.Conflict("No lab in the network can satisfy this job specification"));
        }

        var proposals = new List<PlacementProposal>();
        var windowEnd = job.InternalDueDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        foreach (var candidate in filterResult.Candidates)
        {
            var start = await _timeline.FindEarliestFreeSlotAsync(
                candidate.Machine, job.EstimatedPrintMinutes, _clock.UtcNow, cancellationToken);
            var duration = job.EstimatedPrintMinutes / candidate.Machine.SpeedFactor;
            var end = start.AddMinutes((double)duration);

            if (end > windowEnd) continue;

            var utilization = await _timeline.GetUtilizationAsync(
                candidate.Machine, _clock.UtcNow, windowEnd, cancellationToken);
            var cost = job.Specification.MaterialGrams * 0.5m + (decimal)duration * 0.1m;

            proposals.Add(new PlacementProposal(candidate, start, end, cost, utilization));
        }

        if (proposals.Count == 0)
            return Result.Failure<AssignmentResultDto>(
                Error.Conflict("No capable machine has capacity before the internal due date"));

        var maxCost = proposals.Max(p => p.EstimatedCost);
        var best = _scorer.Rank(job, proposals, maxCost)[0];

        var assignment = job.AssignTo(
            best.Placement.Candidate.Lab.Id,
            best.Placement.Candidate.Machine.Id,
            best.Placement.PlannedStartUtc,
            best.Placement.PlannedEndUtc,
            best.Score);

        if (assignment.IsFailure)
            return Result.Failure<AssignmentResultDto>(assignment.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Job {JobId} assigned to lab {LabId} machine {MachineId} with score {Score}",
            job.Id, job.LabId, job.MachineId, best.Score);

        return Result.Success(new AssignmentResultDto(
            job.Id,
            best.Placement.Candidate.Lab.Id,
            best.Placement.Candidate.Machine.Id,
            best.Placement.PlannedStartUtc,
            best.Placement.PlannedEndUtc,
            best.Score,
            best.Breakdown));
    }
}
