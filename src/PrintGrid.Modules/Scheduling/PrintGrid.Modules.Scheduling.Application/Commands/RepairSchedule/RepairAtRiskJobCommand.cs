using MediatR;
using PrintGrid.SharedKernel.Results;
namespace PrintGrid.Modules.Scheduling.Application.Commands.RepairSchedule;
public record RepairAtRiskJobCommand(Guid JobId) : IRequest<Result<ScheduleRepairResult>>;
public record ScheduleRepairBatch(Guid JobId, Guid LabId, Guid MachineId, int Quantity, DateTime PlannedStartUtc, DateTime PlannedEndUtc);
public record ScheduleRepairResult(string Outcome, bool PriceChanged, IReadOnlyList<ScheduleRepairBatch> Batches, Guid? DateChangeRequestId, DateOnly? ProposedDueDate, string Message);
