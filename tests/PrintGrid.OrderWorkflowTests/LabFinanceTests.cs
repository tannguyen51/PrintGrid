using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Api.Authorization;
using PrintGrid.Api.Controllers;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Analytics.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.OrderWorkflowTests;

public sealed class LabFinanceTests
{
    private static readonly DateTime Now = new(2026, 11, 25, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Completed_job_income_is_visible_before_sync_and_payment_does_not_double_it()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var lab = Lab.Onboard("Lab A", "HCM", 1, owner);
        db.Add(lab);
        var job = await SeedJob(db, lab.Id, 2, 2, JobStatus.Completed);
        var before = await Report(db, owner);
        before.Summary.CompletedJobs.Should().Be(1);
        before.Summary.CompletedProducts.Should().Be(2);
        before.Summary.TotalIncome.Should().Be(140_000m);
        before.Summary.Outstanding.Should().Be(140_000m);
        before.Summary.PendingSyncJobs.Should().Be(1);
        before.Items.Single().PaymentStatus.Should().Be("PendingSync");

        var finance = new FinanceController(db);
        await finance.SyncPayables(default);
        await finance.SyncPayables(default);
        (await db.Set<LabPayable>().CountAsync()).Should().Be(1);
        var payable = await db.Set<LabPayable>().SingleAsync();
        payable.JobId.Should().Be(job.Id);
        payable.Approve(Guid.NewGuid(), Now).IsSuccess.Should().BeTrue();
        payable.MarkPaid(Guid.NewGuid(), "BANK-PAID-001", Now).IsSuccess.Should().BeTrue();
        await db.SaveChangesAsync();

        var after = await Report(db, owner);
        after.Summary.TotalIncome.Should().Be(before.Summary.TotalIncome);
        after.Summary.TotalReceived.Should().Be(140_000m);
        after.Summary.Outstanding.Should().Be(0m);
        after.Summary.CollectionRate.Should().Be(100m);
        after.Summary.PendingSyncJobs.Should().Be(0);
        after.Items.Single().PaymentReference.Should().Be("BANK-PAID-001");
    }

    [Fact]
    public async Task Split_jobs_count_only_their_quantities_and_do_not_pay_cancelled_parents_or_failed_attempts()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var lab = Lab.Onboard("Split Lab", "HCM", 1, owner);
        db.Add(lab);
        var parent = await SeedJob(db, lab.Id, 6, 6, JobStatus.Pending);
        var split = parent.Split([2, 4]);
        split.IsSuccess.Should().BeTrue();
        foreach (var child in split.Value)
        {
            child.AssignTo(lab.Id, Guid.NewGuid(), Now, Now.AddHours(1), 1m);
            db.Add(child);
            db.Entry(child).Property(x => x.Status).CurrentValue = JobStatus.Completed;
            db.Entry(child).Property(x => x.CompletedAtUtc).CurrentValue = Now;
        }
        await db.SaveChangesAsync();
        await SeedJob(db, lab.Id, 6, 6, JobStatus.Failed);
        var report = await Report(db, owner);
        report.Summary.CompletedJobs.Should().Be(2);
        report.Summary.CompletedProducts.Should().Be(6);
        report.Summary.ProductionValue.Should().Be(600_000m);
        report.Summary.TotalIncome.Should().Be(420_000m);
        report.Summary.HubAmount.Should().Be(180_000m);
    }

    [Fact]
    public async Task Price_book_shows_assigned_work_without_counting_it_as_earned_income()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var lab = Lab.Onboard("Lab A", "HCM", 1, owner);
        db.Add(lab);
        await SeedJob(db, lab.Id, 3, 1, JobStatus.InProgress);
        var completed = await Report(db, owner);
        completed.Summary.TotalIncome.Should().Be(0m);
        completed.Items.Should().BeEmpty();
        var prices = await Report(db, owner, scope: "all");
        prices.Summary.TotalIncome.Should().Be(0m);
        prices.Items.Single().NetAmount.Should().Be(70_000m);
        prices.Items.Single().PaymentStatus.Should().Be("Estimated");
    }

    [Fact]
    public async Task A_lab_owner_cannot_read_another_lab_or_an_unclaimed_lab()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var mine = Lab.Onboard("Mine", "HCM", 1, owner);
        var other = Lab.Onboard("Other", "HCM", 1, Guid.NewGuid());
        var unclaimed = Lab.Onboard("Unclaimed", "HCM", 1);
        db.AddRange(mine, other, unclaimed);
        await SeedJob(db, mine.Id, 1, 1, JobStatus.Completed);
        await SeedJob(db, other.Id, 10, 10, JobStatus.Completed);
        await SeedJob(db, unclaimed.Id, 10, 10, JobStatus.Completed);
        var report = await Report(db, owner);
        report.LabId.Should().Be(mine.Id);
        report.Summary.TotalIncome.Should().Be(70_000m);
        (await Controller(db, Guid.NewGuid()).Report()).Should().BeOfType<NotFoundObjectResult>();
        (await new LabFinanceController(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } }
            .Report()).Should().BeOfType<ForbidResult>();
        var auth = typeof(LabFinanceController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single();
        auth.Roles.Should().Be(Roles.LabManager);
    }

    [Fact]
    public async Task Date_range_uses_full_Vietnam_days_and_payment_dates_do_not_change_the_cohort()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var lab = Lab.Onboard("Lab A", "HCM", 1, owner);
        db.Add(lab);
        await SeedJob(db, lab.Id, 1, 1, JobStatus.Completed, new DateTime(2026, 11, 24, 16, 59, 59, DateTimeKind.Utc));
        var included = await SeedJob(db, lab.Id, 1, 1, JobStatus.Completed, new DateTime(2026, 11, 24, 17, 0, 0, DateTimeKind.Utc));
        await SeedJob(db, lab.Id, 1, 1, JobStatus.Completed, new DateTime(2026, 11, 25, 16, 59, 59, DateTimeKind.Utc));
        await SeedJob(db, lab.Id, 1, 1, JobStatus.Completed, new DateTime(2026, 11, 25, 17, 0, 0, DateTimeKind.Utc));
        var payable = LabPayable.Create(included.Id, Guid.NewGuid(), lab.Id, 70_000m, 0m, "VND", Now);
        payable.Approve(Guid.NewGuid(), Now);
        payable.MarkPaid(Guid.NewGuid(), "LATER-PAYMENT", Now.AddMonths(1));
        db.Add(payable); await db.SaveChangesAsync();
        var report = await Report(db, owner, new DateOnly(2026, 11, 25), new DateOnly(2026, 11, 25));
        report.Summary.CompletedJobs.Should().Be(2);
        report.Summary.TotalReceived.Should().Be(70_000m);
        report.Summary.TotalIncome.Should().Be(140_000m);
        report.Summary.CollectionRate.Should().Be(50m);
    }

    [Fact]
    public async Task Reprint_deductions_apply_to_the_at_fault_producer_and_another_producer_gets_paid()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var lab = Lab.Onboard("Producer", "HCM", 1, owner);
        db.Add(lab);
        var own = await SeedJob(db, lab.Id, 1, 1, JobStatus.Completed);
        db.Entry(own).Property(x => x.ReprintIndex).CurrentValue = 1;
        db.Entry(own).Property(x => x.CostBearer).CurrentValue = CostBearer.Lab;
        db.Entry(own).Property(x => x.FaultLabId).CurrentValue = lab.Id;
        var other = await SeedJob(db, lab.Id, 1, 1, JobStatus.Completed);
        db.Entry(other).Property(x => x.ReprintIndex).CurrentValue = 1;
        db.Entry(other).Property(x => x.CostBearer).CurrentValue = CostBearer.Lab;
        db.Entry(other).Property(x => x.FaultLabId).CurrentValue = Guid.NewGuid();
        await db.SaveChangesAsync();
        var before = await Report(db, owner);
        before.Summary.Deductions.Should().Be(70_000m);
        before.Summary.TotalIncome.Should().Be(70_000m);
        await new FinanceController(db).SyncPayables(default);
        var after = await Report(db, owner);
        after.Summary.TotalIncome.Should().Be(before.Summary.TotalIncome);
        (await db.Set<LabPayable>().ToListAsync()).Should().OnlyContain(x => x.LabId == lab.Id);
    }

    [Fact]
    public async Task Table_filters_and_pagination_do_not_change_summary_totals()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var lab = Lab.Onboard("Lab A", "HCM", 1, owner);
        db.Add(lab);
        await SeedJob(db, lab.Id, 1, 1, JobStatus.Completed);
        await SeedJob(db, lab.Id, 2, 2, JobStatus.Completed);
        var report = (LabFinanceReportDto)(await Controller(db, owner).Report(
            search: "PLA", paymentStatus: "PendingSync", pageSize: 1) as OkObjectResult)!.Value!;
        report.TotalCount.Should().Be(2);
        report.Items.Should().HaveCount(1);
        report.Summary.TotalIncome.Should().Be(210_000m);
        var empty = (LabFinanceReportDto)(await Controller(db, owner).Report(search: "not-a-job") as OkObjectResult)!.Value!;
        empty.TotalCount.Should().Be(0);
        empty.Summary.TotalIncome.Should().Be(210_000m);
    }

    [Fact]
    public async Task Frozen_batch_price_takes_precedence_over_rounded_per_unit_prices()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var lab = Lab.Onboard("Lab A", "HCM", 1, owner);
        db.Add(lab);
        await SeedJob(db, lab.Id, 5, 2, JobStatus.Completed, batchTotal: 151_000m);
        var report = await Report(db, owner);
        report.Items.Single().JobValue.Should().Be(60_400m);
        report.Summary.TotalIncome.Should().Be(42_280m);
        await new FinanceController(db).SyncPayables(default);
        (await db.Set<LabPayable>().SingleAsync()).GrossAmount.Should().Be(report.Summary.TotalIncome);
    }

    [Fact]
    public async Task Invalid_filters_are_rejected()
    {
        await using var db = Database();
        var controller = Controller(db, Guid.NewGuid());
        (await controller.Report(page: 0)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.Report(pageSize: 101)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.Report(scope: "other")).Should().BeOfType<BadRequestObjectResult>();
        (await controller.Report(paymentStatus: "other")).Should().BeOfType<BadRequestObjectResult>();
        (await controller.Report(from: new DateOnly(2026, 11, 26), to: new DateOnly(2026, 11, 25)))
            .Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Split_shares_use_the_frozen_batch_total_and_preserve_the_total_after_rounding()
    {
        var split = LabRevenueShare.Calculate(151_000m, 5, 2);
        split.JobValue.Should().Be(60_400m);
        split.LabAmount.Should().Be(42_280m);
        split.HubAmount.Should().Be(18_120m);
        var rounded = LabRevenueShare.Calculate(100m, 3, 1);
        (rounded.LabAmount + rounded.HubAmount).Should().Be(rounded.JobValue);
    }

    private static LabFinanceController Controller(PrintGridDbContext db, Guid owner) => new(db)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner.ToString()),
            new Claim(ClaimTypes.Role, Roles.LabManager)], "test")) } }
    };
    private static async Task<LabFinanceReportDto> Report(PrintGridDbContext db, Guid owner,
        DateOnly? from = null, DateOnly? to = null, string scope = "completed") =>
        (LabFinanceReportDto)(await Controller(db, owner).Report(from, to, scope) as OkObjectResult)!.Value!;

    private static async Task<Job> SeedJob(PrintGridDbContext db, Guid labId, int lineQuantity,
        int jobQuantity, JobStatus status, DateTime? completedAt = null, decimal? batchTotal = null)
    {
        var quote = Quote.CreatePending(Guid.NewGuid(), TimeSpan.FromHours(48));
        quote.AddItem(Guid.NewGuid(), lineQuantity, PrintConfiguration.Create("PLA", "BLACK", .2m, 20, .3m),
            Money.Of(100_000m), 30, 10, 50_000, 50_000, 20, 20, 20);
        if (batchTotal.HasValue) quote.Items.Single().FreezeCalculation("{}", batchTotal.Value);
        quote.MarkReady(new DateOnly(2026, 12, 1), "test");
        var order = Order.CreateFromQuote(quote, Address.Create("A", "B", "C", "HCM", ""), $"PG-{Guid.NewGuid():N}"[..20]);
        var job = Job.Create(order.Items.Single().Id, Guid.NewGuid(),
            JobSpecification.Create(BuildVolume.Create(20, 20, 20), "PLA", "BLACK", .2m, .3m, PrintTechnology.Fdm, 10m),
            30, new DateOnly(2026, 12, 1), jobQuantity);
        job.AssignTo(labId, Guid.NewGuid(), Now, Now.AddHours(1), 1m);
        db.AddRange(order, job);
        db.Entry(job).Property(x => x.Status).CurrentValue = status;
        if (status == JobStatus.Completed) db.Entry(job).Property(x => x.CompletedAtUtc).CurrentValue = completedAt ?? Now;
        await db.SaveChangesAsync(); return job;
    }
    private static PrintGridDbContext Database()
    {
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Customer.Infrastructure.CustomerModule).Assembly);
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Scheduling.Infrastructure.SchedulingModule).Assembly);
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Analytics.Infrastructure.AnalyticsModule).Assembly);
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Admin.Infrastructure.AdminModule).Assembly);
        return new PrintGridDbContext(new DbContextOptionsBuilder<PrintGridDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }
}
