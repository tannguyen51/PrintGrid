using NSubstitute;
using PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.UnitTests.Scheduling;

public class LabRegistryTests
{
    private readonly ILabRepository _labs = Substitute.For<ILabRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task RegisterLab_creates_and_saves_active_lab()
    {
        var handler = new RegisterLabCommandHandler(_labs, _unitOfWork);

        var result = await handler.Handle(new RegisterLabCommand("Test Lab", "Hà Nội", 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Lab");
        result.Value.IsActive.Should().BeTrue();
        result.Value.Machines.Should().BeEmpty();
        await _labs.Received(1).AddAsync(Arg.Any<Lab>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterMachine_adds_machine_to_existing_lab()
    {
        var lab = Lab.Onboard("Test Lab", "Hà Nội", 1);
        _labs.GetByIdAsync(lab.Id, Arg.Any<CancellationToken>()).Returns(lab);
        var handler = new RegisterMachineCommandHandler(_labs, _unitOfWork);

        var result = await handler.Handle(
            new RegisterMachineCommand(
                lab.Id,
                "Prusa #1",
                "Prusa i3 MK3S",
                PrintTechnology.Fdm,
                250, 210, 210,
                0.05m,
                0.15m,
                new[] { "PLA", "PETG" }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BuildWidthMm.Should().Be(250);
        result.Value.Technology.Should().Be(PrintTechnology.Fdm);
        result.Value.SupportedMaterials.Should().Contain("PLA");
        lab.Machines.Should().ContainSingle();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterMachine_unknown_lab_fails_with_not_found()
    {
        var handler = new RegisterMachineCommandHandler(_labs, _unitOfWork);

        var result = await handler.Handle(
            new RegisterMachineCommand(Guid.NewGuid(), "M", "X", PrintTechnology.Fdm, 100, 100, 100, 0.1m, 0.2m, new[] { "PLA" }),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterMachine_deactivated_lab_rejects_new_machines()
    {
        var lab = Lab.Onboard("Test Lab", "Hà Nội", 1);
        lab.Deactivate();
        _labs.GetByIdAsync(lab.Id, Arg.Any<CancellationToken>()).Returns(lab);
        var handler = new RegisterMachineCommandHandler(_labs, _unitOfWork);

        var result = await handler.Handle(
            new RegisterMachineCommand(lab.Id, "M", "X", PrintTechnology.Fdm, 100, 100, 100, 0.1m, 0.2m, new[] { "PLA" }),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetMachineStatus_changes_status_on_owned_machine()
    {
        var lab = Lab.Onboard("Test Lab", "Hà Nội", 1);
        var machine = Machine.Register(lab.Id, "Prusa #1", "MK3S", PrintTechnology.Fdm,
            PrintGrid.Modules.Scheduling.Domain.ValueObjects.BuildVolume.Create(250, 210, 210),
            0.05m, 0.15m, new[] { "PLA" });
        lab.AddMachine(machine);
        _labs.GetByIdAsync(lab.Id, Arg.Any<CancellationToken>()).Returns(lab);
        var handler = new SetMachineStatusCommandHandler(_labs, _unitOfWork);

        var result = await handler.Handle(
            new SetMachineStatusCommand(lab.Id, machine.Id, MachineStatus.Maintenance),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        machine.Status.Should().Be(MachineStatus.Maintenance);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}