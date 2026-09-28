using NSubstitute;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Application.Commands.Users.AssignRole;
using PrintGrid.Modules.Customer.Application.Commands.Users.CreateUser;
using PrintGrid.Modules.Customer.Application.Commands.Users.DeactivateUser;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using CustomerEntity = PrintGrid.Modules.Customer.Domain.Entities.Customer;

namespace PrintGrid.UnitTests.Customer;

public class AdminUserManagementTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();

    public AdminUserManagementTests()
    {
        _hasher.Hash(Arg.Any<string>()).Returns("hashed-password");
    }

    [Fact]
    public async Task CreateUser_creates_customer_with_role_and_returns_id()
    {
        _customers.EmailExistsAsync("lab@printgrid.dev", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateUserCommandHandler(_customers, _unitOfWork, _hasher);

        var result = await handler.Handle(
            new CreateUserCommand("lab@printgrid.dev", "secret", "Lab Manager", null, "LabManager"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _customers.Received(1).AddAsync(Arg.Is<CustomerEntity>(c => c.Email == "lab@printgrid.dev" && c.Roles.Contains("LabManager")), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUser_rejects_a_duplicate_email()
    {
        _customers.EmailExistsAsync("admin@printgrid.dev", Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateUserCommandHandler(_customers, _unitOfWork, _hasher);

        var result = await handler.Handle(
            new CreateUserCommand("admin@printgrid.dev", "secret", "Admin User", null, "Admin"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        await _customers.DidNotReceive().AddAsync(Arg.Any<CustomerEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignRole_assigns_role_to_existing_user()
    {
        var targetUser = CustomerEntity.Register("user@printgrid.dev", "hash", "User", null);
        var actingUserId = Guid.NewGuid();
        _customers.GetByIdAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(targetUser);

        var handler = new AssignRoleCommandHandler(_customers, _unitOfWork);
        var result = await handler.Handle(new AssignRoleCommand(targetUser.Id, "LabOperator", actingUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        targetUser.Roles.Should().ContainSingle().Which.Should().Be("LabOperator");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignRole_rejects_self_assignment()
    {
        var targetUser = CustomerEntity.Register("admin@printgrid.dev", "hash", "Admin", null);
        var handler = new AssignRoleCommandHandler(_customers, _unitOfWork);
        
        var result = await handler.Handle(new AssignRoleCommand(targetUser.Id, "Admin", targetUser.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation_error");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignRole_rejects_not_found_user()
    {
        var actingUserId = Guid.NewGuid();
        _customers.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((CustomerEntity?)null);
        var handler = new AssignRoleCommandHandler(_customers, _unitOfWork);
        
        var result = await handler.Handle(new AssignRoleCommand(Guid.NewGuid(), "Admin", actingUserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task DeactivateUser_deactivates_existing_user()
    {
        var targetUser = CustomerEntity.Register("user@printgrid.dev", "hash", "User", null);
        var actingUserId = Guid.NewGuid();
        _customers.GetByIdAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(targetUser);

        var handler = new DeactivateUserCommandHandler(_customers, _unitOfWork);
        var result = await handler.Handle(new DeactivateUserCommand(targetUser.Id, actingUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        targetUser.IsActive.Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateUser_rejects_self_deactivation()
    {
        var targetUser = CustomerEntity.Register("admin@printgrid.dev", "hash", "Admin", null);
        var handler = new DeactivateUserCommandHandler(_customers, _unitOfWork);
        
        var result = await handler.Handle(new DeactivateUserCommand(targetUser.Id, targetUser.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation_error");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
