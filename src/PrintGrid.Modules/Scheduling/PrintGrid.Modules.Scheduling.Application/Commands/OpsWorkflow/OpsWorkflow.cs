using MediatR;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.Services;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using System.Text.Json;

namespace PrintGrid.Modules.Scheduling.Application.Commands.OpsWorkflow;

public sealed record OpsMonitorPolicy(decimal AtRiskSlackHours);
public sealed record OpsRiskJobDto(Guid JobId, string Status, Guid? LabId, decimal SlackHours, string RiskReason, DateOnly InternalDueDate, DateTime? PlannedEndUtc, int Quantity);
public sealed record OpsLabLoadDto(Guid LabId, string LabName, int ActiveJobs, int Machines, decimal JobsPerMachine, bool IsActive, bool IsInGoodStanding);
public sealed record OpsMonitorDto(int OpenJobs, int AtRiskJobs, decimal AtRiskSlackHours, decimal OnTimeForecastPercent, IReadOnlyList<OpsRiskJobDto> Risks, IReadOnlyList<OpsLabLoadDto> LabLoads, DateTime RefreshedAtUtc);
public sealed record OpsReassignPreviewDto(Guid JobId, bool CanKeepCustomerDate, bool RequiresCustomerApproval, Guid? CurrentLabId, Guid? SuggestedLabId, Guid? SuggestedMachineId, DateTime? PlannedEndUtc, DateOnly? ProposedCustomerDate, string Message);
public sealed record OpsReassignResultDto(string Outcome, Guid JobId, Guid? LabId, Guid? MachineId, Guid? DateChangeRequestId, string Message);

public sealed record GetOpsMonitorQuery : IRequest<Result<OpsMonitorDto>>;
public sealed record PreviewOpsReassignQuery(Guid JobId) : IRequest<Result<OpsReassignPreviewDto>>;
public sealed record ReassignJobByOpsCommand(Guid JobId, Guid ActorId, string Reason, bool ConfirmCustomerDateImpact) : IRequest<Result<OpsReassignResultDto>>;

internal static class OpsRisk
{
    public static decimal SlackHours(Job job, DateTime nowUtc)
    {
        var due = job.InternalDueDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var expectedEnd = job.PlannedEndUtc ?? nowUtc.AddMinutes(job.EstimatedPrintMinutes);
        return decimal.Round((decimal)(due - expectedEnd).TotalHours, 2);
    }

    public static string Reason(Job job, decimal slack) => job.Status switch
    {
        JobStatus.Pending or JobStatus.Reassigned => "Chưa có máy phù hợp trong lịch",
        JobStatus.InProgress when slack < 0 => "Job đang in đã vượt hạn nội bộ",
        _ when job.PlannedEndUtc.HasValue => "Hàng đợi máy đang sát hạn",
        _ => "Không đủ slack để hấp thụ biến động"
    };
}

public sealed class GetOpsMonitorQueryHandler(IJobRepository jobs, ILabRepository labs, OpsMonitorPolicy policy, IDateTimeProvider clock)
    : IRequestHandler<GetOpsMonitorQuery, Result<OpsMonitorDto>>
{
    public async Task<Result<OpsMonitorDto>> Handle(GetOpsMonitorQuery request, CancellationToken ct)
    {
        var open = await jobs.GetOperationalAsync(ct);
        var allLabs = await labs.GetAllAsync(ct);
        var risks = open.Select(job => (Job: job, Slack: OpsRisk.SlackHours(job, clock.UtcNow)))
            .Where(x => x.Slack < policy.AtRiskSlackHours)
            .OrderBy(x => x.Slack)
            .Select(x => new OpsRiskJobDto(x.Job.Id, x.Job.Status.ToString(), x.Job.LabId, x.Slack, OpsRisk.Reason(x.Job, x.Slack), x.Job.InternalDueDate, x.Job.PlannedEndUtc, x.Job.Quantity))
            .ToList();
        var loads = allLabs.Select(lab =>
        {
            var active = open.Count(job => job.LabId == lab.Id);
            var machines = lab.Machines.Count;
            return new OpsLabLoadDto(lab.Id, lab.Name, active, machines, machines == 0 ? active : decimal.Round((decimal)active / machines, 2), lab.IsActive, lab.IsInGoodStanding);
        }).OrderByDescending(x => x.JobsPerMachine).ToList();
        var forecast = open.Count == 0 ? 100m : decimal.Round((decimal)(open.Count - risks.Count) / open.Count * 100m, 1);
        return Result.Success(new OpsMonitorDto(open.Count, risks.Count, policy.AtRiskSlackHours, forecast, risks, loads, clock.UtcNow));
    }
}

public sealed class PreviewOpsReassignQueryHandler(IJobRepository jobs, IAssignmentEngine engine)
    : IRequestHandler<PreviewOpsReassignQuery, Result<OpsReassignPreviewDto>>
{
    public async Task<Result<OpsReassignPreviewDto>> Handle(PreviewOpsReassignQuery request, CancellationToken ct)
    {
        var job = await jobs.GetByIdAsync(request.JobId, ct);
        if (job is null) return Result.Failure<OpsReassignPreviewDto>(Error.NotFound("Job", request.JobId));
        if (job.Status is not (JobStatus.Assigned or JobStatus.Accepted))
            return Result.Failure<OpsReassignPreviewDto>(Error.Conflict("Only an unstarted assigned job can be reassigned"));
        var currentLab = job.LabId;
        var feasible = await engine.AssignAsync(job, new AssignmentRequest(AssignmentTriggers.OpsOverride, ProbeOnly: true, ExcludeLabId: currentLab), ct);
        if (feasible.Ranking.Count > 0)
        {
            var best = feasible.Ranking[0];
            return Result.Success(new OpsReassignPreviewDto(job.Id, true, false, currentLab, best.Placement.Candidate.Lab.Id, best.Placement.Candidate.Machine.Id, best.Placement.PlannedEndUtc, null, "Có thể gán lại mà không đổi ngày cam kết khách."));
        }
        var late = await engine.AssignAsync(job, new AssignmentRequest(AssignmentTriggers.DateChangeProbe, IgnoreDueDate: true, ProbeOnly: true, ExcludeLabId: currentLab), ct);
        var proposed = late.EarliestFeasibleEndUtc is { } end
            ? DateOnly.FromDateTime(end).AddDays(PlacementShortfallService.HubAndTransitBufferDays)
            : job.InternalDueDate.AddDays(PlacementShortfallService.HubAndTransitBufferDays + 1);
        return Result.Success(new OpsReassignPreviewDto(job.Id, false, true, currentLab, null, null, late.EarliestFeasibleEndUtc, proposed, "Gán lại sẽ ảnh hưởng ngày khách; cần bước xác nhận và khách duyệt ngày mới."));
    }
}

public sealed class ReassignJobByOpsCommandHandler(
    IJobRepository jobs,
    IAssignmentEngine engine,
    IDateChangeRequestRepository dateChanges,
    IAssignmentDecisionRepository decisions,
    IScoringConfigurationProvider scoringConfigurations,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReassignJobByOpsCommand, Result<OpsReassignResultDto>>
{
    public async Task<Result<OpsReassignResultDto>> Handle(ReassignJobByOpsCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length < 10)
            return Result.Failure<OpsReassignResultDto>(Error.Validation("Lý do can thiệp phải có ít nhất 10 ký tự"));
        var job = await jobs.GetByIdAsync(command.JobId, ct);
        if (job is null) return Result.Failure<OpsReassignResultDto>(Error.NotFound("Job", command.JobId));
        var preview = await new PreviewOpsReassignQueryHandler(jobs, engine).Handle(new PreviewOpsReassignQuery(command.JobId), ct);
        if (preview.IsFailure) return Result.Failure<OpsReassignResultDto>(preview.Error);

        if (preview.Value.RequiresCustomerApproval)
        {
            if (!command.ConfirmCustomerDateImpact)
                return Result.Failure<OpsReassignResultDto>(Error.Conflict("Xác nhận bước 2 là bắt buộc vì ngày cam kết khách bị ảnh hưởng"));
            if (await dateChanges.HasPendingAsync(job.Id, ct))
                return Result.Failure<OpsReassignResultDto>(Error.Conflict("Job đã có đề nghị đổi ngày đang chờ khách"));
            var request = DateChangeRequest.Propose(job.Id, job.OrderItemId,
                job.InternalDueDate.AddDays(PlacementShortfallService.HubAndTransitBufferDays),
                preview.Value.ProposedCustomerDate!.Value, command.Reason.Trim(), Guid.NewGuid().ToString("N"), clock.UtcNow);
            await dateChanges.AddAsync(request, ct);
            var scoringConfig = await scoringConfigurations.GetActiveAsync(ct);
            await decisions.AddAsync(AssignmentDecision.Record(
                job,
                AssignmentTriggers.OpsOverride,
                "pending_customer_approval",
                JsonSerializer.Serialize(new { capable = Array.Empty<object>(), rejected = Array.Empty<object>(), impact = new { currentLabId = preview.Value.CurrentLabId, proposedCustomerDate = preview.Value.ProposedCustomerDate } }),
                "[]",
                scoringConfig.Version,
                DecisionSnapshotJson.ScoringConfig(scoringConfig),
                0,
                0,
                false,
                command.Reason.Trim(),
                "ops",
                command.ActorId.ToString(),
                chosenLabId: job.LabId,
                chosenMachineId: job.MachineId,
                createdAtUtc: clock.UtcNow), ct);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Success(new OpsReassignResultDto("PendingCustomerApproval", job.Id, job.LabId, job.MachineId, request.Id, "Đã ghi nhận lý do và tạo yêu cầu đổi ngày chờ khách duyệt; lịch hiện tại chưa bị thay đổi."));
        }

        var oldLab = job.LabId;
        var prepared = job.PrepareForOpsReassignment(command.Reason);
        if (prepared.IsFailure) return Result.Failure<OpsReassignResultDto>(prepared.Error);
        var assigned = await engine.AssignAsync(job, new AssignmentRequest(AssignmentTriggers.OpsOverride, "ops", command.ActorId.ToString(), ExcludeLabId: oldLab, Reason: command.Reason.Trim()), ct);
        if (!assigned.Assigned)
            return Result.Failure<OpsReassignResultDto>(Error.Conflict(assigned.Reason ?? "Không thể gán lại job"));
        return Result.Success(new OpsReassignResultDto("Reassigned", job.Id, assigned.Result!.LabId, assigned.Result.MachineId, null, "Đã gán lại qua capability filter và ghi audit."));
    }
}
