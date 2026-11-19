using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.OrderWorkflowTests;

public sealed class QuoteSubmissionTests
{
    private static readonly DateTime Now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Calculating_a_draft_does_not_put_it_in_the_ops_queue()
    {
        await using var db = Database();
        var draft = Draft(Guid.NewGuid(), Guid.NewGuid());
        db.Add(draft); await db.SaveChangesAsync();
        var repository = new QuoteRepository(db);
        (await repository.GetDraftsAsync()).Should().BeEmpty();
        draft.Status.Should().Be(QuoteStatus.Draft);
        draft.ExpiresAt.Should().BeNull();
    }

    [Theory]
    [InlineData(6)]
    [InlineData(1)]
    public async Task Explicit_submission_creates_one_pending_order_and_one_ops_queue_entry(int quantity)
    {
        await using var fixture = new Fixture(quantity);
        await fixture.Seed();
        var result = await fixture.Place.Handle(fixture.Command, default);
        result.IsSuccess.Should().BeTrue(result.Error.Message);
        result.Value.Status.Should().Be("AwaitingApproval");
        fixture.Quote.Status.Should().Be(QuoteStatus.PendingReview);
        var order = await fixture.Orders.GetByQuoteIdAsync(fixture.Quote.Id);
        order!.Status.Should().Be(OrderStatus.AwaitingApproval);
        order.PaidAmount.Should().Be(0);
        order.ApplyPayment(100).IsFailure.Should().BeTrue();
        (await fixture.Quotes.GetDraftsAsync()).Should().ContainSingle(q => q.Id == fixture.Quote.Id);
        var duplicate = await fixture.Place.Handle(fixture.Command, default);
        duplicate.IsFailure.Should().BeTrue();
        (await fixture.Db.Set<Order>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Invalid_delivery_does_not_submit_a_draft_or_create_an_order()
    {
        await using var fixture = new Fixture(6);
        await fixture.Seed();
        var result = await fixture.Place.Handle(fixture.Command with { RequestedDeliveryDate = DateOnly.FromDateTime(Now).AddDays(1) }, default);
        result.IsFailure.Should().BeTrue();
        fixture.Quote.Status.Should().Be(QuoteStatus.Draft);
        (await fixture.Quotes.GetDraftsAsync()).Should().BeEmpty();
        (await fixture.Db.Set<Order>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Ops_cannot_approve_an_unsubmitted_calculation()
    {
        await using var fixture = new Fixture(6);
        await fixture.Seed();
        var result = await fixture.Review.Handle(new ReviewQuoteCommand(fixture.Quote.Id, Guid.NewGuid(), fixture.Quote.TotalPrice.Amount,
            fixture.Quote.PromisedDeliveryDate, null), default);
        result.IsFailure.Should().BeTrue();
        fixture.Quote.Status.Should().Be(QuoteStatus.Draft);
    }

    [Fact]
    public async Task Approving_a_submitted_quote_preserves_the_customer_delivery_date_and_activates_payment()
    {
        await using var fixture = new Fixture(6);
        await fixture.Seed();
        (await fixture.Place.Handle(fixture.Command, default)).IsSuccess.Should().BeTrue();
        var result = await fixture.Review.Handle(new ReviewQuoteCommand(fixture.Quote.Id, Guid.NewGuid(), fixture.Quote.TotalPrice.Amount,
            fixture.Quote.PromisedDeliveryDate, null), default);
        result.IsSuccess.Should().BeTrue(result.Error.Message);
        var order = await fixture.Orders.GetByQuoteIdAsync(fixture.Quote.Id);
        order!.Status.Should().Be(OrderStatus.PaymentPending);
        order.RequestedDeliveryDate.Should().Be(fixture.Command.RequestedDeliveryDate);
        order.PromisedDeliveryDate.Should().Be(fixture.Command.RequestedDeliveryDate);
        order.PaidAmount.Should().Be(0);
        fixture.Quote.Status.Should().Be(QuoteStatus.Converted);
        (await fixture.Quotes.GetDraftsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task History_is_scoped_to_the_customer_and_model_and_paginates_filtered_results()
    {
        await using var db = Database();
        var customer = Guid.NewGuid(); var model = Guid.NewGuid();
        for (var i = 1; i <= 12; i++)
        {
            var quote = Draft(customer, model, price: i * 100);
            if (i <= 3) quote.SubmitForReview();
            db.Add(quote);
        }
        db.Add(Draft(Guid.NewGuid(), model));
        db.Add(Draft(customer, Guid.NewGuid()));
        await db.SaveChangesAsync();
        var repo = new QuoteRepository(db);
        var all = new QuoteHistoryFilter(customer, model, null, null, null, null, "price-low", 2, 5, Now);
        var page = await repo.GetHistoryAsync(all);
        page.TotalCount.Should().Be(12); page.Items.Should().HaveCount(5);
        page.Items.Select(q => q.TotalPrice.Amount).Should().Equal(600, 700, 800, 900, 1000);
        page.DraftQuotes.Should().Be(9); page.PendingReviewQuotes.Should().Be(3);
        var pending = await repo.GetHistoryAsync(all with { Status = QuoteStatus.PendingReview, Page = 1 });
        pending.TotalCount.Should().Be(3);
        pending.Items.Should().OnlyContain(q => q.Status == QuoteStatus.PendingReview);
        var search = await repo.GetHistoryAsync(all with { Search = "PLA", Page = 1, PageSize = 20 });
        search.Items.Should().HaveCount(12);
        var unrelated = await repo.GetHistoryAsync(all with { Search = "ABS", Page = 1 });
        unrelated.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task History_expiry_filter_matches_ready_quotes_whose_validity_has_ended()
    {
        await using var db = Database();
        var quote = Draft(Guid.NewGuid(), Guid.NewGuid());
        quote.Approve(null, quote.TotalPrice.Amount, quote.PromisedDeliveryDate, null, Now.AddDays(-3), TimeSpan.FromHours(1), true, 0);
        db.Add(quote); await db.SaveChangesAsync();
        var filter = new QuoteHistoryFilter(quote.CustomerId, quote.Items.Single().ModelId, QuoteStatus.Expired, null, null, null, "newest", 1, 5, Now);
        var result = await new GetQuoteHistoryQueryHandler(new QuoteRepository(db), Clock()).Handle(new GetQuoteHistoryQuery(
            filter.CustomerId, filter.ModelId, QuoteStatus.Expired, null, null, null), default);
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1); result.Value.Items.Single().Status.Should().Be("Expired");
        result.Value.ReadyQuotes.Should().Be(0);
    }

    [Fact]
    public async Task History_rejects_invalid_dates_pages_and_sort_options()
    {
        var handler = new GetQuoteHistoryQueryHandler(Substitute.For<IQuoteRepository>(), Clock());
        var request = new GetQuoteHistoryQuery(Guid.NewGuid(), Guid.NewGuid(), null, null, null, null);
        (await handler.Handle(request with { Page = 0 }, default)).IsFailure.Should().BeTrue();
        (await handler.Handle(request with { From = new(2026, 11, 20), To = new(2026, 11, 19) }, default)).IsFailure.Should().BeTrue();
        (await handler.Handle(request with { Sort = "unsupported" }, default)).IsFailure.Should().BeTrue();
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public PrintGridDbContext Db { get; } = Database();
        public Quote Quote { get; }
        public QuoteRepository Quotes { get; }
        public OrderRepository Orders { get; }
        public PlaceOrderCommand Command { get; }
        public PlaceOrderCommandHandler Place { get; }
        public ReviewQuoteCommandHandler Review { get; }
        public Fixture(int quantity)
        {
            var owner = Guid.NewGuid(); var model = Guid.NewGuid();
            Quote = Draft(owner, model, quantity: quantity);
            Quotes = new QuoteRepository(Db); Orders = new OrderRepository(Db);
            var models = Substitute.For<IModelRepository>();
            var approved = Model.Create(owner, "Part", null, "part.stl", "STL", 100, null, "models/part.stl", "hash");
            approved.Approve(null, Now);
            models.GetByIdAsync(model, Arg.Any<CancellationToken>()).Returns(approved);
            var capacity = Substitute.For<IProductionCapacityProbe>();
            capacity.FindEarliestFeasibleDeliveryAsync(Arg.Any<IReadOnlyList<TrialPlacementLine>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(new TrialPlacementResult(DateOnly.FromDateTime(Now).AddDays(5), "test"));
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var delivery = new OrderDeliveryService(capacity, Clock(), configuration);
            var uow = new UnitOfWork(Db);
            Place = new PlaceOrderCommandHandler(Quotes, Orders, models, uow, Clock(), configuration, delivery);
            Review = new ReviewQuoteCommandHandler(Quotes, Orders, uow, Clock(), configuration, delivery);
            Command = new PlaceOrderCommand(owner, Quote.Id, "456 Quang Trung", "2", "Go Vap", "HCM", "700000", true,
                RequestedDeliveryDate: DateOnly.FromDateTime(Now).AddDays(9));
        }
        public async Task Seed() { Db.Add(Quote); await Db.SaveChangesAsync(); }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private static Quote Draft(Guid owner, Guid model, decimal price = 1000, int quantity = 1)
    {
        var quote = Quote.CreatePending(owner, TimeSpan.FromHours(48));
        quote.AddItem(model, quantity, PrintConfiguration.Create("PLA", "BLACK", .2m, 20, .3m), Money.Of(price), 30, 10, 500, 500, 20, 20, 20);
        quote.MarkDraft(DateOnly.FromDateTime(Now).AddDays(5), "test"); return quote;
    }
    private static IDateTimeProvider Clock()
    {
        var clock = Substitute.For<IDateTimeProvider>(); clock.UtcNow.Returns(Now); clock.Today.Returns(DateOnly.FromDateTime(Now)); return clock;
    }
    private static PrintGridDbContext Database()
    {
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Customer.Infrastructure.CustomerModule).Assembly);
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Scheduling.Infrastructure.SchedulingModule).Assembly);
        return new PrintGridDbContext(new DbContextOptionsBuilder<PrintGridDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }
}
