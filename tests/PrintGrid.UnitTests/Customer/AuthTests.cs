using NSubstitute;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Application.Commands.Auth.Login;
using PrintGrid.Modules.Customer.Application.Commands.Auth.Register;
using PrintGrid.Modules.Customer.Application.Commands.Auth.GoogleLogin;
using PrintGrid.Modules.Customer.Application.Commands.Auth.ForgotPassword;
using PrintGrid.Modules.Customer.Application.Commands.Auth.ResetPassword;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using CustomerEntity = PrintGrid.Modules.Customer.Domain.Entities.Customer;
using Microsoft.Extensions.Caching.Distributed;

namespace PrintGrid.UnitTests.Customer;

public class RegisterLoginTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration = Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>();

    private readonly AuthTokenPairDto _pair = new("access-token", "refresh-token");

    public RegisterLoginTests()
    {
        _hasher.Hash(Arg.Any<string>()).Returns("hashed-password");
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokens.CreateTokenPair(Arg.Any<AuthUserDto>(), Arg.Any<int>()).Returns(_pair);
        _configuration["App:FrontendUrl"].Returns("http://localhost:5173");
    }

    [Fact]
    public async Task Register_creates_customer_and_returns_a_session()
    {
        _customers.EmailExistsAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new RegisterCustomerCommandHandler(_customers, _unitOfWork, _hasher, _tokens, _emailService, _configuration);

        var result = await handler.Handle(
            new RegisterCustomerCommand("Nguyen A", "a@b.com", "secret123", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.User.Email.Should().Be("a@b.com");
        result.Value.User.Roles.Should().Contain("Customer");
        await _customers.Received(1).AddAsync(Arg.Is<CustomerEntity>(c => c.Email == "a@b.com"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendEmailAsync("a@b.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_rejects_a_duplicate_email()
    {
        _customers.EmailExistsAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(true);
        var handler = new RegisterCustomerCommandHandler(_customers, _unitOfWork, _hasher, _tokens, _emailService, _configuration);

        var result = await handler.Handle(
            new RegisterCustomerCommand("Nguyen A", "a@b.com", "secret123", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("email_exists");
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

    [Fact]
    public async Task Google_login_creates_verified_customer_and_returns_session()
    {
        var google = Substitute.For<IGoogleIdentityVerifier>();
        google.VerifyAsync("credential", Arg.Any<CancellationToken>())
            .Returns(new GoogleIdentity("google-sub", "google@example.com", "Google User", true));
        _customers.GetByGoogleSubjectAsync("google-sub", Arg.Any<CancellationToken>()).Returns((CustomerEntity?)null);
        _customers.GetByEmailAsync("google@example.com", Arg.Any<CancellationToken>()).Returns((CustomerEntity?)null);
        var handler = new GoogleLoginCommandHandler(google, _customers, _unitOfWork, _hasher, _tokens);

        var result = await handler.Handle(new GoogleLoginCommand("credential"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.User.IsEmailVerified.Should().BeTrue();
        await _customers.Received(1).AddAsync(
            Arg.Is<CustomerEntity>(c => c.GoogleSubject == "google-sub" && c.IsEmailVerified),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Forgot_password_does_not_reveal_unknown_email()
    {
        var cache = Substitute.For<IDistributedCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        _customers.GetByEmailAsync("unknown@example.com", Arg.Any<CancellationToken>()).Returns((CustomerEntity?)null);
        var handler = new ForgotPasswordCommandHandler(
            _customers, _unitOfWork, _hasher, _emailService, _configuration, cache);

        var result = await handler.Handle(new ForgotPasswordCommand("unknown@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _emailService.DidNotReceiveWithAnyArgs().SendEmailAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task Reset_password_consumes_valid_token()
    {
        var customer = CustomerEntity.Register("reset@example.com", "old-hash", "Reset User", null);
        customer.SetPasswordResetToken("reset-token-hash", TimeSpan.FromMinutes(30));
        _customers.GetByEmailAsync("reset@example.com", Arg.Any<CancellationToken>()).Returns(customer);
        _hasher.Verify("raw-token", "reset-token-hash").Returns(true);
        _hasher.Hash("new-password").Returns("new-hash");
        var handler = new ResetPasswordCommandHandler(_customers, _unitOfWork, _hasher);

        var result = await handler.Handle(
            new ResetPasswordCommand("reset@example.com", "raw-token", "new-password"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        customer.PasswordHash.Should().Be("new-hash");
        customer.PasswordResetTokenHash.Should().BeNull();
        customer.SessionVersion.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
