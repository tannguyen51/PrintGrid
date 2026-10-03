using MediatR;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public class InspectJobCommandHandler : IRequestHandler<InspectJobCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;

    public InspectJobCommandHandler(
        IJobRepository jobs,
        IUnitOfWork unitOfWork)
    {
        _jobs = jobs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(InspectJobCommand command, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null)
            return Result.Failure(Error.NotFound("Job", command.JobId));

        if (job.Status != JobStatus.AwaitingInspection)
            return Result.Failure(Error.Conflict($"Job in state {job.Status} cannot be inspected"));

        // AC01 (FR-HUB-002, BR-QC-003): Photographic evidence required
        if (command.PhotoUrls is null || command.PhotoUrls.Count == 0)
            return Result.Failure(Error.Validation("Photographic evidence is required for inspection (không ảnh => không lưu kết luận)."));

        // AC02 (FR-HUB-002, BR-QC-002): Every checklist item must be evaluated
        if (command.ChecklistResults is null || command.ChecklistResults.Count == 0)
            return Result.Failure(Error.Validation("Inspection checklist items are mandatory."));

        if (command.Passed)
        {
            var passResult = job.MarkInspectionPassed(command.PhotoUrls.ToList());
            if (passResult.IsFailure) return passResult;
        }
        else
        {
            // AC03 (FR-HUB-002, BR-QC-004): Trượt thiếu quy trách => không chuyển in lại
            if (!command.FaultAttribution.HasValue)
                return Result.Failure(Error.Validation("Fault attribution (Lab, Hub, or Customer) is required when inspection fails."));

            var fault = command.FaultAttribution.Value;
            var failureReason = command.Note ?? $"Failed hub inspection ({fault} fault)";

            // FR-HUB-003 · BR-QC-005...007:
            if (fault is FaultAttribution.Lab or FaultAttribution.Hub)
            {
                // AC01: FAIL + lỗi xưởng/hub => Job URGENT xuất hiện ở lab
                var reprintJob = Job.Create(
                    job.OrderItemId,
                    job.ModelId,
                    job.Specification,
                    job.EstimatedPrintMinutes,
                    job.InternalDueDate);

                await _jobs.AddAsync(reprintJob, cancellationToken);

                var failResult = job.FailInspectionWithReprint(
                    reprintJob.Id,
                    fault,
                    failureReason,
                    command.PhotoUrls.ToList());

                if (failResult.IsFailure) return failResult;
            }
            else // FaultAttribution.Customer
            {
                // AC02: lỗi khách => email gửi, 0 job tạo
                var message = $"Chi tiết in '{job.Specification.MaterialCode}' không đạt kiểm chuẩn do vấn đề mô hình file 3D từ khách hàng. Vui lòng liên hệ để xác nhận in lại có phí hoặc hoàn tiền.";
                var failResult = job.FailInspectionCustomerFault(
                    failureReason,
                    command.PhotoUrls.ToList(),
                    message);

                if (failResult.IsFailure) return failResult;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

