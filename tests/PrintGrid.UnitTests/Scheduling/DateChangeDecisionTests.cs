using NSubstitute;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Application.Commands.DateChanges;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Scheduling;

public sealed class DateChangeDecisionTests
{
    private readonly IDateChangeRequestRepository _dateChanges = Substitute.For<IDateChangeRequestRepository>();
    private readonly IJobRepository _jobs = Substitute.For<IJobRepository>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IRefundRequestRepository _refunds = Substitute.For<IRefundRequestRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Customer_approval_is_the_only_step_that_moves_the_committed_date()
    {
        var state = Arrange();
        var originalOrderDate = state.Order.PromisedDeliveryDate;
        var originalJobDate = state.Job.InternalDueDate;

        var result = await Handler().Handle(
            new DecideDateChangeCommand(state.Request.Token, state.CustomerId, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        state.Request.Status.Should().Be(DateChangeRequestStatus.Approved);
        state.Request.DecidedByCustomerId.Should().Be(state.CustomerId);
        state.Order.PromisedDeliveryDate.Should().Be(state.Request.ProposedDeliveryDate).And.BeAfter(originalOrderDate);
        state.Job.InternalDueDate.Should().BeAfter(originalJobDate);
        await _refunds.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Customer_rejection_keeps_dates_and_creates_one_pending_refund_row()
    {
        var state = Arrange();
        var originalOrderDate = state.Order.PromisedDeliveryDate;
        var originalJobDate = state.Job.InternalDueDate;
        RefundRequest? queued = null;
        await _refunds.AddAsync(Arg.Do<RefundRequest>(x => queued = x), Arg.Any<CancellationToken>());

        var result = await Handler().Handle(
            new DecideDateChangeCommand(state.Request.Token, state.CustomerId, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        state.Request.Status.Should().Be(DateChangeRequestStatus.Rejected);
        state.Order.PromisedDeliveryDate.Should().Be(originalOrderDate);
        state.Job.InternalDueDate.Should().Be(originalJobDate);
        queued.Should().NotBeNull();
        queued!.Status.Should().Be("Pending");
        queued.DateChangeRequestId.Should().Be(state.Request.Id);
        queued.Amount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void B16_reason_is_a_closed_three_value_list()
    {
        Enum.GetValues<DateChangeReason>().Should().HaveCount(3)
            .And.BeEquivalentTo([
                DateChangeReason.CustomerRequested,
                DateChangeReason.ForceMajeure,
                DateChangeReason.ReprintDeadlineUnrecoverable]);
    }

    private DecideDateChangeCommandHandler Handler() =>
        new(_dateChanges, _jobs, _orders, _refunds, _clock, _unitOfWork);

    private (Guid CustomerId, Order Order, Job Job, DateChangeRequest Request) Arrange()
    {
        var customerId = Guid.NewGuid();
        var quote = Quote.CreatePending(customerId, TimeSpan.FromHours(48));
        quote.AddItem(Guid.NewGuid(), 2,
            PrintConfiguration.Create("PLA", "BLACK", .2m, 20, .3m),
            Money.Of(100_000m, "VND"), 120, 50m, 20_000m, 30_000m, 10m, 10m, 10m);
        var promised = new DateOnly(2026, 10, 20);
        quote.MarkReady(promised, "test");
        var order = Order.CreateFromQuote(quote,
            Address.Create("1 Test", "Ward", "District", "HCM", "700000"), "PG-TEST");
        order.ConfirmPayment("txn-test");
        var item = order.Items.Single();
        var job = Job.Create(item.Id, item.ModelId, SpecBuilder.Default(), 120, promised.AddDays(-2));
        var request = DateChangeRequest.Propose(
            job.Id, item.Id, promised, promised.AddDays(3), DateChangeReason.ForceMajeure,
            "Force majeure outage affecting all labs", "decision-token", new DateTime(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc));

        _dateChanges.GetByTokenAsync(request.Token, Arg.Any<CancellationToken>()).Returns(request);
        _orders.GetByItemIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(order);
        _jobs.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        _clock.UtcNow.Returns(new DateTime(2026, 10, 12, 9, 0, 0, DateTimeKind.Utc));
        return (customerId, order, job, request);
    }
}
