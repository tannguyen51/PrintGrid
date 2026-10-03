using MediatR;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Modules.Customer.Application.Models;

/// <summary>
/// BR-IP-002: the customer gets an electronic receipt for every file handover —
/// "we received file X at time T, size S, hash H". Written to the durable email
/// outbox inside the same save path as the upload itself.
/// </summary>
public class ModelUploadedEmailHandler : INotificationHandler<ModelUploadedEvent>
{
    private readonly ICustomerRepository _customers;
    private readonly IEmailOutbox _outbox;

    public ModelUploadedEmailHandler(ICustomerRepository customers, IEmailOutbox outbox)
    {
        _customers = customers;
        _outbox = outbox;
    }

    public async Task Handle(ModelUploadedEvent notification, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(notification.CustomerId, cancellationToken);
        if (customer is null) return;

        var body =
            $"PrintGrid đã nhận file \"{notification.FileName}\" của bạn.\n\n" +
            $"Thời điểm tiếp nhận (UTC): {notification.OccurredOn:dd/MM/yyyy HH:mm:ss}\n" +
            $"Dung lượng: {notification.SizeBytes:N0} bytes\n" +
            $"SHA-256: {notification.Sha256}\n\n" +
            "Bản ghi này là bằng chứng điện tử về việc bàn giao file. " +
            "File thuộc quyền sở hữu của bạn; nền tảng và các xưởng in không được phép sử dụng lại.";

        await _outbox.QueueAsync(customer.Email, "[PrintGrid] Xác nhận đã nhận file 3D", body, cancellationToken);
    }
}
