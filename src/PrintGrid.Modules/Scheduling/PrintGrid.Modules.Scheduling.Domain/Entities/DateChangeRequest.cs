using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

public class DateChangeRequest : Entity<Guid>
{
    public Guid JobId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public DateOnly CurrentDueDate { get; private set; }
    public DateOnly ProposedDueDate { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Pending";
    public DateTime CreatedAtUtc { get; private set; }
    private DateChangeRequest() { }
    public static DateChangeRequest Create(Job job, DateOnly proposedDueDate) => new()
    {
        Id = Guid.NewGuid(), JobId = job.Id, OrderItemId = job.OrderItemId,
        CurrentDueDate = job.InternalDueDate, ProposedDueDate = proposedDueDate,
        Reason = "Không thể đáp ứng ngày cam kết sau khi thử chia số lượng cho nhiều lab",
        CreatedAtUtc = DateTime.UtcNow
    };
}
