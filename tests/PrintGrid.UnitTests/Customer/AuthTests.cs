using NSubstitute;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Application.Commands.Auth.Login;
using PrintGrid.Modules.Customer.Application.Commands.Auth.Register;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using CustomerEntity = PrintGrid.Modules.Customer.Domain.Entities.Customer;

namespace PrintGrid.UnitTests.Customer;

public class RegisterLoginTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();

    private readonly AuthTokenPairDto _pair = new("access-token", "refresh-token");

    public RegisterLoginTests()
    {
        _hasher.Hash(Arg.Any<string>()).Returns("hashed-password");
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokens.CreateTokenPair(Arg.Any<AuthUserDto>()).Returns(_pair);
    }

    [Fact]
    public async Task Register_creates_customer_and_returns_a_session()
    {
        _customers.EmailExistsAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new RegisterCustomerCommandHandler(_customers, _unitOfWork, _hasher, _tokens);

        var result = await handler.Handle(
            new RegisterCustomerCommand("Nguyen A", "a@b.com", "secret123", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.User.Email.Should().Be("a@b.com");
        result.Value.User.Roles.Should().Contain("Customer");
        await _customers.Received(1).AddAsync(Arg.Is<CustomerEntity>(c => c.Email == "a@b.com"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_rejects_a_duplicate_email()
    {
        _customers.EmailExistsAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(true);
        var handler = new RegisterCustomerCommandHandler(_customers, _unitOfWork, _hasher, _tokens);

        var result = await handler.Handle(
            new RegisterCustomerCommand("Nguyen A", "a@b.com", "secret123", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        await _customers.DidNotReceive().AddAsync(Arg.Any<CustomerEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_returns_a_session_for_valid_credentials()
    {
        var customer = CustomerEntity.Register("a@b.com", "hashed", "Nguyen A", null);
        _customers.GetByEmailAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(customer);
        _hasher.Verify("secret123", "hashed").Returns(true);
        var handler = new LoginCustomerCommandHandler(_customers, _unitOfWork, _hasher, _tokens);

        var result = await handler.Handle(
            new LoginCustomerCommand("a@b.com", "secret123"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.User.Id.Should().Be(customer.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_rejects_a_known_email_with_a_wrong_password()
    {
        var customer = CustomerEntity.Register("a@b.com", "hashed", "Nguyen A", null);
        _customers.GetByEmailAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(customer);
        _hasher.Verify("wrong", "hashed").Returns(false);
        var handler = new LoginCustomerCommandHandler(_customers, _unitOfWork, _hasher, _tokens);

        var result = await handler.Handle(
            new LoginCustomerCommand("a@b.com", "wrong"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("unauthorized");
    }

    [Fact]
    public async Task Login_does_not_reveal_whether_an_email_exists()
    {
        _customers.GetByEmailAsync("ghost@b.com", Arg.Any<CancellationToken>()).Returns((CustomerEntity?)null);
        var handler = new LoginCustomerCommandHandler(_customers, _unitOfWork, _hasher, _tokens);

        var result = await handler.Handle(
            new LoginCustomerCommand("ghost@b.com", "whatever"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("unauthorized");
    }
}