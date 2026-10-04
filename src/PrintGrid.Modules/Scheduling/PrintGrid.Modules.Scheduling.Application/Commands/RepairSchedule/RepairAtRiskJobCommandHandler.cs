using MediatR;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.RepairSchedule;

public class RepairAtRiskJobCommandHandler(IJobRepository jobs, ILabRepository labs, IDateChangeRequestRepository requests, CapabilityFilter filter, IMachineTimelineService timeline, IDateTimeProvider clock, IUnitOfWork unitOfWork) : IRequestHandler<RepairAtRiskJobCommand, Result<ScheduleRepairResult>>
{
    public async Task<Result<ScheduleRepairResult>> Handle(RepairAtRiskJobCommand command, CancellationToken ct)
    {
        var job = await jobs.GetByIdAsync(command.JobId, ct);
        if (job is null) return Result.Failure<ScheduleRepairResult>(Error.NotFound("Job", command.JobId));
        if (job.Quantity < 2) return Result.Failure<ScheduleRepairResult>(Error.Validation("Chỉ item có số lượng từ 2 mới có thể chia lô"));

        var candidates = filter.Filter(job.Specification, await labs.GetActiveWithMachinesAsync(ct)).Candidates;
        var dueUtc = job.InternalDueDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var slots = new List<(CapabilityCandidate Candidate, DateTime Start, int Capacity, decimal UnitMinutes)>();
        foreach (var candidate in candidates)
        {
            var unitMinutes = (decimal)job.EstimatedPrintMinutes / job.Quantity / candidate.Machine.SpeedFactor;
            var start = await timeline.FindEarliestFreeSlotAsync(candidate.Machine, Math.Max(1, (int)Math.Ceiling(unitMinutes)), clock.UtcNow, ct);
            var capacity = Math.Max(0, (int)Math.Floor((decimal)(dueUtc - start).TotalMinutes / unitMinutes));
            slots.Add((candidate, start, capacity, unitMinutes));
        }

        // If one lab can still deliver, this is not a split case; normal assignment remains preferable.
        if (slots.Any(x => x.Capacity >= job.Quantity))
            return Result.Success(new ScheduleRepairResult("NotAtRisk", false, [], null, null, "Một lab vẫn đủ khả năng hoàn thành trước hạn; không cần chia lô."));

        var selected = new List<(CapabilityCandidate Candidate, DateTime Start, int Quantity, decimal UnitMinutes)>();
        var remaining = job.Quantity;
        foreach (var slot in slots.Where(x => x.Capacity > 0).OrderByDescending(x => x.Capacity).ThenBy(x => x.Start).GroupBy(x => x.Candidate.Lab.Id).Select(g => g.First()))
        {
            var qty = Math.Min(slot.Capacity, remaining); selected.Add((slot.Candidate, slot.Start, qty, slot.UnitMinutes)); remaining -= qty;
            if (remaining == 0) break;
        }

        if (remaining == 0 && selected.Count >= 2)
        {
            var split = job.Split(selected.Select(x => x.Quantity).ToList());
            if (split.IsFailure) return Result.Failure<ScheduleRepairResult>(split.Error);
            var batches = new List<ScheduleRepairBatch>();
            for (var i = 0; i < split.Value.Count; i++)
            {
                var child = split.Value[i]; var placement = selected[i];
                var end = placement.Start.AddMinutes((double)(placement.UnitMinutes * placement.Quantity));
                var assigned = child.AssignTo(placement.Candidate.Lab.Id, placement.Candidate.Machine.Id, placement.Start, end, 0m, clock.UtcNow);
                if (assigned.IsFailure) return Result.Failure<ScheduleRepairResult>(assigned.Error);
                batches.Add(new(child.Id, placement.Candidate.Lab.Id, placement.Candidate.Machine.Id, child.Quantity, placement.Start, end));
            }
            await jobs.AddRangeAsync(split.Value, ct); await unitOfWork.SaveChangesAsync(ct);
            return Result.Success(new ScheduleRepairResult("SplitAssigned", false, batches, null, null, $"Đã chia {job.Quantity} sản phẩm cho {batches.Count} lab; giá đơn hàng giữ nguyên."));
        }

        if (await requests.HasPendingAsync(job.Id, ct)) return Result.Failure<ScheduleRepairResult>(Error.Conflict("Job đã có đề nghị dời ngày đang chờ xử lý"));
        var totalDailyCapacity = Math.Max(1, slots.Sum(x => Math.Max(x.Capacity, 0)));
        var extraDays = Math.Max(1, (int)Math.Ceiling((decimal)Math.Max(remaining, 1) / totalDailyCapacity));
        var proposed = job.InternalDueDate.AddDays(extraDays); var request = DateChangeRequest.Create(job, proposed);
        await requests.AddAsync(request, ct); await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new ScheduleRepairResult("DateChangeProposed", false, [], request.Id, proposed, "Không thể cứu hạn bằng chia lô; đã sinh đề nghị dời ngày để gửi khách."));
    }
}
