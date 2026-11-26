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
    public async Task Unified_job_table_shows_assigned_work_without_counting_it_as_earned_income()
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
        var filtered = (LabFinanceReportDto)(await Controller(db, owner).Report(scope: "all", paymentStatus: "Estimated") as OkObjectResult)!.Value!;
        filtered.Items.Should().HaveCount(1);
        filtered.Summary.TotalIncome.Should().Be(0m);
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
    public async Task Reprints_without_prior_earned_fees_pay_both_the_at_fault_and_other_producers()
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
        before.Summary.Deductions.Should().Be(0m);
        before.Summary.TotalIncome.Should().Be(140_000m);
        await new FinanceController(db).SyncPayables(default);
        var after = await Report(db, owner);
        after.Summary.TotalIncome.Should().Be(before.Summary.TotalIncome);
        (await db.Set<LabPayable>().ToListAsync()).Should().OnlyContain(x => x.LabId == lab.Id);
    }

    [Fact]
    public async Task Failed_original_and_failed_reprint_still_earn_the_original_fee_once_on_success()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var lab = Lab.Onboard("Lab A", "HCM", 1, owner); db.Add(lab);
        var original = await SeedJob(db, lab.Id, 2, 2, JobStatus.Failed);
        var failed = await Reprint(db, original, lab.Id, JobStatus.Failed);
        var success = await Reprint(db, failed, lab.Id, JobStatus.Completed);

        var before = await Report(db, owner);
        before.Summary.TotalIncome.Should().Be(140_000m);
        before.Items.Single().JobId.Should().Be(success.Id);
        before.Items.Single().DeductionAmount.Should().Be(0m);
        await new FinanceController(db).SyncPayables(default);
        await new FinanceController(db).SyncPayables(default);
        var payable = await db.Set<LabPayable>().SingleAsync();
        payable.JobId.Should().Be(success.Id);
        payable.NetAmount.Should().Be(140_000m);
    }

    [Theory]
    [InlineData("unsynced")]
    [InlineData("Eligible")]
    [InlineData("Approved")]
    [InlineData("Paid")]
    public async Task Already_earned_original_fee_is_not_added_again_by_a_lab_funded_reprint(string status)
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var lab = Lab.Onboard("Lab A", "HCM", 1, owner); db.Add(lab);
        var original = await SeedJob(db, lab.Id, 2, 2, JobStatus.Completed, Now.AddDays(-2));
        if (status != "unsynced")
        {
            var payable = LabPayable.Create(original.Id, Guid.NewGuid(), lab.Id, 140_000m, 0m, "VND", Now.AddDays(-2));
            if (status is "Approved" or "Paid") payable.Approve(owner, Now);
            if (status == "Paid") payable.MarkPaid(owner, "ORIGINAL-FEE", Now);
            db.Add(payable); await db.SaveChangesAsync();
        }
        var reprint = await Reprint(db, original, lab.Id, JobStatus.Completed);
        var before = await Report(db, owner);
        before.Summary.TotalIncome.Should().Be(140_000m);
        before.Items.Single(x => x.JobId == reprint.Id).NetAmount.Should().Be(0m);
        // Historical earnings outside the date filter must still block an extra payment.
        var today = await Report(db, owner, DateOnly.FromDateTime(Now), DateOnly.FromDateTime(Now));
        today.Summary.TotalIncome.Should().Be(0m);
        await new FinanceController(db).SyncPayables(default);
        var after = await Report(db, owner);
        after.Summary.TotalIncome.Should().Be(before.Summary.TotalIncome);
        after.Summary.TotalReceived.Should().Be(status == "Paid" ? 140_000m : 0m);
        (await db.Set<LabPayable>().ToListAsync()).Sum(x => x.NetAmount).Should().Be(140_000m);
    }

    [Fact]
    public async Task Split_reprint_children_share_the_first_success_fee_without_consuming_another_branchs_earnings()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var lab = Lab.Onboard("Lab A", "HCM", 1, owner); db.Add(lab);
        var root = await SeedJob(db, lab.Id, 6, 6, JobStatus.Pending);
        var branches = root.Split([2, 4]).Value;
        foreach (var child in branches)
        {
            child.AssignTo(lab.Id, Guid.NewGuid(), Now, Now.AddHours(1), 1m);
            db.Add(child);
        }
        db.Entry(branches[0]).Property(x => x.Status).CurrentValue = JobStatus.Completed;
        db.Entry(branches[0]).Property(x => x.CompletedAtUtc).CurrentValue = Now;
        db.Entry(branches[1]).Property(x => x.Status).CurrentValue = JobStatus.Failed;
        await db.SaveChangesAsync();
        var replacement = await Reprint(db, branches[1], lab.Id, JobStatus.Pending);
        var children = replacement.Split([1, 3]).Value;
        foreach (var child in children)
        {
            child.AssignTo(lab.Id, Guid.NewGuid(), Now, Now.AddHours(1), 1m);
            db.Add(child);
            db.Entry(child).Property(x => x.Status).CurrentValue = JobStatus.Completed;
            db.Entry(child).Property(x => x.CompletedAtUtc).CurrentValue = Now;
        }
        await db.SaveChangesAsync();
        var report = await Report(db, owner);
        report.Summary.CompletedProducts.Should().Be(6);
        report.Summary.TotalIncome.Should().Be(420_000m);
        await new FinanceController(db).SyncPayables(default);
        (await db.Set<LabPayable>().ToListAsync()).Sum(x => x.NetAmount).Should().Be(420_000m);
    }

    [Fact]
    public async Task Split_replacements_of_an_already_earned_attempt_do_not_duplicate_the_fee()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var lab = Lab.Onboard("Lab A", "HCM", 1, owner); db.Add(lab);
        var original = await SeedJob(db, lab.Id, 4, 4, JobStatus.Completed);
        var replacement = await Reprint(db, original, lab.Id, JobStatus.Pending);
        foreach (var child in replacement.Split([1, 3]).Value)
        {
            child.AssignTo(lab.Id, Guid.NewGuid(), Now, Now.AddHours(1), 1m);
            db.Add(child);
            db.Entry(child).Property(x => x.Status).CurrentValue = JobStatus.Completed;
            db.Entry(child).Property(x => x.CompletedAtUtc).CurrentValue = Now;
        }
        await db.SaveChangesAsync();
        var report = await Report(db, owner);
        report.Summary.TotalIncome.Should().Be(280_000m);
        report.Items.Where(x => x.JobId != original.Id).Should().OnlyContain(x => x.NetAmount == 0m);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sync_repairs_old_unpaid_zero_reprint_fees_and_requires_approval_again(bool approved)
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var lab = Lab.Onboard("Lab A", "HCM", 1, owner); db.Add(lab);
        var original = await SeedJob(db, lab.Id, 2, 2, JobStatus.Failed);
        var reprint = await Reprint(db, original, lab.Id, JobStatus.Completed);
        var old = LabPayable.Create(reprint.Id, Guid.NewGuid(), lab.Id, 140_000m, 140_000m, "VND", Now);
        if (approved) old.Approve(owner, Now);
        db.Add(old); await db.SaveChangesAsync();
        var before = await Report(db, owner);
        before.Summary.TotalIncome.Should().Be(140_000m);
        before.Items.Single().PaymentStatus.Should().Be("PendingSync");
        await new FinanceController(db).SyncPayables(default);
        old.NetAmount.Should().Be(140_000m);
        old.Status.Should().Be("Eligible");
        old.ApprovedBy.Should().BeNull();
        old.ApprovedAtUtc.Should().BeNull();
        (await db.Set<LabPayable>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task A_stale_zero_fee_cannot_be_paid_without_reviewing_the_corrected_amount()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var lab = Lab.Onboard("Lab A", "HCM", 1, owner); db.Add(lab);
        var original = await SeedJob(db, lab.Id, 2, 2, JobStatus.Failed);
        var reprint = await Reprint(db, original, lab.Id, JobStatus.Completed);
        var payable = LabPayable.Create(reprint.Id, Guid.NewGuid(), lab.Id, 140_000m, 140_000m, "VND", Now);
        payable.Approve(owner, Now);
        db.Add(payable); await db.SaveChangesAsync();
        var controller = new FinanceController(db) { ControllerContext = Controller(db, owner).ControllerContext };
        (await controller.Pay(payable.Id, new PayLabRequest("DO-NOT-PAY-STALE-AMOUNT"), default))
            .Should().BeOfType<ConflictObjectResult>();
        payable.Status.Should().Be("Eligible");
        payable.NetAmount.Should().Be(140_000m);
        payable.PaymentReference.Should().BeNull();
        (await controller.Approve(payable.Id, default)).Should().BeOfType<OkObjectResult>();
        (await controller.Pay(payable.Id, new PayLabRequest("CORRECTED-FEE"), default)).Should().BeOfType<OkObjectResult>();
        (await Report(db, owner)).Summary.TotalReceived.Should().Be(140_000m);
    }

    [Fact]
    public async Task System_funded_reprints_keep_their_separate_fee_and_paid_history_is_immutable()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var lab = Lab.Onboard("Lab A", "HCM", 1, owner); db.Add(lab);
        var original = await SeedJob(db, lab.Id, 1, 1, JobStatus.Completed);
        var paid = LabPayable.Create(original.Id, Guid.NewGuid(), lab.Id, 70_000m, 10_000m, "VND", Now);
        paid.Approve(owner, Now); paid.MarkPaid(owner, "FROZEN-RECEIPT", Now);
        db.Add(paid); await db.SaveChangesAsync();
        await Reprint(db, original, lab.Id, JobStatus.Completed, CostBearer.System);
        await new FinanceController(db).SyncPayables(default);
        var report = await Report(db, owner);
        report.Summary.TotalIncome.Should().Be(130_000m);
        report.Summary.TotalReceived.Should().Be(60_000m);
        paid.DeductionAmount.Should().Be(10_000m);
        paid.PaymentReference.Should().Be("FROZEN-RECEIPT");
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

    private static async Task<Job> Reprint(PrintGridDbContext db, Job original, Guid labId,
        JobStatus status, CostBearer costBearer = CostBearer.Lab)
    {
        var reprint = Job.CreateUrgentReprint(original, original.ReprintIndex + 1,
            faultLabId: labId, costBearer: costBearer, createdAtUtc: Now);
        reprint.AssignTo(labId, Guid.NewGuid(), Now, Now.AddHours(1), 1m);
        db.Add(reprint);
        db.Entry(reprint).Property(x => x.Status).CurrentValue = status;
        if (status == JobStatus.Completed) db.Entry(reprint).Property(x => x.CompletedAtUtc).CurrentValue = Now;
        await db.SaveChangesAsync();
        return reprint;
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
