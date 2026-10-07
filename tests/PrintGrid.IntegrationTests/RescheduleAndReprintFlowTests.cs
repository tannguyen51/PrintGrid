using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.Commands.AssignJob;
using PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;
using PrintGrid.Modules.Scheduling.Application.Commands.UrgentReprint;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Events;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using Testcontainers.PostgreSql;

namespace PrintGrid.IntegrationTests;

/// <summary>
/// End-to-end verification against a real PostgreSQL (real EXCLUDE constraint, real
/// append-only trigger, real transaction semantics) for the S8 rows:
/// FR-SCHED-007 automatic rescheduling, FR-HUB-003 urgent reprint, FR-SCHED-009 decision log.
/// </summary>
public class RescheduleAndReprintFlowTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            // UseSetting (not just ConfigureAppConfiguration) so the value is in
            // builder.Configuration by the time Program.cs reads it for Hangfire at startup.
            builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString()
                }));
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrintGridDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<T> InScope<T>(Func<IServiceProvider, Task<T>> work)
    {
        using var scope = _factory.Services.CreateScope();
        return await work(scope.ServiceProvider);
    }

    /// <summary>
    /// Two labs with one capable machine each (so a replan has somewhere else to go), and a
    /// job waiting for a placement.
    /// </summary>
    private async Task<(Guid JobId, Guid MachineA, Guid MachineB)> SeedJobWithTwoMachines(int dueInDays = 5)
    {
        return await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();

            var labA = Lab.Onboard("Integration Lab A", "HCM", transitDaysToHub: 1);
            var machineA = Machine.Register(
                labA.Id, "M-A", "Prusa MK4", PrintTechnology.Fdm,
                BuildVolume.Create(250, 210, 220), 0.1m, 0.2m, new[] { "PLA", "PETG" });
            labA.AddMachine(machineA);

            var labB = Lab.Onboard("Integration Lab B", "HCM", transitDaysToHub: 1);
            var machineB = Machine.Register(
                labB.Id, "M-B", "Prusa MK4", PrintTechnology.Fdm,
                BuildVolume.Create(250, 210, 220), 0.1m, 0.2m, new[] { "PLA", "PETG" });
            labB.AddMachine(machineB);

            var job = Job.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                JobSpecification.Create(
                    BuildVolume.Create(80, 80, 80), "PLA", "BLACK", 0.2m, 0.3m, PrintTechnology.Fdm, 45m),
                estimatedPrintMinutes: 60,
                internalDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(dueInDays)));

            db.Add(labA);
            db.Add(labB);
            db.Add(job);
            await db.SaveChangesAsync();

            return (job.Id, machineA.Id, machineB.Id);
        });
    }

    [Fact]
    public async Task A_declined_job_is_replanned_automatically_and_the_decision_is_logged()
    {
        var (jobId, _, _) = await SeedJobWithTwoMachines();

        await InScope(sp => sp.GetRequiredService<ISender>().Send(new AssignJobCommand(jobId)));

        var (firstPlacement, declinedLabId) = await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();
            var job = await db.Set<Job>().FirstAsync(j => j.Id == jobId);
            job.Status.Should().Be(JobStatus.Assigned);
            var machine = await db.Set<Machine>().FirstAsync(m => m.Id == job.MachineId);
            return (job.MachineId!.Value, machine.LabId);
        });

        // The lab rejects the job — FR-SCHED-007 must repair the plan without a human.
        var declined = await InScope(sp => sp.GetRequiredService<ISender>().Send(new DeclineJobCommand(jobId, "Hết vật liệu")));
        declined.IsSuccess.Should().BeTrue();

        var afterDecline = await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();
            return await db.Set<Job>().FirstAsync(j => j.Id == jobId);
        });

        afterDecline.Status.Should().Be(JobStatus.Assigned,
            "the rescheduler runs on the decline event, inside the same request");
        afterDecline.MachineId.Should().NotBeNull();
        afterDecline.MachineId.Should().NotBe(firstPlacement,
            "BR-RESCHED-001: the lab that declined must not be offered the same job straight back");

        var decisions = await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();
            return await db.Set<AssignmentDecision>()
                .Where(d => d.JobId == jobId)
                .OrderBy(d => d.CreatedAtUtc)
                .ToListAsync();
        });

        decisions.Should().HaveCount(2, "one entry per assignment decision (initial + replan)");

        var replan = decisions[^1];
        replan.Trigger.Should().Be("lab_decline");
        replan.Outcome.Should().Be(AssignmentOutcomes.Assigned);
        replan.ChosenMachineId.Should().Be(afterDecline.MachineId);
        replan.ScoringConfigVersion.Should().Be("2026.10");
        replan.TimeBudgetMs.Should().Be(30_000);
        replan.ElapsedMs.Should().BeLessThan(30_000, "NFR-PERF-002: repair completes well inside the hard budget");
        replan.CandidatesJson.Should().Contain("capable");
        replan.RankingJson.Should().Contain("due_date_slack");

        // DIAGNOSTIC turned regression test: the lab that declined is excluded from the replan,
        // and the exclusion is visible in the audit trail.
        replan.CandidatesJson.Should().Contain(declinedLabId.ToString(),
            "FR-SCHED-009: the trace must explain why the candidate set is smaller than capability allows");
    }

    [Fact]
    public async Task A_job_that_fails_hub_inspection_gets_an_urgent_reprint_that_keeps_the_deadline()
    {
        var (jobId, _, _) = await SeedJobWithTwoMachines();

        await InScope(async sp =>
        {
            var sender = sp.GetRequiredService<ISender>();
            await sender.Send(new AssignJobCommand(jobId));
            await sender.Send(new AcceptJobCommand(jobId));
            await sender.Send(new StartJobCommand(jobId));
            await sender.Send(new CompleteJobCommand(jobId, 58, "Layer adhesion OK", new[] { "lab-qc/photo-1.jpg" }));
            return 0;
        });

        var failedAtLabId = await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();
            return (await db.Set<Job>().FirstAsync(j => j.Id == jobId)).LabId!.Value;
        });

        var inspected = await InScope(sp =>
            sp.GetRequiredService<ISender>().Send(new InspectJobCommand(jobId, Passed: false, Note: "Layer shift")));
        inspected.IsSuccess.Should().BeTrue();

        var state = await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();
            var original = await db.Set<Job>().FirstAsync(j => j.Id == jobId);
            var reprints = await db.Set<Job>().Where(j => j.OriginalJobId == jobId).ToListAsync();
            return (original, reprints);
        });

        state.original.Status.Should().Be(JobStatus.Failed);

        state.reprints.Should().ContainSingle("FR-HUB-003 creates exactly one reprint job per failure");
        var reprint = state.reprints[0];

        reprint.Priority.Should().Be(JobPriority.Urgent);
        reprint.InternalDueDate.Should().Be(state.original.InternalDueDate,
            "BR-RESCHED-003: the reprint inherits the original deadline");
        reprint.CostBearer.Should().Be(CostBearer.Lab, "13-AC: the fault reprint is charged to the lab");
        reprint.FaultLabId.Should().Be(failedAtLabId);
        reprint.ReprintIndex.Should().Be(1);
        reprint.OrderItemId.Should().Be(state.original.OrderItemId);
        reprint.Status.Should().Be(JobStatus.Assigned, "the reprint is placed immediately, not queued");
        reprint.MachineId.Should().NotBeNull();
        reprint.LabId.Should().NotBe(failedAtLabId,
            "the lab that produced the defective part must not be handed the reprint");

        var reprintDecision = await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();
            return await db.Set<AssignmentDecision>()
                .Where(d => d.JobId == reprint.Id)
                .SingleAsync();
        });
        reprintDecision.Trigger.Should().Be(AssignmentTriggers.UrgentReprint);
        reprintDecision.Outcome.Should().Be(AssignmentOutcomes.Assigned);
    }

    [Fact]
    public async Task Work_that_is_already_printing_is_never_reshuffled()
    {
        var (jobId, _, _) = await SeedJobWithTwoMachines();

        var before = await InScope(async sp =>
        {
            var sender = sp.GetRequiredService<ISender>();
            await sender.Send(new AssignJobCommand(jobId));
            await sender.Send(new AcceptJobCommand(jobId));
            await sender.Send(new StartJobCommand(jobId));

            var db = sp.GetRequiredService<PrintGridDbContext>();
            return await db.Set<Job>().FirstAsync(j => j.Id == jobId);
        });

        await InScope(async sp =>
        {
            await sp.GetRequiredService<IPublisher>().Publish(
                new ReschedulingTriggeredEvent(jobId, "machine_breakdown", before.InternalDueDate));
            return 0;
        });

        var after = await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();
            return await db.Set<Job>().FirstAsync(j => j.Id == jobId);
        });

        after.Status.Should().Be(JobStatus.InProgress);
        after.MachineId.Should().Be(before.MachineId, "BR-SCHED-007 locks jobs that are printing");
        // timestamptz keeps microseconds, DateTime ticks are 100 ns — compare with tolerance.
        after.PlannedStartUtc.Should().BeCloseTo(before.PlannedStartUtc!.Value, TimeSpan.FromMilliseconds(1));

        var decisionCount = await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();
            return await db.Set<AssignmentDecision>().CountAsync(d => d.JobId == jobId);
        });
        decisionCount.Should().Be(1, "the skipped reschedule must not create a decision entry");
    }

    [Fact]
    public async Task The_decision_log_cannot_be_updated_or_deleted_even_by_sql()
    {
        var (jobId, _, _) = await SeedJobWithTwoMachines();
        await InScope(sp => sp.GetRequiredService<ISender>().Send(new AssignJobCommand(jobId)));

        var attempts = await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();
            var results = new List<string>();

            foreach (var sql in new[]
                     {
                         "UPDATE scheduling.assignment_decisions SET \"Outcome\" = 'tampered'",
                         "DELETE FROM scheduling.assignment_decisions"
                     })
            {
                try
                {
                    await db.Database.ExecuteSqlRawAsync(sql);
                    results.Add("allowed");
                }
                catch (PostgresException ex)
                {
                    results.Add(ex.MessageText);
                }
            }

            return results;
        });

        attempts.Should().OnlyContain(m => m.Contains("append-only"),
            "FR-SCHED-009 AC3 / NFR-LEGAL-002: the log is immutable in the database itself");
    }

    [Fact]
    public async Task The_third_reprint_goes_to_the_operations_queue_instead_of_being_created()
    {
        var (jobId, _, _) = await SeedJobWithTwoMachines();

        // Drive the chain to the last allowed reprint, then fail it.
        var lastAllowedReprintId = await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();
            var original = await db.Set<Job>().FirstAsync(j => j.Id == jobId);

            var first = Job.CreateUrgentReprint(original, 1);
            first.Fail("first reprint failed");
            var second = Job.CreateUrgentReprint(original, 2);
            second.Fail("second reprint failed");

            db.Add(first);
            db.Add(second);
            await db.SaveChangesAsync();
            return second.Id;
        });

        var result = await InScope(sp =>
            sp.GetRequiredService<ISender>().Send(new CreateUrgentReprintCommand(lastAllowedReprintId, "third failure")));

        result.IsSuccess.Should().BeTrue();
        result.Value!.EscalatedToOps.Should().BeTrue();
        result.Value.ReprintJobId.Should().BeNull();

        var state = await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();
            var escalations = await db.Set<OpsEscalation>().Where(e => e.JobId == lastAllowedReprintId).ToListAsync();
            var created = await db.Set<Job>().CountAsync(j => j.OriginalJobId == jobId);
            return (escalations, created);
        });

        state.escalations.Should().ContainSingle();
        state.escalations[0].Kind.Should().Be(OpsEscalationKind.ReprintLimitExceeded);
        state.escalations[0].Status.Should().Be(OpsEscalationStatus.Open);
        state.created.Should().Be(2, "no third reprint job may be created (BR-RESCHED-004, B14)");
    }
}
