using MediatR;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace PrintGrid.Api.Bootstrap;

/// <summary>
/// Seeds demo data so the teacher can see the full flow immediately:
/// a demo customer with analysed models, a ready quote, sample orders, plus
/// a small network (labs & machines) and one job already assigned to a lab —
/// so the lab page can Accept → Start → Complete end to end.
/// Idempotent — skips everything if the demo account already exists.
/// </summary>
public class DemoDataSeeder
{
    public const string DemoEmail = "demo@printgrid.dev";
    public const string DemoPassword = "Demo@123";

    private readonly PrintGridDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public DemoDataSeeder(
        PrintGridDbContext db,
        IPasswordHasher hasher,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _db = db;
        _hasher = hasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRoleAccountsAsync(cancellationToken);

        var demo = await _db.Set<Customer>().FirstOrDefaultAsync(
            c => c.Email == DemoEmail, cancellationToken);
        if (demo is not null)
        {
            // Demo account exists from an earlier run — still ensure labs/machines exist.
            await SeedNetworkAsync(cancellationToken);
            return;
        }

        demo = Customer.Register(DemoEmail, _hasher.Hash(DemoPassword), "Khách hàng Demo", "0900000000");
        _db.Set<Customer>().Add(demo);
        await _db.SaveChangesAsync(cancellationToken);

        // 3 models with already-analysed geometry.
        var vase = SeedModel(demo.Id, "Bình hoa trang trí", "vase.stl", "STL", 3_248_000, 72m, 62m, 84m, 245.6m, 340);
        var phoneStand = SeedModel(demo.Id, "Đế điện thoại", "phone_stand.obj", "OBJ", 812_400, 60m, 48m, 18m, 41.2m, 96);
        var gear = SeedModel(demo.Id, "Bánh răng 60 răng", "gear60.stl", "STL", 1_536_900, 92m, 92m, 22m, 118.4m, 210);
        _db.Set<Model>().AddRange(vase, phoneStand, gear);
        await _db.SaveChangesAsync(cancellationToken);

        // One ready quote on the vase (PETG, signal red, standard quality).
        var quote = Quote.CreatePending(demo.Id, TimeSpan.FromHours(48));
        quote.AddItem(
            vase.Id,
            1,
            PrintConfiguration.Create("PETG", "SIGNALRED", 0.2m, 30, 0.2m),
            Money.Of(92_000m),
            200,
            60m);
        quote.MarkReady(_clock.Today.AddDays(5));
        _db.Set<Quote>().Add(quote);
        await _db.SaveChangesAsync(cancellationToken);

        // A sample order derived from a converted quote (delivered).
        var prior = Quote.CreatePending(demo.Id, TimeSpan.FromHours(48));
        prior.AddItem(
            gear.Id,
            2,
            PrintConfiguration.Create("PLA", "BLACK", 0.2m, 30, 0.2m),
            Money.Of(64_500m),
            180,
            45m);
        prior.MarkReady(_clock.Today.AddDays(-6));
        await _db.Set<Quote>().AddAsync(prior, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        prior.MarkConverted(_clock.UtcNow);

        var address = Address.Create("1 Dai Co Viet", "Bach Khoa", "Hai Ba Trung", "Ha Noi", "100000");
        var order = Order.CreateFromQuote(prior, address, $"PG-{_clock.Today:yyyyMMdd}-00001");
        order.ConfirmPayment("demo-txn-001");
        order.TransitionTo(OrderStatus.Delivered);
        _db.Set<Order>().Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        await SeedNetworkAsync(cancellationToken);
    }

    /// <summary>
    /// Creates the per-role demo accounts used to show that each role lands in its
    /// own area (lab → /lab/queue, hub → /hub/qc, ops → /scheduling). Idempotent by email.
    /// </summary>
    private async Task SeedRoleAccountsAsync(CancellationToken cancellationToken)
    {
        var (lab, hub, ops) = (
            PrintGrid.Modules.Customer.Application.Auth.DemoRoles.LabEmail,
            PrintGrid.Modules.Customer.Application.Auth.DemoRoles.HubEmail,
            PrintGrid.Modules.Customer.Application.Auth.DemoRoles.OpsEmail);

        if (!await _db.Set<Customer>().AnyAsync(c => c.Email == lab, cancellationToken))
            _db.Set<Customer>().Add(Customer.Register(lab, _hasher.Hash(DemoPassword), "Lab Manager Demo", "0911000001"));
        if (!await _db.Set<Customer>().AnyAsync(c => c.Email == hub, cancellationToken))
            _db.Set<Customer>().Add(Customer.Register(hub, _hasher.Hash(DemoPassword), "Hub QC Demo", "0911000002"));
        if (!await _db.Set<Customer>().AnyAsync(c => c.Email == ops, cancellationToken))
            _db.Set<Customer>().Add(Customer.Register(ops, _hasher.Hash(DemoPassword), "Ops Manager Demo", "0911000003"));

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Seeds a small lab network plus an assigned job (idempotent).</summary>
    private async Task SeedNetworkAsync(CancellationToken cancellationToken)
    {
        if (!await _db.Set<Lab>().AnyAsync(cancellationToken))
        {
            var labA = Lab.Onboard("Lab FPT Bách Khoa", "Hà Nội", 1);
            labA.UpdatePerformance(0.95m, 0.92m);
            labA.AddMachine(Machine.Register(
                labA.Id, "Prusa i3 MK3S #1", "Prusa i3", PrintTechnology.Fdm,
                BuildVolume.Create(250m, 210m, 210m), minLayerHeightMm: 0.05m, achievableToleranceMm: 0.2m,
                new[] { "PLA", "PETG", "ABS", "TPU" }));
            labA.AddMachine(Machine.Register(
                labA.Id, "Bambu X1C #2", "Bambu Lab X1", PrintTechnology.Fdm,
                BuildVolume.Create(256m, 256m, 256m), minLayerHeightMm: 0.04m, achievableToleranceMm: 0.1m,
                new[] { "PLA", "PETG", "ABS", "TPU" }));

            var labB = Lab.Onboard("Lab 3D Sài Gòn", "TP. Hồ Chí Minh", 2);
            labB.UpdatePerformance(0.88m, 0.90m);
            labB.AddMachine(Machine.Register(
                labB.Id, "Elegoo Saturn #1", "Elegoo Saturn 3", PrintTechnology.Sla,
                BuildVolume.Create(218m, 122m, 250m), minLayerHeightMm: 0.01m, achievableToleranceMm: 0.05m,
                new[] { "RESIN" }));

            _db.Set<Lab>().AddRange(labA, labB);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Keep at least one Assigned job so the Lab page always has work after a demo run.
        if (await _db.Set<Job>().AnyAsync(j => j.Status == JobStatus.Assigned, cancellationToken))
            return;

        var machine = await _db.Set<Machine>()
            .OrderBy(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (machine is null) return;

        var spec = JobSpecification.Create(
            BuildVolume.Create(92m, 92m, 22m), "PLA", "BLACK", 0.2m, 0.2m, PrintTechnology.Fdm, materialGrams: 90m);
        var job = Job.Create(
            Guid.NewGuid(), Guid.NewGuid(), spec, estimatedPrintMinutes: 180,
            _clock.Today.AddDays(4));

        var now = _clock.UtcNow;
        job.AssignTo(machine.LabId, machine.Id, now, now.AddHours(4), 0.82m);
        _db.Set<Job>().Add(job);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static Model SeedModel(
        Guid customerId,
        string name,
        string fileName,
        string format,
        long sizeBytes,
        decimal w,
        decimal d,
        decimal h,
        decimal volumeCm3,
        int minutes)
    {
        var model = Model.Create(customerId, name, $"{name} — model demo", fileName, format, sizeBytes, null);
        model.ApplyGeometry(w, d, h, volumeCm3, minutes);
        return model;
    }
}